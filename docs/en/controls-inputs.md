# Input Controls Reference

This document covers the text-input, numeric-input and selection controls in WinformUi: their properties, events and usage. All APIs match the sources under the `WinformUi.Controls` namespace. The example snippets assume that namespaces such as `WinformUi.Controls` have been imported.

Note: `FluentTextBox`, `FluentPasswordBox`, `FluentNumberBox` and `FluentAutoSuggestBox` all derive from the library's `FluentTextInputBase` (a composite control made of a rounded shell plus an embedded native TextBox). The input properties they share are listed once, in the FluentTextBox section; the other controls only list the members they add themselves. `FluentMaskedTextBox` uses the same Fluent rounded shell but embeds a native `MaskedTextBox` in order to keep the full input-mask, validation and editing behavior.

## FluentTextBox

Fluent single-line/multi-line text box: a rounded shell with hover/focus visuals, a 2px accent-colored underline shown at the bottom when focused, plus placeholder text and a left-side icon.

### Properties

The following properties are declared on the shared base class FluentTextInputBase and are available on all four input controls:

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| PlaceholderText | string | "" | Placeholder hint text, shown only while the content is empty |
| Icon | SymbolRegular | SymbolRegular.Empty | Left-side icon; once set, a 32px left margin is reserved for the icon automatically |
| Multiline | bool | false | Whether multi-line mode is on |
| ReadOnly | bool | false | Whether the control is read-only |
| MaxLength | int | 32767 | Maximum number of characters that can be entered |
| TextAlign | HorizontalAlignment | HorizontalAlignment.Left | Horizontal text alignment |
| ScrollBars | ScrollBars | ScrollBars.None | Scroll bars; must be used together with Multiline = true |
| InnerTextBox | TextBox | (internal instance, read-only) | The native TextBox that carries the input, exposed for advanced scenarios |
| SelectedText | string | "" | The currently selected text (read/write) |
| SelectionStart | int | 0 | Start position of the selection |
| SelectionLength | int | 0 | Length of the selection |

The methods `SelectAll()`, `Clear()` and `Focus()` are also provided (Focus hands the focus over to the inner text box).

### Events

No additional events; inherits the standard WinForms events. `TextChanged`, `KeyDown`, `KeyPress` and `KeyUp` are forwarded from the inner text box to the shell control, so they can be subscribed to directly.

### Example

```csharp
var search = new FluentTextBox
{
    PlaceholderText = "搜索...",
    Icon = SymbolRegular.Search24,
    Width = 240
};
search.TextChanged += (s, e) => DoSearch(search.Text);
Controls.Add(search);

var notes = new FluentTextBox
{
    Multiline = true,
    ScrollBars = ScrollBars.Vertical,
    PlaceholderText = "输入备注(可多行)",
    Size = new Size(320, 120)
};
Controls.Add(notes);
```

### Notes

- `ScrollBars` is only meaningful when `Multiline = true`; this is the native TextBox convention. In multi-line mode the inner editing area keeps an 8px margin on the top and on the bottom.
- With `ReadOnly = true` the text can still be selected and copied, it just cannot be edited; to disable the control use `Enabled = false` (both the shell and the text switch to the disabled colors).
- The placeholder text color and the selection color both come from the theme. If you need lower-level control (input masks, for instance), reach the native TextBox through `InnerTextBox`.

## FluentMaskedTextBox

A masked input box with a Fluent rounded shell. Internally it uses a native `MaskedTextBox`, so the native mask validation, insert/overwrite mode, clipboard format, IME, caret and selection behavior are all preserved; the outer control is responsible for the theme, icon, placeholder hint and focus visuals.

It suits inputs where "the character positions and the format are fixed", such as phone numbers, dates, ID card numbers and postal codes. If you only want to limit the length of ordinary text, `FluentTextBox.MaxLength` is simpler; if you need "typed text + a candidate list", use `FluentAutoSuggestBox`.

### Quick Start

The example below can be dropped straight into a plain `Form`. It demonstrates masked input, rejected-input hints, type conversion and error display:

```csharp
using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using WinformUi.Controls;

public sealed class MaskedDemoForm : Form
{
    private readonly FluentErrorProvider _errors = new FluentErrorProvider();
    private readonly FluentMaskedTextBox _date = new FluentMaskedTextBox
    {
        Location = new Point(20, 20),
        Size = new Size(260, 32),
        Mask = "00/00/0000",
        PlaceholderText = "MM/DD/YYYY",
        Icon = SymbolRegular.CalendarDay16,
        ValidatingType = typeof(DateTime),
        Culture = CultureInfo.GetCultureInfo("en-US")
    };

    public MaskedDemoForm()
    {
        Text = "MaskedTextBox demo";
        ClientSize = new Size(360, 150);

        _date.MaskInputRejected += (s, e) =>
            _errors.SetError(_date, "此处只能输入数字,位置:" + e.Position);

        _date.TypeValidationCompleted += (s, e) =>
        {
            if (e.IsValidInput)
                _errors.ClearError(_date);
            else
                _errors.SetError(_date, e.Message ?? "日期无效");
        };

        var validate = new FluentButton
        {
            Text = "校验",
            Location = new Point(20, 70),
            AutoSize = true,
            Appearance = ControlAppearance.Primary
        };
        validate.Click += (s, e) =>
        {
            object value = _date.ValidateText();
            if (value is DateTime date)
                Text = date.ToString("D", _date.Culture);
            else if (_date.ValidatingType == null)
                _errors.SetError(_date, "尚未设置 ValidatingType");
            else if (!_date.MaskCompleted)
                _errors.SetError(_date, "请完整填写日期");
            else
                _errors.SetError(_date, "日期格式无效");
        };

        Controls.AddRange(new Control[] { _date, validate });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _errors.Dispose();
        base.Dispose(disposing);
    }
}
```

The argument of `TypeValidationCompleted` uses **`e.IsValidInput`**, not `e.IsValid`. When the conversion succeeds, `e.ReturnValue` holds the converted object; on failure it is usually null, and the native hint can be read from `e.Message`.

### Mask Character Cheat Sheet

| Mask character | Meaning | Example |
| --- | --- | --- |
| `9` | Optional digit 0–9 | `99999` five-digit postal code, fewer entries allowed |
| `0` | Required digit 0–9 | `000-0000` both the area code and the number must be filled |
| `L` | Required letter | `LLLLLLLLLL` fixed-length letter string |
| `?` | Optional letter | Allows the letter sequence to end early |
| `A` | Required letter or digit | Product codes |
| `a` | Optional letter or digit | An optional alphanumeric position |
| `&` | Required character of any kind | Fixed-length verification code |
| `C` | Optional character of any kind | An optional position that accepts any character |
| `<` / `>` | Converts the following characters to lowercase / uppercase | `<LLL` converts to lowercase |
| `\` | Escapes the next mask character, turning it into a literal | `\9` displays a plain character 9 |

The date separator `/` is rendered as the date separator of the region given by `Culture`. Phone numbers can be written directly as `"(000) 000-0000"`; the spaces, parentheses and hyphens in the mask are literals that are displayed automatically, so the user does not have to type them.

### Properties

#### Fluent Appearance

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| PlaceholderText | string | `""` | Cue banner hint shown while the content is empty; supports Chinese. It is a different kind of hint from `PromptChar` |
| Icon | SymbolRegular | `SymbolRegular.Empty` | Left-side icon; when not Empty, a 32px icon area is reserved |
| Font / Enabled / RightToLeft / ImeMode | Inherited properties | — | Synced to the inner native editor; the layout is recalculated when RTL is toggled |

#### Mask and Input Behavior

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| Mask | string | `""` | Input mask; null is normalized to an empty string |
| PromptChar | char | `'_'` | Prompt character for unfilled positions; it is not caret text |
| BeepOnError | bool | `true` | Whether a beep is played when illegal input is rejected |
| TextMaskFormat | MaskFormat | `IncludePromptAndLiterals` | Determines whether `Text` includes prompt characters and mask literals |
| CutCopyMaskFormat | MaskFormat | `IncludeLiterals` | Determines the format of cut/copied text |
| InsertKeyMode | InsertKeyMode | `Default` | Native insert/overwrite mode policy |
| SkipLiterals | bool | `true` | Whether mask literals are skipped automatically while typing |
| ResetOnPrompt | bool | `true` | Whether typing the prompt character resets the following input |
| ResetOnSpace | bool | `true` | Whether typing a space resets the following input |
| RejectInputOnFirstFailure | bool | `false` | Whether the whole input is rejected after the first failure |
| AllowPromptAsInput | bool | `false` | Whether the prompt character is accepted as valid input |
| AsciiOnly | bool | `false` | Whether input is restricted to ASCII characters |
| ReadOnly | bool | `false` | Read-only; the text can still be selected and copied. To forbid editing use Enabled = false |
| MaxLength | int | `32767` | Maximum length; the actual input is additionally constrained by Mask |
| TextAlign | HorizontalAlignment | `Left` | Horizontal text alignment |

#### Type Validation and Password Display

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| ValidatingType | Type | `null` | Target type for validation, for example `typeof(DateTime)`, `typeof(int)`, `typeof(decimal)` |
| Culture | CultureInfo | Inherits the current regional culture at construction time | Culture used to parse numbers/dates; assigning null throws `ArgumentNullException` |
| UseSystemPasswordChar | bool | `false` | Uses the system password character; takes precedence over PasswordChar |
| PasswordChar | char | `'\0'` | Custom password character; effective only when UseSystemPasswordChar = false |
| MaskCompleted | bool | Depends on Mask and Text, read-only | Whether every **required** position has been filled |
| MaskFull | bool | Depends on Mask and Text, read-only | Whether all required and optional positions have been filled |

`MaskCompleted` and `MaskFull` are not required to be true at the same time. For example, when the mask contains optional digit positions, the type validation can succeed as soon as the required positions are filled and `MaskCompleted` passes, while `MaskFull` is still false.

#### Text, Selection and Inner Editor

| Member | Type | Description |
| --- | --- | --- |
| Text | string | Forwards the inner Text; assigning null becomes an empty string; the exact format is determined by TextMaskFormat |
| SelectedText | string | Text of the current selection, read/write |
| SelectionStart | int | Start of the selection, read/write |
| SelectionLength | int | Length of the selection, read/write |
| InnerMaskedTextBox | MaskedTextBox | The inner native editor, a read-only reference; advanced scenarios can use native members that are not exposed on the outer control |

`InnerMaskedTextBox`, `MaskCompleted`, `MaskFull` and the selection properties do not appear in the designer property grid.

### Methods

| Method | Description |
| --- | --- |
| `void SelectAll()` | Selects all content in the inner editor |
| `void Clear()` | Clears the input, keeping Mask and the other settings |
| `new bool Focus()` | Hands the focus to the inner MaskedTextBox and returns whether it succeeded |
| `object ValidateText()` | Converts the current text according to ValidatingType and returns the boxed value; returns null on failure |

Detailed behavior of `ValidateText()`:

1. When `ValidatingType == null` it returns null and does not raise the type validation event.
2. When the required mask positions are not filled, `ValidateText()` returns `null`; the native editor raises `TypeValidationCompleted` on some of the losing-focus validation paths.
3. When the conversion succeeds it returns a boxed object such as `DateTime` or `int`, and the event argument has `e.IsValidInput == true`.
4. Exceptions thrown inside the event handler keep propagating along the .NET event invocation chain; robust validation should therefore be wrapped in try/catch by your business code.

When you call `ValidateText()` explicitly, a `null` return can mean any of three things: "no ValidatingType", "mask not completed", or "conversion failed". To tell them apart, check `ValidatingType` and `MaskCompleted` first, then combine the event arguments or your own parsing logic.

### Events

| Event | When it is raised / usage |
| --- | --- |
| MaskChanged | Raised after Mask actually changes |
| MaskInputRejected | Raised when user input is rejected by the mask; Position and RejectionHint can be read |
| TypeValidationCompleted | Raised when the native type validation completes, including explicit ValidateText() calls and losing-focus validation; read IsValidInput, ReturnValue, Message, ValidatingType |
| TextChanged | Raised after the inner text changes |
| GotFocus / LostFocus | The inner editor gains/loses focus; the outer control syncs the focus underline |
| KeyDown / KeyPress / KeyUp / Validating | Forwarded from the inner editor and can be subscribed to directly on the outer control |

`Validated` is not forwarded separately. If you need that event or other rarely used MaskedTextBox members, go through `InnerMaskedTextBox`. The outer control has `TabStop = false`; Tab keyboard navigation ultimately lands on the inner editor.

### Common Examples

#### Getting Only the Digits the User Typed

```csharp
var phone = new FluentMaskedTextBox
{
    Mask = "(000) 000-0000",
    PlaceholderText = "电话号码",
    Icon = SymbolRegular.Call24,
    TextMaskFormat = MaskFormat.ExcludePromptAndLiterals,
    Width = 260
};

string digits = phone.Text; // For example 13800138000, without parentheses, spaces or hyphens
if (digits.Length == 11) SavePhone(digits);
```

The four options of `TextMaskFormat`:

| Value | What `Text` contains |
| --- | --- |
| IncludePromptAndLiterals | Prompt characters + mask literals |
| IncludeLiterals | Only mask literals, without the prompt characters of unfilled positions |
| ExcludePromptAndLiterals | Neither |
| IncludePrompt | Prompt characters, but without mask literals |

#### Cancel Losing Focus When Validation Fails

```csharp
_date.Validating += (s, e) =>
{
    if (!_date.MaskCompleted)
    {
        _errors.SetError(_date, "请完整填写日期");
        e.Cancel = true;
    }
};
```

`TypeValidationCompleted.Cancel` can also cancel the focus transfer during the native losing-focus validation, but it does not change the return value of a `ValidateText()` call that was already made explicitly.

#### A Password Displayed Through a Mask

```csharp
var pin = new FluentMaskedTextBox
{
    Mask = "000000",
    PromptChar = '*',
    PasswordChar = '●',
    UseSystemPasswordChar = false,
    PlaceholderText = "六位验证码",
    Width = 220
};
```

When you use the system password character, do not set `PromptChar` to the same character as `PasswordChar`; otherwise the native MaskedTextBox may throw `InvalidOperationException`.

### Using the Designer

- After dragging the control from the toolbox, set `Mask`, `PromptChar`, `ValidatingType`, `Culture` and so on in the Behavior category of the properties window, and `PlaceholderText` / `Icon` in the Fluent category.
- `Mask`, `Text`, font, size and position can be serialized together with the form designer; check states and typed results at runtime still have to be handled by your business code.
- The default event is `MaskInputRejected`, so double-clicking the control usually takes you into that event handler.
- The default size is 200×32; when an icon is present the editing area yields an extra 32px on the left, so a width of at least 180px is recommended.

### Common Pitfalls

- `PlaceholderText` is the overall hint shown when the content is empty, whereas `PromptChar` is the placeholder of each unfilled mask position; do not confuse the two.
- `MaskCompleted` is a property; there is no `MaskCompleted` event.
- Type validation is not a guard on your submit button; whether saving is allowed is a decision for your business code.
- `Culture = null` is not allowed. To follow the system culture, do not assign anything; to pin the format, specify an explicit culture.
- `UseSystemPasswordChar = true` makes the custom `PasswordChar` be ignored.
- Only reach for `InnerMaskedTextBox` when you need the fully native MaskedTextBox API; size changes made directly on it may be overwritten by the next Resize/FontChanged of the outer control.

## FluentPasswordBox

Fluent password box (derives from FluentTextInputBase): the entered content is masked with the system password character by default.

### Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| PasswordChar | char | '\0' | Custom mask character; the default is '\0' and the constructor already enables InnerTextBox.UseSystemPasswordChar, so input is displayed as ● |

The shared PlaceholderText, Icon, MaxLength and so on are listed in the FluentTextBox section.

### Events

No additional events; inherits the standard WinForms events.

### Example

```csharp
var password = new FluentPasswordBox
{
    PlaceholderText = "请输入密码",
    Icon = SymbolRegular.LockClosed24,
    Width = 240
};
password.KeyDown += (s, e) =>
{
    if (e.KeyCode == Keys.Enter) Login(password.Text);
};
Controls.Add(password);
```

### Notes

- The `Text` property always returns the real plaintext; the mask only affects the display. To use a custom mask character, set `PasswordChar` and at the same time set `InnerTextBox.UseSystemPasswordChar` to false, otherwise the custom character will not take effect.
- Single-line by default; password boxes generally do not need Multiline.

## FluentNumberBox

Fluent numeric input box (derives from FluentTextInputBase): plus/minus spin buttons embedded on the right side, with keyboard stepping, mouse wheel adjustment and min/max clamping.

### Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| Value | double | 0d | Current numeric value, automatically clamped to Minimum~Maximum |
| Minimum | double | double.MinValue | Smallest allowed value; after a change the current value is re-clamped immediately |
| Maximum | double | double.MaxValue | Largest allowed value; after a change the current value is re-clamped immediately |
| SmallChange | double | 1d | Small step: the increment used by clicking the spin buttons, the ↑/↓ keys, and one mouse wheel notch |
| LargeChange | double | 10d | Large step: the increment used by the PageUp/PageDown keys |

### Events

- `ValueChanged` (EventHandler): raised when `Value` changes (triggered by the keyboard, the buttons, the mouse wheel and by assigning the value in code alike).

### Example

```csharp
var count = new FluentNumberBox
{
    Value = 42,
    Minimum = 0,
    Maximum = 1000,
    SmallChange = 1,
    LargeChange = 10,
    Width = 180
};
count.ValueChanged += (s, e) => UpdateQuantity(count.Value);
Controls.Add(count);
```

### Notes

- Text typed manually is committed when Enter is pressed or when the control loses focus: it is parsed with the current regional culture (CurrentCulture), and if parsing fails the previous value is restored; input outside the min/max bounds is clamped.
- A fixed 56px on the right side of the control is reserved for the two spin buttons, so this space has to be accounted for when you set the width.
- `Value` stays in sync with the displayed text, and the display format also uses CurrentCulture; to read and write the number, prefer `Value` over `Text`.

## FluentAutoSuggestBox

Fluent auto-suggest box (derives from FluentTextInputBase): while the user types, a candidate list pops up underneath, and choosing a candidate fills the text back in.

### Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| Items | IList&lt;string&gt; | Empty List&lt;string&gt; | The full set of candidates; the control filters them live against the typed text |

### Events

- `SuggestionChosen` (EventHandler): raised after the user picks a candidate by clicking it or by pressing Enter, once the text has been filled back in.

### Example

```csharp
var suggest = new FluentAutoSuggestBox
{
    PlaceholderText = "输入水果名称...",
    Width = 240
};
suggest.Items = new List<string>
{
    "Apple", "Apricot", "Banana", "Blueberry",
    "Cherry", "Grape", "Mango", "Orange"
};
suggest.SuggestionChosen += (s, e) =>
{
    Console.WriteLine("已选择:" + suggest.Text);
};
Controls.Add(suggest);
```

### Notes

- The filtering rule is "contains" matching (case-insensitive, compared with the current culture), not prefix matching; the list closes automatically when the input is empty, when nothing matches, or when the control loses focus.
- The popup list does not steal focus (it is a NoActivate popup window), so you can keep typing while moving the highlight with ↑/↓, confirm with Enter, and close with Esc; the list height is capped at 280px.
- A search icon is drawn on the right side of the control and 32px are reserved for it; `SuggestionChosen` is raised only when a candidate is actually chosen, not for manually typed text.

## FluentCheckBox

Fluent checkbox: a 20px rounded square, filled with the accent color plus a check mark when checked, with three-state support (the indeterminate state shows a dash).

### Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| Checked | bool | false | Whether the box is checked; reading and writing map onto CheckState (Checked/Unchecked) |
| ThreeState | bool | false | Whether the three-state cycle is enabled |
| CheckState | CheckState | CheckState.Unchecked | Check state: Unchecked / Checked / Indeterminate |
| AutoSize | bool | true | Sizes itself to the text automatically (20px box + 8px gap + text width) |

### Events

- `CheckedChanged` (EventHandler): raised when the `Checked` boolean value changes.
- `CheckStateChanged` (EventHandler): raised when `CheckState` changes (more precise in three-state scenarios).

### Example

```csharp
var agree = new FluentCheckBox { Text = "我已阅读并同意条款" };
agree.CheckedChanged += (s, e) => submitButton.Enabled = agree.Checked;
Controls.Add(agree);

var partial = new FluentCheckBox
{
    Text = "全选(三态)",
    ThreeState = true,
    CheckState = CheckState.Indeterminate
};
Controls.Add(partial);
```

### Notes

- With `ThreeState = true` the click cycle is: unchecked → checked → indeterminate → unchecked; `Checked` is also true in the Indeterminate state (that is, it means "not Unchecked").
- Keyboard supported: press the space key while focused to toggle the state; a change of `CheckState` raises both CheckedChanged and CheckStateChanged (as long as the state really changed).
- The default size is 120x24, but with `AutoSize = true` the control shrinks to a suitable size once Text/Font are set.

## FluentRadioButton

Fluent radio button: a 20px circle, filled with the accent color plus a white dot when checked; buttons that share the same parent container are mutually exclusive automatically.

### Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| Checked | bool | false | Whether the button is checked; setting it to true automatically unchecks the other FluentRadioButton instances in the same parent container |
| AutoSize | bool | true | Sizes itself to the text automatically (20px circle + 8px gap + text width) |

### Events

- `CheckedChanged` (EventHandler): raised when `Checked` changes (including when the button gets displaced by another button of the same group).

### Example

```csharp
var group = new Panel { AutoSize = true };

var light = new FluentRadioButton { Text = "浅色", Checked = true, Location = new Point(0, 0) };
var dark = new FluentRadioButton { Text = "深色", Location = new Point(0, 28) };
var system = new FluentRadioButton { Text = "跟随系统", Location = new Point(0, 56) };

light.CheckedChanged += (s, e) => { if (light.Checked) SetTheme("light"); };
dark.CheckedChanged += (s, e) => { if (dark.Checked) SetTheme("dark"); };
system.CheckedChanged += (s, e) => { if (system.Checked) SetTheme("system"); };

group.Controls.AddRange(new Control[] { light, dark, system });
Controls.Add(group);
```

### Notes

- The exclusivity scope is "the same Parent container"; there is no GroupName concept. For several radio groups, put each group into its own Panel.
- Consistent with the standard RadioButton: clicking an already selected button does not uncheck it; the selection can only change by checking another button of the same group or by assigning values in code.
- The space key can check the button; while pressed, the central dot shrinks to give Fluent-style press feedback.

## FluentToggleSwitch

Fluent toggle switch: a 40x20 pill-shaped track plus a sliding knob, with a roughly 150ms slide animation on toggle and optional on/off text on the right.

### Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| Checked | bool | false | Switch state; starting an animation of the knob when it changes |
| OnContent | string | "On" | Text shown on the right in the on state; an empty string shows nothing |
| OffContent | string | "Off" | Text shown on the right in the off state; an empty string shows nothing |

### Events

- `CheckedChanged` (EventHandler): raised when `Checked` changes.

### Example

```csharp
var wifi = new FluentToggleSwitch
{
    OnContent = "已开启",
    OffContent = "已关闭",
    Checked = true,
    Width = 140
};
wifi.CheckedChanged += (s, e) => SetWifiEnabled(wifi.Checked);
Controls.Add(wifi);
```

### Notes

- The animation is driven by an internal 15ms timer; `CheckedChanged` is raised immediately when the state flips and does not wait for the animation to finish.
- The space key toggles the switch; the track is a fixed 40px wide on the left, and the text starts drawing 10px to the right of the track, so lengthening the text requires widening the control accordingly (default 110x24).
- When the control is disposed the animation timer is stopped and disposed, so no manual cleanup is needed.

## FluentSlider

Fluent slider: a 4px rounded track plus an accent-colored filled portion and a 20px composite thumb (solid outer ring + accent-colored inner dot), with dragging, keyboard and mouse wheel support.

### Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| Minimum | double | 0d | Minimum value; the current value is re-clamped immediately after a change |
| Maximum | double | 100d | Maximum value; the current value is re-clamped immediately after a change |
| Value | double | 0d | Current value, automatically clamped to Minimum~Maximum |
| TickFrequency | double | 0d | Tick interval (in value units); 0 means no ticks are drawn |

### Events

- `ValueChanged` (EventHandler): raised when `Value` changes; it fires continuously while dragging.

### Example

```csharp
var volume = new FluentSlider
{
    Minimum = 0,
    Maximum = 100,
    Value = 60,
    TickFrequency = 10,
    Width = 260
};
volume.ValueChanged += (s, e) => SetVolume(volume.Value);
Controls.Add(volume);
```

### Notes

- Keyboard: ←/↓ and →/↑ step by 1% of the range, Home/End jump to the minimum/maximum value; each mouse wheel notch adjusts by 3% of the range. Values do not snap to the ticks, which are only a visual reference.
- The tick marks are drawn below the track (2px further down from the lower edge of the thumb, 4px high); the default height of 32px accommodates them exactly, so a control that is too short will clip them.
- `ValueChanged` fires at a high rate while dragging. If the handling is expensive (writing files, network requests), throttle inside the handler or commit on MouseUp instead.

## FluentComboBox

Fluent combo box (non-editable): a rounded shell plus an arrow on the right; clicking opens a flyout list with hover highlighting and an accent-colored selection bar.

### Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| Items | IList | Empty List&lt;object&gt; | Collection of candidates; elements may be any object. After DataSource is set it points to the bound list, and assigning Items then throws InvalidOperationException |
| DataSource | object | null | Data source: IList, IListSource, IEnumerable, DataTable, DataSet, BindingSource and the like; both the designer smart tag and the properties window (Data category) offer a data-source dropdown |
| DataMember | string | "" | Table name to use when binding a DataSet; an invalid member does not throw, it binds an empty list |
| DisplayMember | string | "" | Name of the item property used as the display text; when empty, ToString() is used; TextSelector takes precedence over it |
| ValueMember | string | "" | Name of the item property used for SelectedValue; when empty, SelectedValue is the item itself |
| SelectedValue | object | null | ValueMember value of the selected item; assigning a value locates the selected item by value |
| TextSelector | Func&lt;object, string&gt; | null | Function that converts an item to its display text (equivalent to DisplayMember); when null, the item's ToString() is called |
| SelectedIndex | int | -1 | Index of the selected item; -1 means nothing is selected, and assignments are clamped automatically to [-1, Items.Count-1] |
| SelectedItem | object | null | The selected item object; when assigned, the position is located via IndexOf, and an item that is not present resets it to -1 |
| SelectedText | string | ""(read-only) | Display text of the selected item after conversion by TextSelector |
| DroppedDown | bool | false(read-only) | Whether the drop-down list is currently expanded |

The methods `OpenDropDown()` / `CloseDropDown()` are also provided.

### Events

- `SelectedIndexChanged` (EventHandler): raised when the selected item changes.
- `DropDownOpened` (EventHandler): raised after the drop-down list opens.
- `DropDownClosed` (EventHandler): raised after the drop-down list closes.

### Example

```csharp
var combo = new FluentComboBox { Width = 200 };
combo.Items = new List<object> { "浅色", "深色", "跟随系统" };
combo.SelectedIndex = 0;
combo.SelectedIndexChanged += (s, e) =>
{
    ApplyTheme(combo.SelectedItem as string);
};
Controls.Add(combo);

// Use TextSelector to specify the display text when binding custom objects
var fontCombo = new FluentComboBox
{
    Width = 220,
    Items = new List<object> { new Font("Segoe UI", 9f), new Font("Consolas", 10f) },
    TextSelector = item => ((Font)item).Name
};
Controls.Add(fontCombo);

// Standard data binding: object lists / DataTable / BindingSource all work
var userCombo = new FluentComboBox { Width = 220, DisplayMember = "Name", ValueMember = "Id" };
userCombo.DataSource = users;                      // List<User>, BindingList<User>, BindingSource…
userCombo.SelectedIndexChanged += (s, e) => LoadUser((int)userCombo.SelectedValue);

var tableCombo = new FluentComboBox { Width = 220, DisplayMember = "Name", ValueMember = "Id" };
tableCombo.DataSource = dataSet;                   // A DataSet needs DataMember as well
tableCombo.DataMember = "Users";
Controls.Add(userCombo);
Controls.Add(tableCombo);
```

### Notes

- Typing text is not supported; items can only be chosen from the list. For "editable + candidates", use FluentAutoSuggestBox.
- Keyboard: F4/space/Enter expand or collapse, and while collapsed ↑/↓ switch the selected item directly; Esc closes the list. The list height is capped at 320px, and anything beyond that scrolls.
- The popup list is a separate borderless rounded form (FluentPopupForm) that closes automatically when it loses activation; `OpenDropDown()` does nothing when `Items` is empty.
- Data binding has the same semantics as the native ListControl: it is resolved through BindingContext/CurrencyManager, and after binding the item at index 0 is selected by default; several controls on the same data source share the currency position (their selections sync with each other); additions and removals on an IBindingList/BindingSource refresh the list in real time; unbinding (DataSource = null) restores the Items you had in the unbound state.
- The designer smart tag matches the native ComboBox: select the control and click the small arrow in its top-right corner, and the "Use data binding items" checkbox switches between the two modes. Unchecked means "unbound mode", offering "Edit Items..." which opens the string collection editor (Items can be edited in the designer and are serialized with the form); checked means "bound mode", offering a "Choose Data Source" dropdown (listing the data components on this form, such as BindingSource/DataSet; supported both by .NET Framework and by the out-of-process designer of .NET 6/8/10) plus direct DataMember/DisplayMember/ValueMember entries. Unchecking it clears the DataSource and restores the unbound Items. The DataSource dropdown in the properties window is equivalent to the smart tag. .NET Framework projects additionally offer "Add Project Data Source..." in bound mode, which opens the data source wizard; for .NET projects, create new data sources through the VS menu "View → Other Windows → Data Sources".

## FluentDropDownList

Fluent flyout list control: 32px rows, a light fill on hover, and an accent-colored vertical bar to the left of the selected item. It is the internal implementation of the ComboBox / AutoSuggestBox popup list, but it can also be used on its own.

### Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| Items | IList | Empty List&lt;object&gt; | Collection of list items; reassigning it resets the scroll position. Assigning Items after DataSource is set throws InvalidOperationException |
| DataSource | object | null | Data source, with the same semantics as in FluentComboBox (IList/DataTable/DataSet/BindingSource…), including the same designer smart tag |
| DataMember | string | "" | Table member of a DataSet |
| DisplayMember | string | "" | Name of the display-text property; TextSelector takes precedence |
| ValueMember | string | "" | Property name behind SelectedValue |
| SelectedValue | object | null | ValueMember value of the selected item; can be assigned to locate the item by value |
| TextSelector | Func&lt;object, string&gt; | null | Function that converts an item to its display text; when null, ToString() is called |
| SelectedIndex | int | -1 | Index of the selected item; when it changes, the list scrolls automatically to make it visible |
| SelectedItem | object | null(read-only) | The currently selected item object |
| ItemHeight | int | 32(read-only) | Fixed row height, useful for computing the height of the popup form |

### Events

- `ItemClicked` (EventHandler): raised when an item is clicked with the mouse, or when Enter is pressed on the selected item.
- `SelectedIndexChanged` (EventHandler): raised when `SelectedIndex` changes (moving the highlight with the keyboard also triggers it).

### Example

```csharp
var popup = new FluentPopupForm();
var list = new FluentDropDownList
{
    Items = new List<object> { "新建", "打开", "保存" },
    Dock = DockStyle.Fill
};
list.ItemClicked += (s, e) =>
{
    ExecuteCommand(list.SelectedItem as string);
    popup.Close();
};
popup.Padding = new Padding(4);
popup.Controls.Add(list);
popup.ShowBelow(anchorButton, list.ItemHeight * 3 + 8);
```

### Notes

- Keyboard: ↑/↓ move the selected item, Home/End jump to the first/last item, Enter raises `ItemClicked`; the mouse wheel scrolls the list (there is no scroll bar, only a scroll offset).
- The self-drawn background uses the theme's AcrylicBackgroundFillDefault color. It is normally hosted inside a FluentPopupForm to get the complete Fluent flyout surface (rounded corners, shadow, Esc/loss-of-activation closing).
- Unlike FluentComboBox, it has no shell and no arrow, and `SelectedIndexChanged` only signals a highlight change; use `ItemClicked` as the point where a choice is actually confirmed.
