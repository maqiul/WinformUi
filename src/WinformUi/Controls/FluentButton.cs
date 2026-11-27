using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using WinformUi.Drawing;
using WinformUi.Theming;

namespace WinformUi.Controls
{
    /// <summary>
    /// Fluent button. Owner-drawn with hover/pressed states,
    /// optional Fluent icon and keyboard focus visual.
    /// </summary>
    public class FluentButton : Button
    {
        private ThemeSubscription _themeSubscription;
        private ControlAppearance _appearance = ControlAppearance.Secondary;
        private SymbolRegular _icon = SymbolRegular.Empty;
        private float _cornerRadius = FluentGraphics.ControlCornerRadius;
        private bool _hovered;
        private bool _pressed;

        public FluentButton()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Font = FluentTypography.Body;
            Padding = new Padding(12, 6, 12, 6);
            AutoSize = true;
            TabStop = true;
        }

        [DefaultValue(ControlAppearance.Secondary)]
        [Category("Fluent")]
        public ControlAppearance Appearance
        {
            get { return _appearance; }
            set
            {
                _appearance = value;
                Invalidate();
            }
        }

        [DefaultValue(SymbolRegular.Empty)]
        [Category("Fluent")]
        public SymbolRegular Icon
        {
            get { return _icon; }
            set
            {
                _icon = value;
                Invalidate();
            }
        }

        [DefaultValue(FluentGraphics.ControlCornerRadius)]
        [Category("Fluent")]
        public float CornerRadius
        {
            get { return _cornerRadius; }
            set
            {
                _cornerRadius = value;
                Invalidate();
            }
        }

        protected FluentPalette Palette => ApplicationThemeManager.Palette;

        public override Size GetPreferredSize(Size proposedSize)
        {
            Size text = TextRenderer.MeasureText(Text ?? string.Empty, Font);
            int width = text.Width + Padding.Horizontal;
            if (_icon != SymbolRegular.Empty)
            {
                width += (int)(Font.Size * 1.15f) + 8;
            }

            return new Size(width, Math.Max(32, text.Height + Padding.Vertical));
        }

        /// <summary>Checked visual for toggle-style derivatives.</summary>
        protected virtual bool IsCheckedVisual => false;

        protected virtual Color GetForegroundColor(FluentVisualState state)
        {
            return ControlHelper.GetForeground(_appearance, state, IsCheckedVisual);
        }

        private FluentVisualState State
        {
            get
            {
                if (!Enabled) return FluentVisualState.Disabled;
                if (_pressed) return FluentVisualState.Pressed;
                if (_hovered) return FluentVisualState.Hover;
                return FluentVisualState.Normal;
            }
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
            _pressed = false;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs mevent)
        {
            base.OnMouseDown(mevent);
            if (mevent.Button == MouseButtons.Left)
            {
                _pressed = true;
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs mevent)
        {
            base.OnMouseUp(mevent);
            _pressed = false;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            ControlHelper.PaintFullBackground(this, e.Graphics);
            Graphics g = e.Graphics;
            FluentGraphics.SetupGraphics(g);

            FluentVisualState state = State;
            Color fill = ControlHelper.GetFill(this, _appearance, state, IsCheckedVisual);
            Color fore = GetForegroundColor(state);

            var bounds = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);

            if (fill.A > 0)
            {
                using (var brush = new SolidBrush(fill))
                {
                    FluentGraphics.FillRoundedRect(g, brush, bounds, _cornerRadius);
                }
            }

            bool transparent = _appearance == ControlAppearance.Transparent;
            if (!transparent && state != FluentVisualState.Pressed)
            {
                bool onAccent = _appearance == ControlAppearance.Primary || IsCheckedVisual;
                FluentGraphics.DrawElevationBorder(g, bounds, _cornerRadius, Palette, onAccent);
            }
            else if (!transparent)
            {
                using (var pen = new Pen(Palette.ControlStrokeDefault, 1f))
                {
                    FluentGraphics.DrawRoundedRect(g, pen, bounds, _cornerRadius);
                }
            }

            // Content: icon + text centered as a group; trailing reservations
            // (e.g. drop-down chevrons) keep the group clear of them.
            string text = Text ?? string.Empty;
            bool hasIcon = _icon != SymbolRegular.Empty;
            float iconSize = Font.Size * 1.15f;
            SizeF textSize = TextRenderer.MeasureText(text, Font);
            float gap = hasIcon && text.Length > 0 ? 8f : 0f;
            float totalWidth = (hasIcon ? iconSize : 0) + gap + textSize.Width;
            float x = (Width - Math.Max(0, Padding.Right - 12) - totalWidth) / 2f;
            float centerY = Height / 2f;

            if (hasIcon)
            {
                var iconRect = new RectangleF(x, centerY - iconSize / 2f, iconSize, iconSize);
                FluentGraphics.DrawGlyph(g, FluentGraphics.Glyph((int)_icon), FluentTypography.IconRegular(iconSize * 0.75f), fore, iconRect);
                x += iconSize + gap;
            }

            if (text.Length > 0)
            {
                using (var format = new StringFormat { LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap })
                {
                    var textRect = new RectangleF(x, 0, textSize.Width + 2, Height);
                    FluentGraphics.DrawText(g, text, Font, fore, textRect, format);
                }
            }

            if (Focused && ShowFocusCues)
            {
                FluentGraphics.DrawFocusVisual(g, new RectangleF(1.5f, 1.5f, Width - 3f, Height - 3f), _cornerRadius, Palette);
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