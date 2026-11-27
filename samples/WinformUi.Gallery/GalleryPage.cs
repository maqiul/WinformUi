using System;
using System.Drawing;
using System.Windows.Forms;
using WinformUi.Controls;
using WinformUi.Theming;

namespace WinformUi.Gallery
{
    /// <summary>Base gallery page: scrollable vertical flow of sections and rows.</summary>
    public class GalleryPage : FluentPage
    {
        protected FlowLayoutPanel Root;
        private readonly FluentScrollViewer _scroll;

        public GalleryPage(string title)
        {
            Padding = new Padding(0);

            _scroll = new FluentScrollViewer { Dock = DockStyle.Fill };
            Root = new SurfaceFlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                BackColor = BackColor,
                Padding = new Padding(28, 20, 28, 28)
            };
            _scroll.Content = Root;
            _scroll.Resize += (s, e) => UpdateRowWidths();
            Root.Layout += (s, e) => Root.Invalidate(true);
            Controls.Add(_scroll);
            ApplicationThemeManager.ThemeChanged += OnThemeChanged;

            var titleLabel = new FluentLabel
            {
                Text = title,
                Font = FluentTypography.Title,
                UseSecondaryColor = false,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 16)
            };
            Root.Controls.Add(titleLabel);
        }

        protected FluentLabel AddDescription(string text)
        {
            var label = new FluentLabel
            {
                Text = text,
                AutoSize = true,
                MaximumSize = new Size(760, 0),
                Margin = new Padding(0, 0, 0, 12)
            };
            Root.Controls.Add(label);
            return label;
        }

        protected FluentLabel AddSection(string text)
        {
            var label = new FluentLabel
            {
                Text = text,
                Font = FluentTypography.Subtitle,
                UseSecondaryColor = false,
                AutoSize = true,
                Margin = new Padding(0, 20, 0, 10)
            };
            Root.Controls.Add(label);
            return label;
        }

        private void OnThemeChanged(object sender, EventArgs e)
        {
            // Compute the surface directly: this handler runs before FluentPage's
            // own subscription refreshes BackColor, so reading BackColor here
            // would copy the previous theme's fill.
            Color surface = SurfaceColor;
            Root.BackColor = surface;
            SyncSurface(Root, surface);
        }

        private static void SyncSurface(Control parent, Color surface)
        {
            foreach (Control c in parent.Controls)
            {
                if (c is SurfaceFlowLayoutPanel || c is SurfacePanel)
                {
                    c.BackColor = surface;
                }

                SyncSurface(c, surface);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ApplicationThemeManager.ThemeChanged -= OnThemeChanged;
            }

            base.Dispose(disposing);
        }

        protected FlowLayoutPanel AddRow()
        {
            var row = new SurfaceFlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                AutoSize = true,
                BackColor = BackColor,
                Margin = new Padding(0, 0, 0, 8)
            };
            Root.Controls.Add(row);
            UpdateRowWidths();
            return row;
        }

        private void UpdateRowWidths()
        {
            int width = Math.Max(100, _scroll.Width - 16);
            Root.MinimumSize = new Size(width, 0);
            foreach (Control c in Root.Controls)
            {
                if (c is FlowLayoutPanel row)
                {
                    row.MaximumSize = new Size(Math.Max(100, width - Root.Padding.Horizontal), 0);
                }
            }

            Root.Invalidate(true);
        }

        protected static T WithMargin<T>(T control, int right = 8, int bottom = 8) where T : Control
        {
            control.Margin = new Padding(0, 0, right, bottom);
            return control;
        }
    }

    /// <summary>Flow panel whose background erase writes real alpha (GDI erase leaves alpha 0, which DWM backdrops read as transparency).</summary>
    internal sealed class SurfaceFlowLayoutPanel : FlowLayoutPanel
    {
        public SurfaceFlowLayoutPanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.ResetClip();
            using (var brush = new SolidBrush(BackColor))
            {
                e.Graphics.FillRectangle(brush, 0, 0, Width, Height);
            }

            base.OnPaint(e);
        }
    }

    /// <summary>Plain-panel equivalent of SurfaceFlowLayoutPanel for absolute-positioned content.</summary>
    internal sealed class SurfacePanel : Panel
    {
        public SurfacePanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.ResetClip();
            using (var brush = new SolidBrush(BackColor))
            {
                e.Graphics.FillRectangle(brush, 0, 0, Width, Height);
            }

            base.OnPaint(e);
        }
    }
}