using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using WinformUi.Drawing;
using WinformUi.Theming;

namespace WinformUi.Controls
{
    /// <summary>
    /// In-app Fluent toast notification: a borderless card that slides in at the
    /// top-right of the primary screen, stacks with other toasts, pauses its timeout
    /// while hovered and fades itself out afterwards.
    /// </summary>
    public sealed class FluentToast
    {
        public FluentToast()
        {
            Duration = TimeSpan.FromSeconds(4);
            Severity = InfoBarSeverity.Informational;
        }

        public string Title { get; set; }

        public string Message { get; set; }

        public InfoBarSeverity Severity { get; set; }

        /// <summary>Visible time before auto-dismiss; pauses while the pointer is over the toast.</summary>
        public TimeSpan Duration { get; set; }

        /// <summary>Shows the toast and returns the hosting window for advanced handling.</summary>
        public FluentToastHost Show()
        {
            var host = new FluentToastHost(this);
            FluentToastManager.Show(host);
            return host;
        }

        public static FluentToastHost Show(string title, string message,
            InfoBarSeverity severity = InfoBarSeverity.Informational, TimeSpan? duration = null)
        {
            var toast = new FluentToast
            {
                Title = title,
                Message = message,
                Severity = severity
            };
            if (duration.HasValue)
            {
                toast.Duration = duration.Value;
            }

            return toast.Show();
        }
    }

    /// <summary>Borderless window hosting one toast card; managed by <see cref="FluentToastManager"/>.</summary>
    public sealed class FluentToastHost : Form
    {
        private readonly FluentToast _toast;
        private Timer _autoClose;
        private Timer _fade;
        private bool _fadingOut;
        private bool _closeHovered;
        private bool _hovered;
        private ThemeSubscription _themeSubscription;

        internal FluentToastHost(FluentToast toast)
        {
            _toast = toast;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            TopMost = true;
            MinimizeBox = false;
            Opacity = 0;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
        }

        protected override bool ShowWithoutActivation => true;

        internal bool IsHovered => _hovered;

        private float S => ControlHelper.DpiOf(this) / 96f;

        private RectangleF CloseRect => new RectangleF(Width - 32 * S, (Height - 22 * S) / 2f, 22 * S, 22 * S);

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            _themeSubscription = new ThemeSubscription(this, () => Invalidate());
            Font = FluentTypography.Body;
            bool hasBoth = !string.IsNullOrEmpty(_toast.Title) && !string.IsNullOrEmpty(_toast.Message);
            int width = (int)(340 * S);
            int height = (int)((hasBoth ? 74 : 50) * S);
            ClientSize = new Size(width, height);
            using (var path = RoundedRegion(width, height))
            {
                Region = new Region(path);
            }

            _autoClose = new Timer { Interval = Math.Max(500, (int)_toast.Duration.TotalMilliseconds) };
            _autoClose.Tick += (s, ev) => BeginDismiss();
            _autoClose.Start();
            _fade = new Timer { Interval = 30 };
            _fade.Tick += OnFadeTick;
            _fade.Start();
            Invalidate();
        }

        private void OnFadeTick(object sender, EventArgs e)
        {
            if (!_fadingOut)
            {
                if (Opacity < 1)
                {
                    Opacity = Math.Min(1, Opacity + 0.15);
                }

                return;
            }

            Opacity -= 0.1f;
            if (Opacity <= 0)
            {
                _fade.Stop();
                FluentToastManager.Remove(this);
                Close();
            }
        }

        internal void BeginDismiss()
        {
            _fadingOut = true;
            _autoClose.Stop();
            if (_fade == null)
            {
                FluentToastManager.Remove(this);
                Close();
                return;
            }

            _fade.Start();
        }

        private GraphicsPath RoundedRegion(int width, int height)
        {
            float radius = 8 * S;
            return FluentGraphics.RoundedRect(new RectangleF(0, 0, width, height), radius);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            bool close = CloseRect.Contains(e.Location);
            if (close != _closeHovered)
            {
                _closeHovered = close;
                Invalidate();
            }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _hovered = true;
            _autoClose.Stop();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hovered = false;
            _closeHovered = false;
            if (!_fadingOut)
            {
                _autoClose.Start();
            }

            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (CloseRect.Contains(e.Location))
            {
                BeginDismiss();
            }
        }

        private Color SeverityColor(FluentPalette p)
        {
            switch (_toast.Severity)
            {
                case InfoBarSeverity.Success: return p.SystemFillSuccess;
                case InfoBarSeverity.Warning: return p.SystemFillCaution;
                case InfoBarSeverity.Error: return p.SystemFillCritical;
                default: return p.SystemFillAttention;
            }
        }

        private Color SeverityBackground(FluentPalette p)
        {
            switch (_toast.Severity)
            {
                case InfoBarSeverity.Success: return p.SystemFillSuccessBackground;
                case InfoBarSeverity.Warning: return p.SystemFillCautionBackground;
                case InfoBarSeverity.Error: return p.SystemFillCriticalBackground;
                default: return p.SystemFillAttentionBackground;
            }
        }

        private int SeverityGlyph()
        {
            switch (_toast.Severity)
            {
                case InfoBarSeverity.Success: return 0xE73E;
                case InfoBarSeverity.Warning: return 0xE7BA;
                case InfoBarSeverity.Error: return 0xEA39;
                default: return 0xE946;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            FluentGraphics.SetupGraphics(g);
            FluentPalette p = ApplicationThemeManager.Palette;

            float radius = 8 * S;
            var bounds = new RectangleF(0, 0, Width, Height);
            using (var brush = new SolidBrush(ControlHelper.BlendOverParent(this, SeverityBackground(p))))
            {
                FluentGraphics.FillRoundedRect(g, brush, bounds, radius);
            }

            using (var pen = new Pen(p.CardStrokeDefaultSolid, 1f))
            using (var path = RoundedRegion(Width, Height))
            {
                g.DrawPath(pen, path);
            }

            float s = S;
            var iconCircle = new RectangleF(12 * s, (Height - 24 * s) / 2f, 24 * s, 24 * s);
            using (var brush = new SolidBrush(SeverityColor(p)))
            {
                g.FillEllipse(brush, iconCircle);
            }

            Color glyphColor = _toast.Severity == InfoBarSeverity.Warning &&
                ApplicationThemeManager.EffectiveTheme == ApplicationTheme.Light
                ? Color.FromArgb(228, 0, 0, 0)
                : Color.White;
            FluentGraphics.DrawGlyph(g, FluentGraphics.Glyph(SeverityGlyph()), FluentTypography.Icon(9f), glyphColor, iconCircle);

            float textX = 48 * s;
            float textWidth = Width - textX - 36 * s;
            bool hasTitle = !string.IsNullOrEmpty(_toast.Title);
            bool hasMessage = !string.IsNullOrEmpty(_toast.Message);

            using (var format = new StringFormat { FormatFlags = StringFormatFlags.LineLimit, Trimming = StringTrimming.EllipsisCharacter })
            {
                if (hasTitle && hasMessage)
                {
                    FluentGraphics.DrawText(g, _toast.Title, FluentTypography.BodyStrong, p.TextFillPrimary,
                        new RectangleF(textX, 8 * s, textWidth, 22 * s), format);
                    FluentGraphics.DrawText(g, _toast.Message, Font, p.TextFillSecondary,
                        new RectangleF(textX, 30 * s, textWidth, Height - 38 * s), format);
                }
                else
                {
                    string text = hasTitle ? _toast.Title : _toast.Message;
                    FluentGraphics.DrawText(g, text, hasTitle ? FluentTypography.BodyStrong : Font,
                        hasTitle ? p.TextFillPrimary : p.TextFillSecondary,
                        new RectangleF(textX, 0, textWidth, Height - 4 * s), format);
                }
            }

            if (_closeHovered)
            {
                using (var brush = new SolidBrush(ControlHelper.BlendOverParent(this, p.SubtleFillSecondary)))
                {
                    FluentGraphics.FillRoundedRect(g, brush, CloseRect, 4 * s);
                }
            }

            FluentGraphics.DrawGlyph(g, FluentGraphics.Glyph(0xE711), FluentTypography.Icon(8f), p.TextFillSecondary, CloseRect);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _autoClose?.Dispose();
                _fade?.Dispose();
                _themeSubscription?.Dispose();
            }

            base.Dispose(disposing);
        }
    }

    internal static class FluentToastManager
    {
        private static readonly List<FluentToastHost> _open = new List<FluentToastHost>();

        public static void Show(FluentToastHost host)
        {
            _open.Insert(0, host);
            host.FormClosed += (s, e) =>
            {
                _open.Remove(host);
                Layout();
            };
            host.Show();
            Layout();
        }

        public static void Remove(FluentToastHost host)
        {
            _open.Remove(host);
            Layout();
        }

        public static void Layout()
        {
            Rectangle area = Screen.PrimaryScreen.WorkingArea;
            int top = area.Top + (int)(12 * (Dpi / 96f));
            foreach (FluentToastHost host in _open)
            {
                int left = area.Right - host.Width - (int)(12 * (Dpi / 96f));
                host.Location = new Point(left, top);
                top += host.Height + (int)(12 * (Dpi / 96f));
            }
        }

        private static float Dpi
        {
            get
            {
                foreach (FluentToastHost host in _open)
                {
                    if (host.IsHandleCreated)
                    {
                        return ControlHelper.DpiOf(host);
                    }
                }

                return 96f;
            }
        }
    }
}
