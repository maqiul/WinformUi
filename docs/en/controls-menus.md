# Menus and Toolbar Rendering

This document covers the WinformUi menu system: `FluentMenuStrip` (menu bar), `FluentContextMenuStrip` (context menu), `FluentToolStripRenderer` (Fluent renderer), and `FluentColorTable` (Fluent color table). All live in the `WinformUi.Controls` namespace. The core idea is to keep every feature of the native `MenuStrip` / `ContextMenuStrip` / `ToolStrip` and replace only the rendering layer, so existing native controls can also be reskinned directly. Example code omits `using` lines; in real usage, import the `System`, `System.Drawing`, `System.Windows.Forms`, `WinformUi.Controls`, `WinformUi.Theming`, and other relevant namespaces.

## FluentMenuStrip

The Fluent menu bar: a `MenuStrip` subclass with a transparent background that shows a rounded, subtly filled highlight on hover, ideal for the top of a Mica window.

### Properties

No new public properties of its own; inherits all properties of `MenuStrip`. The constructor presets: `Renderer = new FluentToolStripRenderer()`, `BackColor = Color.Transparent`, `Font = FluentTypography.Body`, `Padding = new Padding(4, 2, 4, 2)`.

### Events/Methods

No new public events or methods of its own; menu items still use the standard `ToolStripMenuItem`, `ToolStripSeparator`, etc., and event usage is exactly the same as native.

### Example

```csharp
var menu = new FluentMenuStrip { Dock = DockStyle.Top };

var file = new ToolStripMenuItem("文件(&F)");
file.DropDownItems.Add(new ToolStripMenuItem("新建", null, (s, e) => Console.WriteLine("新建")));
file.DropDownItems.Add(new ToolStripMenuItem("打开..."));
file.DropDownItems.Add(new ToolStripSeparator());
file.DropDownItems.Add(new ToolStripMenuItem("退出", null, (s, e) => Close()));

var edit = new ToolStripMenuItem("编辑(&E)");
edit.DropDownItems.Add(new ToolStripMenuItem("剪切") { ShortcutKeys = Keys.Control | Keys.X });
edit.DropDownItems.Add(new ToolStripMenuItem("复制") { ShortcutKeys = Keys.Control | Keys.C });

var view = new ToolStripMenuItem("视图(&V)");
view.DropDownItems.Add(new ToolStripMenuItem("状态栏") { Checked = true, CheckOnClick = true });

menu.Items.AddRange(new ToolStripItem[] { file, edit, view });
Controls.Add(menu);
MainMenuStrip = menu;
```

### Notes

- The menu bar itself is transparent and shows the window background directly (in Mica scenarios the renderer draws the top-level text with GDI+, preventing DWM from treating the GDI text as transparent pixels).
- Drop-downs automatically get the Fluent flyout appearance: opaque surface, outline, rounded hover items.
- To apply the Fluent look to every native toolbar on a form, see the global usage of `FluentToolStripRenderer` below.

## FluentContextMenuStrip

The Fluent context menu: a `ContextMenuStrip` subclass with a flyout surface + rounded hover items that automatically applies a rounded Region when opened.

### Properties

No new public properties of its own; inherits all properties of `ContextMenuStrip`. The constructor presets: `Renderer = new FluentToolStripRenderer()`, `BackColor = Palette.AcrylicBackgroundFillDefault`, `Font = FluentTypography.Body`, `ShowImageMargin = false`, `Padding = new Padding(2)`, and hooks the `Opening` event to refresh the background color and rounded corners.

### Events/Methods

No new public events or methods of its own; usage is the same as the native `ContextMenuStrip`.

### Example

```csharp
var ctxMenu = new FluentContextMenuStrip();
ctxMenu.Items.Add(new ToolStripMenuItem("刷新", null, (s, e) => Console.WriteLine("刷新")));
ctxMenu.Items.Add(new ToolStripMenuItem("重命名"));
ctxMenu.Items.Add(new ToolStripSeparator());

var sortMenu = new ToolStripMenuItem("排序方式");
sortMenu.DropDownItems.Add("名称");
sortMenu.DropDownItems.Add("修改日期");
sortMenu.DropDownItems.Add("大小");
ctxMenu.Items.Add(sortMenu);

ctxMenu.Items.Add(new ToolStripSeparator());
ctxMenu.Items.Add(new ToolStripMenuItem("删除"));

// Attach to any control (including native ones)
var target = new Panel { Size = new Size(320, 90), BorderStyle = BorderStyle.FixedSingle };
target.ContextMenuStrip = ctxMenu;
Controls.Add(target);
```

### Notes

- Every `Opening` re-reads the background color from the current theme and rebuilds the rounded Region, so there is no need to recreate the menu after a theme switch.
- The image margin is disabled by default (`ShowImageMargin = false`); set it back to true yourself if you need the icon column.

## FluentToolStripRenderer

The Fluent toolbar renderer: derived from `ToolStripProfessionalRenderer`, it produces a menu look with rounded hover fills and themed text/arrows/separators; the constructor automatically uses `FluentColorTable` and sets `RoundedEdges = false`.

### Properties

No new public properties of its own; inherits the properties of `ToolStripProfessionalRenderer` (such as `RoundedEdges`, `ColorTable`).

### Events/Methods

No new public events or methods of its own; the appearance is implemented by overriding the `OnRender*` family of protected methods:

- Menu item backgrounds: on hover/press, a rounded fill inset by 2px is drawn; top-level MenuStrip items blend against the form's effective background, while dropdown items blend against the flyout surface.
- Text: enabled items use the primary text color, disabled items the disabled color; MenuStrip top-level text is drawn with GDI+ (Mica-compatible), dropdown text with native GDI.
- Separators: a thin themed line inset by 8px at both ends; arrows use the secondary text color.
- The MenuStrip body itself draws no background or border (stays transparent); other ToolStrips draw the flyout background + outline.

### Example

There are two ways to give a native `MenuStrip` / `ToolStrip` / `ContextMenuStrip` the Fluent renderer:

```csharp
// Option 1: replace per control (affects only that control and its dropdowns)
var nativeMenu = new MenuStrip();
nativeMenu.Renderer = new FluentToolStripRenderer();

var nativeToolStrip = new ToolStrip();
nativeToolStrip.Renderer = new FluentToolStripRenderer();

var nativeContext = new ContextMenuStrip();
nativeContext.Renderer = new FluentToolStripRenderer();
```

```csharp
// Option 2: global replacement — every ToolStrip-family control in the app (including designer-dropped ones) is reskinned
// Recommended to set in Program.Main, before any form is created
ToolStripManager.Renderer = new FluentToolStripRenderer();
```

### Notes

- The global approach is the least work, but if a designer-generated `MenuStrip` explicitly sets its Renderer in the properties, that overrides the global value and the line must be removed.
- The renderer reads `ApplicationThemeManager.Palette` live, so a theme switch takes effect on the next repaint without replacing the instance.
- Swapping the renderer on a native control changes only the appearance; to get the transparent menu-bar effect you also need to set `MenuStrip.BackColor` to `Color.Transparent` (see the `FluentMenuStrip` presets).

## FluentColorTable

The Fluent color table: derived from `ProfessionalColorTable`, it maps the various menu/toolbar color slots onto Fluent theme tokens for use by `FluentToolStripRenderer`, and can also be attached on its own to other Professional renderers.

### Properties

All are overridden read-only color properties, read live from the current theme palette (`P = ApplicationThemeManager.Palette`; Surface = `P.AcrylicBackgroundFillDefault`, Hover = SubtleFillSecondary blended over Surface, Pressed = SubtleFillTertiary blended over Surface):

| Property | Type | Returns | Description |
| --- | --- | --- | --- |
| MenuBorder | Color | P.SurfaceStrokeFlyout | Drop-down menu border. |
| MenuItemBorder | Color | Transparent | Menu item border (hidden). |
| MenuItemSelected / MenuItemSelectedGradientBegin / MenuItemSelectedGradientEnd | Color | Hover | Hovered menu item fill. |
| MenuItemPressedGradientBegin / Middle / End | Color | Pressed | Pressed menu item fill. |
| MenuStripGradientBegin / End | Color | Transparent | Menu bar body is transparent. |
| ToolStripBorder | Color | Transparent | Toolbar border (drawn separately by the renderer). |
| ToolStripGradientBegin / Middle / End | Color | Transparent | Toolbar gradient (hidden). |
| ToolStripDropDownBackground | Color | Surface | Drop-down background. |
| ImageMarginGradientBegin / Middle / End | Color | Surface | Icon margin column background. |
| SeparatorDark | Color | DividerStrokeDefault blended over Surface | Separator line. |
| SeparatorLight | Color | Transparent | Separator light edge (hidden). |
| CheckBackground | Color | P.SystemAccentColorPrimary | Check-mark background. |
| CheckPressedBackground / CheckSelectedBackground | Color | P.AccentFillSecondary | Check-mark pressed/hovered background. |
| ButtonSelectedBorder | Color | Transparent | Toolbar button hover border (hidden). |
| ButtonSelectedHighlight / ButtonSelectedGradientBegin / Middle / End | Color | Hover | Toolbar button hover fill. |
| ButtonPressedGradientBegin / Middle / End | Color | Pressed | Toolbar button pressed fill. |
| OverflowButtonGradientBegin / Middle / End | Color | Transparent | Overflow button gradient (hidden). |

### Events/Methods

No public events or methods of its own.

### Example

```csharp
// Normally you don't need to use it directly: FluentToolStripRenderer already includes it at construction
var renderer = new FluentToolStripRenderer();

// You can also attach the Fluent color table alone to a native Professional renderer
var toolStrip = new ToolStrip();
toolStrip.Renderer = new ToolStripProfessionalRenderer(new FluentColorTable());
Controls.Add(toolStrip);
```

### Notes

- All properties are computed dynamically (each read takes the current palette), so light/dark switching takes effect immediately.
- Semi-transparent tokens are pre-blended with the flyout surface into opaque colors to avoid alpha issues in GDI painting.
