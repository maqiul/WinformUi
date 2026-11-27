using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using WinformUi.Drawing;
using WinformUi.Theming;

namespace WinformUi.Controls
{
    /// <summary>
    /// Fluent slider: 4px track with accent fill and a
    /// 20px composite thumb (solid outer ring + accent inner dot).
    /// </summary>
    [System.ComponentModel.ToolboxItem(true)]
    public class FluentSlider : FluentControlBase
    {
        private const float ThumbSize = 20f;
        private const float TrackHeight = 4f;

        private double _minimum;
        private double _maximum = 100;
        private double _value;
        private double _tickFrequency;
        private bool _dragging;

        public event EventHandler ValueChanged;

        public FluentSlider()
        {
            Size = new Size(200, 32);
            Cursor = Cursors.Hand;
        }

        [DefaultValue(0d)]
        [Category("Fluent")]
        public double Minimum
        {
            get { return _minimum; }
            set { _minimum = value; Value = _value; Invalidate(); }
        }

        [DefaultValue(100d)]
        [Category("Fluent")]
        public double Maximum
        {
            get { return _maximum; }
            set { _maximum = value; Value = _value; Invalidate(); }
        }

        [DefaultValue(0d)]
        [Category("Fluent")]
        public double Value
        {
            get { return _value; }
            set
            {
                double clamped = Math.Max(_minimum, Math.Min(_maximum, value));
                if (_value != clamped)
                {
                    _value = clamped;
                    OnValueChanged(EventArgs.Empty);
                    Invalidate();
                }
            }
        }

        /// <summary>Tick mark interval; 0 disables ticks.</summary>
        [DefaultValue(0d)]
        [Category("Fluent")]
        public double TickFrequency
        {
            get { return _tickFrequency; }
            set { _tickFrequency = value; Invalidate(); }
        }

        protected virtual void OnValueChanged(EventArgs e)
        {
            ValueChanged?.Invoke(this, e);
        }

        private float TrackLeft => ThumbSize / 2f;
        private float TrackRight => Width - ThumbSize / 2f;

        private float ValueToX(double value)
        {
            double range = _maximum - _minimum;
            float fraction = range <= 0 ? 0 : (float)((value - _minimum) / range);
            return TrackLeft + fraction * (TrackRight - TrackLeft);
        }

        private double XToValue(float x)
        {
            float fraction = (x - TrackLeft) / Math.Max(1f, TrackRight - TrackLeft);
            fraction = Math.Max(0, Math.Min(1, fraction));
            return _minimum + fraction * (_maximum - _minimum);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left && Enabled)
            {
                _dragging = true;
                Value = XToValue(e.X);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_dragging)
            {
                Value = XToValue(e.X);
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _dragging = false;
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if (Enabled)
            {
                double step = (_maximum - _minimum) / 100d;
                Value = _value + (e.Delta > 0 ? step : -step) * 3;
            }
        }

        protected override bool IsInputKey(Keys keyData)
        {
            switch (keyData & Keys.KeyCode)
            {
                case Keys.Left:
                case Keys.Right:
                case Keys.Up:
                case Keys.Down:
                case Keys.Home:
                case Keys.End:
                    return true;
                default:
                    return base.IsInputKey(keyData);
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            double step = (_maximum - _minimum) / 100d;
            switch (e.KeyCode)
            {
                case Keys.Left:
                case Keys.Down:
                    Value = _value - step;
                    e.Handled = true;
                    break;
                case Keys.Right:
                case Keys.Up:
                    Value = _value + step;
                    e.Handled = true;
                    break;
                case Keys.Home:
                    Value = _minimum;
                    e.Handled = true;
                    break;
                case Keys.End:
                    Value = _maximum;
                    e.Handled = true;
                    break;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            ControlHelper.PaintFullBackground(this, e.Graphics);
            Graphics g = e.Graphics;
            FluentGraphics.SetupGraphics(g);

            float centerY = Height / 2f;
            float thumbX = ValueToX(_value);
            bool disabled = !Enabled;

            // Ticks
            if (_tickFrequency > 0 && !disabled)
            {
                Color tickColor = ControlHelper.BlendOverParent(this, Palette.ControlStrongFillDefault);
                using (var brush = new SolidBrush(tickColor))
                {
                    for (double v = _minimum; v <= _maximum + 0.0001; v += _tickFrequency)
                    {
                        float x = ValueToX(v);
                        g.FillRectangle(brush, x - 0.5f, centerY + ThumbSize / 2f + 2f, 1f, 4f);
                    }
                }
            }

            // Track
            var track = new RectangleF(TrackLeft, centerY - TrackHeight / 2f, TrackRight - TrackLeft, TrackHeight);
            Color trackColor = disabled
                ? ControlHelper.BlendOverParent(this, Palette.ControlStrongFillDisabled)
                : ControlHelper.BlendOverParent(this, Palette.ControlStrongFillDefault);
            using (var brush = new SolidBrush(trackColor))
            {
                FluentGraphics.FillRoundedRect(g, brush, track, TrackHeight / 2f);
            }

            // Filled portion
            float fillWidth = thumbX - TrackLeft;
            if (fillWidth > 0.5f)
            {
                Color fillColor = disabled ? Palette.AccentFillDisabled : Palette.SystemAccentColorPrimary;
                using (var brush = new SolidBrush(fillColor))
                {
                    FluentGraphics.FillRoundedRect(g, brush, new RectangleF(TrackLeft, centerY - TrackHeight / 2f, fillWidth, TrackHeight), TrackHeight / 2f);
                }
            }

            // Thumb: outer solid circle + elevation border + inner accent dot.
            var outer = new RectangleF(thumbX - ThumbSize / 2f, centerY - ThumbSize / 2f, ThumbSize, ThumbSize);
            Color outerFill = disabled
                ? ControlHelper.BlendOverParent(this, Palette.ControlFillDisabled)
                : Palette.ControlSolidFillDefault;
            using (var brush = new SolidBrush(outerFill))
            {
                g.FillEllipse(brush, outer);
            }

            if (!disabled)
            {
                using (var pen = new Pen(Palette.ControlStrokeDefault, 1f))
                {
                    g.DrawEllipse(pen, outer);
                }
            }

            float innerSize = IsHovered || _dragging ? 12f : 10f;
            if (IsPressed)
            {
                innerSize = 8f;
            }

            Color innerColor = disabled ? Palette.AccentFillDisabled : Palette.SystemAccentColorPrimary;
            using (var brush = new SolidBrush(innerColor))
            {
                g.FillEllipse(brush, thumbX - innerSize / 2f, centerY - innerSize / 2f, innerSize, innerSize);
            }

            if (Focused && ShowFocusCues)
            {
                FluentGraphics.DrawFocusVisual(g, RectangleF.Inflate(outer, 2f, 2f), ThumbSize / 2f, Palette);
            }
        }
    }
}