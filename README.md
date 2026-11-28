# WinformUi

把 Fluent Design(Windows 11 / WinUI 风格)完整带进 Windows Forms 的控件库。
纯 C# 实现,不依赖 WPF、不依赖任何第三方 UI 框架,图标字体内嵌,开箱即用。

- 浅色 / 深色 / 跟随系统三种主题,全局一键切换,所有控件自动换肤
- 读取 Windows 系统强调色,也可自定义强调色
- `FluentWindow` 默认纯色 Fluent 背景,Windows 11 22H2+ 可选开启 Mica / Acrylic / Tabbed 系统背景
- 内嵌 Fluent System Icons 图标字体(`SymbolRegular` / `SymbolFilled` 数千枚图标枚举)
- 高 DPI 友好:矢量绘制 + 原生度量文本渲染,清晰不模糊
- 列表控件(ComboBox / DropDownList / ListBox / ListView)支持标准数据绑定:
  `DataSource` / `DataMember` / `DisplayMember` / `ValueMember` / `SelectedValue`,
  可绑 DataTable / DataSet / BindingSource / 对象列表;设计器智能标记与原生 ComboBox 同款:
  "使用数据绑定项"勾选切换绑定 / 未绑定模式,未绑定模式提供"编辑项..."字符串集合编辑器,
  绑定模式提供"选择数据源"下拉与成员字段(.NET Framework 与 .NET 进程外设计器均支持)
- 附带 Gallery 演示程序,覆盖全部控件与两种主题

## 文档索引

完整参考手册在 [docs/](docs/)(随 NuGet 包一并分发),中文在前、英文镜像在
[docs/en/](docs/en/),章节与小节一一对应:

| 文档 | 内容 | English |
| --- | --- | --- |
| [控件·按钮与基础](docs/controls-buttons.md) | Button / Toggle / Hyperlink / Split / DropDown / Label / Separator / Rating 等 | [en](docs/en/controls-buttons.md) |
| [控件·输入](docs/controls-inputs.md) | TextBox / PasswordBox / NumberBox / AutoSuggestBox / CheckBox / RadioButton / ToggleSwitch / Slider / ComboBox 等 | [en](docs/en/controls-inputs.md) |
| [控件·日期时间颜色](docs/controls-datetime.md) | Calendar / CalendarDatePicker / DatePicker / TimePicker / ColorPicker | [en](docs/en/controls-datetime.md) |
| [控件·集合](docs/controls-collections.md) | ListBox / ListView / TreeView / DataGridView | [en](docs/en/controls-collections.md) |
| [控件·导航与容器](docs/controls-navigation.md) | NavigationView / TabControl / BreadcrumbBar / StatusBar / Card / Expander / ScrollViewer | [en](docs/en/controls-navigation.md) |
| [控件·小组件](docs/controls-widgets.md) | Icon / PictureBox / CodeBlock / RichTextBox / ScrollBar / ToolStrip / NotifyIcon / Toast | [en](docs/en/controls-widgets.md) |
| [控件·布局与窗口](docs/controls-layouts.md) | TabView / GridView / TreeGrid / WrapPanel / UniformGrid / TitleBar / ClientAreaBorder | [en](docs/en/controls-layouts.md) |
| [控件·进度与反馈](docs/controls-feedback.md) | ProgressBar / ProgressRing / InfoBar / Badge / Snackbar / LoadingScreen / TaskbarProgress | [en](docs/en/controls-feedback.md) |
| [控件·对话框与弹出](docs/controls-dialogs.md) | MessageBox / ContentDialog / Flyout / PopupForm / ToolTip | [en](docs/en/controls-dialogs.md) |
| [控件·菜单](docs/controls-menus.md) | MenuStrip / ContextMenuStrip / Fluent 渲染器 | [en](docs/en/controls-menus.md) |
| [窗口](docs/window.md) | FluentWindow、Mica/Acrylic/Tabbed 背景与回退 | [en](docs/en/window.md) |
| [主题系统](docs/theming.md) | 主题 API、完整设计令牌表、强调色派生、字体排印、自定义控件接入 | [en](docs/en/theming.md) |

每个控件文档含属性表(类型/默认值/说明)、事件、可编译示例与使用备注。

## 支持平台

| 目标框架 | 说明 |
| --- | --- |
| `net46` | .NET Framework 4.6+(4.6 ~ 4.8 均可直接引用) |
| `net6.0-windows` | .NET 6 |
| `net8.0-windows` | .NET 8 |
| `net10.0-windows` | .NET 10 |

操作系统:Windows 10 及以上可正常运行。Mica / Acrylic / Tabbed 系统背景为可选项
(`BackdropType`,需要 Windows 11 22H2+);因 GDI 渲染与全客户区 glass 合成在部分
Windows 11 版本上产生重影,1.2.1 起默认使用 Fluent 纯色背景。

## 快速开始

### 安装

NuGet(发布后):

```bash
dotnet add package WinformUi
```

或在本仓库内直接项目引用:

```bash
dotnet add reference src/WinformUi/WinformUi.csproj
```

### 启动时应用主题

```csharp
using System;
using System.Windows.Forms;
using WinformUi.Theming;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        // 三选一:Light / Dark / System(跟随系统)
        ApplicationThemeManager.Apply(ApplicationTheme.System);

        Application.Run(new MainForm());
    }
}
```

### 第一个窗口

```csharp
using System.Drawing;
using WinformUi.Controls;
using WinformUi.Theming;

public class MainForm : FluentWindow
{
    public MainForm()
    {
        Text = "WinformUi 演示";
        ClientSize = new Size(800, 560);

        var button = new FluentButton
        {
            Text = "主要按钮",
            Appearance = ControlAppearance.Primary,
            Icon = SymbolRegular.Home24,
            Location = new Point(24, 24)
        };
        Controls.Add(button);

        var input = new FluentTextBox
        {
            PlaceholderText = "请输入内容",
            Location = new Point(24, 70),
            Width = 240
        };
        Controls.Add(input);
    }
}
```

`FluentWindow` 继承自 `Form`,保留原生标题栏(自动深色模式、Win11 圆角),
客户区应用系统背景。普通 `Form` 上也可以直接使用全部控件。

### 导航视图(侧栏 + 页面)

页面继承 `FluentPage`,导航项指定页面类型,`FluentNavigationView` 自带页面宿主与前进 / 后退历史;
`NavigationViewItemHeader` / `NavigationViewItemSeparator` 负责分组,`Tag` 即导航参数:

```csharp
var nav = new FluentNavigationView { Dock = DockStyle.Fill };
nav.MenuItems.Add(new NavigationViewItemHeader("浏览"));
nav.MenuItems.Add(new NavigationViewItem("首页", SymbolRegular.Home24, typeof(HomePage)));
nav.MenuItems.Add(new NavigationViewItem("输入", SymbolRegular.Code24, typeof(InputsPage)));
nav.MenuItems.Add(new NavigationViewItemSeparator());
nav.MenuItems.Add(new NavigationViewItemHeader("详情"));
nav.MenuItems.Add(new NavigationViewItem("订单 A-1001", SymbolRegular.Document24, typeof(DetailPage)) { Tag = "A-1001" });
nav.FooterMenuItems.Add(new NavigationViewItem("设置", SymbolRegular.Settings24, typeof(SettingsPage)));
Controls.Add(nav);
nav.SelectItem(nav.MenuItems[1], true);
```

页面按「类型 + 参数」缓存,`FluentPage.NavigationCacheMode` 取 `Disabled` / `Enabled` / `Required`,
宿主 `FluentFrame.CacheSize` 控制上限;重写 `OnNavigatedTo` / `OnNavigatedFrom` 拿到
`NavigationEventArgs`(含 `Parameter` 与 `Mode`)完成页面生命周期。

```csharp
public class DetailPage : FluentPage
{
    public DetailPage() { NavigationCacheMode = NavigationCacheMode.Enabled; }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        Text = "订单 " + e.Parameter;   // e.Mode: New / Back / Forward / Refresh
    }
}
```

完整用法见 [samples/WinformUi.Gallery](samples/WinformUi.Gallery)。

## 主题系统

```csharp
// 切换主题(所有已创建控件自动重绘)
ApplicationThemeManager.Apply(ApplicationTheme.Dark);

// 自定义强调色
ApplicationThemeManager.Apply(ApplicationTheme.Light, Color.FromArgb(0, 120, 212));

// 跟随系统时,系统设置变化后刷新(WM_SETTINGCHANGE 会自动触发)
ApplicationThemeManager.RefreshSystemColors();

// 订阅主题变化,刷新自定义绘制的内容
ApplicationThemeManager.ThemeChanged += (s, e) => myControl.Invalidate();

// 读取当前调色板(Fluent 设计令牌)
Color fill = ApplicationThemeManager.Palette.ControlFillDefault;
Color text = ApplicationThemeManager.Palette.TextFillPrimary;
```

`FluentPalette` 提供与 WinUI 对齐的完整设计令牌:文本填充、控件填充、
描边、卡片 / 层、强调色变体、系统状态色等,自定义控件可直接取用。

## 控件清单

| 分类 | 控件 |
| --- | --- |
| 窗口 | `FluentWindow`(Mica/Acrylic/Tabbed 背景、深色标题栏) |
| 按钮 | `FluentButton`(`Appearance` 九种外观、`Icon`)、`FluentToggleButton`、`FluentHyperlinkButton`、`FluentSplitButton`、`FluentDropDownButton` |
| 基础 | `FluentLabel`、`FluentSeparator`、`FluentAnchor`、`FluentIcon`、`FluentPictureBox`、`FluentScrollBar`、`FluentCodeBlock` |
| 输入 | `FluentTextBox`、`FluentRichTextBox`、`FluentMaskedTextBox`、`FluentPasswordBox`、`FluentNumberBox`、`FluentAutoSuggestBox`、`FluentCheckBox`、`FluentRadioButton`、`FluentToggleSwitch`、`FluentSlider`、`FluentComboBox`、`FluentDropDownList`、`FluentRatingControl`、`FluentThumbRate` |
| 日期 / 时间 / 颜色 | `FluentCalendar`、`FluentCalendarDatePicker`、`FluentDatePicker`、`FluentTimePicker`、`FluentColorPicker` |
| 集合 | `FluentListBox`、`FluentCheckedListBox`、`FluentListView`、`FluentTreeView`、`FluentDataGridView`、`FluentGridView`、`FluentTreeGrid` |
| 导航 / 容器 | `FluentNavigationView`、`FluentPage`、`FluentFrame`、`FluentTabControl`、`FluentTabPage`、`FluentTabView`、`FluentBreadcrumbBar`、`FluentStatusBar`、`FluentStatusStrip`、`FluentSplitContainer`、`FluentWrapPanel`、`FluentUniformGrid`、`FluentCard`、`FluentCardAction`、`FluentCardColor`、`FluentExpander`、`FluentCardExpander`、`FluentScrollViewer`、`FluentTitleBar`、`FluentClientAreaBorder` |
| 进度 / 反馈 | `FluentProgressBar`、`FluentProgressRing`、`FluentArc`、`FluentInfoBar`、`FluentBadge`、`FluentInfoBadge`、`FluentSnackbar`、`FluentLoadingScreen`、`FluentErrorProvider`、`FluentToast`、`FluentNotifyIcon`、`FluentTaskbarProgress`(组件) |
| 对话框 / 弹出 | `FluentMessageBox`、`FluentContentDialog`、`FluentFlyout`、`FluentPopupForm`、`FluentToolTip` |
| 菜单 | `FluentMenuStrip`、`FluentToolStrip`、`FluentContextMenuStrip`;原生 `ToolStrip` / `MenuStrip` 可换用 `FluentToolStripRenderer` + `FluentColorTable` |

输入类控件提供圆角外壳、`PlaceholderText`、`Icon`(左侧符号图标);
`FluentMaskedTextBox` 内嵌原生掩码编辑器,保留输入掩码、拒绝输入提示、类型校验与完整编辑行为。
文本框为自绘合成渲染,选区 / 光标 / 滚动与原生 edit 度量完全对齐,
深色模式下滚动条同样换肤。

## 图标

图标字体已内嵌进程序集,无需安装字体文件:

```csharp
button.Icon = SymbolRegular.Settings24;          // 线性图标
// SymbolFilled 提供同族填充图标
```

自定义绘制时也可用 `FluentGraphics.DrawGlyph` / `FluentTypography.IconRegular` /
`IconFilled` 直接画符号。

## 运行 Gallery

```bash
dotnet run --project samples/WinformUi.Gallery -f net8.0-windows
```

Gallery 左侧导航覆盖全部控件分类,Settings 页可切换浅色 / 深色 / 系统主题、
自定义强调色、切换窗口背景类型,所有页面两种主题均经过验证。

## 版本说明

### 1.5.0

- **导航参数与页面缓存**:`FluentFrame.Navigate(pageType, parameter)` 与泛型重载把导航参数纳入
  缓存键与历史记录,同一页面类型带不同参数得到各自独立的实例;新增 `CacheSize`(默认 10)、
  `PageFactory`(`Func<Type, object, Control>`,替代无参构造)、`CurrentPageType`、`CurrentParameter`、
  `CurrentCacheMode`。`FluentPage.NavigationCacheMode` 取 `Disabled` / `Enabled` / `Required`,
  页面可重写 `OnNavigatedTo` / `OnNavigatedFrom` 取 `NavigationEventArgs`(`Mode` 为
  `New` / `Back` / `Forward` / `Refresh`);非 `FluentPage` 的控件实现 `INavigationAware` 同样生效。
- **NavigationView 分组**:`NavigationViewItemHeader` 与 `NavigationViewItemSeparator` 继承自
  `NavigationViewItem`,可直接混入 `MenuItems` / `FooterMenuItems`;两者不可选中、不参与命中测试,
  折叠侧栏时标题不占位。`NavigationViewItem.Tag` 现为导航参数,同一页面类型可注册多个入口。
- **任务栏进度**:新增 `FluentTaskbarProgress` 组件与 `TaskbarProgressState`
  (`None` / `Indeterminate` / `Normal` / `Error` / `Paused`,数值与壳层 `TBPFLAG` 一致),
  把确定、不确定、暂停、错误进度画在窗体的任务栏按钮上;`IsSupported` 探测系统能力,
  不支持时静默降级;属性可从后台线程直接赋值,内部会编组到窗体的 UI 线程再调壳层接口。
- **深色主题下的页面底色**:切换到深色主题时,`FluentPage` 会先于宿主窗体收到主题通知,
  于是把新令牌叠在旧背景上算出一个偏亮的底色并固化下来(实测深色的 `#282828` 变成
  `#C1C1C1`)。现在页面把底色更新推迟到本轮通知全部派发完成之后,导航容器里的页面
  与窗体背景保持一致。

### 1.4.2

- 仅包元数据更新，无代码变更：程序集包内声明了项目主页与 git 仓库地址，
  nuget.org 页面可直接跳转回 GitHub 源码仓库。

### 1.4.1

- **DataGridView 滚动条残影**:在表格主体上滚动滚轮(而非拖动滑块)时,滚动条通道里
  会留下上一帧的滑块残块。根因是原生滚动条子窗口在 `SetScrollInfo` 内部直接搬移通道,
  不会向子类化的窗口过程发送 `WM_PAINT`,而 `RedrawWindow(RDW_UPDATENOW)` 对这种子窗口
  同样不投递;因此 Fluent 自绘滑块只画了挂钩那一刻的位置,之后一直陈旧。现改用
  `InvalidateRect` + `UpdateWindow` 强制走子类绘制,并在 `Scroll` 事件后重绘,滚动、
  拖动、主题切换三种路径均无残影。
- **RichTextBox 禁止选中时抛异常**:把 `IsTextSelectionEnabled` 取消勾选后,消息闸门对
  每条消息取 `WParam.ToInt32()`;在 64 位进程里 `WM_MOUSEACTIVATE`(wParam 为父窗口句柄)、
  `WM_CTLCOLOR*`(wParam 为 HDC)等消息的 wParam 超过 Int32 范围,直接抛
  `System.OverflowException`("算术运算导致溢出")。现只截取低位字
  (`WParam.ToInt64() & 0xFFFF`)判断键码与鼠标状态,闸门本身行为不变:关闸即收起
  现有选区,并拦截拖拽、双击取词、Shift 扩展与 Ctrl+A。

### 1.4.0

新增 FluentIcon / FluentPictureBox / FluentToolStrip / FluentRichTextBox /
FluentScrollBar / FluentCodeBlock / FluentTabView / FluentNotifyIcon / FluentToast /
FluentWrapPanel / FluentUniformGrid / FluentGridView / FluentTreeGrid / FluentTitleBar,
Gallery 补充小组件与布局页,工具箱清单与中英文参考手册同步。

## Release notes

### 1.5.0

- **Navigation parameters and page caching**: `FluentFrame.Navigate(pageType, parameter)` and the
  generic overloads put the navigation parameter into both the cache key and the history, so the same
  page type reached with different parameters keeps separate instances. New members: `CacheSize`
  (10 by default), `PageFactory` (`Func<Type, object, Control>`, used instead of the parameterless
  constructor), `CurrentPageType`, `CurrentParameter`, `CurrentCacheMode`.
  `FluentPage.NavigationCacheMode` takes `Disabled` / `Enabled` / `Required`, and pages can override
  `OnNavigatedTo` / `OnNavigatedFrom` to read `NavigationEventArgs` (`Mode` is `New` / `Back` /
  `Forward` / `Refresh`); any other control gets the same lifecycle by implementing `INavigationAware`.
- **NavigationView grouping**: `NavigationViewItemHeader` and `NavigationViewItemSeparator` derive
  from `NavigationViewItem` and can be mixed straight into `MenuItems` / `FooterMenuItems`. Neither
  can be selected and both are skipped by hit-testing; headers take no space in the compact pane.
  `NavigationViewItem.Tag` is now the navigation parameter, so one page type can be registered
  several times.
- **Taskbar progress**: new `FluentTaskbarProgress` component with `TaskbarProgressState`
  (`None` / `Indeterminate` / `Normal` / `Error` / `Paused`, values matching the shell's `TBPFLAG`)
  to draw determinate, indeterminate, paused and error progress on a form's taskbar button;
  `IsSupported` probes the OS and the component degrades silently when the shell interface is absent.
  The properties may be set from a worker thread — the shell call is marshalled onto the thread that
  owns the form.
- **Page background under the dark theme**: on a theme switch a `FluentPage` receives the notification
  before its host window repaints, so it blended the new token over the previous background and froze
  there — the dark `#282828` surface came out as `#C1C1C1`. The page now defers the recalculation until
  the notification round, window included, has finished, so navigated pages match the window background.

### 1.4.2

- Packaging metadata only, no code change: the package now declares its project
  URL and git repository, so the nuget.org page links back to the GitHub source.

### 1.4.1

- **DataGridView scrollbar ghosting**: scrolling with the mouse wheel over the grid body
  (rather than dragging the thumb) left the previous frame's thumb in the channel. A native
  scrollbar child window moves its thumb by blitting inside `SetScrollInfo`, which never
  sends `WM_PAINT` to the subclassed window procedure, and `RedrawWindow(RDW_UPDATENOW)`
  does not deliver one either — so the Fluent-drawn thumb stayed wherever it had been when
  the hook was installed. Repainting now goes through `InvalidateRect` + `UpdateWindow` and
  also runs after the `Scroll` event, so wheel scrolling, thumb dragging and theme switches
  are all clean.
- **Overflow when disabling text selection**: unchecking "允许选中" (`IsTextSelectionEnabled`)
  made the message gate call `WParam.ToInt32()` on every message. In a 64-bit process
  `WM_MOUSEACTIVATE` (parent HWND in wParam) and `WM_CTLCOLOR*` (HDC in wParam) exceed the
  Int32 range and threw `System.OverflowException`. The gate now masks the low word
  (`WParam.ToInt64() & 0xFFFF`) to read the key code or mouse-state flag; its behaviour is
  unchanged — turning it off collapses the current selection and blocks dragging,
  double-click word selection, Shift extension and Ctrl+A.

### 1.4.0

Added FluentIcon, FluentPictureBox, FluentToolStrip, FluentRichTextBox, FluentScrollBar,
FluentCodeBlock, FluentTabView, FluentNotifyIcon, FluentToast, FluentWrapPanel,
FluentUniformGrid, FluentGridView, FluentTreeGrid and FluentTitleBar; the Gallery gained
Widgets and Layout pages; the toolbox manifest and the Chinese/English reference manuals
were updated.

## Documentation

The full reference manual lives in [docs/](docs/) in Chinese, with an English mirror in
[docs/en/](docs/en/) that keeps the same chapter and section structure — see the
documentation index above.

## Build and packaging

```bash
# 构建(四个目标框架)
dotnet build WinformUi.slnx

# 生成 NuGet 包(输出 src/WinformUi/bin/Release/*.nupkg)
dotnet pack src/WinformUi/WinformUi.csproj -c Release
```

包内包含四个 TFM 的程序集、内嵌图标字体、XML 文档、本 README 与
[THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md)。

发布到 nuget.org(需要自己的账号与 API Key):

```bash
dotnet nuget push src/WinformUi/bin/Release/WinformUi.1.5.0.nupkg \
    --api-key <YOUR_API_KEY> --source https://api.nuget.org/v3/index.json
```

## 使用建议

- **DPI**:建议在应用 manifest 中声明 `PerMonitorV2` DPI 感知
  (可直接抄 [samples/WinformUi.Gallery/app.manifest](samples/WinformUi.Gallery/app.manifest)),
  多显示器混合缩放时保持清晰。
- **设计器**:控件均标注了设计器特性,可加入 VS 工具箱拖拽使用;
  包内 `tools/VisualStudioToolsManifest.xml` 让 VS2017+ 工具箱按
  "WinformUi 按钮 / 输入 / 导航 / 反馈…" 九个分类 tab 自动填充
  (需开启 工具→选项→Windows Forms 设计器→Automatically Populate Toolbox);
  主题在运行时由 `ApplicationThemeManager` 统一驱动。
- **混用原生控件**:原生 WinForms 控件放在 Fluent 窗体上时,主题切换会自动
  重绘整棵控件树;菜单 / 工具条换用 Fluent 渲染器即可统一风格。
- **自定义控件**:继承 `FluentControlBase` 或订阅 `ThemeSubscription`,
  从 `ApplicationThemeManager.Palette` 取色,即可自动跟随主题。

## 许可

本项目采用 [MIT 许可](LICENSE)。图标 glyph 索引等衍生材料的第三方许可声明见
[THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md),随包一并分发。