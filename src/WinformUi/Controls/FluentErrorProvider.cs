using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using WinformUi.Theming;

namespace WinformUi.Controls
{
    /// <summary>显示控件的验证错误，并在鼠标悬停时显示消息。</summary>
    [ToolboxItem(true)]
    public class FluentErrorProvider : Component
    {
        private sealed class ErrorEntry
        {
            public Control Control;
            public string Message;
            public PaintEventHandler PaintHandler;
            public EventHandler DisposedHandler;
        }

        private readonly Dictionary<Control, ErrorEntry> _entries = new Dictionary<Control, ErrorEntry>();
        private readonly Timer _timer;
        private readonly FluentToolTip _toolTip;
        private Image _icon;
        private int _iconPadding = 2;
        private ErrorBlinkStyle _blinkStyle = ErrorBlinkStyle.BlinkIfDifferentError;
        private int _blinkTicks;
        private bool _iconVisible = true;
        private bool _disposed;

        public FluentErrorProvider()
        {
            _icon = SystemIcons.Error.ToBitmap();
            _toolTip = new FluentToolTip { ShowAlways = true };
            _timer = new Timer { Interval = 500, Enabled = false };
            _timer.Tick += OnTimerTick;
            ApplicationThemeManager.ThemeChanged += OnThemeChanged;
        }

        public event EventHandler<ErrorChangedEventArgs> ErrorChanged;

        [Category("Appearance")]
        [DefaultValue(null)]
        public Image Icon
        {
            get { return _icon; }
            set
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(nameof(FluentErrorProvider));
                }

                if (ReferenceEquals(_icon, value))
                {
                    return;
                }

                _icon = value;
                InvalidateErrorControls();
            }
        }

        [DefaultValue(2)]
        [Category("Fluent")]
        public int IconPadding
        {
            get { return _iconPadding; }
            set
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(nameof(FluentErrorProvider));
                }

                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                if (_iconPadding != value)
                {
                    _iconPadding = value;
                    InvalidateErrorControls();
                }
            }
        }

        [DefaultValue(System.Windows.Forms.ErrorBlinkStyle.BlinkIfDifferentError)]
        [Category("Fluent")]
        public System.Windows.Forms.ErrorBlinkStyle BlinkStyle
        {
            get { return _blinkStyle; }
            set
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(nameof(FluentErrorProvider));
                }

                if (_blinkStyle != value)
                {
                    _blinkStyle = value;
                    _iconVisible = true;
                    _blinkTicks = 0;
                    UpdateBlinking();
                    InvalidateErrorControls();
                }
            }
        }

        public void SetError(Control control, string message)
        {
            if (control == null)
            {
                throw new ArgumentNullException(nameof(control));
            }

            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(FluentErrorProvider));
            }

            if (control.IsDisposed || control.Disposing)
            {
                return;
            }

            bool clear = string.IsNullOrEmpty(message);
            string previous = GetError(control);
            if (clear && previous == null)
            {
                return;
            }

            if (!clear && previous == message)
            {
                return;
            }

            if (clear)
            {
                _blinkTicks = 0;
                RemoveEntry(control);
            }
            else
            {
                AddEntry(control, message, previous);
            }

            UpdateBlinking();
            OnErrorChanged(control);
        }

        public string GetError(Control control)
        {
            if (control == null)
            {
                throw new ArgumentNullException(nameof(control));
            }

            ErrorEntry entry;
            return _entries.TryGetValue(control, out entry) ? entry.Message : null;
        }

        public void ClearError(Control control)
        {
            SetError(control, null);
        }

        public void ClearErrors()
        {
            if (_disposed)
            {
                return;
            }

            var controls = _entries.Keys.ToArray();
            foreach (Control control in controls)
            {
                RemoveEntry(control);
                OnErrorChanged(control);
            }

            if (controls.Length > 0)
            {
                UpdateBlinking();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _disposed = true;
                ApplicationThemeManager.ThemeChanged -= OnThemeChanged;

                _timer.Stop();
                _timer.Tick -= OnTimerTick;
                _timer.Dispose();

                RemoveAllEntries();
                ErrorChanged = null;
                _toolTip.Dispose();
                _icon = null;
            }

            base.Dispose(disposing);
        }

        private void AddEntry(Control control, string message, string previous)
        {
            ErrorEntry entry;
            if (!_entries.TryGetValue(control, out entry))
            {
                entry = new ErrorEntry
                {
                    Control = control,
                    PaintHandler = OnControlPaint,
                    DisposedHandler = OnControlDisposed
                };
                _entries.Add(control, entry);

                if (!IsDesignMode(control))
                {
                    control.Paint += entry.PaintHandler;
                    control.Disposed += entry.DisposedHandler;
                }
            }

            entry.Message = message;
            _toolTip.SetToolTip(control, message);
            bool blink = _blinkStyle == ErrorBlinkStyle.AlwaysBlink
                || (_blinkStyle == ErrorBlinkStyle.BlinkIfDifferentError && previous != null && previous != message);

            if (blink)
            {
                if (_blinkStyle != ErrorBlinkStyle.AlwaysBlink)
                {
                    _blinkTicks = 4;
                }
                _iconVisible = false;
            }
            else
            {
                _iconVisible = true;
            }

            InvalidateControl(control);
        }

        private void RemoveEntry(Control control)
        {
            ErrorEntry entry;
            if (!_entries.TryGetValue(control, out entry))
            {
                return;
            }

            _entries.Remove(control);
            if (!IsDesignMode(control) && !control.IsDisposed)
            {
                control.Paint -= entry.PaintHandler;
                control.Disposed -= entry.DisposedHandler;
            }

            _toolTip.SetToolTip(control, null);
            InvalidateControl(control);
        }

        private void RemoveAllEntries()
        {
            var entries = _entries.Values.ToArray();
            _entries.Clear();
            foreach (ErrorEntry entry in entries)
            {
                Control control = entry.Control;
                if (!IsDesignMode(control) && !control.IsDisposed)
                {
                    control.Paint -= entry.PaintHandler;
                    control.Disposed -= entry.DisposedHandler;
                }

                _toolTip.SetToolTip(control, null);
                InvalidateControl(control);
            }
        }

        private void OnControlDisposed(object sender, EventArgs e)
        {
            Control control = (Control)sender;
            ErrorEntry entry;
            if (_entries.TryGetValue(control, out entry))
            {
                RemoveEntry(control);
                UpdateBlinking();
            }
        }

        private void OnControlPaint(object sender, PaintEventArgs e)
        {
            ErrorEntry entry;
            if (_disposed || IsDesignMode(sender as Control) || !_entries.TryGetValue((Control)sender, out entry))
            {
                return;
            }

            Control control = entry.Control;
            Rectangle bounds = control.ClientRectangle;
            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                return;
            }

            using (var pen = new Pen(ApplicationThemeManager.Palette.SystemFillCritical))
            {
                e.Graphics.DrawRectangle(pen, new Rectangle(bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1));
            }

            Image icon = _icon;
            if (icon == null || !_iconVisible)
            {
                return;
            }

            int width = icon.Width;
            int height = Math.Min(icon.Height, bounds.Height);
            int x = Math.Max(0, bounds.Right - _iconPadding - width);
            int y = Math.Max(0, bounds.Top + _iconPadding);
            e.Graphics.DrawImage(icon, new Rectangle(x, y, width, height), 0, 0, icon.Width, icon.Height, GraphicsUnit.Pixel);
        }

        private void OnTimerTick(object sender, EventArgs e)
        {
            if (_disposed || _blinkStyle == ErrorBlinkStyle.NeverBlink || _entries.Count == 0)
            {
                return;
            }

            _iconVisible = !_iconVisible;
            if (_blinkStyle == ErrorBlinkStyle.BlinkIfDifferentError && _blinkTicks > 0 && --_blinkTicks == 0)
            {
                _iconVisible = true;
            }

            InvalidateErrorControls();
            UpdateBlinking();
        }

        private void OnThemeChanged(object sender, EventArgs e)
        {
            if (!_disposed)
            {
                InvalidateErrorControls();
            }
        }

        private void UpdateBlinking()
        {
            if (_disposed)
            {
                return;
            }

            bool enabled = _entries.Count > 0
                && (_blinkStyle == ErrorBlinkStyle.AlwaysBlink
                    || (_blinkStyle == ErrorBlinkStyle.BlinkIfDifferentError && _blinkTicks > 0));
            _timer.Enabled = enabled;
        }

        private void InvalidateErrorControls()
        {
            if (_disposed)
            {
                return;
            }

            foreach (Control control in _entries.Keys)
            {
                InvalidateControl(control);
            }
        }

        private static void InvalidateControl(Control control)
        {
            if (!control.IsDisposed && !control.Disposing)
            {
                control.Invalidate();
            }
        }

        private static bool IsDesignMode(Control control)
        {
            return control != null && control.Site != null && control.Site.DesignMode;
        }

        private void OnErrorChanged(Control control)
        {
            ErrorChanged?.Invoke(this, new ErrorChangedEventArgs(control));
        }
    }

    public sealed class ErrorChangedEventArgs : EventArgs
    {
        public ErrorChangedEventArgs(Control control)
        {
            Control = control;
        }

        public Control Control { get; private set; }
    }
}
