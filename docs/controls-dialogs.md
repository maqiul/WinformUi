# 对话框与弹出层控件

本篇介绍 WinformUi 的对话框与弹出层:`FluentMessageBox`(Fluent 风格消息框)、`FluentContentDialog`(窗体内模态内容对话框)、`FluentFlyout`(锚定弹出层)、`FluentPopupForm`(无边框圆角弹出窗体)和 `FluentToolTip`(Fluent 风格工具提示)。均位于命名空间 `WinformUi.Controls`。示例代码省略 `using` 行,使用时请引入 `System`、`System.Drawing`、`System.Windows.Forms`、`WinformUi.Controls`、`WinformUi.Theming` 等命名空间。

## FluentMessageBox

Fluent 消息框:静态类,弹出一个无边框、圆角、随主题换肤的模态对话框,顶部可选严重级别图标与标题,底部分隔线下方排布按钮,同步返回 `DialogResult`。

### 方法

| 重载 | 说明 |
| --- | --- |
| `static DialogResult Show(string text)` | 只显示正文,无标题无图标,单个 OK 按钮。 |
| `static DialogResult Show(string text, string title)` | 带标题,单个 OK 按钮。 |
| `static DialogResult Show(IWin32Window owner, string text, string title, MessageBoxButtons buttons, MessageBoxIcon icon)` | 完整重载。`owner` 为 null 时以当前活动窗体为宿主(无宿主则按屏幕居中);`buttons` 决定按钮组合;`icon` 决定标题左侧的级别图标。 |

参数取值均为 WinForms 标准枚举:

| MessageBoxButtons | 按钮(左→右) | 主按钮 |
| --- | --- | --- |
| OK | OK | OK |
| OKCancel | OK、Cancel | OK |
| YesNo | Yes、No | Yes |
| YesNoCancel | Yes、No、Cancel | Yes |
| RetryCancel | Retry、Cancel | Retry |
| AbortRetryIgnore | Abort、Retry、Ignore | Retry |

| MessageBoxIcon | 图标 | 颜色 |
| --- | --- | --- |
| None | 不绘制 | — |
| Information | 信息符号 | `SystemFillAttention` |
| Warning | 警告三角 | `SystemFillCaution` |
| Error | 错误圆圈 | `SystemFillCritical` |
| Question | 问号 | `SystemFillAttention` |

### 示例

```csharp
// 最简单的提示
FluentMessageBox.Show("已保存到剪贴板");

// 确认对话框
if (FluentMessageBox.Show(null, "删除后无法恢复,确定继续吗?", "删除记录",
        MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
{
    DeleteSelected();
}

// 父窗体拥有焦点与位置
DialogResult result = FluentMessageBox.Show(this, "网络请求失败,是否重试?", "离线",
    MessageBoxButtons.RetryCancel, MessageBoxIcon.Error);
```

### 备注

- 对话框为 `FormBorderStyle.None` + 圆角 Region,`ShowInTaskbar = false`、`TopMost = true`,
  `StartPosition = CenterParent`;固定宽 440 像素,高度按正文换行后的实际行数计算。
- 主按钮使用 `ControlAppearance.Primary` 并成为 `AcceptButton`(回车触发);带 `DialogResult.Cancel`
  的按钮成为 `CancelButton`(Esc 触发)。按钮文字为英文 OK/Cancel/Yes/No/Retry/Abort/Ignore,
  需要本地化文案时请改用 `FluentContentDialog`。
- `Show` 是阻塞的模态调用,返回值即用户点击的按钮;内部对话框实例在返回前已 `Dispose`。
- 标题与图标都为空时不绘制头部,对话框更紧凑;标题非空但图标为 None 时只画标题文字。

## FluentContentDialog

Fluent 内容对话框:在窗体客户区内铺一层烟色遮罩,中央显示圆角对话框面板,底部最多三个按钮(主按钮/次按钮/关闭按钮)。

注意:它是普通类,**不是 Form 也不是 Control**,`Show` 不阻塞、不返回 `DialogResult`;结果通过按钮事件获取。

### 属性

| 属性 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| Title | string | null | 对话框标题(Subtitle 字体)。 |
| Content | Control | null | 内容控件,置于标题下方;宽度被强制为面板宽 - 48,高度取 `max(Content.Height, 60)`。 |
| PrimaryButtonText | string | null | 主按钮(强调色)文本;为空则不创建该按钮。 |
| SecondaryButtonText | string | null | 次按钮文本;为空则不创建该按钮。 |
| CloseButtonText | string | "Close" | 关闭按钮文本;置为空字符串可隐藏该按钮。 |

### 事件/方法

| 成员 | 签名 | 说明 |
| --- | --- | --- |
| PrimaryButtonClick | event EventHandler | 点击主按钮时触发,随后自动 Close。 |
| SecondaryButtonClick | event EventHandler | 点击次按钮时触发,随后自动 Close。 |
| CloseButtonClick | event EventHandler | 点击关闭按钮时触发,随后自动 Close。 |
| Closed | event EventHandler | 对话框关闭(任意按钮或点击遮罩)后触发。 |
| Show | void Show(Form owner) | 在指定窗体内显示对话框;立即返回,不阻塞。重复调用或 owner 为 null 时忽略。 |
| Close | void Close() | 移除并销毁遮罩,触发 Closed。 |

### 示例

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

// 通过事件取"结果":主按钮里读取输入,在 Closed 中统一收尾
dialog.PrimaryButtonClick += (s, e) => Console.WriteLine("创建项目:" + input.Text);
dialog.SecondaryButtonClick += (s, e) => Console.WriteLine("用户取消");
dialog.Closed += (s, e) => Console.WriteLine("对话框已关闭");

dialog.Show(this);   // this 为当前 Form;非阻塞,代码继续往下执行

// 也可以在业务代码中主动关闭
// dialog.Close();
```

### 备注

- 点击烟色遮罩(对话框以外区域)等同于轻关闭:直接 `Close()`,只触发 `Closed`,不触发任何按钮事件。
- 三个按钮点击后都会自动关闭,无需在事件里再调 `Close()`;需要"校验失败不关闭"的场景请改用自定义窗体。
- `Show` 会通过反射把 Content 的 `DoubleBuffered` 设为 true,以避免 Mica 背景下内容控件出现透明破洞;Content 会被 reparent 到对话框面板,`Close` 时随面板一起被 Dispose,每次弹出请新建内容控件。

## FluentFlyout

Fluent 弹出层:把任意内容控件放进圆角弹出窗体,锚定在某个控件下方显示,失焦自动关闭。

### 属性

| 属性 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| Content | Control | null | 弹出层内容;弹出窗体客户区尺寸为内容尺寸四周各加 8px。 |

### 事件/方法

| 成员 | 签名 | 说明 |
| --- | --- | --- |
| Closed | event EventHandler | 弹出窗体关闭(含轻关闭)后触发。 |
| ShowAt | void ShowAt(Control anchor) | 在锚点控件下方弹出;已打开、Content 或 anchor 为 null 时忽略。 |
| Close | void Close() | 关闭弹出层。 |

### 示例

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

### 备注

- 内部使用 `FluentPopupForm` 承载:点击弹出层以外区域(窗体失活)即自动关闭,Esc 也可关闭。
- 屏幕下方放不下时自动翻转到锚点上方,并保证不超出屏幕工作区右缘。
- 内容控件会被 reparent 到弹出窗体;窗体关闭会 Dispose 内容,复用内容前请注意生命周期。

## FluentPopupForm

无边框圆角弹出窗体:ComboBox、AutoSuggestBox、各类选择器共用的弹出宿主,带 Fluent 弹出层表面(圆角、描边、阴影)。

### 属性

| 属性 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| NoActivate | bool | false | 为 true 时弹出层不抢占焦点(拦截 WM_MOUSEACTIVATE),且失活不自动关闭;用于输入时保持焦点的建议列表。 |

### 事件/方法

| 成员 | 签名 | 说明 |
| --- | --- | --- |
| ShowBelow | void ShowBelow(Control anchor, int popupHeight, int popupWidth = 0) | 在锚点下方定位并以锚点所在窗体为 owner 非模态显示;popupWidth 为 0 时取 `max(anchor.Width, 120)`。 |

其余为继承自 `Form` 的成员(`Show`、`Close`、`FormClosed` 等)。

### 示例

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

### 备注

- 构造即设定:无边框、不显示任务栏、`TopMost = true`、手动定位、DPI 缩放、类阴影(CS_DROPSHADOW),背景为 `AcrylicBackgroundFillDefault`。
- 默认(`NoActivate = false`)失活即 `Close()`,按 Esc 也会关闭;显示与尺寸变化时自动应用圆角 Region。
- `ShowBelow` 的 popupHeight 是窗体总高度;下方空间不足时自动改为锚点上方弹出。

## FluentToolTip

Fluent 工具提示:自绘的圆角弹出层外观,替代系统默认的黄色方框 ToolTip。

### 属性

无自己声明的新公开属性;继承 `ToolTip` 的全部属性。构造函数已预设:`OwnerDraw = true`、`InitialDelay = 400`、`ReshowDelay = 100`、`AutoPopDelay = 8000`,可按需覆盖。

### 事件/方法

无自己声明的新公开事件或方法;内部已挂接继承的 `Popup`(按 DPI 精确测量文本尺寸)与 `Draw`(绘制圆角表面)事件。日常使用继承的 `SetToolTip(Control control, string caption)` 等方法即可。

### 示例

```csharp
var toolTip = new FluentToolTip();

var saveButton = new FluentButton { Text = "保存", Appearance = ControlAppearance.Primary, AutoSize = true };
toolTip.SetToolTip(saveButton, "保存当前文档(Ctrl+S)");

var openButton = new FluentButton { Text = "打开", AutoSize = true };
toolTip.SetToolTip(openButton, "工具提示会自动跟随当前主题。");

// 可选:调整延时
toolTip.InitialDelay = 300;
toolTip.AutoPopDelay = 5000;
```

### 备注

- 尺寸在 Popup 时按关联控件的 DPI 用 GDI 测量,绘制用 GDI+,两者一致保证文本恰好放下;不要自行订阅 Draw 覆盖外观。
- 外观取自当前主题调色板(`AcrylicBackgroundFillDefault` 表面 + `SurfaceStrokeFlyout` 描边),主题切换后自动生效。
- 一个实例可服务多个控件;文本为单行,不支持多行换行。