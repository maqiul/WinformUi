using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using WinformUi.Drawing;
using WinformUi.Theming;

namespace WinformUi.Controls
{
    /// <summary>
    /// Fluent card: rounded layer surface with a subtle stroke.
    /// Host child controls like a Panel.
    /// </summary>
    public class FluentCard : Panel
    {
        private ThemeSubscription _themeSubscription;
        private float _cornerRadius = FluentGraphics.ControlCornerRadius;

        protected override void OnPaintBackground(PaintEventArgs e) => ControlHelper.PaintFullBackground(this, e.Graphics);

        public FluentCard()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor, true);
            Padding = new Padding(12);
            BackColor = Color.Transparent;
            Font = FluentTypography.Body;
        }

        [DefaultValue(FluentGraphics.ControlCornerRadius)]
        [Category("Fluent")]
        public float CornerRadius
        {
            get { return _cornerRadius; }
            set { _cornerRadius = value; Invalidate(); }
        }

        protected FluentPalette Palette => ApplicationThemeManager.Palette;

        protected virtual Color CardFill => ControlHelper.BlendOverParent(this, Palette.CardBackgroundFillDefault);

        protected virtual Color CardStroke => Palette.CardStrokeDefault;

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (_themeSubscription == null)
            {
                _themeSubscription = new ThemeSubscription(this, () => Invalidate());
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            // Keep the ambient background equal to the painted surface so child
            // labels resolve the same color instead of the page behind the card.
            Color fill = CardFill;
            if (BackColor != fill)
            {
                BackColor = fill;
            }

            ControlHelper.PaintFullBackground(this, e.Graphics);
            Graphics g = e.Graphics;
            FluentGraphics.SetupGraphics(g);

            var bounds = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
            using (var brush = new SolidBrush(fill))
            {
                FluentGraphics.FillRoundedRect(g, brush, bounds, _cornerRadius);
            }

            using (var pen = new Pen(CardStroke, 1f))
            {
                FluentGraphics.DrawRoundedRect(g, pen, bounds, _cornerRadius);
            }

            OnPaintCard(e);
        }

        /// <summary>Hook for derivatives to draw extra decoration.</summary>
        protected virtual void OnPaintCard(PaintEventArgs e)
        {
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

    /// <summary>Fluent clickable card: hover/pressed layer states.</summary>
    public class FluentCardAction : FluentCard
    {
        private bool _hovered;
        private bool _pressed;

        public FluentCardAction()
        {
            Cursor = Cursors.Hand;
        }

        protected override Color CardFill
        {
            get
            {
                if (!Enabled) return ControlHelper.BlendOverParent(this, Palette.ControlFillDisabled);
                if (_pressed) return ControlHelper.BlendOverParent(this, Palette.ControlFillTertiary);
                if (_hovered) return ControlHelper.BlendOverParent(this, Palette.ControlFillSecondary);
                return base.CardFill;
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

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
            {
                _pressed = true;
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _pressed = false;
            Invalidate();
        }
    }

    /// <summary>Fluent card with a colored accent strip.</summary>
    public class FluentCardColor : FluentCard
    {
        private Color _accentColor = Color.Empty;

        [Browsable(false)]
        [Category("Fluent")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color AccentColor
        {
            get { return _accentColor; }
            set { _accentColor = value; Invalidate(); }
        }

        protected override void OnPaintCard(PaintEventArgs e)
        {
            Color strip = _accentColor.IsEmpty ? Palette.SystemAccentColorPrimary : _accentColor;
            using (var brush = new SolidBrush(strip))
            {
                FluentGraphics.FillRoundedRect(e.Graphics, brush, new RectangleF(1, 1, 4, Height - 2), 2f);
            }
        }
    }

    /// <summary>
    /// Fluent expander: clickable header row with
    /// chevron, collapsible content panel. Add children to <see cref="ContentPanel"/>.
    /// </summary>
    public class FluentExpander : Control
    {
        private ThemeSubscription _themeSubscription;
        private string _header = "Header";
        private SymbolRegular _headerIcon = SymbolRegular.Empty;
        private bool _isExpanded;
        private bool _headerHovered;
        private readonly Panel _contentPanel;

        public event EventHandler ExpandedChanged;

        protected override void OnPaintBackground(PaintEventArgs e) => ControlHelper.PaintFullBackground(this, e.Graphics);

        public FluentExpander()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            Font = FluentTypography.Body;
            Size = new Size(Scale(300), Scale(48));

            _contentPanel = new Panel
            {
                BackColor = Color.Transparent,
                Visible = false,
                Padding = new Padding(Scale(12), Scale(8), Scale(12), Scale(12))
            };
            Controls.Add(_contentPanel);
        }

        /// <summary>Host for the expandable content.</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Panel ContentPanel => _contentPanel;

        [DefaultValue("Header")]
        [Category("Fluent")]
        public string Header
        {
            get { return _header; }
            set { _header = value; Invalidate(); }
        }

        [DefaultValue(SymbolRegular.Empty)]
        [Category("Fluent")]
        public SymbolRegular HeaderIcon
        {
            get { return _headerIcon; }
            set { _headerIcon = value; Invalidate(); }
        }

        [DefaultValue(false)]
        [Category("Fluent")]
        public bool IsExpanded
        {
            get { return _isExpanded; }
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    _contentPanel.Visible = value;
                    UpdateLayout();
                    OnExpandedChanged(EventArgs.Empty);
                    Invalidate();
                }
            }
        }

        protected FluentPalette Palette => ApplicationThemeManager.Palette;

        private int Scale(int value)
        {
            return (int)Math.Round(value * (ControlHelper.DpiOf(this) / 96f));
        }

        protected virtual void OnExpandedChanged(EventArgs e)
        {
            ExpandedChanged?.Invoke(this, e);
        }

        private int HeaderHeight => Scale(48);

        private void UpdateLayout()
        {
            _contentPanel.Bounds = new Rectangle(1, HeaderHeight, Width - 2, Math.Max(0, Height - HeaderHeight - 1));
            if (_isExpanded)
            {
                int contentHeight = _contentPanel.Padding.Vertical;
                foreach (Control child in _contentPanel.Controls)
                {
                    int bottom = child.Bottom;
                    if (child.AutoSize)
                    {
                        Size preferred = child.GetPreferredSize(Size.Empty);
                        if (preferred.Height > child.Height)
                        {
                            bottom = child.Top + preferred.Height;
                        }
                    }

                    contentHeight = Math.Max(contentHeight, bottom + _contentPanel.Padding.Bottom);
                }

                Height = HeaderHeight + contentHeight + 1;
                _contentPanel.Bounds = new Rectangle(1, HeaderHeight, Width - 2, contentHeight);
            }
            else if (Height < HeaderHeight)
            {
                // Callers often hard-code the 96dpi header height; DPI scaling
                // grows it, so never leave the header clipped.
                Height = HeaderHeight;
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (_isExpanded)
            {
                _contentPanel.Bounds = new Rectangle(1, HeaderHeight, Width - 2, Math.Max(0, Height - HeaderHeight - 1));
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (_themeSubscription == null)
            {
                _themeSubscription = new ThemeSubscription(this, () => Invalidate());
            }

            UpdateLayout();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            bool hover = e.Y < HeaderHeight;
            if (_headerHovered != hover)
            {
                _headerHovered = hover;
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _headerHovered = false;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left && e.Y < HeaderHeight && Enabled)
            {
                IsExpanded = !IsExpanded;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            float radius = FluentGraphics.ControlCornerRadius;
            Color headerFill = ControlHelper.BlendOverParent(this,
                _headerHovered && Enabled ? Palette.ControlFillSecondary : Palette.CardBackgroundFillDefault);
            Color contentFill = ControlHelper.BlendOverParent(this, Palette.CardBackgroundFillSecondary);
            Color surface = _isExpanded ? contentFill : headerFill;
            if (BackColor != surface)
            {
                BackColor = surface;
            }

            ControlHelper.PaintFullBackground(this, e.Graphics);
            Graphics g = e.Graphics;
            FluentGraphics.SetupGraphics(g);

            // Content section
            if (_isExpanded)
            {
                var contentRect = new RectangleF(0.5f, HeaderHeight - 4, Width - 1f, Height - HeaderHeight + 3.5f);
                using (var brush = new SolidBrush(contentFill))
                {
                    FluentGraphics.FillRoundedRect(g, brush, contentRect, radius);
                }
            }

            // Header
            var headerRect = new RectangleF(0.5f, 0.5f, Width - 1f, HeaderHeight - 1f);
            using (var brush = new SolidBrush(headerFill))
            {
                FluentGraphics.FillRoundedRect(g, brush, headerRect, radius);
            }

            using (var pen = new Pen(Palette.CardStrokeDefault, 1f))
            {
                if (_isExpanded)
                {
                    var outer = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
                    FluentGraphics.DrawRoundedRect(g, pen, outer, radius);
                    g.DrawLine(pen, 1, HeaderHeight, Width - 1, HeaderHeight);
                }
                else
                {
                    FluentGraphics.DrawRoundedRect(g, pen, headerRect, radius);
                }
            }

            // Header content: icon + text + chevron
            float x = Scale(14);
            Color textColor = Enabled ? Palette.TextFillPrimary : Palette.TextFillDisabled;
            int iconBox = Scale(16);

            if (_headerIcon != SymbolRegular.Empty)
            {
                FluentGraphics.DrawGlyph(g, FluentGraphics.Glyph((int)_headerIcon), FluentTypography.IconRegular(12f), textColor, new RectangleF(x, (HeaderHeight - iconBox) / 2f, iconBox, iconBox));
                x += Scale(26);
            }

            using (var format = new StringFormat { LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap, Trimming = StringTrimming.EllipsisCharacter })
            {
                FluentGraphics.DrawText(g, _header, Font, textColor, new RectangleF(x, 0, Width - x - Scale(44), HeaderHeight), format);
            }

            int chevron = _isExpanded ? 0xE70E : 0xE70D;
            FluentGraphics.DrawGlyph(g, FluentGraphics.Glyph(chevron), FluentTypography.Icon(9f), textColor, new RectangleF(Width - Scale(32), (HeaderHeight - iconBox) / 2f, iconBox, iconBox));
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

    /// <summary>Fluent card expander: expander with card styling.</summary>
    public class FluentCardExpander : FluentExpander
    {
    }
}