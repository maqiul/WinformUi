# 输入类控件参考

本文档覆盖 WinformUi 中文本输入、数值输入与选择类控件的属性、事件和用法,所有 API 均与 `WinformUi.Controls` 命名空间下的源码一致。示例代码假定已引入 `WinformUi.Controls` 等命名空间。

说明:`FluentTextBox`、`FluentPasswordBox`、`FluentNumberBox`、`FluentAutoSuggestBox` 都派生自库内的 `FluentTextInputBase`(圆角外壳 + 内嵌原生 TextBox 的组合控件)。它们共享的输入属性统一列在 FluentTextBox 一节,其余控件只列各自新增的成员。`FluentMaskedTextBox` 同样采用 Fluent 圆角外壳,但内嵌原生 `MaskedTextBox` 以保留完整的输入掩码、校验和编辑行为。

## FluentTextBox

Fluent 单行/多行文本框:圆角外壳带悬停/焦点视觉,聚焦时底部显示 2px 强调色下划线,支持占位文本与左侧图标。

### 属性

以下属性声明于公共基类 FluentTextInputBase,四个输入控件均可用:

| 属性 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| PlaceholderText | string | "" | 占位提示文本,仅在内容为空时显示 |
| Icon | SymbolRegular | SymbolRegular.Empty | 左侧图标;设置后自动为图标预留 32px 左边距 |
| Multiline | bool | false | 是否多行模式 |
| ReadOnly | bool | false | 是否只读 |
| MaxLength | int | 32767 | 最大可输入字符数 |
| TextAlign | HorizontalAlignment | HorizontalAlignment.Left | 文本水平对齐方式 |
| ScrollBars | ScrollBars | ScrollBars.None | 滚动条,需配合 Multiline = true 使用 |
| InnerTextBox | TextBox | (内部实例,只读) | 承载输入的原生 TextBox,供高级场景直接操作 |
| SelectedText | string | "" | 当前选中的文本(读写) |
| SelectionStart | int | 0 | 选区起始位置 |
| SelectionLength | int | 0 | 选区长度 |

另提供 `SelectAll()`、`Clear()`、`Focus()` 方法(Focus 会把焦点转给内部文本框)。

### 事件

无额外事件,继承标准 WinForms 事件。`TextChanged`、`KeyDown`、`KeyPress`、`KeyUp` 由内部文本框转发到外壳控件,可直接订阅。

### 示例

```csharp
var search = new FluentTextBox
{
    PlaceholderText = "搜索...",
    Icon = SymbolRegular.Search24,
    Width = 240
};
search.TextChanged += (s, e) => DoSearch(search.Text);
Controls.Add(search);

var notes = new FluentTextBox
{
    Multiline = true,
    ScrollBars = ScrollBars.Vertical,
    PlaceholderText = "输入备注(可多行)",
    Size = new Size(320, 120)
};
Controls.Add(notes);
```

### 备注

- `ScrollBars` 只有在 `Multiline = true` 时才有意义,这是原生 TextBox 的约定;多行模式下内部编辑区上下各留 8px 边距。
- `ReadOnly = true` 时仍可选中、复制文本,只是不能编辑;禁用请用 `Enabled = false`(外壳与文字都会变为禁用配色)。
- 占位文本颜色、选区颜色均取自主题;需要更底层控制(如输入掩码)时可通过 `InnerTextBox` 访问原生 TextBox。

## FluentMaskedTextBox

带 Fluent 圆角外壳的掩码输入框。内部使用原生 `MaskedTextBox`,因此保留原生掩码校验、插入/覆盖模式、剪贴板格式、输入法、光标和选区行为;外层负责主题、图标、占位提示和焦点视觉。

适合电话、日期、身份证号、邮编等“字符位置和格式固定”的输入。如果只是限制普通文本长度,使用 `FluentTextBox.MaxLength` 更简单;如果需要“输入文本 + 候选列表”,使用 `FluentAutoSuggestBox`。

### 快速开始

下面示例可直接放入普通 `Form`。它演示了掩码输入、拒绝输入提示、类型转换和错误显示:

```csharp
using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using WinformUi.Controls;

public sealed class MaskedDemoForm : Form
{
    private readonly FluentErrorProvider _errors = new FluentErrorProvider();
    private readonly FluentMaskedTextBox _date = new FluentMaskedTextBox
    {
        Location = new Point(20, 20),
        Size = new Size(260, 32),
        Mask = "00/00/0000",
        PlaceholderText = "MM/DD/YYYY",
        Icon = SymbolRegular.CalendarDay16,
        ValidatingType = typeof(DateTime),
        Culture = CultureInfo.GetCultureInfo("en-US")
    };

    public MaskedDemoForm()
    {
        Text = "MaskedTextBox demo";
        ClientSize = new Size(360, 150);

        _date.MaskInputRejected += (s, e) =>
            _errors.SetError(_date, "此处只能输入数字,位置:" + e.Position);

        _date.TypeValidationCompleted += (s, e) =>
        {
            if (e.IsValidInput)
                _errors.ClearError(_date);
            else
                _errors.SetError(_date, e.Message ?? "日期无效");
        };

        var validate = new FluentButton
        {
            Text = "校验",
            Location = new Point(20, 70),
            AutoSize = true,
            Appearance = ControlAppearance.Primary
        };
        validate.Click += (s, e) =>
        {
            object value = _date.ValidateText();
            if (value is DateTime date)
                Text = date.ToString("D", _date.Culture);
            else if (_date.ValidatingType == null)
                _errors.SetError(_date, "尚未设置 ValidatingType");
            else if (!_date.MaskCompleted)
                _errors.SetError(_date, "请完整填写日期");
            else
                _errors.SetError(_date, "日期格式无效");
        };

        Controls.AddRange(new Control[] { _date, validate });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _errors.Dispose();
        base.Dispose(disposing);
    }
}
```

`TypeValidationCompleted` 的参数使用 **`e.IsValidInput`**,不是 `e.IsValid`。转换成功时 `e.ReturnValue` 是已转换对象;失败时通常为 null,并可通过 `e.Message` 取得原生提示。

### 掩码字符速查

| 掩码字符 | 含义 | 示例 |
| --- | --- | --- |
| `9` | 可选数字 0–9 | `99999` 五位邮编,允许少填 |
| `0` | 必须输入的数字 0–9 | `000-0000` 区号与号码都必须填满 |
| `L` | 必须输入的字母 | `LLLLLLLLLL` 固定长度字母串 |
| `?` | 可选字母 | 允许字母序列提前结束 |
| `A` | 必须输入的字母或数字 | 产品编号 |
| `a` | 可选的字母或数字 | 可选字母数字位 |
| `&` | 必须输入的任意字符 | 固定长度验证码 |
| `C` | 可选的任意字符 | 可选任意字符位 |
| `<` / `>` | 后续字符转为小写 / 大写 | `<LLL 转换为小写 |
| `\` | 转义下一个掩码字符,使其成为字面量 | `\9` 显示普通字符 9 |

日期分隔符 `/` 会按 `Culture` 使用对应地区的日期分隔符。电话号码可直接写成 `"(000) 000-0000"`;掩码中的空格、括号、短横线都是自动显示的字面量,用户无需输入。

### 属性

#### Fluent 外观

| 属性 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| PlaceholderText | string | `""` | 内容为空时显示的 cue banner 提示,支持中文;与 `PromptChar` 是两套不同提示 |
| Icon | SymbolRegular | `SymbolRegular.Empty` | 左侧图标;非 Empty 时预留 32px 图标区 |
| Font / Enabled / RightToLeft / ImeMode | 继承属性 | — | 会同步到内部原生编辑器;RTL 切换时重新布局 |

#### 掩码与输入行为

| 属性 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| Mask | string | `""` | 输入掩码;null 会被规范化为空字符串 |
| PromptChar | char | `'_'` | 未填写位置的提示字符,不是光标提示文本 |
| BeepOnError | bool | `true` | 拒绝非法输入时是否响铃 |
| TextMaskFormat | MaskFormat | `IncludePromptAndLiterals` | 决定 `Text` 是否包含提示字符和掩码字面量 |
| CutCopyMaskFormat | MaskFormat | `IncludeLiterals` | 决定剪切/复制文本的格式 |
| InsertKeyMode | InsertKeyMode | `Default` | 原生插入/覆盖模式策略 |
| SkipLiterals | bool | `true` | 输入时是否自动跳过掩码字面量 |
| ResetOnPrompt | bool | `true` | 输入提示字符时是否重置后续输入 |
| ResetOnSpace | bool | `true` | 输入空格时是否重置后续输入 |
| RejectInputOnFirstFailure | bool | `false` | 第一次失败后是否拒绝本次输入 |
| AllowPromptAsInput | bool | `false` | 是否允许把提示字符当作有效输入 |
| AsciiOnly | bool | `false` | 是否把输入限制为 ASCII 字符 |
| ReadOnly | bool | `false` | 只读;仍可选择、复制,禁止编辑请用 Enabled = false |
| MaxLength | int | `32767` | 最大长度;实际输入还受 Mask 约束 |
| TextAlign | HorizontalAlignment | `Left` | 文本水平对齐 |

#### 类型校验与密码显示

| 属性 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| ValidatingType | Type | `null` | 类型校验目标,例如 `typeof(DateTime)`、`typeof(int)`、`typeof(decimal)` |
| Culture | CultureInfo | 构造时继承当前区域文化 | 数字/日期解析文化;赋值为 null 会抛出 `ArgumentNullException` |
| UseSystemPasswordChar | bool | `false` | 使用系统密码字符;优先级高于 PasswordChar |
| PasswordChar | char | `'\0'` | 自定义密码字符,仅在 UseSystemPasswordChar = false 时生效 |
| MaskCompleted | bool | 取决于 Mask 与 Text,只读 | 所有**必需**位置是否填写完成 |
| MaskFull | bool | 取决于 Mask 与 Text,只读 | 必需位置和可选位置是否全部填写 |

`MaskCompleted` 与 `MaskFull` 不要求同时为 true。例如掩码包含可选数字位时,必需位填完即可通过 `MaskCompleted`,类型校验也可能成功,但 `MaskFull` 仍为 false。

#### 文本、选区与内部编辑器

| 成员 | 类型 | 说明 |
| --- | --- | --- |
| Text | string | 转发内部 Text;null 赋值会变为空字符串;具体格式由 TextMaskFormat 决定 |
| SelectedText | string | 当前选区文本,可读写 |
| SelectionStart | int | 选区起点,可读写 |
| SelectionLength | int | 选区长度,可读写 |
| InnerMaskedTextBox | MaskedTextBox | 内部原生编辑器,只读引用;高级场景可访问未在外层暴露的原生成员 |

`InnerMaskedTextBox`、`MaskCompleted`、`MaskFull` 和选区属性不会出现在设计器属性网格中。

### 方法

| 方法 | 说明 |
| --- | --- |
| `void SelectAll()` | 全选内部编辑器内容 |
| `void Clear()` | 清空输入,保留 Mask 等配置 |
| `new bool Focus()` | 把焦点交给内部 MaskedTextBox,返回是否成功 |
| `object ValidateText()` | 按 ValidatingType 转换当前文本,返回装箱后的值;失败返回 null |

`ValidateText()` 的详细行为:

1. `ValidatingType == null` 时返回 null,不触发类型校验事件。
2. 必需掩码位未填完时,`ValidateText()` 返回 `null`;原生编辑器在部分失焦校验路径中会触发 `TypeValidationCompleted`。
3. 转换成功时返回 `DateTime`、`int` 等装箱对象,事件参数 `e.IsValidInput == true`。
4. 事件处理器中抛出的异常会按 .NET 事件调用链继续传播;需要健壮校验时应按业务代码捕获。

显式调用 `ValidateText()` 时,返回 `null` 可能是“没有 ValidatingType”“掩码未完成”或“转换失败”三种情况。需要区分时,先检查 `ValidatingType` 和 `MaskCompleted`,再结合事件参数或业务解析判断。

### 事件

| 事件 | 触发时机 / 用法 |
| --- | --- |
| MaskChanged | Mask 实际变化后触发 |
| MaskInputRejected | 用户输入被掩码拒绝;可读取 Position、RejectionHint |
| TypeValidationCompleted | 原生类型校验完成时触发,包括显式 ValidateText() 和失焦校验;读取 IsValidInput、ReturnValue、Message、ValidatingType |
| TextChanged | 内部文本变化后触发 |
| GotFocus / LostFocus | 内部编辑器获得/失去焦点;外层同步焦点下划线 |
| KeyDown / KeyPress / KeyUp / Validating | 从内部编辑器转发,可直接在外层订阅 |

`Validated` 没有单独转发。需要该事件或 MaskedTextBox 的其他冷门成员时,可访问 `InnerMaskedTextBox`。外层 `TabStop = false`,键盘 Tab 导航最终聚焦到内部编辑器。

### 常用示例

#### 只取得用户输入的数字

```csharp
var phone = new FluentMaskedTextBox
{
    Mask = "(000) 000-0000",
    PlaceholderText = "电话号码",
    Icon = SymbolRegular.Call24,
    TextMaskFormat = MaskFormat.ExcludePromptAndLiterals,
    Width = 260
};

string digits = phone.Text; // 例如 13800138000,不含括号、空格和短横线
if (digits.Length == 11) SavePhone(digits);
```

`TextMaskFormat` 的四个选项:

| 值 | Text 中包含的内容 |
| --- | --- |
| IncludePromptAndLiterals | 提示字符 + 掩码字面量 |
| IncludeLiterals | 只包含掩码字面量,不包含未填位置的提示字符 |
| ExcludePromptAndLiterals | 两者都不包含 |
| IncludePrompt | 提示字符,但不包含掩码字面量 |

#### 校验失败时取消失焦

```csharp
_date.Validating += (s, e) =>
{
    if (!_date.MaskCompleted)
    {
        _errors.SetError(_date, "请完整填写日期");
        e.Cancel = true;
    }
};
```

`TypeValidationCompleted.Cancel` 也可在原生失焦校验过程中取消焦点转移;但它不会改变已经显式调用的 `ValidateText()` 返回值。

#### 掩码显示的密码

```csharp
var pin = new FluentMaskedTextBox
{
    Mask = "000000",
    PromptChar = '*',
    PasswordChar = '●',
    UseSystemPasswordChar = false,
    PlaceholderText = "六位验证码",
    Width = 220
};
```

使用系统密码字符时,不要把 `PromptChar` 设置得与 `PasswordChar` 相同,否则原生 MaskedTextBox 可能抛出 `InvalidOperationException`。

### 设计器使用

- 工具箱拖入后,在属性窗口的 Behavior 分类设置 `Mask`、`PromptChar`、`ValidatingType`、`Culture` 等;Fluent 分类设置 `PlaceholderText`、`Icon`。
- `Mask`、`Text`、字体、大小、位置等可随窗体设计器序列化;运行时的勾选/输入结果仍需业务代码处理。
- 默认事件为 `MaskInputRejected`,双击控件通常会进入该事件处理器。
- 默认尺寸 200×32;有图标时编辑区左侧会额外让出 32px,建议宽度不少于 180px。

### 常见坑

- `PlaceholderText` 是空内容时的整体提示,`PromptChar` 是每个未填掩码位置的占位符,不要混用。
- `MaskCompleted` 是属性,没有 `MaskCompleted` 事件。
- 类型校验不是提交按钮保护;是否允许保存由业务代码决定。
- `Culture = null` 不被允许。需要跟随系统文化时不要赋值,需要固定格式时指定明确文化。
- `UseSystemPasswordChar = true` 会忽略自定义 `PasswordChar`。
- 需要完全原生的 MaskedTextBox API 时再使用 `InnerMaskedTextBox`;直接改其尺寸可能被外层下一次 Resize/FontChanged 覆盖。

## FluentPasswordBox

Fluent 密码框(继承 FluentTextInputBase):默认以系统密码字符掩码显示输入内容。

### 属性

| 属性 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| PasswordChar | char | '\0' | 自定义掩码字符;默认 '\0' 且构造函数已开启 InnerTextBox.UseSystemPasswordChar,显示为 ● |

共享的 PlaceholderText、Icon、MaxLength 等见 FluentTextBox 一节。

### 事件

无额外事件,继承标准 WinForms 事件。

### 示例

```csharp
var password = new FluentPasswordBox
{
    PlaceholderText = "请输入密码",
    Icon = SymbolRegular.LockClosed24,
    Width = 240
};
password.KeyDown += (s, e) =>
{
    if (e.KeyCode == Keys.Enter) Login(password.Text);
};
Controls.Add(password);
```

### 备注

- `Text` 属性返回的始终是真实明文,掩码只影响显示;自定义掩码字符时设置 `PasswordChar`,若想让自定义字符生效需同时把 `InnerTextBox.UseSystemPasswordChar` 设为 false。
- 默认单行;密码框一般不需要 Multiline。

## FluentNumberBox

Fluent 数值输入框(继承 FluentTextInputBase):右侧内嵌加/减步进按钮,支持键盘步进、滚轮调节与上下限钳制。

### 属性

| 属性 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| Value | double | 0d | 当前数值,自动钳制到 Minimum~Maximum |
| Minimum | double | double.MinValue | 允许的最小值;修改后会立即重新钳制当前值 |
| Maximum | double | double.MaxValue | 允许的最大值;修改后会立即重新钳制当前值 |
| SmallChange | double | 1d | 小步进:点击步进按钮、↑/↓ 键、滚轮一格的增量 |
| LargeChange | double | 10d | 大步进:PageUp/PageDown 键的增量 |

### 事件

- `ValueChanged`(EventHandler):`Value` 变化时触发(键盘、按钮、滚轮、代码赋值均会触发)。

### 示例

```csharp
var count = new FluentNumberBox
{
    Value = 42,
    Minimum = 0,
    Maximum = 1000,
    SmallChange = 1,
    LargeChange = 10,
    Width = 180
};
count.ValueChanged += (s, e) => UpdateQuantity(count.Value);
Controls.Add(count);
```

### 备注

- 手动键入的文本在按 Enter 或失去焦点时提交:按当前区域文化(CurrentCulture)解析,解析失败则回退为原值;超出上下限的输入会被钳制。
- 控件右侧固定预留 56px 给两个步进按钮,设置宽度时需留出这部分空间。
- `Value` 与显示文本同步,显示格式同样使用 CurrentCulture;读写数值请优先用 `Value` 而不是 `Text`。

## FluentAutoSuggestBox

Fluent 自动建议框(继承 FluentTextInputBase):输入时在下方弹出候选列表,选中候选项后回填文本。

### 属性

| 属性 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| Items | IList&lt;string&gt; | 空的 List&lt;string&gt; | 候选项全集;控件按输入内容实时过滤 |

### 事件

- `SuggestionChosen`(EventHandler):用户通过点击或 Enter 选中某个候选项、文本已回填后触发。

### 示例

```csharp
var suggest = new FluentAutoSuggestBox
{
    PlaceholderText = "输入水果名称...",
    Width = 240
};
suggest.Items = new List<string>
{
    "Apple", "Apricot", "Banana", "Blueberry",
    "Cherry", "Grape", "Mango", "Orange"
};
suggest.SuggestionChosen += (s, e) =>
{
    Console.WriteLine("已选择:" + suggest.Text);
};
Controls.Add(suggest);
```

### 备注

- 过滤规则为"包含匹配"(不区分大小写、按当前文化比较),不是前缀匹配;输入为空、无匹配项或失去焦点时列表自动关闭。
- 弹出列表不抢焦点(NoActivate 弹出窗),可边打字边用 ↑/↓ 移动高亮、Enter 确认、Esc 关闭;列表高度上限 280px。
- 控件右侧固定绘制搜索图标并预留 32px;`SuggestionChosen` 只在"选中候选项"时触发,手动输入不触发。

## FluentCheckBox

Fluent 复选框:20px 圆角方框,选中时强调色填充 + 对勾,支持三态(不确定态显示横杠)。

### 属性

| 属性 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| Checked | bool | false | 是否选中;读写会映射到 CheckState(Checked/Unchecked) |
| ThreeState | bool | false | 是否启用三态循环 |
| CheckState | CheckState | CheckState.Unchecked | 勾选状态:Unchecked / Checked / Indeterminate |
| AutoSize | bool | true | 按文本自动调整尺寸(方框 20px + 间距 8px + 文本宽度) |

### 事件

- `CheckedChanged`(EventHandler):`Checked` 布尔值变化时触发。
- `CheckStateChanged`(EventHandler):`CheckState` 变化时触发(三态场景下更精确)。

### 示例

```csharp
var agree = new FluentCheckBox { Text = "我已阅读并同意条款" };
agree.CheckedChanged += (s, e) => submitButton.Enabled = agree.Checked;
Controls.Add(agree);

var partial = new FluentCheckBox
{
    Text = "全选(三态)",
    ThreeState = true,
    CheckState = CheckState.Indeterminate
};
Controls.Add(partial);
```

### 备注

- `ThreeState = true` 时点击循环顺序为:未选 → 选中 → 不确定 → 未选;`Checked` 在 Indeterminate 下也为 true(即"非 Unchecked")。
- 支持键盘:获得焦点后按空格切换状态;`CheckState` 变化会同时触发 CheckedChanged 与 CheckStateChanged(只要状态确实改变)。
- 默认尺寸 120x24,但 `AutoSize = true` 会在设置 Text/Font 后自动收缩到合适大小。

## FluentRadioButton

Fluent 单选按钮:20px 圆形,选中时强调色填充 + 白色圆点;同一父容器内自动互斥。

### 属性

| 属性 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| Checked | bool | false | 是否选中;设为 true 时自动取消同父容器内其他 FluentRadioButton 的选中 |
| AutoSize | bool | true | 按文本自动调整尺寸(圆形 20px + 间距 8px + 文本宽度) |

### 事件

- `CheckedChanged`(EventHandler):`Checked` 变化时触发(包括被同组其他按钮挤掉时)。

### 示例

```csharp
var group = new Panel { AutoSize = true };

var light = new FluentRadioButton { Text = "浅色", Checked = true, Location = new Point(0, 0) };
var dark = new FluentRadioButton { Text = "深色", Location = new Point(0, 28) };
var system = new FluentRadioButton { Text = "跟随系统", Location = new Point(0, 56) };

light.CheckedChanged += (s, e) => { if (light.Checked) SetTheme("light"); };
dark.CheckedChanged += (s, e) => { if (dark.Checked) SetTheme("dark"); };
system.CheckedChanged += (s, e) => { if (system.Checked) SetTheme("system"); };

group.Controls.AddRange(new Control[] { light, dark, system });
Controls.Add(group);
```

### 备注

- 互斥范围是"同一个 Parent 容器",没有 GroupName 概念;需要多组单选时,把每组放进各自的 Panel。
- 与标准 RadioButton 一致:点击已选中的按钮不会取消选中,只能通过选中同组其他项或代码赋值改变。
- 支持空格键选中;按下状态中央圆点会缩小,呈现 Fluent 按压反馈。

## FluentToggleSwitch

Fluent 开关:40x20 胶囊轨道 + 滑动旋钮,切换时带约 150ms 的滑动动画,右侧可显示开/关文案。

### 属性

| 属性 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| Checked | bool | false | 开关状态;变化时启动旋钮滑动动画 |
| OnContent | string | "On" | 打开状态时右侧显示的文案,空字符串则不显示 |
| OffContent | string | "Off" | 关闭状态时右侧显示的文案,空字符串则不显示 |

### 事件

- `CheckedChanged`(EventHandler):`Checked` 变化时触发。

### 示例

```csharp
var wifi = new FluentToggleSwitch
{
    OnContent = "已开启",
    OffContent = "已关闭",
    Checked = true,
    Width = 140
};
wifi.CheckedChanged += (s, e) => SetWifiEnabled(wifi.Checked);
Controls.Add(wifi);
```

### 备注

- 动画由内部 15ms 定时器驱动,`CheckedChanged` 在状态翻转时立即触发,不等待动画结束。
- 支持空格键切换;轨道左侧固定 40px 宽,文案从轨道右侧 10px 处开始绘制,加长文案时请相应加宽控件(默认 110x24)。
- 控件 Dispose 时会停止并释放动画定时器,无需手动清理。

## FluentSlider

Fluent 滑块:4px 圆角轨道 + 强调色已填充段 + 20px 复合拇指(实心外圈 + 强调色内点),支持拖动、键盘与滚轮。

### 属性

| 属性 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| Minimum | double | 0d | 最小值;修改后立即重新钳制当前值 |
| Maximum | double | 100d | 最大值;修改后立即重新钳制当前值 |
| Value | double | 0d | 当前值,自动钳制到 Minimum~Maximum |
| TickFrequency | double | 0d | 刻度间隔(按值计算);0 表示不绘制刻度 |

### 事件

- `ValueChanged`(EventHandler):`Value` 变化时触发,拖动过程中会连续触发。

### 示例

```csharp
var volume = new FluentSlider
{
    Minimum = 0,
    Maximum = 100,
    Value = 60,
    TickFrequency = 10,
    Width = 260
};
volume.ValueChanged += (s, e) => SetVolume(volume.Value);
Controls.Add(volume);
```

### 备注

- 键盘:←/↓ 与 →/↑ 按量程的 1% 步进,Home/End 跳到最小/最大值;滚轮每格调整量程的 3%。刻度值不会吸附,仅为视觉参考。
- 刻度线绘制在轨道下方(拇指下缘再往下 2px、高 4px),默认高度 32px 刚好容纳;控件太矮时会被裁掉。
- `ValueChanged` 在拖动中高频触发,若处理开销大(如写文件、网络请求),建议在处理器内做节流或改用 MouseUp 后提交。

## FluentComboBox

Fluent 组合框(不可编辑):圆角外壳 + 右侧箭头,点击弹出带悬停高亮与强调色选中条的浮出列表。

### 属性

| 属性 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| Items | IList | 空的 List&lt;object&gt; | 候选项集合,元素可为任意对象;设置 DataSource 后指向绑定列表,此时赋值 Items 抛 InvalidOperationException |
| DataSource | object | null | 数据源:IList、IListSource、IEnumerable、DataTable、DataSet、BindingSource 等;设计器智能标记与属性窗格(Data 分类)均提供数据源下拉 |
| DataMember | string | "" | 绑定 DataSet 时指定表名;无效成员不抛异常,绑定空列表 |
| DisplayMember | string | "" | 用作显示文本的项属性名;为空时 ToString();TextSelector 优先于它 |
| ValueMember | string | "" | 用作 SelectedValue 的项属性名;为空时 SelectedValue 即项本身 |
| SelectedValue | object | null | 选中项的 ValueMember 值;赋值按值定位选中项 |
| TextSelector | Func&lt;object, string&gt; | null | 项到显示文本的转换函数(相当于 DisplayMember);null 时调用项的 ToString() |
| SelectedIndex | int | -1 | 选中项索引;-1 表示未选中,赋值自动钳制到 [-1, Items.Count-1] |
| SelectedItem | object | null | 选中项对象;赋值时按 IndexOf 定位,不存在则回到 -1 |
| SelectedText | string | ""(只读) | 选中项经 TextSelector 转换后的显示文本 |
| DroppedDown | bool | false(只读) | 下拉列表当前是否展开 |

另提供 `OpenDropDown()` / `CloseDropDown()` 方法。

### 事件

- `SelectedIndexChanged`(EventHandler):选中项变化时触发。
- `DropDownOpened`(EventHandler):下拉列表打开后触发。
- `DropDownClosed`(EventHandler):下拉列表关闭后触发。

### 示例

```csharp
var combo = new FluentComboBox { Width = 200 };
combo.Items = new List<object> { "浅色", "深色", "跟随系统" };
combo.SelectedIndex = 0;
combo.SelectedIndexChanged += (s, e) =>
{
    ApplyTheme(combo.SelectedItem as string);
};
Controls.Add(combo);

// 绑定自定义对象时用 TextSelector 指定显示文本
var fontCombo = new FluentComboBox
{
    Width = 220,
    Items = new List<object> { new Font("Segoe UI", 9f), new Font("Consolas", 10f) },
    TextSelector = item => ((Font)item).Name
};
Controls.Add(fontCombo);

// 标准数据绑定:对象列表 / DataTable / BindingSource 均可
var userCombo = new FluentComboBox { Width = 220, DisplayMember = "Name", ValueMember = "Id" };
userCombo.DataSource = users;                      // List<User>、BindingList<User>、BindingSource…
userCombo.SelectedIndexChanged += (s, e) => LoadUser((int)userCombo.SelectedValue);

var tableCombo = new FluentComboBox { Width = 220, DisplayMember = "Name", ValueMember = "Id" };
tableCombo.DataSource = dataSet;                   // DataSet 需配合 DataMember
tableCombo.DataMember = "Users";
Controls.Add(userCombo);
Controls.Add(tableCombo);
```

### 备注

- 不支持编辑文本,只支持从列表选择;需要"可输入 + 候选"请用 FluentAutoSuggestBox。
- 键盘:F4/空格/Enter 展开或收起,收起状态下 ↑/↓ 直接切换选中项,Esc 关闭列表;列表高度上限 320px,超出可滚动。
- 弹出列表是独立的无边框圆角窗体(FluentPopupForm),失去激活自动关闭;`Items` 为空时 `OpenDropDown()` 不生效。
- 数据绑定与原生 ListControl 同语义:经 BindingContext/CurrencyManager 解析,绑定后默认选中第 0 项;同一数据源的多个控件共享货币位置(选中互相同步);IBindingList/BindingSource 增删项实时刷新;解除绑定(DataSource = null)恢复未绑定时的 Items。
- 设计器智能标记与原生 ComboBox 同款:选中控件点右上角小箭头,"使用数据绑定项"勾选切换两种模式。未勾选为"未绑定模式",提供"编辑项..."打开字符串集合编辑器(Items 可在设计器编辑并随窗体序列化);勾选为"绑定模式",提供"选择数据源"下拉(列出本窗体上的 BindingSource/DataSet 等数据组件,.NET Framework 与 .NET 6/8/10 进程外设计器均支持)及 DataMember/DisplayMember/ValueMember 直填项;取消勾选会清掉 DataSource 并恢复未绑定 Items。属性窗格 DataSource 的下拉与智能标记等价。.NET Framework 项目绑定模式另含"添加项目数据源..."打开数据源窗口;.NET 项目新建数据源请用 VS 菜单"视图→其他窗口→数据源"。

## FluentDropDownList

Fluent 浮出列表控件:32px 行高、悬停浅色填充、选中项左侧带强调色竖条;是 ComboBox / AutoSuggestBox 弹出列表的内部实现,也可独立使用。

### 属性

| 属性 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| Items | IList | 空的 List&lt;object&gt; | 列表项集合;重新赋值会重置滚动位置;设置 DataSource 后赋值 Items 抛 InvalidOperationException |
| DataSource | object | null | 数据源,语义与 FluentComboBox 相同(IList/DataTable/DataSet/BindingSource…),含同款设计器智能标记 |
| DataMember | string | "" | DataSet 的表名成员 |
| DisplayMember | string | "" | 显示文本属性名;TextSelector 优先 |
| ValueMember | string | "" | SelectedValue 属性名 |
| SelectedValue | object | null | 选中项的 ValueMember 值,可按值赋值定位 |
| TextSelector | Func&lt;object, string&gt; | null | 项到显示文本的转换函数;null 时调用 ToString() |
| SelectedIndex | int | -1 | 选中项索引;变化时自动滚动使其可见 |
| SelectedItem | object | null(只读) | 当前选中项对象 |
| ItemHeight | int | 32(只读) | 固定行高,可用于计算弹出窗高度 |

### 事件

- `ItemClicked`(EventHandler):鼠标点击某项或选中项上按 Enter 时触发。
- `SelectedIndexChanged`(EventHandler):`SelectedIndex` 变化时触发(键盘移动高亮也会触发)。

### 示例

```csharp
var popup = new FluentPopupForm();
var list = new FluentDropDownList
{
    Items = new List<object> { "新建", "打开", "保存" },
    Dock = DockStyle.Fill
};
list.ItemClicked += (s, e) =>
{
    ExecuteCommand(list.SelectedItem as string);
    popup.Close();
};
popup.Padding = new Padding(4);
popup.Controls.Add(list);
popup.ShowBelow(anchorButton, list.ItemHeight * 3 + 8);
```

### 备注

- 键盘:↑/↓ 移动选中项,Home/End 跳到首/末项,Enter 触发 `ItemClicked`;滚轮滚动列表(无滚动条,靠滚动偏移)。
- 自绘背景使用主题的 AcrylicBackgroundFillDefault 色,通常放在 FluentPopupForm 内使用以获得完整的 Fluent 浮出面效果(圆角、阴影、Esc/失活关闭)。
- 与 FluentComboBox 不同,它没有外壳与箭头,`SelectedIndexChanged` 只表示高亮变化,"确认选择"请以 `ItemClicked` 为准。