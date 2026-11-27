using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using WinformUi.Drawing;
using WinformUi.Theming;

namespace WinformUi.Controls
{
    /// <summary>
    /// Themed wrapping panel: lays children out left-to-right in rows that wrap at
    /// the edge (uniform equal-size items, like the reference wrap panel without
    /// virtualization — use <see cref="FluentGridView"/> for large item sets).
    /// </summary>
    [ToolboxItem(true)]
    public class FluentWrapPanel : FlowLayoutPanel
    {
        private ThemeSubscription _themeSubscription;

        public FluentWrapPanel()
        {
            WrapContents = true;
            FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            AutoScroll = true;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (_themeSubscription == null)
            {
                _themeSubscription = new ThemeSubscription(this, () => Invalidate());
            }

            Invalidate();
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            ControlHelper.PaintFullBackground(this, e.Graphics);
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

    /// <summary>
    /// Equal-cell layout panel: children are arranged in a fixed Rows x Columns grid
    /// with identical cell sizes, like the reference uniform grid panel.
    /// </summary>
    [ToolboxItem(true)]
    public class FluentUniformGrid : Panel
    {
        private int _rows;
        private int _columns = 1;

        public FluentUniformGrid()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        /// <summary>Row count; 0 means auto-computed from Columns and the child count.</summary>
        [Category("Layout")]
        [DefaultValue(0)]
        public int Rows
        {
            get { return _rows; }
            set
            {
                _rows = Math.Max(0, value);
                PerformLayout();
            }
        }

        [Category("Layout")]
        [DefaultValue(1)]
        public int Columns
        {
            get { return _columns; }
            set
            {
                _columns = Math.Max(1, value);
                PerformLayout();
            }
        }

        protected override void OnLayout(LayoutEventArgs e)
        {
            base.OnLayout(e);
            int count = Controls.Count;
            if (count == 0)
            {
                return;
            }

            int cols = _columns;
            int rows = _rows;
            if (rows == 0 && cols > 1)
            {
                rows = (count + cols - 1) / cols;
            }
            else if (rows > 0 && cols == 1 && count > rows)
            {
                cols = (count + rows - 1) / rows;
            }

            if (rows == 0)
            {
                rows = count;
                cols = 1;
            }

            int cellWidth = Math.Max(1, Width / cols);
            int cellHeight = Math.Max(1, Height / rows);
            for (int i = 0; i < count; i++)
            {
                int r = i / cols;
                int c = i % cols;
                if (r >= rows)
                {
                    Controls[i].Visible = false;
                    continue;
                }

                Controls[i].Visible = true;
                Controls[i].Bounds = new Rectangle(c * cellWidth, r * cellHeight, cellWidth, cellHeight);
            }
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            ControlHelper.PaintFullBackground(this, e.Graphics);
        }
    }
}
