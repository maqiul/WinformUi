# Dialog and Popup Controls

This document covers the dialogs and popups in WinformUi: `FluentMessageBox` (a Fluent message box), `FluentContentDialog` (a modal in-form content dialog), `FluentFlyout` (an anchored popup), `FluentPopupForm` (a borderless rounded popup form) and `FluentToolTip` (a Fluent-styled tooltip). All of them live in the `WinformUi.Controls` namespace. The example code omits the `using` lines; when using it, import namespaces such as `System`, `System.Drawing`, `System.Windows.Forms`, `WinformUi.Controls`, `WinformUi.Theming`.

## FluentMessageBox

A Fluent message box: a static class that pops a borderless, rounded, theme-aware modal dialog with
an optional severity glyph and title on top and a button footer under a divider line, returning a
`DialogResult` synchronously.

### Methods

| Overload | Description |
| --- | --- |
| `static DialogResult Show(string text)` | Body text only: no title, no icon, a single OK button. |
| `static DialogResult Show(string text, string title)` | With a title and a single OK button. |
| `static DialogResult Show(IWin32Window owner, string text, string title, MessageBoxButtons buttons, MessageBoxIcon icon)` | Full overload. A null `owner` falls back to the application's active form (or screen centering when there is none); `buttons` selects the button set; `icon` selects the severity glyph beside the title. |

Both parameters are standard WinForms enums:

| MessageBoxButtons | Buttons (left to right) | Primary |
| --- | --- | --- |
| OK | OK | OK |
| OKCancel | OK, Cancel | OK |
| YesNo | Yes, No | Yes |
| YesNoCancel | Yes, No, Cancel | Yes |
| RetryCancel | Retry, Cancel | Retry |
| AbortRetryIgnore | Abort, Retry, Ignore | Retry |

| MessageBoxIcon | Glyph | Color |
| --- | --- | --- |
| None | Not drawn | — |
| Information | Info symbol | `SystemFillAttention` |
| Warning | Warning triangle | `SystemFillCaution` |
| Error | Error circle | `SystemFillCritical` |
| Question | Question mark | `SystemFillAttention` |

### Example

```csharp
// The simplest notice
FluentMessageBox.Show("Saved to the clipboard");

// A confirmation
if (FluentMessageBox.Show(null, "This cannot be undone. Continue?", "Delete record",
        MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
{
    DeleteSelected();
}

// Owned by a parent form for positioning and ownership
DialogResult result = FluentMessageBox.Show(this, "The request failed. Retry?", "Offline",
    MessageBoxButtons.RetryCancel, MessageBoxIcon.Error);
```

### Notes

- The dialog is `FormBorderStyle.None` with a rounded Region, `ShowInTaskbar = false`,
  `TopMost = true`, `StartPosition = CenterParent`; it is 440 pixels wide and its height follows the
  wrapped body text.
- The primary button uses `ControlAppearance.Primary` and becomes the `AcceptButton` (Enter); the
  button carrying `DialogResult.Cancel` becomes the `CancelButton` (Esc). Button captions are the
  English OK/Cancel/Yes/No/Retry/Abort/Ignore — use `FluentContentDialog` when you need localized
  captions.
- `Show` is a blocking modal call whose return value is the button clicked; the internal dialog
  instance is disposed before it returns.
- When both the title and the icon are empty the header is skipped and the dialog is more compact; a
  title without an icon renders the text alone.

## FluentContentDialog

Fluent content dialog: lays a smoke-colored overlay over the form's client area, shows a rounded dialog panel in the center, and has up to three buttons at the bottom (primary button / secondary button / close button).

Note: it is a plain class — **not a Form and not a Control**; `Show` does not block and does not return a `DialogResult`; the result is obtained through the button events.

### Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| Title | string | null | Dialog title (Subtitle font). |
| Content | Control | null | Content control, placed below the title; its width is forced to the panel width minus 48, and its height is `max(Content.Height, 60)`. |
| PrimaryButtonText | string | null | Text of the primary (accent-color) button; if empty, the button is not created. |
| SecondaryButtonText | string | null | Text of the secondary button; if empty, the button is not created. |
| CloseButtonText | string | "Close" | Text of the close button; set it to an empty string to hide the button. |

### Events/Methods

| Member | Signature | Description |
| --- | --- | --- |
| PrimaryButtonClick | event EventHandler | Raised when the primary button is clicked; the dialog then closes automatically. |
| SecondaryButtonClick | event EventHandler | Raised when the secondary button is clicked; the dialog then closes automatically. |
| CloseButtonClick | event EventHandler | Raised when the close button is clicked; the dialog then closes automatically. |
| Closed | event EventHandler | Raised after the dialog closes (via any button or a click on the overlay). |
| Show | void Show(Form owner) | Shows the dialog inside the given form; returns immediately and does not block. Repeated calls, or a null owner, are ignored. |
| Close | void Close() | Removes and destroys the overlay, and raises Closed. |

### Example

```csharp
var content = new Panel { Size = new Size(360, 90), BackColor = Color.Transparent };
var input = new FluentTextBox { Width = 360, PlaceholderText = "项目名称", Location = new Point(0, 28) };
content.Controls.Add(new FluentLabel { Text = "输入新项目的名称:", AutoSize = true, Location = new Point(0, 0) });
content.Controls.Add(input);

var dialog = new FluentContentDialog
{
    Title = "创建项目",
    Content = content,
    PrimaryButtonText = "创建",
    SecondaryButtonText = "取消",
    CloseButtonText = "关闭"
};

// Get the "result" through events: read the input in the primary-button handler, do the wrap-up in Closed
dialog.PrimaryButtonClick += (s, e) => Console.WriteLine("创建项目:" + input.Text);
dialog.SecondaryButtonClick += (s, e) => Console.WriteLine("用户取消");
dialog.Closed += (s, e) => Console.WriteLine("对话框已关闭");

dialog.Show(this);   // this is the current Form; non-blocking, code keeps executing

// You can also close it explicitly from business code
// dialog.Close();
```

### Notes

- Clicking the smoke-colored overlay (the area outside the dialog) is a light dismissal: it calls `Close()` directly and raises only `Closed`, never any button event.
- All three buttons close the dialog automatically, so there is no need to call `Close()` again from an event handler; for scenarios such as "validation failed, keep the dialog open", use a custom form instead.
- `Show` sets the Content's `DoubleBuffered` to true via reflection, to avoid transparent holes appearing in the content controls under a Mica background; the Content is reparented into the dialog panel and gets disposed together with the panel on `Close`, so create fresh content controls for each popup.

## FluentFlyout

Fluent flyout: puts any content control into a rounded popup form, shown anchored below a control, and closes automatically when it loses focus.

### Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| Content | Control | null | Flyout content; the popup form's client size is the content size plus 8px on every side. |

### Events/Methods

| Member | Signature | Description |
| --- | --- | --- |
| Closed | event EventHandler | Raised after the popup form closes (including a light dismissal). |
| ShowAt | void ShowAt(Control anchor) | Pops up below the anchor control; ignored if already open, or if Content or anchor is null. |
| Close | void Close() | Closes the flyout. |

### Example

```csharp
var anchorButton = new FluentButton { Text = "更多操作", AutoSize = true, Location = new Point(24, 24) };
Controls.Add(anchorButton);

var content = new Panel { Size = new Size(240, 96) };
content.Controls.Add(new FluentLabel
{
    Text = "快捷操作",
    AutoSize = true,
    UseSecondaryColor = false,
    Location = new Point(12, 10)
});
var action = new FluentButton { Text = "重命名", AutoSize = true, Location = new Point(12, 40) };
content.Controls.Add(action);

var flyout = new FluentFlyout { Content = content };
action.Click += (s, e) => flyout.Close();
flyout.Closed += (s, e) => Console.WriteLine("弹出层已关闭");
anchorButton.Click += (s, e) => flyout.ShowAt(anchorButton);
```

### Notes

- Internally hosted by a `FluentPopupForm`: clicking outside the flyout (the form deactivating) closes it automatically, and Esc also closes it.
- When there is not enough room below, it automatically flips to above the anchor, and it never extends past the right edge of the screen work area.
- The content control is reparented into the popup form; closing the form disposes the content, so mind the lifetime before reusing content.

## FluentPopupForm

Borderless rounded popup form: the shared popup host used by ComboBox, AutoSuggestBox and the various picker controls, with a Fluent overlay surface (rounded corners, stroke, shadow).

### Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| NoActivate | bool | false | When true the popup does not steal focus (it intercepts WM_MOUSEACTIVATE) and does not close automatically on deactivation; intended for suggestion lists that must keep focus while typing. |

### Events/Methods

| Member | Signature | Description |
| --- | --- | --- |
| ShowBelow | void ShowBelow(Control anchor, int popupHeight, int popupWidth = 0) | Positions below the anchor and shows modelessly with the anchor's form as owner; when popupWidth is 0, `max(anchor.Width, 120)` is used. |

Everything else is inherited from `Form` (`Show`, `Close`, `FormClosed`, etc.).

### Example

```csharp
var anchorButton = new FluentButton { Text = "选择", AutoSize = true, Location = new Point(24, 24) };
Controls.Add(anchorButton);

var popup = new FluentPopupForm();
var list = new FluentDropDownList
{
    Items = new List<string> { "选项一", "选项二", "选项三" },
    Dock = DockStyle.Fill
};
list.ItemClicked += (s, e) =>
{
    Console.WriteLine(list.SelectedItem);
    popup.Close();
};
popup.Controls.Add(list);
anchorButton.Click += (s, e) => popup.ShowBelow(anchorButton, 120);
```

### Notes

- Set at construction time: borderless, not shown in the taskbar, `TopMost = true`, manual positioning, DPI scaling, class shadow (CS_DROPSHADOW), and `AcrylicBackgroundFillDefault` as the background.
- By default (`NoActivate = false`) the form calls `Close()` as soon as it deactivates, and Esc also closes it; the rounded Region is reapplied whenever the form is shown or its size changes.
- The popupHeight of `ShowBelow` is the total form height; when there is not enough space below, it automatically pops up above the anchor instead.

## FluentToolTip

Fluent tooltip: a self-drawn rounded overlay appearance that replaces the system default square tooltip.

### Properties

No new public properties declared by this class; all properties of `ToolTip` are inherited. The constructor already presets `OwnerDraw = true`, `InitialDelay = 400`, `ReshowDelay = 100`, `AutoPopDelay = 8000`, which you may override as needed.

### Events/Methods

No new public events or methods declared by this class; the inherited `Popup` event (measures the text size precisely according to DPI) and `Draw` event (draws the rounded surface) are already hooked internally. For everyday use the inherited `SetToolTip(Control control, string caption)` and similar methods are all you need.

### Example

```csharp
var toolTip = new FluentToolTip();

var saveButton = new FluentButton { Text = "保存", Appearance = ControlAppearance.Primary, AutoSize = true };
toolTip.SetToolTip(saveButton, "保存当前文档(Ctrl+S)");

var openButton = new FluentButton { Text = "打开", AutoSize = true };
toolTip.SetToolTip(openButton, "工具提示会自动跟随当前主题。");

// Optional: adjust the delays
toolTip.InitialDelay = 300;
toolTip.AutoPopDelay = 5000;
```

### Notes

- The size is measured with GDI at Popup time using the DPI of the associated control, and drawing uses GDI+; the two are consistent so that the text always fits exactly; do not subscribe to Draw yourself to override the appearance.
- The appearance comes from the current theme's palette (`AcrylicBackgroundFillDefault` surface + `SurfaceStrokeFlyout` stroke) and takes effect automatically after a theme switch.
- One instance can serve multiple controls; the text is a single line and does not support multi-line wrapping.
