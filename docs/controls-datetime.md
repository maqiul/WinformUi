# 日期、时间与颜色控件参考

本文档覆盖 WinformUi 中日期/时间/颜色相关的五个控件:`FluentCalendar`、`FluentCalendarDatePicker`、`FluentDatePicker`、`FluentTimePicker`、`FluentColorPicker`。全部位于命名空间 `WinformUi.Controls`,随 `ApplicationThemeManager` 的主题(浅色/深色/强调色)自动换肤。属性、事件与默认值均以源码为准,示例片段可直接放入窗体或页面代码中(需自行引入 `System`、`System.Drawing`、`System.Windows.Forms`、`WinformUi.Controls`、`WinformUi.Theming` 等命名空间)。

## FluentCalendar

自绘月历:月份标题与上/下月导航按钮、星期行、6×7 日期网格,带今日圆形高亮、选中圆角块与悬停反馈。

### 属性

| 属性 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| DisplayMonth | DateTime | 当前月份的第一天 | 当前显示的月份;赋值时自动归一化到该月 1 日,变化时触发 DisplayMonthChanged。 |
| SelectedDate | DateTime? | null | 选中的日期;赋非空值时会把 DisplayMonth 切换到该日期所在月份。 |

### 事件

| 事件 | 说明 |
| --- | --- |
| DateSelected | 用户点击某个日期格时触发(点击上/下月溢出的日期也会选中并跳转月份)。 |
| DisplayMonthChanged | 显示月份变化时触发(点击导航按钮或设置 DisplayMonth/SelectedDate 引起)。 |

### 示例

```csharp
var calendar = new FluentCalendar();
var state = new FluentLabel { Text = "Click a day", AutoSize = true };
calendar.DateSelected += (s, e) => state.Text = calendar.SelectedDate.HasValue
    ? calendar.SelectedDate.Value.ToString("D") : "Click a day";
calendar.DisplayMonthChanged += (s, e) =>
    Console.WriteLine(calendar.DisplayMonth.ToString("Y"));

// 预设选中日期(会自动跳转到对应月份)
calendar.SelectedDate = new DateTime(2026, 5, 20);

Controls.Add(calendar);
Controls.Add(state);
```

### 备注

- 星期行使用当前文化的 `ShortestDayNames`(最短星期名),每周起始日取当前文化的 `FirstDayOfWeek`。
- 默认尺寸 280×292,`TabStop = false`,不参与 Tab 键焦点循环。
- 今日绘制为强调色实心圆,选中日期绘制为强调色圆角块;非本月日期使用禁用文本色。

## FluentCalendarDatePicker

日历式日期选择框:文本外壳 + 日历图标,点击弹出内嵌 `FluentCalendar` 的浮层。

### 属性

| 属性 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| SelectedDate | DateTime? | null | 选中日期;为空时显示占位文本。注意:通过代码赋值也会触发 DateSelected。 |
| PlaceholderText | string | "Pick a date" | 未选择日期时显示的占位文本。 |

### 事件

| 事件 | 说明 |
| --- | --- |
| DateSelected | 选中日期变化时触发,包括用户在弹出日历中点选和代码设置 SelectedDate。 |

### 示例

```csharp
var picker = new FluentCalendarDatePicker { Width = 200, PlaceholderText = "选择日期" };
var state = new FluentLabel { Text = "No date selected", AutoSize = true };
picker.DateSelected += (s, e) => state.Text = picker.SelectedDate.HasValue
    ? picker.SelectedDate.Value.ToString("D") : "No date selected";

Controls.Add(picker);
Controls.Add(state);
```

### 备注

- 默认尺寸 200×32;点击、空格键或回车键打开日历浮层,Esc 关闭。
- 已选日期按当前文化的短日期格式("d")显示。
- 浮层由内部的 `FluentPopupForm` 承载,控件 Dispose 时自动关闭。

## FluentDatePicker

分段式日期选择器:日 / 月 / 年三个分段,每段点击弹出对应的下拉列表。

### 属性

| 属性 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| SelectedDate | DateTime | DateTime.Today | 选中日期;值变化时触发 DateSelected。 |

### 事件

| 事件 | 说明 |
| --- | --- |
| DateSelected | 用户在分段列表中选定新值导致日期变化,或代码赋新值时触发。 |

### 示例

```csharp
var datePicker = new FluentDatePicker { Width = 280 };
datePicker.DateSelected += (s, e) =>
    Console.WriteLine(datePicker.SelectedDate.ToString("d"));

// 预设日期
datePicker.SelectedDate = new DateTime(2026, 11, 1);

Controls.Add(datePicker);
```

### 备注

- 默认尺寸 260×32;月份名取自当前文化的 `DateTimeFormat.MonthNames`。
- 年份列表范围为当前选中年份 ±50 年;切换月/年时会自动把"日"收敛到目标月份的天数上限(如 1 月 31 日切到 2 月变为 28/29 日)。
- 分段列表使用内部 `FluentDropDownList`,弹出位置自动避让屏幕下边缘(空间不足时向上弹出)。

## FluentTimePicker

时间选择框:显示 HH:mm,点击弹出小时/分钟两列列表,底部带确认(✓)与取消(✕)按钮。

### 属性

| 属性 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| SelectedTime | TimeSpan | DateTime.Now.TimeOfDay(构造时的当前时间) | 选中时间;仅显示并选择小时与分钟,秒在确认时归零。 |

### 事件

| 事件 | 说明 |
| --- | --- |
| TimeSelected | 用户点击确认按钮选定新时间,或代码赋新值时触发;点击取消不触发。 |

### 示例

```csharp
var timePicker = new FluentTimePicker { Width = 160 };
timePicker.TimeSelected += (s, e) =>
    Console.WriteLine(string.Format("{0:00}:{1:00}",
        timePicker.SelectedTime.Hours, timePicker.SelectedTime.Minutes));

// 预设为 09:30
timePicker.SelectedTime = new TimeSpan(9, 30, 0);

Controls.Add(timePicker);
```

### 备注

- 默认尺寸 160×32;小时列 00–23,分钟列 00–59,均为 24 小时制。
- 只有点击确认按钮才提交选择;点击取消或直接关闭浮层不会修改 SelectedTime。

## FluentColorPicker

颜色选择器:色相/饱和度光谱区、明度滑条、十六进制输入框与颜色预览块。

### 属性

| 属性 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| Color | Color | 红色 #FF0000(HSV 初始为 0,1,1) | 当前颜色;内部以 HSV 存储,读取时转换为 RGB(Alpha 恒为 255)。赋值会触发 ColorChanged。 |

### 事件

| 事件 | 说明 |
| --- | --- |
| ColorChanged | 颜色变化时触发:拖动光谱/明度滑条过程中持续触发,十六进制输入提交或代码设置 Color 时也触发。 |

### 示例

```csharp
var picker = new FluentColorPicker();
var state = new FluentLabel { Text = "#FF0000", AutoSize = true };
picker.ColorChanged += (s, e) =>
{
    Color c = picker.Color;
    state.Text = string.Format("#{0:X2}{1:X2}{2:X2}", c.R, c.G, c.B);
};

// 预设颜色
picker.Color = Color.FromArgb(0x00, 0x78, 0xD4);

Controls.Add(picker);
Controls.Add(state);
```

### 备注

- 默认尺寸 260×264,`TabStop = false`;光谱横轴为色相(0–360°)、纵轴为饱和度(顶部 1、底部 0),下方滑条控制明度。
- 十六进制输入框接受 `#RRGGBB` 或 `RRGGBB`(6 位),在失去焦点或按回车时提交;非法输入会回退为当前颜色文本。
- 拖动过程中 ColorChanged 会高频触发,若处理逻辑较重建议自行节流。