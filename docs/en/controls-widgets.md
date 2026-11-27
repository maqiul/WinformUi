# Widgets Reference

This document covers the eight widgets in WinformUi: `FluentIcon` (symbol/image icon renderer), `FluentPictureBox` (rounded-corner picture box), `FluentCodeBlock` (syntax-highlighted code block), `FluentRichTextBox` (Fluent rich-text editing/read-only box), `FluentScrollBar` (thin dynamic scrollbar), `FluentToolStrip` (themed toolbar), `FluentNotifyIcon` (tray icon component), and `FluentToast` (in-app floating notification). All live in the `WinformUi.Controls` namespace; theme colors automatically reskin with the current palette of `ApplicationThemeManager`. Every control that subscribes to theme changes establishes the subscription when its handle is created and releases it in `Dispose`. Properties, events, and default values are all taken from the source code; the example snippets below omit `using` lines and can be dropped directly into form or page code.

## FluentIcon

A lightweight icon renderer: inside a small, non-focusable, Tab-excluded control (16×16 by default) it paints a Fluent System Symbol (regular or filled variant), a raw glyph character from any icon font, or an image, scaling automatically to the control size and tinting according to the theme.

### Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| Symbol | SymbolRegular | SymbolRegular.Empty | Regular-variant Fluent symbol; falls back to other icon sources when Empty. |
| FilledSymbol | SymbolFilled | SymbolFilled.Empty | Filled-variant Fluent symbol; takes priority over Symbol when non-Empty. |
| Glyph | string | "" | Raw glyph character (can come from any icon font); used only when no symbol or image is set. Assigning null is treated as an empty string. |
| Image | Image | null | Bitmap source, drawn centered and aspect-fit scaled; takes priority over all glyph sources. Marked RefreshProperties.Repaint. |
| Foreground | Color | Follows theme | Alias wrapper over ForeColor; hidden from the designer (Browsable=false, not serialized); always follows the theme text color when not explicitly assigned. |

Painting priority is: Image > FilledSymbol > Symbol > Glyph; when none of the four is set, only blank space is drawn.

### Example

```csharp
// Fluent symbol icons (regular + filled)
var search = new FluentIcon
{
    Symbol = SymbolRegular.Search20,
    Size = new Size(20, 20),
    Location = new Point(12, 12)
};
var searchFilled = new FluentIcon
{
    FilledSymbol = SymbolFilled.Search20,
    Size = new Size(20, 20),
    Location = new Point(40, 12)
};

// Raw glyph character
var glyphIcon = new FluentIcon
{
    Glyph = "\uE713",
    Size = new Size(16, 16),
    Location = new Point(68, 12)
};

// Image source: aspect-fit scaling, centered
var pictureIcon = new FluentIcon
{
    Image = Image.FromFile("logo.png"),
    Size = new Size(48, 48),
    Location = new Point(92, 8)
};

Controls.Add(search);
Controls.Add(searchFilled);
Controls.Add(glyphIcon);
Controls.Add(pictureIcon);
```

### Notes

- Default size is 16×16; the constructor sets `TabStop = false` and turns off `ControlStyles.Selectable`, so the control can never receive focus and does not respond to the keyboard.
- The glyph font size is computed automatically from the control's shorter side: `max(6, min(width, height)) × 0.75` points, so changing `Size` scales the icon.
- When disabled (`Enabled = false`), the glyph is tinted with the palette's `Palette.TextFillDisabled`.
- Glyphs are drawn with shared fonts cached internally; user code is not involved in their lifecycle, so do not attempt to dispose these fonts.
- Image drawing enables HighQualityBicubic interpolation; the `Image` property does not take ownership of the passed-in image, and the caller must still dispose the `Image` after the control is disposed.
- Before painting, `FluentGraphics.SetupGraphics` uniformly configures antialiasing and other graphics quality settings.

## FluentPictureBox

A Fluent-flavored picture box derived from the native `PictureBox`: the constructor sets `SizeMode` to `Zoom` (aspect-fit scaling), replaces the background with transparent unless it was explicitly set, and optionally uses `CornerRadius` to achieve corner clipping plus a 1px divider-color outline.

### Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| CornerRadius | int | 0 | Corner radius in 96-DPI device-independent pixels; 0 means no clipping and no border; negative values are clamped to 0. Scaled internally by the control's current DPI. |
| SizeMode | PictureBoxSizeMode | Zoom (preset by constructor) | Inherited from PictureBox; the constructor presets the aspect-fit zoom mode, which you may change back as needed. |
| BackColor | Color | Transparent (after theme application) | At construction, if it is still SystemColors.Control it is replaced with transparent, so the image floats over the parent surface. |

All other properties (`Image`, `InitialImage`, `ErrorImage`, etc.) are native `PictureBox` capabilities, left unchanged.

### Example

```csharp
var avatar = new FluentPictureBox
{
    Image = Image.FromFile("avatar.jpg"),
    Size = new Size(96, 96),
    CornerRadius = 48,          // Circular avatar
    Location = new Point(16, 16)
};

var banner = new FluentPictureBox
{
    Image = Image.FromFile("banner.png"),
    Size = new Size(320, 120),
    CornerRadius = 8,
    Location = new Point(16, 128)
};

Controls.Add(avatar);
Controls.Add(banner);
```

### Notes

- When `CornerRadius > 0`: painting is clipped to a rounded-rect path before calling the base class, then a 1px hairline border is drawn along a rounded rect inset by 0.5px using `Palette.DividerStrokeDefault`; the radius is scaled by `ControlHelper.DpiOf(this) / 96f`, keeping the visual proportion consistent at high DPI.
- The theme subscription is established in `OnHandleCreated`, and theme switches trigger an automatic repaint; the subscription is released in `Dispose`.
- Instances whose `BackColor` was explicitly set are not changed to transparent by the theme-application logic.
- Inherits PictureBox's Image lifecycle semantics: dispose the assigned `Image` yourself when you are done with it.

## FluentCodeBlock

A read-only, selectable, theme-aware code presentation control derived from `FluentRichTextBox`: it ships built-in regex colorizers for XAML and C# (comments/strings/numbers/keywords, tags/attributes/entities each in their own color), has a distinct color set for dark and light themes, and provides a one-call method to copy the source code to the clipboard.

### Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| Code | string | "" | The source code to display (a wrapper over the internal Text); assigning it automatically re-colorizes. DefaultProperty, supports localization. |
| Language | SyntaxLanguage | SyntaxLanguage.Autodetect | Colorizing language: `Autodetect`, `Xaml`, `CSharp`, `Plain`; switching re-colorizes immediately. |
| Text | string | — | Base-class text property, hidden from the designer (Browsable=false, not serialized); reading/writing at runtime still works, but use Code for colorizing. |

`SyntaxLanguage` enum members: `Autodetect`, `Xaml`, `CSharp`, `Plain`. Behavior properties inherited from FluentRichTextBox are preset in the constructor to `ReadOnly = true`, `WordWrap = false`, `DetectUrls = false`; default size is 420×180.

### Example

```csharp
var block = new FluentCodeBlock
{
    Location = new Point(16, 16),
    Size = new Size(420, 180)
};
block.Language = SyntaxLanguage.CSharp;
block.Code = "public sealed class Greeter\n{\n    public string Hello => \"world\"; // 注释\n}";

var xaml = new FluentCodeBlock
{
    Location = new Point(16, 208),
    Size = new Size(420, 140),
    Language = SyntaxLanguage.Xaml,
    Code = "<ContentDialog Title=\"提示\" PrimaryButtonText=\"OK\" />"
};

var copy = new FluentButton { Text = "复制源码", AutoSize = true, Location = new Point(16, 356) };
copy.Click += (s, e) => block.CopySourceToClipboard();

Controls.Add(block);
Controls.Add(xaml);
Controls.Add(copy);
```

### Notes

- Auto-detection rule: after `TrimStart()`, source text starting with `<` is treated as XAML, otherwise as C#; a null text is treated as XAML. `Plain` disables colorization entirely and uses only the default text color.
- Colorization is based on regex matching (the keyword table contains roughly 80 C# keywords); it is pure presentation-layer coloring, not a full parser; overlapping matches let the later one paint over the earlier one.
- The default body color follows the theme: the dark theme uses Aqua (#9CDCFE), the light theme uses CornflowerBlue (#6495ED); comments, strings, keywords, tags, attributes, and numbers each have their own dark/light color pairs.
- The font probes installed system fonts on first creation, preferring Fira Code / Cascadia Code / JetBrains Mono, then Consolas, then Courier New (10 pt); the result is cached process-wide. Explicitly setting `Font` overrides this.
- Colorization automatically re-runs on text changes, language switches, and theme switches; an internal re-entrancy guard prevents recursion, and it is skipped while the handle is not yet created.
- Read-only behavior is implemented through the base class `ReadOnly`: rubber-band selection and Ctrl+C still work; `CopySourceToClipboard()` copies the un-colorized plain-text source.
- All other behavior (DPI, rounded shell, focus accent bar, theme subscription and disposal) is the same as FluentRichTextBox; see the next section.

## FluentRichTextBox

A Fluent rich-text control built on the native `RichTextBox` editing engine: the shell paints its own rounded background, hover/disabled fills, and a 2px accent-colored bottom edge in the focused state, with a borderless editing area embedded inside; `IsTextSelectionEnabled` has the same meaning as in the reference implementation — when set to false, all user text selection is completely disabled (dragging, double-click word selection, Shift-extension, and Ctrl+A are all intercepted), while editing and caret placement are unaffected.

### Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| IsTextSelectionEnabled | bool | true | User selection mode: false disables all user selection operations and clears any existing selection; independent of ReadOnly. |
| ReadOnly | bool | false | Read-only; setting true forbids editing but keeps selection and copying. |
| WordWrap | bool | true | Whether to auto-wrap lines, passed straight through to the inner RichTextBox. |
| DetectUrls | bool | false | Whether to auto-detect URLs as links, passed straight through to the inner control. |
| MaxLength | int | 0 | Maximum character count, 0 means unlimited, passed straight through to the inner control. |
| Text | string | "" | Plain-text content; assigning null is treated as an empty string. The DefaultEvent is TextChanged. |
| Rtf | string | — | RTF content read/write, hidden from the designer; can replace the whole rich-text content. |
| SelectedText / SelectedRtf | string | — | Selection text/RTF; both reading and writing are hidden from the designer. |
| SelectionStart / SelectionLength | int | — | Selection position and length, hidden from the designer. |
| SelectionFontValue / SelectionColorValue | Font / Color | — | Wrapper properties that apply a font/color to the selection, hidden from the designer. |
| InnerRichTextBox | RichTextBox | Inner instance | Read-only; exposes the embedded native control for the remaining RichTextBox APIs; hidden from the designer. |

### Events

| Event | Description |
| --- | --- |
| LinkClicked | Raised when a `DetectUrls` or RTF link is clicked; forwarded from the inner control with LinkClickedEventArgs. |
| TextChanged | Inherited from Control; raised when the inner editing area's text changes. |

### Example

```csharp
var editor = new FluentRichTextBox
{
    Location = new Point(16, 16),
    Size = new Size(320, 150),
    DetectUrls = true
};
editor.Text = "访问 https://example.com 了解更多。";
editor.LinkClicked += (s, e) => MessageBox.Show(e.LinkText);

// Read-only presentation: selectable and copyable, not editable
var viewer = new FluentRichTextBox
{
    Location = new Point(16, 180),
    Size = new Size(320, 100),
    ReadOnly = true
};
viewer.AppendText("第一段\n");
viewer.AppendText("第二段\n");
viewer.SelectAll();

// Load rich text directly via RTF
viewer.Rtf = @"{\rtf1\ansi 这是{\b 加粗}文本}";

Controls.Add(editor);
Controls.Add(viewer);
```

### Notes

- Besides properties, public methods are provided: `Select(int start, int length)`, `SelectAll()`, `Clear()`, `AppendText(string text)`; `Focus()` is overridden to hand focus directly to the inner editing area.
- Default size is 280×150, `MinimumSize` is 60×34; the shell corner radius uses `FluentGraphics.ControlCornerRadius`, and the embedded editing area is inset 2px on all sides.
- The shell fill is layered by state and blended onto the parent container's background: disabled → `ControlFillDisabled`, focused → `ControlFillInputActive`, hovered → `ControlFillSecondary`, normal → `ControlFillDefault`; when focused and enabled, a 2px accent-colored bottom bar (rounded-clipped) is drawn on top.
- The control itself has `TabStop = false`; focus is carried by the inner editing area (inner `TabStop = true`); clicking anywhere on the shell hands focus to the editing area.
- CreateParams appends WS_CLIPCHILDREN, which together with full-control double-buffered self-painting prevents the shell and the child window from tearing each other.
- The inner editing area's non-client-area scrollbars are repainted in Fluent style by `FluentNcScrollBar` during WM_NCPAINT; on theme switches `FluentNcScrollBar.Redraw` is called to refresh them.
- The selection gate filters messages one by one and reads the key code / mouse-state flags only from `WParam.ToInt64() & 0xFFFF`; on x64 the wParam of messages such as `WM_MOUSEACTIVATE` and `WM_CTLCOLOR*` is a pointer, and calling `ToInt32()` on it throws `System.OverflowException` (fixed in 1.4.1).
- `Font` changes are automatically synced to the inner control, and `Enabled` changes sync the read-only text color `Palette.TextFillDisabled`; the theme subscription is established when the handle is created and released in Dispose.
- FluentCodeBlock derives from this control and overrides Text serialization, the default WordWrap/DetectUrls, etc.; do not change these presets directly on a CodeBlock.

## FluentScrollBar

A thin Fluent scrollbar (14-DIP track): the rounded thumb thickens on hover, the direction arrow buttons fade in on hover, and it supports dragging, page clicks, press-and-hold repeat, and the mouse wheel. Enabling `Dynamic` turns it into the auto-hiding variant — it fades out after an idle timeout and fades in during interaction.

### Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| Orientation | Orientation | Vertical | Direction; switching automatically swaps width/height. |
| Dynamic | bool | false | Auto-hide mode: fully transparent when not hovered and idle, fades in during interaction, fades out after Timeout milliseconds of inactivity. |
| Timeout | int | 1000 | Milliseconds of inactivity before fade-out in dynamic mode; negative values are clamped to 0, and the actual timer floor is 50ms. |
| Minimum | int | 0 | Minimum value; if greater than Maximum it also raises Maximum accordingly. |
| Maximum | int | 100 | Maximum value; if less than Minimum it also lowers Minimum accordingly. |
| Value | int | 0 | Current value; the effective upper bound is `Maximum - LargeChange + 1`, and assignment is automatically clamped. Has an Int32Converter. |
| SmallChange | int | 1 | Single-step amount (arrow button/wheel base); clamped to a minimum of 1. |
| LargeChange | int | 10 | Page amount, which also determines the thumb length ratio; clamped to a minimum of 1. |

### Events

| Event | Description |
| --- | --- |
| ValueChanged | Raised whenever Value actually changes through any path (code assignment, dragging, wheel, buttons, page clicks). |
| Scroll | DefaultEvent; carries ScrollEventArgs — ThumbTrack while dragging, EndScroll on release, SmallIncrement/SmallDecrement for the wheel, LargeIncrement/LargeDecrement/ThumbPosition for page clicks, Small* for the arrows. |

### Example

```csharp
var bar = new FluentScrollBar
{
    Location = new Point(240, 16),
    Size = new Size(14, 180),
    Minimum = 0,
    Maximum = 1000,
    SmallChange = 20,
    LargeChange = 100
};
var readout = new FluentLabel { Text = "Value: 0", AutoSize = true, Location = new Point(16, 16) };
bar.ValueChanged += (s, e) => readout.Text = "Value: " + bar.Value;

// Horizontal + auto-hiding
var hbar = new FluentScrollBar
{
    Orientation = Orientation.Horizontal,
    Location = new Point(16, 210),
    Size = new Size(240, 14),
    Dynamic = true,
    Timeout = 1500
};

Controls.Add(bar);
Controls.Add(hbar);
Controls.Add(readout);
```

### Notes

- Default size is 14×100, and `BackColor` is preset to transparent; the constructor turns off the `Selectable` style and sets `TabStop = false`, offering no keyboard interaction — it is mouse-only by design.
- All geometry (12px arrow buttons, 12px track capsule, 6px/8px-on-hover thumb thickness, minimum 24px thumb length) is defined at 96 DIP and converted through `Scale()` by the control's DPI.
- On hover or drag: the thumb thickens and changes color, and the arrow buttons at both ends (chevron glyphs) fade in; otherwise only the capsule track and thumb are shown.
- Holding an arrow button fires single steps repeatedly at a 100ms interval; the repeat stops on `MouseUp` or when the pointer leaves the control.
- Each wheel notch (120 delta) moves `SmallChange × 3`; multi-notch wheel deltas are scaled by the notch count.
- Dynamic mode is coordinated by three lazily created Timers: 16ms fade-in/fade-out (alpha steps of 26 per frame), and the idle-hide timer (≥50ms); all three timers are disposed in Dispose.
- The thumb position maps linearly from `[Minimum, Maximum - LargeChange + 1]`; if the range degenerates (maximum ≤ minimum), the thumb stays pinned at the start of the track.

## FluentToolStrip

A themed toolbar derived from the native `ToolStrip`: buttons, toggle buttons, dropdowns, separators, overflow, and so on all keep the standard ToolStrip item model, while the look is taken over by a built-in Fluent renderer (flat background, hover/press highlights, bottom divider line).

### Properties

This control adds no new public properties; the following inherited properties are preset in the constructor:

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| Dock | DockStyle | Top | Preset to dock at the top; can be changed. |
| AutoSize | bool | true | Preset to auto-size the height. |
| ImageScalingSize | Size | 16×16 | Preset icon size, consistent with the FluentIcon system. |
| GripStyle | ToolStripGripStyle | Hidden | Preset to hide the drag grip. |
| Padding | Padding | 6,3,6,3 | Preset padding. |
| Renderer | ToolStripRenderer | Built-in Fluent renderer | Replaced with the Fluent renderer at construction (derives from ToolStripProfessionalRenderer with a unified color table, RoundedEdges=false); hidden from the designer. |

### Example

```csharp
var strip = new FluentToolStrip();

var bold = new ToolStripButton("加粗") { DisplayStyle = ToolStripItemDisplayStyle.Text };
var italic = new ToolStripButton("斜体") { DisplayStyle = ToolStripItemDisplayStyle.Text };
var undo = new ToolStripButton("撤销", SystemIcons.Application.ToBitmap())
{
    DisplayStyle = ToolStripItemDisplayStyle.ImageAndText
};

var zoom = new ToolStripDropDownButton("缩放", new[]
{
    (ToolStripItem)new ToolStripButton("50%") { DisplayStyle = ToolStripItemDisplayStyle.Text },
    new ToolStripButton("100%") { DisplayStyle = ToolStripItemDisplayStyle.Text },
    new ToolStripButton("200%") { DisplayStyle = ToolStripItemDisplayStyle.Text }
});

strip.Items.AddRange(new ToolStripItem[] { bold, italic, undo, new ToolStripSeparator(), zoom });
bold.Click += (s, e) => Console.WriteLine("bold");

// Place on the form (optionally wrap in a ToolStripContainer first, or just Dock it)
Controls.Add(strip);
```

### Notes

- A toolbar docked on the form paints its own blended `ControlFillDefault` background and, in horizontal orientation, draws a 1px `DividerStrokeDefault` divider along its bottom edge; popped-up submenus reuse the FluentMenu system's flyout-surface rendering.
- The theme subscription is established in `OnHandleCreated`; both `OnParentChanged` and `OnItemAdded` re-apply the colors, so newly added items automatically get the foreground color; the subscription is released in Dispose.
- The foreground color uniformly comes from `Palette.TextFillPrimary` and is written item-by-item into each `ToolStripItem.ForeColor`; item selected/press highlights are painted by the renderer by blending `SubtleFillSecondary/SubtleTertiary` with the current surface.
- Standard features (the overflow button, mnemonic shortcuts, ImageList, layout direction) are unaffected and work with native ToolStrip semantics.
- Like the rest of the ToolStrip family, it supports being placed in a `ToolStripContainer` or added via `Controls.Add` directly; in the designer it is a visible toolbox control (ToolboxItem=true).

## FluentNotifyIcon

A tray (notification area) icon component: it derives from `Component` rather than Control and is used from the component tray, wrapping the Shell tray's icon, tooltip text, right-click context menu, balloon tips, and click events routed by mouse button.

### Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| TooltipText | string | "" | Hover tooltip; automatically truncated beyond 63 characters (Shell limit), supports localization. |
| Icon | Icon | null | Tray icon; when null, only the tooltip text is shown. |
| Menu | FluentContextMenuStrip | null | Right-click menu; any ContextMenuStrip works, but FluentContextMenuStrip keeps the themed look. |
| FocusOnLeftClick | bool | true | Automatically restores and activates the first open form on left click (skipping notification host windows). |
| MenuOnRightClick | bool | true | Whether a right click pops up Menu; toggling immediately attaches/detaches the ContextMenuStrip. |
| BalloonTimeout | TimeSpan | 00:00:03 | Default balloon display duration for the title/content-less overloads. |
| Visible | bool | false | Whether to show in the tray; equivalent to Register/Unregister. |
| IsRegistered | bool | — | Read-only; whether the icon is currently registered and visible; hidden from the designer. |

### Methods

| Method | Description |
| --- | --- |
| Register() | Adds the icon to the tray (Visible = true). |
| Unregister() | Removes it from the tray (Visible = false). |
| SetIcon(Bitmap bitmap) | Convenience method: builds an Icon from a bitmap and assigns it; disposes the previous Icon unless it was the system Application icon. A null bitmap clears the icon. |
| ShowBalloonTip(TimeSpan timeout) | Shows an empty balloon tip. |
| ShowBalloonTip(TimeSpan timeout, string title, string message, ToolTipIcon icon = Info) | Shows a system balloon tip with title/content/icon. |

### Events

| Event | Description |
| --- | --- |
| LeftClick / LeftDoubleClick | Left single/double click (delegate type RoutedNotifyIconEventHandler); the single click restores and activates the main window by default. |
| RightClick / RightDoubleClick | Right single/double click; the right click also pops up the menu according to MenuOnRightClick. |
| MiddleClick / MiddleDoubleClick | Middle single/double click. |
| BalloonTipShown / BalloonTipClosed / BalloonTipClicked | Balloon shown, closed, clicked; forwarded from the underlying NotifyIcon. |

### Example

```csharp
// Typically declared as a form field and manually added to the components container for automatic Dispose
var tray = new FluentNotifyIcon
{
    TooltipText = "WinformUi 示例程序",
    Icon = SystemIcons.Application,
    FocusOnLeftClick = true
};

var menu = new FluentContextMenuStrip();
menu.Items.Add("退出", null, (s, e) => Application.Exit());
tray.Menu = menu;

components.Add(tray); // Hand it to the container for automatic Dispose

tray.LeftDoubleClick += (s, e) => Console.WriteLine("双击托盘图标");
tray.ShowBalloonTip(TimeSpan.FromSeconds(5), "提示", "程序已在后台运行", ToolTipIcon.Info);
tray.Register();
```

### Notes

- As a Component it is not disposed automatically with the form: this class only has a parameterless constructor, so declare a field and call `components.Add(tray)` as in the example above, or dispose it manually; on Dispose the icon is hidden automatically (Visible = false) and the internal NotifyIcon is released.
- `SetIcon` builds the icon via `Icon.FromHandle(bitmap.GetHicon())`; the handle is not released by this component, and you must manage the passed-in `Bitmap`'s lifecycle yourself.
- Left-click main-window restoration logic: iterates `Application.OpenForms`, skips forms whose handles are not yet created and `FluentToastHost` windows, restores the first matching form from minimized state and calls Activate — only one form is handled.
- `TooltipText` is uniformly truncated to 63 characters before being written, but reading it back always returns the full original value.
- The event delegate `RoutedNotifyIconEventHandler(object sender, EventArgs e)` carries no mouse coordinates; it routes only by mouse button.
- Invisible by default (`Visible = false`); you must explicitly call `Register()` or set `Visible = true` for it to appear in the tray.

## FluentToast

An in-app floating notification: `FluentToast` itself is a sealed data + presentation helper class (not a Control); after `Show()`, an internal `FluentToastHost` (a borderless top-level form) hosts the card, which slides in from the top-right of the primary screen's work area and stacks; hovering pauses the timer, and after the timeout it fades out and closes.

### Properties (FluentToast)

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| Title | string | null | Title; when both Title and Message are non-null the card is two lines tall (74 DIP), otherwise single-line height is 50 DIP. |
| Message | string | null | Body text string. |
| Severity | InfoBarSeverity | InfoBarSeverity.Informational | Severity level, which determines the icon, dot color, and background: `Informational`, `Success`, `Warning`, `Error`. |
| Duration | TimeSpan | 00:00:04 | How long the toast stays before auto-dismissal; effective floor is 500ms; the timer pauses while hovered. |

### Methods

| Method | Description |
| --- | --- |
| Show() | Instance method: displays this notification and returns the host window FluentToastHost (usable for advanced handling, e.g. early dismissal). |
| static Show(string title, string message, InfoBarSeverity severity = Informational, TimeSpan? duration = null) | Static shortcut: constructs and displays in one statement, returning the host FluentToastHost. |

### Example

```csharp
// Static shortcut
FluentToast.Show("已保存", "所有更改都已写入磁盘。", InfoBarSeverity.Success);

// Instance style: custom duration and severity
var toast = new FluentToast
{
    Title = "磁盘空间不足",
    Message = "还剩 120 MB,建议清理后继续。",
    Severity = InfoBarSeverity.Warning,
    Duration = TimeSpan.FromSeconds(8)
};
toast.Show();

// Error severity + grab the host window
FluentToastHost host = FluentToast.Show("操作失败", "无法连接到服务器。", InfoBarSeverity.Error);
```

### Notes

- The host `FluentToastHost` is a borderless, `TopMost`, taskbar-hidden transparent startup form (`ShowWithoutActivation`, does not steal focus) with 8 DIP corner clipping and a 1px `CardStrokeDefaultSolid` outline; it is 340 DIP wide, with height 74 or 50 DIP depending on the presence of title/body, all scaled by DPI.
- Position is fixed at the top-right of the primary monitor's work area; multiple notifications stack top-to-bottom with 12 DIP spacing; new notifications insert at the front, and closing any one triggers an automatic re-layout of the rest.
- Fade-in raises opacity by 0.15 every 30ms until 1; after dismissal (timeout or clicking ×) opacity drops by 0.1 every 30ms, and at zero the toast is removed from the manager and Closed.
- Hovering anywhere on the card pauses the auto-dismiss timer; moving away resumes it. The 22×22 DIP close button at the top-right has a hover highlight, and clicking it immediately starts the fade-out.
- Severity colors come from the palette: Informational → the SystemFillAttention family, Success → the SystemFillSuccess family, Warning → the SystemFillCaution family, Error → the SystemFillCritical family; for Warning in the light theme the glyph switches to dark red to guarantee contrast.
- Over-wide text is automatically truncated line-by-line with an ellipsis (LineLimit + EllipsisCharacter); two-line layout is used only when both title and body are non-empty, otherwise single-line.
- Theme switches trigger an immediate repaint via the theme subscription; Dispose releases the auto-dismiss timer, the fade-out timer, and the subscription.
- When `FluentNotifyIcon` restores the main window, it excludes toast host windows from the candidates so it never "activates" a notification window.
