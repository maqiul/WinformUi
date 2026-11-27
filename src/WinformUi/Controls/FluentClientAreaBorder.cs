using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using WinformUi.Drawing;
using WinformUi.Theming;

namespace WinformUi.Controls
{
    /// <summary>
    /// Content panel for custom-chrome windows: pads its children by the window
    /// resize-frame thickness when the host form is maximized (borderless windows
    /// otherwise push content off-screen), and paints a hairline border on OS
    /// versions that lack the modern automatic window border.
    /// </summary>
    [ToolboxItem(true)]
    public class FluentClientAreaBorder : Panel
    {
        private Form _trackedForm;
        private ThemeSubscription _themeSubscription;
        private static readonly bool SupportsModernBorder = Environment.OSVersion.Version.Major >= 10;

        public FluentClientAreaBorder()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (_themeSubscription == null)
            {
                _themeSubscription = new ThemeSubscription(this, Invalidate);
            }

            HookForm();
            UpdatePadding();
        }

        protected override void OnParentChanged(EventArgs e)
        {
            base.OnParentChanged(e);
            HookForm();
            UpdatePadding();
        }

        private void HookForm()
        {
            if (ReferenceEquals(_trackedForm, FindForm()))
            {
                return;
            }

            if (_trackedForm != null)
            {
                _trackedForm.LocationChanged -= OnFormStateChanged;
                _trackedForm.Resize -= OnFormStateChanged;
            }

            _trackedForm = FindForm();
            if (_trackedForm != null)
            {
                _trackedForm.LocationChanged += OnFormStateChanged;
                _trackedForm.Resize += OnFormStateChanged;
            }
        }

        private void OnFormStateChanged(object sender, EventArgs e)
        {
            UpdatePadding();
            Invalidate();
        }

        private void UpdatePadding()
        {
            int pad = 0;
            Form form = _trackedForm;
            if (form != null &&
                form.FormBorderStyle == FormBorderStyle.None &&
                form.WindowState == FormWindowState.Maximized)
            {
                Size frame = SystemInformation.FrameBorderSize;
                pad = Math.Max(frame.Width, frame.Height);
            }

            if (Padding.All != pad)
            {
                Padding = new Padding(pad);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (SupportsModernBorder)
            {
                return;
            }

            Color borderColor = ApplicationThemeManager.EffectiveTheme == ApplicationTheme.Dark
                ? Color.FromArgb(255, 0x3A, 0x3A, 0x3A)
                : Color.FromArgb(255, 0x7A, 0x7A, 0x7A);
            using (var pen = new Pen(borderColor, 1f))
            {
                e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_trackedForm != null)
                {
                    _trackedForm.LocationChanged -= OnFormStateChanged;
                    _trackedForm.Resize -= OnFormStateChanged;
                    _trackedForm = null;
                }

                _themeSubscription?.Dispose();
                _themeSubscription = null;
            }

            base.Dispose(disposing);
        }
    }
}
