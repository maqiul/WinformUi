using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace WinformUi.Controls
{
    /// <summary>
    /// Fluent notification-area (system tray) icon component: tooltip, icon,
    /// themed context menu, balloon tips and per-button click events, mirroring
    /// the reference NotifyIcon API on top of the Win32 shell tray.
    /// </summary>
    [DefaultEvent("LeftClick")]
    public class FluentNotifyIcon : Component
    {
        private readonly NotifyIcon _notify = new NotifyIcon();
        private FluentContextMenuStrip _menu;
        private bool _focusOnLeftClick = true;
        private bool _menuOnRightClick = true;
        private string _tooltipText = string.Empty;
        private TimeSpan _balloonTimeout = TimeSpan.FromSeconds(3);

        public FluentNotifyIcon()
        {
            _notify.MouseClick += OnNotifyMouseClick;
            _notify.MouseDoubleClick += OnNotifyMouseDoubleClick;
            _notify.BalloonTipShown += (s, e) => BalloonTipShown?.Invoke(this, e);
            _notify.BalloonTipClosed += (s, e) => BalloonTipClosed?.Invoke(this, e);
            _notify.BalloonTipClicked += (s, e) => BalloonTipClicked?.Invoke(this, e);
        }

        /// <summary>Tooltip shown when the pointer rests over the tray icon.</summary>
        [Category("Behavior")]
        [DefaultValue("")]
        [Localizable(true)]
        public string TooltipText
        {
            get { return _tooltipText; }
            set
            {
                _tooltipText = value ?? string.Empty;
                _notify.Text = Truncate(_tooltipText, 63);
            }
        }

        /// <summary>The tray icon. Use the application icon or a custom .ico.</summary>
        [Category("Behavior")]
        [DefaultValue(null)]
        public Icon Icon
        {
            get { return _notify.Icon; }
            set { _notify.Icon = value; }
        }

        /// <summary>Convenience: builds an Icon from a bitmap and assigns it.</summary>
        public void SetIcon(Bitmap bitmap)
        {
            Icon previous = _notify.Icon;
            _notify.Icon = bitmap == null ? null : Icon.FromHandle(bitmap.GetHicon());
            if (previous != null && !ReferenceEquals(previous, SystemIcons.Application))
            {
                previous.Dispose();
            }
        }

        /// <summary>
        /// Menu shown on right-click. FluentContextMenuStrip keeps the themed look;
        /// any ContextMenuStrip works too.
        /// </summary>
        [Category("Behavior")]
        [DefaultValue(null)]
        public FluentContextMenuStrip Menu
        {
            get { return _menu; }
            set
            {
                _menu = value;
                _notify.ContextMenuStrip = _menuOnRightClick ? _menu : null;
            }
        }

        [Category("Behavior")]
        [DefaultValue(true)]
        [Description("Left-click restores and activates the application's first open form.")]
        public bool FocusOnLeftClick
        {
            get { return _focusOnLeftClick; }
            set { _focusOnLeftClick = value; }
        }

        [Category("Behavior")]
        [DefaultValue(true)]
        public bool MenuOnRightClick
        {
            get { return _menuOnRightClick; }
            set
            {
                _menuOnRightClick = value;
                _notify.ContextMenuStrip = value ? _menu : null;
            }
        }

        [Category("Behavior")]
        [DefaultValue(typeof(TimeSpan), "00:00:03")]
        public TimeSpan BalloonTimeout
        {
            get { return _balloonTimeout; }
            set { _balloonTimeout = value; }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IsRegistered => _notify.Visible;

        /// <summary>Adds the icon to the tray.</summary>
        public void Register()
        {
            _notify.Visible = true;
        }

        /// <summary>Removes the icon from the tray.</summary>
        public void Unregister()
        {
            _notify.Visible = false;
        }

        [Category("Behavior")]
        [DefaultValue(false)]
        public bool Visible
        {
            get { return _notify.Visible; }
            set { _notify.Visible = value; }
        }

        public void ShowBalloonTip(TimeSpan timeout)
        {
            _notify.ShowBalloonTip((int)timeout.TotalMilliseconds);
        }

        public void ShowBalloonTip(TimeSpan timeout, string title, string message, ToolTipIcon icon = ToolTipIcon.Info)
        {
            _notify.BalloonTipTitle = title ?? string.Empty;
            _notify.BalloonTipText = message ?? string.Empty;
            _notify.BalloonTipIcon = icon;
            _notify.ShowBalloonTip((int)timeout.TotalMilliseconds);
        }

        public event RoutedNotifyIconEventHandler LeftClick;
        public event RoutedNotifyIconEventHandler LeftDoubleClick;
        public event RoutedNotifyIconEventHandler RightClick;
        public event RoutedNotifyIconEventHandler RightDoubleClick;
        public event RoutedNotifyIconEventHandler MiddleClick;
        public event RoutedNotifyIconEventHandler MiddleDoubleClick;
        public event EventHandler BalloonTipShown;
        public event EventHandler BalloonTipClosed;
        public event EventHandler BalloonTipClicked;

        private void OnNotifyMouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                LeftClick?.Invoke(this, EventArgs.Empty);
                if (_focusOnLeftClick)
                {
                    FocusMainWindow();
                }
            }
            else if (e.Button == MouseButtons.Right)
            {
                RightClick?.Invoke(this, EventArgs.Empty);
            }
            else if (e.Button == MouseButtons.Middle)
            {
                MiddleClick?.Invoke(this, EventArgs.Empty);
            }
        }

        private void OnNotifyMouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                LeftDoubleClick?.Invoke(this, EventArgs.Empty);
            }
            else if (e.Button == MouseButtons.Right)
            {
                RightDoubleClick?.Invoke(this, EventArgs.Empty);
            }
            else if (e.Button == MouseButtons.Middle)
            {
                MiddleDoubleClick?.Invoke(this, EventArgs.Empty);
            }
        }

        private void FocusMainWindow()
        {
            foreach (Form form in Application.OpenForms)
            {
                if (!form.IsHandleCreated || form is FluentToastHost)
                {
                    continue;
                }

                if (form.WindowState == FormWindowState.Minimized)
                {
                    form.WindowState = FormWindowState.Normal;
                }

                form.Activate();
                return;
            }
        }

        private static string Truncate(string value, int max)
        {
            return value != null && value.Length > max ? value.Substring(0, max) : value;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _notify.Visible = false;
                _notify.Dispose();
            }

            base.Dispose(disposing);
        }
    }

    /// <summary>Delegate for tray icon click events, matching the reference routed style.</summary>
    public delegate void RoutedNotifyIconEventHandler(object sender, EventArgs e);
}
