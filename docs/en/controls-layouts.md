# Layout and Window Controls Reference

This document covers the seven layout- and window-related types in WinformUi: `FluentTabView` (together with its tab class `FluentTabViewPage`), `FluentGridView`, `FluentTreeGrid` (together with `FluentTreeGridColumn` / `FluentTreeGridNode`), `FluentWrapPanel`, `FluentUniformGrid`, `FluentTitleBar` and `FluentClientAreaBorder`. All of them live in the `WinformUi.Controls` namespace; controls that derive from `FluentControlBase` re-skin automatically with the theme managed by `ApplicationThemeManager`, and layout panels and content containers also subscribe to theme changes in order to repaint their background. Properties, events and default values are all taken from the source code; to keep this document short, the example snippets omit the `using` lines (you need to import the `System`, `System.Drawing`, `System.Windows.Forms`, `WinformUi.Controls`, etc. namespaces yourself).

## FluentTabView

A browser-style tab container: every tab is a rounded label that may carry an icon and may be closable, and the selected tab has an accent-colored bar along its top edge. The tab row wraps automatically when the labels no longer fit, tabs can be reordered by dragging with the mouse, the close button appears on hover, and an optional "new tab" button can be shown. The content of a tab is a real child-control container (used exactly like a TabControl). Default size 400×240.

### Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| Pages | List&lt;FluentTabViewPage&gt; | empty list | The collection of tab pages (the content property; editable directly in the designer). Register pages through AddPage/AddTab — do not just put them into the list without registering them. |
| SelectedIndex | int | -1 | Index of the currently selected tab; an out-of-range value or -1 means no selection; assigning it raises SelectedIndexChanged. |
| SelectedPage | FluentTabViewPage | null | The selected page object; Browsable(false); assigning it is equivalent to setting SelectedIndex via IndexOf. |
| CanReorderTabs | bool | true | Whether a label may be reordered by holding it and dragging (the drag becomes active once the distance exceeds the system DragSize). |
| CanAddTabs | bool | true | Whether a "+" button is drawn to the right of the tab row; clicking it calls AddTab() to create a default tab. |
| TabHeight | int | 36 | Height of the tab row (DIP, scaled internally by DPI); assigned values are clamped to no less than 24. |

The tab class `FluentTabViewPage` (derives from Panel):

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| Text | string | "Tab" | Title of the tab; changing it re-lays out the tab row automatically. Inherited from Panel. |
| Icon | SymbolRegular | SymbolRegular.Empty | 16px icon to the left of the tab; Empty is not drawn. |
| IsClosable | bool | true | Whether a close button is drawn on the right side of the tab (a rounded background appears for it on hover/press). |

### Events

| Event | Description |
| --- | --- |
| SelectedIndexChanged | Raised when the selected tab changes (by clicking, by keyboard, or by assigning the property in code). |
| TabAdding | Raised before AddTab; the TabViewTabAddingEventArgs argument carries Page. Setting Cancel = true cancels the addition, and a handler may also replace or supply Page (when no page is passed the control creates a default tab itself). AddPage does not raise this event. |
| TabClosing | Raised when the close button is clicked or CloseTab is called; the TabViewTabClosingEventArgs argument carries Page. Setting Cancel = true prevents closing (CloseTab then returns false). |

The following methods are also provided: `AddTab(page = null, title = null)` goes through the TabAdding flow and selects the new tab, returning the added page (null when the addition was cancelled); `AddPage(page, title = null)` registers a page directly, bypassing TabAdding, and selects it if there is no selection at the moment; `CloseTab(page)` goes through the TabClosing flow and returns whether the tab was actually closed.

### Example

```csharp
var tabs = new FluentTabView { Dock = DockStyle.Fill };

// Option 1: AddTab creates and selects the page in one step
var home = tabs.AddTab(title: "首页");
home.Icon = SymbolRegular.Home24;
var label = new FluentLabel { Text = "Home", AutoSize = true, Location = new Point(16, 16) };
home.Controls.Add(label);

// Option 2: build the page first, then register it
var settingsPage = new FluentTabViewPage
{
    Text = "设置",
    Icon = SymbolRegular.Settings24,
    IsClosable = false
};
tabs.AddPage(settingsPage);

// Intercept closing: refuse to close the settings page while there are unsaved changes
bool unsavedChanges = true;
tabs.TabClosing += (s, e) =>
{
    if (e.Page == settingsPage && unsavedChanges)
        e.Cancel = true;
};

// Customize the new tab when the "+" button is clicked
tabs.TabAdding += (s, e) =>
{
    e.Page = e.Page ?? new FluentTabViewPage();
    if (e.Page != null)
        e.Page.Text = "标签 " + (tabs.Pages.Count + 1);
};

Controls.Add(tabs);
```

### Notes

- Tab text is measured with FluentTypography.Body and has a minimum width of 48px (DIP); when a label no longer fits at the end of a row it wraps to the next one, the number of tab rows grows with the content, the content area sits directly below the tab strip, and a 1px separator border with rounded bottom corners is drawn.
- The Left/Right keys switch between adjacent tabs (only when focus is on the FluentTabView itself and not on a child control); clicking a label selects it, and holding a label and dragging it horizontally reorders it (CanReorderTabs).
- When CanAddTabs is enabled, space for the "+" button area is permanently reserved on the right; the AddTab triggered by clicking "+" raises TabAdding first, so you can Cancel it or replace the page in that event.
- All sizes go through the control's Scale methods and are scaled by DPI, so no manual adjustment is needed on high-DPI displays.
- On a theme switch the page backgrounds are set to transparent so the container's base color shows through; do not reset BackColor on pages in a way that relies on fixed colors.
- If you lay FluentTabView out together with other controls using Dock, remember the WinForms Dock ordering rule: the control added first ends up closer to the edge.

## FluentGridView

A virtualized tile view: equally sized rounded cards are arranged in columns and wrap automatically, and only the visible rows are painted. A single click selects (the selected card gets a 2px accent-colored border), the mouse wheel scrolls a whole row at a time, arrow-key navigation is supported, and the data-binding model is exactly the same as FluentListBox (IList / BindingSource / DataTable, and so on). Default size 320×220.

### Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| Items | IList | empty List&lt;object&gt; | The data items in unbound mode; replacing the whole list clears the selection and resets scrolling. Reading or assigning Items while DataSource is set throws InvalidOperationException. Browsable(false), so it is not shown in the designer property grid. |
| DataSource | object | null | The data source: IList, IListSource, IEnumerable, DataTable, BindingSource and so on; resolved the same way as in FluentListBox (through BindingContext/CurrencyManager). |
| DataMember | string | "" | Table name when binding to a DataSet. |
| DisplayMember | string | "" | Name of the item property used as the card title; ToString() is used when it is empty. |
| ValueMember | string | "" | Name of the item property used for SelectedValue. |
| SelectedValue | object | null | The ValueMember value of the selected item; assigning it locates and selects the item by value (by scanning the list; the selection stays unchanged when nothing matches). |
| TextSelector | Func&lt;object, string&gt; | null | Custom title-extraction function; takes precedence over DisplayMember, and when it is null the bound text/ToString path is used. |
| IconSelector | Func&lt;object, SymbolRegular&gt; | null | 16px icon drawn above each item; nothing is drawn when it is null or returns Empty. |
| TileSize | Size | 88, 88 | Tile size (DIP); width and height are each clamped to no less than 24. When StretchTiles is true only the width is used as the basis for computing the column count. |
| Spacing | int | 6 | Spacing between tiles and the padding around all four edges (DIP); no less than 0. |
| StretchTiles | bool | true | Whether the full row width is divided evenly among the tiles (otherwise the fixed TileSize width is kept). |
| SelectedIndex | int | -1 | Index of the selected item; an out-of-range value is treated as -1; assigning it scrolls the item into view and syncs the binding position, and raises SelectedIndexChanged. |
| SelectedItem | object | null | The selected item object; assigning it locates the item by IndexOf and leaves the selection unchanged when the item is not found. |

The card click argument class `FluentGridViewItemClickEventArgs` carries `Index` (int) and `Item` (object).

### Events

| Event | Description |
| --- | --- |
| SelectedIndexChanged | Raised when the selection changes: by clicking, keyboard navigation, or assigning in code. |
| ItemClicked | Raised when the left mouse button is pressed on a tile and released on the same tile (after SelectedIndexChanged). |

The method `RefreshItems()` is also provided: use it to re-resolve the binding and refresh after the bound list has been modified in place.

### Example

```csharp
var grid = new FluentGridView { Size = new Size(360, 260) };
grid.Items.Add("Azure");
grid.Items.Add("Bing");
grid.Items.Add("Copilot");
grid.Items.Add("Edge");
grid.Items.Add("GitHub");
grid.Items.Add("Windows");
grid.IconSelector = item => SymbolRegular.Home24;
grid.SelectedIndexChanged += (s, e) =>
    Console.WriteLine("selected: " + grid.SelectedItem);
grid.ItemClicked += (s, e) =>
    Console.WriteLine("clicked #" + e.Index);

// Binding custom objects (App is a plain class declared outside the form:
// class App { public string Name { get; set; } public string Id { get; set; } })
var apps = new List<App> { new App { Name = "邮件", Id = "mail" } };
var bound = new FluentGridView
{
    Size = new Size(360, 260),
    TileSize = new Size(110, 96),
    DisplayMember = "Name",
    ValueMember = "Id",
    DataSource = new BindingSource { DataSource = apps }
};
string id = (string)bound.SelectedValue;

Controls.Add(grid);
Controls.Add(bound);
```

### Notes

- Only the visible rows are painted (the first and last visible row are computed from the scroll position), so with many items the drawing cost is proportional to the visible area; prefer it over FluentWrapPanel when there are many data items.
- Keyboard: Left/Right move between adjacent items, Up/Down move to the row above/below in the same column, Home/End jump to the first/last item, and the selected item is scrolled into view automatically. Scrolling is mouse-wheel only and no scroll bar is drawn; each wheel notch scrolls one row (the row count is computed from Delta/120).
- The selected card uses a ControlFillSecondary background plus a 2px accent-colored rounded border, and hovering produces a slight background change; to customize the card content override the protected `PaintTile(Graphics, int, RectangleF)`.
- Items, TextSelector, IconSelector, SelectedValue and SelectedItem are marked Browsable(false) and are for code use only; designer data binding goes through DataSource/DisplayMember/ValueMember. Several list controls bound to the same data source share their current position through the CurrencyManager.
- All layout sizes (TileSize, Spacing) are DPI-scaled before they take part in the column-count computation, so the number of columns adapts automatically on high DPI.

## FluentTreeGrid

A tree data grid: a header row plus multi-column tree rows. Rows expand and collapse like a TreeView (a collapsed node shows a right-pointing arrow, an expanded one a down-pointing arrow), selection covers the whole row (accent-colored background plus a 3px vertical bar on the left), column and row separator lines are drawn, and mouse-wheel scrolling plus tree keyboard navigation are supported. Default size 480×240.

### Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| Columns | List&lt;FluentTreeGridColumn&gt; | empty list | Collection of column definitions (the content property); when it is empty each row shows only the node Text and no header text columns are drawn. |
| RootNodes | List&lt;FluentTreeGridNode&gt; | empty list | Collection of root nodes (the content property); after modifying it you must call Rebuild() to flatten the visible rows again. |
| SelectedNode | FluentTreeGridNode | null | The selected node; assigning it performs a visibility check along the ancestor chain, scrolls the node into view and raises SelectedNodeChanged. Browsable(false). |

The column class `FluentTreeGridColumn`:

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| Title | string | "" | Header text. |
| Width | int | 0 | Column width (DIP); 0 means the remaining space is divided evenly with the other columns whose width is 0. |
| TextAlign | HorizontalAlignment | Left | Alignment of the header and the cells (Left/Center/Right). |

The node class `FluentTreeGridNode`:

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| Text | string | "" | Text of column 0 (the tree column). |
| Icon | SymbolRegular | SymbolRegular.Empty | 16px icon to the left of the text; Empty is not drawn. |
| Tag | object | null | Arbitrary business data. |
| Cells | List&lt;string&gt; | empty list | Cell values from column 1 onwards (Cells[i] corresponds to Columns[i+1]); missing entries are drawn as empty strings. |
| IsExpanded | bool | false | Whether the child nodes are expanded; at run time clicking the arrow or pressing Left/Right toggles it. |
| Nodes | List&lt;FluentTreeGridNode&gt; | empty list | Collection of child nodes. |

Convenient constructors: `new FluentTreeGridNode("text", child1, child2...)` fills Text and the child nodes at the same time; `new FluentTreeGridColumn("title", width)` fills Title and Width at the same time.

### Events

| Event | Description |
| --- | --- |
| SelectedNodeChanged | Raised when SelectedNode changes. |
| NodeExpandedChanged | Raised when a node's expanded/collapsed state changes (clicking the arrow with the mouse or pressing Left/Right). |

### Example

```csharp
var treeGrid = new FluentTreeGrid { Dock = DockStyle.Fill };
treeGrid.Columns.Add(new FluentTreeGridColumn("名称", 220));
treeGrid.Columns.Add(new FluentTreeGridColumn("大小", 100) { TextAlign = HorizontalAlignment.Right });
treeGrid.Columns.Add(new FluentTreeGridColumn("类型"));   // Width = 0, divides up the remaining space

var project = new FluentTreeGridNode("WinformUi") { IsExpanded = true };
project.Cells.Add("—");
var src = new FluentTreeGridNode("src") { IsExpanded = true };
src.Nodes.Add(new FluentTreeGridNode("FluentTabView.cs"));
project.Nodes.Add(src);
treeGrid.RootNodes.Add(project);
treeGrid.RootNodes.Add(new FluentTreeGridNode("README.md"));
treeGrid.Rebuild();

treeGrid.SelectedNodeChanged += (s, e) =>
    Console.WriteLine(treeGrid.SelectedNode != null ? treeGrid.SelectedNode.Text : "(none)");

Controls.Add(treeGrid);
```

### Notes

- After modifying RootNodes, or the Nodes/IsExpanded of any node, you must call `Rebuild()` to flatten the visible rows again (the first OnPaint performs an automatic Rebuild if the tree was never flattened, but later modifications are not detected automatically).
- Layout constants (DIP, scaled internally by DPI): header height 36, row height 30, indentation per level 18; both selection and scrolling automatically make the row visible.
- Keyboard: Up/Down move between visible rows, Left collapses the current node, Right expands it, Home/End jump to the first/last visible row; scrolling is mouse-wheel only, one row at a time, and the header stays fixed and never scrolls out of view.
- In column 0 the arrow (when the node has children) and then the optional icon are drawn in order before the text; clicking the arrow rectangle (18px wide, full row height) toggles expansion, while clicking anywhere else in the row selects it.
- This control is a self-drawn display control; it offers no cell editing, sorting or data-source binding. Use FluentDataGridView when you need an editable grid.

## FluentWrapPanel

A wrapping flow panel with a themed background, derived from `FlowLayoutPanel`: child controls are laid out from left to right according to their Margin, wrapping automatically at the boundary, and scroll bars appear automatically when the content overflows. Suitable for loose arrangement of a small number of equal-sized cards or chips; switch to FluentGridView when there are many items and virtualized scrolling is needed.

### Properties

FluentWrapPanel adds no public properties of its own; everything is inherited from `FlowLayoutPanel`, and the constructor presets three values:

| Property (inherited) | Type | Constructor preset | Description |
| --- | --- | --- | --- |
| WrapContents | bool | true | Wraps automatically at the edge; can be set back to false to get single-direction scrolling. |
| FlowDirection | FlowDirection | LeftToRight | Layout direction; can be changed to TopDown/RightToLeft and so on. |
| AutoScroll | bool | true | Enables automatic scrolling when the content exceeds the client area. |

Everything else — `Padding`, the `Margin` of each child control, `FlowDirection` and so on — is adjusted using the normal FlowLayoutPanel conventions to control spacing and alignment.

### Example

```csharp
var wrap = new FluentWrapPanel
{
    Dock = DockStyle.Fill,
    Padding = new Padding(8)
};

foreach (string name in new[] { "Azure", "Bing", "Copilot", "Edge", "GitHub", "Office", "OneDrive", "Outlook", "Teams" })
{
    var chip = new FluentButton { Text = name, AutoSize = true, Margin = new Padding(4) };
    wrap.Controls.Add(chip);
}

Controls.Add(wrap);
```

### Notes

- The background is painted in OnPaintBackground through the control library's shared solid-background drawing (ControlHelper.PaintFullBackground), and it repaints automatically when ApplicationThemeManager switches themes (subscribed internally through ThemeSubscription).
- There is no virtualization: every child control is a real control and takes part in layout, so with tens or hundreds of items the performance and scroll smoothness are worse than FluentGridView.
- Like the plain FlowLayoutPanel, it arranges children by their DesiredSize/AutoSize; a row's height is determined by the tallest control in it and equal heights are not guaranteed. Use FluentUniformGrid or FluentGridView when you need a strict grid of identical cells.

## FluentUniformGrid

An even-cell layout panel derived from `Panel`: it divides the client area into equally sized cells of Rows × Columns, and child controls are placed into the cells in the order they were added (their Controls index).

### Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| Rows | int | 0 | Number of rows; 0 means automatic: if Columns > 1 the row count is derived from the number of child controls rounded up, otherwise the row count equals the number of child controls (a single column). Assigned values are clamped to no less than 0. |
| Columns | int | 1 | Number of columns; no less than 1. When Rows is specified and Columns is still the default 1, the column count grows by rounding up if the number of child controls exceeds the row count. |

### Example

```csharp
var uniform = new FluentUniformGrid { Dock = DockStyle.Fill, Rows = 2, Columns = 3 };
for (int i = 1; i <= 6; i++)
{
    var card = new FluentCard { Text = "卡片 " + i, Margin = new Padding(4) };
    uniform.Controls.Add(card);
}

// Only the column count is fixed, the row count is automatic
var autoRows = new FluentUniformGrid { Columns = 4 };
foreach (string s in new[] { "A", "B", "C", "D", "E" })
    autoRows.Controls.Add(new FluentLabel { Text = s, AutoSize = true });

Controls.Add(uniform);
Controls.Add(autoRows);
```

### Notes

- Cell size = client-area width / column count and height / row count (integer division, at least 1px); child controls are given Bounds directly so they fill the cell, and the child controls' own Margin/Padding take no part in the computation — if you need gaps, wrap each child in another panel or use a control with internal padding.
- Child controls that do not fit (row index >= Rows) are hidden by setting Visible = false rather than scrolled, so make sure the cell count is large enough.
- Layout happens in OnLayout; adding or removing child controls and changing the size both trigger an automatic relayout. The background uses the same themed solid drawing as FluentWrapPanel.
- The index order is the visual order: the i-th child control sits in row i / Columns and column i % Columns.

## FluentTitleBar

A Fluent title bar for borderless forms: an optional icon on the left (taken automatically from the owning Form.Icon) plus the title text, and a group of help/minimize/restore/maximize/close buttons on the right. It supports dragging the title area to move the window, double-click to maximize, resizing by dragging the top edge, and a system menu on right-click; when a borderless window is maximized it automatically adds Padding to the form so that content is not clipped by the screen edge. It is meant to be docked at the top of a `FormBorderStyle.None` form, and the default docked height is fixed at 48px (DPI-scaled).

### Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| Title | string | "" | Title text (localization attribute Localizable); drawn with the bold BodyStrong font and truncated with an ellipsis when it is too long. |
| ShowHelpButton | bool | false | Whether a "?" help button is shown to the left of the minimize button; clicking it raises HelpButtonClick. |
| LeadingContent | Control | null | Slot control placed immediately to the right of the title (vertically centered); Browsable(false), and assigning it adds/removes the child control automatically and re-lays out. |
| TrailingContent | Control | null | Slot placed to the left of the button group (vertically centered); same as above. |

The visibility of the button group is decided by the owning form: no minimize button is drawn when Form.MinimizeBox is false, and no maximize button is drawn when Form.MaximizeBox is false; while the form is maximized the maximize button automatically turns into the "restore" glyph. If the owning form has an Icon it is drawn as a 16px icon, and if it has none the title shifts left to take the space — the title bar itself has no ShowIcon property.

### Events

| Event | Description |
| --- | --- |
| HelpButtonClick | Raised when the help button is clicked; no help window is opened automatically, the behavior is entirely up to the subscriber. |

### Example

```csharp
public sealed class ChromeForm : Form
{
    public ChromeForm()
    {
        Text = "自定义标题栏演示";
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(720, 480);
        Icon = SystemIcons.Application;

        // Note: add the Fill content first, then dock the title bar to Top,
        // otherwise the Fill area is covered by the title bar (WinForms Dock ordering rule).
        var content = new FluentWrapPanel { Dock = DockStyle.Fill };
        Controls.Add(content);

        var titleBar = new FluentTitleBar
        {
            Dock = DockStyle.Top,
            Title = "WinformUi 演示",
            ShowHelpButton = true
        };
        titleBar.HelpButtonClick += (s, e) =>
            MessageBox.Show("帮助");
        titleBar.TrailingContent = new FluentLabel
            { Text = "已保存", AutoSize = true };
        Controls.Add(titleBar);
    }
}
```

### Notes

- Hit testing is implemented by overriding WM_NCHITTEST: the button area returns HTMINBUTTON/HTMAXBUTTON/HTCLOSE/HTHELP (which keeps DWM hover tracking, while the click itself is handled by the control), and when the window is not maximized and is in its normal state the top corners and the left/right edges return HTTOPLEFT/HTTOPRIGHT/HTTOP/HTLEFT/HTRIGHT so they can be stretched, while the remaining area returns HTCAPTION — so blank space can be dragged to move the window and double-clicked to maximize/restore; right-clicking the title area pops up the form's system menu (Move/Size/Minimize/Close). Stretching by the bottom edge is handled by the rest of the form's client area and is not the title bar's responsibility.
- Maximization compensation: when the owning form has FormBorderStyle.None and WindowState = Maximized, the control temporarily changes Form.Padding to the width of SystemInformation.FrameBorderSize (the original value is restored when the control is disposed or the window is restored); content panels should therefore rely on Padding rather than negative margins for layout. When the content area uses FluentClientAreaBorder, that control takes care of the same fill, so when the two are combined follow the form's actual layout to avoid the look of doubled whitespace.
- When docked Top or Bottom the height is locked by SetBoundsCore to Scale(48), and the buttons are 46×32 (DIP); setting these manually has no effect.
- The close button turns red (SystemFillCritical) with a white icon on hover, while the other buttons get a SubtleFill rounded background on hover; when the form has Enabled = false the buttons stop responding to hit testing.
- Background painting distinguishes the host: when the host is a FluentWindow that has already applied its backdrop the backdrop shows through, otherwise an opaque window color is painted, so that PrintWindow captures do not come out black.
- Dock ordering reminder: in WinForms the Dock control added first takes the more outer edge. To have the title bar span the whole top row, call Controls.Add for the content area (Fill) first and then for the title bar (Top); reversing the order makes the Fill content cover the title bar area. The equivalent operation in the designer is to move FluentTitleBar to the end of the "bring to front / top ordering".

## FluentClientAreaBorder

A content container panel for custom-bordered forms, derived from `Panel`: it is intended for forms with FormBorderStyle.None and does two things — it adds Padding equal to the thickness of the window resize border to itself when the host form is maximized (a borderless maximized window would otherwise push content off the screen edge), and on older systems that lack a modern window border it draws a 1px outer outline.

### Properties

FluentClientAreaBorder adds no public properties at all; everything comes from `Panel` (the typical usage is `Dock = DockStyle.Fill`). The relevant internal behavior:

| Internal mechanism | Description |
| --- | --- |
| Padding (automatic) | When the host form has no border and is maximized, this is automatically set to the larger of the width and height of SystemInformation.FrameBorderSize; in every other state it is restored to 0. Do not override this Padding manually, or this control will rewrite it on maximizing. |
| Border line (automatic) | A 1px rectangular outline is drawn only when the operating system's major version is below 10 (no modern window border support), using the color of the current ApplicationThemeManager theme (dark #3A3A3A, light #7A7A7A); on Windows 10+ nothing is drawn and the system DWM outline is left in charge. |
| Form tracking | On OnHandleCreated / OnParentChanged it hooks the host form's LocationChanged / Resize to recompute Padding, and unhooks them in Dispose. |

### Example

```csharp
public sealed class BorderlessForm : Form
{
    public BorderlessForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        ClientSize = new Size(640, 420);

        // Add the Fill content container first, then the Top title bar, so the title bar spans the top row
        var body = new FluentClientAreaBorder { Dock = DockStyle.Fill };
        var hello = new FluentLabel { Text = "内容区", AutoSize = true, Location = new Point(20, 20) };
        body.Controls.Add(hello);
        Controls.Add(body);

        var titleBar = new FluentTitleBar { Dock = DockStyle.Top, Title = "无边框窗体" };
        Controls.Add(titleBar);
    }
}
```

### Notes

- Use it together with FluentTitleBar: the title bar handles the non-client-area interaction (dragging, hit testing) while this control handles the content area's safe-area inset when maximized, and both detect the FormBorderStyle.None + Maximized state.
- The background uses Panel's default painting and repaints automatically on theme switches through ThemeSubscription; dock or position child controls inside its client area as usual — on maximizing they will notice the Padding change and move inwards as a whole, which is the intended behavior.
- The outline on older systems (Windows 8.1 and earlier) takes effect only on sub-Windows-10 versions; the project multi-targets from net48 up to net10, and when running on modern Windows the border is provided by the system so the control does not draw a duplicate line.
- It does not do rounded corners and does not do shadows; it is only a lightweight "content safe area plus compatibility outline" container. For the overall window look, combine it with FluentWindow.
