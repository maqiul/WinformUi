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
    /// Fluent time picker: hour/minute flyout columns with
    /// accept / cancel footer.
    /// </summary>
    public class FluentTimePicker : Control
    {
        private ThemeSubscription _themeSubscription;
        private TimeSpan _selectedTime = DateTime.Now.TimeOfDay;
        private bool _hovered;
        private FluentPopupForm _popup;

        public event EventHandler TimeSelected;

        protected override void OnPaintBackground(PaintEventArgs e) => ControlHelper.PaintFullBackground(this, e.Graphics);

        public FluentTimePicker()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            Font = FluentTypography.Body;
            Size = new Size(160, 32);
            Cursor = Cursors.Hand;
            TabStop = true;
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public TimeSpan SelectedTime
        {
            get { return _selectedTime; }
            set
            {
                if (_selectedTime != value)
                {
                    _selectedTime = value;
                    OnTimeSelected(EventArgs.Empty);
                    Invalidate();
                }
            }
        }

        protected FluentPalette Palette => ApplicationThemeManager.Palette;

        protected virtual void OnTimeSelected(EventArgs e)
        {
            TimeSelected?.Invoke(this, e);
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
                OpenPopup();
            }
        }

        private void OpenPopup()
        {
            if (_popup != null || !Enabled)
            {
                return;
            }

            _popup = new FluentPopupForm();

            var hours = new List<object>();
            var minutes = new List<object>();
            for (int h = 0; h < 24; h++) hours.Add(h.ToString("00"));
            for (int m = 0; m < 60; m++) minutes.Add(m.ToString("00"));

            var hourList = new FluentDropDownList { Items = hours, SelectedIndex = _selectedTime.Hours, Bounds = new Rectangle(4, 4, 80, 260) };
            var minuteList = new FluentDropDownList { Items = minutes, SelectedIndex = _selectedTime.Minutes, Bounds = new Rectangle(88, 4, 80, 260) };

            var accept = new FluentButton { Appearance = ControlAppearance.Primary, Icon = SymbolRegular.Checkmark24, Bounds = new Rectangle(4, 270, 80, 30) };
            var cancel = new FluentButton { Icon = SymbolRegular.Dismiss24, Bounds = new Rectangle(88, 270, 80, 30) };

            accept.Click += (s, e) =>
            {
                int hour = hourList.SelectedIndex < 0 ? 0 : hourList.SelectedIndex;
                int minute = minuteList.SelectedIndex < 0 ? 0 : minuteList.SelectedIndex;
                SelectedTime = new TimeSpan(hour, minute, 0);
                _popup?.Close();
            };
            cancel.Click += (s, e) => _popup?.Close();

            _popup.ClientSize = new Size(172, 306);
            _popup.Controls.AddRange(new Control[] { hourList, minuteList, accept, cancel });
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

            string text = string.Format("{0:00}:{1:00}", _selectedTime.Hours, _selectedTime.Minutes);
            Color textColor = Enabled ? Palette.TextFillPrimary : Palette.TextFillDisabled;
            using (var format = new StringFormat { LineAlignment = StringAlignment.Center })
            {
                FluentGraphics.DrawText(g, text, Font, textColor, new RectangleF(12, 0, Width - 44, Height), format);
            }

            Color glyphColor = Enabled ? Palette.TextFillSecondary : Palette.TextFillDisabled;
            FluentGraphics.DrawGlyph(g, FluentGraphics.Glyph(0xE823), FluentTypography.Icon(11f), glyphColor, new RectangleF(Width - 28, (Height - 16) / 2f, 16, 16));

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
    /// Fluent color picker: hue/saturation spectrum,
    /// value slider, hex input and preview swatch.
    /// </summary>
    [System.ComponentModel.ToolboxItem(true)]
    public class FluentColorPicker : FluentControlBase
    {
        private float _hue;
        private float _saturation = 1f;
        private float _value = 1f;
        private Bitmap _spectrumCache;
        private readonly TextBox _hexBox;
        private bool _updatingHex;
        private int _dragMode; // 0 none, 1 spectrum, 2 value

        public event EventHandler ColorChanged;

        public FluentColorPicker()
        {
            Size = new Size(260, 264);
            TabStop = false;

            _hexBox = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Font = FluentTypography.Body,
                MaxLength = 7,
                Width = 90
            };
            _hexBox.Leave += (s, e) => CommitHex();
            _hexBox.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    CommitHex();
                    e.SuppressKeyPress = true;
                }
            };
            Controls.Add(_hexBox);
            UpdateHexText();
            LayoutHexBox();
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color Color
        {
            get { return HsvToRgb(_hue, _saturation, _value); }
            set
            {
                RgbToHsv(value, out _hue, out _saturation, out _value);
                InvalidateSpectrum();
                UpdateHexText();
                OnColorChanged(EventArgs.Empty);
                Invalidate();
            }
        }

        protected virtual void OnColorChanged(EventArgs e)
        {
            ColorChanged?.Invoke(this, e);
        }

        private RectangleF SpectrumRect => new RectangleF(8, 8, Width - 16, Height - 104);
        private RectangleF ValueRect => new RectangleF(8, Height - 88, Width - 16, 16);
        private RectangleF SwatchRect => new RectangleF(8, Height - 56, 32, 32);

        private void LayoutHexBox()
        {
            if (_hexBox == null)
            {
                return;
            }

            _hexBox.Location = new Point(48, Height - 48);
            _hexBox.BackColor = ControlHelper.BlendOverParent(this, Palette.ControlFillDefault);
            _hexBox.ForeColor = Palette.TextFillPrimary;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            InvalidateSpectrum();
            LayoutHexBox();
        }

        protected override void OnThemeChanged()
        {
            base.OnThemeChanged();
            LayoutHexBox();
        }

        private void InvalidateSpectrum()
        {
            _spectrumCache?.Dispose();
            _spectrumCache = null;
        }

        private void UpdateHexText()
        {
            _updatingHex = true;
            Color c = Color;
            _hexBox.Text = string.Format("#{0:X2}{1:X2}{2:X2}", c.R, c.G, c.B);
            _updatingHex = false;
        }

        private void CommitHex()
        {
            if (_updatingHex)
            {
                return;
            }

            string text = (_hexBox.Text ?? string.Empty).Trim().TrimStart('#');
            if (text.Length == 6)
            {
                int rgb;
                if (int.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out rgb))
                {
                    Color = Color.FromArgb(255, (rgb >> 16) & 0xFF, (rgb >> 8) & 0xFF, rgb & 0xFF);
                    return;
                }
            }

            UpdateHexText();
        }

        private Bitmap BuildSpectrum()
        {
            int w = Math.Max(1, (int)SpectrumRect.Width);
            int h = Math.Max(1, (int)SpectrumRect.Height);
            var bitmap = new Bitmap(w, h);

            for (int x = 0; x < w; x++)
            {
                float hue = x / (float)Math.Max(1, w - 1) * 360f;
                Color pure = HsvToRgb(hue, 1f, 1f);
                using (var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                    new Rectangle(x, 0, 1, h), Color.White, pure, System.Drawing.Drawing2D.LinearGradientMode.Vertical))
                {
                    using (var g = Graphics.FromImage(bitmap))
                    {
                        g.FillRectangle(brush, x, 0, 1, h);
                    }
                }
            }

            return bitmap;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            if (SpectrumRect.Contains(e.Location))
            {
                _dragMode = 1;
                UpdateFromSpectrum(e.Location);
            }
            else if (ValueRect.Contains(e.Location))
            {
                _dragMode = 2;
                UpdateFromValue(e.Location);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_dragMode == 1) UpdateFromSpectrum(e.Location);
            else if (_dragMode == 2) UpdateFromValue(e.Location);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _dragMode = 0;
        }

        private void UpdateFromSpectrum(Point point)
        {
            RectangleF rect = SpectrumRect;
            _hue = Math.Max(0, Math.Min(1, (point.X - rect.X) / rect.Width)) * 360f;
            _saturation = 1f - Math.Max(0, Math.Min(1, (point.Y - rect.Y) / rect.Height));
            UpdateHexText();
            OnColorChanged(EventArgs.Empty);
            Invalidate();
        }

        private void UpdateFromValue(Point point)
        {
            RectangleF rect = ValueRect;
            _value = Math.Max(0, Math.Min(1, (point.X - rect.X) / rect.Width));
            UpdateHexText();
            OnColorChanged(EventArgs.Empty);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            ControlHelper.PaintFullBackground(this, e.Graphics);
            Graphics g = e.Graphics;
            FluentGraphics.SetupGraphics(g);

            // Spectrum
            RectangleF spectrum = SpectrumRect;
            if (_spectrumCache == null)
            {
                _spectrumCache = BuildSpectrum();
            }

            using (var path = FluentGraphics.RoundedRect(spectrum, FluentGraphics.ControlCornerRadius))
            {
                g.SetClip(path);
                g.DrawImage(_spectrumCache, spectrum);
                g.ResetClip();
            }

            using (var pen = new Pen(Palette.ControlStrokeDefault, 1f))
            {
                FluentGraphics.DrawRoundedRect(g, pen, spectrum, FluentGraphics.ControlCornerRadius);
            }

            // Spectrum marker
            float markerX = spectrum.X + _hue / 360f * spectrum.Width;
            float markerY = spectrum.Y + (1f - _saturation) * spectrum.Height;
            using (var pen = new Pen(Color.White, 2f))
            {
                g.DrawEllipse(pen, markerX - 6, markerY - 6, 12, 12);
            }

            using (var pen = new Pen(Color.FromArgb(120, 0, 0, 0), 1f))
            {
                g.DrawEllipse(pen, markerX - 7, markerY - 7, 14, 14);
            }

            // Value slider
            RectangleF valueRect = ValueRect;
            Color full = HsvToRgb(_hue, _saturation, 1f);
            using (var brush = new System.Drawing.Drawing2D.LinearGradientBrush(valueRect, Color.Black, full, System.Drawing.Drawing2D.LinearGradientMode.Horizontal))
            using (var path = FluentGraphics.RoundedRect(valueRect, 8f))
            {
                g.FillPath(brush, path);
            }

            float thumbX = valueRect.X + _value * valueRect.Width;
            using (var brush = new SolidBrush(Palette.ControlSolidFillDefault))
            {
                g.FillEllipse(brush, thumbX - 8, valueRect.Y + valueRect.Height / 2f - 8, 16, 16);
            }

            using (var pen = new Pen(Palette.ControlStrokeDefault, 1f))
            {
                g.DrawEllipse(pen, thumbX - 8, valueRect.Y + valueRect.Height / 2f - 8, 16, 16);
            }

            // Swatch
            using (var brush = new SolidBrush(Color))
            {
                FluentGraphics.FillRoundedRect(g, brush, SwatchRect, FluentGraphics.ControlCornerRadius);
            }

            using (var pen = new Pen(Palette.ControlStrokeDefault, 1f))
            {
                FluentGraphics.DrawRoundedRect(g, pen, SwatchRect, FluentGraphics.ControlCornerRadius);
            }
        }

        internal static Color HsvToRgb(float h, float s, float v)
        {
            h = h % 360f;
            if (h < 0) h += 360f;
            float c = v * s;
            float x = c * (1 - Math.Abs((h / 60f) % 2 - 1));
            float m = v - c;
            float r, gg, b;

            if (h < 60) { r = c; gg = x; b = 0; }
            else if (h < 120) { r = x; gg = c; b = 0; }
            else if (h < 180) { r = 0; gg = c; b = x; }
            else if (h < 240) { r = 0; gg = x; b = c; }
            else if (h < 300) { r = x; gg = 0; b = c; }
            else { r = c; gg = 0; b = x; }

            return Color.FromArgb(255,
                (int)Math.Round((r + m) * 255),
                (int)Math.Round((gg + m) * 255),
                (int)Math.Round((b + m) * 255));
        }

        internal static void RgbToHsv(Color color, out float h, out float s, out float v)
        {
            float r = color.R / 255f, gg = color.G / 255f, b = color.B / 255f;
            float max = Math.Max(r, Math.Max(gg, b));
            float min = Math.Min(r, Math.Min(gg, b));
            float delta = max - min;

            v = max;
            s = max == 0 ? 0 : delta / max;

            if (delta == 0) h = 0;
            else if (max == r) h = 60 * (((gg - b) / delta) % 6);
            else if (max == gg) h = 60 * (((b - r) / delta) + 2);
            else h = 60 * (((r - gg) / delta) + 4);

            if (h < 0) h += 360;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                InvalidateSpectrum();
            }

            base.Dispose(disposing);
        }
    }
}