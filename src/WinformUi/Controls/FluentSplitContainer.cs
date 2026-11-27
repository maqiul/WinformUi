using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using WinformUi.Drawing;
using WinformUi.Theming;

namespace WinformUi.Controls
{
    /// <summary>Fluent split container with native panel sizing and a themed splitter.</summary>
    [DefaultEvent("SplitterMoved")]
    public class FluentSplitContainer : SplitContainer
    {
        private static FluentPalette Palette => ApplicationThemeManager.Palette;
        private ThemeSubscription _themeSubscription;
        private double? _pendingRatio;

        public FluentSplitContainer()
        {
            Font = FluentTypography.Body;
            SplitterWidth = 8;
            SplitterIncrement = 1;
            BackColor = Color.Transparent;
            Panel1.BackColor = Color.Transparent;
            Panel2.BackColor = Color.Transparent;
        }

        /// <summary>Native SplitContainer keeps the last-set distance when the control shrinks,
        /// which pushes the splitter outside the visible area; re-clamp it instead.</summary>
        protected override void SetBoundsCore(int x, int y, int width, int height, BoundsSpecified specified)
        {
            base.SetBoundsCore(x, y, width, height, specified);
            if (IsHandleCreated && Orientation == Orientation.Vertical)
            {
                int total = width - SplitterWidth;
                int maximum = total - Panel2MinSize;
                if (maximum >= Panel1MinSize && SplitterDistance > maximum)
                {
                    try
                    {
                        SplitterDistance = maximum;
                    }
                    catch (InvalidOperationException)
                    {
                    }
                }
            }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public double SplitterRatio
        {
            get
            {
                int total = Orientation == Orientation.Vertical ? Width : Height;
                return total > 0 ? Math.Max(0d, Math.Min(1d, SplitterDistance / (double)total)) : 0.5d;
            }
            set { SetSplitterRatio(value); }
        }

        public void SetSplitterRatio(double ratio)
        {
            if (double.IsNaN(ratio) || ratio < 0d || ratio > 1d)
            {
                throw new ArgumentOutOfRangeException(nameof(ratio));
            }

            _pendingRatio = ratio;
            if (!IsHandleCreated)
            {
                return;
            }

            ApplyPendingRatio();
        }

        private void ApplyPendingRatio()
        {
            if (!_pendingRatio.HasValue)
            {
                return;
            }

            int total = Orientation == Orientation.Vertical ? Width : Height;
            if (total <= 0)
            {
                return;
            }

            int distance = (int)Math.Round(total * _pendingRatio.Value);
            try
            {
                SplitterDistance = Math.Max(Panel1MinSize, Math.Min(total - Panel2MinSize, distance));
            }
            catch (InvalidOperationException)
            {
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (_themeSubscription == null)
            {
                _themeSubscription = new ThemeSubscription(this, ApplyTheme);
            }

            ApplyTheme();
            ApplyPendingRatio();
        }

        protected override void OnParentChanged(EventArgs e)
        {
            base.OnParentChanged(e);
            ApplyTheme();
        }

        protected virtual void ApplyTheme()
        {
            if (Panel1 != null)
            {
                Panel1.BackColor = Color.Transparent;
            }

            if (Panel2 != null)
            {
                Panel2.BackColor = Color.Transparent;
            }

            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            FluentGraphics.SetupGraphics(g);

            Rectangle splitter = SplitterRectangle;
            if (splitter.Width <= 0 || splitter.Height <= 0)
            {
                return;
            }

            Color fill = ControlHelper.BlendOverParent(this, Palette.SubtleFillSecondary);
            using (var brush = new SolidBrush(fill))
            {
                g.FillRectangle(brush, splitter);
            }

            using (var pen = new Pen(ControlHelper.BlendOverParent(this, Palette.DividerStrokeDefault), 1f))
            using (var dotBrush = new SolidBrush(ControlHelper.BlendOverParent(this, Palette.TextFillSecondary)))
            {
                if (Orientation == Orientation.Vertical)
                {
                    int x = splitter.Left + splitter.Width / 2;
                    g.DrawLine(pen, x, splitter.Top, x, splitter.Bottom);
                    int centerY = splitter.Top + splitter.Height / 2;
                    for (int i = -1; i <= 1; i++)
                    {
                        g.FillEllipse(dotBrush, x - 1, centerY + i * 4 - 1, 2, 2);
                    }
                }
                else
                {
                    int y = splitter.Top + splitter.Height / 2;
                    g.DrawLine(pen, splitter.Left, y, splitter.Right, y);
                    int centerX = splitter.Left + splitter.Width / 2;
                    for (int i = -1; i <= 1; i++)
                    {
                        g.FillEllipse(dotBrush, centerX + i * 4 - 1, y - 1, 2, 2);
                    }
                }
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
