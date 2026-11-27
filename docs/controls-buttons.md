# 按钮与展示类控件参考

本文档覆盖 WinformUi 中按钮家族(FluentButton 及其派生类)与轻量展示类控件的属性、事件和用法,所有 API 均与 `WinformUi.Controls` 命名空间下的源码一致。示例代码假定已引入 `WinformUi.Controls`、`WinformUi.Theming` 等命名空间,可直接放入窗体或页面代码中使用。

## FluentButton

Fluent 风格按钮:自绘圆角外观,带悬停/按下视觉状态、可选 Fluent 图标和键盘焦点框。

### 属性

| 属性 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| Appearance | ControlAppearance | ControlAppearance.Secondary | 按钮外观样式,可选值见下方备注 |
| Icon | SymbolRegular | SymbolRegular.Empty | 显示在文本左侧的 Fluent 图标,Empty 表示不显示图标 |
| CornerRadius | float | FluentGraphics.ControlCornerRadius(4f) | 圆角半径(像素) |

### 事件

无额外事件,继承标准 WinForms 事件(Click、MouseEnter 等)。

### 示例

```csharp
var saveButton = new FluentButton
{
    Text = "保存",
    Appearance = ControlAppearance.Primary,
    Icon = SymbolRegular.Save24,
    CornerRadius = 4f
};
saveButton.Click += (s, e) => MessageBox.Show("已保存");
Controls.Add(saveButton);
```

### 备注

- `ControlAppearance` 枚举共九个值:`Secondary`(默认标准按钮)、`Primary`(强调色)、`Info`、`Success`、`Caution`、`Danger`、`Transparent`(无背景)、`Dark`、`Light`。
- 构造函数中 `AutoSize = true`,首选尺寸按"图标 + 文本 + 内边距"计算,最小高度 32px;图标与文本作为整体水平居中。
- 控件订阅主题变化,切换亮/暗主题时自动重绘,无需手动刷新。

## FluentToggleButton

可切换状态的按钮(继承 FluentButton):选中时以强调色填充,点击在选中/未选中间切换。

### 属性

| 属性 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| Checked | bool | false | 是否处于选中状态;选中时按钮显示强调色填充 |

继承自 FluentButton 的 `Appearance`、`Icon`、`CornerRadius` 同样可用。

### 事件

- `CheckedChanged`(EventHandler):`Checked` 值变化时触发,点击和代码赋值都会触发。

### 示例

```csharp
var pinButton = new FluentToggleButton
{
    Text = "固定到顶部",
    Icon = SymbolRegular.Pin24
};
pinButton.CheckedChanged += (s, e) =>
{
    TopMost = pinButton.Checked;
};
Controls.Add(pinButton);
```

### 备注

- 点击时先翻转 `Checked` 再触发 `Click`,因此在 `Click` 处理器中读到的已是新状态。
- 与 FluentCheckBox 不同,它的外观是"按钮"而非勾选框,适合工具栏开关类场景。

## FluentHyperlinkButton

超链接样式按钮(继承 FluentButton):强调色文字、透明背景,点击可打开指定 URI。

### 属性

| 属性 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| NavigateUri | string | null | 点击时通过系统 Shell 打开的 URI(如网址);为空则不导航 |

### 事件

无额外事件,继承标准 WinForms 事件(可用 `Click` 自定义行为)。

### 示例

```csharp
var link = new FluentHyperlinkButton
{
    Text = "查看在线文档",
    NavigateUri = "https://example.com/docs"
};
Controls.Add(link);
```

### 备注

- 默认 `Appearance = ControlAppearance.Transparent`、手型光标;文字颜色使用主题的强调文本色(AccentTextFill 系列),悬停/按下时自动变浅。
- 导航是"尽力而为":URI 无效或系统没有关联程序时静默忽略,不会抛异常。
- 若只想响应点击而不打开链接,保持 `NavigateUri` 为空并订阅 `Click` 即可。

## FluentSplitButton

拆分按钮(继承 FluentButton):左侧为主操作区,右侧 32px 的箭头区点击后弹出 Flyout 菜单。

### 属性

| 属性 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| Flyout | ContextMenuStrip | null | 点击右侧箭头区时弹出的菜单 |

### 事件

无额外事件,继承标准 WinForms 事件。注意:点击箭头区不会触发 `Click`,只有主操作区会。

### 示例

```csharp
var menu = new ContextMenuStrip();
menu.Items.Add("另存为...");
menu.Items.Add("导出 PDF");

var split = new FluentSplitButton
{
    Text = "保存",
    Appearance = ControlAppearance.Primary,
    Flyout = menu,
    Size = new Size(160, 32)
};
split.Click += (s, e) => SaveDocument();
Controls.Add(split);
```

### 备注

- 右侧箭头区宽度固定 32px,默认 `Padding` 右侧预留 40px 以免文字与分隔线重叠;手动设置尺寸时请保证足够宽度。
- `Flyout` 为 null 时点击箭头区无任何效果。

## FluentDropDownButton

下拉按钮(继承 FluentButton):整个按钮点击后在下方弹出 Flyout 菜单,右侧绘制箭头图标。

### 属性

| 属性 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| Flyout | ContextMenuStrip | null | 点击按钮时弹出的菜单 |

### 事件

无额外事件,继承标准 WinForms 事件(`Click` 正常触发,随后弹出菜单)。

### 示例

```csharp
var menu = new ContextMenuStrip();
menu.Items.Add("选项一");
menu.Items.Add("选项二");
menu.Items.Add(new ToolStripSeparator());
menu.Items.Add("选项三");

var drop = new FluentDropDownButton
{
    Text = "更多操作",
    Flyout = menu,
    Size = new Size(150, 32)
};
Controls.Add(drop);
```

### 备注

- 与 FluentSplitButton 的区别:没有独立主操作区,任何位置点击都会弹出菜单并触发 `Click`。
- 默认 `Padding` 右侧预留 36px 用于绘制箭头。

## FluentLabel

Fluent 风格文本标签(继承 Label):默认使用次要文本色,随主题与禁用状态自动变色。

### 属性

| 属性 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| UseSecondaryColor | bool | true | true 使用次要文本色(TextFillSecondary),false 使用主要文本色(TextFillPrimary) |

### 事件

无额外事件,继承标准 WinForms 事件。

### 示例

```csharp
var caption = new FluentLabel
{
    Text = "此设置将在下次启动时生效",
    AutoSize = true
};
var title = new FluentLabel
{
    Text = "常规设置",
    UseSecondaryColor = false,
    AutoSize = true
};
Controls.Add(caption);
Controls.Add(title);
```

### 备注

- 控件禁用(`Enabled = false`)时自动切换为禁用文本色(TextFillDisabled),与 `UseSecondaryColor` 无关。
- 构造函数设置 `UseMnemonic = false`,文本中的 `&` 按字面显示,不会被解释为助记键。
- 内部启用 `UseCompatibleTextRendering`(GDI+ 渲染),以保证在 Mica 等系统 backdrop 上文字 alpha 正确。

## FluentSeparator

Fluent 风格分隔线:1px 细线,颜色取主题的分隔线色(DividerStrokeDefault)。

### 属性

| 属性 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| Orientation | Orientation | Orientation.Horizontal | 分隔线方向;Vertical 时绘制竖线 |

### 事件

无额外事件,继承标准 WinForms 事件。

### 示例

```csharp
var hLine = new FluentSeparator { Width = 300 };

var vLine = new FluentSeparator
{
    Orientation = Orientation.Vertical,
    Size = new Size(1, 48)
};
Controls.Add(hLine);
Controls.Add(vLine);
```

### 备注

- 默认尺寸 100x1、`TabStop = false`、背景透明;线绘制在控件宽/高的中点位置。
- 用作竖线时需要手动把 `Width` 设为 1(或较小值)并给定 `Height`。

## FluentAnchor

相对定位容器(继承 Panel):把第一个子控件摆放在可用空间内 0~1 的相对位置上。

### 属性

| 属性 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| RelativePosition | PointF | (0.5f, 0.5f) | 子控件的相对位置,X/Y 取值 0~1;(0,0) 左上、(0.5,0.5) 居中、(1,1) 右下 |

### 事件

无额外事件,继承标准 WinForms 事件。

### 示例

```csharp
var anchor = new FluentAnchor
{
    Dock = DockStyle.Fill,
    RelativePosition = new PointF(1f, 1f) // 右下角
};
anchor.Controls.Add(new FluentButton
{
    Text = "下一步",
    Appearance = ControlAppearance.Primary
});
Controls.Add(anchor);
```

### 备注

- 只布局 `Controls[0]`(第一个子控件),其余子控件不受影响;位置按 `(容器尺寸 - 子控件尺寸) * 相对值` 计算并钳制到非负。
- 背景透明,本身不绘制任何内容;容器尺寸变化时自动重新布局。

## FluentRatingControl

星级评分控件:交互式五角星评分,支持悬停预览与只读展示。

### 属性

| 属性 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| Value | int | 0 | 当前评分(点亮的星数),自动钳制到 0~Maximum |
| Maximum | int | 5 | 星星总数,最小为 1 |
| ReadOnly | bool | false | 只读模式:不响应点击与悬停,光标恢复为默认箭头 |

### 事件

- `ValueChanged`(EventHandler):`Value` 变化时触发。

### 示例

```csharp
var rating = new FluentRatingControl
{
    Maximum = 5,
    Value = 3,
    Size = new Size(160, 32)
};
rating.ValueChanged += (s, e) =>
{
    Console.WriteLine("当前评分:" + rating.Value);
};
Controls.Add(rating);

var showOnly = new FluentRatingControl { Value = 4, ReadOnly = true };
Controls.Add(showOnly);
```

### 备注

- 鼠标悬停时以强调色预览将要选中的星数,离开后恢复实际值;左键点击第 N 颗星即把 `Value` 设为 N。
- 星星尺寸由控件大小自动推算(`Min(Height-4, (Width-4)/Maximum)`),默认尺寸 140x28;修改 `Maximum` 后建议同步加宽控件。

## FluentThumbRate

赞/踩二选一评价控件:一对拇指图标,选中项以强调色高亮。

### 属性

| 属性 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| Value | int | 0 | 评价状态:1 = 赞,-1 = 踩,0 = 未评价;赋值自动钳制到 [-1, 1] |

### 事件

- `ValueChanged`(EventHandler):`Value` 变化时触发。

### 示例

```csharp
var thumb = new FluentThumbRate();
thumb.ValueChanged += (s, e) =>
{
    switch (thumb.Value)
    {
        case 1: SendFeedback("like"); break;
        case -1: SendFeedback("dislike"); break;
        default: SendFeedback("none"); break;
    }
};
Controls.Add(thumb);
```

### 备注

- 再次点击已选中的拇指会取消评价(`Value` 回到 0),赞与踩互斥。
- 默认尺寸 72x32,两个点击区各为 32x28,分别贴左、贴右;悬停时显示浅色底。

## FluentArc

圆弧控件:绘制一段带圆头端点的圆环弧线,常用于拼出进度环/加载环等视觉效果。

### 属性

| 属性 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| StartAngle | float | 0f | 起始角度(度),GDI+ 约定:0 度指向 3 点钟方向,顺时针为正 |
| EndAngle | float | 270f | 结束角度(度),绘制从 StartAngle 到 EndAngle 的弧 |
| Thickness | float | 4f | 弧线粗细(像素) |
| ArcColor | Color | Color.Empty | 弧线颜色;Empty 时使用主题强调色(SystemAccentColorPrimary) |

### 事件

无额外事件,继承标准 WinForms 事件。

### 示例

```csharp
var arc = new FluentArc
{
    Size = new Size(48, 48),
    StartAngle = -90f,
    EndAngle = 180f,   // 从 12 点钟方向顺时针画 270 度
    Thickness = 5f
};
Controls.Add(arc);
```

### 备注

- 扫过角度(`EndAngle - StartAngle`)接近 0 时不绘制任何内容;端点为圆头(LineCap.Round)。
- 本身不含动画;配合 Timer 周期性修改 `StartAngle`/`EndAngle` 即可实现旋转加载环。
- `TabStop = false`,纯展示控件,不参与 Tab 焦点循环。