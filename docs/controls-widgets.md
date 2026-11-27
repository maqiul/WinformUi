# 小组件参考

本文档覆盖 WinformUi 中的八个小组件:`FluentIcon`(符号/图片图标呈现器)、`FluentPictureBox`(圆角图片框)、`FluentCodeBlock`(语法高亮代码块)、`FluentRichTextBox`( Fluent 富文本编辑/只读框)、`FluentScrollBar`(细长动态滚动条)、`FluentToolStrip`(主题化工具栏)、`FluentNotifyIcon`(托盘图标组件)与 `FluentToast`(应用内浮动通知)。全部位于命名空间 `WinformUi.Controls`,主题色随 `ApplicationThemeManager` 的当前调色板自动换肤;凡订阅主题的控件均在句柄创建时建立订阅、在 `Dispose` 中释放。属性、事件与默认值均以源码为准,以下示例片段均省略 `using` 行,可直接放入窗体或页面代码中。

## FluentIcon

轻量图标呈现器:在一个不可聚焦、不参与 Tab 的 16×16 默认小控件里绘制 Fluent System Symbol(常规或填充变体)、任意图标字体的原始字形字符,或一张图片,并按控件尺寸自动缩放、按主题着色。

### 属性

| 属性 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| Symbol | SymbolRegular | SymbolRegular.Empty | 常规变体 Fluent 符号;为 Empty 时回落到其他图标来源。 |
| FilledSymbol | SymbolFilled | SymbolFilled.Empty | 填充变体 Fluent 符号;非 Empty 时优先于 Symbol。 |
| Glyph | string | "" | 原始字形字符(可来自任意图标字体);仅在未设置符号和图片时使用。赋 null 按空字符串处理。 |
| Image | Image | null | 位图来源,居中并按等比(aspect-fit)缩放绘制;优先级高于所有字形来源。标记 RefreshProperties.Repaint。 |
| Foreground | Color | 跟随主题 | ForeColor 的别名包装器;对设计器隐藏(Browsable=false,不序列化),不显式赋值时始终跟随主题文字色。 |

绘制优先级为:Image > FilledSymbol > Symbol > Glyph;四者都未设置时只绘制空白。

### 示例

```csharp
// Fluent 符号图标(常规 + 填充)
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

// 原始字形字符
var glyphIcon = new FluentIcon
{
    Glyph = "\uE713",
    Size = new Size(16, 16),
    Location = new Point(68, 12)
};

// 图片来源:等比缩放居中
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

### 备注

- 默认尺寸 16×16;构造时设置 `TabStop = false` 并关闭 `ControlStyles.Selectable`,永远不可聚焦、不响应键盘。
- 字形字号按控件短边自动计算:`max(6, min(宽, 高)) × 0.75` 磅,因此改变 `Size` 即可缩放图标。
- 禁用(`Enabled = false`)时字形改用调色板 `Palette.TextFillDisabled` 着色。
- 字形使用内部缓存的共享字体绘制,用户代码不涉及其生命周期,不要尝试释放这些字体。
- 图片绘制启用 HighQualityBicubic 插值;`Image` 属性不会拥有传入图片的生命周期,控件 `Dispose` 后仍需由调用方释放 `Image`。
- 绘图前通过 `FluentGraphics.SetupGraphics` 统一设置抗锯齿等图形质量。

## FluentPictureBox

继承原生 `PictureBox` 的 Fluent 化图片框:构造时即把 `SizeMode` 设为 `Zoom`(等比缩放),背景在未显式设置时改为透明,并可选地以 `CornerRadius` 实现圆角裁剪加 1px 分隔色描边。

### 属性

| 属性 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| CornerRadius | int | 0 | 以 96 DPI 设备无关像素计的圆角半径;0 表示不裁剪、不画边框;负值钳制为 0。内部按控件当前 DPI 缩放。 |
| SizeMode | PictureBoxSizeMode | Zoom(构造预设) | 继承自 PictureBox;构造器预设等比缩放模式,可按需改回。 |
| BackColor | Color | Transparent(主题应用后) | 构造时若仍为 SystemColors.Control 则替换为透明,使图片悬浮于父容器表面。 |

其余属性(`Image`、`InitialImage`、`ErrorImage` 等)均为 `PictureBox` 原生能力,未做改动。

### 示例

```csharp
var avatar = new FluentPictureBox
{
    Image = Image.FromFile("avatar.jpg"),
    Size = new Size(96, 96),
    CornerRadius = 48,          // 圆形头像
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

### 备注

- `CornerRadius > 0` 时:用圆角矩形路径裁剪后调用基类绘制,再沿内缩 0.5px 的圆角矩形以 `Palette.DividerStrokeDefault` 画 1px 发丝边框;半径按 `ControlHelper.DpiOf(this) / 96f` 缩放,高 DPI 下视觉比例一致。
- 主题订阅在 `OnHandleCreated` 时建立,主题切换自动重绘;`Dispose` 时释放订阅。
- 显式设置过 `BackColor` 的实例不会被主题应用逻辑改成透明。
- 继承 PictureBox 的 Image 生命周期语义:请在使用完毕后自行释放赋入的 `Image`。

## FluentCodeBlock

继承 `FluentRichTextBox` 的只读、可选中、主题感知代码呈现控件:内置针对 XAML 与 C# 的正则着色器(注释/字符串/数字/关键字、标签/特性/实体分色),深浅主题各有一套颜色,并提供一键复制源码到剪贴板的方法。

### 属性

| 属性 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| Code | string | "" | 要显示的源码(即内部 Text 的包装);赋值自动重新着色。DefaultProperty,支持本地化。 |
| Language | SyntaxLanguage | SyntaxLanguage.Autodetect | 着色语言:`Autodetect`、`Xaml`、`CSharp`、`Plain`;切换立即重新着色。 |
| Text | string | — | 基类文本属性,对设计器隐藏(Browsable=false,不序列化);运行时读写仍有效,着色请用 Code。 |

`SyntaxLanguage` 枚举成员:`Autodetect`、`Xaml`、`CSharp`、`Plain`。继承自 FluentRichTextBox 的行为属性在构造时预设为 `ReadOnly = true`、`WordWrap = false`、`DetectUrls = false`,默认尺寸 420×180。

### 示例

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

### 备注

- 自动检测规则:源码 `TrimStart()` 后以 `<` 开头判为 XAML,否则按 C# 处理;文本为 null 时按 XAML。`Plain` 完全不着色,仅使用默认文字色。
- 着色基于正则匹配(关键字表为约 80 个 C# 关键字),是纯呈现层着色,不是完整解析器;重叠匹配以后者覆盖前者。
- 默认正文颜色随主题切换:深色主题用 Aqua(#9CDCFE),浅色主题用 CornflowerBlue(#6495ED);注释、字符串、关键字、标签、特性、数字各有独立的深/浅配色。
- 字体在首次创建时探测系统已安装字体,优先 Fira Code / Cascadia Code / JetBrains Mono,其次 Consolas,最后 Courier New(10 磅),结果进程内缓存;显式改 `Font` 可覆盖。
- 着色在文本变化、语言切换和主题切换时自动重跑,内部用防重入标志避免递归;句柄未创建时跳过。
- 只读通过基类 `ReadOnly` 实现:仍可框选、Ctrl+C 复制;`CopySourceToClipboard()` 复制的是未着色纯文本源码。
- 其余行为(DPI、圆角外壳、焦点强调条、主题订阅与释放)与 FluentRichTextBox 相同,见下一节。

## FluentRichTextBox

基于原生 `RichTextBox` 编辑引擎的 Fluent 富文本控件:外壳自绘圆角背景、悬停/禁用填充与焦点态 2px 强调色底边,内嵌无边框编辑区;`IsTextSelectionEnabled` 与参考实现同义,置 false 时彻底禁止用户框选(拖拽、双击取词、Shift 扩展、Ctrl+A 均被拦截),编辑与光标定位不受影响。

### 属性

| 属性 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| IsTextSelectionEnabled | bool | true | 用户选择模式:false 时禁止一切用户选择操作并收起现有选区;与 ReadOnly 相互独立。 |
| ReadOnly | bool | false | 只读;置 true 后禁止编辑,仍保留框选与复制。 |
| WordWrap | bool | true | 是否自动换行,直通内部 RichTextBox。 |
| DetectUrls | bool | false | 是否自动把 URL 识别为链接,直通内部控件。 |
| MaxLength | int | 0 | 最大字符数,0 表示不限制,直通内部控件。 |
| Text | string | "" | 纯文本内容;赋 null 按空字符串处理。DefaultEvent 为 TextChanged。 |
| Rtf | string | — | RTF 内容读写,对设计器隐藏;可整体替换富文本。 |
| SelectedText / SelectedRtf | string | — | 选区文本/RTF,读写均对设计器隐藏。 |
| SelectionStart / SelectionLength | int | — | 选区位置与长度,对设计器隐藏。 |
| SelectionFontValue / SelectionColorValue | Font / Color | — | 对选区应用字体/颜色的包装属性,对设计器隐藏。 |
| InnerRichTextBox | RichTextBox | 内部实例 | 只读,暴露内嵌原生控件以使用其余 RichTextBox API;对设计器隐藏。 |

### 事件

| 事件 | 说明 |
| --- | --- |
| LinkClicked | 点击 `DetectUrls` 或 RTF 链接时触发,转发自内部控件,参数为 LinkClickedEventArgs。 |
| TextChanged | 继承自 Control;内部编辑区文本变化时触发。 |

### 示例

```csharp
var editor = new FluentRichTextBox
{
    Location = new Point(16, 16),
    Size = new Size(320, 150),
    DetectUrls = true
};
editor.Text = "访问 https://example.com 了解更多。";
editor.LinkClicked += (s, e) => MessageBox.Show(e.LinkText);

// 只读呈现:可选可复制、不可编辑
var viewer = new FluentRichTextBox
{
    Location = new Point(16, 180),
    Size = new Size(320, 100),
    ReadOnly = true
};
viewer.AppendText("第一段\n");
viewer.AppendText("第二段\n");
viewer.SelectAll();

// 用 RTF 直接加载富文本
viewer.Rtf = @"{\rtf1\ansi 这是{\b 加粗}文本}";

Controls.Add(editor);
Controls.Add(viewer);
```

### 备注

- 除属性外还提供公开方法:`Select(int start, int length)`、`SelectAll()`、`Clear()`、`AppendText(string text)`;`Focus()` 被重写为直接把焦点转给内部编辑区。
- 默认尺寸 280×150,`MinimumSize` 为 60×34;外壳圆角使用 `FluentGraphics.ControlCornerRadius`,内嵌编辑区四周内缩 2px。
- 外壳填充按状态分层混合到父容器背景上:禁用 → `ControlFillDisabled`,获得焦点 → `ControlFillInputActive`,悬停 → `ControlFillSecondary`,常态 → `ControlFillDefault`;焦点且启用时底部再画 2px 强调色圆角裁剪条。
- 控件本体 `TabStop = false`,焦点由内部编辑区承载(内部 `TabStop = true`);点击外壳任意处即把焦点转给编辑区。
- CreateParams 追加 WS_CLIPCHILDREN,配合全窗体双缓冲自绘,避免外壳与子窗口互相撕裂。
- 内部编辑区的非客户区滚动条由 `FluentNcScrollBar` 在 WM_NCPAINT 中重绘为 Fluent 样式,主题切换时调用 `FluentNcScrollBar.Redraw` 刷新。
- 选择闸门按消息逐条过滤,只从 `WParam.ToInt64() & 0xFFFF` 取键码/鼠标状态位;x64 下 `WM_MOUSEACTIVATE`、`WM_CTLCOLOR*` 等消息的 wParam 是指针,直接 `ToInt32()` 会抛 `System.OverflowException`(1.4.1 修复)。
- `Font` 变化自动同步到内部控件,`Enabled` 变化同步只读文字色 `Palette.TextFillDisabled`;主题订阅在句柄创建时建立、Dispose 时释放。
- FluentCodeBlock 继承本控件并重写 Text 序列化、默认 WordWrap/DetectUrls 等,勿在 CodeBlock 上直接改这些预设。

## FluentScrollBar

细长 Fluent 滚动条(14 DIP 轨道):圆角滑块悬停加粗、方向箭头按钮悬停淡入,支持拖动、翻页点击、连按重复与滚轮;开启 `Dynamic` 后变为自动隐藏变体——静止超时后淡出、交互时淡入。

### 属性

| 属性 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| Orientation | Orientation | Vertical | 方向;切换时自动交换宽/高。 |
| Dynamic | bool | false | 自动隐藏模式:未悬停且无交互时全透明,交互时淡入,静止 Timeout 毫秒后淡出。 |
| Timeout | int | 1000 | 动态模式下淡出前的静止毫秒数;负值钳制为 0,实际定时器下限 50ms。 |
| Minimum | int | 0 | 最小值;若大于 Maximum 会连带抬升 Maximum。 |
| Maximum | int | 100 | 最大值;若小于 Minimum 会连带压低 Minimum。 |
| Value | int | 0 | 当前值;实际可用上限为 `Maximum - LargeChange + 1`,赋值自动钳制。带 Int32Converter。 |
| SmallChange | int | 1 | 单步量(箭头按钮/滚轮基数);最小钳制为 1。 |
| LargeChange | int | 10 | 翻页量,同时决定滑块长度比例;最小钳制为 1。 |

### 事件

| 事件 | 说明 |
| --- | --- |
| ValueChanged | Value 因任意途径(代码赋值、拖动、滚轮、按钮、翻页)实际改变时触发。 |
| Scroll | DefaultEvent;携带 ScrollEventArgs,拖动中为 ThumbTrack、松手为 EndScroll,滚轮为 SmallIncrement/SmallDecrement,翻页为 LargeIncrement/LargeDecrement/ThumbPosition,箭头为 Small*。 |

### 示例

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

// 横向 + 自动隐藏
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

### 备注

- 默认尺寸 14×100,`BackColor` 预设透明;构造时关闭 `Selectable` 样式且 `TabStop = false`,不提供键盘操作,纯鼠标交互。
- 全部几何尺寸(12px 箭头按钮、12px 轨道胶囊、6px/悬停 8px 滑块厚度、最小 24px 滑块长度)均以 96 DIP 为基准经 `Scale()` 按控件 DPI 换算。
- 悬停或拖动时:滑块加厚换色,两端箭头按钮( Chevron 字形)淡入显示;平时只显示胶囊轨道与滑块。
- 按住箭头按钮以 100ms 间隔连发单步;`MouseUp` 或离开控件即停止重复。
- 滚轮每格(120 delta)移动 `SmallChange × 3`,多格滚轮按格数放大。
- 动态模式由三个惰性 Timer 协作:16ms 淡入淡出(每帧 alpha 步进 26)、静止隐藏计时(≥50ms);Dispose 时三个定时器全部释放。
- 滑块位置线性映射自 `[Minimum, Maximum - LargeChange + 1]`;区间退化(最大值 ≤ 最小值)时固定停靠在轨道起点。

## FluentToolStrip

继承原生 `ToolStrip` 的主题化工具栏:按钮、切换按钮、下拉、分隔线、溢出等全部保留标准 ToolStrip 项模型,外观由内置 Fluent 渲染器接管(平面底色、悬停/按下高亮、底部分隔线)。

### 属性

本控件不新增公开属性,以下继承属性在构造时被预设:

| 属性 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| Dock | DockStyle | Top | 预设停靠顶部,可改。 |
| AutoSize | bool | true | 预设自适应高度。 |
| ImageScalingSize | Size | 16×16 | 预设图标尺寸,与 FluentIcon 体系一致。 |
| GripStyle | ToolStripGripStyle | Hidden | 预设隐藏拖动把手。 |
| Padding | Padding | 6,3,6,3 | 预设内边距。 |
| Renderer | ToolStripRenderer | 内置 Fluent 渲染器 | 构造时替换为 Fluent 渲染器(继承 ToolStripProfessionalRenderer 与统一色表,RoundedEdges=false);对设计器隐藏。 |

### 示例

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

// 放入表单(可先包一层 ToolStripContainer,或直接 Dock)
Controls.Add(strip);
```

### 备注

- 停靠在表单上的工具栏绘制自身混合后的 `ControlFillDefault` 底色,并在水平模式下沿底边画 1px `DividerStrokeDefault` 分隔线;弹出子菜单则沿用 FluentMenu 体系的弹出面渲染。
- 主题订阅在 `OnHandleCreated` 时建立;`OnParentChanged` 和 `OnItemAdded` 都会重新应用配色,新加项自动获得前景色;Dispose 释放订阅。
- 前景色统一取 `Palette.TextFillPrimary`,并逐项写入每个 `ToolStripItem.ForeColor`;项的选中/按下高亮由渲染器基于 `SubtleFillSecondary/SubtleTertiary` 与当前表面混合绘制。
- 标准功能(Overflow 溢出按钮、快捷键 Mnemonic、ImageList、布局方向)不受影响,按原生 ToolStrip 语义工作。
- 与其他 ToolStrip 家族一样支持放入 `ToolStripContainer` 或直接 `Controls.Add`;设计器中是工具箱可见控件(ToolboxItem=true)。

## FluentNotifyIcon

托盘(通知区域)图标组件:继承 `Component` 而非 Control,放在组件栏里使用,封装 Shell 托盘的图标、提示文字、右键上下文菜单、气泡提示,以及按鼠标键路由的点击事件。

### 属性

| 属性 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| TooltipText | string | "" | 悬停提示;超过 63 字符自动截断(Shell 限制),支持本地化。 |
| Icon | Icon | null | 托盘图标;赋 null 时只显示提示文字。 |
| Menu | FluentContextMenuStrip | null | 右键菜单;任何 ContextMenuStrip 均可用,但 FluentContextMenuStrip 保持主题外观。 |
| FocusOnLeftClick | bool | true | 左键单击时自动恢复并激活第一个打开的窗体(跳过通知宿主窗)。 |
| MenuOnRightClick | bool | true | 右键是否弹出 Menu;切换即时挂接/摘除 ContextMenuStrip。 |
| BalloonTimeout | TimeSpan | 00:00:03 | 无标题/内容重载时气泡的默认显示时长。 |
| Visible | bool | false | 是否在托盘中显示;与 Register/Unregister 等价。 |
| IsRegistered | bool | — | 只读,当前是否已注册显示;对设计器隐藏。 |

### 方法

| 方法 | 说明 |
| --- | --- |
| Register() | 把图标加入托盘(Visible = true)。 |
| Unregister() | 从托盘移除(Visible = false)。 |
| SetIcon(Bitmap bitmap) | 便捷方法:由位图构建 Icon 并赋值;会释放此前非系统 Application 图标的旧 Icon。bitmap 为 null 时清空图标。 |
| ShowBalloonTip(TimeSpan timeout) | 显示空白气泡。 |
| ShowBalloonTip(TimeSpan timeout, string title, string message, ToolTipIcon icon = Info) | 显示带标题/内容/图标的系统气泡提示。 |

### 事件

| 事件 | 说明 |
| --- | --- |
| LeftClick / LeftDoubleClick | 左键单击/双击(委托类型 RoutedNotifyIconEventHandler);单击默认附带主窗恢复激活。 |
| RightClick / RightDoubleClick | 右键单击/双击;右键同时按 MenuOnRightClick 弹出菜单。 |
| MiddleClick / MiddleDoubleClick | 中键单击/双击。 |
| BalloonTipShown / BalloonTipClosed / BalloonTipClicked | 气泡显示、关闭、被点击,转发自底层 NotifyIcon。 |

### 示例

```csharp
// 通常声明为窗体字段,并手动加入 components 容器以获得自动 Dispose
var tray = new FluentNotifyIcon
{
    TooltipText = "WinformUi 示例程序",
    Icon = SystemIcons.Application,
    FocusOnLeftClick = true
};

var menu = new FluentContextMenuStrip();
menu.Items.Add("退出", null, (s, e) => Application.Exit());
tray.Menu = menu;

components.Add(tray); // 交给容器自动 Dispose

tray.LeftDoubleClick += (s, e) => Console.WriteLine("双击托盘图标");
tray.ShowBalloonTip(TimeSpan.FromSeconds(5), "提示", "程序已在后台运行", ToolTipIcon.Info);
tray.Register();
```

### 备注

- 作为 Component 不会随窗体自动释放:本类只有无参构造,请像上例那样声明字段并 `components.Add(tray)`,或手动 `Dispose`;Dispose 时自动隐藏图标(Visible = false)并释放内部 NotifyIcon。
- `SetIcon` 通过 `Icon.FromHandle(bitmap.GetHicon())` 构建图标,该句柄不由本组件释放,传入的 `Bitmap` 也请自行管理生命周期。
- 左键恢复主窗逻辑:遍历 `Application.OpenForms`,跳过未创建句柄的窗体和 `FluentToastHost`,将第一个命中的窗体从最小化还原并 Activate,只处理一个窗体。
- `TooltipText` 写入前统一截断到 63 字符,读回的始终是完整原值。
- 事件委托 `RoutedNotifyIconEventHandler(object sender, EventArgs e)` 不区分鼠标坐标,只按鼠标键路由。
- 默认不可见(`Visible = false`),必须显式 `Register()` 或设置 `Visible = true` 才出现在托盘。

## FluentToast

应用内浮动通知:`FluentToast` 本身是纯数据+展示的 sealed 辅助类(不是 Control),`Show()` 后由内部 `FluentToastHost`(无边框顶层窗体)承载卡片,从主屏工作区右上角滑入堆叠,悬停暂停计时,超时后淡出关闭。

### 属性(FluentToast)

| 属性 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| Title | string | null | 标题;与 Message 同时非空时卡片为双行高(74 DIP),单行高 50 DIP。 |
| Message | string | null | 正文字符串。 |
| Severity | InfoBarSeverity | InfoBarSeverity.Informational | 严重级别,决定图标、圆点颜色与背景:`Informational`、`Success`、`Warning`、`Error`。 |
| Duration | TimeSpan | 00:00:04 | 自动消失前的停留时长;实际下限 500ms;悬停期间计时暂停。 |

### 方法

| 方法 | 说明 |
| --- | --- |
| Show() | 实例方法:显示本条通知,返回宿主窗口 FluentToastHost(可用于高级处理,如提前关闭)。 |
| static Show(string title, string message, InfoBarSeverity severity = Informational, TimeSpan? duration = null) | 静态快捷方法:一条语句构造并显示,返回宿主 FluentToastHost。 |

### 示例

```csharp
// 静态快捷方式
FluentToast.Show("已保存", "所有更改都已写入磁盘。", InfoBarSeverity.Success);

// 实例方式:自定义时长与级别
var toast = new FluentToast
{
    Title = "磁盘空间不足",
    Message = "还剩 120 MB,建议清理后继续。",
    Severity = InfoBarSeverity.Warning,
    Duration = TimeSpan.FromSeconds(8)
};
toast.Show();

// 错误级 + 拿到宿主窗
FluentToastHost host = FluentToast.Show("操作失败", "无法连接到服务器。", InfoBarSeverity.Error);
```

### 备注

- 宿主 `FluentToastHost` 是无边框、`TopMost`、不进任务栏的透明启动窗体(`ShowWithoutActivation`,不抢焦点),8 DIP 圆角裁剪,1px `CardStrokeDefaultSolid` 描边;宽 340 DIP,高随标题/正文有无取 74 或 50 DIP,全部按 DPI 缩放。
- 位置固定在主显示器工作区右上角,多条通知自上而下堆叠、间距 12 DIP;新通知插到最前,任一关闭后其余自动重新布局。
- 淡入以每 30ms 提升 0.15 不透明度直到 1;关闭(超时或点 ×)后每 30ms 降 0.1 淡出,归零时从管理器移除并 Close。
- 悬停整个卡片会暂停自动关闭计时,移开恢复;右上 22×22 DIP 关闭按钮带悬停高亮,点击立即进入淡出。
- 严重级别配色来自调色板:Informational → SystemFillAttention 系,Success → SystemFillSuccess 系,Warning → SystemFillCaution 系,Error → SystemFillCritical 系;Warning 在浅色主题下字形改用深红以保证对比度。
- 文本超宽自动按行截断加省略号(LineLimit + EllipsisCharacter);仅当标题与正文都非空时才双行排版,否则单行。
- 主题切换通过主题订阅即时重绘;Dispose 时释放自动关闭计时器、淡出计时器与订阅。
- `FluentNotifyIcon` 恢复主窗时会把 Toast 宿主窗排除在候选之外,避免"激活"了一个通知窗。
