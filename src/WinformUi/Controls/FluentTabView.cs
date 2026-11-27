using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using WinformUi.Drawing;
using WinformUi.Theming;

namespace WinformUi.Controls
{
    /// <summary>A single page inside a <see cref="FluentTabView"/>.</summary>
    [ToolboxItem(false)]
    public class FluentTabViewPage : Panel
    {
        private SymbolRegular _icon = SymbolRegular.Empty;
        private bool _isClosable = true;

        public FluentTabViewPage()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Text = "Tab";
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            TabView?.LayoutTabs();
            TabView?.Invalidate();
        }

        [Category("Appearance")]
        [DefaultValue(SymbolRegular.Empty)]
        public SymbolRegular Icon
        {
            get { return _icon; }
            set
            {
                _icon = value;
                TabView?.Invalidate();
            }
        }

        [Category("Behavior")]
        [DefaultValue(true)]
        public bool IsClosable
        {
            get { return _isClosable; }
            set
            {
                _isClosable = value;
                TabView?.LayoutTabs();
                TabView?.Invalidate();
            }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        internal FluentTabView TabView
        {
            get { return Parent as FluentTabView; }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.Style |= 0x02000000; // WS_CLIPCHILDREN
                return cp;
            }
        }
    }

    /// <summary>Cancel-aware event args for adding a tab.</summary>
    public class TabViewTabAddingEventArgs : CancelEventArgs
    {
        public TabViewTabAddingEventArgs(FluentTabViewPage page)
        {
            Page = page;
        }

        public FluentTabViewPage Page { get; internal set; }
    }

    /// <summary>Cancel-aware event args for closing a tab.</summary>
    public class TabViewTabClosingEventArgs : CancelEventArgs
    {
        public TabViewTabClosingEventArgs(FluentTabViewPage page)
        {
            Page = page;
        }

        public FluentTabViewPage Page { get; internal set; }
    }

    /// <summary>
    /// Fluent browser-style tab view: closable tabs with hover-revealed buttons,
    /// an optional add button, drag-to-reorder, wrapping tab rows and themed content
    /// borders. Pages host real controls like a TabControl.
    /// </summary>
    [ToolboxItem(true)]
    [DefaultEvent("SelectedIndexChanged")]
    public class FluentTabView : FluentControlBase
    {
        private readonly List<FluentTabViewPage> _pages = new List<FluentTabViewPage>();
        private readonly List<Rectangle> _tabRects = new List<Rectangle>();
        private readonly List<Rectangle> _closeRects = new List<Rectangle>();
        private int _selected = -1;
        private bool _canReorderTabs = true;
        private bool _canAddTabs = true;
        private bool _addHovered;
        private bool _addPressed;
        private Rectangle _addRect;
        private int _hoveredTab = -1;
        private int _pressedTab = -1;
        private bool _reorderArmed;
        private bool _reordering;
        private Point _dragStart;
        private Rectangle _stripBounds;
        private int _tabHeight = 36;

        public FluentTabView()
        {
            Size = new Size(400, 240);
            SetStyle(ControlStyles.ContainerControl, true);
        }

        /// <summary>The tab pages. Assign pages before adding them to the control.</summary>
        [Category("Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public List<FluentTabViewPage> Pages => _pages;

        [Category("Behavior")]
        [DefaultValue(true)]
        public bool CanReorderTabs
        {
            get { return _canReorderTabs; }
            set { _canReorderTabs = value; }
        }

        [Category("Behavior")]
        [DefaultValue(true)]
        public bool CanAddTabs
        {
            get { return _canAddTabs; }
            set
            {
                _canAddTabs = value;
                LayoutTabs();
                Invalidate();
            }
        }

        [Category("Appearance")]
        [DefaultValue(36)]
        public int TabHeight
        {
            get { return _tabHeight; }
            set
            {
                _tabHeight = Math.Max(24, value);
                LayoutTabs();
                Invalidate();
            }
        }

        [Category("Behavior")]
        [DefaultValue(-1)]
        public int SelectedIndex
        {
            get { return _selected; }
            set
            {
                int clamped = value < -1 || value >= _pages.Count ? -1 : value;
                if (_selected == clamped)
                {
                    return;
                }

                _selected = clamped;
                ShowSelectedPage();
                Invalidate();
                SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public FluentTabViewPage SelectedPage
        {
            get { return _selected >= 0 && _selected < _pages.Count ? _pages[_selected] : null; }
            set { SelectedIndex = value == null ? -1 : _pages.IndexOf(value); }
        }

        public event EventHandler SelectedIndexChanged;
        public event EventHandler<TabViewTabAddingEventArgs> TabAdding;
        public event EventHandler<TabViewTabClosingEventArgs> TabClosing;

        protected virtual void OnTabAdding(TabViewTabAddingEventArgs e)
        {
            TabAdding?.Invoke(this, e);
        }

        protected virtual void OnTabClosing(TabViewTabClosingEventArgs e)
        {
            TabClosing?.Invoke(this, e);
        }

        /// <summary>
        /// Adds a tab. With no page, a default one is created; TabAdding can cancel
        /// the operation or replace/supply the page. The added tab becomes selected.
        /// </summary>
        public FluentTabViewPage AddTab(FluentTabViewPage page = null, string title = null)
        {
            var e = new TabViewTabAddingEventArgs(page);
            OnTabAdding(e);
            if (e.Cancel)
            {
                return null;
            }

            FluentTabViewPage target = e.Page;
            if (target == null)
            {
                target = new FluentTabViewPage { Text = title ?? "Tab " + (_pages.Count + 1) };
                e.Page = target;
            }

            AddPage(target, title);
            SelectedIndex = _pages.Count - 1;
            return target;
        }

        /// <summary>Registers a page with the strip without going through TabAdding.</summary>
        public void AddPage(FluentTabViewPage page, string title = null)
        {
            if (page == null || _pages.Contains(page))
            {
                return;
            }

            if (title != null)
            {
                page.Text = title;
            }

            _pages.Add(page);
            page.Visible = false;
            Controls.Add(page);
            LayoutTabs();
            if (_selected < 0)
            {
                SelectedIndex = 0;
            }

            Invalidate();
        }

        /// <summary>Removes a tab from the strip; raises TabClosing and honors Cancel.</summary>
        public bool CloseTab(FluentTabViewPage page)
        {
            if (page == null || !_pages.Contains(page))
            {
                return false;
            }

            var e = new TabViewTabClosingEventArgs(page);
            OnTabClosing(e);
            if (e.Cancel)
            {
                return false;
            }

            int index = _pages.IndexOf(page);
            _pages.RemoveAt(index);
            Controls.Remove(page);
            if (_selected == index)
            {
                SelectedIndex = _pages.Count == 0 ? -1 : Math.Min(index, _pages.Count - 1);
            }
            else if (_selected > index)
            {
                _selected--;
            }

            LayoutTabs();
            Invalidate();
            return true;
        }

        internal void LayoutTabs()
        {
            _tabRects.Clear();
            _closeRects.Clear();
            if (!IsHandleCreated)
            {
                return;
            }

            int rowHeight = Scale(TabHeight);
            int x = Scale(4);
            int y = Scale(4);
            int rightReserve = CanAddTabs ? Scale(36) + Scale(4) : Scale(4);
            int maxRight = Width - rightReserve;

            using (var measure = new Bitmap(1, 1))
            using (Graphics mg = Graphics.FromImage(measure))
            {
                FluentGraphics.SetupGraphics(mg);
                Font font = FluentTypography.Body;
                for (int i = 0; i < _pages.Count; i++)
                {
                    FluentTabViewPage page = _pages[i];
                    Size text = Size.Round(mg.MeasureString(page.Text ?? string.Empty, font));
                    int width = Scale(12);
                    if (page.Icon != SymbolRegular.Empty)
                    {
                        width += Scale(16) + Scale(4);
                    }

                    width += Math.Max(Scale(48), text.Width) + Scale(4);
                    if (page.IsClosable)
                    {
                        width += Scale(24);
                    }

                    if (x > Scale(4) && x + width > maxRight)
                    {
                        x = Scale(4);
                        y += rowHeight;
                    }

                    _tabRects.Add(new Rectangle(x, y, width, rowHeight - Scale(2)));
                    int closeLeft = x + width - Scale(24);
                    _closeRects.Add(new Rectangle(closeLeft, y + (rowHeight - Scale(2) - Scale(20)) / 2, Scale(20), Scale(20)));
                    x += width + Scale(1);
                }
            }

            int stripBottom = _pages.Count == 0 ? y : y + rowHeight;
            _stripBounds = new Rectangle(0, 0, Width, stripBottom);
            _addRect = CanAddTabs
                ? new Rectangle(Width - Scale(36) - Scale(4), Scale(4), Scale(36), Scale(36))
                : Rectangle.Empty;

            LayoutPages(stripBottom);
        }

        private void LayoutPages(int stripBottom)
        {
            var content = new Rectangle(Scale(1), stripBottom + Scale(1),
                Math.Max(0, Width - Scale(2)), Math.Max(0, Height - stripBottom - Scale(3)));
            foreach (FluentTabViewPage page in _pages)
            {
                if (page.Visible)
                {
                    page.Bounds = content;
                }
            }
        }

        private void ShowSelectedPage()
        {
            for (int i = 0; i < _pages.Count; i++)
            {
                _pages[i].Visible = i == _selected;
                if (i == _selected)
                {
                    LayoutPages(_stripBounds.Bottom);
                }
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            LayoutTabs();
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            LayoutTabs();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            LayoutTabs();
            ShowSelectedPage();
            ApplyTabTheme();
        }

        protected override void OnThemeChanged()
        {
            base.OnThemeChanged();
            ApplyTabTheme();
        }

        private void ApplyTabTheme()
        {
            foreach (FluentTabViewPage page in _pages)
            {
                page.BackColor = Color.Transparent;
            }

            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            ControlHelper.PaintFullBackground(this, g);
            FluentGraphics.SetupGraphics(g);

            // Unselected tabs first, then the selected one so its border overlaps neighbors.
            for (int i = 0; i < _tabRects.Count; i++)
            {
                if (i != _selected)
                {
                    PaintTab(g, i, false);
                }
            }

            if (_selected >= 0 && _selected < _tabRects.Count)
            {
                PaintTab(g, _selected, true);
            }

            if (CanAddTabs)
            {
                PaintAddButton(g);
            }

            // Content border: 1px hairline with bottom corners rounded.
            var bounds = new RectangleF(_stripBounds.Right - _stripBounds.Width + 0.5f, _stripBounds.Bottom + 0.5f,
                Width - 1f, Height - _stripBounds.Bottom - 1f);
            using (var pen = new Pen(Palette.DividerStrokeDefault, 1f))
            using (var path = RoundedContentPath(bounds))
            {
                g.DrawPath(pen, path);
            }
        }

        private System.Drawing.Drawing2D.GraphicsPath RoundedContentPath(RectangleF bounds)
        {
            float radius = FluentGraphics.ControlCornerRadius;
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.StartFigure();
            path.AddLine(bounds.X, bounds.Y, bounds.Right, bounds.Y);
            path.AddArc(bounds.Right - radius * 2, bounds.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
            path.AddLine(bounds.X + radius, bounds.Bottom, bounds.X, bounds.Bottom);
            path.AddArc(bounds.X, bounds.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
            path.CloseFigure();
            return path;
        }

        private void PaintTab(Graphics g, int index, bool selected)
        {
            if (index >= _pages.Count)
            {
                return;
            }

            FluentTabViewPage page = _pages[index];
            Rectangle rect = _tabRects[index];
            var bounds = new RectangleF(rect.X + 0.5f, rect.Y + 0.5f, rect.Width - 1f, rect.Height);
            float radius = Scale(8);

            Color fill;
            Color textColor;
            if (selected)
            {
                fill = ControlHelper.BlendOverParent(this, Palette.ControlFillDefault);
                textColor = Palette.TextFillPrimary;
            }
            else
            {
                bool hovered = index == _hoveredTab;
                fill = hovered ? ControlHelper.BlendOverParent(this, Palette.SubtleFillSecondary) : Color.Transparent;
                textColor = index == _pressedTab ? Palette.TextFillSecondary : Palette.TextFillPrimary;
            }

            using (var path = TopRoundedPath(bounds, radius))
            {
                if (fill.A > 0 && fill != Color.Transparent)
                {
                    using (var brush = new SolidBrush(fill))
                    {
                        g.FillPath(brush, path);
                    }
                }

                if (selected)
                {
                    using (var pen = new Pen(Palette.DividerStrokeDefault, 1f))
                    {
                        g.DrawPath(pen, path);
                    }

                    using (var brush = new SolidBrush(Palette.SystemAccentColorPrimary))
                    {
                        g.FillRectangle(brush, bounds.X + radius, bounds.Y - 0.5f, bounds.Width - radius * 2, Scale(2));
                    }
                }
            }

            float x = rect.X + Scale(6);
            if (page.Icon != SymbolRegular.Empty)
            {
                FluentGraphics.DrawGlyph(g, FluentGraphics.Glyph((int)page.Icon), FluentTypography.IconRegular(9f),
                    selected ? textColor : Palette.TextFillSecondary,
                    new RectangleF(x, rect.Y + (rect.Height - Scale(16)) / 2f, Scale(16), Scale(16)));
                x += Scale(16) + Scale(4);
            }

            var textRect = new RectangleF(x, rect.Y, rect.Right - (page.IsClosable ? Scale(24) : Scale(6)) - x, rect.Height);
            using (var format = new StringFormat(StringFormatFlags.NoWrap))
            {
                format.Alignment = StringAlignment.Near;
                format.LineAlignment = StringAlignment.Center;
                format.Trimming = StringTrimming.EllipsisCharacter;
                FluentGraphics.DrawText(g, page.Text, FluentTypography.Body,
                    Enabled ? textColor : Palette.TextFillDisabled,
                    textRect, format);
            }

            if (page.IsClosable)
            {
                bool hot = index == _hoveredTab || index == _pressedTab;
                Rectangle close = _closeRects[index];
                if (hot)
                {
                    var box = new RectangleF(close.X, close.Y, close.Width, close.Height);
                    Color buttonFill = index == _pressedTab
                        ? ControlHelper.BlendOverParent(this, Palette.SubtleFillTertiary)
                        : ControlHelper.BlendOverParent(this, Palette.SubtleFillSecondary);
                    using (var brush = new SolidBrush(buttonFill))
                    {
                        FluentGraphics.FillRoundedRect(g, brush, box, Scale(4));
                    }
                }

                FluentGraphics.DrawGlyph(g, FluentGraphics.Glyph(0xE711), FluentTypography.Icon(8f),
                    hot ? Palette.TextFillPrimary : Palette.TextFillSecondary,
                    new RectangleF(close.X, close.Y, close.Width, close.Height));
            }
        }

        private System.Drawing.Drawing2D.GraphicsPath TopRoundedPath(RectangleF bounds, float radius)
        {
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.StartFigure();
            path.AddArc(bounds.X, bounds.Y, radius * 2, radius * 2, 180, 90);
            path.AddArc(bounds.Right - radius * 2, bounds.Y, radius * 2, radius * 2, 270, 90);
            path.AddLine(bounds.Right, bounds.Bottom + 1, bounds.X, bounds.Bottom + 1);
            path.CloseFigure();
            return path;
        }

        private void PaintAddButton(Graphics g)
        {
            if (_addRect.Width == 0)
            {
                return;
            }

            var bounds = new RectangleF(_addRect.X, _addRect.Y, _addRect.Width, _addRect.Height);
            if (_addHovered || _addPressed)
            {
                Color fill = _addPressed
                    ? ControlHelper.BlendOverParent(this, Palette.SubtleFillTertiary)
                    : ControlHelper.BlendOverParent(this, Palette.SubtleFillSecondary);
                using (var brush = new SolidBrush(fill))
                {
                    FluentGraphics.FillRoundedRect(g, brush, bounds, Scale(4));
                }
            }

            FluentGraphics.DrawGlyph(g, FluentGraphics.Glyph(0xE710), FluentTypography.IconRegular(12f),
                Palette.TextFillPrimary, bounds);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int tab = HitTestTab(e.Location);
            bool add = CanAddTabs && _addRect.Contains(e.Location);
            if (tab != _hoveredTab || add != _addHovered)
            {
                _hoveredTab = tab;
                _addHovered = add;
                Invalidate();
            }

            if (_reorderArmed && !_reordering && _pressedTab >= 0 &&
                Math.Abs(e.X - _dragStart.X) + Math.Abs(e.Y - _dragStart.Y) > SystemInformation.DragSize.Width)
            {
                _reordering = true;
            }

            if (_reordering && CanReorderTabs && tab >= 0 && tab != _pressedTab && tab < _pages.Count)
            {
                MoveTab(_pressedTab, tab);
                _pressedTab = tab;
            }
        }

        private void MoveTab(int from, int to)
        {
            FluentTabViewPage moved = _pages[from];
            FluentTabViewPage selectedPage = SelectedPage;
            _pages.RemoveAt(from);
            _pages.Insert(to, moved);
            _selected = _pages.IndexOf(selectedPage);
            LayoutTabs();
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            if (CanAddTabs && _addRect.Contains(e.Location))
            {
                _addPressed = true;
                Invalidate();
                return;
            }

            int tab = HitTestTab(e.Location);
            if (tab < 0)
            {
                return;
            }

            if (_pages[tab].IsClosable && _closeRects[tab].Contains(e.Location))
            {
                CloseTab(_pages[tab]);
                return;
            }

            _pressedTab = tab;
            _reorderArmed = CanReorderTabs;
            _dragStart = e.Location;
            SelectedIndex = tab;
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (_addPressed)
            {
                _addPressed = false;
                if (_addRect.Contains(e.Location))
                {
                    AddTab();
                }

                Invalidate();
            }

            _pressedTab = -1;
            _reorderArmed = false;
            _reordering = false;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hoveredTab = -1;
            _addHovered = false;
            Invalidate();
        }

        private int HitTestTab(Point pt)
        {
            for (int i = 0; i < _tabRects.Count; i++)
            {
                if (_tabRects[i].Contains(pt))
                {
                    return i;
                }
            }

            return -1;
        }

        protected override bool IsInputKey(Keys keyData)
        {
            if (keyData == Keys.Left || keyData == Keys.Right)
            {
                return true;
            }

            return base.IsInputKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Left && _selected > 0)
            {
                SelectedIndex = _selected - 1;
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Right && _selected < _pages.Count - 1)
            {
                SelectedIndex = _selected + 1;
                e.Handled = true;
            }
        }
    }
}
