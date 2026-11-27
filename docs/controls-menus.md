# 菜单与工具栏渲染

本篇介绍 WinformUi 的菜单体系:`FluentMenuStrip`(菜单栏)、`FluentContextMenuStrip`(右键菜单)、`FluentToolStripRenderer`(Fluent 渲染器)和 `FluentColorTable`(Fluent 配色表)。均位于命名空间 `WinformUi.Controls`。核心思路是保留原生 `MenuStrip` / `ContextMenuStrip` / `ToolStrip` 的全部功能,只替换渲染层,因此也可以给既有原生控件直接换肤。示例代码省略 `using` 行,使用时请引入 `System`、`System.Drawing`、`System.Windows.Forms`、`WinformUi.Controls`、`WinformUi.Theming` 等命名空间。

## FluentMenuStrip

Fluent 菜单栏:透明底、悬停时显示圆角微填充的 `MenuStrip` 子类,适合放在 Mica 窗体顶部。

### 属性

无自己声明的新公开属性;继承 `MenuStrip` 的全部属性。构造函数已预设:`Renderer = new FluentToolStripRenderer()`、`BackColor = Color.Transparent`、`Font = FluentTypography.Body`、`Padding = new Padding(4, 2, 4, 2)`。

### 事件/方法

无自己声明的新公开事件或方法;菜单项仍使用标准 `ToolStripMenuItem`、`ToolStripSeparator` 等,事件用法与原生完全一致。

### 示例

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

### 备注

- 菜单栏本体透明,直接透出窗体背景(Mica 场景下由渲染器用 GDI+ 绘制顶层文字,避免 DWM 把 GDI 文字当成透明像素)。
- 下拉部分自动获得 Fluent 弹出层外观:不透明表面、描边、圆角悬停项。
- 需要把 Fluent 外观应用到窗体上所有原生工具栏时,见下文 `FluentToolStripRenderer` 的全局用法。

## FluentContextMenuStrip

Fluent 右键菜单:弹出层表面 + 圆角悬停项的 `ContextMenuStrip` 子类,打开时自动应用圆角 Region。

### 属性

无自己声明的新公开属性;继承 `ContextMenuStrip` 的全部属性。构造函数已预设:`Renderer = new FluentToolStripRenderer()`、`BackColor = Palette.AcrylicBackgroundFillDefault`、`Font = FluentTypography.Body`、`ShowImageMargin = false`、`Padding = new Padding(2)`,并挂接 `Opening` 事件刷新背景色与圆角。

### 事件/方法

无自己声明的新公开事件或方法;用法与原生 `ContextMenuStrip` 一致。

### 示例

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

// 挂到任意控件(包括原生控件)上
var target = new Panel { Size = new Size(320, 90), BorderStyle = BorderStyle.FixedSingle };
target.ContextMenuStrip = ctxMenu;
Controls.Add(target);
```

### 备注

- 每次 `Opening` 都会按当前主题重取背景色并重建圆角 Region,主题切换后无需重建菜单。
- 默认关闭图标边距(`ShowImageMargin = false`),需要显示图标列时可自行改回 true。

## FluentToolStripRenderer

Fluent 工具栏渲染器:继承 `ToolStripProfessionalRenderer`,产出圆角悬停填充、主题化文字/箭头/分隔线的菜单外观;构造时自动使用 `FluentColorTable`,并设 `RoundedEdges = false`。

### 属性

无自己声明的新公开属性;继承 `ToolStripProfessionalRenderer` 的属性(如 `RoundedEdges`、`ColorTable`)。

### 事件/方法

无自己声明的新公开事件或方法;通过重写 `OnRender*` 系列受保护方法实现外观:

- 菜单项背景:悬停/按下时绘制内缩 2px 的圆角填充;MenuStrip 顶层项按窗体有效背景合成,下拉项按弹出层表面合成。
- 文字:启用项用主文本色、禁用项用禁用色;MenuStrip 顶层文字用 GDI+ 绘制(兼容 Mica),下拉文字用原生 GDI。
- 分隔线:两端内缩 8px 的主题色细线;箭头用次级文本色。
- MenuStrip 本体不画背景与边框(保持透明);其他 ToolStrip 画弹出层背景 + 描边。

### 示例

给原生 `MenuStrip` / `ToolStrip` / `ContextMenuStrip` 换 Fluent 渲染器有两种方式:

```csharp
// 方式一:逐个控件替换(只影响该控件及其下拉)
var nativeMenu = new MenuStrip();
nativeMenu.Renderer = new FluentToolStripRenderer();

var nativeToolStrip = new ToolStrip();
nativeToolStrip.Renderer = new FluentToolStripRenderer();

var nativeContext = new ContextMenuStrip();
nativeContext.Renderer = new FluentToolStripRenderer();
```

```csharp
// 方式二:全局替换,应用内所有 ToolStrip 系控件(含设计器拖出的)统一换肤
// 建议在 Program.Main 中、创建任何窗体之前设置
ToolStripManager.Renderer = new FluentToolStripRenderer();
```

### 备注

- 全局方式最省事,但设计器生成的 `MenuStrip` 若在属性里显式设置了 Renderer,会覆盖全局值,需删掉该行。
- 渲染器实时读取 `ApplicationThemeManager.Palette`,主题切换后重绘即生效,无需更换实例。
- 给原生控件换渲染器只改变外观;要获得透明菜单栏效果,还需把 `MenuStrip.BackColor` 设为 `Color.Transparent`(参考 `FluentMenuStrip` 的预设)。

## FluentColorTable

Fluent 配色表:继承 `ProfessionalColorTable`,把菜单/工具栏的各颜色槽位映射到 Fluent 主题令牌,供 `FluentToolStripRenderer` 使用,也可单独配给其他 Professional 渲染器。

### 属性

全部为重写的只读颜色属性,实时取自当前主题调色板(`P = ApplicationThemeManager.Palette`;Surface = `P.AcrylicBackgroundFillDefault`,Hover = SubtleFillSecondary 与 Surface 合成,Pressed = SubtleFillTertiary 与 Surface 合成):

| 属性 | 类型 | 返回值 | 说明 |
| --- | --- | --- | --- |
| MenuBorder | Color | P.SurfaceStrokeFlyout | 下拉菜单边框。 |
| MenuItemBorder | Color | Transparent | 菜单项边框(隐藏)。 |
| MenuItemSelected / MenuItemSelectedGradientBegin / MenuItemSelectedGradientEnd | Color | Hover | 悬停菜单项填充。 |
| MenuItemPressedGradientBegin / Middle / End | Color | Pressed | 按下菜单项填充。 |
| MenuStripGradientBegin / End | Color | Transparent | 菜单栏本体透明。 |
| ToolStripBorder | Color | Transparent | 工具栏边框(由渲染器另画)。 |
| ToolStripGradientBegin / Middle / End | Color | Transparent | 工具栏渐变(隐藏)。 |
| ToolStripDropDownBackground | Color | Surface | 下拉背景。 |
| ImageMarginGradientBegin / Middle / End | Color | Surface | 图标边距列背景。 |
| SeparatorDark | Color | DividerStrokeDefault 与 Surface 合成 | 分隔线。 |
| SeparatorLight | Color | Transparent | 分隔线亮边(隐藏)。 |
| CheckBackground | Color | P.SystemAccentColorPrimary | 勾选标记背景。 |
| CheckPressedBackground / CheckSelectedBackground | Color | P.AccentFillSecondary | 勾选标记按下/悬停背景。 |
| ButtonSelectedBorder | Color | Transparent | 工具栏按钮悬停边框(隐藏)。 |
| ButtonSelectedHighlight / ButtonSelectedGradientBegin / Middle / End | Color | Hover | 工具栏按钮悬停填充。 |
| ButtonPressedGradientBegin / Middle / End | Color | Pressed | 工具栏按钮按下填充。 |
| OverflowButtonGradientBegin / Middle / End | Color | Transparent | 溢出按钮渐变(隐藏)。 |

### 事件/方法

无自己声明的公开事件或方法。

### 示例

```csharp
// 通常无需直接使用:FluentToolStripRenderer 构造时已内置
var renderer = new FluentToolStripRenderer();

// 也可以把 Fluent 配色单独配给原生 Professional 渲染器
var toolStrip = new ToolStrip();
toolStrip.Renderer = new ToolStripProfessionalRenderer(new FluentColorTable());
Controls.Add(toolStrip);
```

### 备注

- 所有属性都是动态计算(每次读取都取当前调色板),因此浅色/深色切换即时生效。
- 半透明令牌已预先与弹出层表面合成为不透明色,避免 GDI 绘制出现 alpha 问题。