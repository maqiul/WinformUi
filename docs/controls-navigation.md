# 导航与容器控件参考

本文档覆盖 WinformUi 中导航与容器相关的控件:`FluentNavigationView`(含 `NavigationViewItem`、`FluentPage`、`FluentFrame`)、`FluentTabControl`/`FluentTabPage`、`FluentBreadcrumbBar`、`FluentStatusBar`/`FluentStatusStrip`、`FluentSplitContainer`、`FluentCard`/`FluentCardAction`/`FluentCardColor`、`FluentExpander`/`FluentCardExpander`、`FluentScrollViewer`。全部位于命名空间 `WinformUi.Controls`,随 `ApplicationThemeManager` 的主题自动换肤。属性、事件与默认值均以源码为准,示例片段可直接放入窗体代码中(需自行引入 `System`、`System.Drawing`、`System.Windows.Forms`、`WinformUi.Controls`、`WinformUi.Theming` 等命名空间)。

## FluentPage

可导航页面的基类(继承 `UserControl`,实现 `INavigationAware`):不透明层状表面、DPI 自动缩放,主题切换时自动更新背景色。

### 属性

| 属性 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| NavigationCacheMode | NavigationCacheMode | Enabled | 本页面在宿主 `FluentFrame` 中的缓存策略,详见下文枚举说明。 |

其余继承 UserControl 的全部属性。构造函数预设:`Padding = new Padding(24, 16, 24, 24)`、`AutoScaleMode = AutoScaleMode.Dpi`、背景为主题的 Layer 表面色。受保护属性 `SurfaceColor`(Color)可供派生类取当前层表面色。

### 事件

本类未声明新事件,继承标准 Control/UserControl 事件。页面生命周期通过重写虚方法获得:

| 受保护虚方法 | 说明 |
| --- | --- |
| `virtual void OnNavigatedTo(NavigationEventArgs e)` | 页面成为当前页之后调用(含前进/后退/刷新回到本页)。 |
| `virtual void OnNavigatedFrom(NavigationEventArgs e)` | 页面即将不再是当前页之前调用。 |

`INavigationAware` 在 `FluentPage` 上是显式实现,派生类只需重写 `protected virtual` 版本;若要给非 `FluentPage` 的任意 `Control` 加生命周期,自行实现 `INavigationAware` 接口即可。

### NavigationCacheMode

| 值 | 说明 |
| --- | --- |
| Disabled | 不缓存。离开该页时页面实例被 `Dispose`,再次导航进入会重新构造。 |
| Enabled | 受 `FluentFrame.CacheSize` 上限约束的缓存(默认)。超出上限时按最久未使用淘汰。 |
| Required | 永久缓存,不计入 `CacheSize`,也不会被淘汰;适合承载昂贵状态的页。 |

### NavigationMode 与 NavigationEventArgs

`NavigationMode` 标识本次导航到达方式:`New`(全新导航)、`Back`(后退)、`Forward`(前进)、`Refresh`(对同一"类型+参数"重复导航,仅复位历史)。

`NavigationEventArgs` 携带 `PageType`(Type)、`Parameter`(object)、`Mode`(NavigationMode)、`CacheMode`(NavigationCacheMode)。

### 示例

```csharp
public class DetailPage : FluentPage
{
    private readonly FluentLabel _title = new FluentLabel { AutoSize = true };

    public DetailPage()
    {
        // 参数相同的实例复用,不同订单各自独立缓存
        NavigationCacheMode = NavigationCacheMode.Enabled;
        Controls.Add(_title);
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        _title.Text = "订单 " + e.Parameter;
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        // 离开前暂停动画、提交编辑
    }
}
```

### 备注

- 页面背景刻意保持不透明,避免透明背景在真实窗口句柄上引发递归父级绘制导致的残影;请勿将其 BackColor 改为 Transparent。
- 主题切换时页面底色会推迟到本轮通知派发完成后再计算:页面比宿主窗体更早收到主题事件,
  当场混合会把新令牌叠在窗体的旧背景上并固化下来(深色下表现为偏亮的灰底)。


## FluentFrame

带历史记录与参数缓存的导航宿主(继承 `Panel`):按"页面类型 + 导航参数"缓存实例并停靠显示,内容区绘制左上圆角的 Fluent 层表面。

### 属性

| 属性 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| CurrentPage | Control | null | 只读,当前显示的页面实例。 |
| CurrentPageType | Type | null | 只读,当前页面的类型。 |
| CurrentParameter | object | null | 只读,到达当前页时使用的导航参数。 |
| CurrentCacheMode | NavigationCacheMode | Enabled | 只读,当前页生效的缓存模式(非 `FluentPage` 一律按 Enabled)。 |
| CanGoBack | bool | false | 只读,历史栈中是否可后退。 |
| CanGoForward | bool | false | 只读,历史栈中是否可前进。 |
| CacheSize | int | 10 | 保留的 Enabled 页面数量上限;设为 0 等同于全部不缓存。赋负值抛 `ArgumentOutOfRangeException`。`Required` 页面不计入。 |
| PageFactory | Func&lt;Type, object, Control&gt; | null | 可选的页面工厂,替代无参构造。参数为页面类型与导航参数;返回 null 抛 `InvalidOperationException`。 |

### 事件

| 事件 | 说明 |
| --- | --- |
| Navigated | 每次导航完成后触发,包括 Navigate、GoBack、GoForward 与同页刷新。事件参数为 `NavigationEventArgs`(在 `FluentPage`/`FluentFrame` 一节说明)。 |

公开方法:

| 方法 | 说明 |
| --- | --- |
| Control Navigate(Type pageType) | 以空参数导航到指定页面类型(必须是 Control 类型),返回页面实例。 |
| Control Navigate(Type pageType, object parameter) | 带参数导航。`parameter` 参与缓存键与历史记录,同一类型不同参数会得到不同实例;`e.Parameter` 在 `OnNavigatedTo` 中可读回。 |
| T Navigate&lt;T&gt;() where T : Control | 泛型版导航,返回强类型页面。 |
| T Navigate&lt;T&gt;(object parameter) where T : Control | 泛型带参导航。 |
| void GoBack() | 后退到历史中的上一页(CanGoBack 为 false 时无操作),`Mode` 为 Back。 |
| void GoForward() | 前进到历史中的下一页(CanGoForward 为 false 时无操作),`Mode` 为 Forward。 |

### 示例

```csharp
var frame = new FluentFrame { Dock = DockStyle.Fill, CacheSize = 6 };

// 没有无参构造的页面交给工厂创建
frame.PageFactory = (type, parameter) =>
    type == typeof(DetailPage) ? new DetailPage((string)parameter) : (Control)Activator.CreateInstance(type);

frame.Navigated += (s, e) =>
{
    var args = (NavigationEventArgs)e;
    Console.WriteLine(args.PageType.Name + " [" + args.Mode + "] " + args.Parameter);
};

frame.Navigate<HomePage>();
frame.Navigate(typeof(DetailPage), "A-1001");
frame.Navigate(typeof(DetailPage), "A-1002");   // 独立实例,互不覆盖
if (frame.CanGoBack) frame.GoBack();
if (frame.CanGoForward) frame.GoForward();

Controls.Add(frame);
```

### 备注

- 缓存键与历史条目都是 `页面类型 + 参数`,因此"同一类型不同参数"不会被误当作同一页复用。
- 历史中的同一条目重复导航不会新增记录;当前页与目标"类型+参数"完全一致时只触发一次 `Mode = Refresh` 的生命周期回调。
- `PageFactory` 为空时要求页面类型提供公共无参构造;有工厂时由工厂负责处理参数。
- 在同一位置导航到新页面会截断其后的前进历史,与浏览器行为一致。
- 离开页面时,`Disabled` 页(以及 `CacheSize = 0` 时的 `Enabled` 页)会被立即 `Dispose`;`Enabled` 页超出 `CacheSize` 时按最久未使用淘汰,`Required` 页永不淘汰。
- 调用方自己 `Dispose` 掉 `Navigate` 返回的页面是允许的:缓存条目会在下次导航时发现对象已释放并自动重建一个新实例。

## NavigationViewItem

`FluentNavigationView` 的菜单项(普通类,非控件)。

### 属性

| 属性 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| Content | string | null | 菜单项显示文本。 |
| Icon | SymbolRegular | SymbolRegular.Empty | 菜单图标;为 Empty 时不绘制。 |
| TargetPageType | Type | null | 点击后要导航到的页面类型;为 null 时只选中不导航。 |
| Tag | object | null | 作为导航参数传给 `FluentFrame.Navigate(TargetPageType, Tag)`,同时可用于附加数据。 |

构造函数:`NavigationViewItem()`、`NavigationViewItem(string content, SymbolRegular icon, Type targetPageType)`。

### NavigationViewItemHeader 与 NavigationViewItemSeparator

两者都继承 `NavigationViewItem`,用于把菜单分组,但不可选中、不可导航、不参与命中测试:

| 类型 | 构造 | 展开侧栏 | 折叠侧栏 |
| --- | --- | --- | --- |
| `NavigationViewItemHeader` | `()`、`(string text)` | 34 像素小标题,只显示 `Content`(图标与 TargetPageType 被忽略) | 高度 0,不占位 |
| `NavigationViewItemSeparator` | `()` | 16 像素高的分隔线(`DividerStrokeDefault`) | 12 像素 |

```csharp
navigation.MenuItems.Add(new NavigationViewItemHeader("组件"));
navigation.MenuItems.Add(new NavigationViewItem("Buttons", SymbolRegular.Keyboard24, typeof(ButtonsPage)));
navigation.MenuItems.Add(new NavigationViewItem("Collections", SymbolRegular.List24, typeof(CollectionsPage)));
navigation.MenuItems.Add(new NavigationViewItemSeparator());
navigation.MenuItems.Add(new NavigationViewItemHeader("应用"));
navigation.MenuItems.Add(new NavigationViewItem("Settings", SymbolRegular.Settings24, typeof(SettingsPage)));
navigation.FooterMenuItems.Add(new NavigationViewItem("About", SymbolRegular.Info24, typeof(AboutPage)));
```

`SelectItem` 与鼠标点击都会自动跳过这两类项,`SelectedItem` 永远不会是它们。

### 带参数导航

`Tag` 即导航参数,因此同一个页面类型可以注册多个入口:

```csharp
navigation.MenuItems.Add(new NavigationViewItem("订单 1001", SymbolRegular.Document24, typeof(DetailPage)) { Tag = "A-1001" });
navigation.MenuItems.Add(new NavigationViewItem("订单 1002", SymbolRegular.Document24, typeof(DetailPage)) { Tag = "A-1002" });
// 点击后 DetailPage.OnNavigatedTo 收到 e.Parameter == "A-1001" / "A-1002"
```


## FluentNavigationView

Fluent 导航视图:左侧图标菜单栏、选中项强调色竖条、汉堡按钮折叠为紧凑栏,右侧为层状内容框架(`FluentFrame`)。

### 属性

| 属性 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| MenuItems | List&lt;NavigationViewItem&gt; | 空列表 | 只读集合,主菜单项(汉堡按钮下方从上排列);直接 Add 填充。可混入 `NavigationViewItemHeader`/`NavigationViewItemSeparator` 分组。 |
| FooterMenuItems | List&lt;NavigationViewItem&gt; | 空列表 | 只读集合,底部菜单项(贴底排列,上方有分隔线),常放"设置"。同样支持分组标题与分隔线。 |
| Frame | FluentFrame | 内置实例 | 只读,内容框架;高级场景可直接对它 Navigate(含带参)/GoBack/GoForward,也可设置 `CacheSize`、`PageFactory`。 |
| IsPaneOpen | bool | true | 侧栏是否展开;false 时折叠为 48 像素宽、只显示图标(分组标题此时不占位)。 |
| PaneWidth | int | 220 | 展开状态的侧栏宽度(像素)。 |
| SelectedItem | NavigationViewItem | null | 当前选中项;赋值等价于 `SelectItem(value, true)`(会导航)。 |

### 事件

| 事件 | 说明 |
| --- | --- |
| SelectionChanged | 选中项变化时触发:点击菜单/底部项、调用 SelectItem 或设置 SelectedItem。 |
| Frame.Navigated | 内容框架的导航事件(可拿到 `NavigationEventArgs` 的 Mode/Parameter)。 |

公开方法:`void SelectItem(NavigationViewItem item, bool navigate)` —— 选中指定项;navigate 为 true 且该项 TargetPageType 非空时,以 `item.Tag` 作为导航参数调用 `_frame.Navigate(TargetPageType, Tag)`。item 为 null、与当前选中项相同、或是分组标题/分隔线时无操作。

### 示例

参照 Gallery 主窗体的真实用法,菜单项通过 `TargetPageType` 与页面类型绑定,`Tag` 作为导航参数:

```csharp
var navigation = new FluentNavigationView { Dock = DockStyle.Fill };
navigation.MenuItems.Add(new NavigationViewItemHeader("浏览"));
navigation.MenuItems.Add(new NavigationViewItem("Home", SymbolRegular.Home24, typeof(HomePage)));
navigation.MenuItems.Add(new NavigationViewItem("Buttons", SymbolRegular.Keyboard24, typeof(ButtonsPage)));
navigation.MenuItems.Add(new NavigationViewItem("Collections", SymbolRegular.List24, typeof(CollectionsPage)));
navigation.MenuItems.Add(new NavigationViewItemSeparator());
navigation.MenuItems.Add(new NavigationViewItemHeader("详情"));
navigation.MenuItems.Add(new NavigationViewItem("订单 A-1001", SymbolRegular.Document24, typeof(DetailPage)) { Tag = "A-1001" });
navigation.FooterMenuItems.Add(new NavigationViewItem("Settings", SymbolRegular.Settings24, typeof(SettingsPage)));

navigation.SelectionChanged += (s, e) =>
    Console.WriteLine(navigation.SelectedItem?.Content);

Controls.Add(navigation);

// 初始选中第一个真正可选项并导航到 HomePage
navigation.SelectItem(navigation.MenuItems[1], true);

// 只切换高亮、不导航
navigation.SelectItem(navigation.MenuItems[2], false);

// 折叠/展开侧栏
navigation.IsPaneOpen = false;
navigation.PaneWidth = 260;

// 直接使用内容框架做带参导航与前进后退
navigation.Frame.Navigate(typeof(DetailPage), "A-1002");
navigation.Frame.GoBack();
```

### 备注

- 构造函数中 `Dock = DockStyle.Fill`;普通菜单项行高 38 像素,分组标题 34 像素(折叠时 0)、分隔线 16 像素(折叠时 12),汉堡按钮区 48 像素,折叠宽度固定 48。
- 点击汉堡按钮切换 IsPaneOpen;底部项与主菜单项共用同一选中态(同一时刻只有一个 SelectedItem)。
- 页面由内部 FluentFrame 按"类型 + Tag 参数"缓存,默认 `CacheSize = 10`。页面类需为 Control 派生:未设置 `navigation.Frame.PageFactory` 时必须具有公共无参构造(通常继承 FluentPage)。


## FluentTabPage

`FluentTabControl` 的标签页(普通类,非控件)。

### 属性

| 属性 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| Text | string | null | 标签头显示文本。 |
| Content | Control | null | 页内容控件,由 TabControl 负责定位与显示/隐藏。 |
| Tag | object | null | 任意附加数据。 |

构造函数:`FluentTabPage()`、`FluentTabPage(string text, Control content)`。

## FluentTabControl

Fluent 标签控件:顶部标签头条,选中标签与下方内容表面连成一体(WinUI 风格)。

### 属性

| 属性 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| TabPages | List&lt;FluentTabPage&gt; | 空列表 | 只读集合,标签页;推荐用 AddTab 添加。 |
| SelectedIndex | int | -1 | 当前选中标签索引;赋值自动收敛到 [-1, Count-1],触发 SelectedIndexChanged。AddTab 添加第一页时自动置 0。 |

### 事件

| 事件 | 说明 |
| --- | --- |
| SelectedIndexChanged | 选中标签变化时触发:点击标签头或代码设置 SelectedIndex。 |

公开方法:`FluentTabPage AddTab(string text, Control content)` —— 新建标签页并把内容控件加入本控件,返回该页;首个标签自动选中。

### 示例

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

### 备注

- 标签头高 36 像素,宽度按文本实测;内容控件由控件统一布局到标签头下方,AddTab 会把内容的 Dock 重置为 None,无需自行停靠。
- 绘制时内容控件的 BackColor 会被同步为内容表面色,以兼容系统背景合成;标签头不支持关闭按钮或拖拽排序。

## FluentBreadcrumbBar

Fluent 面包屑导航:以箭头(chevron)分隔的路径项,末项加粗表示当前位置。

### 属性

| 属性 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| Items | List&lt;string&gt; | 空列表 | 只读集合,路径各段文本;推荐用 SetPath 一次性设置。 |

公开方法:`void SetPath(params string[] items)` —— 清空并重建路径。

### 事件

| 事件 | 说明 |
| --- | --- |
| ItemClicked | EventHandler&lt;int&gt;,点击某段时触发,参数为该段索引。 |

### 示例

```csharp
var crumbs = new FluentBreadcrumbBar { Width = 420 };
crumbs.SetPath("Home", "Documents", "Projects", "WinformUi");
var state = new FluentLabel { Text = "Click a segment", AutoSize = true };
crumbs.ItemClicked += (s, i) => state.Text = "Navigated to: " + crumbs.Items[i];

Controls.Add(crumbs);
Controls.Add(state);
```

### 备注

- 默认尺寸 300×28,`TabStop = false`;末项(当前层)加粗且不显示悬停底色,但点击仍会触发 ItemClicked。
- 控件本身不执行导航,需按索引在事件里自行处理(如截断路径、切换页面)。

## FluentStatusBar

Fluent 状态栏(继承 `Panel`):底部条带,顶部分隔线,主题化的次要背景色。

### 属性

本类未声明新的公开属性,继承 Panel 的全部属性。构造函数预设:`Dock = DockStyle.Bottom`、`Height = 28`、`Padding = new Padding(8, 0, 8, 0)`、字体为 Caption。

### 事件

本类未声明新事件,继承标准 Panel/Control 事件。

### 示例

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

### 备注

- 自身只绘制背景与分隔线,内容完全由子控件承载(如 Gallery 主窗体用 FluentLabel 显示主题/背景材质状态)。

## FluentStatusStrip

Fluent 状态栏(继承原生 `StatusStrip`):保留完整的 `ToolStripItem` 集合、布局、提示与交互模型,只统一背景、顶部分隔线、文字和项目配色。适合“就绪 / 文件数 / 进度 / 菜单”这类经典桌面窗口底栏。

### 与 FluentStatusBar 的选择

| 需求 | 推荐 |
| --- | --- |
| `ToolStripStatusLabel`、`ToolStripSplitButton`、`ToolStripDropDownButton`、`ToolStripProgressBar` 等原生项目 | `FluentStatusStrip` |
| 任意子控件、自定义布局、进度环、按钮、复杂交互 | `FluentStatusBar` |

两者都默认停靠底部、高 28px,但内容模型不同:`FluentStatusStrip` 的内容放进 `Items`,`FluentStatusBar` 的内容放进 `Controls`。

### 快速开始

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

### 构造默认值

本类没有新增自己的公开属性、事件或方法,以下属性全部继承自 `StatusStrip`。

| 继承属性 | 默认值 | 说明 |
| --- | --- | --- |
| Dock | DockStyle.Bottom | 自动占用窗体底部;如需放在面板中也可改为其他停靠方式。 |
| Height | 28 | 默认高度,可修改。 |
| Font | FluentTypography.Caption | 状态栏小号字体。 |
| GripStyle | ToolStripGripStyle.Hidden | 不显示拖动布局手柄。 |
| SizingGrip | false | 不显示右下角窗口缩放斜纹。 |
| ShowItemToolTips | true | 允许项目显示 ToolTipText。 |
| Padding | `(4, 0, 4, 0)` | 左右内边距 4px。 |
| Renderer | 内部 FluentStatusStripRenderer | 决定背景、边框和项目的主题绘制。 |

### 原生项目

| 项目类型 | 用途 | 常用成员/事件 |
| --- | --- | --- |
| ToolStripStatusLabel | 状态文本、固定宽度或弹性占据剩余空间 | `Text`、`ToolTipText`、`Spring`、`TextAlign`、`Click` |
| ToolStripProgressBar | 原生确定进度 | `Minimum`、`Maximum`、`Value`、`Style` |
| ToolStripDropDownButton | 带下拉菜单的状态操作 | `DropDownItems`、`DropDownOpening`、`DropDownOpened`、`DropDownClosed` |
| ToolStripSplitButton | 主操作与下拉备选操作 | `ButtonClick`、`DropDownItems`、`DefaultItem` |
| ToolStripLabel | 普通标签项目 | `Text`、`ToolTipText`、`Click` |

内容应通过 `status.Items` 添加。不要把 `FluentStatusStrip` 当作普通 `Panel`,往它的 `Controls` 集合塞子控件。

### 下拉菜单与拆分按钮

```csharp
var views = new ToolStripDropDownButton("视图");
views.DropDownItems.Add("小图标", null, (s, e) => Console.WriteLine("小图标"));
views.DropDownItems.Add("详细信息", null, (s, e) => Console.WriteLine("详细信息"));
views.DropDownOpening += (s, e) =>
{
    // 每次打开前按当前数据更新菜单
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

`ToolStripDropDownButton` 的主按钮和 `ToolStripSplitButton` 的左侧主操作都可能触发 `ItemClicked`;`ToolStripSplitButton.ButtonClick` 是更明确的主操作事件。如果同时订阅多个事件,请按项目类型分支,避免一次操作重复执行业务。

### 继承事件

| 事件 | 说明 |
| --- | --- |
| ItemClicked | 项目被点击后触发,`e.ClickedItem` 给出项目。具体项目还可能触发自己的 `Click`/`ButtonClick`。 |
| ItemAdded | 新项目加入后触发。 |
| ItemRemoved | 项目移除后触发。 |
| LayoutChanged / LayoutCompleted | 项目布局变化过程中触发。 |
| Paint | 状态栏自身绘制时触发。 |

本类没有新增公开方法,`Items.Add`、`Items.AddRange`、`Items.Clear`、`Items.Remove` 都使用原生集合 API。

### 设计器使用

1. 把 `FluentStatusStrip` 拖到窗体上,默认显示在底部。
2. 选中状态栏,在“属性”窗口展开 `Items`,点击集合编辑器按钮。
3. 在集合编辑器中添加 `ToolStripStatusLabel`、`ToolStripProgressBar` 等项目,设置文本、宽度、提示和 Spring。
4. `StatusStrip.Items` 不提供任意 `Control` 的通用承载模型;需要操作按钮时优先选择 `ToolStripSplitButton` 或 `ToolStripDropDownButton`。

设计器序列化的是原生项目集合。运行时再通过 `Items.Add` 添加的项目同样会获得当前主题的前景色和背景色。

`ToolStripDropDownButton` 左侧主按钮的点击与 `ToolStripSplitButton` 主操作按钮的点击会分别进入 `ItemClicked` 和 `ButtonClick` 路径;不要把“只打开菜单”和“执行业务”写成同一处理器。

### 常见坑

- **主题只自动处理顶层项目。** 加入 `Items` 的项会获得 BackColor/ForeColor;`DropDownItems` 中的菜单项不会由本类递归逐项着色,下拉菜单仍使用原生渲染器体系。
- **进度条不是 FluentProgressBar。** `ToolStripProgressBar` 保留 Windows 原生进度绘制,不能通过设置本库 `FluentProgressBar` 的属性来控制它。
- **替换 Renderer 会丢失 Fluent 外观。** 代码或设计器中重新设置 `Renderer` 后,本类不再保证背景、分隔线和项目配色。
- **不要混用 Controls 与 Items。** 如果确实要放自定义控件,使用 `FluentStatusBar` 更符合其 Panel 内容模型。
- **状态文本更新没有额外事件。** 本类不代理 `ToolStripStatusLabel.TextChanged`;需要监听时直接订阅具体项目的事件。
- **主题切换不恢复自定义项目颜色。** `ApplyTheme` 会把顶层项目背景和前景设为状态栏当前颜色;项目级特殊配色需要在主题切换后自行重新应用。

## FluentSplitContainer

Fluent 分隔容器(继承原生 `SplitContainer`):保留两个面板、可拖动分隔条、键盘操作与设计器功能,在分隔区域绘制 Fluent 填充、分隔线和三点拖动提示。适合左侧导航 + 右侧内容、上下代码预览 + 编辑区等可调整布局。

### 快速开始

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

        // 等窗体完成 Dock/Padding 布局后再应用目标比例。
        split.BeginInvoke(new Action(() => split.SetSplitterRatio(0.28)));
    }
}
```

`Dock = Fill` 由示例显式设置。控件构造时保留原生 `DockStyle.None`,不会强制铺满父容器。

### 构造默认值

| 属性 | 默认值 | 说明 |
| --- | --- | --- |
| Dock | DockStyle.None | 与原生 SplitContainer 一致;需要填充时自行设置 Dock。 |
| Size | 150×100 | 原生默认尺寸;代码中通常会覆盖。 |
| Orientation | Orientation.Vertical | 左右两个面板。 |
| SplitterWidth | 8 | 分隔条宽度。 |
| SplitterIncrement | 1 | 键盘移动的基本步长。 |
| SplitterDistance | 50 | 原生初始距离;设置最小尺寸或改变尺寸后可能被钳制。 |
| Panel1MinSize | 25 | 第一面板最小尺寸。 |
| Panel2MinSize | 25 | 第二面板最小尺寸。 |
| Panel1.BackColor / Panel2.BackColor | Color.Transparent | 构造及主题应用时使用透明背景。 |

### 新增属性

| 属性 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| SplitterRatio | double | 当前实际比例 | 第一面板占整个容器在分隔方向上的比例。只读属性网格中隐藏,不在设计器中序列化。 |

`SplitterRatio` 的分子是当前 `SplitterDistance`,分母是 `Orientation.Vertical ? Width : Height`,即**包含分隔条空间**。它不是持久化字段,每次读取都从当前尺寸和距离计算。

设置比例:

```csharp
split.SplitterRatio = 0.25;
// 等价于
split.SetSplitterRatio(0.25);
```

`SetSplitterRatio` 先按 `Panel1MinSize` 和 `total - Panel2MinSize` 计算目标范围,再把 `total * ratio` 四舍五入为整数,最后交给原生 `SplitterDistance`。原生布局还可能扣除 `SplitterWidth` 并再次钳制,因此实际读取值可能与请求值有少量差异。

`ratio` 小于 0、大于 1 或为 `NaN` 时抛出 `ArgumentOutOfRangeException`。当两个最小尺寸加起来超过容器可用长度时,代码的最大值可能小于最小值;当前实现按原生距离属性钳制,极端小尺寸下应检查实际布局而不是假设比例一定成立。

### 方向与常用继承属性

| 值/属性 | 说明 |
| --- | --- |
| Orientation.Vertical | 左右分隔,距离使用 Width,键盘 Left/Right 调整。 |
| Orientation.Horizontal | 上下分隔,距离使用 Height,键盘 Up/Down 调整。 |
| Panel1 / Panel2 | 两个原生 Panel,分别装载左右或上下内容。 |
| SplitterDistance | 第一面板尺寸,包含其内边距影响后的实际布局结果。 |
| Panel1MinSize / Panel2MinSize | 拖动与比例设置使用的最小面板尺寸。 |
| SplitterIncrement | 键盘移动步长;鼠标拖动不受它限制。 |
| SplitterWidth | 分隔条宽度;过小会使三点提示难以点击。 |
| FixedPanel | 固定某一面板,改变容器尺寸时优先扩展另一面板。 |
| IsSplitterFixed | 禁止用户拖动分隔条;代码仍可修改距离。 |
| BorderStyle | 容器边框样式;本类主题绘制不替代该属性。 |
| BackColor | 容器背景;面板默认透明,用于显示父容器或容器背景。 |

### 继承事件与方法

| 成员 | 说明 |
| --- | --- |
| SplitterMoving | 分隔条移动过程中触发,可通过 `e.Cancel` 取消。鼠标拖动期间可能连续触发。 |
| SplitterMoved | 移动完成后触发;代码修改 `SplitterDistance` 也可能触发,但不保证触发 `SplitterMoving`。 |
| SplitterDoubleClick | 双击分隔条时触发,默认恢复/调整行为由基类处理。 |
| Panel1 / Panel2.Controls | 使用普通 Panel 方式装载内容。 |
| SetSplitterRatio(double ratio) | 本类新增方法,按容器当前总尺寸设置比例并处理最小尺寸。 |

### 键盘与鼠标

- 鼠标拖动分隔条调整面板大小。
- 双击分隔条触发原生 `SplitterDoubleClick` 和归位逻辑。
- 让分隔条获得焦点后,`Left`/`Up` 减小 `SplitterDistance`,`Right`/`Down` 增大它;水平方向同样遵循这两个键组。
- 键盘调整期间,`Escape` 取消本次调整。
- `SplitterMoving` 中可取消移动,`SplitterMoved` 表示最终提交。
- 分隔条本身没有新增按钮或智能标记。

### 比例、缩放与持久化

- 比例不是设计器持久化值。重新打开窗体后,以设计器序列化的 `Size`、`Orientation`、`SplitterDistance` 和最小尺寸为准。
- 窗体 DPI 缩放、Dock、Padding、最小尺寸或 `FixedPanel` 改变后,像素距离与比例都可能变化。
- 如果需要固定的 25% / 40% 布局,在窗体 `Load`、`OnLayout` 的最后阶段或首次 `BeginInvoke` 中重新调用 `SetSplitterRatio`。
- 不建议在构造函数中、父容器完成布局前读取比例并保存;此时读到的是初始尺寸。
- 用户拖动后是否重新套用业务比例由你决定。若禁止用户调整,设置 `IsSplitterFixed = true`。

### 设计器使用

1. 把 `FluentSplitContainer` 拖到窗体上,设置 `Location`、`Size`、`Orientation`。
2. 选中 Panel1 或 Panel2,在“属性”窗口展开 `Controls` 集合编辑器添加内容。
3. 设置 `Panel1MinSize`、`Panel2MinSize`、`SplitterWidth` 和 `FixedPanel`。
4. 拖动分隔条得到初始像素距离,该值由原生设计器序列化。
5. `SplitterRatio` 不出现在设计器属性网格中,运行时用代码设置。

### 常见坑

- **不要忘记设置 Dock。** 构造时是 `DockStyle.None`;设置 `Dock = Fill` 会覆盖普通流式布局中的 Location/Size 效果。
- **不要手动给分隔条加子控件。** 分隔区域由基类布局,本类只绘制视觉提示。
- **最小尺寸会阻止目标比例。** 例如宽度 300px、Panel1MinSize 100px 时,25% 不会得到 75px,实际第一面板至少 100px(还需满足 Panel2MinSize)。
- **透明面板不是无限透明窗口。** `Panel1`、`Panel2` 使用透明背景以合成父容器颜色;在主题切换或父容器变化时会重新应用透明。自定义不透明面板背景应由你的子内容决定。
- **比例包含分隔条。** `SplitterRatio = 0.5` 不等于 Panel1 与 Panel2 内容宽度完全相等,因为 8px 分隔条也计入分母。
- **程序赋值和用户拖动事件不同。** 不要假设 `SetSplitterRatio` 一定触发 `SplitterMoving`;需要最终结果时在设置后读取 `SplitterRatio`,或监听 `SplitterMoved`。
- **主题只绘制分隔区域。** 两个面板中的 Fluent 控件各自订阅主题,原生 WinForms 控件不会自动变成 Fluent 外观。


## FluentCard

Fluent 卡片(继承 `Panel`):圆角层表面 + 细描边,像 Panel 一样承载子控件。

### 属性

| 属性 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| CornerRadius | float | 4f(FluentGraphics.ControlCornerRadius) | 卡片圆角半径。 |

构造函数预设:`Padding = new Padding(12)`,字体为 Body。派生类可重写受保护的 `CardFill`/`CardStroke` 颜色与 `OnPaintCard(PaintEventArgs)` 绘制钩子。

### 事件

本类未声明新事件,继承标准 Panel/Control 事件。

### 示例

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

### 备注

- 绘制时会把自身 BackColor 同步为卡片填充色,使子控件(如标签)解析出与卡面一致的环境背景色。

## FluentCardAction

可点击的 Fluent 卡片(继承 `FluentCard`):带悬停/按下/禁用层状态,交互感受类似按钮。

### 属性

本类未声明新的公开属性,继承 FluentCard 的 CornerRadius 等。构造函数预设 `Cursor = Cursors.Hand`。

### 事件

本类未声明新事件;使用继承的标准 `Click` 事件响应点击。

### 示例

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

### 备注

- 状态填充:悬停用 ControlFillSecondary,按下用 ControlFillTertiary,禁用用 ControlFillDisabled;鼠标移出会同时清除按下态。

## FluentCardColor

带彩色竖条的 Fluent 卡片(继承 `FluentCard`):左缘 4 像素强调条用于分类标识。

### 属性

| 属性 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| AccentColor | Color | Color.Empty | 竖条颜色;为 Empty 时回退到主题的 SystemAccentColorPrimary。 |

### 事件

本类未声明新事件,继承 FluentCard/Panel 的标准事件。

### 示例

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

### 备注

- 竖条绘制在卡片左缘(宽 4 像素、上下各留 1 像素),建议内容左边距 ≥ 20 像素避让。

## FluentExpander

Fluent 展开器:可点击的头部行(图标 + 标题 + 箭头),下方为可折叠内容面板;子控件加到 `ContentPanel`。

### 属性

| 属性 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| ContentPanel | Panel | 内置实例 | 只读,可展开内容的宿主面板;初始隐藏,Padding 为 (12, 8, 12, 12)。 |
| Header | string | "Header" | 头部标题文本。 |
| HeaderIcon | SymbolRegular | SymbolRegular.Empty | 头部图标;为 Empty 时不绘制。 |
| IsExpanded | bool | false | 是否展开;变化时显示/隐藏内容面板、重算高度并触发 ExpandedChanged。 |

### 事件

| 事件 | 说明 |
| --- | --- |
| ExpandedChanged | 展开状态变化时触发:点击头部或代码设置 IsExpanded。 |

### 示例

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

### 备注

- 默认尺寸 300×48(仅头部);头部高固定 48 像素,点击头部任意位置切换展开。
- 展开时控件高度自动调整为"头部 + 内容高度",内容高度取各子控件 Bottom 加下边距的最大值,因此子控件需自行指定 Location/尺寸。
- 禁用(Enabled = false)时头部不响应点击,文本与图标使用禁用色。

## FluentCardExpander

卡片风格的展开器(继承 `FluentExpander`),语义上表示头部与内容共享同一卡片表面。

### 属性

当前实现未新增任何属性,与 FluentExpander 完全一致(ContentPanel、Header、HeaderIcon、IsExpanded)。

### 事件

继承 FluentExpander 的 ExpandedChanged。

### 示例

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

### 备注

- 当前版本为 FluentExpander 的空派生类,外观与行为一致,主要用作语义区分与未来扩展点。

## FluentScrollViewer

Fluent 滚动视图(继承 `Panel`):承载单个内容控件,绘制纤细的垂直滚动条,悬停/拖动时加宽。

### 属性

| 属性 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| Content | Control | null | 被滚动的内容控件;设置时替换旧内容并复位滚动。需自行把内容的 Height 设为期望的总高度。 |
| ScrollOffset | int | 0 | 当前垂直滚动偏移(像素);赋值自动收敛到 [0, 内容高度 − 视口高度]。 |

### 事件

本类未声明新事件,继承标准 Panel/Control 事件。

### 示例

参照 Gallery 页面基类的真实用法,内容用自动尺寸的纵向 FlowLayoutPanel:

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

// 以编程方式滚动
scroll.ScrollOffset = 200;
```

### 备注

- 仅支持垂直滚动;内容宽度自动设为视口宽(需要滚动条时右侧预留 14 像素),高度决定可滚动范围。
- 滚动条常态宽 6 像素、悬停或拖动时 10 像素;支持滚轮、拖动滑块、点击轨道翻页(每次一页视口高度)。
- 控件背景刻意保持不透明以避免透明模拟导致的兄弟控件残影;主题切换与更换父容器时自动刷新背景。