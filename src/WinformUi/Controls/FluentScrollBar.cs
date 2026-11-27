using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using WinformUi.Drawing;
using WinformUi.Theming;

namespace WinformUi.Controls
{
    /// <summary>
    /// Slim Fluent scrollbar (14 DIP track): rounded thumb that thickens on hover,
    /// caret buttons that fade in, page/channel clicks, wheel and drag support.
    /// Set <see cref="Dynamic"/> for the auto-hiding variant that fades out a timeout
    /// after the last interaction, like the reference dynamic scrollbar.
    /// </summary>
    [ToolboxItem(true)]
    [DefaultEvent("Scroll")]
    public class FluentScrollBar : FluentControlBase
    {
        private Orientation _orientation = Orientation.Vertical;
        private int _minimum;
        private int _maximum = 100;
        private int _value;
        private int _smallChange = 1;
        private int _largeChange = 10;
        private bool _dynamic;
        private int _timeout = 1000;

        private bool _dragging;
        private int _dragStartValue;
        private int _dragStartPixel;
        private ScrollEventType _repeatKind;
        private Timer _fadeTimer;
        private Timer _repeatTimer;
        private Timer _hideTimer;
        private int _alpha = 255;

        public FluentScrollBar()
        {
            SetStyle(ControlStyles.Selectable, false);
            TabStop = false;
            BackColor = Color.Transparent;
            Size = new Size(14, 100);
        }

        [Category("Behavior")]
        [DefaultValue(Orientation.Vertical)]
        public Orientation Orientation
        {
            get { return _orientation; }
            set
            {
                if (_orientation != value)
                {
                    _orientation = value;
                    Size = new Size(Height, Width);
                    Invalidate();
                }
            }
        }

        [Category("Behavior")]
        [DefaultValue(false)]
        [Description("Auto-hide the bar: fully transparent until hovered or actively scrolling, fading back out after the timeout.")]
        public bool Dynamic
        {
            get { return _dynamic; }
            set
            {
                if (_dynamic != value)
                {
                    _dynamic = value;
                    _alpha = value ? 0 : 255;
                    Invalidate();
                }
            }
        }

        [Category("Behavior")]
        [DefaultValue(1000)]
        [Description("Milliseconds of inactivity before a dynamic bar fades out.")]
        public int Timeout
        {
            get { return _timeout; }
            set { _timeout = Math.Max(0, value); }
        }

        [Category("Data")]
        [DefaultValue(0)]
        public int Minimum
        {
            get { return _minimum; }
            set
            {
                _minimum = value;
                if (_maximum < _minimum)
                {
                    _maximum = _minimum;
                }

                ClampAndInvalidate();
            }
        }

        [Category("Data")]
        [DefaultValue(100)]
        public int Maximum
        {
            get { return _maximum; }
            set
            {
                _maximum = value;
                if (_minimum > _maximum)
                {
                    _minimum = _maximum;
                }

                ClampAndInvalidate();
            }
        }

        [Category("Data")]
        [RefreshProperties(RefreshProperties.Repaint)]
        [DefaultValue(0)]
        [TypeConverter(typeof(Int32Converter))]
        public int Value
        {
            get { return _value; }
            set
            {
                int clamped = Math.Max(_minimum, Math.Min(ValueMax, value));
                if (_value != clamped)
                {
                    _value = clamped;
                    ValueChanged?.Invoke(this, EventArgs.Empty);
                    Invalidate();
                }
            }
        }

        [Category("Behavior")]
        [DefaultValue(1)]
        public int SmallChange
        {
            get { return _smallChange; }
            set { _smallChange = Math.Max(1, value); }
        }

        [Category("Behavior")]
        [DefaultValue(10)]
        public int LargeChange
        {
            get { return _largeChange; }
            set
            {
                _largeChange = Math.Max(1, value);
                ClampAndInvalidate();
            }
        }

        public event EventHandler ValueChanged;
        public event ScrollEventHandler Scroll;

        protected virtual void OnScroll(ScrollEventArgs e)
        {
            Scroll?.Invoke(this, e);
        }

        private int ValueMax => Math.Max(_minimum, _maximum - _largeChange + 1);

        private void ClampAndInvalidate()
        {
            _value = Math.Max(_minimum, Math.Min(ValueMax, _value));
            Invalidate();
        }

        private struct Metrics
        {
            public RectangleF Channel;
            public RectangleF Thumb;
            public RectangleF ButtonStart;
            public RectangleF ButtonEnd;
        }

        private Metrics Measure()
        {
            bool vertical = _orientation == Orientation.Vertical;
            float button = Scale(12);
            float gap = button + 2;

            var m = new Metrics();
            if (vertical)
            {
                m.ButtonStart = new RectangleF(1, 1, Scale(12), button);
                m.ButtonEnd = new RectangleF(1, Height - 1 - button, Scale(12), button);
                m.Channel = new RectangleF(0, gap, Width, Math.Max(1, Height - gap * 2));

                float len = ThumbLength(m.Channel.Height);
                float usable = Math.Max(1f, m.Channel.Height - len);
                float pos = ValueMax == _minimum ? 0 : (float)(_value - _minimum) / (ValueMax - _minimum) * usable;
                m.Thumb = new RectangleF(0, m.Channel.Y + pos, Width, len);
            }
            else
            {
                m.ButtonStart = new RectangleF(1, 1, button, Scale(12));
                m.ButtonEnd = new RectangleF(Width - 1 - button, 1, Scale(12), Scale(12));
                m.Channel = new RectangleF(gap, 0, Math.Max(1, Width - gap * 2), Height);

                float len = ThumbLength(m.Channel.Width);
                float usable = Math.Max(1f, m.Channel.Width - len);
                float pos = ValueMax == _minimum ? 0 : (float)(_value - _minimum) / (ValueMax - _minimum) * usable;
                m.Thumb = new RectangleF(m.Channel.X + pos, 0, len, Height);
            }

            return m;
        }

        private float ThumbLength(float channelLength)
        {
            float ratio = (float)_largeChange / Math.Max(1, ValueMax - _minimum + _largeChange);
            return Math.Max(Scale(24), Math.Min(channelLength, ratio * channelLength));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            FluentGraphics.SetupGraphics(g);
            int alpha = _dynamic ? _alpha : 255;
            if (alpha == 0)
            {
                return;
            }

            Metrics m = Measure();
            bool vertical = _orientation == Orientation.Vertical;
            bool hot = IsHovered || _dragging;
            FluentPalette p = Palette;

            float pillThickness = Scale(12);
            float thumbThickness = Scale(hot ? 8 : 6);

            RectangleF pill = vertical
                ? new RectangleF((Width - pillThickness) / 2f, m.Channel.Y, pillThickness, m.Channel.Height)
                : new RectangleF(m.Channel.X, (Height - pillThickness) / 2f, m.Channel.Width, pillThickness);

            RectangleF thumbRect = vertical
                ? new RectangleF((Width - thumbThickness) / 2f, m.Thumb.Y, thumbThickness, m.Thumb.Height)
                : new RectangleF(m.Thumb.X, (Height - thumbThickness) / 2f, m.Thumb.Width, thumbThickness);

            float corner = Math.Min(thumbRect.Width, thumbRect.Height) / 2f;

            using (var brush = new SolidBrush(Tinted(ControlHelper.BlendOverParent(this, p.SubtleFillSecondary), alpha)))
            {
                FluentGraphics.FillRoundedRect(g, brush, pill, pillThickness / 2f);
            }

            Color fill = Tinted(ControlHelper.BlendOverParent(this, hot ? p.ControlFillTertiary : p.ControlFillSecondary), alpha);
            Color stroke = Tinted(p.ControlStrokeDefault, alpha);
            if (hot)
            {
                fill = Tinted(ControlHelper.BlendOverParent(this, p.ControlFillTertiary), alpha);
            }

            using (var brush = new SolidBrush(fill))
            {
                FluentGraphics.FillRoundedRect(g, brush, thumbRect, corner);
            }

            using (var pen = new Pen(stroke, 1f))
            {
                FluentGraphics.DrawRoundedRect(g, pen, RectangleF.Inflate(thumbRect, -0.5f, -0.5f), corner);
            }

            if (hot)
            {
                DrawCaretButton(g, m.ButtonStart, vertical, true, alpha);
                DrawCaretButton(g, m.ButtonEnd, vertical, false, alpha);
            }
        }

        private void DrawCaretButton(Graphics g, RectangleF bounds, bool vertical, bool towardsMin, int alpha)
        {
            float glyph = Scale(12);
            RectangleF box = vertical
                ? new RectangleF((Width - glyph) / 2f, bounds.Y, glyph, glyph)
                : new RectangleF(bounds.X, (Height - glyph) / 2f, glyph, glyph);
            int code = towardsMin
                ? (vertical ? 0xE70E : 0xE76B)
                : (vertical ? 0xE70D : 0xE76C);
            FluentGraphics.DrawGlyph(g, FluentGraphics.Glyph(code), FluentTypography.Icon(8f),
                Tinted(Palette.TextFillSecondary, alpha), box);
        }

        private static Color Tinted(Color color, int alpha)
        {
            return Color.FromArgb((int)((long)color.A * alpha / 255), color);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            Metrics m = Measure();
            PointF pt = e.Location;

            if (m.Thumb.Contains(pt))
            {
                _dragging = true;
                _dragStartValue = _value;
                _dragStartPixel = _orientation == Orientation.Vertical ? e.Y : e.X;
                OnScroll(new ScrollEventArgs(ScrollEventType.ThumbTrack, _value));
                TouchInteraction();
                Invalidate();
                return;
            }

            if (m.ButtonStart.Contains(pt))
            {
                Step(true);
                StartRepeat(true);
                return;
            }

            if (m.ButtonEnd.Contains(pt))
            {
                Step(false);
                StartRepeat(false);
                return;
            }

            PageTo(pt);
            TouchInteraction();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (!_dragging)
            {
                return;
            }

            Metrics m = Measure();
            bool vertical = _orientation == Orientation.Vertical;
            float channelLen = vertical ? m.Channel.Height : m.Channel.Width;
            float thumbLen = vertical ? m.Thumb.Height : m.Thumb.Width;
            float usable = Math.Max(1f, channelLen - thumbLen);
            int deltaPixels = (vertical ? e.Y : e.X) - _dragStartPixel;
            int newValue = _dragStartValue + (int)Math.Round(deltaPixels / usable * Math.Max(1, ValueMax - _minimum));
            if (newValue != _value)
            {
                Value = newValue;
                OnScroll(new ScrollEventArgs(ScrollEventType.ThumbTrack, _value));
            }

            TouchInteraction();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (_dragging)
            {
                _dragging = false;
                OnScroll(new ScrollEventArgs(ScrollEventType.EndScroll, _value));
                Invalidate();
            }

            StopRepeat();
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            int steps = Math.Max(1, Math.Abs(e.Delta) / 120) * _smallChange * 3;
            StepWithin(e.Delta > 0 ? _value - steps : _value + steps);
            OnScroll(new ScrollEventArgs(e.Delta > 0 ? ScrollEventType.SmallDecrement : ScrollEventType.SmallIncrement, _value));
            TouchInteraction();
        }

        private void Step(bool towardsMin)
        {
            StepWithin(towardsMin ? _value - _smallChange : _value + _smallChange);
            OnScroll(new ScrollEventArgs(towardsMin ? ScrollEventType.SmallDecrement : ScrollEventType.SmallIncrement, _value));
            TouchInteraction();
        }

        private void StepWithin(int target)
        {
            Value = target;
        }

        private void PageTo(PointF pt)
        {
            Metrics m = Measure();
            bool forward = _orientation == Orientation.Vertical ? pt.Y > m.Thumb.Bottom : pt.X > m.Thumb.Right;
            bool backward = _orientation == Orientation.Vertical ? pt.Y < m.Thumb.Top : pt.X < m.Thumb.Left;
            Value = _value + (forward ? _largeChange : backward ? -_largeChange : 0);
            OnScroll(new ScrollEventArgs(forward ? ScrollEventType.LargeIncrement : backward ? ScrollEventType.LargeDecrement : ScrollEventType.ThumbPosition, _value));
        }

        private void StartRepeat(bool towardsMin)
        {
            _repeatKind = towardsMin ? ScrollEventType.SmallDecrement : ScrollEventType.SmallIncrement;
            if (_repeatTimer == null)
            {
                _repeatTimer = new Timer { Interval = 100 };
                _repeatTimer.Tick += (s, e) =>
                {
                    int delta = _repeatKind == ScrollEventType.SmallDecrement ? -_smallChange : _smallChange;
                    StepWithin(_value + delta);
                    OnScroll(new ScrollEventArgs(_repeatKind, _value));
                    TouchInteraction();
                };
            }

            _repeatTimer.Start();
        }

        private void StopRepeat()
        {
            _repeatTimer?.Stop();
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            TouchInteraction();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            StopRepeat();
            if (_dynamic)
            {
                EnsureFadeTimer();
                _fadeTimer.Start();
            }
        }

        private void TouchInteraction()
        {
            if (!_dynamic)
            {
                return;
            }

            EnsureFadeTimer();
            EnsureHideTimer();
            _hideTimer.Stop();
            _hideTimer.Interval = Math.Max(50, _timeout);
            _hideTimer.Start();
            if (_alpha < 255)
            {
                _fadeTimer.Start();
            }
        }

        private void EnsureFadeTimer()
        {
            if (_fadeTimer != null)
            {
                return;
            }

            _fadeTimer = new Timer { Interval = 16 };
            _fadeTimer.Tick += (s, e) =>
            {
                bool show = IsHovered || _dragging || (_hideTimer != null && _hideTimer.Enabled);
                int target = show ? 255 : 0;
                const int step = 26;
                if (_alpha < target)
                {
                    _alpha = Math.Min(target, _alpha + step);
                }
                else if (_alpha > target)
                {
                    _alpha = Math.Max(target, _alpha - step);
                }

                Invalidate();
                if (_alpha == target)
                {
                    _fadeTimer.Stop();
                }
            };
        }

        private void EnsureHideTimer()
        {
            if (_hideTimer != null)
            {
                return;
            }

            _hideTimer = new Timer();
            _hideTimer.Tick += (s, e) =>
            {
                _hideTimer.Stop();
                EnsureFadeTimer();
                _fadeTimer.Start();
            };
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _fadeTimer?.Dispose();
                _repeatTimer?.Dispose();
                _hideTimer?.Dispose();
                _fadeTimer = null;
                _repeatTimer = null;
                _hideTimer = null;
            }

            base.Dispose(disposing);
        }
    }
}
