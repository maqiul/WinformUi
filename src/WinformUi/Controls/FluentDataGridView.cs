using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using WinformUi.Drawing;
using WinformUi.Interop;
using WinformUi.Theming;

namespace WinformUi.Controls
{
    /// <summary>
    /// Fluent-themed DataGridView: palette-driven header, row,
    /// grid line and selection colors.
    /// </summary>
    public class FluentDataGridView : DataGridView
    {
        private ThemeSubscription _themeSubscription;
        private readonly System.Collections.Generic.List<ScrollBarSubclass> _scrollBarHooks =
            new System.Collections.Generic.List<ScrollBarSubclass>();

        public FluentDataGridView()
        {
            // The live window DC keeps destination alpha under Mica, so GDI+
            // fills and GDI text would composite as backdrop holes; a buffered
            // DIB carries real alpha and lets RepairTextAlpha fix glyph pixels.
            DoubleBuffered = true;
            BorderStyle = BorderStyle.None;
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            RowHeadersVisible = false;
            EnableHeadersVisualStyles = false;
            AllowUserToAddRows = false;
            AllowUserToResizeRows = false;
            SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            MultiSelect = false;
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            Font = FluentTypography.Body;
            ColumnHeadersHeight = 36;
            RowTemplate.Height = 32;
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            // Scrolling moves the native thumb by blitting, which never repaints
            // the child window through our subclass, so repaint after every scroll.
            Scroll += (s, e) => RepaintScrollBars();
        }

        private void RepaintScrollBars()
        {
            foreach (ScrollBarSubclass hook in _scrollBarHooks)
            {
                FluentNcScrollBar.RedrawCtl(hook.Handle);
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
            HookScrollBars();
        }

        private void ApplyTheme()
        {
            FluentPalette p = ApplicationThemeManager.Palette;
            Color background = p.ApplicationBackground;
            Color surface = AccentColor.Blend(p.SubtleFillSecondary, background);

            BackgroundColor = background;
            BackColor = background;
            GridColor = AccentColor.Blend(p.DividerStrokeDefault, background);

            DefaultCellStyle.BackColor = background;
            DefaultCellStyle.ForeColor = p.TextFillPrimary;
            DefaultCellStyle.SelectionBackColor = surface;
            DefaultCellStyle.SelectionForeColor = p.TextFillPrimary;
            DefaultCellStyle.Padding = new Padding(8, 0, 8, 0);

            AlternatingRowsDefaultCellStyle.BackColor = AccentColor.Blend(p.SubtleFillTertiary, background);
            AlternatingRowsDefaultCellStyle.SelectionBackColor = surface;

            ColumnHeadersDefaultCellStyle.BackColor = p.SolidBackgroundFillSecondary;
            ColumnHeadersDefaultCellStyle.ForeColor = p.TextFillSecondary;
            ColumnHeadersDefaultCellStyle.SelectionBackColor = p.SolidBackgroundFillSecondary;
            ColumnHeadersDefaultCellStyle.SelectionForeColor = p.TextFillSecondary;
            ColumnHeadersDefaultCellStyle.Font = FluentTypography.Caption;
            ColumnHeadersDefaultCellStyle.Padding = new Padding(8, 0, 8, 0);

            if (IsHandleCreated)
            {
                RepaintScrollBars();
            }

            Invalidate();
        }

        protected override void OnCellPainting(DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < -1 || e.ColumnIndex < 0)
            {
                base.OnCellPainting(e);
                return;
            }

            // GDI+ text: native DataGridView text is GDI, whose alpha-0 pixels
            // would show the DWM system backdrop through the glyphs.
            e.PaintBackground(e.CellBounds, true);
            using (var pen = new Pen(GridColor, 1f))
            {
                e.Graphics.DrawLine(pen, e.CellBounds.Left, e.CellBounds.Bottom - 1, e.CellBounds.Right, e.CellBounds.Bottom - 1);
            }

            bool selected = (e.State & DataGridViewElementStates.Selected) != 0;
            Color fore = selected ? e.CellStyle.SelectionForeColor : e.CellStyle.ForeColor;
            var rect = e.CellBounds;
            rect.Inflate(-8, 0);
            using (var format = new StringFormat { LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap })
            {
                FluentGraphics.DrawText(e.Graphics, e.FormattedValue?.ToString() ?? string.Empty, e.CellStyle.Font, fore, rect, format);
            }

            e.Handled = true;
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            // WM_PARENTNOTIFY(WM_CREATE): a scrollbar child may appear lazily
            // when the content first overflows, after the handle-created pass.
            // WM_SIZE/WM_PAINT cover children created without a notification
            // reaching us; the hook itself is idempotent and cheap.
            if (m.Msg == 0x0210 || m.Msg == 0x0005 || m.Msg == 0x000F)
            {
                HookScrollBars();
            }
        }

        private void HookScrollBars()
        {
            if (!IsHandleCreated)
            {
                return;
            }

            for (IntPtr child = NativeMethods.GetWindow(Handle, NativeMethods.GW_CHILD);
                child != IntPtr.Zero;
                child = NativeMethods.GetWindow(child, NativeMethods.GW_HWNDNEXT))
            {
                var name = new System.Text.StringBuilder(64);
                NativeMethods.GetClassName(child, name, name.Capacity);
                if (name.ToString().IndexOf("ScrollBar", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                bool hooked = false;
                foreach (ScrollBarSubclass hook in _scrollBarHooks)
                {
                    if (hook.Handle == child)
                    {
                        hooked = true;
                        break;
                    }
                }

                if (!hooked)
                {
                    var subclass = new ScrollBarSubclass(this);
                    subclass.AssignHandle(child);
                    _scrollBarHooks.Add(subclass);
                    FluentNcScrollBar.RedrawCtl(child);
                }
            }
        }

        private sealed class ScrollBarSubclass : NativeWindow
        {
            private readonly FluentDataGridView _owner;

            public ScrollBarSubclass(FluentDataGridView owner)
            {
                _owner = owner;
            }

            protected override void WndProc(ref Message m)
            {
                if (m.Msg == FluentNcScrollBar.WM_ERASEBKGND)
                {
                    m.Result = (IntPtr)1;
                    return;
                }

                if (m.Msg == FluentNcScrollBar.WM_PAINT)
                {
                    FluentNcScrollBar.PaintCtl(Handle, _owner.BackgroundColor);
                    return;
                }

                base.WndProc(ref m);
                // The native thumb-tracking loop draws outside WM_PAINT;
                // repaint over it once the mouse/capture message completes.
                if ((m.Msg >= 0x0200 && m.Msg <= 0x020E) || m.Msg == 0x0215)
                {
                    FluentNcScrollBar.RedrawCtl(Handle);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (ScrollBarSubclass hook in _scrollBarHooks)
                {
                    hook.ReleaseHandle();
                }

                _scrollBarHooks.Clear();
                _themeSubscription?.Dispose();
                _themeSubscription = null;
            }

            base.Dispose(disposing);
        }
    }
}