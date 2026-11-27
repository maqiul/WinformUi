using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using WinformUi.Drawing;
using WinformUi.Theming;

namespace WinformUi.Controls
{
    /// <summary>
    /// Owner-drawn month calendar:
    /// header navigation, weekday row, 6x7 day grid with today/selected/hover treatments.
    /// </summary>
    [System.ComponentModel.ToolboxItem(true)]
    public class FluentCalendar : FluentControlBase
    {
        private DateTime _displayMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        private DateTime? _selectedDate;
        private int _hoverDay; // day number in displayed month, 0 = none
        private int _hoverHeader; // 0 none, 1 prev, 2 next
        private DateTime _gridStart;

        public event EventHandler DateSelected;
        public event EventHandler DisplayMonthChanged;

        public FluentCalendar()
        {
            Size = new Size(280, 292);
            Cursor = Cursors.Hand;
            TabStop = false;
            RecomputeGridStart();
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public DateTime DisplayMonth
        {
            get { return _displayMonth; }
            set
            {
                var month = new DateTime(value.Year, value.Month, 1);
                if (_displayMonth != month)
                {
                    _displayMonth = month;
                    RecomputeGridStart();
                    OnDisplayMonthChanged(EventArgs.Empty);
                    Invalidate();
                }
            }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public DateTime? SelectedDate
        {
            get { return _selectedDate; }
            set
            {
                _selectedDate = value;
                if (value.HasValue)
                {
                    DisplayMonth = value.Value;
                }

                Invalidate();
            }
        }

        protected virtual void OnDateSelected(EventArgs e)
        {
            DateSelected?.Invoke(this, e);
        }

        protected virtual void OnDisplayMonthChanged(EventArgs e)
        {
            DisplayMonthChanged?.Invoke(this, e);
        }

        private void RecomputeGridStart()
        {
            int firstDayOfWeek = (int)CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek;
            int offset = ((int)_displayMonth.DayOfWeek - firstDayOfWeek + 7) % 7;
            _gridStart = _displayMonth.AddDays(-offset);
        }

        private const int Pad = 8;
        private const int HeaderHeight = 36;
        private const int WeekdayHeight = 24;

        private float CellWidth => (Width - Pad * 2) / 7f;
        private float CellHeight => 30f;
        private float GridTop => Pad + HeaderHeight + WeekdayHeight;

        private RectangleF PrevButtonRect => new RectangleF(Width - Pad - 60, Pad + 4, 28, 28);
        private RectangleF NextButtonRect => new RectangleF(Width - Pad - 28, Pad + 4, 28, 28);

        private RectangleF DayRect(int index)
        {
            int row = index / 7;
            int col = index % 7;
            return new RectangleF(Pad + col * CellWidth, GridTop + row * CellHeight, CellWidth, CellHeight);
        }

        private int DayIndexAt(Point point)
        {
            if (point.Y < GridTop || point.Y > GridTop + 6 * CellHeight)
            {
                return -1;
            }

            int col = (int)((point.X - Pad) / CellWidth);
            int row = (int)((point.Y - GridTop) / CellHeight);
            if (col < 0 || col > 6 || row < 0 || row > 5)
            {
                return -1;
            }

            return row * 7 + col;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int header = PrevButtonRect.Contains(e.Location) ? 1 : (NextButtonRect.Contains(e.Location) ? 2 : 0);
            int index = DayIndexAt(e.Location);
            int day = 0;
            if (index >= 0)
            {
                DateTime date = _gridStart.AddDays(index);
                if (date.Month == _displayMonth.Month)
                {
                    day = date.Day;
                }
            }

            if (_hoverHeader != header || _hoverDay != day)
            {
                _hoverHeader = header;
                _hoverDay = day;
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hoverHeader = 0;
            _hoverDay = 0;
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            if (PrevButtonRect.Contains(e.Location))
            {
                DisplayMonth = _displayMonth.AddMonths(-1);
                return;
            }

            if (NextButtonRect.Contains(e.Location))
            {
                DisplayMonth = _displayMonth.AddMonths(1);
                return;
            }

            int index = DayIndexAt(e.Location);
            if (index >= 0)
            {
                DateTime date = _gridStart.AddDays(index);
                _selectedDate = date;
                if (date.Month != _displayMonth.Month)
                {
                    DisplayMonth = date;
                }

                OnDateSelected(EventArgs.Empty);
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            ControlHelper.PaintFullBackground(this, e.Graphics);
            Graphics g = e.Graphics;
            FluentGraphics.SetupGraphics(g);
            FluentPalette p = Palette;

            using (var brush = new SolidBrush(p.AcrylicBackgroundFillDefault))
            {
                g.FillRectangle(brush, ClientRectangle);
            }

            // Header: month title + navigation buttons.
            string title = _displayMonth.ToString("Y", CultureInfo.CurrentCulture);
            using (var format = new StringFormat { LineAlignment = StringAlignment.Center })
            {
                FluentGraphics.DrawText(g, title, FluentTypography.BodyStrong, p.TextFillPrimary, new RectangleF(Pad + 4, Pad, Width - 80, HeaderHeight), format);
            }

            DrawHeaderButton(g, PrevButtonRect, 0xE76B, _hoverHeader == 1);
            DrawHeaderButton(g, NextButtonRect, 0xE76C, _hoverHeader == 2);

            // Weekday row.
            string[] dayNames = CultureInfo.CurrentCulture.DateTimeFormat.ShortestDayNames;
            int firstDayOfWeek = (int)CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek;
            using (var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap })
            {
                for (int i = 0; i < 7; i++)
                {
                    string name = dayNames[(firstDayOfWeek + i) % 7];
                    FluentGraphics.DrawText(g, name, Font, p.TextFillSecondary, new RectangleF(Pad + i * CellWidth, Pad + HeaderHeight, CellWidth, WeekdayHeight), format);
                }
            }

            // Day grid.
            DateTime today = DateTime.Today;
            using (var centerFormat = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                for (int i = 0; i < 42; i++)
                {
                    DateTime date = _gridStart.AddDays(i);
                    RectangleF cell = DayRect(i);
                    var inner = RectangleF.Inflate(cell, -3, -2);

                    bool inMonth = date.Month == _displayMonth.Month;
                    bool isToday = date == today;
                    bool isSelected = _selectedDate.HasValue && date == _selectedDate.Value;
                    bool isHover = inMonth && date.Day == _hoverDay;

                    if (isSelected)
                    {
                        using (var brush = new SolidBrush(p.SystemAccentColorPrimary))
                        {
                            FluentGraphics.FillRoundedRect(g, brush, inner, FluentGraphics.ControlCornerRadius);
                        }
                    }
                    else if (isToday)
                    {
                        using (var brush = new SolidBrush(p.SystemAccentColorPrimary))
                        {
                            float d = Math.Min(inner.Width, inner.Height);
                            g.FillEllipse(brush, inner.X + (inner.Width - d) / 2f, inner.Y + (inner.Height - d) / 2f, d, d);
                        }
                    }
                    else if (isHover)
                    {
                        using (var brush = new SolidBrush(AccentColor.Blend(p.SubtleFillSecondary, p.AcrylicBackgroundFillDefault)))
                        {
                            FluentGraphics.FillRoundedRect(g, brush, inner, FluentGraphics.ControlCornerRadius);
                        }
                    }

                    Color textColor;
                    if (isSelected || isToday)
                    {
                        textColor = p.TextOnAccentPrimary;
                    }
                    else if (!inMonth)
                    {
                        textColor = p.TextFillDisabled;
                    }
                    else
                    {
                        textColor = p.TextFillPrimary;
                    }

                    FluentGraphics.DrawText(g, date.Day.ToString(), Font, textColor, cell, centerFormat);
                }
            }
        }

        private void DrawHeaderButton(Graphics g, RectangleF rect, int glyph, bool hovered)
        {
            FluentPalette p = Palette;
            if (hovered)
            {
                using (var brush = new SolidBrush(AccentColor.Blend(p.SubtleFillSecondary, p.AcrylicBackgroundFillDefault)))
                {
                    FluentGraphics.FillRoundedRect(g, brush, rect, FluentGraphics.ControlCornerRadius);
                }
            }

            FluentGraphics.DrawGlyph(g, FluentGraphics.Glyph(glyph), FluentTypography.Icon(9f), p.TextFillPrimary, rect);
        }
    }

    /// <summary>
    /// Fluent calendar date picker:
    /// text shell + calendar glyph, flyout month calendar.
    /// </summary>
    public class FluentCalendarDatePicker : Control
    {
        private ThemeSubscription _themeSubscription;
        private DateTime? _selectedDate;
        private bool _hovered;
        private FluentPopupForm _popup;
        private string _placeholderText = "Pick a date";

        public event EventHandler DateSelected;

        protected override void OnPaintBackground(PaintEventArgs e) => ControlHelper.PaintFullBackground(this, e.Graphics);

        public FluentCalendarDatePicker()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            Font = FluentTypography.Body;
            Size = new Size(200, 32);
            Cursor = Cursors.Hand;
            TabStop = true;
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public DateTime? SelectedDate
        {
            get { return _selectedDate; }
            set
            {
                _selectedDate = value;
                OnDateSelected(EventArgs.Empty);
                Invalidate();
            }
        }

        [DefaultValue("Pick a date")]
        [Category("Fluent")]
        public string PlaceholderText
        {
            get { return _placeholderText; }
            set { _placeholderText = value; Invalidate(); }
        }

        protected FluentPalette Palette => ApplicationThemeManager.Palette;

        protected virtual void OnDateSelected(EventArgs e)
        {
            DateSelected?.Invoke(this, e);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (_themeSubscription == null)
            {
                _themeSubscription = new ThemeSubscription(this, () => Invalidate());
            }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _hovered = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hovered = false;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left && Enabled)
            {
                if (!Focused) Focus();
                OpenCalendar();
            }
        }

        protected override bool IsInputKey(Keys keyData)
        {
            return (keyData & Keys.KeyCode) == Keys.Space || base.IsInputKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter)
            {
                OpenCalendar();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                _popup?.Close();
                e.Handled = true;
            }
        }

        private void OpenCalendar()
        {
            if (_popup != null || !Enabled)
            {
                return;
            }

            _popup = new FluentPopupForm();
            var calendar = new FluentCalendar
            {
                SelectedDate = _selectedDate,
                Location = new Point(4, 4)
            };
            calendar.DateSelected += (s, e) =>
            {
                SelectedDate = calendar.SelectedDate;
                _popup?.Close();
            };
            _popup.ClientSize = new Size(calendar.Width + 8, calendar.Height + 8);
            _popup.Controls.Add(calendar);
            _popup.FormClosed += (s, e) => { _popup = null; Invalidate(); };
            _popup.ShowBelow(this, _popup.Height, _popup.ClientSize.Width);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            ControlHelper.PaintFullBackground(this, e.Graphics);
            Graphics g = e.Graphics;
            FluentGraphics.SetupGraphics(g);

            Color fill = !Enabled
                ? ControlHelper.BlendOverParent(this, Palette.ControlFillDisabled)
                : (_hovered || _popup != null
                    ? ControlHelper.BlendOverParent(this, Palette.ControlFillSecondary)
                    : ControlHelper.BlendOverParent(this, Palette.ControlFillDefault));

            var bounds = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
            using (var brush = new SolidBrush(fill))
            {
                FluentGraphics.FillRoundedRect(g, brush, bounds, FluentGraphics.ControlCornerRadius);
            }

            FluentGraphics.DrawElevationBorder(g, bounds, FluentGraphics.ControlCornerRadius, Palette);

            bool hasDate = _selectedDate.HasValue;
            string text = hasDate ? _selectedDate.Value.ToString("d", CultureInfo.CurrentCulture) : _placeholderText;
            Color textColor = !Enabled ? Palette.TextFillDisabled : (hasDate ? Palette.TextFillPrimary : Palette.TextPlaceholder);
            using (var format = new StringFormat { LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap })
            {
                FluentGraphics.DrawText(g, text, Font, textColor, new RectangleF(12, 0, Width - 44, Height), format);
            }

            Color glyphColor = Enabled ? Palette.TextFillSecondary : Palette.TextFillDisabled;
            FluentGraphics.DrawGlyph(g, FluentGraphics.Glyph(0xE787), FluentTypography.Icon(11f), glyphColor, new RectangleF(Width - 28, (Height - 16) / 2f, 16, 16));

            if (Focused && ShowFocusCues && _popup == null)
            {
                FluentGraphics.DrawFocusVisual(g, new RectangleF(1.5f, 1.5f, Width - 3f, Height - 3f), FluentGraphics.ControlCornerRadius, Palette);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _popup?.Close();
                _themeSubscription?.Dispose();
                _themeSubscription = null;
            }

            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// Fluent date picker: day / month / year segments,
    /// each opening a flyout list.
    /// </summary>
    public class FluentDatePicker : Control
    {
        private ThemeSubscription _themeSubscription;
        private DateTime _selectedDate = DateTime.Today;
        private FluentPopupForm _popup;
        private int _hoverSegment; // 0 none, 1 day, 2 month, 3 year

        public event EventHandler DateSelected;

        protected override void OnPaintBackground(PaintEventArgs e) => ControlHelper.PaintFullBackground(this, e.Graphics);

        public FluentDatePicker()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            Font = FluentTypography.Body;
            Size = new Size(260, 32);
            Cursor = Cursors.Hand;
            TabStop = true;
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public DateTime SelectedDate
        {
            get { return _selectedDate; }
            set
            {
                if (_selectedDate != value)
                {
                    _selectedDate = value;
                    OnDateSelected(EventArgs.Empty);
                    Invalidate();
                }
            }
        }

        protected FluentPalette Palette => ApplicationThemeManager.Palette;

        protected virtual void OnDateSelected(EventArgs e)
        {
            DateSelected?.Invoke(this, e);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (_themeSubscription == null)
            {
                _themeSubscription = new ThemeSubscription(this, () => Invalidate());
            }
        }

        private RectangleF SegmentRect(int segment)
        {
            float w = (Width - 1f) / 3f;
            return new RectangleF(0.5f + (segment - 1) * w, 0.5f, w, Height - 1f);
        }

        private int SegmentAt(Point point)
        {
            for (int i = 1; i <= 3; i++)
            {
                if (SegmentRect(i).Contains(point))
                {
                    return i;
                }
            }

            return 0;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int segment = SegmentAt(e.Location);
            if (_hoverSegment != segment)
            {
                _hoverSegment = segment;
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hoverSegment = 0;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left && Enabled)
            {
                if (!Focused) Focus();
                int segment = SegmentAt(e.Location);
                if (segment > 0)
                {
                    OpenSegment(segment);
                }
            }
        }

        private void OpenSegment(int segment)
        {
            _popup?.Close();

            var items = new List<object>();
            int selected = 0;

            if (segment == 1)
            {
                int days = DateTime.DaysInMonth(_selectedDate.Year, _selectedDate.Month);
                for (int d = 1; d <= days; d++) items.Add(d);
                selected = _selectedDate.Day - 1;
            }
            else if (segment == 2)
            {
                string[] names = CultureInfo.CurrentCulture.DateTimeFormat.MonthNames;
                for (int m = 0; m < 12; m++) items.Add(names[m]);
                selected = _selectedDate.Month - 1;
            }
            else
            {
                for (int y = _selectedDate.Year - 50; y <= _selectedDate.Year + 50; y++) items.Add(y);
                selected = 50;
            }

            _popup = new FluentPopupForm();
            var list = new FluentDropDownList
            {
                Items = items,
                SelectedIndex = selected,
                Dock = DockStyle.Fill
            };
            list.ItemClicked += (s, e) =>
            {
                int index = list.SelectedIndex;
                DateTime date = _selectedDate;
                if (segment == 1)
                {
                    date = new DateTime(date.Year, date.Month, index + 1);
                }
                else if (segment == 2)
                {
                    int day = Math.Min(date.Day, DateTime.DaysInMonth(date.Year, index + 1));
                    date = new DateTime(date.Year, index + 1, day);
                }
                else
                {
                    int year = (int)items[index];
                    int day = Math.Min(date.Day, DateTime.DaysInMonth(year, date.Month));
                    date = new DateTime(year, date.Month, day);
                }

                SelectedDate = date;
                _popup?.Close();
            };
            _popup.Padding = new Padding(4);
            _popup.Controls.Add(list);
            _popup.FormClosed += (s, e) => { _popup = null; Invalidate(); };

            RectangleF rect = SegmentRect(segment);
            int height = Math.Min(items.Count * list.ItemHeight + 8, 320);
            Point screen = PointToScreen(new Point((int)rect.X, Height));
            Screen area = Screen.FromControl(this);
            if (screen.Y + height > area.WorkingArea.Bottom)
            {
                screen = PointToScreen(new Point((int)rect.X, -height));
            }

            _popup.Bounds = new Rectangle(screen, new Size((int)rect.Width + 8, height));
            _popup.Show(FindForm());
            list.Focus();
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            ControlHelper.PaintFullBackground(this, e.Graphics);
            Graphics g = e.Graphics;
            FluentGraphics.SetupGraphics(g);

            var bounds = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
            Color shellFill = ControlHelper.BlendOverParent(this, Enabled ? Palette.ControlFillDefault : Palette.ControlFillDisabled);
            using (var brush = new SolidBrush(shellFill))
            {
                FluentGraphics.FillRoundedRect(g, brush, bounds, FluentGraphics.ControlCornerRadius);
            }

            string[] texts =
            {
                _selectedDate.Day.ToString(),
                CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(_selectedDate.Month),
                _selectedDate.Year.ToString()
            };

            using (var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap, Trimming = StringTrimming.EllipsisCharacter })
            {
                for (int i = 1; i <= 3; i++)
                {
                    RectangleF rect = SegmentRect(i);
                    if (Enabled && _hoverSegment == i)
                    {
                        using (var brush = new SolidBrush(ControlHelper.BlendOverParent(this, Palette.ControlFillSecondary)))
                        {
                            FluentGraphics.FillRoundedRect(g, brush, RectangleF.Inflate(rect, -2, -2), FluentGraphics.ControlCornerRadius);
                        }
                    }

                    Color textColor = Enabled ? Palette.TextFillPrimary : Palette.TextFillDisabled;
                    FluentGraphics.DrawText(g, texts[i - 1], Font, textColor, rect, format);

                    if (i < 3)
                    {
                        using (var pen = new Pen(Palette.DividerStrokeDefault, 1f))
                        {
                            g.DrawLine(pen, rect.Right, 6, rect.Right, Height - 6);
                        }
                    }
                }
            }

            FluentGraphics.DrawElevationBorder(g, bounds, FluentGraphics.ControlCornerRadius, Palette);

            if (Focused && ShowFocusCues && _popup == null)
            {
                FluentGraphics.DrawFocusVisual(g, new RectangleF(1.5f, 1.5f, Width - 3f, Height - 3f), FluentGraphics.ControlCornerRadius, Palette);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _popup?.Close();
                _themeSubscription?.Dispose();
                _themeSubscription = null;
            }

            base.Dispose(disposing);
        }
    }
}