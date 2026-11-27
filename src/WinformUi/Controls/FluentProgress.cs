using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using WinformUi.Drawing;
using WinformUi.Theming;

namespace WinformUi.Controls
{
    /// <summary>Fluent progress bar: 4px rounded track, determinate or indeterminate.</summary>
    [System.ComponentModel.ToolboxItem(true)]
    public class FluentProgressBar : FluentControlBase
    {
        private double _minimum;
        private double _maximum = 100;
        private double _value;
        private bool _isIndeterminate;
        private readonly Timer _timer;
        private float _indeterminatePhase;

        public FluentProgressBar()
        {
            Size = new Size(200, 8);
            TabStop = false;
            _timer = new Timer { Interval = 16 };
            _timer.Tick += (s, e) =>
            {
                _indeterminatePhase += 0.02f;
                if (_indeterminatePhase > 1.4f)
                {
                    _indeterminatePhase = -0.4f;
                }

                Invalidate();
            };
        }

        [DefaultValue(0d)]
        [Category("Fluent")]
        public double Minimum
        {
            get { return _minimum; }
            set { _minimum = value; Invalidate(); }
        }

        [DefaultValue(100d)]
        [Category("Fluent")]
        public double Maximum
        {
            get { return _maximum; }
            set { _maximum = value; Invalidate(); }
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
                    Invalidate();
                }
            }
        }

        [DefaultValue(false)]
        [Category("Fluent")]
        public bool IsIndeterminate
        {
            get { return _isIndeterminate; }
            set
            {
                _isIndeterminate = value;
                if (value)
                {
                    _timer.Start();
                }
                else
                {
                    _timer.Stop();
                }

                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            ControlHelper.PaintFullBackground(this, e.Graphics);
            Graphics g = e.Graphics;
            FluentGraphics.SetupGraphics(g);

            float trackHeight = 4f;
            float y = (Height - trackHeight) / 2f;
            var track = new RectangleF(0, y, Width, trackHeight);
            float radius = trackHeight / 2f;

            Color trackColor = ControlHelper.BlendOverParent(this, Palette.ControlStrongStrokeDefault);
            using (var brush = new SolidBrush(trackColor))
            {
                FluentGraphics.FillRoundedRect(g, brush, track, radius);
            }

            if (!Enabled)
            {
                return;
            }

            using (var brush = new SolidBrush(Palette.SystemAccentColorPrimary))
            {
                if (_isIndeterminate)
                {
                    float barWidth = Width * 0.35f;
                    float x = _indeterminatePhase * (Width + barWidth) - barWidth;
                    var clip = new RectangleF(Math.Max(0, x), y, Math.Min(barWidth, Width - Math.Max(0, x)), trackHeight);
                    if (clip.Width > 0)
                    {
                        using (var path = FluentGraphics.RoundedRect(track, radius))
                        {
                            g.SetClip(path);
                            FluentGraphics.FillRoundedRect(g, brush, new RectangleF(x, y, barWidth, trackHeight), radius);
                            g.ResetClip();
                        }
                    }
                }
                else
                {
                    double range = _maximum - _minimum;
                    float fraction = range <= 0 ? 0 : (float)((_value - _minimum) / range);
                    float width = Width * fraction;
                    if (width > 0.5f)
                    {
                        FluentGraphics.FillRoundedRect(g, brush, new RectangleF(0, y, width, trackHeight), radius);
                    }
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _timer.Stop();
                _timer.Dispose();
            }

            base.Dispose(disposing);
        }
    }

    /// <summary>Fluent progress ring: rotating accent arc.</summary>
    [System.ComponentModel.ToolboxItem(true)]
    public class FluentProgressRing : FluentControlBase
    {
        private bool _isActive = true;
        private readonly Timer _timer;
        private float _angle;

        public FluentProgressRing()
        {
            Size = new Size(32, 32);
            TabStop = false;
            _timer = new Timer { Interval = 16 };
            _timer.Tick += (s, e) =>
            {
                _angle = (_angle + 6f) % 360f;
                Invalidate();
            };
        }

        [DefaultValue(true)]
        [Category("Fluent")]
        public bool IsActive
        {
            get { return _isActive; }
            set
            {
                _isActive = value;
                if (value)
                {
                    _timer.Start();
                }
                else
                {
                    _timer.Stop();
                }

                Invalidate();
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (_isActive)
            {
                _timer.Start();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            ControlHelper.PaintFullBackground(this, e.Graphics);
            if (!_isActive)
            {
                return;
            }

            Graphics g = e.Graphics;
            FluentGraphics.SetupGraphics(g);

            float thickness = Math.Max(2f, Math.Min(Width, Height) / 10f);
            var rect = new RectangleF(thickness / 2f + 1, thickness / 2f + 1, Width - thickness - 2, Height - thickness - 2);

            using (var pen = new Pen(Palette.SystemAccentColorPrimary, thickness))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                g.DrawArc(pen, rect, _angle, 270f);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _timer.Stop();
                _timer.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}