using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using WinformUi.Drawing;
using WinformUi.Theming;

namespace WinformUi.Controls
{
    /// <summary>
    /// Fluent-painted system scrollbars. The dark scrollbar theme assets are not
    /// reachable per window on all supported builds, so scrollbar surfaces are
    /// painted with the Fluent thumb while scrolling interaction stays native.
    /// </summary>
    internal static class FluentNcScrollBar
    {
        public const int WM_NCPAINT = 0x0085;
        public const int WM_PAINT = 0x000F;
        public const int WM_ERASEBKGND = 0x0014;
        private const int SB_HORZ = 0;
        private const int SB_VERT = 1;
        private const int SB_CTL = 2;
        private const int SBS_VERT = 0x1;
        private const int GWL_STYLE = -16;
        private const int WS_VSCROLL = 0x00200000;
        private const int WS_HSCROLL = 0x00100000;
        private const uint SIF_RANGE = 0x1;
        private const uint SIF_PAGE = 0x2;
        private const uint SIF_POS = 0x4;
        private const uint RDW_INVALIDATE = 0x1;
        private const uint RDW_ERASE = 0x4;
        private const uint RDW_UPDATENOW = 0x200;
        private const uint RDW_FRAME = 0x400;

        [StructLayout(LayoutKind.Sequential)]
        private struct SCROLLINFO
        {
            public uint cbSize;
            public uint fMask;
            public int nMin;
            public int nMax;
            public uint nPage;
            public int nPos;
            public int nTrackPos;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PAINTSTRUCT
        {
            public IntPtr hdc;
            public int fErase;
            public int rcLeft;
            public int rcTop;
            public int rcRight;
            public int rcBottom;
            public int fRestore;
            public int fIncUpdate;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] rgbReserved;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("user32.dll")]
        private static extern IntPtr BeginPaint(IntPtr hWnd, ref PAINTSTRUCT ps);

        [DllImport("user32.dll")]
        private static extern bool EndPaint(IntPtr hWnd, ref PAINTSTRUCT ps);

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern bool GetScrollInfo(IntPtr hWnd, int fnBar, ref SCROLLINFO info);

        [DllImport("user32.dll")]
        private static extern bool RedrawWindow(IntPtr hWnd, IntPtr rect, IntPtr region, uint flags);

        [DllImport("user32.dll")]
        private static extern bool InvalidateRect(IntPtr hWnd, IntPtr rect, bool erase);

        [DllImport("user32.dll")]
        private static extern bool UpdateWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool GetClientRect(IntPtr hWnd, out RECT rect);

        [DllImport("gdi32.dll")]
        private static extern int SelectClipRgn(IntPtr hdc, IntPtr hrgn);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        public static void Redraw(IntPtr hwnd)
        {
            // RDW_ERASE drops the native pixels a scrollbar kept from before the
            // Fluent hook took over, so no stale thumb or arrow survives.
            RedrawWindow(hwnd, IntPtr.Zero, IntPtr.Zero, RDW_INVALIDATE | RDW_ERASE | RDW_FRAME | RDW_UPDATENOW);
        }

        /// <summary>Repaints a standalone scrollbar control window right now.</summary>
        public static void RedrawCtl(IntPtr hwnd)
        {
            // A scrollbar control moves its thumb by blitting inside
            // SetScrollInfo, so no WM_PAINT reaches the subclass and the
            // previously painted Fluent thumb survives. RedrawWindow does not
            // queue one either; only InvalidateRect plus UpdateWindow gets the
            // subclassed window proc to repaint the whole bar.
            InvalidateRect(hwnd, IntPtr.Zero, true);
            UpdateWindow(hwnd);
        }

        /// <summary>Paints non-client scrollbars; true when the caller should skip default processing.</summary>
        public static bool PaintNc(IntPtr hwnd, int width, int height, Color track)
        {
            int style = GetWindowLong(hwnd, GWL_STYLE);
            bool vertical = (style & WS_VSCROLL) != 0;
            bool horizontal = (style & WS_HSCROLL) != 0;
            if (!vertical && !horizontal)
            {
                return false;
            }

            IntPtr hdc = GetWindowDC(hwnd);
            if (hdc == IntPtr.Zero)
            {
                return false;
            }

            try
            {
                using (Graphics g = Graphics.FromHdc(hdc))
                {
                    int vw = SystemInformation.VerticalScrollBarWidth;
                    int hh = SystemInformation.HorizontalScrollBarHeight;
                    var vTrack = new Rectangle(width - vw, 0, vw, height - (horizontal ? hh : 0));
                    var hTrack = new Rectangle(0, height - hh, width - (vertical ? vw : 0), hh);

                    using (var brush = new SolidBrush(track))
                    {
                        if (vertical)
                        {
                            g.FillRectangle(brush, vTrack);
                        }

                        if (horizontal)
                        {
                            g.FillRectangle(brush, hTrack);
                        }

                        if (vertical && horizontal)
                        {
                            g.FillRectangle(brush, width - vw, height - hh, vw, hh);
                        }
                    }

                    if (vertical)
                    {
                        DrawThumb(g, hwnd, SB_VERT, vTrack, true, track);
                    }

                    if (horizontal)
                    {
                        DrawThumb(g, hwnd, SB_HORZ, hTrack, false, track);
                    }
                }
            }
            finally
            {
                ReleaseDC(hwnd, hdc);
            }

            return true;
        }

        /// <summary>Paints a standalone scrollbar control window (SB_CTL).</summary>
        public static void PaintCtl(IntPtr hwnd, Color track)
        {
            var ps = new PAINTSTRUCT();
            IntPtr hdc = BeginPaint(hwnd, ref ps);
            if (hdc == IntPtr.Zero)
            {
                return;
            }

            try
            {
                // A scroll blits the channel and invalidates only the sliver it
                // exposed, so BeginPaint's DC is clipped there and stale thumb
                // pixels survive outside it. Drop the clip and repaint the bar.
                SelectClipRgn(hdc, IntPtr.Zero);

                using (Graphics g = Graphics.FromHdc(hdc))
                {
                    RECT client;
                    GetClientRect(hwnd, out client);
                    var rect = new Rectangle(0, 0, client.Right - client.Left, client.Bottom - client.Top);
                    using (var brush = new SolidBrush(track))
                    {
                        g.FillRectangle(brush, rect);
                    }

                    bool vertical = (GetWindowLong(hwnd, GWL_STYLE) & SBS_VERT) != 0;
                    DrawThumb(g, hwnd, SB_CTL, rect, vertical, track);
                }
            }
            finally
            {
                EndPaint(hwnd, ref ps);
            }
        }

        private static void DrawThumb(Graphics g, IntPtr hwnd, int bar, Rectangle track, bool vertical, Color trackColor)
        {
            var info = new SCROLLINFO();
            info.cbSize = (uint)Marshal.SizeOf(typeof(SCROLLINFO));
            info.fMask = SIF_RANGE | SIF_PAGE | SIF_POS;
            if (!GetScrollInfo(hwnd, bar, ref info))
            {
                return;
            }

            int total = info.nMax - info.nMin + 1;
            int page = (int)info.nPage;
            if (total <= 0 || page <= 0)
            {
                return;
            }

            const int inset = 2;
            float trackLen = (vertical ? track.Height : track.Width) - inset * 2;
            float thumbLen = Math.Max(24f, trackLen * Math.Min(1f, page / (float)total));
            float travel = trackLen - thumbLen;
            int range = total - page;
            float offset = range <= 0 ? 0 : (info.nPos - info.nMin) / (float)range;
            float start = (vertical ? track.Y : track.X) + inset + travel * offset;
            float slim = Math.Max(4f, (vertical ? track.Width : track.Height) * 0.4f);

            RectangleF thumb = vertical
                ? new RectangleF(track.X + (track.Width - slim) / 2f, start, slim, thumbLen)
                : new RectangleF(start, track.Y + (track.Height - slim) / 2f, thumbLen, slim);

            FluentPalette p = ApplicationThemeManager.Palette;
            using (var brush = new SolidBrush(AccentColor.Blend(p.ControlStrongFillDefault, trackColor)))
            {
                FluentGraphics.FillRoundedRect(g, brush, thumb, (vertical ? thumb.Width : thumb.Height) / 2f);
            }
        }
    }
}