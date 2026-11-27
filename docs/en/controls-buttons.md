# Button and Presentation Controls Reference

This document covers the property, event and usage details of the button family in WinformUi (FluentButton and its derived classes) together with the lightweight presentation controls. All APIs match the sources under the `WinformUi.Controls` namespace. The examples assume that namespaces such as `WinformUi.Controls` and `WinformUi.Theming` have been imported, and can be dropped straight into form or page code.

## FluentButton

Fluent-style button: self-drawn rounded appearance, with hover/pressed visual states, an optional Fluent icon and a keyboard focus rectangle.

### Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| Appearance | ControlAppearance | ControlAppearance.Secondary | Visual style of the button; see the Notes below for the available values |
| Icon | SymbolRegular | SymbolRegular.Empty | Fluent icon shown to the left of the text; Empty means no icon is shown |
| CornerRadius | float | FluentGraphics.ControlCornerRadius(4f) | Corner radius (pixels) |

### Events

No additional events; inherits the standard WinForms events (Click, MouseEnter, etc.).

### Example

```csharp
var saveButton = new FluentButton
{
    Text = "保存",
    Appearance = ControlAppearance.Primary,
    Icon = SymbolRegular.Save24,
    CornerRadius = 4f
};
saveButton.Click += (s, e) => MessageBox.Show("已保存");
Controls.Add(saveButton);
```

### Notes

- The `ControlAppearance` enum has nine values: `Secondary` (the default standard button), `Primary` (accent color), `Info`, `Success`, `Caution`, `Danger`, `Transparent` (no background), `Dark`, `Light`.
- The constructor sets `AutoSize = true`; the preferred size is computed as "icon + text + padding", with a minimum height of 32px; the icon and the text are centered horizontally as a single unit.
- The control subscribes to theme changes and repaints automatically when switching between light and dark themes; no manual refresh is needed.

## FluentToggleButton

A button whose state can be toggled (derives from FluentButton): when checked it is filled with the accent color, and each click switches between the checked and unchecked states.

### Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| Checked | bool | false | Whether the button is in the checked state; when checked the button shows an accent-color fill |

`Appearance`, `Icon` and `CornerRadius` inherited from FluentButton remain available.

### Events

- `CheckedChanged` (EventHandler): raised when the `Checked` value changes; triggered both by clicking and by assigning the property in code.

### Example

```csharp
var pinButton = new FluentToggleButton
{
    Text = "固定到顶部",
    Icon = SymbolRegular.Pin24
};
pinButton.CheckedChanged += (s, e) =>
{
    TopMost = pinButton.Checked;
};
Controls.Add(pinButton);
```

### Notes

- On click, `Checked` is flipped first and `Click` is raised afterwards, so a `Click` handler already reads the new state.
- Unlike FluentCheckBox, its appearance is a "button" rather than a checkbox, which makes it suitable for toolbar-style on/off switches.

## FluentHyperlinkButton

Hyperlink-styled button (derives from FluentButton): accent-colored text on a transparent background, and clicking opens the specified URI.

### Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| NavigateUri | string | null | URI opened through the system shell on click (a web address, for instance); if empty, no navigation happens |

### Events

No additional events; inherits the standard WinForms events (use `Click` to implement custom behavior).

### Example

```csharp
var link = new FluentHyperlinkButton
{
    Text = "查看在线文档",
    NavigateUri = "https://example.com/docs"
};
Controls.Add(link);
```

### Notes

- Defaults are `Appearance = ControlAppearance.Transparent` and a hand cursor; the text color uses the theme's accent text color (the AccentTextFill family) and automatically lightens on hover and when pressed.
- Navigation is "best effort": an invalid URI, or the absence of a registered handler on the system, is silently ignored and never throws.
- If you only want to react to the click without opening a link, leave `NavigateUri` empty and subscribe to `Click`.

## FluentSplitButton

Split button (derives from FluentButton): the left side is the primary action area, and clicking the 32px arrow area on the right opens a Flyout menu.

### Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| Flyout | ContextMenuStrip | null | Menu shown when the arrow area on the right is clicked |

### Events

No additional events; inherits the standard WinForms events. Note: clicking the arrow area does not raise `Click`; only the primary action area does.

### Example

```csharp
var menu = new ContextMenuStrip();
menu.Items.Add("另存为...");
menu.Items.Add("导出 PDF");

var split = new FluentSplitButton
{
    Text = "保存",
    Appearance = ControlAppearance.Primary,
    Flyout = menu,
    Size = new Size(160, 32)
};
split.Click += (s, e) => SaveDocument();
Controls.Add(split);
```

### Notes

- The arrow area on the right has a fixed width of 32px, and the default `Padding` reserves 40px on the right so that the text does not overlap the separator line; when sizing the control manually, make sure the width is large enough.
- When `Flyout` is null, clicking the arrow area has no effect at all.

## FluentDropDownButton

Drop-down button (derives from FluentButton): clicking anywhere on the button opens a Flyout menu below it, with an arrow icon drawn on the right.

### Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| Flyout | ContextMenuStrip | null | Menu shown when the button is clicked |

### Events

No additional events; inherits the standard WinForms events (`Click` is raised normally, followed by the menu opening).

### Example

```csharp
var menu = new ContextMenuStrip();
menu.Items.Add("选项一");
menu.Items.Add("选项二");
menu.Items.Add(new ToolStripSeparator());
menu.Items.Add("选项三");

var drop = new FluentDropDownButton
{
    Text = "更多操作",
    Flyout = menu,
    Size = new Size(150, 32)
};
Controls.Add(drop);
```

### Notes

- Difference from FluentSplitButton: there is no separate primary action area, so a click at any position opens the menu and raises `Click`.
- The default `Padding` reserves 36px on the right for drawing the arrow.

## FluentLabel

Fluent-style text label (derives from Label): uses the secondary text color by default and changes color automatically with the theme and the disabled state.

### Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| UseSecondaryColor | bool | true | true uses the secondary text color (TextFillSecondary), false uses the primary text color (TextFillPrimary) |

### Events

No additional events; inherits the standard WinForms events.

### Example

```csharp
var caption = new FluentLabel
{
    Text = "此设置将在下次启动时生效",
    AutoSize = true
};
var title = new FluentLabel
{
    Text = "常规设置",
    UseSecondaryColor = false,
    AutoSize = true
};
Controls.Add(caption);
Controls.Add(title);
```

### Notes

- When the control is disabled (`Enabled = false`) it automatically switches to the disabled text color (TextFillDisabled), regardless of `UseSecondaryColor`.
- The constructor sets `UseMnemonic = false`, so a `&` in the text is displayed literally and is not interpreted as a mnemonic.
- Internally `UseCompatibleTextRendering` (GDI+ rendering) is enabled so that text alpha is correct over system backdrops such as Mica.

## FluentSeparator

Fluent-style separator line: a 1px thin line whose color comes from the theme's divider color (DividerStrokeDefault).

### Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| Orientation | Orientation | Orientation.Horizontal | Direction of the separator; a vertical line is drawn when set to Vertical |

### Events

No additional events; inherits the standard WinForms events.

### Example

```csharp
var hLine = new FluentSeparator { Width = 300 };

var vLine = new FluentSeparator
{
    Orientation = Orientation.Vertical,
    Size = new Size(1, 48)
};
Controls.Add(hLine);
Controls.Add(vLine);
```

### Notes

- Default size 100x1, `TabStop = false`, transparent background; the line is drawn at the midpoint of the control's width/height.
- For a vertical line you need to set `Width` to 1 (or another small value) manually and provide a `Height`.

## FluentAnchor

Relative-positioning container (derives from Panel): places the first child control at a relative position, in the range 0 to 1, within the available space.

### Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| RelativePosition | PointF | (0.5f, 0.5f) | Relative position of the child control; X/Y take values from 0 to 1. (0,0) is top-left, (0.5,0.5) is centered, (1,1) is bottom-right |

### Events

No additional events; inherits the standard WinForms events.

### Example

```csharp
var anchor = new FluentAnchor
{
    Dock = DockStyle.Fill,
    RelativePosition = new PointF(1f, 1f) // bottom-right corner
};
anchor.Controls.Add(new FluentButton
{
    Text = "下一步",
    Appearance = ControlAppearance.Primary
});
Controls.Add(anchor);
```

### Notes

- Only `Controls[0]` (the first child control) is laid out; other child controls are unaffected. The position is computed as `(container size - child size) * relative value` and clamped to non-negative.
- The background is transparent and the container draws nothing itself; layout is redone automatically when the container size changes.

## FluentRatingControl

Star rating control: interactive five-pointed-star rating with hover preview and read-only display.

### Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| Value | int | 0 | Current rating (number of lit stars); automatically clamped to 0~Maximum |
| Maximum | int | 5 | Total number of stars; minimum is 1 |
| ReadOnly | bool | false | Read-only mode: does not react to clicks or hovering, and the cursor reverts to the default arrow |

### Events

- `ValueChanged` (EventHandler): raised when `Value` changes.

### Example

```csharp
var rating = new FluentRatingControl
{
    Maximum = 5,
    Value = 3,
    Size = new Size(160, 32)
};
rating.ValueChanged += (s, e) =>
{
    Console.WriteLine("当前评分:" + rating.Value);
};
Controls.Add(rating);

var showOnly = new FluentRatingControl { Value = 4, ReadOnly = true };
Controls.Add(showOnly);
```

### Notes

- While the mouse hovers over the control, the number of stars that would be selected is previewed in the accent color, and the actual value is restored after the pointer leaves; a left click on the Nth star sets `Value` to N.
- The star size is derived automatically from the control size (`Min(Height-4, (Width-4)/Maximum)`), and the default size is 140x28; after changing `Maximum` it is a good idea to widen the control accordingly.

## FluentThumbRate

Thumbs-up / thumbs-down either-or feedback control: a pair of thumb icons, where the selected one is highlighted in the accent color.

### Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| Value | int | 0 | Feedback state: 1 = like, -1 = dislike, 0 = not rated; assignment is automatically clamped to [-1, 1] |

### Events

- `ValueChanged` (EventHandler): raised when `Value` changes.

### Example

```csharp
var thumb = new FluentThumbRate();
thumb.ValueChanged += (s, e) =>
{
    switch (thumb.Value)
    {
        case 1: SendFeedback("like"); break;
        case -1: SendFeedback("dislike"); break;
        default: SendFeedback("none"); break;
    }
};
Controls.Add(thumb);
```

### Notes

- Clicking the already-selected thumb again clears the feedback (`Value` returns to 0); like and dislike are mutually exclusive.
- Default size 72x32; the two click areas are 32x28 each, aligned to the left and to the right respectively, and a light background is shown on hover.

## FluentArc

Arc control: draws a ring segment with round end caps, typically combined with other elements to build progress-ring or spinner-style visual effects.

### Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| StartAngle | float | 0f | Start angle in degrees, following the GDI+ convention: 0 degrees points to 3 o'clock and clockwise is positive |
| EndAngle | float | 270f | End angle in degrees; the arc is drawn from StartAngle to EndAngle |
| Thickness | float | 4f | Stroke thickness of the arc (pixels) |
| ArcColor | Color | Color.Empty | Arc color; when Empty the theme's accent color (SystemAccentColorPrimary) is used |

### Events

No additional events; inherits the standard WinForms events.

### Example

```csharp
var arc = new FluentArc
{
    Size = new Size(48, 48),
    StartAngle = -90f,
    EndAngle = 180f,   // draws 270 degrees clockwise starting from 12 o'clock
    Thickness = 5f
};
Controls.Add(arc);
```

### Notes

- Nothing is drawn when the swept angle (`EndAngle - StartAngle`) is close to 0; the end caps are round (LineCap.Round).
- The control has no animation of its own; periodically modifying `StartAngle`/`EndAngle` from a Timer is enough to implement a spinning loader ring.
- `TabStop = false`: it is a pure display control and does not take part in the Tab focus cycle.
