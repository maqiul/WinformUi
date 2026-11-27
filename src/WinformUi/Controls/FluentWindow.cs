using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using WinformUi.Interop;
using WinformUi.Theming;

namespace WinformUi.Controls
{
    /// <summary>
    /// Fluent-styled top level window. Keeps the native caption
    /// and applies a themed title bar over a solid Fluent background.
    /// Windows 11 Mica/Acrylic system backdrop is opt-in via <see cref="BackdropType"/>:
    /// the full-client glass pass double-composites GDI-rendered (zero-alpha) pixels,
    /// which ghosts text on some Windows 11 builds.
    /// </summary>
    public class FluentWindow : Form
    {
        private WindowBackdropType _backdropType = WindowBackdropType.None;
        private bool _backdropApplied;
        private bool _wasMinimized;
        private ThemeSubscription _themeSubscription;

        public FluentWindow()
        {
            Font = FluentTypography.Body;
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = ApplicationThemeManager.Palette.ApplicationBackground;
        }

        /// <summary>System backdrop effect. Mica/Acrylic/Tabbed need Windows 11 22H2+; opt-in because GDI content can ghost under the glass pass.</summary>
        [DefaultValue(WindowBackdropType.None)]
        [Category("Fluent")]
        public WindowBackdropType BackdropType
        {
            get { return _backdropType; }
            set
            {
                if (_backdropType != value)
                {
                    _backdropType = value;
                    if (IsHandleCreated)
                    {
                        ApplyBackdrop();
                        Invalidate(true);
                    }
                }
            }
        }

        /// <summary>True when a real system backdrop is active (false on unsupported systems).</summary>
        [Browsable(false)]
        public bool IsBackdropApplied => _backdropApplied;

        protected FluentPalette Palette => ApplicationThemeManager.Palette;

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (_themeSubscription == null)
            {
                _themeSubscription = new ThemeSubscription(this, OnThemeChanged);
            }

            ApplyBackdrop();
            ApplyTitleBarTheme();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            // Backdrop attributes sometimes need a second pass once the window is visible.
            ApplyBackdrop();
        }

        private void OnThemeChanged()
        {
            ApplyTitleBarTheme();
            if (!_backdropApplied)
            {
                // Children inherit BackColor ambiently; a stale light value
                // paints white boxes under dark-theme translucent fills.
                BackColor = Palette.ApplicationBackground;
            }
            Invalidate(true);
        }

        private void ApplyTitleBarTheme()
        {
            if (!IsHandleCreated)
            {
                return;
            }

            DwmApi.SetDarkMode(Handle, ApplicationThemeManager.EffectiveTheme == ApplicationTheme.Dark);
            if (NativeMethods.IsWindows11OrGreater)
            {
                DwmApi.SetCornerPreference(Handle, DwmApi.DWMWCP_ROUND);
            }
        }

        private void ApplyBackdrop()
        {
            if (!IsHandleCreated)
            {
                return;
            }

            _backdropApplied = DwmApi.TrySetBackdrop(Handle, _backdropType);
            if (!_backdropApplied)
            {
                BackColor = Palette.ApplicationBackground;
            }
            else
            {
                BackColor = Color.Black;
            }
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            if (_backdropApplied)
            {
                // Black on the extended DWM frame is composed as transparent,
                // letting the system backdrop show through.
                e.Graphics.Clear(Color.Black);
            }
            else
            {
                using (var brush = new SolidBrush(Palette.ApplicationBackground))
                {
                    e.Graphics.FillRectangle(brush, ClientRectangle);
                }
            }
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (m.Msg == NativeMethods.WM_SIZE)
            {
                if (m.WParam == (IntPtr)NativeMethods.SIZE_MINIMIZED)
                {
                    _wasMinimized = true;
                }
                else if (_wasMinimized)
                {
                    // DWM drops the redirection surface of backdrop-class windows
                    // while minimized and restore does not auto-invalidate it.
                    _wasMinimized = false;
                    Invalidate(true);
                }
            }
            else if (m.Msg == NativeMethods.WM_SETTINGCHANGE || m.Msg == NativeMethods.WM_DWMCOLORIZATIONCOLORCHANGED)
            {
                ApplicationThemeManager.RefreshSystemColors();
                ApplyTitleBarTheme();
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