# Date, Time and Color Controls Reference

This document covers the five date/time/color related controls in WinformUi: `FluentCalendar`, `FluentCalendarDatePicker`, `FluentDatePicker`, `FluentTimePicker`, `FluentColorPicker`. All of them live in the namespace `WinformUi.Controls` and re-skin automatically with the theme (light/dark/accent color) driven by `ApplicationThemeManager`. Properties, events and default values all follow the sources; the example snippets can be dropped straight into form or page code (you need to import namespaces such as `System`, `System.Drawing`, `System.Windows.Forms`, `WinformUi.Controls`, `WinformUi.Theming` yourself).

## FluentCalendar

Self-drawn monthly calendar: a month title with previous/next month navigation buttons, a weekday header row, and a 6×7 date grid, with a circular highlight for today, a rounded block for the selected date, and hover feedback.

### Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| DisplayMonth | DateTime | First day of the current month | The month currently displayed; assigning a value automatically normalizes it to the 1st of that month, and a change raises DisplayMonthChanged. |
| SelectedDate | DateTime? | null | The selected date; assigning a non-null value switches DisplayMonth to the month that contains the date. |

### Events

| Event | Description |
| --- | --- |
| DateSelected | Raised when the user clicks a date cell (clicking a date that overflows into the previous/next month also selects it and jumps to that month). |
| DisplayMonthChanged | Raised when the displayed month changes (caused by clicking the navigation buttons or by setting DisplayMonth/SelectedDate). |

### Example

```csharp
var calendar = new FluentCalendar();
var state = new FluentLabel { Text = "Click a day", AutoSize = true };
calendar.DateSelected += (s, e) => state.Text = calendar.SelectedDate.HasValue
    ? calendar.SelectedDate.Value.ToString("D") : "Click a day";
calendar.DisplayMonthChanged += (s, e) =>
    Console.WriteLine(calendar.DisplayMonth.ToString("Y"));

// Preselect a date (automatically jumps to the corresponding month)
calendar.SelectedDate = new DateTime(2026, 5, 20);

Controls.Add(calendar);
Controls.Add(state);
```

### Notes

- The weekday row uses the current culture's `ShortestDayNames`, and the first day of the week comes from the current culture's `FirstDayOfWeek`.
- Default size 280×292, `TabStop = false`, so it does not take part in the Tab key focus cycle.
- Today is drawn as a filled circle in the accent color and the selected date as a rounded block in the accent color; dates outside the displayed month use the disabled text color.

## FluentCalendarDatePicker

Calendar-style date picker: a text shell plus a calendar icon; clicking opens a flyout that hosts an embedded `FluentCalendar`.

### Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| SelectedDate | DateTime? | null | The selected date; when empty the placeholder text is shown. Note: assigning in code also raises DateSelected. |
| PlaceholderText | string | "Pick a date" | Placeholder text shown while no date is selected. |

### Events

| Event | Description |
| --- | --- |
| DateSelected | Raised when the selected date changes, including both the user picking a date in the popped-up calendar and code setting SelectedDate. |

### Example

```csharp
var picker = new FluentCalendarDatePicker { Width = 200, PlaceholderText = "选择日期" };
var state = new FluentLabel { Text = "No date selected", AutoSize = true };
picker.DateSelected += (s, e) => state.Text = picker.SelectedDate.HasValue
    ? picker.SelectedDate.Value.ToString("D") : "No date selected";

Controls.Add(picker);
Controls.Add(state);
```

### Notes

- Default size 200×32; a click, the space bar, or the Enter key opens the calendar flyout, and Esc closes it.
- The selected date is displayed using the current culture's short date pattern ("d").
- The flyout is hosted by an internal `FluentPopupForm`, which closes automatically when the control is disposed.

## FluentDatePicker

Segmented date picker: three segments for day / month / year, each of which opens the corresponding drop-down list when clicked.

### Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| SelectedDate | DateTime | DateTime.Today | The selected date; a change of the value raises DateSelected. |

### Events

| Event | Description |
| --- | --- |
| DateSelected | Raised when the date changes because the user picked a new value in a segment list, or when a new value is assigned in code. |

### Example

```csharp
var datePicker = new FluentDatePicker { Width = 280 };
datePicker.DateSelected += (s, e) =>
    Console.WriteLine(datePicker.SelectedDate.ToString("d"));

// Preselect a date
datePicker.SelectedDate = new DateTime(2026, 11, 1);

Controls.Add(datePicker);
```

### Notes

- Default size 260×32; month names come from the current culture's `DateTimeFormat.MonthNames`.
- The year list spans the currently selected year ±50; when the month or year changes, the "day" segment is automatically narrowed to the number of days in the target month (so January 31 becomes February 28/29 when switching to February).
- The segment lists use an internal `FluentDropDownList`, and the pop-up position automatically avoids the bottom screen edge (opening upwards when there is not enough room).

## FluentTimePicker

Time picker: displays HH:mm, and clicking opens a two-column list of hours/minutes with Confirm (✓) and Cancel (✕) buttons at the bottom.

### Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| SelectedTime | TimeSpan | DateTime.Now.TimeOfDay (the current time at construction) | The selected time; only hours and minutes are displayed and selectable, and seconds are zeroed on confirmation. |

### Events

| Event | Description |
| --- | --- |
| TimeSelected | Raised when the user picks a new time by clicking the Confirm button, or when a new value is assigned in code; clicking Cancel does not raise it. |

### Example

```csharp
var timePicker = new FluentTimePicker { Width = 160 };
timePicker.TimeSelected += (s, e) =>
    Console.WriteLine(string.Format("{0:00}:{1:00}",
        timePicker.SelectedTime.Hours, timePicker.SelectedTime.Minutes));

// Preselect 09:30
timePicker.SelectedTime = new TimeSpan(9, 30, 0);

Controls.Add(timePicker);
```

### Notes

- Default size 160×32; the hour column covers 00–23 and the minute column 00–59, both on a 24-hour clock.
- The selection is committed only when the Confirm button is clicked; clicking Cancel, or simply closing the flyout, leaves SelectedTime unchanged.

## FluentColorPicker

Color picker: a hue/saturation spectrum area, a brightness slider, a hex input box, and a color preview swatch.

### Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| Color | Color | Red #FF0000 (initial HSV is 0,1,1) | The current color; stored internally as HSV and converted to RGB on read (Alpha is always 255). Assigning raises ColorChanged. |

### Events

| Event | Description |
| --- | --- |
| ColorChanged | Raised when the color changes: continuously while dragging in the spectrum area or on the brightness slider, and also when the hex input is committed or Color is set in code. |

### Example

```csharp
var picker = new FluentColorPicker();
var state = new FluentLabel { Text = "#FF0000", AutoSize = true };
picker.ColorChanged += (s, e) =>
{
    Color c = picker.Color;
    state.Text = string.Format("#{0:X2}{1:X2}{2:X2}", c.R, c.G, c.B);
};

// Preselect a color
picker.Color = Color.FromArgb(0x00, 0x78, 0xD4);

Controls.Add(picker);
Controls.Add(state);
```

### Notes

- Default size 260×264, `TabStop = false`; in the spectrum area the horizontal axis is hue (0–360°) and the vertical axis is saturation (1 at the top, 0 at the bottom), with the slider below controlling brightness.
- The hex input box accepts `#RRGGBB` or `RRGGBB` (6 digits) and commits on losing focus or when Enter is pressed; invalid input reverts to the current color's text.
- During dragging, ColorChanged fires at a high rate, so apply your own throttling if the handler does significant work.
