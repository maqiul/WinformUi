using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using WinformUi.Drawing;
using WinformUi.Theming;

namespace WinformUi.Controls
{
    /// <summary>
    /// Fluent scroll viewer:
    /// hosts one content control and draws a slim scrollbar that widens on hover.
    /// </summary>
    public class FluentScrollViewer : Panel
    {
        private Control _content;
        private ThemeSubscription _themeSubscription;
        private int _offsetY;
        private bool _scrollHovered;
        private bool _thumbDragging;
        private int _dragStartY;
        private int _dragStartOffset;

        protected override void OnPaintBackground(PaintEventArgs e) => ControlHelper.PaintFullBackground(this, e.Graphics);

        public FluentScrollViewer()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            // Opaque surface: a transparent backcolor makes WinForms simulate the
            // parent's paint (including sibling controls) into our buffer, which
            // leaves stale sibling pixels behind.
            BackColor = ControlHelper.GetEffectiveBackground(this);
            AutoScroll = false;
        }

        /// <summary>The scrolled content. Set its Height to the desired extent.</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Control Content
        {
            get { return _content; }
            set
            {
                if (_content != null)
                {
                    Controls.Remove(_content);
                }

                _content = value;
                if (value != null)
                {
                    Controls.Add(value);
                }

                _offsetY = 0;
                LayoutContent();
                Invalidate();
            }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int ScrollOffset
        {
            get { return _offsetY; }
            set
            {
                int max = MaxOffset;
                int clamped = Math.Max(0, Math.Min(max, value));
                if (_offsetY != clamped)
                {
                    _offsetY = clamped;
                    LayoutContent();
                    Invalidate();
                }
            }
        }

        private int MaxOffset => _content == null ? 0 : Math.Max(0, _content.Height - Height);

        private bool NeedsScroll => MaxOffset > 0;

        private float ThumbWidth => _scrollHovered || _thumbDragging ? 10f : 6f;

        private RectangleF TrackRect => new RectangleF(Width - ThumbWidth - 3, 2, ThumbWidth, Height - 4);

        private RectangleF ThumbRect
        {
            get
            {
                if (!NeedsScroll)
                {
                    return RectangleF.Empty;
                }

                RectangleF track = TrackRect;
                float ratio = Height / (float)_content.Height;
                float thumbHeight = Math.Max(24f, track.Height * ratio);
                float travel = track.Height - thumbHeight;
                float y = track.Y + (MaxOffset == 0 ? 0 : travel * _offsetY / MaxOffset);
                return new RectangleF(track.X, y, track.Width, thumbHeight);
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (_themeSubscription == null)
            {
                _themeSubscription = new ThemeSubscription(this, () =>
                {
                    BackColor = ControlHelper.GetEffectiveBackground(this);
                    Invalidate();
                });
            }
        }

        protected override void OnParentChanged(EventArgs e)
        {
            base.OnParentChanged(e);
            BackColor = ControlHelper.GetEffectiveBackground(this);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            LayoutContent();
            Invalidate();
        }

        private void LayoutContent()
        {
            if (_content != null)
            {
                _content.Bounds = new Rectangle(0, -_offsetY, Width - (NeedsScroll ? 14 : 0), _content.Height);
                _content.Invalidate(true);
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            ScrollOffset = _offsetY - e.Delta / 2;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (!NeedsScroll || e.Button != MouseButtons.Left)
            {
                return;
            }

            if (ThumbRect.Contains(e.Location))
            {
                _thumbDragging = true;
                _dragStartY = e.Y;
                _dragStartOffset = _offsetY;
            }
            else if (TrackRect.Contains(e.Location))
            {
                ScrollOffset = _offsetY + (e.Y < ThumbRect.Y ? -Height : Height);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            bool hover = NeedsScroll && e.X >= Width - 16;
            if (_scrollHovered != hover)
            {
                _scrollHovered = hover;
                Invalidate();
            }

            if (_thumbDragging)
            {
                RectangleF track = TrackRect;
                float thumbHeight = ThumbRect.Height;
                float travel = track.Height - thumbHeight;
                if (travel > 0)
                {
                    int delta = e.Y - _dragStartY;
                    ScrollOffset = _dragStartOffset + (int)(delta / travel * MaxOffset);
                }
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _thumbDragging = false;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _scrollHovered = false;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            ControlHelper.PaintFullBackground(this, e.Graphics);
            if (!NeedsScroll)
            {
                return;
            }

            Graphics g = e.Graphics;
            FluentGraphics.SetupGraphics(g);
            FluentPalette p = ApplicationThemeManager.Palette;

            if (_scrollHovered || _thumbDragging)
            {
                Color trackFill = AccentColor.Blend(p.SubtleFillTertiary, ControlHelper.GetEffectiveBackground(this));
                using (var brush = new SolidBrush(trackFill))
                {
                    FluentGraphics.FillRoundedRect(g, brush, TrackRect, TrackRect.Width / 2f);
                }
            }

            RectangleF thumb = ThumbRect;
            Color thumbFill = ControlHelper.BlendOverParent(this, p.ControlStrongFillDefault);
            using (var brush = new SolidBrush(thumbFill))
            {
                FluentGraphics.FillRoundedRect(g, brush, thumb, thumb.Width / 2f);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _themeSubscription?.Dispose();
                _themeSubscription = null;
            }

            base.Dispose(disposing);
        }
    }
}