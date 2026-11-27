using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using WinformUi.Drawing;
using WinformUi.Theming;

namespace WinformUi.Controls
{
    /// <summary>
    /// Fluent rich-text editor on the native RichTextBox editing engine:
    /// rounded themed shell, hover/focus fills and an accent underline,
    /// plus the reference IsTextSelectionEnabled gate for user selection.
    /// </summary>
    [DefaultEvent("TextChanged")]
    public class FluentRichTextBox : ContainerControl
    {
        private readonly RichTextInner _inner;
        private ThemeSubscription _themeSubscription;
        private bool _focused;
        private bool _isTextSelectionEnabled = true;

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.Style |= 0x02000000; // WS_CLIPCHILDREN
                return cp;
            }
        }

        public FluentRichTextBox()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            Font = FluentTypography.Body;
            Size = new Size(280, 150);
            MinimumSize = new Size(60, 34);
            TabStop = false;

            _inner = new RichTextInner
            {
                BorderStyle = BorderStyle.None,
                Font = FluentTypography.Body,
                ScrollBars = RichTextBoxScrollBars.Vertical,
                TabStop = true
            };
            _inner.GotFocus += (s, e) => { _focused = true; ApplyTheme(); };
            _inner.LostFocus += (s, e) => { _focused = false; ApplyTheme(); };
            _inner.TextChanged += (s, e) => OnTextChanged(e);
            _inner.LinkClicked += (s, e) => OnLinkClicked(this, e);
            Controls.Add(_inner);
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public RichTextBox InnerRichTextBox => _inner;

        protected FluentPalette Palette => ApplicationThemeManager.Palette;

        /// <summary>
        /// User selection mode: false collapses the current selection and blocks
        /// drag, double-click, shift-extension and Ctrl+A selection, matching the
        /// reference IsTextSelectionEnabled semantics. Editing is unaffected.
        /// </summary>
        [Category("Behavior")]
        [DefaultValue(true)]
        public bool IsTextSelectionEnabled
        {
            get { return _isTextSelectionEnabled; }
            set
            {
                if (_isTextSelectionEnabled != value)
                {
                    _isTextSelectionEnabled = value;
                    _inner.AllowSelection = value;
                }
            }
        }

        [Category("Behavior")]
        [DefaultValue(false)]
        public bool ReadOnly
        {
            get { return _inner.ReadOnly; }
            set { _inner.ReadOnly = value; }
        }

        [Category("Behavior")]
        [DefaultValue(true)]
        public bool WordWrap
        {
            get { return _inner.WordWrap; }
            set { _inner.WordWrap = value; }
        }

        [Category("Behavior")]
        [DefaultValue(false)]
        public bool DetectUrls
        {
            get { return _inner.DetectUrls; }
            set { _inner.DetectUrls = value; }
        }

        [Category("Behavior")]
        [DefaultValue(0)]
        public int MaxLength
        {
            get { return _inner.MaxLength; }
            set { _inner.MaxLength = value; }
        }

        public override string Text
        {
            get { return _inner.Text; }
            set { _inner.Text = value ?? string.Empty; }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Rtf
        {
            get { return _inner.Rtf; }
            set { _inner.Rtf = value; }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string SelectedText
        {
            get { return _inner.SelectedText; }
            set { _inner.SelectedText = value; }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string SelectedRtf
        {
            get { return _inner.SelectedRtf; }
            set { _inner.SelectedRtf = value; }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int SelectionStart
        {
            get { return _inner.SelectionStart; }
            set { _inner.SelectionStart = value; }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int SelectionLength
        {
            get { return _inner.SelectionLength; }
            set { _inner.SelectionLength = value; }
        }

        public void Select(int start, int length) => _inner.Select(start, length);
        public void SelectAll() => _inner.SelectAll();
        public void Clear() => _inner.Clear();
        public void AppendText(string text) => _inner.AppendText(text);
        public new bool Focus() => _inner.Focus();

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Font SelectionFontValue
        {
            get { return _inner.SelectionFont; }
            set { _inner.SelectionFont = value; }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color SelectionColorValue
        {
            get { return _inner.SelectionColor; }
            set { _inner.SelectionColor = value; }
        }

        public event LinkClickedEventHandler LinkClicked;

        protected virtual void OnLinkClicked(object sender, LinkClickedEventArgs e)
        {
            LinkClicked?.Invoke(sender, e);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (_themeSubscription == null)
            {
                _themeSubscription = new ThemeSubscription(this, ApplyTheme);
            }

            ApplyTheme();
            LayoutInner();
        }

        protected internal virtual void ApplyTheme()
        {
            _inner.ForeColor = Enabled ? Palette.TextFillPrimary : Palette.TextFillDisabled;
            _inner.BackColor = GetShellFill();
            if (_inner.IsHandleCreated)
            {
                FluentNcScrollBar.Redraw(_inner.Handle);
            }

            _inner.Invalidate();
            Invalidate();
        }

        private Color GetShellFill()
        {
            if (!Enabled)
            {
                return ControlHelper.BlendOverParent(this, Palette.ControlFillDisabled);
            }

            if (_focused)
            {
                return ControlHelper.BlendOverParent(this, Palette.ControlFillInputActive);
            }

            if (ClientRectangle.Contains(PointToClient(Cursor.Position)))
            {
                return ControlHelper.BlendOverParent(this, Palette.ControlFillSecondary);
            }

            return ControlHelper.BlendOverParent(this, Palette.ControlFillDefault);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            LayoutInner();
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            if (_inner != null)
            {
                _inner.Font = Font;
            }
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            _inner.Enabled = Enabled;
            ApplyTheme();
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            ApplyTheme();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            ApplyTheme();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (!_focused)
            {
                _inner.Focus();
            }
        }

        private void LayoutInner()
        {
            if (_inner == null)
            {
                return;
            }

            int inset = 2;
            _inner.Bounds = new Rectangle(inset, inset, Math.Max(0, Width - inset * 2), Math.Max(0, Height - inset * 2));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            ControlHelper.PaintFullBackground(this, e.Graphics);
            Graphics g = e.Graphics;
            FluentGraphics.SetupGraphics(g);

            Color fill = GetShellFill();
            if (_inner.BackColor != fill)
            {
                _inner.BackColor = fill;
            }

            var bounds = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
            using (var brush = new SolidBrush(fill))
            {
                FluentGraphics.FillRoundedRect(g, brush, bounds, FluentGraphics.ControlCornerRadius);
            }

            FluentGraphics.DrawElevationBorder(g, bounds, FluentGraphics.ControlCornerRadius, Palette);
            if (_focused && Enabled)
            {
                using (var brush = new SolidBrush(Palette.SystemAccentColorPrimary))
                using (var path = FluentGraphics.RoundedRect(bounds, FluentGraphics.ControlCornerRadius))
                {
                    g.SetClip(path);
                    g.FillRectangle(brush, bounds.X, bounds.Bottom - 2f, bounds.Width, 2f);
                    g.ResetClip();
                }
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

        private sealed class RichTextInner : RichTextBox
        {
            private bool _allowSelection = true;

            [System.ComponentModel.Browsable(false)]
            [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
            public bool AllowSelection
            {
                get { return _allowSelection; }
                set
                {
                    _allowSelection = value;
                    if (!value && SelectionLength > 0)
                    {
                        Select(SelectionStart, 0);
                    }
                }
            }

            protected override void WndProc(ref Message m)
            {
                if (m.Msg == FluentNcScrollBar.WM_NCPAINT &&
                    FluentNcScrollBar.PaintNc(Handle, Width, Height, BackColor))
                {
                    return;
                }

                if (!_allowSelection)
                {
                    // Only the low word is a key code or mouse-state flag here;
                    // messages that carry an HWND in wParam overflow Int32 on x64.
                    int low = (int)(m.WParam.ToInt64() & 0xFFFF);
                    bool drag = m.Msg == 0x0200 && (low & 0x0001) != 0;
                    bool wordSelect = m.Msg == 0x0203;
                    bool extend = m.Msg == 0x0100 && low >= 0x25 && low <= 0x28 && (ModifierKeys & Keys.Shift) != 0;
                    bool selectAll = m.Msg == 0x0100 && low == 0x41 && (ModifierKeys & Keys.Control) != 0;
                    if (drag || wordSelect || extend || selectAll)
                    {
                        return;
                    }
                }

                base.WndProc(ref m);
            }
        }
    }
}
