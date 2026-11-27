using System.ComponentModel;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using WinformUi.Drawing;
using WinformUi.Theming;

namespace WinformUi.Controls
{
    /// <summary>
    /// Fluent message box: borderless rounded dialog with
    /// severity glyph, title, message and a themed button footer.
    /// </summary>
    public static class FluentMessageBox
    {
        public static DialogResult Show(string text)
        {
            return Show(null, text, string.Empty, MessageBoxButtons.OK, MessageBoxIcon.None);
        }

        public static DialogResult Show(string text, string title)
        {
            return Show(null, text, title, MessageBoxButtons.OK, MessageBoxIcon.None);
        }

        public static DialogResult Show(IWin32Window owner, string text, string title, MessageBoxButtons buttons, MessageBoxIcon icon)
        {
            using (var dialog = new MessageDialogForm(text, title, buttons, icon))
            {
                return owner == null ? dialog.ShowDialog() : dialog.ShowDialog(owner);
            }
        }

        private sealed class MessageDialogForm : Form
        {
            private readonly string _text;
            private readonly string _title;
            private readonly MessageBoxIcon _icon;
            private readonly List<FluentButton> _buttons = new List<FluentButton>();

            public MessageDialogForm(string text, string title, MessageBoxButtons buttons, MessageBoxIcon icon)
            {
                _text = text ?? string.Empty;
                _title = title ?? string.Empty;
                _icon = icon;

                FormBorderStyle = FormBorderStyle.None;
                ShowInTaskbar = false;
                StartPosition = FormStartPosition.CenterParent;
                BackColor = ApplicationThemeManager.Palette.SolidBackgroundFillBase;
                Font = FluentTypography.Body;
                KeyPreview = true;
                TopMost = true;

                BuildButtons(buttons);
                LayoutDialog();
            }

            protected override CreateParams CreateParams
            {
                get
                {
                    CreateParams cp = base.CreateParams;
                    cp.ClassStyle |= 0x20000;
                    return cp;
                }
            }

            private void BuildButtons(MessageBoxButtons buttons)
            {
                switch (buttons)
                {
                    case MessageBoxButtons.OKCancel:
                        AddButton("OK", DialogResult.OK, true);
                        AddButton("Cancel", DialogResult.Cancel, false);
                        break;
                    case MessageBoxButtons.YesNo:
                        AddButton("Yes", DialogResult.Yes, true);
                        AddButton("No", DialogResult.No, false);
                        break;
                    case MessageBoxButtons.YesNoCancel:
                        AddButton("Yes", DialogResult.Yes, true);
                        AddButton("No", DialogResult.No, false);
                        AddButton("Cancel", DialogResult.Cancel, false);
                        break;
                    case MessageBoxButtons.RetryCancel:
                        AddButton("Retry", DialogResult.Retry, true);
                        AddButton("Cancel", DialogResult.Cancel, false);
                        break;
                    case MessageBoxButtons.AbortRetryIgnore:
                        AddButton("Abort", DialogResult.Abort, false);
                        AddButton("Retry", DialogResult.Retry, true);
                        AddButton("Ignore", DialogResult.Ignore, false);
                        break;
                    default:
                        AddButton("OK", DialogResult.OK, true);
                        break;
                }
            }

            private void AddButton(string text, DialogResult result, bool primary)
            {
                var button = new FluentButton
                {
                    Text = text,
                    Appearance = primary ? ControlAppearance.Primary : ControlAppearance.Secondary,
                    DialogResult = result,
                    Size = new Size(110, 32)
                };
                button.Click += (s, e) => { DialogResult = result; Close(); };
                _buttons.Add(button);
                Controls.Add(button);
                if (primary)
                {
                    AcceptButton = button;
                }

                if (result == DialogResult.Cancel)
                {
                    CancelButton = button;
                }
            }

            private void LayoutDialog()
            {
                const int width = 440;
                const int pad = 24;

                SizeF textSize = TextRenderer.MeasureText(_text, FluentTypography.Body, new Size(width - pad * 2, int.MaxValue), TextFormatFlags.WordBreak);

                int headerHeight = string.IsNullOrEmpty(_title) && _icon == MessageBoxIcon.None ? 0 : 40;
                int contentHeight = (int)Math.Ceiling(textSize.Height) + 8;
                int footerHeight = 64;
                int height = pad + headerHeight + contentHeight + footerHeight;

                ClientSize = new Size(width, height);

                float buttonX = width - pad;
                for (int i = _buttons.Count - 1; i >= 0; i--)
                {
                    buttonX -= _buttons[i].Width;
                    _buttons[i].Location = new Point((int)buttonX, height - footerHeight + 16);
                    buttonX -= 8;
                }

                using (var path = FluentGraphics.RoundedRect(new RectangleF(0, 0, Width, Height), FluentGraphics.PopupCornerRadius))
                {
                    Region = new Region(path);
                }
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                ControlHelper.PaintFullBackground(this, e.Graphics);
                base.OnPaint(e);
                Graphics g = e.Graphics;
                FluentGraphics.SetupGraphics(g);
                FluentPalette p = ApplicationThemeManager.Palette;

                const int pad = 24;
                float y = pad;

                if (_icon != MessageBoxIcon.None || !string.IsNullOrEmpty(_title))
                {
                    if (_icon != MessageBoxIcon.None)
                    {
                        int glyph;
                        Color color;
                        switch (_icon)
                        {
                            case MessageBoxIcon.Error:
                                glyph = 0xEA39; color = p.SystemFillCritical; break;
                            case MessageBoxIcon.Warning:
                                glyph = 0xE7BA; color = p.SystemFillCaution; break;
                            case MessageBoxIcon.Question:
                                glyph = 0xE9CE; color = p.SystemFillAttention; break;
                            default:
                                glyph = 0xE946; color = p.SystemFillAttention; break;
                        }

                        FluentGraphics.DrawGlyph(g, FluentGraphics.Glyph(glyph), FluentTypography.Icon(18f), color, new RectangleF(pad, y, 28, 28));
                    }

                    if (!string.IsNullOrEmpty(_title))
                    {
                        float titleX = _icon != MessageBoxIcon.None ? pad + 38 : pad;
                        using (var format = new StringFormat { LineAlignment = StringAlignment.Center })
                        {
                            FluentGraphics.DrawText(g, _title, FluentTypography.Subtitle, p.TextFillPrimary, new RectangleF(titleX, y - 4, Width - titleX - pad, 36), format);
                        }
                    }

                    y += 40;
                }

                FluentGraphics.DrawText(g, _text, FluentTypography.Body, p.TextFillPrimary, new RectangleF(pad, y, Width - pad * 2, Height - y - 64), null);

                // Footer divider
                using (var pen = new Pen(p.CardStrokeDefault, 1f))
                {
                    g.DrawLine(pen, 0, Height - 64, Width, Height - 64);
                }

                using (var pen = new Pen(p.SurfaceStrokeDefault, 1f))
                {
                    FluentGraphics.DrawRoundedRect(g, pen, new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f), FluentGraphics.PopupCornerRadius);
                }
            }
        }
    }

    /// <summary>
    /// Fluent content dialog: modal smoke overlay inside a form
    /// with a centered dialog surface and up to three footer buttons.
    /// </summary>
    public class FluentContentDialog
    {
        private Panel _overlay;
        private Panel _dialog;
        private Form _owner;

        public string Title { get; set; }
        public Control Content { get; set; }
        public string PrimaryButtonText { get; set; }
        public string SecondaryButtonText { get; set; }
        public string CloseButtonText { get; set; } = "Close";

        public event EventHandler PrimaryButtonClick;
        public event EventHandler SecondaryButtonClick;
        public event EventHandler CloseButtonClick;
        public event EventHandler Closed;

        public void Show(Form owner)
        {
            if (_overlay != null || owner == null)
            {
                return;
            }

            _owner = owner;
            FluentPalette p = ApplicationThemeManager.Palette;

            _overlay = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = AccentColor.Blend(p.SmokeFillDefault, p.ApplicationBackground)
            };
            _overlay.Click += (s, e) => Close();

            _dialog = new DialogSurface
            {
                BackColor = p.SolidBackgroundFillBase,
                Size = new Size(Math.Min(520, owner.ClientSize.Width - 48), 200)
            };

            var titleLabel = new FluentLabel
            {
                Text = Title ?? string.Empty,
                Font = FluentTypography.Subtitle,
                UseSecondaryColor = false,
                AutoSize = false,
                Bounds = new Rectangle(24, 20, _dialog.Width - 48, 32),
                TextAlign = ContentAlignment.MiddleLeft
            };
            _dialog.Controls.Add(titleLabel);

            int contentTop = 60;
            int footerHeight = 64;
            int contentHeight = 100;

            if (Content != null)
            {
                // A non-buffered content control erases on its live window DC,
                // where GDI+ fills preserve the destination alpha; under Mica
                // those alpha-0 pixels become backdrop holes. Buffering gives
                // the content surface real alpha.
                typeof(Control).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.SetValue(Content, true);
                contentHeight = Math.Max(Content.Height, 60);
                Content.Location = new Point(24, contentTop);
                Content.Width = _dialog.Width - 48;
                _dialog.Controls.Add(Content);
            }

            _dialog.Height = contentTop + contentHeight + 16 + footerHeight;

            var buttons = new List<FluentButton>();
            if (!string.IsNullOrEmpty(PrimaryButtonText))
            {
                var b = new FluentButton { Text = PrimaryButtonText, Appearance = ControlAppearance.Primary };
                b.Click += (s, e) => { PrimaryButtonClick?.Invoke(this, EventArgs.Empty); Close(); };
                buttons.Add(b);
            }

            if (!string.IsNullOrEmpty(SecondaryButtonText))
            {
                var b = new FluentButton { Text = SecondaryButtonText };
                b.Click += (s, e) => { SecondaryButtonClick?.Invoke(this, EventArgs.Empty); Close(); };
                buttons.Add(b);
            }

            if (!string.IsNullOrEmpty(CloseButtonText))
            {
                var b = new FluentButton { Text = CloseButtonText };
                b.Click += (s, e) => { CloseButtonClick?.Invoke(this, EventArgs.Empty); Close(); };
                buttons.Add(b);
            }

            float totalWidth = buttons.Count * 130 + (buttons.Count - 1) * 8;
            float x = (_dialog.Width - totalWidth) / 2f;
            foreach (var b in buttons)
            {
                b.Size = new Size(130, 32);
                b.Location = new Point((int)x, _dialog.Height - footerHeight + 16);
                x += 138;
                _dialog.Controls.Add(b);
            }

            _dialog.Location = new Point(
                (owner.ClientSize.Width - _dialog.Width) / 2,
                (owner.ClientSize.Height - _dialog.Height) / 2);

            _overlay.Controls.Add(_dialog);
            owner.Controls.Add(_overlay);
            _overlay.BringToFront();

            using (var path = FluentGraphics.RoundedRect(new RectangleF(0, 0, _dialog.Width, _dialog.Height), FluentGraphics.PopupCornerRadius))
            {
                _dialog.Region = new Region(path);
            }
        }

        public void Close()
        {
            if (_overlay == null)
            {
                return;
            }

            _owner.Controls.Remove(_overlay);
            _overlay.Dispose();
            _overlay = null;
            _dialog = null;
            _owner = null;
            Closed?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>Fluent flyout: arbitrary content in a rounded popup.</summary>
    public class FluentFlyout
    {
        private FluentPopupForm _popup;

        public Control Content { get; set; }
        public event EventHandler Closed;

        public void ShowAt(Control anchor)
        {
            if (_popup != null || Content == null || anchor == null)
            {
                return;
            }

            _popup = new FluentPopupForm();
            Content.Location = new Point(8, 8);
            Size contentSize = Content.Size;
            if (Content.AutoSize)
            {
                Size preferred = Content.GetPreferredSize(Size.Empty);
                contentSize = new Size(Math.Max(contentSize.Width, preferred.Width), Math.Max(contentSize.Height, preferred.Height));
            }

            _popup.ClientSize = new Size(contentSize.Width + 16, contentSize.Height + 16);
            _popup.Controls.Add(Content);
            _popup.FormClosed += (s, e) =>
            {
                _popup = null;
                Closed?.Invoke(this, EventArgs.Empty);
            };
            _popup.ShowBelow(anchor, _popup.Height, _popup.ClientSize.Width);
        }

        public void Close()
        {
            _popup?.Close();
        }
    }

    /// <summary>Fluent loading screen: full overlay with progress ring.</summary>
    public class FluentLoadingScreen : Control
    {
        private readonly FluentProgressRing _ring;
        private string _text = "Loading...";

        protected override void OnPaintBackground(PaintEventArgs e) => ControlHelper.PaintFullBackground(this, e.Graphics);

        public FluentLoadingScreen()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            Dock = DockStyle.Fill;
            _ring = new FluentProgressRing { Size = new Size(48, 48) };
            Controls.Add(_ring);
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string LoadingText
        {
            get { return _text; }
            set { _text = value; Invalidate(); }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            _ring.Location = new Point((Width - _ring.Width) / 2, (Height - _ring.Height) / 2 - 16);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            FluentPalette p = ApplicationThemeManager.Palette;
            if (_ring.BackColor != p.ApplicationBackground)
            {
                _ring.BackColor = p.ApplicationBackground;
            }

            ControlHelper.PaintFullBackground(this, e.Graphics);
            using (var brush = new SolidBrush(p.ApplicationBackground))
            {
                e.Graphics.FillRectangle(brush, ClientRectangle);
            }

            using (var format = new StringFormat { Alignment = StringAlignment.Center })
            {
                FluentGraphics.DrawText(e.Graphics, _text, FluentTypography.Body, p.TextFillPrimary, new RectangleF(0, _ring.Bottom + 12, Width, 24), format);
            }
        }

        public static FluentLoadingScreen Show(Form owner, string text)
        {
            var screen = new FluentLoadingScreen { LoadingText = text };
            owner.Controls.Add(screen);
            screen.BringToFront();
            return screen;
        }

        public void HideScreen()
        {
            Parent?.Controls.Remove(this);
            Dispose();
        }
    }

    /// <summary>ContentDialog surface: buffered so border and divider survive the backdrop blit.</summary>
    internal sealed class DialogSurface : Panel
    {
        public const int FooterHeight = 64;

        public DialogSurface()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            ControlHelper.PaintFullBackground(this, e.Graphics);
            base.OnPaint(e);
            Graphics g = e.Graphics;
            FluentGraphics.SetupGraphics(g);
            FluentPalette p = ApplicationThemeManager.Palette;
            using (var pen = new Pen(p.SurfaceStrokeDefault, 1f))
            {
                FluentGraphics.DrawRoundedRect(g, pen, new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f), FluentGraphics.PopupCornerRadius);
            }

            using (var pen = new Pen(p.CardStrokeDefault, 1f))
            {
                g.DrawLine(pen, 0, Height - FooterHeight, Width, Height - FooterHeight);
            }
        }
    }
}