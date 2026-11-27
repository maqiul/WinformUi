using System;
using System.Runtime.InteropServices;

namespace WinformUi.Interop
{
    internal static class NativeMethods
    {
        public const int WM_NCLBUTTONDOWN = 0x00A1;
        public const int WM_NCLBUTTONUP = 0x00A2;
        public const int WM_NCLBUTTONDBLCLK = 0x00A3;
        public const int WM_NCRBUTTONUP = 0x00A5;
        public const int WM_SYSCOMMAND = 0x0112;
        public const int SC_MOVE = 0xF010;
        public const int SC_SIZE = 0xF120;
        public const int WM_NCMOUSEMOVE = 0x02A0;
        public const int WM_NCMOUSELEAVE = 0x02A2;
        public const int WM_SIZE = 0x0005;
        public const int SIZE_MINIMIZED = 1;
        public const int HTCAPTION = 0x0002;
        public const int WM_NCHITTEST = 0x0084;
        public const int WM_SETTINGCHANGE = 0x001A;
        public const int WM_DWMCOLORIZATIONCOLORCHANGED = 0x0320;
        public const int EM_SETCUEBANNER = 0x1501;

        public const int HTCLIENT = 1;
        public const int HTLEFT = 10;
        public const int HTRIGHT = 11;
        public const int HTTOP = 12;
        public const int HTTOPLEFT = 13;
        public const int HTTOPRIGHT = 14;
        public const int HTBOTTOM = 15;
        public const int HTBOTTOMLEFT = 16;
        public const int HTBOTTOMRIGHT = 17;

        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, string lParam);

        [DllImport("user32.dll")]
        public static extern bool SetProcessDPIAware();

        [DllImport("user32.dll")]
        public static extern int GetDpiForWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll")]
        public static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

        public const uint GW_CHILD = 5;
        public const uint GW_HWNDNEXT = 2;

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);

        [DllImport("ntdll.dll")]
        private static extern int RtlGetVersion(ref OSVERSIONINFOEX versionInfo);

        [StructLayout(LayoutKind.Sequential)]
        private struct OSVERSIONINFOEX
        {
            public int dwOSVersionInfoSize;
            public int dwMajorVersion;
            public int dwMinorVersion;
            public int dwBuildNumber;
            public int dwPlatformId;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szCSDVersion;
            public short wServicePackMajor;
            public short wServicePackMinor;
            public short wSuiteMask;
            public byte wProductType;
            public byte wReserved;
        }

        private static Version _osVersion;

        /// <summary>Real OS version (Environment.OSVersion lies without a compatibility manifest on .NET Framework).</summary>
        public static Version OsVersion
        {
            get
            {
                if (_osVersion == null)
                {
                    var info = new OSVERSIONINFOEX();
                    info.dwOSVersionInfoSize = Marshal.SizeOf(typeof(OSVERSIONINFOEX));
                    if (RtlGetVersion(ref info) == 0)
                    {
                        _osVersion = new Version(info.dwMajorVersion, info.dwMinorVersion, info.dwBuildNumber);
                    }
                    else
                    {
                        _osVersion = Environment.OSVersion.Version;
                    }
                }

                return _osVersion;
            }
        }

        public static bool IsWindows11OrGreater => OsVersion.Major >= 10 && OsVersion.Build >= 22000;

        /// <summary>System backdrop (Mica/Acrylic) requires Windows 11 22H2 (build 22621).</summary>
        public static bool SupportsSystemBackdrop => OsVersion.Major >= 10 && OsVersion.Build >= 22621;
    }

    internal static class DwmApi
    {
        public const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        public const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        public const int DWMWA_SYSTEMBACKDROP_TYPE = 38;
        public const int DWMWA_MICA_EFFECT = 1029; // undocumented, Win11 21H2 fallback

        public const int DWMWCP_ROUND = 2;

        // Values must match the SDK's DWM_SYSTEMBACKDROP_TYPE enum exactly;
        // an off-by-one here silently enables AUTO (Mica) for "None".
        public const int DWMSBT_AUTO = 1;
        public const int DWMSBT_NONE = 2;
        public const int DWMSBT_MAINWINDOW = 3;   // Mica
        public const int DWMSBT_TRANSIENTWINDOW = 4; // Acrylic
        public const int DWMSBT_TABBEDWINDOW = 5;  // Mica Alt

        [StructLayout(LayoutKind.Sequential)]
        public struct MARGINS
        {
            public int cxLeftWidth;
            public int cxRightWidth;
            public int cyTopHeight;
            public int cyBottomHeight;
        }

        [DllImport("dwmapi.dll")]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

        [DllImport("dwmapi.dll")]
        public static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref MARGINS margins);

        public static void SetDarkMode(IntPtr hwnd, bool dark)
        {
            int value = dark ? 1 : 0;
            DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref value, sizeof(int));
        }

        public static void SetCornerPreference(IntPtr hwnd, int preference)
        {
            DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref preference, sizeof(int));
        }

        /// <summary>Returns true when a real system backdrop was applied.</summary>
        public static bool TrySetBackdrop(IntPtr hwnd, Theming.WindowBackdropType type)
        {
            if (type == Theming.WindowBackdropType.None || !NativeMethods.SupportsSystemBackdrop)
            {
                int none = DWMSBT_NONE;
                if (NativeMethods.SupportsSystemBackdrop)
                {
                    DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE, ref none, sizeof(int));
                }

                return false;
            }

            int backdrop;
            switch (type)
            {
                case Theming.WindowBackdropType.Acrylic:
                    backdrop = DWMSBT_TRANSIENTWINDOW;
                    break;
                case Theming.WindowBackdropType.Tabbed:
                    backdrop = DWMSBT_TABBEDWINDOW;
                    break;
                default:
                    backdrop = DWMSBT_MAINWINDOW;
                    break;
            }

            int result = DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE, ref backdrop, sizeof(int));
            if (result != 0)
            {
                return false;
            }

            var margins = new MARGINS { cxLeftWidth = -1, cxRightWidth = -1, cyTopHeight = -1, cyBottomHeight = -1 };
            DwmExtendFrameIntoClientArea(hwnd, ref margins);
            return true;
        }
    }
}