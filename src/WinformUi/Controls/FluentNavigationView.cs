using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using WinformUi.Drawing;
using WinformUi.Theming;

namespace WinformUi.Controls
{
    /// <summary>Base class for navigable pages.</summary>
    public class FluentPage : UserControl
    {
        private ThemeSubscription _themeSubscription;

        public FluentPage()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            Font = FluentTypography.Body;
            // Opaque layer surface: transparent backcolors here would force every
            // descendant into the recursive parent-paint transparency simulation,
            // which leaves stale sibling pixels behind on live HWNDs.
            BackColor = SurfaceColor;
            AutoScaleMode = AutoScaleMode.Dpi;
            Padding = new Padding(24, 16, 24, 24);
        }

        protected Color SurfaceColor => ControlHelper.BlendOverParent(this, ApplicationThemeManager.Palette.LayerFillDefault);

        protected override void OnPaint(PaintEventArgs e)
        {
            ControlHelper.PaintFullBackground(this, e.Graphics);
            base.OnPaint(e);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (_themeSubscription == null)
            {
                _themeSubscription = new ThemeSubscription(this, () => BackColor = SurfaceColor);
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

    /// <summary>
    /// Navigation host with history. Pages are cached by type and
    /// docked into the frame; the surface gets the Fluent layer treatment.
    /// </summary>
    [System.ComponentModel.ToolboxItem(false)]
    public class FluentFrame : Panel
    {
        private readonly Dictionary<Type, Control> _cache = new Dictionary<Type, Control>();
        private readonly List<Type> _history = new List<Type>();
        private int _historyIndex = -1;
        private ThemeSubscription _themeSubscription;

        public event EventHandler Navigated;

        public FluentFrame()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Padding = new Padding(1);
        }

        [Browsable(false)]
        public Control CurrentPage { get; private set; }

        [Browsable(false)]
        public bool CanGoBack => _historyIndex > 0;

        [Browsable(false)]
        public bool CanGoForward => _historyIndex >= 0 && _historyIndex < _history.Count - 1;

        protected FluentPalette Palette => ApplicationThemeManager.Palette;

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (_themeSubscription == null)
            {
                _themeSubscription = new ThemeSubscription(this, () => Invalidate());
            }
        }

        public Control Navigate(Type pageType)
        {
            if (pageType == null || !typeof(Control).IsAssignableFrom(pageType))
            {
                throw new ArgumentException("pageType must be a Control type.", nameof(pageType));
            }

            Control page;
            if (!_cache.TryGetValue(pageType, out page))
            {
                page = (Control)Activator.CreateInstance(pageType);
                page.Dock = DockStyle.Fill;
                _cache[pageType] = page;
                Controls.Add(page);
            }

            CurrentPage = page;
            page.BringToFront();
            page.Visible = true;

            foreach (Control other in Controls)
            {
                if (other != page)
                {
                    other.Visible = false;
                }
            }

            if (_historyIndex < 0 || _history[_historyIndex] != pageType)
            {
                if (_historyIndex < _history.Count - 1)
                {
                    _history.RemoveRange(_historyIndex + 1, _history.Count - _historyIndex - 1);
                }

                _history.Add(pageType);
                _historyIndex = _history.Count - 1;
            }

            OnNavigated(EventArgs.Empty);
            ScheduleRepaint();
            return page;
        }

        public T Navigate<T>() where T : Control
        {
            return (T)Navigate(typeof(T));
        }

        public void GoBack()
        {
            if (CanGoBack)
            {
                _historyIndex--;
                NavigateCached(_history[_historyIndex]);
            }
        }

        public void GoForward()
        {
            if (CanGoForward)
            {
                _historyIndex++;
                NavigateCached(_history[_historyIndex]);
            }
        }

        private void NavigateCached(Type pageType)
        {
            Control page = _cache[pageType];
            CurrentPage = page;
            page.BringToFront();
            foreach (Control other in Controls)
            {
                other.Visible = other == page;
            }

            OnNavigated(EventArgs.Empty);
            ScheduleRepaint();
        }

        private void ScheduleRepaint()
        {
            if (CurrentPage == null)
            {
                return;
            }

            // AutoSize/flow layout settles over several message-loop passes; a
            // repaint queued behind them clears surfaces painted mid-settle.
            if (IsHandleCreated)
            {
                BeginInvoke((Action)(() =>
                {
                    CurrentPage.Invalidate(true);
                    CurrentPage.Refresh();
                }));
            }
            else
            {
                CurrentPage.Invalidate(true);
            }
        }

        protected virtual void OnNavigated(EventArgs e)
        {
            Navigated?.Invoke(this, e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            ControlHelper.PaintFullBackground(this, e.Graphics);
            Graphics g = e.Graphics;
            FluentGraphics.SetupGraphics(g);

            Color fill = ControlHelper.BlendOverParent(this, Palette.LayerFillDefault);
            var bounds = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);

            // Layer with rounded top-left corner, like the WinUI NavigationView content grid.
            using (var path = new System.Drawing.Drawing2D.GraphicsPath())
            {
                float r = 8f;
                path.AddArc(bounds.X, bounds.Y, r * 2, r * 2, 180, 90);
                path.AddLine(bounds.X + r, bounds.Y, bounds.Right, bounds.Y);
                path.AddLine(bounds.Right, bounds.Y, bounds.Right, bounds.Bottom);
                path.AddLine(bounds.Right, bounds.Bottom, bounds.X, bounds.Bottom);
                path.CloseFigure();

                using (var brush = new SolidBrush(fill))
                {
                    g.FillPath(brush, path);
                }

                using (var pen = new Pen(Palette.CardStrokeDefault, 1f))
                {
                    g.DrawPath(pen, path);
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
    }

    /// <summary>A NavigationView menu entry.</summary>
    public class NavigationViewItem
    {
        public string Content { get; set; }
        public SymbolRegular Icon { get; set; }
        public Type TargetPageType { get; set; }
        public object Tag { get; set; }

        public NavigationViewItem()
        {
        }

        public NavigationViewItem(string content, SymbolRegular icon, Type targetPageType)
        {
            Content = content;
            Icon = icon;
            TargetPageType = targetPageType;
        }

        public override string ToString()
        {
            return Content ?? string.Empty;
        }
    }

    /// <summary>
    /// Fluent navigation view: left menu pane with icons,
    /// accent selection pill, hamburger compact mode, and a layered content frame.
    /// </summary>
    public class FluentNavigationView : Control
    {
        private const int ItemHeight = 38;
        private const int HamburgerRowHeight = 48;

        private readonly List<NavigationViewItem> _menuItems = new List<NavigationViewItem>();
        private readonly List<NavigationViewItem> _footerItems = new List<NavigationViewItem>();
        private readonly FluentFrame _frame;
        private ThemeSubscription _themeSubscription;
        private NavigationViewItem _selectedItem;
        private int _hoverIndex = -1; // index into combined hit list
        private bool _isPaneOpen = true;
        private int _paneWidth = 220;
        private bool _hamburgerHovered;

        public event EventHandler SelectionChanged;

        public FluentNavigationView()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            Font = FluentTypography.Body;
            Dock = DockStyle.Fill;

            _frame = new FluentFrame { Dock = DockStyle.None };
            Controls.Add(_frame);
            UpdateFrameBounds();
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public List<NavigationViewItem> MenuItems => _menuItems;

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public List<NavigationViewItem> FooterMenuItems => _footerItems;

        /// <summary>The content frame; navigate it directly for advanced scenarios.</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public FluentFrame Frame => _frame;

        [DefaultValue(true)]
        [Category("Fluent")]
        public bool IsPaneOpen
        {
            get { return _isPaneOpen; }
            set
            {
                if (_isPaneOpen != value)
                {
                    _isPaneOpen = value;
                    UpdateFrameBounds();
                    Invalidate();
                }
            }
        }

        [DefaultValue(220)]
        [Category("Fluent")]
        public int PaneWidth
        {
            get { return _paneWidth; }
            set
            {
                _paneWidth = value;
                UpdateFrameBounds();
                Invalidate();
            }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public NavigationViewItem SelectedItem
        {
            get { return _selectedItem; }
            set { SelectItem(value, true); }
        }

        protected FluentPalette Palette => ApplicationThemeManager.Palette;

        private int EffectivePaneWidth => _isPaneOpen ? _paneWidth : 48;

        private void UpdateFrameBounds()
        {
            _frame.Bounds = new Rectangle(EffectivePaneWidth, 0, Math.Max(0, Width - EffectivePaneWidth), Height);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdateFrameBounds();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (_themeSubscription == null)
            {
                _themeSubscription = new ThemeSubscription(this, () => Invalidate());
            }
        }

        public void SelectItem(NavigationViewItem item, bool navigate)
        {
            if (item == null || _selectedItem == item)
            {
                return;
            }

            _selectedItem = item;
            if (navigate && item.TargetPageType != null)
            {
                _frame.Navigate(item.TargetPageType);
            }

            OnSelectionChanged(EventArgs.Empty);
            Invalidate();
        }

        protected virtual void OnSelectionChanged(EventArgs e)
        {
            SelectionChanged?.Invoke(this, e);
        }

        private RectangleF HamburgerRect => new RectangleF(4, 4, 40, 40);

        private RectangleF ItemRect(int index)
        {
            return new RectangleF(4, HamburgerRowHeight + index * ItemHeight, EffectivePaneWidth - 8, ItemHeight - 2);
        }

        private RectangleF FooterItemRect(int index)
        {
            float y = Height - (_footerItems.Count - index) * ItemHeight - 4;
            return new RectangleF(4, y, EffectivePaneWidth - 8, ItemHeight - 2);
        }

        /// <summary>Returns (list, index) for a point: 0 hamburger, 1 menu, 2 footer.</summary>
        private int HitTest(Point point, out int index)
        {
            index = -1;
            if (HamburgerRect.Contains(point))
            {
                return 0;
            }

            for (int i = 0; i < _menuItems.Count; i++)
            {
                if (ItemRect(i).Contains(point))
                {
                    index = i;
                    return 1;
                }
            }

            for (int i = 0; i < _footerItems.Count; i++)
            {
                if (FooterItemRect(i).Contains(point))
                {
                    index = i;
                    return 2;
                }
            }

            return -1;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int index;
            int zone = HitTest(e.Location, out index);
            int hoverKey = zone == 1 ? index : (zone == 2 ? 1000 + index : -1);
            bool hamburger = zone == 0;

            if (_hoverIndex != hoverKey || _hamburgerHovered != hamburger)
            {
                _hoverIndex = hoverKey;
                _hamburgerHovered = hamburger;
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hoverIndex = -1;
            _hamburgerHovered = false;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            int index;
            int zone = HitTest(e.Location, out index);
            if (zone == 0)
            {
                IsPaneOpen = !IsPaneOpen;
            }
            else if (zone == 1)
            {
                SelectItem(_menuItems[index], true);
            }
            else if (zone == 2)
            {
                SelectItem(_footerItems[index], true);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            ControlHelper.PaintFullBackground(this, e.Graphics);
            Graphics g = e.Graphics;
            FluentGraphics.SetupGraphics(g);

            // Hamburger
            if (_hamburgerHovered)
            {
                using (var brush = new SolidBrush(ControlHelper.BlendOverParent(this, Palette.SubtleFillSecondary)))
                {
                    FluentGraphics.FillRoundedRect(g, brush, HamburgerRect, FluentGraphics.ControlCornerRadius);
                }
            }

            FluentGraphics.DrawGlyph(g, FluentGraphics.Glyph(0xE700), FluentTypography.Icon(12f), Palette.TextFillPrimary, HamburgerRect);

            // Menu items
            for (int i = 0; i < _menuItems.Count; i++)
            {
                DrawItem(g, _menuItems[i], ItemRect(i), _hoverIndex == i);
            }

            // Separator above footer
            if (_footerItems.Count > 0)
            {
                float y = Height - _footerItems.Count * ItemHeight - 8;
                using (var pen = new Pen(ControlHelper.BlendOverParent(this, Palette.DividerStrokeDefault), 1f))
                {
                    g.DrawLine(pen, 8, y, EffectivePaneWidth - 8, y);
                }
            }

            for (int i = 0; i < _footerItems.Count; i++)
            {
                DrawItem(g, _footerItems[i], FooterItemRect(i), _hoverIndex == 1000 + i);
            }
        }

        private void DrawItem(Graphics g, NavigationViewItem item, RectangleF rect, bool hovered)
        {
            bool selected = item == _selectedItem;

            Color fill = Color.Transparent;
            if (selected || hovered)
            {
                fill = ControlHelper.BlendOverParent(this, Palette.SubtleFillSecondary);
            }

            if (fill.A > 0)
            {
                using (var brush = new SolidBrush(fill))
                {
                    FluentGraphics.FillRoundedRect(g, brush, rect, FluentGraphics.ControlCornerRadius);
                }
            }

            if (selected)
            {
                using (var brush = new SolidBrush(Palette.SystemAccentColorPrimary))
                {
                    FluentGraphics.FillRoundedRect(g, brush, new RectangleF(rect.X + 2, rect.Y + (rect.Height - 16) / 2f, 3, 16), 1.5f);
                }
            }

            float iconSize = 16f;
            float iconX = _isPaneOpen ? rect.X + 12 : rect.X + (rect.Width - iconSize) / 2f;
            var iconRect = new RectangleF(iconX, rect.Y + (rect.Height - iconSize) / 2f, iconSize, iconSize);
            Color glyphColor = selected ? Palette.TextFillPrimary : Palette.TextFillSecondary;
            if (item.Icon != SymbolRegular.Empty)
            {
                FluentGraphics.DrawGlyph(g, FluentGraphics.Glyph((int)item.Icon), FluentTypography.IconRegular(12f), glyphColor, iconRect);
            }

            if (_isPaneOpen)
            {
                using (var format = new StringFormat { LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap, Trimming = StringTrimming.EllipsisCharacter })
                {
                    FluentGraphics.DrawText(g, item.Content ?? string.Empty, Font, Palette.TextFillPrimary, new RectangleF(rect.X + 40, rect.Y, rect.Width - 48, rect.Height), format);
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
    }
}