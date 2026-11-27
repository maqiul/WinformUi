using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using WinformUi.Drawing;
using WinformUi.Theming;

namespace WinformUi.Controls
{
    /// <summary>
    /// Lightweight icon presenter: renders a Fluent System Symbol (regular or filled),
    /// a raw glyph character or an image, scaled to the control bounds and tinted by the theme.
    /// </summary>
    [ToolboxItem(true)]
    public class FluentIcon : FluentControlBase
    {
        private SymbolRegular _symbol = SymbolRegular.Empty;
        private SymbolFilled _filledSymbol = SymbolFilled.Empty;
        private string _glyph = string.Empty;
        private Image _image;

        public FluentIcon()
        {
            TabStop = false;
            SetStyle(ControlStyles.Selectable, false);
            Size = new Size(16, 16);
        }

        /// <summary>Regular-variant Fluent symbol; Empty falls through to the other sources.</summary>
        [DefaultValue(SymbolRegular.Empty)]
        public SymbolRegular Symbol
        {
            get { return _symbol; }
            set
            {
                if (_symbol != value)
                {
                    _symbol = value;
                    Invalidate();
                }
            }
        }

        /// <summary>Filled-variant Fluent symbol; takes precedence over <see cref="Symbol"/>.</summary>
        [DefaultValue(SymbolFilled.Empty)]
        public SymbolFilled FilledSymbol
        {
            get { return _filledSymbol; }
            set
            {
                if (_filledSymbol != value)
                {
                    _filledSymbol = value;
                    Invalidate();
                }
            }
        }

        /// <summary>Raw glyph character (from any icon font); used when no symbol or image is set.</summary>
        [DefaultValue("")]
        public string Glyph
        {
            get { return _glyph; }
            set
            {
                if (!string.Equals(_glyph, value, StringComparison.Ordinal))
                {
                    _glyph = value ?? string.Empty;
                    Invalidate();
                }
            }
        }

        /// <summary>Bitmap source drawn centered and aspect-fit; takes precedence over glyph sources.</summary>
        [DefaultValue(null)]
        [RefreshProperties(RefreshProperties.Repaint)]
        public Image Image
        {
            get { return _image; }
            set
            {
                if (!ReferenceEquals(_image, value))
                {
                    _image = value;
                    Invalidate();
                }
            }
        }

        /// <summary>Foreground tint; follows the theme unless explicitly set.</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color Foreground
        {
            get { return ForeColor; }
            set { ForeColor = value; }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            FluentGraphics.SetupGraphics(g);
            Color color = Enabled ? ForeColor : Palette.TextFillDisabled;
            var bounds = new RectangleF(0, 0, Width, Height);

            if (_image != null)
            {
                DrawImageAspectFit(g, _image, bounds);
                return;
            }

            string glyph;
            Font font;
            if (_filledSymbol != SymbolFilled.Empty)
            {
                glyph = FluentGraphics.Glyph((int)_filledSymbol);
                font = FluentTypography.IconFilled(GlyphEmPts(bounds));
            }
            else if (_symbol != SymbolRegular.Empty)
            {
                glyph = FluentGraphics.Glyph((int)_symbol);
                font = FluentTypography.IconRegular(GlyphEmPts(bounds));
            }
            else if (_glyph.Length > 0)
            {
                glyph = _glyph;
                font = FluentTypography.Icon(GlyphEmPts(bounds));
            }
            else
            {
                return;
            }

            // Cached shared fonts: never dispose here.
            FluentGraphics.DrawGlyph(g, glyph, font, color, bounds);
        }

        private float GlyphEmPts(RectangleF bounds)
        {
            float px = Math.Max(6f, Math.Min(bounds.Width, bounds.Height));
            // Font sizes are in points (DPI-scaled pixels); divide by the scale
            // so a glyph sized for the control's device pixels never overflows it.
            return px * 0.75f / DpiScale;
        }

        private static void DrawImageAspectFit(Graphics g, Image image, RectangleF bounds)
        {
            float scale = Math.Min(bounds.Width / image.Width, bounds.Height / image.Height);
            float w = image.Width * scale;
            float h = image.Height * scale;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.DrawImage(image, bounds.X + (bounds.Width - w) / 2f, bounds.Y + (bounds.Height - h) / 2f, w, h);
        }
    }

    /// <summary>
    /// Picture box with Fluent defaults: aspect-uniform sizing and optional
    /// rounded corners that clip the image and paint a hairline border.
    /// </summary>
    [ToolboxItem(true)]
    public class FluentPictureBox : PictureBox
    {
        private int _cornerRadius;
        private ThemeSubscription _themeSubscription;

        public FluentPictureBox()
        {
            SizeMode = PictureBoxSizeMode.Zoom;
            ApplyTheme();
        }

        /// <summary>Rounded corner radius in device-independent pixels; 0 disables clipping and border.</summary>
        [DefaultValue(0)]
        public int CornerRadius
        {
            get { return _cornerRadius; }
            set
            {
                if (_cornerRadius != value)
                {
                    _cornerRadius = Math.Max(0, value);
                    Invalidate();
                }
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (_themeSubscription == null)
            {
                _themeSubscription = new ThemeSubscription(this, OnThemeChanged);
            }

            ApplyTheme();
        }

        private void OnThemeChanged()
        {
            ApplyTheme();
            Invalidate();
        }

        private void ApplyTheme()
        {
            if (BackColor == SystemColors.Control)
            {
                BackColor = Color.Transparent;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (_cornerRadius <= 0)
            {
                base.OnPaint(e);
                return;
            }

            float radius = _cornerRadius * ControlHelper.DpiOf(this) / 96f;
            var bounds = new RectangleF(0, 0, Width, Height);
            using (GraphicsPath path = FluentGraphics.RoundedRect(bounds, radius))
            {
                e.Graphics.SetClip(path);
                base.OnPaint(e);
                e.Graphics.ResetClip();
            }

            FluentGraphics.SetupGraphics(e.Graphics);
            using (var pen = new Pen(ApplicationThemeManager.Palette.DividerStrokeDefault, 1f))
            {
                FluentGraphics.DrawRoundedRect(e.Graphics, pen, RectangleF.Inflate(bounds, -0.5f, -0.5f), radius);
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
