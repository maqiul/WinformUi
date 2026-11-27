using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using WinformUi.Drawing;
using WinformUi.Theming;

namespace WinformUi.Controls
{
    /// <summary>
    /// Composite Fluent text input: a borderless native TextBox hosted inside an
    /// owner-drawn rounded shell with hover/focus visuals.
    /// </summary>
    [System.ComponentModel.ToolboxItem(false)]
    public class FluentTextInputBase : ContainerControl
    {
        private readonly PlaceholderTextBox _inner;
        private ThemeSubscription _themeSubscription;
        private SymbolRegular _icon = SymbolRegular.Empty;
        private bool _focused;

        protected override void OnPaintBackground(PaintEventArgs e) => ControlHelper.PaintFullBackground(this, e.Graphics);

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                // Without WS_CLIPCHILDREN the buffered shell blit paints over the
                // hosted edit, washing its glyphs out with the semi-transparent fill.
                cp.Style |= 0x02000000; // WS_CLIPCHILDREN
                return cp;
            }
        }

        public FluentTextInputBase()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            Font = FluentTypography.Body;
            Size = new Size(200, 32);
            TabStop = false;

            _inner = new PlaceholderTextBox
            {
                BorderStyle = BorderStyle.None,
                Font = FluentTypography.Body,
                TabStop = true
            };
            _inner.GotFocus += (s, e) => { _focused = true; _inner.Invalidate(); Invalidate(); };
            _inner.LostFocus += (s, e) => { _focused = false; _inner.Invalidate(); Invalidate(); };
            _inner.TextChanged += (s, e) => OnTextChanged(e);
            _inner.KeyDown += (s, e) => OnKeyDown(e);
            _inner.KeyPress += (s, e) => OnKeyPress(e);
            _inner.KeyUp += (s, e) => OnKeyUp(e);
            Controls.Add(_inner);
        }

        /// <summary>The hosted native TextBox, for advanced scenarios.</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public TextBox InnerTextBox => _inner;

        protected FluentPalette Palette => ApplicationThemeManager.Palette;

        protected bool IsInputFocused => _focused;

        [Browsable(true)]
        [Category("Fluent")]
        [DefaultValue("")]
        public string PlaceholderText
        {
            get { return _inner.Placeholder; }
            set
            {
                _inner.Placeholder = value ?? string.Empty;
                _inner.Invalidate();
            }
        }

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

        [Browsable(true)]
        [DefaultValue(false)]
        public bool Multiline
        {
            get { return _inner.Multiline; }
            set
            {
                _inner.Multiline = value;
                LayoutInner();
            }
        }

        [Browsable(true)]
        [DefaultValue(false)]
        public bool ReadOnly
        {
            get { return _inner.ReadOnly; }
            set { _inner.ReadOnly = value; }
        }

        [Browsable(true)]
        [DefaultValue(32767)]
        public int MaxLength
        {
            get { return _inner.MaxLength; }
            set { _inner.MaxLength = value; }
        }

        [Browsable(true)]
        [DefaultValue(HorizontalAlignment.Left)]
        public HorizontalAlignment TextAlign
        {
            get { return _inner.TextAlign; }
            set { _inner.TextAlign = value; }
        }

        [Browsable(true)]
        [DefaultValue(ScrollBars.None)]
        public ScrollBars ScrollBars
        {
            get { return _inner.ScrollBars; }
            set { _inner.ScrollBars = value; }
        }

        public override string Text
        {
            get { return _inner.Text; }
            set { _inner.Text = value; }
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

        public void SelectAll()
        {
            _inner.SelectAll();
        }

        public void Clear()
        {
            _inner.Clear();
        }

        public new bool Focus()
        {
            return _inner.Focus();
        }

        /// <summary>Extra space reserved on the right for spin/dropdown buttons.</summary>
        protected virtual int ReservedRight => 0;

        protected virtual int ReservedLeft => _icon != SymbolRegular.Empty ? 32 : 0;

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

        protected virtual void ApplyTheme()
        {
            _inner.ForeColor = Enabled ? Palette.TextFillPrimary : Palette.TextFillDisabled;
            _inner.BackColor = GetShellFill();
            _inner.PlaceholderColor = Enabled ? Palette.TextPlaceholder : Palette.TextFillDisabled;
            _inner.SelectionFill = Palette.AccentFillSelectedTextBackground;
            _inner.SelectionText = Palette.TextOnAccentSelectedText;
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
            if (_inner == null)
            {
                return;
            }

            _inner.Font = Font;
            LayoutInner();
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

        private void LayoutInner()
        {
            if (_inner == null)
            {
                return;
            }

            int left = 10 + ReservedLeft;
            int right = 10 + ReservedRight;

            if (Multiline)
            {
                _inner.Bounds = new Rectangle(left, 8, Math.Max(0, Width - left - right), Math.Max(0, Height - 16));
            }
            else
            {
                int innerHeight = _inner.PreferredHeight;
                int y = Math.Max(0, (Height - innerHeight) / 2);
                _inner.Bounds = new Rectangle(left, y, Math.Max(0, Width - left - right), innerHeight);
            }
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
                // Fluent focused text box: 2px accent underline along the bottom.
                using (var brush = new SolidBrush(Palette.SystemAccentColorPrimary))
                using (var path = FluentGraphics.RoundedRect(bounds, FluentGraphics.ControlCornerRadius))
                {
                    var clip = new RectangleF(bounds.X, bounds.Bottom - 2f, bounds.Width, 2f);
                    g.SetClip(path);
                    g.FillRectangle(brush, clip);
                    g.ResetClip();
                }
            }

            if (_icon != SymbolRegular.Empty)
            {
                float iconSize = 16f;
                var iconRect = new RectangleF(10, (Height - iconSize) / 2f, iconSize, iconSize);
                Color iconColor = Enabled ? Palette.TextFillSecondary : Palette.TextFillDisabled;
                FluentGraphics.DrawGlyph(g, FluentGraphics.Glyph((int)_icon), FluentTypography.IconRegular(12f), iconColor, iconRect);
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

        // The native edit's rendering leaves alpha-blended pixels that wash out under
        // the Mica backdrop, so the client is owner-drawn (background, value text,
        // selection and placeholder) into a 32bpp bitmap and blitted with real alpha.
        private sealed class PlaceholderTextBox : TextBox
        {
            private const int WM_PAINT = 0x000F;
            private const int WM_PRINTCLIENT = 0x0318;
            private const int EM_GETSEL = 0x00B0;
            private const int EM_GETLINECOUNT = 0x00BA;
            private const int EM_LINEINDEX = 0x00BB;
            private const int EM_LINELENGTH = 0x00C1;
            private const int EM_GETLINE = 0x00C4;
            private const int EM_GETFIRSTVISIBLELINE = 0x00CE;
            private const int EM_POSFROMCHAR = 0x00D6;
            private const int EM_SETREDRAW = 0x000B;

            public string Placeholder = string.Empty;
            public Color PlaceholderColor = Color.Gray;
            public Color SelectionFill = SystemColors.Highlight;
            public Color SelectionText = SystemColors.HighlightText;

            private bool _composing;
            private bool _caretVisible = true;
            private Timer _blinkTimer;
            private static readonly BufferedGraphicsContext _bufferContext = new BufferedGraphicsContext();

            protected override void OnTextChanged(EventArgs e)
            {
                base.OnTextChanged(e);
                // The edit only invalidates the changed glyph range; stale overlay
                // pixels (e.g. a placeholder) would survive outside that region.
                Invalidate();
            }

            protected override void OnHandleCreated(EventArgs e)
            {
                base.OnHandleCreated(e);
                // The edit repaints changed glyph ranges straight onto the live DC
                // inside mutation messages, double-rendering over our composite with
                // alpha-zero ClearType pixels. Suppress all native painting; every
                // pixel of this client comes from RenderClient.
                SendMessage(Handle, EM_SETREDRAW, IntPtr.Zero, IntPtr.Zero);
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing && _blinkTimer != null)
                {
                    _blinkTimer.Dispose();
                }

                base.Dispose(disposing);
            }

            private void StartBlink()
            {
                if (_blinkTimer == null)
                {
                    _blinkTimer = new Timer { Interval = SystemInformation.CaretBlinkTime };
                    _blinkTimer.Tick += (s, e) =>
                    {
                        _caretVisible = !_caretVisible;
                        Invalidate();
                    };
                }

                _caretVisible = true;
                _blinkTimer.Start();
                Invalidate();
            }

            [DllImport("user32.dll")]
            private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

            [DllImport("user32.dll")]
            private static extern IntPtr SendMessage(IntPtr hWnd, int msg, out int wParam, out int lParam);

            [DllImport("user32.dll", CharSet = CharSet.Unicode)]
            private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, StringBuilder lParam);

            [DllImport("user32.dll")]
            private static extern IntPtr BeginPaint(IntPtr hWnd, ref PAINTSTRUCT ps);

            [DllImport("user32.dll")]
            private static extern bool EndPaint(IntPtr hWnd, ref PAINTSTRUCT ps);

            [DllImport("user32.dll")]
            private static extern bool DestroyCaret();

            [DllImport("user32.dll")]
            private static extern bool UpdateWindow(IntPtr hWnd);

            [StructLayout(LayoutKind.Sequential)]
            private struct PAINTSTRUCT
            {
                public IntPtr hdc;
                public bool fErase;
                public RECT rcPaint;
                public bool fRestore;
                public bool fIncUpdate;
                [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
                public byte[] rgbReserved;
            }

            [StructLayout(LayoutKind.Sequential)]
            private struct RECT
            {
                public int Left, Top, Right, Bottom;
            }

            protected override void WndProc(ref Message m)
            {
                if (m.Msg == FluentNcScrollBar.WM_NCPAINT &&
                    FluentNcScrollBar.PaintNc(Handle, Width, Height, BackColor))
                {
                    return;
                }

                if (m.Msg == WM_PAINT && !_composing)
                {
                    // Owner-draw through a 32bpp bitmap: GDI and GDI+ fills on a live
                    // window DC preserve the destination alpha, which DWM reads as
                    // transparency under Mica. A bitmap blit writes real alpha.
                    var ps = new PAINTSTRUCT();
                    IntPtr hdc = BeginPaint(Handle, ref ps);
                    RenderClient(hdc);
                    EndPaint(Handle, ref ps);
                    return;
                }

                base.WndProc(ref m);
                if (m.Msg == 0x010D) // WM_IME_STARTCOMPOSITION
                {
                    _composing = true;
                    // The composition string is painted by the edit itself.
                    SendMessage(Handle, EM_SETREDRAW, (IntPtr)1, IntPtr.Zero);
                    Invalidate();
                    return;
                }

                if (m.Msg == 0x010E) // WM_IME_ENDCOMPOSITION
                {
                    _composing = false;
                    SendMessage(Handle, EM_SETREDRAW, IntPtr.Zero, IntPtr.Zero);
                    DestroyCaret();
                    Invalidate();
                    UpdateWindow(Handle);
                    return;
                }

                if (m.Msg == WM_PRINTCLIENT)
                {
                    RenderClient(m.WParam);
                }

                switch (m.Msg)
                {
                    case 0x0007: // WM_SETFOCUS
                        // The native caret blits via XOR, which corrupts ClearType
                        // fringes; draw our own caret in the composited bitmap instead.
                        DestroyCaret();
                        StartBlink();
                        break;
                    case 0x0008: // WM_KILLFOCUS
                        if (_blinkTimer != null)
                        {
                            _blinkTimer.Stop();
                        }

                        Invalidate();
                        break;
                    case 0x0030: // WM_SETFONT
                        DestroyCaret();
                        break;
                    case 0x000C: // WM_SETTEXT
                    case 0x00B1: // EM_SETSEL
                    case 0x0100: // WM_KEYDOWN
                    case 0x0102: // WM_CHAR
                    case 0x0200: // WM_MOUSEMOVE
                        // The edit paints the drag selection straight onto the live
                        // DC (alpha-zero pixels wash to light blue under Mica);
                        // overwrite it with our composite on every drag step.
                        if (((int)m.WParam & 0x0001) == 0)
                        {
                            break;
                        }

                        goto case 0x0201;
                    case 0x0202: // WM_LBUTTONUP
                    case 0x0203: // WM_LBUTTONDBLCLK
                    case 0x0201: // WM_LBUTTONDOWN
                    case 0x0300: // WM_CUT
                    case 0x0302: // WM_PASTE
                    case 0x0303: // WM_CLEAR
                        _caretVisible = true;
                        // The edit repaints the changed glyph range straight onto the
                        // live DC inside these messages; those alpha-zero ClearType
                        // pixels composite as Mica holes over our buffered render.
                        // Repaint synchronously so the composite overwrites them now,
                        // not on the next posted WM_PAINT.
                        Invalidate();
                        UpdateWindow(Handle);
                        break;
                }
            }

            private void RenderClient(IntPtr hdc)
            {
                int w = ClientSize.Width;
                int h = ClientSize.Height;
                if (w <= 0 || h <= 0)
                {
                    return;
                }

                // A BufferedGraphics DIB keeps GDI ClearType text at native weight;
                // Graphics.FromImage surfaces fall back to grayscale antialiasing,
                // which reads as semi-bold. Render blits real alpha, so the opaque
                // client cannot show Mica holes through glyph fringes.
                using (var target = Graphics.FromHdc(hdc))
                using (var buf = _bufferContext.Allocate(target, new Rectangle(0, 0, w, h)))
                {
                    Graphics bg = buf.Graphics;
                    bg.Clear(BackColor);
                    PaintOverlay(bg);
                    // ClearType on a DIB zeroes glyph alpha; the blit carries it
                    // to the window, where DWM would composite glyphs as holes.
                    FluentGraphics.RepairTextAlpha(bg, new Rectangle(0, 0, w, h), BackColor);
                    buf.Render(hdc);
                }
            }

            private void PaintOverlay(Graphics g)
            {
                if (Text.Length == 0)
                {
                    // While the IME composes, the window text is still empty but
                    // the composition string is on screen; keep the placeholder hidden.
                    if (!_composing)
                    {
                        DrawPlaceholder(g);
                    }
                }
                else
                {
                    DrawText(g);
                }

                DrawCaret(g);
            }

            private void DrawPlaceholder(Graphics g)
            {
                if (Placeholder.Length == 0)
                {
                    return;
                }

                using (var format = new StringFormat(StringFormatFlags.NoWrap))
                {
                    format.LineAlignment = Multiline ? StringAlignment.Near : StringAlignment.Center;
                    format.Alignment = TextAlign == HorizontalAlignment.Right ? StringAlignment.Far
                        : TextAlign == HorizontalAlignment.Center ? StringAlignment.Center
                        : StringAlignment.Near;
                    FluentGraphics.DrawText(g, Placeholder, Font, PlaceholderColor, new RectangleF(0, 0, ClientSize.Width, ClientSize.Height), format);
                }
            }

            private void DrawText(Graphics g)
            {
                int selStart, selEnd;
                SendMessage(Handle, EM_GETSEL, out selStart, out selEnd);
                if (!Focused)
                {
                    selStart = selEnd = 0;
                }
                char mask = PasswordChar;
                if (mask == '\0' && UseSystemPasswordChar)
                {
                    mask = '●';
                }

                int lineCount = (int)SendMessage(Handle, EM_GETLINECOUNT, IntPtr.Zero, IntPtr.Zero);
                int firstLine = (int)SendMessage(Handle, EM_GETFIRSTVISIBLELINE, IntPtr.Zero, IntPtr.Zero);
                using (var selectionFill = new SolidBrush(SelectionFill))
                {
                    for (int line = firstLine; line < lineCount; line++)
                    {
                        int lineStart = (int)SendMessage(Handle, EM_LINEINDEX, (IntPtr)line, IntPtr.Zero);
                        if (lineStart < 0)
                        {
                            break;
                        }

                        int lineLen = (int)SendMessage(Handle, EM_LINELENGTH, (IntPtr)lineStart, IntPtr.Zero);
                        if (lineLen <= 0)
                        {
                            continue;
                        }

                        var sb = new StringBuilder(lineLen + 1);
                        sb.Append((char)lineLen);
                        SendMessage(Handle, EM_GETLINE, (IntPtr)line, sb);
                        string lineText = sb.ToString(0, Math.Min(lineLen, sb.Length)).TrimEnd('\r', '\n');
                        if (mask != '\0')
                        {
                            lineText = new string(mask, lineText.Length);
                        }

                        int lineEnd = lineStart + lineText.Length;
                        int pos = lineStart;
                        while (pos < lineEnd)
                        {
                            int stop = lineEnd;
                            if (selStart > pos && selStart < stop) stop = selStart;
                            if (selEnd > pos && selEnd < stop) stop = selEnd;
                            bool inSelection = pos >= selStart && pos < selEnd;
                            Point origin = CharPosition(g, pos);
                            if (inSelection)
                            {
                                Point end = CharPosition(g, stop);
                                float width = end.X > origin.X ? end.X - origin.X : ClientSize.Width - origin.X;
                                float y = Multiline ? origin.Y : 0f;
                                float height = Multiline ? Font.Height : ClientSize.Height;
                                g.FillRectangle(selectionFill, origin.X, y, width, height);
                            }

                            // Per-glyph at native char positions: DrawTextEx cumulative
                            // advances drift from the edit's own layout, which put the
                            // caret and selection inside glyphs. One glyph per native
                            // position keeps text, selection, caret and scroll aligned.
                            Color color = inSelection ? SelectionText : ForeColor;
                            for (int i = pos; i < stop; i++)
                            {
                                FluentGraphics.DrawText(g, lineText.Substring(i - lineStart, 1), Font, color, CharPosition(g, i));
                            }

                            pos = stop;
                        }
                    }
                }
            }

            private void DrawCaret(Graphics g)
            {
                if (!Focused || _composing || !_caretVisible)
                {
                    return;
                }

                int selStart, selEnd;
                SendMessage(Handle, EM_GETSEL, out selStart, out selEnd);
                // Text is painted per glyph at these same native positions.
                Point origin = CharPosition(g, selEnd);
                using (var brush = new SolidBrush(ForeColor))
                {
                    g.FillRectangle(brush, origin.X, origin.Y, 1f, Font.Height);
                }
            }

            private int TextWidth(Graphics g, string text)
            {
                if (text.Length == 0)
                {
                    return 0;
                }

                return TextRenderer.MeasureText(g, text, Font, new Size(int.MaxValue, int.MaxValue),
                    TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix).Width;
            }

            private Point CharPosition(Graphics g, int charIndex)
            {
                int packed = (int)SendMessage(Handle, EM_POSFROMCHAR, (IntPtr)charIndex, IntPtr.Zero);
                if (packed != -1)
                {
                    return new Point((short)(packed & 0xFFFF), (short)(packed >> 16));
                }

                // Single-line edits return -1 once the index reaches the text length;
                // derive the end-of-text position from the previous glyph's advance.
                if (charIndex <= 0)
                {
                    return Point.Empty;
                }

                Point prev = CharPosition(g, charIndex - 1);
                char ch = Text[charIndex - 1];
                if (PasswordChar != '\0')
                {
                    ch = PasswordChar;
                }
                else if (UseSystemPasswordChar)
                {
                    ch = '●';
                }

                Size advance = TextRenderer.MeasureText(g, ch.ToString(), Font, new Size(int.MaxValue, int.MaxValue),
                    TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                return new Point(prev.X + advance.Width, prev.Y);
            }
        }
    }

    /// <summary>Fluent single/multi-line text box.</summary>
    [System.ComponentModel.ToolboxItem(true)]
    public class FluentTextBox : FluentTextInputBase
    {
    }

    /// <summary>Fluent password box.</summary>
    [System.ComponentModel.ToolboxItem(true)]
    public class FluentPasswordBox : FluentTextInputBase
    {
        public FluentPasswordBox()
        {
            InnerTextBox.UseSystemPasswordChar = true;
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public char PasswordChar
        {
            get { return InnerTextBox.PasswordChar; }
            set { InnerTextBox.PasswordChar = value; }
        }
    }
}