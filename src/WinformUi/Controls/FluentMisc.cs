using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using WinformUi.Drawing;
using WinformUi.Theming;

namespace WinformUi.Controls
{
    /// <summary>Fluent rating control: interactive star rating.</summary>
    [System.ComponentModel.ToolboxItem(true)]
    public class FluentRatingControl : FluentControlBase
    {
        private int _value;
        private int _maximum = 5;
        private int _hoverValue;
        private bool _readOnly;

        public event EventHandler ValueChanged;

        public FluentRatingControl()
        {
            Size = new Size(140, 28);
            Cursor = Cursors.Hand;
        }

        [DefaultValue(0)]
        [Category("Fluent")]
        public int Value
        {
            get { return _value; }
            set
            {
                int clamped = Math.Max(0, Math.Min(_maximum, value));
                if (_value != clamped)
                {
                    _value = clamped;
                    OnValueChanged(EventArgs.Empty);
                    Invalidate();
                }
            }
        }

        [DefaultValue(5)]
        [Category("Fluent")]
        public int Maximum
        {
            get { return _maximum; }
            set { _maximum = Math.Max(1, value); Invalidate(); }
        }

        [DefaultValue(false)]
        [Category("Fluent")]
        public bool ReadOnly
        {
            get { return _readOnly; }
            set { _readOnly = value; Cursor = value ? Cursors.Default : Cursors.Hand; }
        }

        protected virtual void OnValueChanged(EventArgs e)
        {
            ValueChanged?.Invoke(this, e);
        }

        private float StarSize => Math.Min(Height - 4, (Width - 4) / (float)_maximum);

        private int StarAt(Point point)
        {
            int index = (int)(point.X / StarSize);
            return index >= 0 && index < _maximum ? index + 1 : 0;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (!_readOnly)
            {
                int hover = StarAt(e.Location);
                if (_hoverValue != hover)
                {
                    _hoverValue = hover;
                    Invalidate();
                }
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hoverValue = 0;
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (!_readOnly && e.Button == MouseButtons.Left)
            {
                Value = StarAt(e.Location);
            }
        }

        internal static GraphicsPath StarPath(RectangleF rect)
        {
            var path = new GraphicsPath();
            float cx = rect.X + rect.Width / 2f;
            float cy = rect.Y + rect.Height / 2f;
            float outer = Math.Min(rect.Width, rect.Height) / 2f;
            float inner = outer * 0.45f;

            var points = new PointF[10];
            for (int i = 0; i < 10; i++)
            {
                float r = i % 2 == 0 ? outer : inner;
                double angle = -Math.PI / 2 + i * Math.PI / 5;
                points[i] = new PointF(cx + (float)Math.Cos(angle) * r, cy + (float)Math.Sin(angle) * r);
            }

            path.AddPolygon(points);
            return path;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            ControlHelper.PaintFullBackground(this, e.Graphics);
            Graphics g = e.Graphics;
            FluentGraphics.SetupGraphics(g);

            int displayValue = _hoverValue > 0 ? _hoverValue : _value;
            float size = StarSize;

            for (int i = 0; i < _maximum; i++)
            {
                var rect = new RectangleF(i * size + 2, (Height - size) / 2f, size - 4, size - 4);
                using (var path = StarPath(rect))
                {
                    if (i < displayValue)
                    {
                        using (var brush = new SolidBrush(Palette.SystemAccentColorPrimary))
                        {
                            g.FillPath(brush, path);
                        }
                    }
                    else
                    {
                        using (var pen = new Pen(Palette.ControlStrongStrokeDefault, 1.4f))
                        {
                            g.DrawPath(pen, path);
                        }
                    }
                }
            }
        }
    }

    /// <summary>Fluent thumb rating: like / dislike toggle pair.</summary>
    [System.ComponentModel.ToolboxItem(true)]
    public class FluentThumbRate : FluentControlBase
    {
        private int _value; // 1 like, -1 dislike, 0 none
        private int _hover; // 1 like, 2 dislike

        public event EventHandler ValueChanged;

        public FluentThumbRate()
        {
            Size = new Size(72, 32);
            Cursor = Cursors.Hand;
        }

        /// <summary>1 = liked, -1 = disliked, 0 = none.</summary>
        [DefaultValue(0)]
        [Category("Fluent")]
        public int Value
        {
            get { return _value; }
            set
            {
                int clamped = Math.Max(-1, Math.Min(1, value));
                if (_value != clamped)
                {
                    _value = clamped;
                    OnValueChanged(EventArgs.Empty);
                    Invalidate();
                }
            }
        }

        protected virtual void OnValueChanged(EventArgs e)
        {
            ValueChanged?.Invoke(this, e);
        }

        private RectangleF LikeRect => new RectangleF(0, (Height - 28) / 2f, 32, 28);
        private RectangleF DislikeRect => new RectangleF(Width - 32, (Height - 28) / 2f, 32, 28);

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int hover = LikeRect.Contains(e.Location) ? 1 : (DislikeRect.Contains(e.Location) ? 2 : 0);
            if (_hover != hover)
            {
                _hover = hover;
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hover = 0;
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button != MouseButtons.Left) return;
            if (LikeRect.Contains(e.Location)) Value = _value == 1 ? 0 : 1;
            else if (DislikeRect.Contains(e.Location)) Value = _value == -1 ? 0 : -1;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            ControlHelper.PaintFullBackground(this, e.Graphics);
            Graphics g = e.Graphics;
            FluentGraphics.SetupGraphics(g);

            DrawThumb(g, LikeRect, 0xF820, _value == 1, _hover == 1);
            DrawThumb(g, DislikeRect, 0xF81E, _value == -1, _hover == 2);
        }

        private void DrawThumb(Graphics g, RectangleF rect, int glyph, bool active, bool hovered)
        {
            if (hovered || active)
            {
                using (var brush = new SolidBrush(ControlHelper.BlendOverParent(this, Palette.SubtleFillSecondary)))
                {
                    FluentGraphics.FillRoundedRect(g, brush, rect, FluentGraphics.ControlCornerRadius);
                }
            }

            Color color = active ? Palette.SystemAccentColorPrimary : Palette.TextFillSecondary;
            FluentGraphics.DrawGlyph(g, FluentGraphics.Glyph(glyph), FluentTypography.IconRegular(12f), color, rect);
        }
    }

    /// <summary>Fluent arc: partial ring segment.</summary>
    [System.ComponentModel.ToolboxItem(true)]
    public class FluentArc : FluentControlBase
    {
        private float _startAngle;
        private float _endAngle = 270f;
        private float _thickness = 4f;
        private Color _arcColor = Color.Empty;

        public FluentArc()
        {
            Size = new Size(48, 48);
            TabStop = false;
        }

        [DefaultValue(0f)]
        [Category("Fluent")]
        public float StartAngle
        {
            get { return _startAngle; }
            set { _startAngle = value; Invalidate(); }
        }

        [DefaultValue(270f)]
        [Category("Fluent")]
        public float EndAngle
        {
            get { return _endAngle; }
            set { _endAngle = value; Invalidate(); }
        }

        [DefaultValue(4f)]
        [Category("Fluent")]
        public float Thickness
        {
            get { return _thickness; }
            set { _thickness = value; Invalidate(); }
        }

        [Browsable(false)]
        [Category("Fluent")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color ArcColor
        {
            get { return _arcColor; }
            set { _arcColor = value; Invalidate(); }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            ControlHelper.PaintFullBackground(this, e.Graphics);
            Graphics g = e.Graphics;
            FluentGraphics.SetupGraphics(g);

            float sweep = _endAngle - _startAngle;
            if (Math.Abs(sweep) < 0.01f)
            {
                return;
            }

            Color color = _arcColor.IsEmpty ? Palette.SystemAccentColorPrimary : _arcColor;
            var rect = new RectangleF(_thickness / 2f, _thickness / 2f, Width - _thickness, Height - _thickness);
            using (var pen = new Pen(color, _thickness))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                g.DrawArc(pen, rect, _startAngle, sweep);
            }
        }
    }

    /// <summary>
    /// Fluent anchor: positions its single child at a relative
    /// position (0..1) of the available space.
    /// </summary>
    public class FluentAnchor : Panel
    {
        private PointF _relativePosition = new PointF(0.5f, 0.5f);

        public FluentAnchor()
        {
            BackColor = Color.Transparent;
        }

        [Browsable(false)]
        [Category("Fluent")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public PointF RelativePosition
        {
            get { return _relativePosition; }
            set
            {
                _relativePosition = value;
                PerformLayout();
            }
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);
            if (Controls.Count > 0)
            {
                Control child = Controls[0];
                int x = (int)((Width - child.Width) * _relativePosition.X);
                int y = (int)((Height - child.Height) * _relativePosition.Y);
                child.Location = new Point(Math.Max(0, x), Math.Max(0, y));
            }
        }
    }

    /// <summary>
    /// Fluent split button: primary action plus a chevron
    /// section opening a flyout menu.
    /// </summary>
    public class FluentSplitButton : FluentButton
    {
        private bool _chevronClicked;
        private ContextMenuStrip _flyout;

        public FluentSplitButton()
        {
            Padding = new Padding(12, 6, 40, 6);
        }

        [Browsable(false)]
        [Category("Fluent")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ContextMenuStrip Flyout
        {
            get { return _flyout; }
            set { _flyout = value; }
        }

        protected override void OnMouseDown(MouseEventArgs mevent)
        {
            _chevronClicked = mevent.X >= Width - 32;
            base.OnMouseDown(mevent);
            if (_chevronClicked && _flyout != null)
            {
                _flyout.Show(this, new Point(0, Height));
            }
        }

        protected override void OnClick(EventArgs e)
        {
            if (_chevronClicked)
            {
                _chevronClicked = false;
                return;
            }

            base.OnClick(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            ControlHelper.PaintFullBackground(this, e.Graphics);
            base.OnPaint(e);
            using (var pen = new Pen(Palette.ControlStrokeDefault, 1f))
            {
                e.Graphics.DrawLine(pen, Width - 32, 6, Width - 32, Height - 6);
            }

            FluentGraphics.DrawGlyph(e.Graphics, FluentGraphics.Glyph(0xE70D), FluentTypography.Icon(8f), Palette.TextFillSecondary, new RectangleF(Width - 28, (Height - 14) / 2f, 14, 14));
        }
    }

    /// <summary>Fluent drop-down button: opens a flyout on click.</summary>
    public class FluentDropDownButton : FluentButton
    {
        private ContextMenuStrip _flyout;

        public FluentDropDownButton()
        {
            Padding = new Padding(12, 6, 36, 6);
        }

        [Browsable(false)]
        [Category("Fluent")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ContextMenuStrip Flyout
        {
            get { return _flyout; }
            set { _flyout = value; }
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            _flyout?.Show(this, new Point(0, Height));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            ControlHelper.PaintFullBackground(this, e.Graphics);
            base.OnPaint(e);
            FluentGraphics.DrawGlyph(e.Graphics, FluentGraphics.Glyph(0xE70D), FluentTypography.Icon(8f), Palette.TextFillSecondary, new RectangleF(Width - 26, (Height - 14) / 2f, 14, 14));
        }
    }
}