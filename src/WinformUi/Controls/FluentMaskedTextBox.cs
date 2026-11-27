using System;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using WinformUi.Drawing;
using WinformUi.Interop;
using WinformUi.Theming;

namespace WinformUi.Controls
{
    /// <summary>Fluent text input with native mask validation and editing behavior.</summary>
    [DefaultEvent("MaskInputRejected")]
    public class FluentMaskedTextBox : ContainerControl
    {
        private readonly MaskedTextBox _inner;
        private ThemeSubscription _themeSubscription;
        private SymbolRegular _icon = SymbolRegular.Empty;
        private bool _focused;

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.Style |= 0x02000000; // WS_CLIPCHILDREN
                return cp;
            }
        }

        public FluentMaskedTextBox()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            Font = FluentTypography.Body;
            Size = new Size(200, 32);
            TabStop = false;

            _inner = new MaskedTextBox
            {
                BorderStyle = BorderStyle.None,
                Font = FluentTypography.Body,
                TabStop = true
            };
            _inner.GotFocus += (s, e) => { _focused = true; Invalidate(); };
            _inner.LostFocus += (s, e) => { _focused = false; Invalidate(); };
            _inner.TextChanged += (s, e) => OnTextChanged(e);
            _inner.MaskChanged += (s, e) => OnMaskChanged(e);
            _inner.TypeValidationCompleted += (s, e) => OnTypeValidationCompleted(e);
            _inner.KeyDown += (s, e) => OnKeyDown(e);
            _inner.KeyPress += (s, e) => OnKeyPress(e);
            _inner.KeyUp += (s, e) => OnKeyUp(e);
            _inner.Validating += (s, e) => OnValidating(e);
            _inner.MaskInputRejected += (s, e) => OnMaskInputRejected(e);
            Controls.Add(_inner);
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public MaskedTextBox InnerMaskedTextBox => _inner;

        protected FluentPalette Palette => ApplicationThemeManager.Palette;

        [Category("Fluent")]
        [DefaultValue("")]
        public string PlaceholderText
        {
            get { return _placeholderText; }
            set
            {
                _placeholderText = value ?? string.Empty;
                UpdateCueBanner();
            }
        }

        private string _placeholderText = string.Empty;

        [DefaultValue(SymbolRegular.Empty)]
        [Category("Fluent")]
        public SymbolRegular Icon
        {
            get { return _icon; }
            set
            {
                _icon = value;
                LayoutInner();
                Invalidate();
            }
        }

        [Category("Behavior")]
        [DefaultValue("")]
        public string Mask
        {
            get { return _inner.Mask; }
            set { _inner.Mask = value ?? string.Empty; }
        }

        [Category("Behavior")]
        [DefaultValue('_')]
        public char PromptChar
        {
            get { return _inner.PromptChar; }
            set { _inner.PromptChar = value; }
        }

        [Category("Behavior")]
        [DefaultValue(true)]
        public bool BeepOnError
        {
            get { return _inner.BeepOnError; }
            set { _inner.BeepOnError = value; }
        }

        [Category("Behavior")]
        [DefaultValue(MaskFormat.IncludePromptAndLiterals)]
        public MaskFormat TextMaskFormat
        {
            get { return _inner.TextMaskFormat; }
            set { _inner.TextMaskFormat = value; }
        }

        [Category("Behavior")]
        [DefaultValue(MaskFormat.IncludeLiterals)]
        public MaskFormat CutCopyMaskFormat
        {
            get { return _inner.CutCopyMaskFormat; }
            set { _inner.CutCopyMaskFormat = value; }
        }

        [Category("Behavior")]
        [DefaultValue(InsertKeyMode.Default)]
        public InsertKeyMode InsertKeyMode
        {
            get { return _inner.InsertKeyMode; }
            set { _inner.InsertKeyMode = value; }
        }

        [Category("Behavior")]
        [DefaultValue(true)]
        public bool SkipLiterals
        {
            get { return _inner.SkipLiterals; }
            set { _inner.SkipLiterals = value; }
        }

        [Category("Behavior")]
        [DefaultValue(true)]
        public bool ResetOnPrompt
        {
            get { return _inner.ResetOnPrompt; }
            set { _inner.ResetOnPrompt = value; }
        }

        [Category("Behavior")]
        [DefaultValue(true)]
        public bool ResetOnSpace
        {
            get { return _inner.ResetOnSpace; }
            set { _inner.ResetOnSpace = value; }
        }

        [Category("Behavior")]
        [DefaultValue(false)]
        public bool RejectInputOnFirstFailure
        {
            get { return _inner.RejectInputOnFirstFailure; }
            set { _inner.RejectInputOnFirstFailure = value; }
        }

        [Category("Behavior")]
        [DefaultValue(false)]
        public bool AllowPromptAsInput
        {
            get { return _inner.AllowPromptAsInput; }
            set { _inner.AllowPromptAsInput = value; }
        }

        [Category("Behavior")]
        [DefaultValue(false)]
        public bool AsciiOnly
        {
            get { return _inner.AsciiOnly; }
            set { _inner.AsciiOnly = value; }
        }

        [Category("Behavior")]
        [DefaultValue(null)]
        public Type ValidatingType
        {
            get { return _inner.ValidatingType; }
            set { _inner.ValidatingType = value; }
        }

        [Category("Behavior")]
        [DefaultValue(null)]
        public CultureInfo Culture
        {
            get { return _inner.Culture; }
            set { _inner.Culture = value; }
        }

        [Category("Behavior")]
        [DefaultValue(false)]
        public bool UseSystemPasswordChar
        {
            get { return _inner.UseSystemPasswordChar; }
            set { _inner.UseSystemPasswordChar = value; }
        }

        [Category("Behavior")]
        [DefaultValue('\0')]
        public char PasswordChar
        {
            get { return _inner.PasswordChar; }
            set { _inner.PasswordChar = value; }
        }

        [Category("Behavior")]
        [DefaultValue(false)]
        public bool ReadOnly
        {
            get { return _inner.ReadOnly; }
            set { _inner.ReadOnly = value; }
        }

        [Category("Behavior")]
        [DefaultValue(32767)]
        public int MaxLength
        {
            get { return _inner.MaxLength; }
            set { _inner.MaxLength = value; }
        }

        [Category("Behavior")]
        [DefaultValue(HorizontalAlignment.Left)]
        public HorizontalAlignment TextAlign
        {
            get { return _inner.TextAlign; }
            set { _inner.TextAlign = value; }
        }

        [Browsable(false)]
        public bool MaskCompleted => _inner.MaskCompleted;

        [Browsable(false)]
        public bool MaskFull => _inner.MaskFull;

        public event EventHandler MaskChanged;
        public event TypeValidationEventHandler TypeValidationCompleted;
        public event MaskInputRejectedEventHandler MaskInputRejected;

        public override string Text
        {
            get { return _inner.Text; }
            set { _inner.Text = value ?? string.Empty; }
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

        public void SelectAll() => _inner.SelectAll();
        public void Clear() => _inner.Clear();
        public new bool Focus() => _inner.Focus();
        public object ValidateText() => _inner.ValidateText();

        protected virtual void OnMaskChanged(EventArgs e)
        {
            MaskChanged?.Invoke(this, e);
        }

        protected virtual void OnTypeValidationCompleted(TypeValidationEventArgs e)
        {
            TypeValidationCompleted?.Invoke(this, e);
        }

        protected virtual void OnMaskInputRejected(MaskInputRejectedEventArgs e)
        {
            MaskInputRejected?.Invoke(this, e);
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
            UpdateCueBanner();
        }

        protected virtual void ApplyTheme()
        {
            _inner.ForeColor = Enabled ? Palette.TextFillPrimary : Palette.TextFillDisabled;
            _inner.BackColor = GetShellFill();
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

        protected override void OnImeModeChanged(EventArgs e)
        {
            base.OnImeModeChanged(e);
            if (_inner != null)
            {
                _inner.ImeMode = ImeMode;
            }
        }

        protected override void OnRightToLeftChanged(EventArgs e)
        {
            base.OnRightToLeftChanged(e);
            if (_inner != null)
            {
                _inner.RightToLeft = RightToLeft;
                LayoutInner();
            }
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
                LayoutInner();
            }
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            _inner.Enabled = Enabled;
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

        private int ReservedLeft => _icon != SymbolRegular.Empty ? 32 : 0;

        private void LayoutInner()
        {
            if (_inner == null)
            {
                return;
            }

            int left = 10 + ReservedLeft;
            int height = Math.Max(20, _inner.PreferredHeight);
            _inner.Bounds = new Rectangle(left, Math.Max(0, (Height - height) / 2), Math.Max(0, Width - left - 10), height);
        }

        private void UpdateCueBanner()
        {
            if (_inner == null || !_inner.IsHandleCreated)
            {
                return;
            }

            NativeMethods.SendMessage(_inner.Handle, NativeMethods.EM_SETCUEBANNER, IntPtr.Zero, _placeholderText);
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

            if (_icon != SymbolRegular.Empty)
            {
                const float iconSize = 16f;
                FluentGraphics.DrawGlyph(g, FluentGraphics.Glyph((int)_icon), FluentTypography.IconRegular(12f),
                    Enabled ? Palette.TextFillSecondary : Palette.TextFillDisabled,
                    new RectangleF(10, (Height - iconSize) / 2f, iconSize, iconSize));
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
