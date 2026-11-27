using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using WinformUi.Drawing;
using WinformUi.Theming;

namespace WinformUi.Controls
{
    public enum InfoBarSeverity
    {
        Informational,
        Success,
        Warning,
        Error
    }

    /// <summary>
    /// Fluent info bar: severity-colored banner with icon,
    /// title, message and optional close button.
    /// </summary>
    [System.ComponentModel.ToolboxItem(true)]
    public class FluentInfoBar : FluentControlBase
    {
        private InfoBarSeverity _severity = InfoBarSeverity.Informational;
        private string _title = string.Empty;
        private string _message = string.Empty;
        private bool _isClosable = true;
        private bool _closeHovered;

        public event EventHandler CloseButtonClick;
        public event EventHandler Closed;

        public FluentInfoBar()
        {
            Size = new Size(400, 52);
            TabStop = false;
        }

        [DefaultValue(InfoBarSeverity.Informational)]
        [Category("Fluent")]
        public InfoBarSeverity Severity
        {
            get { return _severity; }
            set { _severity = value; Invalidate(); }
        }

        [DefaultValue("")]
        [Category("Fluent")]
        public string Title
        {
            get { return _title; }
            set { _title = value; Invalidate(); }
        }

        [DefaultValue("")]
        [Category("Fluent")]
        public string Message
        {
            get { return _message; }
            set { _message = value; Invalidate(); }
        }

        [DefaultValue(true)]
        [Category("Fluent")]
        public bool IsClosable
        {
            get { return _isClosable; }
            set { _isClosable = value; Invalidate(); }
        }

        private Color SeverityColor
        {
            get
            {
                switch (_severity)
                {
                    case InfoBarSeverity.Success: return Palette.SystemFillSuccess;
                    case InfoBarSeverity.Warning: return Palette.SystemFillCaution;
                    case InfoBarSeverity.Error: return Palette.SystemFillCritical;
                    default: return Palette.SystemFillAttention;
                }
            }
        }

        private Color BackgroundColor
        {
            get
            {
                switch (_severity)
                {
                    case InfoBarSeverity.Success: return Palette.SystemFillSuccessBackground;
                    case InfoBarSeverity.Warning: return Palette.SystemFillCautionBackground;
                    case InfoBarSeverity.Error: return Palette.SystemFillCriticalBackground;
                    default: return Palette.SystemFillAttentionBackground;
                }
            }
        }

        private int SeverityGlyph
        {
            get
            {
                switch (_severity)
                {
                    case InfoBarSeverity.Success: return 0xE73E;
                    case InfoBarSeverity.Warning: return 0xE7BA;
                    case InfoBarSeverity.Error: return 0xEA39;
                    default: return 0xE946;
                }
            }
        }

        private RectangleF CloseRect => new RectangleF(Width - Scale(36), (Height - Scale(24)) / 2f, Scale(24), Scale(24));

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            bool hover = _isClosable && CloseRect.Contains(e.Location);
            if (_closeHovered != hover)
            {
                _closeHovered = hover;
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _closeHovered = false;
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (_isClosable && CloseRect.Contains(e.Location))
            {
                CloseButtonClick?.Invoke(this, EventArgs.Empty);
                Visible = false;
                Closed?.Invoke(this, EventArgs.Empty);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            ControlHelper.PaintFullBackground(this, e.Graphics);
            Graphics g = e.Graphics;
            FluentGraphics.SetupGraphics(g);

            Color background = ControlHelper.BlendOverParent(this, BackgroundColor);
            var bounds = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
            using (var brush = new SolidBrush(background))
            {
                FluentGraphics.FillRoundedRect(g, brush, bounds, FluentGraphics.ControlCornerRadius);
            }

            using (var pen = new Pen(Palette.CardStrokeDefault, 1f))
            {
                FluentGraphics.DrawRoundedRect(g, pen, bounds, FluentGraphics.ControlCornerRadius);
            }

            // Severity icon in a filled circle
            var iconCircle = new RectangleF(Scale(12), (Height - Scale(24)) / 2f, Scale(24), Scale(24));
            using (var brush = new SolidBrush(SeverityColor))
            {
                g.FillEllipse(brush, iconCircle);
            }

            Color glyphOnSeverity = _severity == InfoBarSeverity.Warning && ApplicationThemeManager.EffectiveTheme == ApplicationTheme.Light
                ? Color.FromArgb(228, 0, 0, 0)
                : Color.White;
            FluentGraphics.DrawGlyph(g, FluentGraphics.Glyph(SeverityGlyph), FluentTypography.Icon(9f), glyphOnSeverity, iconCircle);

            float textX = Scale(48);
            float textWidth = Width - textX - (_isClosable ? Scale(44) : Scale(12));
            bool hasTitle = !string.IsNullOrEmpty(_title);

            using (var format = new StringFormat { LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap, Trimming = StringTrimming.EllipsisCharacter })
            {
                if (hasTitle && !string.IsNullOrEmpty(_message))
                {
                    FluentGraphics.DrawText(g, _title, FluentTypography.BodyStrong, Palette.TextFillPrimary, new RectangleF(textX, 6, textWidth, Height / 2f - 4), format);
                    FluentGraphics.DrawText(g, _message, Font, Palette.TextFillSecondary, new RectangleF(textX, Height / 2f, textWidth, Height / 2f - 6), format);
                }
                else
                {
                    string text = hasTitle ? _title : _message;
                    FluentGraphics.DrawText(g, text, hasTitle ? FluentTypography.BodyStrong : Font, hasTitle ? Palette.TextFillPrimary : Palette.TextFillSecondary, new RectangleF(textX, 0, textWidth, Height), format);
                }
            }

            if (_isClosable)
            {
                if (_closeHovered)
                {
                    using (var brush = new SolidBrush(ControlHelper.BlendOverParent(this, Palette.SubtleFillSecondary)))
                    {
                        FluentGraphics.FillRoundedRect(g, brush, CloseRect, FluentGraphics.ControlCornerRadius);
                    }
                }

                FluentGraphics.DrawGlyph(g, FluentGraphics.Glyph(0xE711), FluentTypography.Icon(8f), Palette.TextFillSecondary, CloseRect);
            }
        }
    }

    /// <summary>Fluent badge: small pill with a value or text.</summary>
    [System.ComponentModel.ToolboxItem(true)]
    public class FluentBadge : FluentControlBase
    {
        private string _content = "1";
        private ControlAppearance _appearance = ControlAppearance.Primary;

        public FluentBadge()
        {
            Size = new Size(24, 18);
            TabStop = false;
        }

        [DefaultValue("1")]
        [Category("Fluent")]
        public string Content
        {
            get { return _content; }
            set { _content = value; Invalidate(); }
        }

        [DefaultValue(ControlAppearance.Primary)]
        [Category("Fluent")]
        public ControlAppearance Appearance
        {
            get { return _appearance; }
            set { _appearance = value; Invalidate(); }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            ControlHelper.PaintFullBackground(this, e.Graphics);
            Graphics g = e.Graphics;
            FluentGraphics.SetupGraphics(g);

            Color fill = ControlHelper.GetFill(this, _appearance, FluentVisualState.Normal);
            Color fore = ControlHelper.GetForeground(_appearance, FluentVisualState.Normal);
            if (_appearance == ControlAppearance.Secondary)
            {
                fill = ControlHelper.BlendOverParent(this, Palette.ControlStrongFillDefault);
                fore = Palette.TextFillInverse;
            }

            var bounds = new RectangleF(0, 0, Width, Height);
            using (var brush = new SolidBrush(fill))
            {
                FluentGraphics.FillRoundedRect(g, brush, bounds, Height / 2f);
            }

            using (var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                FluentGraphics.DrawText(g, _content, FluentTypography.Caption, fore, bounds, format);
            }
        }
    }

    public enum InfoBadgeSeverity
    {
        Attention,
        Informational,
        Success,
        Caution,
        Critical
    }

    /// <summary>Fluent info badge: severity dot or numeric pill.</summary>
    [System.ComponentModel.ToolboxItem(true)]
    public class FluentInfoBadge : FluentControlBase
    {
        private int _value;
        private InfoBadgeSeverity _severity = InfoBadgeSeverity.Attention;

        public FluentInfoBadge()
        {
            Size = new Size(16, 16);
            TabStop = false;
        }

        /// <summary>0 renders a dot; positive values render a numeric pill.</summary>
        [DefaultValue(0)]
        [Category("Fluent")]
        public int Value
        {
            get { return _value; }
            set
            {
                _value = value;
                Size = value > 0
                    ? new Size(Math.Max(Scale(18), Scale(10) + value.ToString().Length * Scale(7)), Scale(16))
                    : new Size(Scale(10), Scale(10));
                Invalidate();
            }
        }

        [DefaultValue(InfoBadgeSeverity.Attention)]
        [Category("Fluent")]
        public InfoBadgeSeverity Severity
        {
            get { return _severity; }
            set { _severity = value; Invalidate(); }
        }

        private Color SeverityColor
        {
            get
            {
                switch (_severity)
                {
                    case InfoBadgeSeverity.Informational: return Palette.SystemFillInformational;
                    case InfoBadgeSeverity.Success: return Palette.SystemFillSuccess;
                    case InfoBadgeSeverity.Caution: return Palette.SystemFillCaution;
                    case InfoBadgeSeverity.Critical: return Palette.SystemFillCritical;
                    default: return Palette.SystemFillAttention;
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            ControlHelper.PaintFullBackground(this, e.Graphics);
            Graphics g = e.Graphics;
            FluentGraphics.SetupGraphics(g);

            using (var brush = new SolidBrush(SeverityColor))
            {
                if (_value <= 0)
                {
                    g.FillEllipse(brush, 0, 0, Width, Height);
                }
                else
                {
                    FluentGraphics.FillRoundedRect(g, brush, new RectangleF(0, 0, Width, Height), Height / 2f);
                    using (var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                    {
                        FluentGraphics.DrawText(g, _value.ToString(), FluentTypography.Caption, Palette.TextFillInverse, new RectangleF(0, 0, Width, Height), format);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Fluent snackbar: bottom-of-form transient notification
    /// with optional action button and auto-hide.
    /// </summary>
    [System.ComponentModel.ToolboxItem(true)]
    public class FluentSnackbar : FluentControlBase
    {
        private readonly Timer _timer;
        private string _title = string.Empty;
        private string _message = string.Empty;
        private string _actionText = string.Empty;
        private bool _actionHovered;

        public event EventHandler ActionClick;
        public event EventHandler Hidden;

        public FluentSnackbar()
        {
            Size = new Size(420, 56);
            TabStop = false;
            _timer = new Timer { Interval = 4000 };
            _timer.Tick += (s, e) => HideSnackbar();
        }

        [DefaultValue("")]
        [Category("Fluent")]
        public string Title
        {
            get { return _title; }
            set { _title = value; Invalidate(); }
        }

        [DefaultValue("")]
        [Category("Fluent")]
        public string Message
        {
            get { return _message; }
            set { _message = value; Invalidate(); }
        }

        [DefaultValue("")]
        [Category("Fluent")]
        public string ActionText
        {
            get { return _actionText; }
            set { _actionText = value; Invalidate(); }
        }

        [DefaultValue(4000)]
        [Category("Fluent")]
        public int Timeout
        {
            get { return _timer.Interval; }
            set { _timer.Interval = Math.Max(500, value); }
        }

        public void ShowSnackbar()
        {
            Visible = true;
            BringToFront();
            _timer.Stop();
            _timer.Start();
        }

        public void HideSnackbar()
        {
            _timer.Stop();
            Visible = false;
            Hidden?.Invoke(this, EventArgs.Empty);
        }

        private RectangleF ActionRect
        {
            get
            {
                if (string.IsNullOrEmpty(_actionText))
                {
                    return RectangleF.Empty;
                }

                float width = TextRenderer.MeasureText(_actionText, FluentTypography.BodyStrong).Width + 20;
                return new RectangleF(Width - width - 12, (Height - 30) / 2f, width, 30);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            bool hover = !string.IsNullOrEmpty(_actionText) && ActionRect.Contains(e.Location);
            if (_actionHovered != hover)
            {
                _actionHovered = hover;
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (!string.IsNullOrEmpty(_actionText) && ActionRect.Contains(e.Location))
            {
                ActionClick?.Invoke(this, EventArgs.Empty);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            ControlHelper.PaintFullBackground(this, e.Graphics);
            Graphics g = e.Graphics;
            FluentGraphics.SetupGraphics(g);

            var bounds = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
            using (var brush = new SolidBrush(Palette.AcrylicBackgroundFillDefault))
            {
                FluentGraphics.FillRoundedRect(g, brush, bounds, FluentGraphics.PopupCornerRadius);
            }

            using (var pen = new Pen(Palette.SurfaceStrokeFlyout, 1f))
            {
                FluentGraphics.DrawRoundedRect(g, pen, bounds, FluentGraphics.PopupCornerRadius);
            }

            float textWidth = Width - 32 - (string.IsNullOrEmpty(_actionText) ? 0 : ActionRect.Width + 12);
            bool hasTitle = !string.IsNullOrEmpty(_title);
            using (var format = new StringFormat { LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap, Trimming = StringTrimming.EllipsisCharacter })
            {
                if (hasTitle && !string.IsNullOrEmpty(_message))
                {
                    FluentGraphics.DrawText(g, _title, FluentTypography.BodyStrong, Palette.TextFillPrimary, new RectangleF(16, 6, textWidth, Height / 2f - 4), format);
                    FluentGraphics.DrawText(g, _message, Font, Palette.TextFillSecondary, new RectangleF(16, Height / 2f, textWidth, Height / 2f - 6), format);
                }
                else
                {
                    string text = hasTitle ? _title : _message;
                    FluentGraphics.DrawText(g, text, Font, Palette.TextFillPrimary, new RectangleF(16, 0, textWidth, Height), format);
                }
            }

            if (!string.IsNullOrEmpty(_actionText))
            {
                RectangleF rect = ActionRect;
                if (_actionHovered)
                {
                    using (var brush = new SolidBrush(ControlHelper.BlendOverParent(this, Palette.SubtleFillSecondary)))
                    {
                        FluentGraphics.FillRoundedRect(g, brush, rect, FluentGraphics.ControlCornerRadius);
                    }
                }

                using (var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    FluentGraphics.DrawText(g, _actionText, FluentTypography.BodyStrong, Palette.AccentTextFillPrimary, rect, format);
                }
            }
        }

        /// <summary>Shows a snackbar anchored to the bottom of the given form.</summary>
        public static FluentSnackbar Show(Form owner, string title, string message, string actionText = null)
        {
            var snackbar = new FluentSnackbar
            {
                Title = title,
                Message = message,
                ActionText = actionText ?? string.Empty
            };
            snackbar.Location = new Point((owner.ClientSize.Width - snackbar.Width) / 2, owner.ClientSize.Height - snackbar.Height - 16);
            snackbar.Anchor = AnchorStyles.Bottom;
            owner.Controls.Add(snackbar);
            snackbar.ShowSnackbar();
            snackbar.Hidden += (s, e) =>
            {
                owner.Controls.Remove(snackbar);
                snackbar.Dispose();
            };
            return snackbar;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _timer.Stop();
                _timer.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}