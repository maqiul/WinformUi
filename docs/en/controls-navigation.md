# Navigation and Container Controls Reference

This document covers the navigation and container related controls in WinformUi:
`FluentNavigationView` (with `NavigationViewItem`, `FluentPage`, `FluentFrame`),
`FluentTabControl`/`FluentTabPage`, `FluentBreadcrumbBar`,
`FluentStatusBar`/`FluentStatusStrip`, `FluentSplitContainer`,
`FluentCard`/`FluentCardAction`/`FluentCardColor`, `FluentExpander`/`FluentCardExpander`,
`FluentScrollViewer`. All live in the namespace `WinformUi.Controls` and re-skin automatically
with the theme from `ApplicationThemeManager`. Properties, events and default values all follow
the source code; example snippets can be dropped straight into form code (you need to import
namespaces such as `System`, `System.Drawing`, `System.Windows.Forms`, `WinformUi.Controls`,
`WinformUi.Theming`).

## FluentPage

Base class for navigable pages (derives from `UserControl` and implements `INavigationAware`):
opaque layered surface, automatic DPI scaling, and its background color updates automatically on
theme switches.

### Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| NavigationCacheMode | NavigationCacheMode | Enabled | How the host `FluentFrame` caches this page; see the enum notes below. |

Other than that it inherits all UserControl properties. Constructor presets:
`Padding = new Padding(24, 16, 24, 24)`, `AutoScaleMode = AutoScaleMode.Dpi`, background set to
the theme's Layer surface color. The protected property `SurfaceColor` (Color) lets derived
classes read the current layer surface color.

### Events

This class declares no new events; it inherits the standard Control/UserControl events. Page
lifecycle is obtained by overriding the virtual methods:

| Protected virtual method | Description |
| --- | --- |
| `virtual void OnNavigatedTo(NavigationEventArgs e)` | Called after the page becomes the current page (including arriving via forward/back/refresh). |
| `virtual void OnNavigatedFrom(NavigationEventArgs e)` | Called just before the page stops being the current page. |

`INavigationAware` is implemented explicitly on `FluentPage`, so derived classes only override the
`protected virtual` version. To get lifecycle callbacks on an arbitrary `Control` that does not
derive from `FluentPage`, implement `INavigationAware` yourself.

### NavigationCacheMode

| Value | Description |
| --- | --- |
| Disabled | Not cached. The instance is `Dispose`d when the page leaves and a new one is built on the next visit. |
| Enabled | Cached, bounded by `FluentFrame.CacheSize` (the default). Least-recently-used pages are evicted beyond the limit. |
| Required | Always cached; never counted against `CacheSize` and never evicted. Suitable for pages holding expensive state. |

### NavigationMode and NavigationEventArgs

`NavigationMode` states how the page was reached: `New` (fresh navigation), `Back`, `Forward`, or
`Refresh` (re-navigating to the same "type + parameter", which only replays the history position).

`NavigationEventArgs` carries `PageType` (Type), `Parameter` (object), `Mode` (NavigationMode) and
`CacheMode` (NavigationCacheMode).

### Example

```csharp
public class DetailPage : FluentPage
{
    private readonly FluentLabel _title = new FluentLabel { AutoSize = true };

    public DetailPage()
    {
        // Instances are reused per identical parameter; different orders cache separately
        NavigationCacheMode = NavigationCacheMode.Enabled;
        Controls.Add(_title);
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        _title.Text = "Order " + e.Parameter;
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        // Stop animations / commit edits before leaving
    }
}
```

### Notes

- The page background is deliberately kept opaque: a transparent background on a real window
  handle would trigger recursive parent painting and leave artifacts; do not set its BackColor
  to Transparent.
- On a theme switch the page recalculates its background only after the notification round has
  finished. A page hears the theme event before its host window repaints, so blending then would
  composite the new token over the window's previous background and freeze there — a washed-out
  gray surface under the dark theme.

## FluentFrame

Navigation host with parameter-aware history and configurable caching (derives from `Panel`):
instances are cached by "page type + navigation parameter", docked for display, and the content
area paints a Fluent layer surface with a rounded top-left corner.

### Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| CurrentPage | Control | null | Read-only, the page instance currently displayed. |
| CurrentPageType | Type | null | Read-only, the type of the current page. |
| CurrentParameter | object | null | Read-only, the navigation parameter used to reach the current page. |
| CurrentCacheMode | NavigationCacheMode | Enabled | Read-only, the cache mode in effect for the current page (any non-`FluentPage` counts as Enabled). |
| CanGoBack | bool | false | Read-only, whether back navigation is possible in the history stack. |
| CanGoForward | bool | false | Read-only, whether forward navigation is possible in the history stack. |
| CacheSize | int | 10 | Maximum number of Enabled pages kept; 0 disables caching entirely. Assigning a negative value throws `ArgumentOutOfRangeException`. `Required` pages are not counted. |
| PageFactory | Func&lt;Type, object, Control&gt; | null | Optional page factory used instead of the parameterless constructor. Arguments are the page type and the navigation parameter; returning null throws `InvalidOperationException`. |

### Events

| Event | Description |
| --- | --- |
| Navigated | Raised after every completed navigation, including Navigate, GoBack, GoForward and same-page refresh. The argument is a `NavigationEventArgs` (see the FluentPage/FluentFrame sections). |

Public methods:

| Method | Description |
| --- | --- |
| Control Navigate(Type pageType) | Navigates to the given page type (must be a Control type) with a null parameter and returns the page instance. |
| Control Navigate(Type pageType, object parameter) | Navigates with a parameter. The parameter takes part in the cache key and in history, so the same type with different parameters yields different instances; read it back from `e.Parameter` in `OnNavigatedTo`. |
| T Navigate&lt;T&gt;() where T : Control | Generic navigation, returns a strongly-typed page. |
| T Navigate&lt;T&gt;(object parameter) where T : Control | Generic parameterized navigation. |
| void GoBack() | Goes back to the previous page in history (no-op when CanGoBack is false); `Mode` is Back. |
| void GoForward() | Goes forward to the next page in history (no-op when CanGoForward is false); `Mode` is Forward. |

### Example

```csharp
var frame = new FluentFrame { Dock = DockStyle.Fill, CacheSize = 6 };

// Pages without a parameterless constructor are built by the factory
frame.PageFactory = (type, parameter) =>
    type == typeof(DetailPage) ? new DetailPage((string)parameter) : (Control)Activator.CreateInstance(type);

frame.Navigated += (s, e) =>
{
    var args = (NavigationEventArgs)e;
    Console.WriteLine(args.PageType.Name + " [" + args.Mode + "] " + args.Parameter);
};

frame.Navigate<HomePage>();
frame.Navigate(typeof(DetailPage), "A-1001");
frame.Navigate(typeof(DetailPage), "A-1002");   // separate instance, does not overwrite the first
if (frame.CanGoBack) frame.GoBack();
if (frame.CanGoForward) frame.GoForward();

Controls.Add(frame);
```

### Notes

- Both the cache key and each history entry are "page type + parameter", so the same type with a
  different parameter is never mistaken for the same page.
- Re-navigating to the entry you are already on does not append history: when the current
  "type + parameter" matches the target exactly, only a `Mode = Refresh` lifecycle callback fires.
- When `PageFactory` is null the page type must provide a public parameterless constructor; with a
  factory, the factory handles the parameter.
- Navigating to a new page from the same position truncates the forward history after it,
  matching browser behavior.
- On leaving, `Disabled` pages (and `Enabled` pages when `CacheSize = 0`) are disposed immediately;
  `Enabled` pages beyond `CacheSize` are evicted least-recently-used first, `Required` pages never.
- Disposing a page you were handed by `Navigate` is allowed: the cache notices the released instance
  on the next navigation and creates a fresh one instead of touching a disposed control.

## NavigationViewItem

Menu item of `FluentNavigationView` (plain class, not a control).

### Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| Content | string | null | Display text of the menu item. |
| Icon | SymbolRegular | SymbolRegular.Empty | Menu icon; nothing is drawn when set to Empty. |
| TargetPageType | Type | null | Page type to navigate to on click; when null, the item is only selected without navigating. |
| Tag | object | null | Passed as the navigation parameter to `FluentFrame.Navigate(TargetPageType, Tag)`; can still hold attached data. |

Constructors: `NavigationViewItem()`, `NavigationViewItem(string content, SymbolRegular icon, Type targetPageType)`.

### NavigationViewItemHeader and NavigationViewItemSeparator

Both derive from `NavigationViewItem` and group the menu, but they cannot be selected or navigated
to and are skipped by hit-testing:

| Type | Constructors | Expanded pane | Compact pane |
| --- | --- | --- | --- |
| `NavigationViewItemHeader` | `()`, `(string text)` | 34-pixel section caption, only `Content` is drawn (Icon and TargetPageType are ignored) | Height 0, takes no space |
| `NavigationViewItemSeparator` | `()` | 16-pixel divider line (`DividerStrokeDefault`) | 12 pixels |

```csharp
navigation.MenuItems.Add(new NavigationViewItemHeader("Controls"));
navigation.MenuItems.Add(new NavigationViewItem("Buttons", SymbolRegular.Keyboard24, typeof(ButtonsPage)));
navigation.MenuItems.Add(new NavigationViewItem("Collections", SymbolRegular.List24, typeof(CollectionsPage)));
navigation.MenuItems.Add(new NavigationViewItemSeparator());
navigation.MenuItems.Add(new NavigationViewItemHeader("App"));
navigation.MenuItems.Add(new NavigationViewItem("Settings", SymbolRegular.Settings24, typeof(SettingsPage)));
navigation.FooterMenuItems.Add(new NavigationViewItem("About", SymbolRegular.Info24, typeof(AboutPage)));
```

`SelectItem` and mouse clicks skip both types automatically, and `SelectedItem` is never a header
or a separator.

### Navigating with a parameter

Because `Tag` is the navigation parameter, one page type can be registered several times:

```csharp
navigation.MenuItems.Add(new NavigationViewItem("Order 1001", SymbolRegular.Document24, typeof(DetailPage)) { Tag = "A-1001" });
navigation.MenuItems.Add(new NavigationViewItem("Order 1002", SymbolRegular.Document24, typeof(DetailPage)) { Tag = "A-1002" });
// DetailPage.OnNavigatedTo receives e.Parameter == "A-1001" / "A-1002"
```

## FluentNavigationView

Fluent navigation view: an icon menu pane on the left, an accent-color selection bar on the
active item, a hamburger button that collapses the pane into a compact strip, and a layered
content frame (`FluentFrame`) on the right.

### Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| MenuItems | List&lt;NavigationViewItem&gt; | Empty list | Read-only collection, the main menu items (laid out top-down below the hamburger button); populate by calling Add directly. `NavigationViewItemHeader`/`NavigationViewItemSeparator` can be mixed in to group the menu. |
| FooterMenuItems | List&lt;NavigationViewItem&gt; | Empty list | Read-only collection, footer menu items (flush with the bottom, divider line above), typically holds "Settings". Group headers and separators work here too. |
| Frame | FluentFrame | Built-in instance | Read-only, the content frame; advanced scenarios can call Navigate (including the parameterized overload)/GoBack/GoForward on it directly, or set `CacheSize` and `PageFactory`. |
| IsPaneOpen | bool | true | Whether the pane is expanded; when false it collapses to 48 pixels wide, showing icons only (group headers then take no space). |
| PaneWidth | int | 220 | Pane width in the expanded state (pixels). |
| SelectedItem | NavigationViewItem | null | The currently selected item; assigning is equivalent to `SelectItem(value, true)` (navigates). |

### Events

| Event | Description |
| --- | --- |
| SelectionChanged | Raised when the selection changes: clicking a menu/footer item, calling SelectItem, or setting SelectedItem. |
| Frame.Navigated | The content frame's navigation event (gives access to `NavigationEventArgs` with Mode/Parameter). |

Public method: `void SelectItem(NavigationViewItem item, bool navigate)` — selects the given
item; when navigate is true and the item's TargetPageType is non-null, the frame is navigated with
`item.Tag` as the navigation parameter (`_frame.Navigate(TargetPageType, Tag)`). No-op when item
is null, is already the selected item, or is a group header/separator.

### Example

Following the real usage in the Gallery main form, menu items are bound to page types through
`TargetPageType` and pass `Tag` as the navigation parameter:

```csharp
var navigation = new FluentNavigationView { Dock = DockStyle.Fill };
navigation.MenuItems.Add(new NavigationViewItemHeader("Browse"));
navigation.MenuItems.Add(new NavigationViewItem("Home", SymbolRegular.Home24, typeof(HomePage)));
navigation.MenuItems.Add(new NavigationViewItem("Buttons", SymbolRegular.Keyboard24, typeof(ButtonsPage)));
navigation.MenuItems.Add(new NavigationViewItem("Collections", SymbolRegular.List24, typeof(CollectionsPage)));
navigation.MenuItems.Add(new NavigationViewItemSeparator());
navigation.MenuItems.Add(new NavigationViewItemHeader("Details"));
navigation.MenuItems.Add(new NavigationViewItem("Order A-1001", SymbolRegular.Document24, typeof(DetailPage)) { Tag = "A-1001" });
navigation.FooterMenuItems.Add(new NavigationViewItem("Settings", SymbolRegular.Settings24, typeof(SettingsPage)));

navigation.SelectionChanged += (s, e) =>
    Console.WriteLine(navigation.SelectedItem?.Content);

Controls.Add(navigation);

// Initially select the first selectable item and navigate to HomePage
navigation.SelectItem(navigation.MenuItems[1], true);

// Only change the highlight, without navigating
navigation.SelectItem(navigation.MenuItems[2], false);

// Collapse/expand the pane
navigation.IsPaneOpen = false;
navigation.PaneWidth = 260;

// Use the content frame directly for parameterized navigation and back/forward
navigation.Frame.Navigate(typeof(DetailPage), "A-1002");
navigation.Frame.GoBack();
```

### Notes

- The constructor sets `Dock = DockStyle.Fill`; regular menu item rows are 38 pixels tall, group
  headers 34 (0 when collapsed), separators 16 (12 when collapsed), the hamburger button area is
  48 pixels, and the collapsed width is fixed at 48.
- Clicking the hamburger button toggles IsPaneOpen; footer items and main menu items share one
  selection state (there is only one SelectedItem at any moment).
- Pages are cached by "type + Tag parameter" inside the internal FluentFrame, with `CacheSize = 10`
  by default. Page classes must derive from Control; unless you set `navigation.Frame.PageFactory`
  they also need a public parameterless constructor (typically derive from FluentPage).

## FluentTabPage

Tab page of `FluentTabControl` (plain class, not a control).

### Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| Text | string | null | Display text of the tab header. |
| Content | Control | null | The page's content control; the TabControl handles its placement and show/hide. |
| Tag | object | null | Arbitrary attached data. |

Constructors: `FluentTabPage()`, `FluentTabPage(string text, Control content)`.

## FluentTabControl

Fluent tab control: a tab header strip on top, with the selected tab visually merged into the
content surface below (WinUI style).

### Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| TabPages | List&lt;FluentTabPage&gt; | Empty list | Read-only collection, the tab pages; prefer AddTab to add them. |
| SelectedIndex | int | -1 | Index of the currently selected tab; assignment is automatically clamped to [-1, Count-1] and raises SelectedIndexChanged. AddTab sets it to 0 when the first page is added. |

### Events

| Event | Description |
| --- | --- |
| SelectedIndexChanged | Raised when the selected tab changes: clicking a tab header or setting SelectedIndex in code. |

Public method: `FluentTabPage AddTab(string text, Control content)` — creates a new tab page
and adds the content control to this control; returns the page; the first tab is selected
automatically.

### Example

```csharp
var tabs = new FluentTabControl { Size = new Size(560, 220) };

var tab1 = new Panel();
tab1.Controls.Add(new FluentLabel { Text = "第一页内容", AutoSize = true, Location = new Point(16, 16) });
var tab2 = new Panel();
tab2.Controls.Add(new FluentButton
{
    Text = "第二页的按钮",
    Appearance = ControlAppearance.Primary,
    Location = new Point(16, 16),
    AutoSize = true
});

tabs.AddTab("Overview", tab1);
tabs.AddTab("Actions", tab2);
tabs.SelectedIndexChanged += (s, e) => Console.WriteLine(tabs.SelectedIndex);
tabs.SelectedIndex = 1;

Controls.Add(tabs);
```

### Notes

- Tab headers are 36 pixels tall, with width measured from the text; content controls are
  laid out uniformly below the header strip by the control itself, and AddTab resets the
  content's Dock to None, so you do not need to dock it yourself.
- During painting, the content control's BackColor is synchronized to the content surface
  color to stay compatible with system background compositing; tab headers do not support
  close buttons or drag-to-reorder.

## FluentBreadcrumbBar

Fluent breadcrumb navigation: path segments separated by chevrons, with the last segment bolded
to indicate the current location.

### Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| Items | List&lt;string&gt; | Empty list | Read-only collection, the text of each path segment; prefer SetPath to set the whole path at once. |

Public method: `void SetPath(params string[] items)` — clears and rebuilds the path.

### Events

| Event | Description |
| --- | --- |
| ItemClicked | EventHandler&lt;int&gt;, raised when a segment is clicked; the argument is that segment's index. |

### Example

```csharp
var crumbs = new FluentBreadcrumbBar { Width = 420 };
crumbs.SetPath("Home", "Documents", "Projects", "WinformUi");
var state = new FluentLabel { Text = "Click a segment", AutoSize = true };
crumbs.ItemClicked += (s, i) => state.Text = "Navigated to: " + crumbs.Items[i];

Controls.Add(crumbs);
Controls.Add(state);
```

### Notes

- Default size 300×28, `TabStop = false`; the last segment (current level) is bold and shows no
  hover background, but clicking it still raises ItemClicked.
- The control itself performs no navigation; handle it yourself by index in the event (e.g.
  truncate the path, switch pages).

## FluentStatusBar

Fluent status bar (derives from `Panel`): a bottom strip with a divider line on top and a
themed secondary background color.

### Properties

This class declares no new public properties; it inherits all Panel properties.
Constructor presets: `Dock = DockStyle.Bottom`, `Height = 28`,
`Padding = new Padding(8, 0, 8, 0)`, font set to Caption.

### Events

This class declares no new events; it inherits the standard Panel/Control events.

### Example

```csharp
var statusBar = new FluentStatusBar();
var statusLabel = new FluentLabel
{
    AutoSize = true,
    Dock = DockStyle.Left,
    TextAlign = ContentAlignment.MiddleLeft,
    Font = FluentTypography.Caption,
    Text = "就绪"
};
statusBar.Controls.Add(statusLabel);
Controls.Add(statusBar);
```

### Notes

- It only paints the background and the divider line itself; all content is carried by child
  controls (e.g. the Gallery main form uses a FluentLabel to show theme/background material
  state).

## FluentStatusStrip

Fluent status bar (derives from the native `StatusStrip`): keeps the full `ToolStripItem`
collection, layout, tooltip and interaction model, unifying only the background, top divider,
text and item colors. Suited to the classic desktop window footer of "ready / file count /
progress / menus".

### Choosing Between the Two

| Need | Recommended |
| --- | --- |
| Native items such as `ToolStripStatusLabel`, `ToolStripSplitButton`, `ToolStripDropDownButton`, `ToolStripProgressBar` | `FluentStatusStrip` |
| Arbitrary child controls, custom layouts, progress rings, buttons, complex interaction | `FluentStatusBar` |

Both dock to the bottom with a height of 28px by default, but their content models differ:
`FluentStatusStrip` content goes into `Items`, while `FluentStatusBar` content goes into
`Controls`.

### Quick Start

```csharp
using System;
using System.Drawing;
using System.Windows.Forms;
using WinformUi.Controls;

public sealed class StatusStripDemoForm : FluentWindow
{
    private readonly ToolStripStatusLabel _summary = new ToolStripStatusLabel("已加载 0 个文件")
    {
        Spring = true,
        TextAlign = ContentAlignment.MiddleRight,
        ToolTipText = "当前文件数量"
    };

    public StatusStripDemoForm()
    {
        Text = "StatusStrip demo";
        ClientSize = new Size(640, 360);

        var status = new FluentStatusStrip();
        var ready = new ToolStripStatusLabel("就绪")
        {
            ToolTipText = "当前状态"
        };
        ready.Click += (s, e) => Console.WriteLine("状态标签被点击");

        var progress = new ToolStripProgressBar
        {
            Minimum = 0,
            Maximum = 100,
            Value = 40,
            Width = 140,
            ToolTipText = "同步进度"
        };

        status.Items.AddRange(new ToolStripItem[] { ready, _summary, progress });
        status.ItemClicked += (s, e) => Console.WriteLine("点击项目: " + e.ClickedItem?.Text);
        Controls.Add(status);

        var load = new FluentButton
        {
            Text = "模拟加载",
            Location = new Point(24, 24),
            Size = new Size(100, 32),
            Appearance = ControlAppearance.Primary,
            Dock = DockStyle.Top
        };
        load.Click += (s, e) =>
        {
            ready.Text = "正在加载...";
            _summary.Text = "已加载 3 个文件";
            progress.Value = 80;
        };
        Controls.Add(load);
    }
}
```

### Constructor Defaults

This class adds no public properties, events or methods of its own; all the properties below
are inherited from `StatusStrip`.

| Inherited Property | Default | Description |
| --- | --- | --- |
| Dock | DockStyle.Bottom | Automatically occupies the bottom of the form; can use other docking modes if placed inside a panel. |
| Height | 28 | Default height, modifiable. |
| Font | FluentTypography.Caption | Small status-bar font. |
| GripStyle | ToolStripGripStyle.Hidden | Does not show the drag layout grip. |
| SizingGrip | false | Does not show the diagonal resize grip in the bottom-right corner. |
| ShowItemToolTips | true | Allows items to display ToolTipText. |
| Padding | `(4, 0, 4, 0)` | 4px left/right inner padding. |
| Renderer | Internal FluentStatusStripRenderer | Determines the themed painting of background, border and items. |

### Native Items

| Item Type | Purpose | Common Members/Events |
| --- | --- | --- |
| ToolStripStatusLabel | Status text, fixed width or spring to fill remaining space | `Text`、`ToolTipText`、`Spring`、`TextAlign`、`Click` |
| ToolStripProgressBar | Native determinate progress | `Minimum`、`Maximum`、`Value`、`Style` |
| ToolStripDropDownButton | Status actions with a dropdown menu | `DropDownItems`、`DropDownOpening`、`DropDownOpened`、`DropDownClosed` |
| ToolStripSplitButton | Primary action plus dropdown alternates | `ButtonClick`、`DropDownItems`、`DefaultItem` |
| ToolStripLabel | Plain label item | `Text`、`ToolTipText`、`Click` |

Content must be added through `status.Items`. Do not treat `FluentStatusStrip` like a regular
`Panel` and stuff child controls into its `Controls` collection.

### Drop-Down Buttons and Split Buttons

```csharp
var views = new ToolStripDropDownButton("视图");
views.DropDownItems.Add("小图标", null, (s, e) => Console.WriteLine("小图标"));
views.DropDownItems.Add("详细信息", null, (s, e) => Console.WriteLine("详细信息"));
views.DropDownOpening += (s, e) =>
{
    // Update the menu from the current data before each open
};
status.Items.Add(views);

var save = new ToolStripSplitButton
{
    Text = "保存",
    ToolTipText = "保存当前文档"
};
save.ButtonClick += (s, e) => Console.WriteLine("保存当前文档");
save.DropDownItems.Add("另存为", null, (s, e) => Console.WriteLine("另存为"));
status.Items.Add(save);
```

Both the main button of `ToolStripDropDownButton` and the left primary action of
`ToolStripSplitButton` can raise `ItemClicked`; `ToolStripSplitButton.ButtonClick` is the more
explicit primary-action event. If you subscribe to multiple events, branch by item type to
avoid executing business logic twice for a single operation.

### Inherited Events

| Event | Description |
| --- | --- |
| ItemClicked | Raised after an item is clicked; `e.ClickedItem` identifies the item. The specific item may also raise its own `Click`/`ButtonClick`. |
| ItemAdded | Raised after a new item is added. |
| ItemRemoved | Raised after an item is removed. |
| LayoutChanged / LayoutCompleted | Raised during item layout changes. |
| Paint | Raised when the status bar paints itself. |

This class adds no public methods; `Items.Add`, `Items.AddRange`, `Items.Clear`, `Items.Remove`
all use the native collection API.

### Using the Designer

1. Drop a `FluentStatusStrip` onto the form; it shows at the bottom by default.
2. Select the status bar, expand `Items` in the Properties window, and click the collection
   editor button.
3. In the collection editor, add items such as `ToolStripStatusLabel` and
   `ToolStripProgressBar`, and set their text, width, tooltips and Spring.
4. `StatusStrip.Items` does not provide a general hosting model for arbitrary `Control`s; when
   you need action buttons, prefer `ToolStripSplitButton` or `ToolStripDropDownButton`.

What the designer serializes is the native item collection. Items added at runtime via
`Items.Add` likewise receive the foreground and background colors of the current theme.

Clicks on the left primary button of a `ToolStripDropDownButton` and on the primary action
button of a `ToolStripSplitButton` enter the `ItemClicked` and `ButtonClick` paths
respectively; do not write "just open the menu" and "execute business logic" as the same
handler.

### Common Pitfalls

- **Theming only handles top-level items.** Items added to `Items` get BackColor/ForeColor;
  menu items inside `DropDownItems` are not recursively colored by this class, and the drop-down
  menus still use the native renderer system.
- **The progress bar is not FluentProgressBar.** `ToolStripProgressBar` keeps the native Windows
  progress painting and cannot be controlled through this library's `FluentProgressBar`
  properties.
- **Replacing the Renderer loses the Fluent look.** Once `Renderer` is reset in code or the
  designer, this class no longer guarantees the background, divider and item colors.
- **Do not mix Controls and Items.** If you really need to place custom controls, `FluentStatusBar`
  fits better with its Panel content model.
- **No extra event for status text updates.** This class does not proxy
  `ToolStripStatusLabel.TextChanged`; subscribe to the concrete item's own events when you need
  to listen.
- **Theme switches do not restore custom item colors.** `ApplyTheme` sets top-level item
  background and foreground to the status bar's current colors; item-specific colors must be
  reapplied by you after a theme switch.

## FluentSplitContainer

Fluent split container (derives from the native `SplitContainer`): keeps the two panels, the
draggable splitter, keyboard handling and designer support, painting a Fluent fill, a divider
line and a three-dot drag hint in the splitter area. Suited to adjustable layouts such as
left navigation + right content, or top code preview + bottom editor.

### Quick Start

```csharp
using System;
using System.Drawing;
using System.Windows.Forms;
using WinformUi.Controls;

public sealed class SplitContainerDemoForm : FluentWindow
{
    public SplitContainerDemoForm()
    {
        Text = "SplitContainer demo";
        ClientSize = new Size(720, 420);

        var split = new FluentSplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            Panel1MinSize = 140,
            Panel2MinSize = 180
        };

        split.Panel1.Controls.Add(new FluentLabel
        {
            Text = "导航区域",
            AutoSize = true,
            Location = new Point(16, 16)
        });
        split.Panel2.Controls.Add(new FluentLabel
        {
            Text = "内容区域",
            AutoSize = true,
            Location = new Point(16, 16)
        });

        split.SplitterMoving += (s, e) =>
        {
            if (split.Orientation == Orientation.Vertical && e.SplitX <= 100)
                e.Cancel = true;
            else if (split.Orientation == Orientation.Horizontal && e.SplitY <= 100)
                e.Cancel = true;
        };
        split.SplitterMoved += (s, e) =>
            Console.WriteLine("当前比例: " + split.SplitterRatio.ToString("P0"));

        Controls.Add(split);

        // Apply the target ratio only after the form finishes Dock/Padding layout.
        split.BeginInvoke(new Action(() => split.SetSplitterRatio(0.28)));
    }
}
```

`Dock = Fill` is set explicitly by the example. The control keeps the native `DockStyle.None`
on construction and does not force itself to fill its parent container.

### Constructor Defaults

| Property | Default | Description |
| --- | --- | --- |
| Dock | DockStyle.None | Same as the native SplitContainer; set Dock yourself when you need filling. |
| Size | 150×100 | Native default size; usually overridden in code. |
| Orientation | Orientation.Vertical | Two side-by-side panels. |
| SplitterWidth | 8 | Splitter bar width. |
| SplitterIncrement | 1 | Base step for keyboard movement. |
| SplitterDistance | 50 | Native initial distance; may be clamped after setting minimum sizes or changing the size. |
| Panel1MinSize | 25 | Minimum size of the first panel. |
| Panel2MinSize | 25 | Minimum size of the second panel. |
| Panel1.BackColor / Panel2.BackColor | Color.Transparent | Transparent backgrounds used at construction and on theme application. |

### New Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| SplitterRatio | double | Current actual ratio | The proportion of the first panel along the split direction relative to the whole container. Hidden in the read-only property grid and not serialized by the designer. |

For `SplitterRatio`, the numerator is the current `SplitterDistance` and the denominator is
`Orientation.Vertical ? Width : Height`, i.e. it **includes the splitter area**. It is not a
persisted field; each read computes it from the current size and distance.

Setting the ratio:

```csharp
split.SplitterRatio = 0.25;
// equivalent to
split.SetSplitterRatio(0.25);
```

`SetSplitterRatio` first computes the target range from `Panel1MinSize` and
`total - Panel2MinSize`, then rounds `total * ratio` to an integer, and finally hands it to the
native `SplitterDistance`. The native layout may additionally subtract `SplitterWidth` and clamp
again, so the value you read back may differ slightly from the requested one.

A `ratio` less than 0, greater than 1, or `NaN` throws `ArgumentOutOfRangeException`. When the
two minimum sizes together exceed the container's available length, the code's maximum may be
below its minimum; the current implementation clamps via the native distance property, so at
extremely small sizes you should check the actual layout instead of assuming the ratio holds.

### Orientation and Common Inherited Properties

| Value/Property | Description |
| --- | --- |
| Orientation.Vertical | Left/right split, distance uses Width, adjusted with Left/Right keys. |
| Orientation.Horizontal | Top/bottom split, distance uses Height, adjusted with Up/Down keys. |
| Panel1 / Panel2 | Two native Panels, holding the left/right or top/bottom content respectively. |
| SplitterDistance | Size of the first panel; the actual layout result includes the effect of its padding. |
| Panel1MinSize / Panel2MinSize | Minimum panel sizes used by dragging and ratio setting. |
| SplitterIncrement | Keyboard movement step; mouse dragging is not limited by it. |
| SplitterWidth | Splitter bar width; too small makes the three-dot hint hard to click. |
| FixedPanel | Fixes one panel so the other expands preferentially when the container is resized. |
| IsSplitterFixed | Prevents the user from dragging the splitter; code can still change the distance. |
| BorderStyle | Container border style; this class's themed painting does not replace it. |
| BackColor | Container background; panels default to transparent to show the parent or container background. |

### Inherited Events and Methods

| Member | Description |
| --- | --- |
| SplitterMoving | Raised while the splitter moves; can be canceled via `e.Cancel`. May fire continuously during a mouse drag. |
| SplitterMoved | Raised after the move completes; changing `SplitterDistance` in code may also raise it, but raising `SplitterMoving` first is not guaranteed. |
| SplitterDoubleClick | Raised on double-clicking the splitter; the default restore/adjust behavior is handled by the base class. |
| Panel1 / Panel2.Controls | Load content the ordinary Panel way. |
| SetSplitterRatio(double ratio) | Method added by this class; sets the ratio against the container's current total size and handles minimum sizes. |

### Keyboard and Mouse

- Drag the splitter with the mouse to resize the panels.
- Double-clicking the splitter raises the native `SplitterDoubleClick` and snap logic.
- Once the splitter has focus, `Left`/`Up` decrease `SplitterDistance` and `Right`/`Down`
  increase it; the horizontal orientation follows the same two key groups.
- During keyboard adjustment, `Escape` cancels the adjustment.
- Movement can be canceled in `SplitterMoving`; `SplitterMoved` signals the final commit.
- The splitter itself has no added buttons or smart tags.

### Ratios, Scaling and Persistence

- The ratio is not a designer-persisted value. After reopening the form, the designer-serialized
  `Size`, `Orientation`, `SplitterDistance` and minimum sizes are authoritative.
- After form DPI scaling, Dock, Padding, minimum sizes or `FixedPanel` change, both the pixel
  distance and the ratio can change.
- If you need a fixed 25% / 40% layout, re-invoke `SetSplitterRatio` in the form's `Load`, at
  the end of `OnLayout`, or in a first `BeginInvoke`.
- It is not recommended to read and persist the ratio in the constructor before the parent
  container has finished layout; what you read then is the initial size.
- Whether to re-apply your business ratio after user dragging is up to you. To forbid user
  adjustment, set `IsSplitterFixed = true`.

### Using the Designer

1. Drop a `FluentSplitContainer` onto the form and set `Location`, `Size`, `Orientation`.
2. Select Panel1 or Panel2 and add content via the `Controls` collection editor in the
   Properties window.
3. Set `Panel1MinSize`, `Panel2MinSize`, `SplitterWidth` and `FixedPanel`.
4. Drag the splitter to obtain the initial pixel distance; the native designer serializes that
   value.
5. `SplitterRatio` does not appear in the designer property grid; set it in code at runtime.

### Common Pitfalls

- **Do not forget to set Dock.** It is `DockStyle.None` on construction; setting `Dock = Fill`
  overrides the Location/Size effects in normal flow layout.
- **Do not manually add child controls to the splitter.** The splitter area is laid out by the
  base class; this class only paints the visual hint.
- **Minimum sizes can block the target ratio.** For example, at 300px width with
  Panel1MinSize 100px, 25% will not give you 75px — the first panel is at least 100px (and
  Panel2MinSize must still be satisfied).
- **Transparent panels are not an infinitely transparent window.** `Panel1` and `Panel2` use
  transparent backgrounds to composite the parent container's color; transparency is reapplied
  on theme switches and parent changes. Opaque custom panel backgrounds should be decided by
  your child content.
- **The ratio includes the splitter.** `SplitterRatio = 0.5` does not mean Panel1 and Panel2
  content widths are exactly equal, because the 8px splitter counts in the denominator.
- **Programmatic assignment and user dragging raise different events.** Do not assume
  `SetSplitterRatio` always triggers `SplitterMoving`; when you need the final result, read
  `SplitterRatio` after setting it, or listen to `SplitterMoved`.
- **Theming only paints the splitter area.** Fluent controls inside the two panels each
  subscribe to the theme themselves; native WinForms controls do not automatically gain a
  Fluent appearance.

## FluentCard

Fluent card (derives from `Panel`): a rounded layer surface with a thin border, hosting child
controls just like a Panel.

### Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| CornerRadius | float | 4f (FluentGraphics.ControlCornerRadius) | Card corner radius. |

Constructor presets: `Padding = new Padding(12)`, font set to Body. Derived classes can override
the protected `CardFill`/`CardStroke` colors and the `OnPaintCard(PaintEventArgs)` painting
hook.

### Events

This class declares no new events; it inherits the standard Panel/Control events.

### Example

```csharp
var card = new FluentCard { Size = new Size(280, 110), Padding = new Padding(16) };
card.Controls.Add(new FluentLabel
{
    Text = "卡片标题",
    AutoSize = true,
    UseSecondaryColor = false,
    Font = FluentTypography.Subtitle,
    Location = new Point(16, 12)
});
card.Controls.Add(new FluentLabel
{
    Text = "带边框和圆角的简单内容表面。",
    AutoSize = true,
    MaximumSize = new Size(232, 0),
    Location = new Point(16, 44)
});
Controls.Add(card);
```

### Notes

- During painting, its own BackColor is synchronized to the card fill color so that child
  controls (e.g. labels) resolve an ambient background consistent with the card surface.

## FluentCardAction

A clickable Fluent card (derives from `FluentCard`): with hover/pressed/disabled layer states,
feeling much like a button to interact with.

### Properties

This class declares no new public properties; it inherits FluentCard's CornerRadius and so on.
The constructor presets `Cursor = Cursors.Hand`.

### Events

This class declares no new events; use the inherited standard `Click` event to respond to
clicks.

### Example

```csharp
var actionCard = new FluentCardAction { Size = new Size(280, 110) };
actionCard.Controls.Add(new FluentLabel
{
    Text = "点我",
    AutoSize = true,
    Location = new Point(16, 16)
});
int clicks = 0;
var state = new FluentLabel { Text = "Clicks: 0", AutoSize = true };
actionCard.Click += (s, e) => state.Text = "Clicks: " + (++clicks);

Controls.Add(actionCard);
Controls.Add(state);
```

### Notes

- State fills: hover uses ControlFillSecondary, pressed uses ControlFillTertiary, disabled uses
  ControlFillDisabled; moving the mouse out clears the pressed state as well.

## FluentCardColor

A Fluent card with a colored accent bar (derives from `FluentCard`): a 4-pixel bar on the left
edge used as a category marker.

### Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| AccentColor | Color | Color.Empty | Bar color; when Empty, falls back to the theme's SystemAccentColorPrimary. |

### Events

This class declares no new events; it inherits the standard FluentCard/Panel events.

### Example

```csharp
var blueCard = new FluentCardColor
{
    Size = new Size(280, 90),
    AccentColor = Color.FromArgb(0x00, 0x78, 0xD4)
};
blueCard.Controls.Add(new FluentLabel { Text = "信息", AutoSize = true, Location = new Point(20, 16) });

var greenCard = new FluentCardColor
{
    Size = new Size(280, 90),
    AccentColor = Color.FromArgb(0x0F, 0x7B, 0x0F)
};
greenCard.Controls.Add(new FluentLabel { Text = "成功", AutoSize = true, Location = new Point(20, 16) });

Controls.Add(blueCard);
Controls.Add(greenCard);
```

### Notes

- The bar is painted on the card's left edge (4 pixels wide, with 1 pixel inset at top and
  bottom); a left content margin of at least 20 pixels is recommended to avoid it.

## FluentExpander

Fluent expander: a clickable header row (icon + title + chevron arrow) with a collapsible
content panel below; add child controls to `ContentPanel`.

### Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| ContentPanel | Panel | Built-in instance | Read-only, the host panel for the expandable content; initially hidden, Padding is (12, 8, 12, 12). |
| Header | string | "Header" | Header title text. |
| HeaderIcon | SymbolRegular | SymbolRegular.Empty | Header icon; nothing is drawn when set to Empty. |
| IsExpanded | bool | false | Whether expanded; on change it shows/hides the content panel, recomputes the height and raises ExpandedChanged. |

### Events

| Event | Description |
| --- | --- |
| ExpandedChanged | Raised when the expansion state changes: clicking the header or setting IsExpanded in code. |

### Example

```csharp
var expander = new FluentExpander
{
    Header = "Settings group",
    HeaderIcon = SymbolRegular.Settings24,
    Width = 420
};
expander.ContentPanel.Padding = new Padding(12);
expander.ContentPanel.Controls.Add(new FluentToggleSwitch
{
    OnContent = "Enable feature",
    OffContent = "Enable feature",
    Checked = true,
    Width = 180,
    Location = new Point(12, 8)
});
expander.ContentPanel.Controls.Add(new FluentCheckBox
{
    Text = "Show advanced options",
    Location = new Point(12, 44)
});
expander.ExpandedChanged += (s, e) => Console.WriteLine(expander.IsExpanded);

Controls.Add(expander);
```

### Notes

- Default size 300×48 (header only); the header height is fixed at 48 pixels, and clicking
  anywhere on the header toggles expansion.
- When expanded, the control's height automatically adjusts to "header + content height"; the
  content height is the maximum of the child controls' Bottom plus bottom padding, so child
  controls must specify their own Location/size.
- When disabled (Enabled = false) the header does not respond to clicks, and text and icon use
  the disabled colors.

## FluentCardExpander

A card-style expander (derives from `FluentExpander`), semantically indicating that the header
and content share one card surface.

### Properties

The current implementation adds no properties at all; it is identical to FluentExpander
(ContentPanel, Header, HeaderIcon, IsExpanded).

### Events

Inherits FluentExpander's ExpandedChanged.

### Example

```csharp
var cardExpander = new FluentCardExpander
{
    Header = "Card expander",
    HeaderIcon = SymbolRegular.Info24,
    Width = 420
};
cardExpander.ContentPanel.Padding = new Padding(12);
cardExpander.ContentPanel.Controls.Add(new FluentLabel
{
    Text = "头部和内容共享一个卡片表面。",
    AutoSize = true,
    MaximumSize = new Size(390, 0),
    Location = new Point(12, 8)
});
Controls.Add(cardExpander);
```

### Notes

- In the current version this is an empty derived class of FluentExpander with identical
  appearance and behavior, used mainly as a semantic distinction and a future extension point.

## FluentScrollViewer

Fluent scroll view (derives from `Panel`): hosts a single content control and paints a thin
vertical scrollbar that widens on hover/drag.

### Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| Content | Control | null | The control being scrolled; setting it replaces the old content and resets scrolling. You must set the content's Height to the desired total height yourself. |
| ScrollOffset | int | 0 | Current vertical scroll offset (pixels); assignment is automatically clamped to [0, content height − viewport height]. |

### Events

This class declares no new events; it inherits the standard Panel/Control events.

### Example

Following the real usage in the Gallery page base class, the content is an auto-sizing vertical
FlowLayoutPanel:

```csharp
var scroll = new FluentScrollViewer { Dock = DockStyle.Fill };
var root = new FlowLayoutPanel
{
    FlowDirection = FlowDirection.TopDown,
    WrapContents = false,
    AutoSize = true
};
for (int i = 0; i < 40; i++)
{
    root.Controls.Add(new FluentLabel { Text = "Item " + i, AutoSize = true });
}
scroll.Content = root;
Controls.Add(scroll);

// Scroll programmatically
scroll.ScrollOffset = 200;
```

### Notes

- Only vertical scrolling is supported; the content width is automatically set to the viewport
  width (reserving 14 pixels on the right when a scrollbar is needed), and the height determines
  the scrollable range.
- The scrollbar is 6 pixels wide normally, 10 pixels when hovered or dragged; it supports the
  mouse wheel, dragging the thumb, and clicking the track to page (one viewport height per
  click).
- The control's background is deliberately opaque to avoid artifacts from simulated transparency
  affecting sibling controls; the background refreshes automatically on theme switches and when
  the parent container changes.
