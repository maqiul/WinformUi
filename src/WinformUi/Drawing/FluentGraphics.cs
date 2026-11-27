using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using WinformUi.Theming;

namespace WinformUi.Drawing
{
    /// <summary>
    /// GDI+ helpers reproducing the Fluent visual language:
    /// 4px control corners, 8px overlay corners, elevation borders, focus visuals.
    /// </summary>
    public static class FluentGraphics
    {
        public const float ControlCornerRadius = 4f;
        public const float OverlayCornerRadius = 4f;
        public const float PopupCornerRadius = 8f;

        public static GraphicsPath RoundedRect(RectangleF bounds, float radius)
        {
            var path = new GraphicsPath();
            if (radius <= 0f)
            {
                path.AddRectangle(bounds);
                path.CloseFigure();
                return path;
            }

            float d = radius * 2f;
            d = Math.Min(d, Math.Min(bounds.Width, bounds.Height));
            var arc = new RectangleF(bounds.X, bounds.Y, d, d);

            path.AddArc(arc, 180, 90);
            arc.X = bounds.Right - d;
            path.AddArc(arc, 270, 90);
            arc.Y = bounds.Bottom - d;
            path.AddArc(arc, 0, 90);
            arc.X = bounds.X;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static void FillRoundedRect(Graphics g, Brush brush, RectangleF bounds, float radius)
        {
            using (var path = RoundedRect(bounds, radius))
            {
                g.FillPath(brush, path);
            }
        }

        public static void DrawRoundedRect(Graphics g, Pen pen, RectangleF bounds, float radius)
        {
            using (var path = RoundedRect(bounds, radius))
            {
                g.DrawPath(pen, path);
            }
        }

        /// <summary>
        /// The Fluent "elevation border": 1px stroke, slightly darker at the bottom edge.
        /// </summary>
        public static void DrawElevationBorder(Graphics g, RectangleF bounds, float radius, FluentPalette palette, bool onAccent = false)
        {
            Color top = onAccent ? palette.ControlStrokeOnAccentDefault : palette.ControlStrokeSecondary;
            Color bottom = onAccent ? palette.ControlStrokeOnAccentSecondary : palette.ControlStrokeDefault;

            // The Fluent gradient runs bottom(strong) -> top(weak) over 3px; approximate with a vertical blend.
            using (var path = RoundedRect(bounds, radius))
            using (var brush = new LinearGradientBrush(
                new RectangleF(bounds.X, bounds.Y - 1, bounds.Width, bounds.Height + 2),
                top, bottom, LinearGradientMode.Vertical))
            using (var pen = new Pen(brush, 1f))
            {
                g.DrawPath(pen, path);
            }
        }

        /// <summary>2px keyboard focus visual (outer dark/light ring + inner contrast ring).</summary>
        public static void DrawFocusVisual(Graphics g, RectangleF bounds, float radius, FluentPalette palette)
        {
            using (var outer = new Pen(palette.FocusStrokeOuter, 2f))
            {
                DrawRoundedRect(g, outer, bounds, radius + 1f);
            }

            var inner = RectangleF.Inflate(bounds, -2f, -2f);
            using (var pen = new Pen(palette.FocusStrokeInner, 1f))
            {
                DrawRoundedRect(g, pen, inner, radius);
            }
        }

        public static void SetupGraphics(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            g.PixelOffsetMode = PixelOffsetMode.Half;
        }

        /// <summary>Draws a Fluent System Icons glyph centered in the given bounds.</summary>
        public static void DrawGlyph(Graphics g, string glyph, Font iconFont, Color color, RectangleF bounds)
        {
            using (var brush = new SolidBrush(color))
            using (var format = new StringFormat(StringFormatFlags.NoWrap))
            {
                // A layout rectangle clips the glyph to the font cell, cutting
                // icon edges; measure and draw at a point instead.
                SizeF size = g.MeasureString(glyph, iconFont, int.MaxValue, format);
                float x = bounds.X + (bounds.Width - size.Width) / 2f;
                float y = bounds.Y + (bounds.Height - size.Height) / 2f;
                g.DrawString(glyph, iconFont, brush, x, y, format);
            }
        }

        public static string Glyph(int codePoint)
        {
            return char.ConvertFromUtf32(codePoint);
        }

        /// <summary>
        /// Draws layout text with GDI (TextRenderer). GDI+ DrawString with
        /// ClearTypeGridFit renders Regular weights heavier and softer than the
        /// native GDI text used by standard WinForms controls. GDI leaves glyph
        /// pixel alpha at 0, which the DWM backdrop reads as transparent, so the
        /// alpha is restored afterwards (see RepairTextAlpha).
        /// </summary>
        public static void DrawText(Graphics g, string text, Font font, Color color, RectangleF layout, StringFormat format)
        {
            Rectangle rect = Rectangle.Round(layout);
            TextRenderer.DrawText(g, text ?? string.Empty, font, rect, color, TextFlags(format));
            RepairTextAlpha(g, rect, ApplicationThemeManager.Palette.ApplicationBackground);
        }

        /// <summary>Point-origin text with no padding, for native-metric layouts such as edit controls.</summary>
        public static void DrawText(Graphics g, string text, Font font, Color color, Point origin)
        {
            // Callers repair alpha once over the whole painted surface afterwards.
            TextRenderer.DrawText(g, text ?? string.Empty, font, origin, color, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
        }

        /// <summary>
        /// GDI writes glyph RGB but zeroes the alpha channel. Pixels the text
        /// touched (alpha 0, RGB different from the clear seed) get alpha 255
        /// back; untouched backdrop pixels stay transparent. Only DIB sections
        /// expose pixels; live window DCs without a backdrop ignore alpha
        /// anyway and are skipped.
        /// </summary>
        public static void RepairTextAlpha(Graphics g, Rectangle rect, Color seed)
        {
            IntPtr hdc = g.GetHdc();
            try
            {
                IntPtr hbmp = GetCurrentObject(hdc, OBJ_BITMAP);
                if (hbmp == IntPtr.Zero || GetObject(hbmp, Marshal.SizeOf(typeof(BITMAP)), out BITMAP surface) == 0)
                {
                    return;
                }

                if (surface.bmBits == IntPtr.Zero || surface.bmBitsPixel != 32)
                {
                    return;
                }

                int height = Math.Abs(surface.bmHeight);
                bool topDown = surface.bmHeight < 0;
                rect.Intersect(Rectangle.FromLTRB(0, 0, surface.bmWidth, height));
                if (rect.Width <= 0 || rect.Height <= 0)
                {
                    return;
                }

                byte[] row = new byte[rect.Width * 4];
                for (int r = 0; r < rect.Height; r++)
                {
                    int sourceRow = topDown ? rect.Y + r : height - 1 - rect.Y - r;
                    IntPtr rowPtr = IntPtr.Add(surface.bmBits, sourceRow * surface.bmWidthBytes + rect.X * 4);
                    Marshal.Copy(rowPtr, row, 0, row.Length);

                    bool changed = false;
                    for (int i = 0; i < row.Length; i += 4)
                    {
                        if (row[i + 3] == 0 && (row[i] != seed.B || row[i + 1] != seed.G || row[i + 2] != seed.R))
                        {
                            row[i + 3] = 255;
                            changed = true;
                        }
                    }

                    if (changed)
                    {
                        Marshal.Copy(row, 0, rowPtr, row.Length);
                    }
                }
            }
            finally
            {
                g.ReleaseHdc();
            }
        }

        private const uint OBJ_BITMAP = 7;

        /// <summary>
        /// GDI+ skips alpha-0 fills entirely, so a transparent clear leaves the
        /// buffer RGB at zero; GDI text then blends its fringes against black.
        /// Seed transparent pixels with the color the backdrop approximates.
        /// </summary>
        public static void SeedTransparentBackground(Graphics g, Color rgb)
        {
            IntPtr hdc = g.GetHdc();
            try
            {
                IntPtr hbmp = GetCurrentObject(hdc, OBJ_BITMAP);
                if (hbmp == IntPtr.Zero || GetObject(hbmp, Marshal.SizeOf(typeof(BITMAP)), out BITMAP surface) == 0)
                {
                    return;
                }

                if (surface.bmBits == IntPtr.Zero || surface.bmBitsPixel != 32)
                {
                    return;
                }

                int height = Math.Abs(surface.bmHeight);
                bool topDown = surface.bmHeight < 0;
                byte[] row = new byte[surface.bmWidth * 4];
                for (int r = 0; r < height; r++)
                {
                    int sourceRow = topDown ? r : height - 1 - r;
                    IntPtr rowPtr = IntPtr.Add(surface.bmBits, sourceRow * surface.bmWidthBytes);
                    Marshal.Copy(rowPtr, row, 0, row.Length);

                    bool changed = false;
                    for (int i = 0; i < row.Length; i += 4)
                    {
                        if (row[i + 3] == 0)
                        {
                            row[i] = rgb.B;
                            row[i + 1] = rgb.G;
                            row[i + 2] = rgb.R;
                            changed = true;
                        }
                    }

                    if (changed)
                    {
                        Marshal.Copy(row, 0, rowPtr, row.Length);
                    }
                }
            }
            finally
            {
                g.ReleaseHdc();
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct BITMAP
        {
            public int bmType;
            public int bmWidth;
            public int bmHeight;
            public int bmWidthBytes;
            public ushort bmPlanes;
            public ushort bmBitsPixel;
            public IntPtr bmBits;
        }

        [DllImport("gdi32.dll")]
        private static extern IntPtr GetCurrentObject(IntPtr hdc, uint type);

        [DllImport("gdi32.dll")]
        private static extern int GetObject(IntPtr hgdiobj, int cbBuffer, out BITMAP buffer);

        private static TextFormatFlags TextFlags(StringFormat format)
        {
            // GDI+ DrawString never processes '&' mnemonics unless asked; keep GDI aligned.
            TextFormatFlags flags = TextFormatFlags.NoPrefix;
            if (format == null)
            {
                return flags | TextFormatFlags.WordBreak;
            }

            switch (format.Alignment)
            {
                case StringAlignment.Center: flags |= TextFormatFlags.HorizontalCenter; break;
                case StringAlignment.Far: flags |= TextFormatFlags.Right; break;
            }

            switch (format.LineAlignment)
            {
                case StringAlignment.Center: flags |= TextFormatFlags.VerticalCenter; break;
                case StringAlignment.Far: flags |= TextFormatFlags.Bottom; break;
            }

            flags |= (format.FormatFlags & StringFormatFlags.NoWrap) != 0
                ? TextFormatFlags.SingleLine
                : TextFormatFlags.WordBreak;

            switch (format.Trimming)
            {
                case StringTrimming.EllipsisCharacter: flags |= TextFormatFlags.EndEllipsis; break;
                case StringTrimming.EllipsisWord: flags |= TextFormatFlags.WordEllipsis; break;
            }

            return flags;
        }

        public static RectangleF Deflate(RectangleF r, float amount)
        {
            return RectangleF.Inflate(r, -amount, -amount);
        }
    }
}