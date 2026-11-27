using System;
using System.Windows.Forms;
using WinformUi.Theming;

namespace WinformUi.Controls
{
    /// <summary>
    /// Keeps a control subscribed to <see cref="ApplicationThemeManager.ThemeChanged"/>
    /// for the lifetime of its native handle (handles can be recreated by WinForms).
    /// </summary>
    public sealed class ThemeSubscription : IDisposable
    {
        private readonly Control _control;
        private readonly Action _onChanged;
        private bool _subscribed;
        private bool _disposed;

        public ThemeSubscription(Control control, Action onChanged)
        {
            _control = control;
            _onChanged = onChanged;
            control.HandleCreated += OnHandleCreated;
            control.HandleDestroyed += OnHandleDestroyed;
            control.Disposed += OnDisposed;
            if (control.IsHandleCreated)
            {
                Subscribe();
            }
        }

        private void OnHandleCreated(object sender, EventArgs e)
        {
            Subscribe();
        }

        private void OnHandleDestroyed(object sender, EventArgs e)
        {
            Unsubscribe();
        }

        private void OnDisposed(object sender, EventArgs e)
        {
            Dispose();
        }

        private void Subscribe()
        {
            if (!_subscribed && !_disposed)
            {
                ApplicationThemeManager.ThemeChanged += OnThemeChanged;
                _subscribed = true;
            }
        }

        private void Unsubscribe()
        {
            if (_subscribed)
            {
                ApplicationThemeManager.ThemeChanged -= OnThemeChanged;
                _subscribed = false;
            }
        }

        private void OnThemeChanged(object sender, EventArgs e)
        {
            if (!_disposed && _control.IsHandleCreated)
            {
                _onChanged();
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            Unsubscribe();
            _control.HandleCreated -= OnHandleCreated;
            _control.HandleDestroyed -= OnHandleDestroyed;
            _control.Disposed -= OnDisposed;
        }
    }
}