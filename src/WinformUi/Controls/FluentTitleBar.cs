using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using WinformUi.Drawing;
using WinformUi.Interop;
using WinformUi.Theming;

namespace WinformUi.Controls
{
    /// <summary>
    /// Fluent custom title bar for borderless forms: icon, title, optional content
    /// slots and caption/minimize/restore/maximize/close buttons. The control answers
    /// WM_NCHITTEST so empty areas drag the window, double-click maximizes, the top
    /// edge resizes and right-click opens the system menu, while the buttons stay
    /// client areas with hover feedback. Dock it at the top of a form with
    /// <c>FormBorderStyle.None</c>; it pads the form while maximized so content is not
    /// clipped by the shell.
    /// </summary>
    [ToolboxItem(true)]
    [DefaultProperty("Title")]
    public class FluentTitleBar : FluentControlBase
    {
        private string _title = string.Empty;
        private bool _showHelpButton;
        private ButtonVisual _hoveredButton = ButtonVisual.None;
        private ButtonVisual _pressedButton = ButtonVisual.None;
        private Control _leadingContent;
        private Control _trailingContent;
        private Form _parentForm;
        private Padding _restorePadding = Padding.Empty;
        private bool _paddingApplied;

        private enum ButtonVisual
        {
            None,
            Minimize,
            Restore,
            Maximize,
            Help,
            Close
        }

        private const int BaseHeight = 48;
        private const int BaseButtonWidth = 46;
        private const int BaseButtonHeight = 32;

        public FluentTitleBar()
        {
            Size = new Size(400, Scale(BaseHeight));
            Dock = DockStyle.Top;
            BackColor = Color.Transparent;
            TabStop = false;
            SetStyle(ControlStyles.ResizeRedraw, true);
        }

        [DefaultValue("")]
        [Category("Fluent")]
        [Localizable(true)]
        public string Title
        {
            get { return _title; }
            set { _title = value ?? string.Empty; Invalidate(); }
        }

        [DefaultValue(false)]
        [Category("Fluent")]
        public bool ShowHelpButton
        {
            get { return _showHelpButton; }
            set { _showHelpButton = value; Invalidate(); }
        }

        /// <summary>Control shown right after the title.</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Control LeadingContent
        {
            get { return _leadingContent; }
            set { SetContentSlot(ref _leadingContent, value); }
        }

        /// <summary>Control shown before the caption buttons.</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Control TrailingContent
        {
            get { return _trailingContent; }
            set { SetContentSlot(ref _trailingContent, value); }
        }

        public event EventHandler HelpButtonClick;

        private void SetContentSlot(ref Control slot, Control value)
        {
            if (ReferenceEquals(slot, value))
            {
                return;
            }

            if (slot != null)
            {
                Controls.Remove(slot);
            }

            slot = value;
            if (value != null)
            {
                Controls.Add(value);
            }

            LayoutContent();
            Invalidate();
        }

        private Form Owner
        {
            get { return _parentForm ?? FindForm(); }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            _parentForm = FindForm();
            if (_parentForm != null)
            {
                _parentForm.Resize += OnParentChanged;
            }

            ApplyMaximizePadding();
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            if (_parentForm != null)
            {
                _parentForm.Resize -= OnParentChanged;
                _parentForm = null;
            }

            base.OnHandleDestroyed(e);
        }

        private void OnParentChanged(object sender, EventArgs e)
        {
            ApplyMaximizePadding();
            LayoutContent();
            Invalidate();
        }

        /// <summary>Borderless maximized windows cover the resize frame; keep content inside the work area.</summary>
        private void ApplyMaximizePadding()
        {
            Form form = Owner;
            if (form == null)
            {
                return;
            }

            bool needsPadding = form.FormBorderStyle == FormBorderStyle.None
                && form.WindowState == FormWindowState.Maximized;
            if (needsPadding && !_paddingApplied)
            {
                _restorePadding = form.Padding;
                int pad = Math.Max(1, SystemInformation.FrameBorderSize.Width);
                form.Padding = new Padding(pad, pad, pad, pad);
                _paddingApplied = true;
            }
            else if (!needsPadding && _paddingApplied)
            {
                form.Padding = _restorePadding;
                _paddingApplied = false;
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            LayoutContent();
        }

        protected override void SetBoundsCore(int x, int y, int width, int height, BoundsSpecified specified)
        {
            if (Dock == DockStyle.Top || Dock == DockStyle.Bottom)
            {
                height = Scale(BaseHeight);
            }

            base.SetBoundsCore(x, y, width, height, specified);
        }

        private void LayoutContent()
        {
            int buttonsLeft = Width - ButtonColumnWidth() - Scale(4);
            int x = Scale(12);
            if (_leadingContent != null)
            {
                _leadingContent.Location = new Point(x, (Height - _leadingContent.Height) / 2);
                x += _leadingContent.Width + Scale(8);
            }

            if (_trailingContent != null)
            {
                int tx = Math.Max(x, buttonsLeft - _trailingContent.Width);
                _trailingContent.Location = new Point(tx, (Height - _trailingContent.Height) / 2);
            }
        }

        private int ButtonColumnWidth()
        {
            return Scale(BaseButtonWidth) * WindowButtons.Count;
        }

        private System.Collections.Generic.List<ButtonVisual> WindowButtons
        {
            get
            {
                var buttons = new System.Collections.Generic.List<ButtonVisual>();
                Form form = Owner;
                if (_showHelpButton)
                {
                    buttons.Add(ButtonVisual.Help);
                }

                if (form == null || form.MinimizeBox)
                {
                    buttons.Add(ButtonVisual.Minimize);
                }

                if (form == null || form.MaximizeBox)
                {
                    buttons.Add(IsMaximized ? ButtonVisual.Restore : ButtonVisual.Maximize);
                }

                buttons.Add(ButtonVisual.Close);
                return buttons;
            }
        }

        private Rectangle ButtonRect(ButtonVisual which)
        {
            System.Collections.Generic.List<ButtonVisual> buttons = WindowButtons;
            int index = buttons.IndexOf(which);
            if (index < 0)
            {
                return Rectangle.Empty;
            }

            int bw = Scale(BaseButtonWidth);
            return new Rectangle(Width - (buttons.Count - index) * bw, (Height - Scale(BaseButtonHeight)) / 2, bw, Scale(BaseButtonHeight));
        }

        private ButtonVisual ButtonAt(Point point)
        {
            foreach (ButtonVisual button in WindowButtons)
            {
                if (ButtonRect(button).Contains(point))
                {
                    return button;
                }
            }

            return ButtonVisual.None;
        }

        private bool IsMaximized
        {
            get
            {
                Form form = Owner;
                return form != null && form.WindowState == FormWindowState.Maximized;
            }
        }

        private const int HTMINBUTTON = 8;
        private const int HTMAXBUTTON = 9;
        private const int HTCLOSE = 20;
        private const int HTHELP = 21;

        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case NativeMethods.WM_NCHITTEST:
                    m.Result = HitTest(m.LParam);
                    return;
                case NativeMethods.WM_NCLBUTTONDOWN:
                {
                    int code = m.WParam.ToInt32();
                    if (code == HTMINBUTTON || code == HTMAXBUTTON || code == HTCLOSE || code == HTHELP)
                    {
                        _pressedButton = ButtonFromHit(code);
                        Invalidate();
                        return;
                    }

                    // The bar is a child window: DefWindowProc would drag/resize the bar itself.
                    // Route caption moves and edge resizes to the owner form instead.
                    Form downOwner = Owner;
                    if (downOwner != null && downOwner.IsHandleCreated)
                    {
                        bool caption = code == NativeMethods.HTCAPTION;
                        bool resize = code >= NativeMethods.HTLEFT && code <= NativeMethods.HTBOTTOMRIGHT;
                        if (caption || resize)
                        {
                            NativeMethods.ReleaseCapture();
                            int command = (caption ? NativeMethods.SC_MOVE : NativeMethods.SC_SIZE) | code;
                            NativeMethods.SendMessage(downOwner.Handle, NativeMethods.WM_SYSCOMMAND, new IntPtr(command), IntPtr.Zero);
                            return;
                        }
                    }

                    break;
                }
                case NativeMethods.WM_NCLBUTTONDBLCLK:
                {
                    Form dblOwner = Owner;
                    if (m.WParam.ToInt32() == NativeMethods.HTCAPTION && dblOwner != null)
                    {
                        dblOwner.WindowState = dblOwner.WindowState == FormWindowState.Maximized
                            ? FormWindowState.Normal
                            : FormWindowState.Maximized;
                        return;
                    }

                    break;
                }
                case NativeMethods.WM_NCLBUTTONUP:
                {
                    int code = m.WParam.ToInt32();
                    if (code == HTMINBUTTON || code == HTMAXBUTTON || code == HTCLOSE || code == HTHELP)
                    {
                        ButtonVisual button = ButtonFromHit(code);
                        if (button == ButtonVisual.Maximize && IsMaximized)
                        {
                            button = ButtonVisual.Restore;
                        }

                        _pressedButton = ButtonVisual.None;
                        if (ButtonAt(PointToClient(PointFromLParam(m.LParam))) == button)
                        {
                            InvokeButton(button);
                        }

                        Invalidate();
                        return;
                    }

                    break;
                }
                case NativeMethods.WM_NCMOUSEMOVE:
                {
                    Point local = PointToClient(PointFromLParam(m.LParam));
                    ButtonVisual hit = ButtonAt(local);
                    if (hit != _hoveredButton)
                    {
                        _hoveredButton = hit;
                        Invalidate();
                    }

                    return;
                }
                case NativeMethods.WM_NCMOUSELEAVE:
                    if (_hoveredButton != ButtonVisual.None)
                    {
                        _hoveredButton = ButtonVisual.None;
                        Invalidate();
                    }

                    return;
                case NativeMethods.WM_NCRBUTTONUP:
                    if (m.WParam.ToInt32() == NativeMethods.HTCAPTION)
                    {
                        ShowSystemMenu();
                        m.Result = IntPtr.Zero;
                        return;
                    }

                    break;
            }

            base.WndProc(ref m);
        }

        private static ButtonVisual ButtonFromHit(int code)
        {
            switch (code)
            {
                case HTMINBUTTON: return ButtonVisual.Minimize;
                case HTMAXBUTTON: return ButtonVisual.Maximize; // Restore shares the code
                case HTCLOSE: return ButtonVisual.Close;
                default: return ButtonVisual.Help;
            }
        }

        private static Point PointFromLParam(IntPtr lParam)
        {
            long v = lParam.ToInt64();
            return new Point(unchecked((short)(v & 0xFFFF)), unchecked((short)((v >> 16) & 0xFFFF)));
        }

        private IntPtr HitTest(IntPtr lParam)
        {
            Point local = PointToClient(PointFromLParam(lParam));
            Form form = Owner;

            foreach (ButtonVisual button in WindowButtons)
            {
                if (ButtonRect(button).Contains(local) && (form == null || form.Enabled))
                {
                    switch (button)
                    {
                        case ButtonVisual.Minimize: return new IntPtr(HTMINBUTTON);
                        case ButtonVisual.Maximize:
                        case ButtonVisual.Restore: return new IntPtr(HTMAXBUTTON);
                        case ButtonVisual.Close: return new IntPtr(HTCLOSE);
                        default: return new IntPtr(HTHELP);
                    }
                }
            }

            bool borderless = form == null || form.FormBorderStyle == FormBorderStyle.None;
            if (borderless && form != null && !IsMaximized && form.WindowState == FormWindowState.Normal)
            {
                int edge = Math.Max(Scale(5), SystemInformation.FrameBorderSize.Width);
                if (local.Y < edge)
                {
                    if (local.X < edge) return new IntPtr(NativeMethods.HTTOPLEFT);
                    if (local.X > Width - edge) return new IntPtr(NativeMethods.HTTOPRIGHT);
                    return new IntPtr(NativeMethods.HTTOP);
                }

                if (local.X < edge) return new IntPtr(NativeMethods.HTLEFT);
                if (local.X > Width - edge) return new IntPtr(NativeMethods.HTRIGHT);
            }

            return new IntPtr(NativeMethods.HTCAPTION);
        }

        private void InvokeButton(ButtonVisual button)
        {
            Form form = Owner;
            if (form == null)
            {
                return;
            }

            switch (button)
            {
                case ButtonVisual.Minimize:
                    form.WindowState = FormWindowState.Minimized;
                    break;
                case ButtonVisual.Maximize:
                    form.WindowState = FormWindowState.Maximized;
                    break;
                case ButtonVisual.Restore:
                    form.WindowState = FormWindowState.Normal;
                    break;
                case ButtonVisual.Close:
                    form.Close();
                    break;
                case ButtonVisual.Help:
                    HelpButtonClick?.Invoke(this, EventArgs.Empty);
                    break;
            }

            LayoutContent();
            Invalidate();
        }

        private void ShowSystemMenu()
        {
            Form form = Owner;
            if (form == null || !form.IsHandleCreated)
            {
                return;
            }

            IntPtr menu = GetSystemMenu(form.Handle, false);
            if (menu == IntPtr.Zero)
            {
                return;
            }

            Point cursor = Cursor.Position;
            int cmd = TrackPopupMenu(menu, TPM_LEFTALIGN | TPM_RETURNCMD | TPM_RIGHTBUTTON,
                cursor.X, cursor.Y, 0, form.Handle, IntPtr.Zero);
            if (cmd != 0)
            {
                NativeMethods.SendMessage(form.Handle, 0x0112, (IntPtr)cmd, IntPtr.Zero);
            }
        }

        private const uint TPM_LEFTALIGN = 0x0000;
        private const uint TPM_RETURNCMD = 0x0100;
        private const uint TPM_RIGHTBUTTON = 0x0002;

        [DllImport("user32.dll")]
        private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

        [DllImport("user32.dll")]
        private static extern int TrackPopupMenu(IntPtr hMenu, uint uFlags, int x, int y, int nReserved, IntPtr hWnd, IntPtr prcRect);

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (_hoveredButton != ButtonVisual.None)
            {
                _hoveredButton = ButtonVisual.None;
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Form form = Owner;
            bool backdropWindow = form is FluentWindow && ((FluentWindow)form).IsBackdropApplied;
            if (!backdropWindow)
            {
                // Paint an opaque chrome background: a transparent child would
                // render black in PrintWindow captures and leave stale pixels.
                Color back = ControlHelper.GetEffectiveBackground(this);
                if (back.A == 255)
                {
                    using (var brush = new SolidBrush(back))
                    {
                        g.FillRectangle(brush, ClientRectangle);
                    }
                }
                else
                {
                    ControlHelper.PaintFullBackground(this, g);
                }
            }
            else
            {
                ControlHelper.PaintFullBackground(this, g);
            }

            FluentGraphics.SetupGraphics(g);
            FluentPalette p = Palette;

            if (HasIcon && form != null)
            {
                int iconSize = Scale(16);
                var iconRect = new Rectangle(Scale(14), (Height - iconSize) / 2, iconSize, iconSize);
                g.DrawIconUnstretched(form.Icon, iconRect);
            }

            if (!string.IsNullOrEmpty(_title))
            {
                using (var format = new StringFormat(StringFormatFlags.NoWrap))
                {
                    format.LineAlignment = StringAlignment.Center;
                    format.Trimming = StringTrimming.EllipsisCharacter;
                    float left = TitleLeft();
                    var rect = new RectangleF(left, 0, Math.Max(0, Width - left - ButtonColumnWidth() - Scale(8)), Height);
                    FluentGraphics.DrawText(g, _title, FluentTypography.BodyStrong,
                        Enabled ? p.TextFillPrimary : p.TextFillDisabled, rect, format);
                }
            }

            PaintButton(g, ButtonVisual.Minimize, 0xE921, p);
            PaintButton(g, ButtonVisual.Restore, 0xE923, p);
            PaintButton(g, ButtonVisual.Maximize, 0xE922, p);
            PaintButton(g, ButtonVisual.Help, 0xE897, p);
            PaintButton(g, ButtonVisual.Close, 0xE8BB, p);
        }

        private bool HasIcon
        {
            get
            {
                Form form = Owner;
                return form != null && form.Icon != null;
            }
        }

        private float TitleLeft()
        {
            return Scale(14) + (HasIcon ? Scale(24) : 0);
        }

        private void PaintButton(Graphics g, ButtonVisual which, int glyph, FluentPalette p)
        {
            Rectangle rect = ButtonRect(which);
            if (rect.IsEmpty)
            {
                return;
            }

            bool hot = _hoveredButton == which || _pressedButton == which;
            if (hot)
            {
                Color hover = which == ButtonVisual.Close ? p.SystemFillCritical : p.SubtleFillSecondary;
                if (_pressedButton == which && which != ButtonVisual.Close)
                {
                    hover = p.SubtleFillTertiary;
                }

                using (var brush = new SolidBrush(ControlHelper.BlendOverParent(this, hover)))
                {
                    FluentGraphics.FillRoundedRect(g, brush, rect, Scale(4));
                }
            }

            Color fore = hot && which == ButtonVisual.Close ? Color.White : p.TextFillPrimary;
            FluentGraphics.DrawGlyph(g, FluentGraphics.Glyph(glyph), FluentTypography.Icon(10f), fore, rect);
        }
    }
}
