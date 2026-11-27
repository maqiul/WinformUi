using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using WinformUi.Theming;

namespace WinformUi.Controls
{
    /// <summary>
    /// Fluent-themed tool strip on the native ToolStrip item model: buttons,
    /// toggle buttons, drop-downs, separators and overflow all keep standard behavior.
    /// </summary>
    [ToolboxItem(true)]
    public class FluentToolStrip : ToolStrip
    {
        private ThemeSubscription _themeSubscription;

        public FluentToolStrip()
        {
            Dock = DockStyle.Top;
            Renderer = new FluentToolStripStripRenderer();
            ImageScalingSize = new Size(16, 16);
            GripStyle = ToolStripGripStyle.Hidden;
            AutoSize = true;
            Padding = new Padding(6, 3, 6, 3);
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
            BackColor = ControlHelper.BlendOverParent(this, p.ControlFillDefault);
            ForeColor = p.TextFillPrimary;

            foreach (ToolStripItem item in Items)
            {
                ApplyItemTheme(item);
            }

            Invalidate();
        }

        private void ApplyItemTheme(ToolStripItem item)
        {
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

    internal sealed class FluentToolStripStripRenderer : FluentToolStripRenderer
    {
        private static FluentPalette P => ApplicationThemeManager.Palette;

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            // Unlike flyout menus, a docked toolbar paints the strip's own themed color.
            using (var brush = new SolidBrush(e.ToolStrip.BackColor))
            {
                e.Graphics.FillRectangle(brush, e.AffectedBounds);
            }
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            if (e.ToolStrip.Orientation == Orientation.Horizontal)
            {
                using (var pen = new Pen(P.DividerStrokeDefault, 1f))
                {
                    int y = e.ToolStrip.Height - 1;
                    e.Graphics.DrawLine(pen, 0, y, e.ToolStrip.Width - 1, y);
                }
            }
        }
    }
}
