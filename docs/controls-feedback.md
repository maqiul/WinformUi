# 反馈与进度控件

本篇介绍 WinformUi 中用于向用户传达状态与结果的控件:`FluentProgressBar`、`FluentProgressRing`、`FluentInfoBar`、`FluentBadge`、`FluentInfoBadge`、`FluentSnackbar`、`FluentErrorProvider`、`FluentLoadingScreen`、`FluentTaskbarProgress`。所有类型位于命名空间 `WinformUi.Controls`,配色随 `ApplicationThemeManager` 当前主题自动切换。示例代码省略 `using` 行,使用时请引入 `System`、`System.Drawing`、`System.Windows.Forms`、`WinformUi.Controls`、`WinformUi.Theming` 等命名空间。

## FluentProgressBar

Fluent 风格进度条:4 像素高的圆角轨道,支持确定进度与不确定(循环滚动)两种模式。

### 属性

| 属性 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| Minimum | double | 0 | 进度范围下限。 |
| Maximum | double | 100 | 进度范围上限。 |
| Value | double | 0 | 当前进度,自动钳制到 [Minimum, Maximum] 区间。 |
| IsIndeterminate | bool | false | 为 true 时播放循环滚动动画(内部 16ms 定时器),忽略 Value。 |

### 事件/方法

无自己声明的公开事件或方法;通过上述属性驱动。构造函数默认尺寸 200×8。

### 示例

```csharp
var progress = new FluentProgressBar
{
    Width = 320,
    Minimum = 0,
    Maximum = 100,
    Value = 40
};
Controls.Add(progress);

// 任务进度未知时切换为不确定模式
progress.IsIndeterminate = true;

// 拿到进度后切回确定模式
progress.IsIndeterminate = false;
progress.Value = 75;
```

### 备注

- 控件禁用(`Enabled = false`)时只绘制轨道,不绘制进度填充。
- 轨道高度固定为 4px 并在控件高度内垂直居中,增加 Height 不会加粗轨道。
- 进度填充使用系统强调色(`Palette.SystemAccentColorPrimary`)。

## FluentProgressRing

Fluent 风格进度环:旋转的强调色圆弧,表示"正在处理、进度未知"。

### 属性

| 属性 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| IsActive | bool | true | 为 true 时旋转动画运行;为 false 时停止定时器且不绘制任何内容。 |

### 事件/方法

无自己声明的公开事件或方法。构造函数默认尺寸 32×32;句柄创建时若 `IsActive` 为 true 会自动启动动画。

### 示例

```csharp
var ring = new FluentProgressRing
{
    Size = new Size(48, 48),
    Location = new Point(24, 24)
};
Controls.Add(ring);

// 工作完成后停止(停止后控件不绘制圆弧)
ring.IsActive = false;
```

### 备注

- 圆弧粗细随尺寸自适应:`max(2, min(Width, Height) / 10)`。
- 只有不确定模式,没有百分比进度;需要数值进度请用 `FluentProgressBar`。

## FluentInfoBar

Fluent 信息栏:按严重级别着色的横幅,含圆形图标、标题、正文和可选关闭按钮。

### 属性

| 属性 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| Severity | InfoBarSeverity | Informational | 严重级别,决定背景色、图标圆底色与图标字形。 |
| Title | string | "" | 加粗标题;与 Message 同时存在时分两行显示。 |
| Message | string | "" | 正文文本;单独存在时垂直居中显示。 |
| IsClosable | bool | true | 是否显示右侧关闭按钮。 |

`InfoBarSeverity` 枚举(定义于 `WinformUi.Controls`):

| 值 | 含义 |
| --- | --- |
| Informational | 信息(注意力色) |
| Success | 成功(绿色系) |
| Warning | 警告(警示色) |
| Error | 错误(严重色) |

### 事件/方法

| 成员 | 签名 | 说明 |
| --- | --- | --- |
| CloseButtonClick | event EventHandler | 点击关闭按钮时触发(随后控件自动隐藏)。 |
| Closed | event EventHandler | 关闭按钮点击导致 `Visible = false` 之后触发。 |

### 示例

```csharp
var bar = new FluentInfoBar
{
    Severity = InfoBarSeverity.Warning,
    Title = "磁盘空间不足",
    Message = "剩余空间低于 10%,建议清理临时文件。",
    IsClosable = true,
    Width = 640
};
bar.CloseButtonClick += (s, e) => Console.WriteLine("用户关闭了提示");
Controls.Add(bar);
```

### 备注

- 构造函数默认尺寸 400×52;文本超宽时以省略号截断,不换行。
- 点击关闭按钮只会把 `Visible` 置为 false,不会从父容器移除控件;如需彻底移除请在 `Closed` 事件中自行 `Controls.Remove` 并 `Dispose`。
- 浅色主题下 Warning 图标使用深色字形以保证对比度,其余级别为白色字形。

## FluentBadge

Fluent 徽标:小胶囊形状的标签,用于展示 "New"、"Beta" 等短文本。

### 属性

| 属性 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| Content | string | "1" | 胶囊内显示的文本。 |
| Appearance | ControlAppearance | Primary | 外观变体,决定填充色与前景色。 |

### 事件/方法

无自己声明的公开事件或方法。构造函数默认尺寸 24×18。

### 示例

```csharp
var row = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 32 };
row.Controls.Add(new FluentBadge { Content = "New" });
row.Controls.Add(new FluentBadge { Content = "Preview", Appearance = ControlAppearance.Info });
row.Controls.Add(new FluentBadge { Content = "Beta", Appearance = ControlAppearance.Caution });
row.Controls.Add(new FluentBadge { Content = "Deprecated", Appearance = ControlAppearance.Danger });
Controls.Add(row);
```

### 备注

- `ControlAppearance` 可选值:Secondary、Primary、Info、Success、Caution、Danger、Transparent、Dark、Light。
- Secondary 外观特殊处理为强填充底色 + 反色文字。
- 胶囊圆角为高度的一半;文本变长时请自行加宽 Width,控件不自动测量。

## FluentInfoBadge

Fluent 信息徽标:严重级别着色的小圆点(Value ≤ 0)或数字胶囊(Value > 0),常用于导航项角标。

### 属性

| 属性 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| Value | int | 0 | 0 或负数绘制圆点;正数绘制数字胶囊,并按位数自动调整尺寸。 |
| Severity | InfoBadgeSeverity | Attention | 严重级别,决定填充色。 |

`InfoBadgeSeverity` 枚举(定义于 `WinformUi.Controls`):

| 值 | 含义 |
| --- | --- |
| Attention | 注意力(默认,强调色系) |
| Informational | 信息 |
| Success | 成功 |
| Caution | 警示 |
| Critical | 严重 |

### 事件/方法

无自己声明的公开事件或方法。构造函数默认尺寸 16×16。

### 示例

```csharp
// 未读小红点
var dot = new FluentInfoBadge();

// 数字角标
var count = new FluentInfoBadge { Value = 12, Severity = InfoBadgeSeverity.Critical };

// 成功状态数字
var done = new FluentInfoBadge { Value = 8, Severity = InfoBadgeSeverity.Success };
```

### 备注

- 设置 `Value` 会自动改尺寸:正数为 `max(18, 10 + 位数×7)` × 16 的胶囊,否则为 10×10 的圆点;不要再手动固定 Size。
- 数字文本使用反色(`Palette.TextFillInverse`)居中绘制。

## FluentSnackbar

Fluent Snackbar:窗体底部的瞬时通知条,可带一个操作按钮,超时自动隐藏。

### 属性

| 属性 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| Title | string | "" | 加粗标题;与 Message 同时存在时分两行显示。 |
| Message | string | "" | 正文文本。 |
| ActionText | string | "" | 右侧操作按钮文本;为空则不显示按钮。 |
| Timeout | int | 4000 | 自动隐藏延时(毫秒),设置时钳制为不小于 500。 |

### 事件/方法

| 成员 | 签名 | 说明 |
| --- | --- | --- |
| ActionClick | event EventHandler | 点击操作按钮区域时触发(不会自动隐藏)。 |
| Hidden | event EventHandler | 调用 `HideSnackbar()`(含超时自动隐藏)后触发。 |
| ShowSnackbar | void ShowSnackbar() | 显示控件、置顶并重启超时计时。 |
| HideSnackbar | void HideSnackbar() | 停止计时、隐藏控件并触发 Hidden。 |
| Show(静态) | static FluentSnackbar Show(Form owner, string title, string message, string actionText = null) | 在窗体底部居中创建并显示一条 Snackbar,返回实例;隐藏后自动从窗体移除并 Dispose。 |

### 示例

```csharp
// 一行弹出(推荐):底部居中,4 秒后自动消失并自清理
FluentSnackbar.Show(this, "消息已发送", "你的消息已送达。", "撤销");

// 需要响应操作按钮时,接住返回的实例
var snackbar = FluentSnackbar.Show(this, "已删除 3 项", "", "撤销");
snackbar.ActionClick += (s, e) =>
{
    // 在这里执行撤销逻辑
    snackbar.HideSnackbar();
};

// 也可以手动放置并控制显示时机
var manual = new FluentSnackbar
{
    Title = "同步完成",
    Message = "共同步 128 个文件。",
    Timeout = 6000,
    Location = new Point(16, ClientSize.Height - 72),
    Anchor = AnchorStyles.Bottom | AnchorStyles.Left
};
Controls.Add(manual);
manual.ShowSnackbar();
```

### 备注

- 构造函数默认尺寸 420×56;静态 `Show` 使用 `AnchorStyles.Bottom` 锚定在窗体底部上方 16px 处。
- 静态 `Show` 返回的实例在 Hidden 后已被移除并 Dispose,不要在隐藏后再访问它。
- 点击操作按钮只触发 `ActionClick`,是否隐藏由你决定(通常调用 `HideSnackbar()`)。

## FluentErrorProvider

Fluent 验证错误组件:继承 `Component`,不是可视 `Control`,也不是 WinForms 原生 `ErrorProvider` 那样的 `IExtenderProvider`。它在关联控件的**客户区内部**绘制严重色边框与右上角错误图标,并通过 Fluent 风格提示消息提供错误详情。

适合把业务校验结果显示在输入框、下拉框、掩码输入框等控件上。它只负责显示错误,不执行校验、不增加“Error”扩展属性,也不会自动阻止按钮点击或保存操作。

### 快速开始

下面是一个包含名称与邮箱两个字段、点击“保存”时校验的完整窗体。`FluentErrorProvider` 由窗体持有并显式释放,不需要加入 `Controls` 集合。

```csharp
using System;
using System.Drawing;
using System.Windows.Forms;
using WinformUi.Controls;

public sealed class ValidationDemoForm : FluentWindow
{
    private readonly FluentErrorProvider _errors = new FluentErrorProvider();

    private readonly FluentTextBox _name = new FluentTextBox
    {
        PlaceholderText = "请输入名称",
        Location = new Point(24, 24),
        Size = new Size(260, 32)
    };

    private readonly FluentTextBox _email = new FluentTextBox
    {
        PlaceholderText = "name@example.com",
        Location = new Point(24, 68),
        Size = new Size(260, 32)
    };

    public ValidationDemoForm()
    {
        Text = "ErrorProvider demo";
        ClientSize = new Size(420, 180);

        var save = new FluentButton
        {
            Text = "保存",
            Appearance = ControlAppearance.Primary,
            Location = new Point(24, 120),
            Size = new Size(88, 32)
        };

        Controls.AddRange(new Control[] { _name, _email, save });
        _name.TextChanged += (s, e) => _errors.ClearError(_name);
        _email.TextChanged += (s, e) => _errors.ClearError(_email);
        save.Click += OnSaveClick;
        _errors.ErrorChanged += OnErrorChanged;
    }

    private void OnSaveClick(object sender, EventArgs e)
    {
        // 示例只演示显示错误;实际项目中应仅在业务数据通过后保存。
        _errors.ClearErrors();

        if (string.IsNullOrWhiteSpace(_name.Text))
        {
            _errors.SetError(_name, "名称不能为空");
        }
        else if (_name.Text.Trim().Length > 30)
        {
            _errors.SetError(_name, "名称不能超过 30 个字符");
        }

        string email = (_email.Text ?? string.Empty).Trim();
        if (email.Length == 0)
        {
            _errors.SetError(_email, "邮箱不能为空");
        }
        else if (email.IndexOf('@') <= 0 || email.LastIndexOf('.') <= email.IndexOf('@') + 1)
        {
            _errors.SetError(_email, "请输入有效的邮箱地址");
        }
    }

    private void OnErrorChanged(object sender, ErrorChangedEventArgs e)
    {
        Console.WriteLine(e.Control.Name + ": " + _errors.GetError(e.Control));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _errors.Dispose();
        }

        base.Dispose(disposing);
    }
}
```

这个示例的邮箱判断只用于演示错误展示,不是完整的邮件地址解析器。生产环境可使用项目的邮箱校验规则或成熟库。

### 属性

| 属性 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| Icon | Image | `SystemIcons.Error` 生成的位图 | 错误图标。设为 `null` 时仍绘制错误边框,只隐藏图标。 |
| IconPadding | int | `2` | 图标距关联控件客户区边缘的内边距,单位为像素;不能为负数。 |
| BlinkStyle | ErrorBlinkStyle | `BlinkIfDifferentError` | 图标闪烁策略,使用原生 `System.Windows.Forms.ErrorBlinkStyle` 枚举。 |

设置自定义 `Icon` 后,组件只引用图像,不会释放调用方传入的 `Image`。请由设置图像的一方管理其生命周期。图标按原始宽高绘制,高度超过关联控件客户区高度时会被压缩;不会预留布局空间。

### 方法

| 方法 | 说明 |
| --- | --- |
| `void SetError(Control control, string message)` | 添加、替换或清除错误。`null` 和空字符串都会清除;纯空白字符串仍是一条有效错误消息。 |
| `string GetError(Control control)` | 返回指定控件的错误消息;无记录时返回 `null`。 |
| `void ClearError(Control control)` | 调用 `SetError(control, null)`。 |
| `void ClearErrors()` | 清除当前所有记录;每个实际被清除的控件分别触发一次 `ErrorChanged`。 |

`control` 为 `null` 时,`SetError` 和 `GetError` 抛出 `ArgumentNullException`。组件释放后,`SetError`、`Icon`、`IconPadding`、`BlinkStyle` 的设置操作抛出 `ObjectDisposedException`;`GetError` 读取空字典返回 `null`,`ClearError` 因空消息且无记录而直接返回,`ClearErrors` 直接返回。

### 事件

| 成员 | 签名 | 说明 |
| --- | --- | --- |
| ErrorChanged | `event EventHandler<ErrorChangedEventArgs>` | 某个控件实际新增、修改或清除错误后触发。 |
| ErrorChangedEventArgs.Control | `Control` | 只读,返回错误发生变化的控件。 |

事件语义需要特别注意:

- 第一次设置非空消息、把消息 A 改成消息 B、把已有消息清空,都会触发事件。
- 重复设置相同消息,或对从未设置错误的控件调用清除,不会触发。
- `ClearErrors()` 按控件逐条触发;没有错误时不触发。
- 关联控件被 `Dispose` 时,对应记录被静默移除,不触发 `ErrorChanged`。
- 释放 `FluentErrorProvider` 本身会移除全部显示,也不触发 `ErrorChanged`。

如果需要在错误清除后执行“保存并关闭”,应检查业务数据是否有效,或读取 `GetError`,不要只依赖事件次数。

### ErrorBlinkStyle 行为

| 值 | 行为 |
| --- | --- |
| NeverBlink | 图标始终显示,不启动内部定时器。 |
| BlinkIfDifferentError | 默认值。已有错误的消息发生变化时,图标短暂闪烁约 4 个 500ms 节拍,然后恢复显示。首次设置错误不闪烁。 |
| AlwaysBlink | 只要存在错误就持续闪烁。 |

修改 `BlinkStyle` 会重置可见状态和闪烁计数。`Icon = null` 时没有可闪烁图标,但错误边框仍然显示。

### 设计器使用

1. 从工具箱把 `FluentErrorProvider` 拖到窗体上;它出现在组件栏,不会占据窗体客户区。
2. 在“属性”窗口设置 `Icon`、`IconPadding`、`BlinkStyle`。
3. 在代码中调用 `SetError` 关联运行时控件。当前组件不提供设计器中的控件选择器或 `Error` 扩展属性,也不会序列化运行时的错误文本。

设计器模式下关联控件不会挂接运行时 `Paint` 处理器,因此设计器不会显示错误边框。运行窗体后才会显示。

### 常见坑

- **它不是 `ErrorProvider` 的视觉换皮。** 原生 `ErrorProvider` 通常在控件外侧工作,本组件在客户区内绘制;不要按原生扩展属性模型编写代码。
- **图标和边框会覆盖内容。** 输入框右侧如果放密码显示按钮、清除按钮或状态图标,需要为错误图标预留位置,避免重叠。增加 `IconPadding` 只是调整偏移,不会改变控件布局。
- **提示文本会占用原 ToolTip 文本。** 内部使用 `FluentToolTip.SetToolTip(control, message)` 设置消息,清除错误时再设为 `null`;控件原来的 ToolTip 文本不会被保存和恢复。同一控件不要同时依赖两套 ToolTip 文本。
- **一个错误入口只保留一条消息。** 同一控件再次 `SetError` 会替换旧消息;如果需要展示多条错误,请先在业务层合并成一条字符串。
- **错误显示不等于校验通过。** 点击保存、关闭窗体、切换页面前是否继续,必须由你自己的业务逻辑决定。
- **及时释放组件。** 推荐把它作为窗体字段并在 `Dispose(bool)` 中释放,或放入支持 `IContainer` 的设计器组件生命周期中。
- **不要缓存旧提示结果。** 控件被释放后记录会自动消失,再次查询应使用 `GetError`。

## FluentLoadingScreen

Fluent 加载屏:铺满父容器的全屏遮罩,中央显示进度环和加载文字。

### 属性

| 属性 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| LoadingText | string | "Loading..." | 进度环下方居中显示的提示文字。 |

### 事件/方法

| 成员 | 签名 | 说明 |
| --- | --- | --- |
| Show(静态) | static FluentLoadingScreen Show(Form owner, string text) | 创建加载屏加入窗体并置顶,返回实例。 |
| HideScreen | void HideScreen() | 从父容器移除并 Dispose 自身。 |

### 示例

```csharp
// 弹出一个全窗体加载层,可在按钮点击后手动隐藏
var loading = FluentLoadingScreen.Show(this, "正在加载...");
loading.HideScreen();
```

### 备注

- 加载层会覆盖父容器并拦截交互,加载结束后必须调用 `HideScreen()`。
- 隐藏后实例已被释放,不要再次访问。
- 示例应放在实际加载任务完成或失败的分支中,不要加载后一直保留遮罩。

## FluentTaskbarProgress

非可视化组件(`Component`):把进度画在窗体的**任务栏按钮**上,支持不确定动画、确定进度、暂停(琥珀色)与错误(红色)四种状态,对应 Windows 壳层的 `ITaskbarList3`。把它拖到窗体的组件栏并指定 `Form` 即可,值变化实时生效。

### 属性

| 属性 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| Form | Form | null | 显示进度的窗体。赋值时订阅 `HandleCreated`/`FormClosed`:句柄已存在则立即应用,尚未创建则等创建后自动补画;换绑或置 null 会先清除旧窗体按钮上的进度。 |
| State | TaskbarProgressState | None | 任务栏进度状态,枚举值见下表。 |
| Value | int | 0 | 已完成量,范围 `0 ~ Total`;小于 0 自动收敛为 0,大于 Total 在写壳层时按 Total 截断。 |
| Total | int | 100 | 总量;`<= 0` 归一化为 100。 |
| IsSupported(静态) | bool | — | 只读,当前系统是否提供任务栏进度接口(Windows 7 及以上为 true)。 |

### TaskbarProgressState

数值与壳层 `TBPFLAG` 完全一致,可直接透传:

| 值 | 数值 | 外观 |
| --- | --- | --- |
| None | 0 | 不显示进度。 |
| Indeterminate | 1 | 绿色来回滚动的不确定动画。 |
| Normal | 2 | 系统强调色的确定进度条。 |
| Error | 4 | 红色的确定进度条。 |
| Paused | 8 | 琥珀色的确定进度条。 |

### 事件/方法

| 成员 | 签名 | 说明 |
| --- | --- | --- |
| 构造 | `FluentTaskbarProgress()` | 独立实例,自行设置 `Form`。 |
| 构造 | `FluentTaskbarProgress(IContainer container)` | 加入容器(设计器的组件栏)由其接管释放。 |
| Apply | `void Apply()` | 用当前 `State`/`Value`/`Total` 重画一次,句柄重建后可手动调用。 |
| Reset | `void Reset()` | 清除任务栏按钮上的进度并把 `State` 复位为 `None`。 |

### 示例

```csharp
// 窗体字段
private FluentTaskbarProgress _taskbar;

private void OnLoad(EventArgs e)
{
    _taskbar = new FluentTaskbarProgress { Form = this };
}

private async Task DownloadAsync()
{
    if (!FluentTaskbarProgress.IsSupported) return;

    _taskbar.Total = 100;
    _taskbar.State = TaskbarProgressState.Normal;
    foreach (int percent in await GetProgressAsync())
    {
        _taskbar.Value = percent;           // 任务栏按钮实时填充
    }

    try
    {
        await CommitAsync();
        _taskbar.Reset();                   // 成功后清除
    }
    catch
    {
        _taskbar.State = TaskbarProgressState.Error;
        _taskbar.Value = _taskbar.Total;    // 红色满条提示失败
    }
}

// 耗时不明的阶段用不确定动画
_taskbar.State = TaskbarProgressState.Indeterminate;
```

### 备注

- `Value` 只在 `Normal`/`Error`/`Paused` 三种确定状态下写回壳层;`Indeterminate` 与 `None` 只设置状态,避免壳层把滚动动画当成数值条。
- 壳层的 `ITaskbarList3` 对象按 UI 线程缓存且不会释放:提前 `Marshal.ReleaseComObject` 会让任务栏按钮丢掉正在显示的进度条,因此组件内部保留单例、`Dispose` 时只解除窗体订阅。
- 属性可从任意线程设置:组件会把写壳层的动作 `BeginInvoke` 编组到窗体的 UI 线程,不会在工作线程上另建一个 `ITaskbarList3`。窗体句柄尚未创建时本次更新直接跳过,`HandleCreated` 之后自动补画。
- 系统不支持或组策略禁用时,所有调用静默忽略,进度不显示但不会抛异常;可先用 `IsSupported` 判断。
- 多窗体应用应各自创建一个组件实例并把 `Form` 指向自己的顶层窗体,任务栏按钮彼此独立。
- 窗体关闭时会自动清除该按钮上的进度,无需手动 `Reset()`。

