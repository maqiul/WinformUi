# 布局与窗口控件参考

本文档覆盖 WinformUi 中布局与窗口相关的七个类型:`FluentTabView`(含页签类 `FluentTabViewPage`)、`FluentGridView`、`FluentTreeGrid`(含 `FluentTreeGridColumn` / `FluentTreeGridNode`)、`FluentWrapPanel`、`FluentUniformGrid`、`FluentTitleBar`、`FluentClientAreaBorder`。全部位于命名空间 `WinformUi.Controls`,其中继承 `FluentControlBase` 的控件随 `ApplicationThemeManager` 的主题自动换肤,布局面板与内容容器也会订阅主题变化重绘背景。属性、事件与默认值均以源码为准;为节省篇幅,示例片段省略 `using` 行(需自行引入 `System`、`System.Drawing`、`System.Windows.Forms`、`WinformUi.Controls` 等命名空间)。

## FluentTabView

浏览器风格的页签容器:每个页签是一个可携带图标、可关闭的圆角标签,选中页签顶部有强调色横条;标签行放不下时自动换行,支持鼠标拖动重排、悬停显示关闭按钮、可选的"新建标签"按钮,页签内容为真实子控件容器(与 TabControl 用法一致)。默认尺寸 400×240。

### 属性

| 属性 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| Pages | List&lt;FluentTabViewPage&gt; | 空列表 | 页签页面集合(内容属性,设计器可直接编辑);通过 AddPage/AddTab 注册,不要只往列表里塞而不注册。 |
| SelectedIndex | int | -1 | 当前选中页签索引;越界或 -1 视为无选中;赋值触发 SelectedIndexChanged。 |
| SelectedPage | FluentTabViewPage | null | 选中页面对象;为 Browsable(false),赋值等价于按 IndexOf 设置 SelectedIndex。 |
| CanReorderTabs | bool | true | 是否允许按住标签拖动重排(拖动距离超过系统 DragSize 后激活)。 |
| CanAddTabs | bool | true | 是否在标签行右侧绘制"+"按钮,点击调用 AddTab() 新建默认页签。 |
| TabHeight | int | 36 | 标签行高度(DIP,内部按 DPI 缩放);赋值钳制到不小于 24。 |

页签类 `FluentTabViewPage`(继承 Panel):

| 属性 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| Text | string | "Tab" | 页签标题;修改后自动重排标签行。继承自 Panel。 |
| Icon | SymbolRegular | SymbolRegular.Empty | 页签左侧 16px 图标;Empty 不绘制。 |
| IsClosable | bool | true | 是否在页签右侧绘制关闭按钮(悬停/按下时出现圆角底)。 |

### 事件

| 事件 | 说明 |
| --- | --- |
| SelectedIndexChanged | 选中页签变化时触发(点击、键盘、代码赋值)。 |
| TabAdding | AddTab 前触发,参数 TabViewTabAddingEventArgs 携带 Page;置 Cancel = true 可取消添加,也可在处理器里替换/补上 Page(未传页面时控件会自建默认页签)。AddPage 不触发此事件。 |
| TabClosing | 点击关闭按钮或调用 CloseTab 时触发,参数 TabViewTabClosingEventArgs 携带 Page;置 Cancel = true 可阻止关闭(CloseTab 返回 false)。 |

另提供方法:`AddTab(page = null, title = null)` 走 TabAdding 流程并选中新页签,返回添加的页面(被取消时为 null);`AddPage(page, title = null)` 直接注册页面、绕过 TabAdding,若当前无选中则选中它;`CloseTab(page)` 走 TabClosing 流程,返回是否真正关闭。

### 示例

```csharp
var tabs = new FluentTabView { Dock = DockStyle.Fill };

// 方式一:AddTab 一步创建并选中
var home = tabs.AddTab(title: "首页");
home.Icon = SymbolRegular.Home24;
var label = new FluentLabel { Text = "Home", AutoSize = true, Location = new Point(16, 16) };
home.Controls.Add(label);

// 方式二:先构造页面再注册
var settingsPage = new FluentTabViewPage
{
    Text = "设置",
    Icon = SymbolRegular.Settings24,
    IsClosable = false
};
tabs.AddPage(settingsPage);

// 拦截关闭:未保存时禁止关掉"设置"页
bool unsavedChanges = true;
tabs.TabClosing += (s, e) =>
{
    if (e.Page == settingsPage && unsavedChanges)
        e.Cancel = true;
};

// 点"+"按钮时自定义新页签
tabs.TabAdding += (s, e) =>
{
    e.Page = e.Page ?? new FluentTabViewPage();
    if (e.Page != null)
        e.Page.Text = "标签 " + (tabs.Pages.Count + 1);
};

Controls.Add(tabs);
```

### 备注

- 标签文本按 FluentTypography.Body 测量,最小宽度 48px(DIP);行尾放不下自动换到下一行,标签行数随内容增长,内容区紧贴标签条底部并绘制下缘圆角的 1px 分隔边框。
- 键盘 Left/Right 在相邻页签间切换(仅当焦点在 FluentTabView 自身而非子控件时);点击标签即选中,按住标签横向拖动可重排(CanReorderTabs)。
- CanAddTabs 开启时右侧固定预留 "+" 按钮区域;点击"+"触发的 AddTab 会先抛 TabAdding,可在该事件里 Cancel 或替换页面。
- 所有尺寸均经控件的 Scale 方法按 DPI 缩放,高 DPI 下无需手工调整。
- 主题切换时页面背景被设为透明以透出容器底色,不要在页面上重设 BackColor 依赖固定颜色。
- 若把 FluentTabView 与其他控件一起用 Dock 布局,注意 WinForms 的 Dock 次序规则:先加的控件更靠边缘。

## FluentGridView

虚拟化磁贴视图:等大圆角卡片按列自动换行排布,只绘制可见行;单击选中(选中卡片带 2px 强调色描边),滚轮整行滚动,支持方向键导航,数据绑定模型与 FluentListBox 完全一致(IList / BindingSource / DataTable 等)。默认尺寸 320×220。

### 属性

| 属性 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| Items | IList | 空的 List&lt;object&gt; | 未绑定模式的数据项;整体替换会清空选择并复位滚动。已设置 DataSource 时读取或赋值 Items 抛 InvalidOperationException。Browsable(false),设计器属性窗格不显示。 |
| DataSource | object | null | 数据源:IList、IListSource、IEnumerable、DataTable、BindingSource 等,解析方式与 FluentListBox 相同(经 BindingContext/CurrencyManager)。 |
| DataMember | string | "" | 绑定 DataSet 时的表名。 |
| DisplayMember | string | "" | 用作卡片标题的项属性名;为空时 ToString()。 |
| ValueMember | string | "" | 用作 SelectedValue 的项属性名。 |
| SelectedValue | object | null | 选中项的 ValueMember 值;赋值按值定位选中项(遍历查找,找不到保持不变)。 |
| TextSelector | Func&lt;object, string&gt; | null | 自定义标题提取函数;优先于 DisplayMember,为 null 时走绑定文本/ToString。 |
| IconSelector | Func&lt;object, SymbolRegular&gt; | null | 每项上方绘制的 16px 图标;为 null 或返回 Empty 时不绘制。 |
| TileSize | Size | 88, 88 | 磁贴尺寸(DIP);宽高分别钳制到不小于 24。StretchTiles 为 true 时仅宽度作为列数计算基准。 |
| Spacing | int | 6 | 磁贴间距及四边留白(DIP);不小于 0。 |
| StretchTiles | bool | true | 是否把整行宽度平分给磁贴(否则保持 TileSize 固定宽度)。 |
| SelectedIndex | int | -1 | 选中项索引;越界视为 -1;赋值会自动滚动到可见并同步绑定位置,触发 SelectedIndexChanged。 |
| SelectedItem | object | null | 选中项对象;赋值按 IndexOf 定位,未找到时不变。 |

卡片点击参数类 `FluentGridViewItemClickEventArgs` 携带 `Index`(int)与 `Item`(object)。

### 事件

| 事件 | 说明 |
| --- | --- |
| SelectedIndexChanged | 选择变化时触发:点击、键盘导航或代码赋值。 |
| ItemClicked | 鼠标左键在磁贴上按下并抬起于同一磁贴时触发(在 SelectedIndexChanged 之后)。 |

另提供方法 `RefreshItems()`:绑定列表被原地修改后重新解析绑定并刷新。

### 示例

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

// 绑定自定义对象(App 为在窗体外声明的普通类:
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

### 备注

- 仅绘制可见行(按滚动位置计算首末行),大量条目时绘制开销与可见区成正比;数据项多时优先用它而不是 FluentWrapPanel。
- 键盘:Left/Right 相邻项、Up/Down 上下同列、Home/End 首末项,选中项自动滚动到可见区;滚动只靠滚轮,不绘制滚动条,每次滚轮一格一行(按 Delta/120 计行)。
- 选中卡片为 ControlFillSecondary 底 + 2px 强调色圆角描边,悬停有轻微底色变化;自定义卡片内容可重写受保护的 `PaintTile(Graphics, int, RectangleF)`。
- Items、TextSelector、IconSelector、SelectedValue、SelectedItem 标记了 Browsable(false),仅供代码使用;设计器数据绑定走 DataSource/DisplayMember/ValueMember。同一数据源的多个列表控件经 CurrencyManager 同步选中位置。
- 全部布局尺寸(TileSize、Spacing)按 DPI Scale 后参与列数计算,高 DPI 下列数会自动适配。

## FluentTreeGrid

树形数据网格:表头行 + 多列树形行,行可像 TreeView 一样展开/折叠(折叠节点显示向右箭头,展开显示向下箭头),整行选中(强调色底 + 左侧 3px 竖条),列分隔线与行分隔线,支持滚轮滚动与树形键盘导航。默认尺寸 480×240。

### 属性

| 属性 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| Columns | List&lt;FluentTreeGridColumn&gt; | 空列表 | 列定义集合(内容属性);为空时整行只显示节点 Text,无表头文字列。 |
| RootNodes | List&lt;FluentTreeGridNode&gt; | 空列表 | 根节点集合(内容属性);修改后需调用 Rebuild() 重新展平可见行。 |
| SelectedNode | FluentTreeGridNode | null | 选中节点;赋值会展开链路上的可见性检查并滚动到可见,触发 SelectedNodeChanged。Browsable(false)。 |

列类 `FluentTreeGridColumn`:

| 属性 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| Title | string | "" | 表头文字。 |
| Width | int | 0 | 列宽(DIP);0 表示与其余 0 宽列平分剩余空间。 |
| TextAlign | HorizontalAlignment | Left | 表头与单元格对齐方式(Left/Center/Right)。 |

节点类 `FluentTreeGridNode`:

| 属性 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| Text | string | "" | 第 0 列(树列)文本。 |
| Icon | SymbolRegular | SymbolRegular.Empty | 文本左侧 16px 图标;Empty 不绘制。 |
| Tag | object | null | 任意业务数据。 |
| Cells | List&lt;string&gt; | 空列表 | 第 1 列起的单元格值(Cells[i] 对应 Columns[i+1]),不足时按空字符串绘制。 |
| IsExpanded | bool | false | 是否展开子节点;运行时点击箭头或键盘 Left/Right 会翻转它。 |
| Nodes | List&lt;FluentTreeGridNode&gt; | 空列表 | 子节点集合。 |

构造便捷:`new FluentTreeGridNode("文本", child1, child2...)` 可同时填 Text 与子节点;`new FluentTreeGridColumn("标题", 宽度)` 可同时填 Title 与 Width。

### 事件

| 事件 | 说明 |
| --- | --- |
| SelectedNodeChanged | SelectedNode 变化时触发。 |
| NodeExpandedChanged | 节点展开/折叠状态变化时触发(鼠标点箭头或键盘 Left/Right)。 |

### 示例

```csharp
var treeGrid = new FluentTreeGrid { Dock = DockStyle.Fill };
treeGrid.Columns.Add(new FluentTreeGridColumn("名称", 220));
treeGrid.Columns.Add(new FluentTreeGridColumn("大小", 100) { TextAlign = HorizontalAlignment.Right });
treeGrid.Columns.Add(new FluentTreeGridColumn("类型"));   // Width = 0,平分剩余

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

### 备注

- 修改 RootNodes、任意节点的 Nodes/IsExpanded 后必须调用 `Rebuild()` 重新展平可见行(首次 OnPaint 时若从未展平会自动 Rebuild 一次,但之后的修改不会自动感知)。
- 布局常量(DIP,内部按 DPI 缩放):表头高 36、行高 30、每层缩进 18;选中/滚动均自动确保行可见。
- 键盘:Up/Down 在可见行间移动,Left 折叠当前节点、Right 展开,Home/End 跳首/末可见行;滚动仅靠滚轮,一次一行,表头固定不滚出。
- 第 0 列文本前依序绘制箭头(有子节点时)与可选图标;点击箭头矩形区域(18px 宽、整行高)切换展开,点击行内其他位置选中。
- 该控件为自绘展示控件,不提供单元格编辑、排序或数据源绑定;需要编辑网格请用 FluentDataGridView。

## FluentWrapPanel

带主题背景的换行流式面板,继承 `FlowLayoutPanel`:子控件按 Margin 从左到右排布、到边界自动换行,内容超出时自动出现滚动条。适合少量等大卡片/标签的松散排布;条目多、需虚拟化滚动的场景请改用 FluentGridView。

### 属性

FluentWrapPanel 自身不新增公共属性,全部沿用 `FlowLayoutPanel`,构造函数预设了三个值:

| 属性(继承) | 类型 | 构造预设 | 说明 |
| --- | --- | --- | --- |
| WrapContents | bool | true | 到达边缘自动换行;可再改回 false 变单向滚动。 |
| FlowDirection | FlowDirection | LeftToRight | 排列方向;可改为 TopDown/RightToLeft 等。 |
| AutoScroll | bool | true | 内容超出客户区时启用自动滚动。 |

其余如 `Padding`、各子控件的 `Margin`、`FlowDirection` 等按 FlowLayoutPanel 常规用法调整间距与对齐。

### 示例

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

### 备注

- 背景在 OnPaintBackground 里走控件统一的实心背景绘制(ControlHelper.PaintFullBackground),随 ApplicationThemeManager 主题切换自动重绘(内部经 ThemeSubscription 订阅)。
- 无虚拟化:所有子控件都是真实控件并参与布局,几十上百个条目时性能与滚动平滑度不如 FluentGridView。
- 与原生 FlowLayoutPanel 一样按子控件 DesiredSize/AutoSize 排布,行高由该行最高控件决定,不保证等高;需要严格等大单元格网格用 FluentUniformGrid 或 FluentGridView。

## FluentUniformGrid

等分单元格布局面板,继承 `Panel`:把客户区按 Rows × Columns 均分成等大单元格,子控件按加入顺序(Controls 索引)依次填入。

### 属性

| 属性 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| Rows | int | 0 | 行数;0 表示自动:若 Columns > 1 则按子控件数向上取整算出行数,否则行数取子控件总数(单列)。赋值钳制到不小于 0。 |
| Columns | int | 1 | 列数;不小于 1。Rows 已指定且 Columns 为默认 1 时,若子控件数超过行数,列数按向上取整自动增加。 |

### 示例

```csharp
var uniform = new FluentUniformGrid { Dock = DockStyle.Fill, Rows = 2, Columns = 3 };
for (int i = 1; i <= 6; i++)
{
    var card = new FluentCard { Text = "卡片 " + i, Margin = new Padding(4) };
    uniform.Controls.Add(card);
}

// 只定列数,行数自动
var autoRows = new FluentUniformGrid { Columns = 4 };
foreach (string s in new[] { "A", "B", "C", "D", "E" })
    autoRows.Controls.Add(new FluentLabel { Text = s, AutoSize = true });

Controls.Add(uniform);
Controls.Add(autoRows);
```

### 备注

- 单元格尺寸 = 客户区宽/列数、高/行数(整数除法,至少 1px);子控件被直接设置 Bounds 填满单元格,子控件自身的 Margin/Padding 不参与计算——需要间隙请在子控件外层再套一个面板或用带内边距的控件。
- 排不下(行号 ≥ Rows)的子控件会被置 Visible = false 隐藏而不是滚动,请确保单元格数量足够。
- 布局在 OnLayout 中完成,增删子控件、改变尺寸都会自动重排;背景同 FluentWrapPanel 走主题化实心绘制。
- 索引顺序即视觉顺序:第 i 个子控件位于第 i / Columns 行、第 i % Columns 列。

## FluentTitleBar

无边框窗体的 Fluent 标题栏:左侧可选图标(自动取所属 Form.Icon)与标题文字,右侧为帮助/最小化/还原/最大化/关闭按钮组,支持标题区拖拽移动、双击最大化、顶端边缘拖拽调整大小、右键弹出系统菜单;无边框最大化时自动给窗体补 Padding,防止内容被屏幕边缘裁掉。建议停靠于 `FormBorderStyle.None` 窗体顶部,默认停靠高度固定为 48px(DPI 缩放)。

### 属性

| 属性 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| Title | string | "" | 标题文字(本地化属性 Localizable);加粗 BodyStrong 字体绘制,超长省略号截断。 |
| ShowHelpButton | bool | false | 是否在最小化按钮左侧显示"?"帮助按钮,点击触发 HelpButtonClick。 |
| LeadingContent | Control | null | 紧跟标题右侧插槽控件(垂直居中);Browsable(false),赋值自动加入/移除子控件并重新布局。 |
| TrailingContent | Control | null | 按钮组左侧插槽控件(垂直居中);同上。 |

按钮组的可见性由所属窗体决定:Form.MinimizeBox 为 false 时不画最小化按钮,Form.MaximizeBox 为 false 时不画最大化按钮;窗体处于最大化时最大化按钮自动变为"还原"图标。所属窗体设置了 Icon 则自动绘制 16px 图标,未设置则标题左移补位——标题栏本身没有 ShowIcon 属性。

### 事件

| 事件 | 说明 |
| --- | --- |
| HelpButtonClick | 点击帮助按钮时触发;不会自动打开任何帮助窗口,行为完全由订阅方决定。 |

### 示例

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

        // 注意:先加 Fill 内容,再 Dock 标题栏到 Top,
        // 否则 Fill 区域会被标题栏遮挡(WinForms Dock 次序规则)。
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

### 备注

- 命中测试通过重写 WM_NCHITTEST 实现:按钮区返回 HTMINBUTTON/HTMAXBUTTON/HTCLOSE/HTHELP(保留 DWM 悬停追踪,点击由控件自行处理),非最大化且普通状态时顶端两角与左右边缘分别返回 HTTOPLEFT/HTTOPRIGHT/HTTOP/HTLEFT/HTRIGHT 供拉伸,其余区域返回 HTCAPTION——空白处可拖拽移动、双击最大化/还原;右键标题区弹出窗体系统菜单(移动/大小/最小化/关闭)。底边缘的拉伸由窗体其他客户区处理,不属于标题栏职责。
- 最大化补偿:当所属窗体为 FormBorderStyle.None 且 WindowState = Maximized 时,控件把 Form.Padding 临时改为 SystemInformation.FrameBorderSize 宽度(析构或还原时恢复原值);因此内容面板应依赖 Padding 而不是负边距布局。若内容区使用 FluentClientAreaBorder,则由其负责同样的填充,两者搭配时以窗体实际布局为准避免双重留白观感。
- 停靠 Top 或 Bottom 时高度被 SetBoundsCore 锁定为 Scale(48),按钮尺寸 46×32(DIP),手工设置无效。
- 关闭按钮悬停为红色(SystemFillCritical)白色图标,其余按钮悬停为 SubtleFill 圆角底;窗体 Enabled = false 时按钮命中失效。
- 背景绘制区分宿主:宿主为已应用回底的 FluentWindow 时透出回底,否则绘制不透明窗体色,避免 PrintWindow 截屏发黑。
- Dock 次序提醒:WinForms 中先加入的 Dock 控件占据更外侧边缘。要让标题栏占满整行顶部,应先 Controls.Add 内容区(Fill)再 Controls.Add 标题栏(Top);顺序颠倒时 Fill 内容会盖到标题栏区域。设计器里等价操作是把 FluentTitleBar 的"置于前端/顶端顺序"调到最后。

## FluentClientAreaBorder

自定义边框窗体的内容容器面板,继承 `Panel`:专用于 FormBorderStyle.None 的窗体,负责两件事——宿主窗体最大化时给自身加上窗口拉伸框厚度的 Padding(无边框最大化默认会把内容顶出屏幕边缘),以及在缺少现代窗口描边的旧系统上绘制 1px 外框线。

### 属性

FluentClientAreaBorder 不新增任何公共属性,全部沿用 `Panel`(典型用法是 `Dock = DockStyle.Fill`)。内部行为相关:

| 内部机制 | 说明 |
| --- | --- |
| Padding(自动) | 宿主窗体为 None 边框且最大化时,自动设为 SystemInformation.FrameBorderSize 的宽高最大值;其余状态恢复为 0。请勿手工覆盖该 Padding,否则最大化时会被本控件重写。 |
| 边框线(自动) | 仅当运行系统主版本号 < 10(不支持现代窗口边框)时绘制 1px 矩形描边,颜色随 ApplicationThemeManager 当前主题(深色 #3A3A3A,浅色 #7A7A7A);Windows 10+ 上不绘制,交给系统 DWM 描边。 |
| 窗体跟踪 | OnHandleCreated / OnParentChanged 时挂接宿主窗体的 LocationChanged / Resize 重算 Padding,Dispose 时解绑。 |

### 示例

```csharp
public sealed class BorderlessForm : Form
{
    public BorderlessForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        ClientSize = new Size(640, 420);

        // 先加 Fill 内容容器,再加 Top 标题栏,保证标题栏占满顶行
        var body = new FluentClientAreaBorder { Dock = DockStyle.Fill };
        var hello = new FluentLabel { Text = "内容区", AutoSize = true, Location = new Point(20, 20) };
        body.Controls.Add(hello);
        Controls.Add(body);

        var titleBar = new FluentTitleBar { Dock = DockStyle.Top, Title = "无边框窗体" };
        Controls.Add(titleBar);
    }
}
```

### 备注

- 与 FluentTitleBar 配套使用:标题栏处理窗口非客户区交互(拖动、命中测试),本控件处理内容区在最大化时的安全区内缩,二者都检测 FormBorderStyle.None + Maximized 状态。
- 背景为 Panel 默认绘制,主题切换时经 ThemeSubscription 自动重绘;子控件请在其客户区内自行 Dock/定位,最大化时它会感受到 Padding 变化而整体内移,这是预期行为。
- 旧系统(Win 8.1 及更早)上的描边只在 Win10 以下版本生效;项目多目标支持 net48 至 net10,运行于现代 Windows 时该边框由系统提供,控件不会重复画线。
- 它不做圆角、不做投影,仅是"内容安全区 + 兼容描边"的轻量容器;需要整体窗口效果请配合 FluentWindow 使用。
