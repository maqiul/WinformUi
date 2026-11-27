using System;
using System.Drawing;
using System.Windows.Forms;
using WinformUi.Drawing;
using WinformUi.Theming;

namespace WinformUi.Controls
{
    /// <summary>ProfessionalColorTable mapped onto Fluent flyout tokens.</summary>
    [System.ComponentModel.ToolboxItem(false)]
    public class FluentColorTable : ProfessionalColorTable
    {
        private static FluentPalette P => ApplicationThemeManager.Palette;

        private static Color Surface => P.AcrylicBackgroundFillDefault;
        private static Color Hover => AccentColor.Blend(P.SubtleFillSecondary, Surface);
        private static Color Pressed => AccentColor.Blend(P.SubtleFillTertiary, Surface);

        public override Color MenuBorder => P.SurfaceStrokeFlyout;
        public override Color MenuItemBorder => Color.Transparent;
        public override Color MenuItemSelected => Hover;
        public override Color MenuItemSelectedGradientBegin => Hover;
        public override Color MenuItemSelectedGradientEnd => Hover;
        public override Color MenuItemPressedGradientBegin => Pressed;
        public override Color MenuItemPressedGradientEnd => Pressed;
        public override Color MenuItemPressedGradientMiddle => Pressed;
        public override Color MenuStripGradientBegin => Color.Transparent;
        public override Color MenuStripGradientEnd => Color.Transparent;
        public override Color ToolStripBorder => Color.Transparent;
        public override Color ToolStripGradientBegin => Color.Transparent;
        public override Color ToolStripGradientMiddle => Color.Transparent;
        public override Color ToolStripGradientEnd => Color.Transparent;
        public override Color ToolStripDropDownBackground => Surface;
        public override Color ImageMarginGradientBegin => Surface;
        public override Color ImageMarginGradientMiddle => Surface;
        public override Color ImageMarginGradientEnd => Surface;
        public override Color SeparatorDark => AccentColor.Blend(P.DividerStrokeDefault, Surface);
        public override Color SeparatorLight => Color.Transparent;
        public override Color CheckBackground => P.SystemAccentColorPrimary;
        public override Color CheckPressedBackground => P.AccentFillSecondary;
        public override Color CheckSelectedBackground => P.AccentFillSecondary;
        public override Color ButtonSelectedBorder => Color.Transparent;
        public override Color ButtonSelectedHighlight => Hover;
        public override Color ButtonSelectedGradientBegin => Hover;
        public override Color ButtonSelectedGradientMiddle => Hover;
        public override Color ButtonSelectedGradientEnd => Hover;
        public override Color ButtonPressedGradientBegin => Pressed;
        public override Color ButtonPressedGradientMiddle => Pressed;
        public override Color ButtonPressedGradientEnd => Pressed;
        public override Color OverflowButtonGradientBegin => Color.Transparent;
        public override Color OverflowButtonGradientMiddle => Color.Transparent;
        public override Color OverflowButtonGradientEnd => Color.Transparent;
    }

    /// <summary>ToolStrip renderer producing Fluent menus: rounded hover fills, themed text.</summary>
    [System.ComponentModel.ToolboxItem(false)]
    public class FluentToolStripRenderer : ToolStripProfessionalRenderer
    {
        public FluentToolStripRenderer() : base(new FluentColorTable())
        {
            RoundedEdges = false;
        }

        private static FluentPalette P => ApplicationThemeManager.Palette;

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            if (!e.Item.Selected && !e.Item.Pressed)
            {
                return;
            }

            Color surface = P.AcrylicBackgroundFillDefault;
            Color fill = e.Item.Pressed
                ? AccentColor.Blend(P.SubtleFillTertiary, surface)
                : AccentColor.Blend(P.SubtleFillSecondary, surface);

            // MenuStrip top-level items sit on a transparent bar; dropdown items on the flyout.
            bool topLevel = e.ToolStrip is MenuStrip && e.Item.OwnerItem == null;
            if (topLevel)
            {
                fill = e.Item.Pressed
                    ? AccentColor.Blend(P.SubtleFillTertiary, ControlHelper.GetEffectiveBackground(e.ToolStrip))
                    : AccentColor.Blend(P.SubtleFillSecondary, ControlHelper.GetEffectiveBackground(e.ToolStrip));
            }

            var rect = new Rectangle(2, 1, e.Item.Width - 4, e.Item.Height - 2);
            using (var brush = new SolidBrush(fill))
            {
                FluentGraphics.FillRoundedRect(e.Graphics, brush, rect, FluentGraphics.ControlCornerRadius);
            }
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = e.Item.Enabled ? P.TextFillPrimary : P.TextFillDisabled;

            // The transparent menu bar paints onto the Mica window's live DC, where
            // GDI text leaves pixel alpha at 0 and DWM reads it as transparent;
            // only that surface keeps GDI+. Buffered dropdowns use native GDI text.
            bool topLevel = e.ToolStrip is MenuStrip && e.Item.OwnerItem == null;
            if (!topLevel)
            {
                base.OnRenderItemText(e);
                return;
            }

            using (var brush = new SolidBrush(e.TextColor))
            using (var format = new StringFormat { LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap, HotkeyPrefix = System.Drawing.Text.HotkeyPrefix.Show })
            {
                format.Alignment = (e.TextFormat & TextFormatFlags.HorizontalCenter) != 0
                    ? StringAlignment.Center
                    : StringAlignment.Near;
                e.Graphics.DrawString(e.Text, e.TextFont, brush, e.TextRectangle, format);
            }
        }

        protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
        {
            e.ArrowColor = P.TextFillSecondary;
            base.OnRenderArrow(e);
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            Color line = AccentColor.Blend(P.DividerStrokeDefault, P.AcrylicBackgroundFillDefault);
            using (var pen = new Pen(line, 1f))
            {
                int y = e.Item.Height / 2;
                e.Graphics.DrawLine(pen, 8, y, e.Item.Width - 8, y);
            }
        }

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            if (e.ToolStrip is MenuStrip)
            {
                return; // transparent bar
            }

            using (var brush = new SolidBrush(P.AcrylicBackgroundFillDefault))
            {
                e.Graphics.FillRectangle(brush, e.AffectedBounds);
            }
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            if (e.ToolStrip is MenuStrip)
            {
                return;
            }

            using (var pen = new Pen(P.SurfaceStrokeFlyout, 1f))
            {
                var rect = new Rectangle(0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
                e.Graphics.DrawRectangle(pen, rect);
            }
        }

        protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
        {
            using (var brush = new SolidBrush(P.AcrylicBackgroundFillDefault))
            {
                e.Graphics.FillRectangle(brush, e.AffectedBounds);
            }
        }
    }

    /// <summary>Fluent context menu: flyout surface, rounded hover items.</summary>
    public class FluentContextMenuStrip : ContextMenuStrip
    {
        public FluentContextMenuStrip()
        {
            Renderer = new FluentToolStripRenderer();
            BackColor = ApplicationThemeManager.Palette.AcrylicBackgroundFillDefault;
            Font = FluentTypography.Body;
            ShowImageMargin = false;
            Padding = new Padding(2);
            Opening += OnOpening;
        }

        private void OnOpening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            BackColor = ApplicationThemeManager.Palette.AcrylicBackgroundFillDefault;
            using (var path = FluentGraphics.RoundedRect(new RectangleF(0, 0, Width, Height), FluentGraphics.PopupCornerRadius))
            {
                Region = new Region(path);
            }
        }
    }

    /// <summary>Fluent menu bar: transparent bar with subtle hover.</summary>
    public class FluentMenuStrip : MenuStrip
    {
        public FluentMenuStrip()
        {
            Renderer = new FluentToolStripRenderer();
            BackColor = Color.Transparent;
            Font = FluentTypography.Body;
            Padding = new Padding(4, 2, 4, 2);
        }
    }

    /// <summary>Fluent tool tip: rounded flyout surface.</summary>
    public class FluentToolTip : ToolTip
    {
        public FluentToolTip()
        {
            OwnerDraw = true;
            InitialDelay = 400;
            ReshowDelay = 100;
            AutoPopDelay = 8000;
            Popup += OnPopup;
            Draw += OnDraw;
        }

        private void OnPopup(object sender, PopupEventArgs e)
        {
            // Measure with the same GDI flags used to draw, so the flyout fits exactly.
            string text = GetToolTip(e.AssociatedControl) ?? "";
            Size size;
            int dpi = ControlHelper.DpiOf(e.AssociatedControl);
            using (var bmp = new Bitmap(1, 1))
            {
                bmp.SetResolution(dpi, dpi);
                using (var g = Graphics.FromImage(bmp))
                {
                    size = TextRenderer.MeasureText(g, text, FluentTypography.Body, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
                }
            }
            e.ToolTipSize = new Size(size.Width + 18, size.Height + 12);
        }

        private void OnDraw(object sender, DrawToolTipEventArgs e)
        {
            FluentPalette p = ApplicationThemeManager.Palette;
            Graphics g = e.Graphics;
            FluentGraphics.SetupGraphics(g);

            var bounds = new RectangleF(0.5f, 0.5f, e.Bounds.Width - 1f, e.Bounds.Height - 1f);
            using (var brush = new SolidBrush(p.AcrylicBackgroundFillDefault))
            {
                FluentGraphics.FillRoundedRect(g, brush, bounds, FluentGraphics.OverlayCornerRadius);
            }

            using (var pen = new Pen(p.SurfaceStrokeFlyout, 1f))
            {
                FluentGraphics.DrawRoundedRect(g, pen, bounds, FluentGraphics.OverlayCornerRadius);
            }

            using (var format = new StringFormat { LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap })
            {
                FluentGraphics.DrawText(g, e.ToolTipText, FluentTypography.Body, p.TextFillPrimary,
                    new RectangleF(8, 6, e.Bounds.Width - 16, e.Bounds.Height - 12), format);
            }
        }
    }
}