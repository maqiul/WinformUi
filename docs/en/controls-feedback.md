# Feedback and Progress Controls

This chapter introduces the controls WinformUi uses to communicate status and results to the user: `FluentProgressBar`, `FluentProgressRing`, `FluentInfoBar`, `FluentBadge`, `FluentInfoBadge`, `FluentSnackbar`, `FluentErrorProvider` and `FluentLoadingScreen`. All types live in the `WinformUi.Controls` namespace and their colors follow the current `ApplicationThemeManager` theme automatically. The example code omits the `using` lines; when using it, import the `System`, `System.Drawing`, `System.Windows.Forms`, `WinformUi.Controls`, `WinformUi.Theming`, etc. namespaces.

## FluentProgressBar

A Fluent-style progress bar: a rounded track 4 pixels high, supporting both determinate progress and indeterminate (continuously looping) mode.

### Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| Minimum | double | 0 | Lower bound of the progress range. |
| Maximum | double | 100 | Upper bound of the progress range. |
| Value | double | 0 | Current progress, automatically clamped into the [Minimum, Maximum] interval. |
| IsIndeterminate | bool | false | When true the looping animation plays (an internal 16ms timer) and Value is ignored. |

### Events/Methods

No public events or methods declared by the control itself; it is driven through the properties above. The constructor's default size is 200×8.

### Example

```csharp
var progress = new FluentProgressBar
{
    Width = 320,
    Minimum = 0,
    Maximum = 100,
    Value = 40
};
Controls.Add(progress);

// Switch to indeterminate mode while the task progress is unknown
progress.IsIndeterminate = true;

// Switch back to determinate mode once the progress is known
progress.IsIndeterminate = false;
progress.Value = 75;
```

### Notes

- When the control is disabled (`Enabled = false`) only the track is drawn, not the progress fill.
- The track height is fixed at 4px and vertically centered within the control's height; increasing Height does not make the track thicker.
- The progress fill uses the system accent color (`Palette.SystemAccentColorPrimary`).

## FluentProgressRing

A Fluent-style progress ring: a rotating accent-colored arc that means "working, progress unknown".

### Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| IsActive | bool | true | The rotation animation runs when true; when false the timer stops and nothing is drawn at all. |

### Events/Methods

No public events or methods declared by the control itself. The constructor's default size is 32×32; if `IsActive` is true when the handle is created the animation starts automatically.

### Example

```csharp
var ring = new FluentProgressRing
{
    Size = new Size(48, 48),
    Location = new Point(24, 24)
};
Controls.Add(ring);

// Stop when the work is done (once stopped the control draws no arc)
ring.IsActive = false;
```

### Notes

- The stroke thickness of the arc adapts to the size: `max(2, min(Width, Height) / 10)`.
- There is only an indeterminate mode, no percentage progress; use `FluentProgressBar` when you need a numeric value.

## FluentInfoBar

A Fluent info bar: a banner colored by severity, with a circular icon, a title, a body message and an optional close button.

### Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| Severity | InfoBarSeverity | Informational | Severity level; decides the background color, the icon circle's fill color and the icon glyph. |
| Title | string | "" | Bold title; when both it and Message are present they are shown on two lines. |
| Message | string | "" | Body text; when it appears alone it is vertically centered. |
| IsClosable | bool | true | Whether the close button on the right is shown. |

The `InfoBarSeverity` enum (defined in `WinformUi.Controls`):

| Value | Meaning |
| --- | --- |
| Informational | Information (attention color) |
| Success | Success (green tones) |
| Warning | Warning (caution color) |
| Error | Error (critical color) |

### Events/Methods

| Member | Signature | Description |
| --- | --- | --- |
| CloseButtonClick | event EventHandler | Raised when the close button is clicked (the control then hides itself automatically). |
| Closed | event EventHandler | Raised after the close-button click has set `Visible = false`. |

### Example

```csharp
var bar = new FluentInfoBar
{
    Severity = InfoBarSeverity.Warning,
    Title = "磁盘空间不足",
    Message = "剩余空间低于 10%,建议清理临时文件。",
    IsClosable = true,
    Width = 640
};
bar.CloseButtonClick += (s, e) => Console.WriteLine("用户关闭了提示");
Controls.Add(bar);
```

### Notes

- The constructor's default size is 400×52; text that is too wide is truncated with an ellipsis and never wrapped onto several lines.
- Clicking the close button only sets `Visible` to false; the control is not removed from its parent container. To remove it completely, call `Controls.Remove` and `Dispose` yourself in the `Closed` event.
- In the light theme the Warning icon uses a dark glyph to guarantee contrast; the other levels use a white glyph.

## FluentBadge

A Fluent badge: a small pill-shaped label for short texts such as "New" or "Beta".

### Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| Content | string | "1" | The text shown inside the pill. |
| Appearance | ControlAppearance | Primary | Appearance variant; decides the fill and foreground colors. |

### Events/Methods

No public events or methods declared by the control itself. The constructor's default size is 24×18.

### Example

```csharp
var row = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 32 };
row.Controls.Add(new FluentBadge { Content = "New" });
row.Controls.Add(new FluentBadge { Content = "Preview", Appearance = ControlAppearance.Info });
row.Controls.Add(new FluentBadge { Content = "Beta", Appearance = ControlAppearance.Caution });
row.Controls.Add(new FluentBadge { Content = "Deprecated", Appearance = ControlAppearance.Danger });
Controls.Add(row);
```

### Notes

- Available `ControlAppearance` values: Secondary, Primary, Info, Success, Caution, Danger, Transparent, Dark, Light.
- The Secondary appearance is handled specially as a strong fill background plus inverted text color.
- The pill's corner radius is half of the height; when the text grows longer you have to widen Width yourself, the control does not measure itself automatically.

## FluentInfoBadge

A Fluent info badge: a small dot (Value ≤ 0) or numeric pill (Value > 0) colored by severity, commonly used as a corner marker on navigation items.

### Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| Value | int | 0 | Zero or a negative value draws a dot; a positive value draws a numeric pill whose size follows the digit count. |
| Severity | InfoBadgeSeverity | Attention | Severity level; decides the fill color. |

The `InfoBadgeSeverity` enum (defined in `WinformUi.Controls`):

| Value | Meaning |
| --- | --- |
| Attention | Attention (the default, accent-color tones) |
| Informational | Information |
| Success | Success |
| Caution | Caution |
| Critical | Critical |

### Events/Methods

No public events or methods declared by the control itself. The constructor's default size is 16×16.

### Example

```csharp
// Unread red dot
var dot = new FluentInfoBadge();

// Numeric badge
var count = new FluentInfoBadge { Value = 12, Severity = InfoBadgeSeverity.Critical };

// Success-state count
var done = new FluentInfoBadge { Value = 8, Severity = InfoBadgeSeverity.Success };
```

### Notes

- Setting `Value` changes the size automatically: a positive value gives a pill of `max(18, 10 + digits×7)` × 16, otherwise a 10×10 dot; do not fix Size manually any more.
- The numeric text is drawn centered with the inverted color (`Palette.TextFillInverse`).

## FluentSnackbar

A Fluent Snackbar: a transient notification bar at the bottom of a form, optionally with one action button, which hides itself after a timeout.

### Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| Title | string | "" | Bold title; when both it and Message are present they are shown on two lines. |
| Message | string | "" | Body text. |
| ActionText | string | "" | Text of the action button on the right; no button is shown when it is empty. |
| Timeout | int | 4000 | Delay in milliseconds before hiding automatically; clamped to no less than 500 when assigned. |

### Events/Methods

| Member | Signature | Description |
| --- | --- | --- |
| ActionClick | event EventHandler | Raised when the action button area is clicked (the snackbar does not hide automatically). |
| Hidden | event EventHandler | Raised after `HideSnackbar()` is called (including the automatic hide on timeout). |
| ShowSnackbar | void ShowSnackbar() | Shows the control, brings it to the front and restarts the timeout timer. |
| HideSnackbar | void HideSnackbar() | Stops the timer, hides the control and raises Hidden. |
| Show (static) | static FluentSnackbar Show(Form owner, string title, string message, string actionText = null) | Creates and shows a Snackbar centered at the bottom of the form and returns the instance; after hiding it removes itself from the form and Disposes itself. |

### Example

```csharp
// One-line popup (recommended): bottom-centered, disappears by itself after 4 seconds and cleans up
FluentSnackbar.Show(this, "消息已发送", "你的消息已送达。", "撤销");

// When you need to react to the action button, keep the returned instance
var snackbar = FluentSnackbar.Show(this, "已删除 3 项", "", "撤销");
snackbar.ActionClick += (s, e) =>
{
    // Run the undo logic here
    snackbar.HideSnackbar();
};

// You can also place it manually and control when it is shown
var manual = new FluentSnackbar
{
    Title = "同步完成",
    Message = "共同步 128 个文件。",
    Timeout = 6000,
    Location = new Point(16, ClientSize.Height - 72),
    Anchor = AnchorStyles.Bottom | AnchorStyles.Left
};
Controls.Add(manual);
manual.ShowSnackbar();
```

### Notes

- The constructor's default size is 420×56; the static `Show` anchors the snackbar with `AnchorStyles.Bottom` 16px above the bottom edge of the form.
- The instance returned by the static `Show` has already been removed and Disposed after Hidden, so do not access it again once it has been hidden.
- Clicking the action button only raises `ActionClick`; whether to hide is up to you (typically by calling `HideSnackbar()`).

## FluentErrorProvider

A Fluent validation-error component: it derives from `Component`, so it is not a visible `Control`, and it is not an `IExtenderProvider` like the native WinForms `ErrorProvider`. It draws a critical-colored border and an error icon in the top-right corner **inside the client area** of the associated control, and provides the error details through a Fluent-style tooltip message.

It is meant for surfacing business-validation results on controls such as text boxes, combo boxes and masked input boxes. It only displays errors: it performs no validation, adds no "Error" extended property, and never automatically blocks a button click or a save operation.

### Quick Start

Below is a complete form with a name and an e-mail field, validated when "save" is clicked. The form owns the `FluentErrorProvider` and disposes it explicitly; it does not need to be added to the `Controls` collection.

```csharp
using System;
using System.Drawing;
using System.Windows.Forms;
using WinformUi.Controls;

public sealed class ValidationDemoForm : FluentWindow
{
    private readonly FluentErrorProvider _errors = new FluentErrorProvider();

    private readonly FluentTextBox _name = new FluentTextBox
    {
        PlaceholderText = "请输入名称",
        Location = new Point(24, 24),
        Size = new Size(260, 32)
    };

    private readonly FluentTextBox _email = new FluentTextBox
    {
        PlaceholderText = "name@example.com",
        Location = new Point(24, 68),
        Size = new Size(260, 32)
    };

    public ValidationDemoForm()
    {
        Text = "ErrorProvider demo";
        ClientSize = new Size(420, 180);

        var save = new FluentButton
        {
            Text = "保存",
            Appearance = ControlAppearance.Primary,
            Location = new Point(24, 120),
            Size = new Size(88, 32)
        };

        Controls.AddRange(new Control[] { _name, _email, save });
        _name.TextChanged += (s, e) => _errors.ClearError(_name);
        _email.TextChanged += (s, e) => _errors.ClearError(_email);
        save.Click += OnSaveClick;
        _errors.ErrorChanged += OnErrorChanged;
    }

    private void OnSaveClick(object sender, EventArgs e)
    {
        // This example only demonstrates showing errors; in a real project you should save only after the business data passes validation.
        _errors.ClearErrors();

        if (string.IsNullOrWhiteSpace(_name.Text))
        {
            _errors.SetError(_name, "名称不能为空");
        }
        else if (_name.Text.Trim().Length > 30)
        {
            _errors.SetError(_name, "名称不能超过 30 个字符");
        }

        string email = (_email.Text ?? string.Empty).Trim();
        if (email.Length == 0)
        {
            _errors.SetError(_email, "邮箱不能为空");
        }
        else if (email.IndexOf('@') <= 0 || email.LastIndexOf('.') <= email.IndexOf('@') + 1)
        {
            _errors.SetError(_email, "请输入有效的邮箱地址");
        }
    }

    private void OnErrorChanged(object sender, ErrorChangedEventArgs e)
    {
        Console.WriteLine(e.Control.Name + ": " + _errors.GetError(e.Control));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _errors.Dispose();
        }

        base.Dispose(disposing);
    }
}
```

The e-mail check in this example exists only to demonstrate error display; it is not a complete e-mail address parser. In production, use the project's e-mail validation rules or a mature library.

### Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| Icon | Image | a bitmap generated from `SystemIcons.Error` | The error icon. When it is set to `null` the error border is still drawn; only the icon is hidden. |
| IconPadding | int | `2` | Padding, in pixels, between the icon and the edge of the associated control's client area; it cannot be negative. |
| BlinkStyle | ErrorBlinkStyle | `BlinkIfDifferentError` | Icon blinking policy, using the native `System.Windows.Forms.ErrorBlinkStyle` enum. |

After you set a custom `Icon`, the component only references the image and never disposes the `Image` passed in by the caller; whoever sets the image manages its lifetime. The icon is drawn at its original width and height and is squashed when its height exceeds the associated control's client-area height; no layout space is reserved for it.

### Methods

| Method | Description |
| --- | --- |
| `void SetError(Control control, string message)` | Adds, replaces or clears an error. Both `null` and the empty string clear it; a string of only whitespace is still a valid error message. |
| `string GetError(Control control)` | Returns the error message of the given control; `null` when nothing is recorded. |
| `void ClearError(Control control)` | Calls `SetError(control, null)`. |
| `void ClearErrors()` | Clears every current record; each control that actually gets cleared raises `ErrorChanged` once. |

When `control` is `null`, `SetError` and `GetError` throw `ArgumentNullException`. After the component is disposed, the setters of `SetError`, `Icon`, `IconPadding` and `BlinkStyle` throw `ObjectDisposedException`; `GetError` reads the empty dictionary and returns `null`, `ClearError` returns immediately because the message is empty and nothing is recorded, and `ClearErrors` returns immediately.

### Events

| Member | Signature | Description |
| --- | --- | --- |
| ErrorChanged | `event EventHandler<ErrorChangedEventArgs>` | Raised after a control actually gains, modifies or clears an error. |
| ErrorChangedEventArgs.Control | `Control` | Read-only; returns the control whose error changed. |

The event semantics deserve special attention:

- Setting a non-empty message for the first time, changing message A into message B, or clearing an existing message all raise the event.
- Setting the same message again, or clearing a control that never had an error, does not raise it.
- `ClearErrors()` raises the event control by control; nothing is raised when there are no errors.
- When an associated control is `Dispose`d its record is removed silently, without raising `ErrorChanged`.
- Disposing the `FluentErrorProvider` itself removes all displays, and it also does not raise `ErrorChanged`.

If you need to "save and close" after an error is cleared, check whether the business data is valid, or read `GetError` - do not rely on how often the event fires.

### ErrorBlinkStyle Behavior

| Value | Behavior |
| --- | --- |
| NeverBlink | The icon is always shown and the internal timer is never started. |
| BlinkIfDifferentError | The default. When the message of an existing error changes, the icon blinks briefly for about 4 beats of 500ms and then returns to being shown. Setting an error for the first time does not blink. |
| AlwaysBlink | Keeps blinking as long as an error exists. |

Changing `BlinkStyle` resets the visible state and the blink counter. With `Icon = null` there is no icon to blink, but the error border is still shown.

### Designer Usage

1. Drag `FluentErrorProvider` from the toolbox onto the form; it appears in the component tray and does not occupy the form's client area.
2. Set `Icon`, `IconPadding` and `BlinkStyle` in the Properties window.
3. Call `SetError` in code to associate runtime controls. The component currently offers no control picker in the designer and no `Error` extended property, and it does not serialize runtime error text either.

In designer mode no runtime `Paint` handler is attached to the associated controls, so the designer does not show the error border. It only appears once you run the form.

### Common Pitfalls

- **It is not a reskinned `ErrorProvider`.** The native `ErrorProvider` usually works outside the control, whereas this component draws inside the client area; do not write code against the native extended-property model.
- **The icon and border paint over content.** If the right side of an input box holds a show-password button, a clear button or a status icon, reserve room for the error icon to avoid overlapping. Increasing `IconPadding` only shifts the offset; it does not change the control's layout.
- **The message takes over the original ToolTip text.** Internally the message is set through `FluentToolTip.SetToolTip(control, message)` and reset to `null` when the error is cleared; the control's previous ToolTip text is neither saved nor restored. Do not rely on two sets of ToolTip text for the same control.
- **One error slot keeps only one message.** Calling `SetError` again on the same control replaces the old message; if you need to show several errors, merge them into a single string in your business layer first.
- **Showing an error is not the same as validation passing.** Whether to continue when Save is clicked, the form is closed, or a page is switched must be decided by your own business logic.
- **Dispose the component promptly.** The recommendation is to keep it as a form field and dispose it in `Dispose(bool)`, or to put it into a designer component lifetime based on `IContainer`.
- **Do not cache stale results.** Records disappear automatically once the control is disposed; query again with `GetError`.

## FluentLoadingScreen

A Fluent loading screen: a full-screen overlay covering the parent container, with a progress ring and loading text in the center.

### Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| LoadingText | string | "Loading..." | Prompt text shown centered below the progress ring. |

### Events/Methods

| Member | Signature | Description |
| --- | --- | --- |
| Show (static) | static FluentLoadingScreen Show(Form owner, string text) | Creates the loading screen, adds it to the form and brings it to the front, then returns the instance. |
| HideScreen | void HideScreen() | Removes the control from its parent container and Disposes itself. |

### Example

```csharp
// Pop up a loading layer covering the whole form; hide it manually after a button click
var loading = FluentLoadingScreen.Show(this, "正在加载...");
loading.HideScreen();
```

### Notes

- The loading layer covers the parent container and intercepts interaction, so `HideScreen()` must be called once the loading is over.
- After hiding, the instance has been disposed; do not access it again.
- Put the call in the branch where the actual loading task finishes or fails; do not keep the overlay up forever after loading.
