using System;
using System.Drawing;
using System.Windows.Forms;
using WinformUi.Controls;
using WinformUi.Theming;

namespace WinformUi.Gallery
{
    public class DateTimePage : GalleryPage
    {
        public DateTimePage() : base("Date & Time")
        {
            AddSection("Pickers");
            var row = AddRow();
            var calendarPicker = WithMargin(new FluentCalendarDatePicker { Width = 200 });
            var calendarState = WithMargin(new FluentLabel { Text = "No date selected", AutoSize = true });
            calendarPicker.DateSelected += (s, e) => calendarState.Text = calendarPicker.SelectedDate.HasValue
                ? calendarPicker.SelectedDate.Value.ToString("D") : "No date selected";
            row.Controls.AddRange(new Control[] { calendarPicker, calendarState });

            var row2 = AddRow();
            row2.Controls.Add(WithMargin(new FluentDatePicker { Width = 280 }));
            row2.Controls.Add(WithMargin(new FluentTimePicker { Width = 160 }));

            AddSection("Calendar");
            var calRow = AddRow();
            var calendar = new FluentCalendar { Margin = new Padding(0, 0, 16, 8) };
            var calState = WithMargin(new FluentLabel { Text = "Click a day", AutoSize = true });
            calendar.DateSelected += (s, e) => calState.Text = calendar.SelectedDate.HasValue
                ? calendar.SelectedDate.Value.ToString("D") : "Click a day";
            calRow.Controls.AddRange(new Control[] { calendar, calState });

            AddSection("Color picker");
            var colorRow = AddRow();
            var picker = new FluentColorPicker { Margin = new Padding(0, 0, 16, 8) };
            var colorState = WithMargin(new FluentLabel { Text = "#FF0000", AutoSize = true });
            picker.ColorChanged += (s, e) =>
            {
                Color c = picker.Color;
                colorState.Text = string.Format("#{0:X2}{1:X2}{2:X2}", c.R, c.G, c.B);
            };
            colorRow.Controls.AddRange(new Control[] { picker, colorState });
        }
    }

    public class ProgressPage : GalleryPage
    {
        private readonly Timer _timer;
        private readonly FluentProgressBar _determinate;

        public ProgressPage() : base("Progress & Sliders")
        {
            AddSection("Progress bars");
            var barRow = AddRow();
            _determinate = WithMargin(new FluentProgressBar { Value = 0, Width = 320 });
            barRow.Controls.Add(_determinate);
            var barRow2 = AddRow();
            barRow2.Controls.Add(WithMargin(new FluentProgressBar { IsIndeterminate = true, Width = 320 }));
            barRow2.Controls.Add(WithMargin(new FluentProgressRing()));
            barRow2.Controls.Add(WithMargin(new FluentProgressRing { Size = new Size(48, 48) }));

            _timer = new Timer { Interval = 50 };
            _timer.Tick += (s, e) =>
            {
                _determinate.Value = (_determinate.Value + 1) % 101;
                if (_taskbar != null && _taskbar.State == TaskbarProgressState.Normal)
                {
                    _taskbar.Value = (int)_determinate.Value;
                }
            };
            _timer.Start();

            AddSection("Sliders");
            var sliderRow = AddRow();
            var slider = WithMargin(new FluentSlider { Value = 40, Width = 300 });
            var sliderState = WithMargin(new FluentLabel { Text = "40", AutoSize = true });
            slider.ValueChanged += (s, e) => sliderState.Text = ((int)slider.Value).ToString();
            sliderRow.Controls.AddRange(new Control[] { slider, sliderState });
            var sliderRow2 = AddRow();
            sliderRow2.Controls.Add(WithMargin(new FluentSlider { Value = 60, TickFrequency = 10, Width = 300 }));
            sliderRow2.Controls.Add(WithMargin(new FluentSlider { Value = 30, Width = 200, Enabled = false }));

            AddSection("Rating");
            var ratingRow = AddRow();
            var rating = WithMargin(new FluentRatingControl { Value = 3 });
            var ratingState = WithMargin(new FluentLabel { Text = "3 / 5", AutoSize = true });
            rating.ValueChanged += (s, e) => ratingState.Text = rating.Value + " / 5";
            var thumb = WithMargin(new FluentThumbRate());
            var thumbState = WithMargin(new FluentLabel { Text = "No vote", AutoSize = true });
            thumb.ValueChanged += (s, e) => thumbState.Text = thumb.Value == 1 ? "Liked" : (thumb.Value == -1 ? "Disliked" : "No vote");
            ratingRow.Controls.AddRange(new Control[] { rating, ratingState, thumb, thumbState });

            AddSection("Arc");
            var arcRow = AddRow();
            arcRow.Controls.Add(WithMargin(new FluentArc { StartAngle = -90, EndAngle = 180 }));
            arcRow.Controls.Add(WithMargin(new FluentArc { StartAngle = 0, EndAngle = 270, Thickness = 8, Size = new Size(64, 64) }));

            AddSection("Taskbar button");
            AddDescription("Progress is drawn on the window's taskbar button; the Normal bar follows the progress bar above.");
            var taskbarRow = AddRow();
            var tbNormal = WithMargin(new FluentButton { Text = "Normal", AutoSize = true });
            var tbPaused = WithMargin(new FluentButton { Text = "Paused", AutoSize = true });
            var tbError = WithMargin(new FluentButton { Text = "Error", AutoSize = true });
            var tbIndeterminate = WithMargin(new FluentButton { Text = "Indeterminate", AutoSize = true });
            var tbClear = WithMargin(new FluentButton { Text = "Clear", AutoSize = true });
            tbNormal.Click += (s, e) =>
            {
                AttachTaskbar();
                _taskbar.State = TaskbarProgressState.Normal;
            };
            tbPaused.Click += (s, e) =>
            {
                AttachTaskbar();
                _taskbar.State = TaskbarProgressState.Paused;
            };
            tbError.Click += (s, e) =>
            {
                AttachTaskbar();
                _taskbar.State = TaskbarProgressState.Error;
            };
            tbIndeterminate.Click += (s, e) =>
            {
                AttachTaskbar();
                _taskbar.State = TaskbarProgressState.Indeterminate;
            };
            tbClear.Click += (s, e) =>
            {
                AttachTaskbar();
                _taskbar.Reset();
            };
            taskbarRow.Controls.AddRange(new Control[] { tbNormal, tbPaused, tbError, tbIndeterminate, tbClear });
        }

        private FluentTaskbarProgress _taskbar;

        private void AttachTaskbar()
        {
            if (_taskbar == null)
            {
                _taskbar = new FluentTaskbarProgress();
            }

            if (_taskbar.Form == null)
            {
                _taskbar.Form = FindForm();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _timer.Stop();
                _timer.Dispose();
                if (_taskbar != null)
                {
                    _taskbar.Dispose();
                }
            }

            base.Dispose(disposing);
        }
    }
}