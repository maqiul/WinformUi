using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using WinformUi.Theming;

namespace WinformUi.Controls
{
    /// <summary>Fluent status strip using the native ToolStrip item model.</summary>
    [ToolboxItem(true)]
    public class FluentStatusStrip : StatusStrip
    {
        private ThemeSubscription _themeSubscription;

        public FluentStatusStrip()
        {
            Dock = DockStyle.Bottom;
            Height = 28;
            Font = FluentTypography.Caption;
            GripStyle = ToolStripGripStyle.Hidden;
            SizingGrip = false;
            ShowItemToolTips = true;
            Padding = new Padding(4, 0, 4, 0);
            Renderer = new FluentStatusStripRenderer();
            ApplyTheme();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (_themeSubscription == null)
            {
                _themeSubscription = new ThemeSubscription(this, ApplyTheme);
            }

            ApplyTheme();
        }

        protected override void OnParentChanged(EventArgs e)
        {
            base.OnParentChanged(e);
            ApplyTheme();
        }

        protected override void OnItemAdded(ToolStripItemEventArgs e)
        {
            base.OnItemAdded(e);
            ApplyItemTheme(e.Item);
        }

        private void ApplyTheme()
        {
            FluentPalette p = ApplicationThemeManager.Palette;
            BackColor = ControlHelper.BlendOverParent(this, p.SolidBackgroundFillSecondary);
            ForeColor = p.TextFillPrimary;

            foreach (ToolStripItem item in Items)
            {
                ApplyItemTheme(item);
            }

            Invalidate();
        }

        private void ApplyItemTheme(ToolStripItem item)
        {
            item.BackColor = BackColor;
            item.ForeColor = ForeColor;
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

    internal sealed class FluentStatusStripRenderer : FluentToolStripRenderer
    {
        private static FluentPalette P => ApplicationThemeManager.Palette;

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            using (var brush = new SolidBrush(P.SolidBackgroundFillSecondary))
            {
                e.Graphics.FillRectangle(brush, e.AffectedBounds);
            }
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            using (var pen = new Pen(P.DividerStrokeDefault, 1f))
            {
                e.Graphics.DrawLine(pen, 0, 0, e.ToolStrip.Width - 1, 0);
            }
        }
    }
}
