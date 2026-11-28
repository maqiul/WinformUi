using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using WinformUi.Drawing;
using WinformUi.Theming;

namespace WinformUi.Controls
{
    /// <summary>Specifies whether a navigated page is cached by its frame.</summary>
    public enum NavigationCacheMode
    {
        Disabled,
        Enabled,
        Required
    }

    /// <summary>Identifies how a page was reached.</summary>
    public enum NavigationMode
    {
        New,
        Back,
        Forward,
        Refresh
    }

    /// <summary>Describes a page navigation and its parameter.</summary>
    public sealed class NavigationEventArgs : EventArgs
    {
        public NavigationEventArgs(Type pageType, object parameter, NavigationMode mode, NavigationCacheMode cacheMode)
        {
            PageType = pageType;
            Parameter = parameter;
            Mode = mode;
            CacheMode = cacheMode;
        }

        public Type PageType { get; private set; }
        public object Parameter { get; private set; }
        public NavigationMode Mode { get; private set; }
        public NavigationCacheMode CacheMode { get; private set; }
    }

    /// <summary>Implemented by pages that need navigation lifecycle callbacks.</summary>
    public interface INavigationAware
    {
        void OnNavigatedTo(NavigationEventArgs e);
        void OnNavigatedFrom(NavigationEventArgs e);
    }

    /// <summary>Base class for navigable pages.</summary>
    public class FluentPage : UserControl, INavigationAware
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

        [Category("Fluent")]
        [DefaultValue(NavigationCacheMode.Enabled)]
        public NavigationCacheMode NavigationCacheMode { get; set; } = NavigationCacheMode.Enabled;

        void INavigationAware.OnNavigatedTo(NavigationEventArgs e)
        {
            OnNavigatedTo(e);
        }

        void INavigationAware.OnNavigatedFrom(NavigationEventArgs e)
        {
            OnNavigatedFrom(e);
        }

        /// <summary>Called after this page becomes the current page.</summary>
        protected virtual void OnNavigatedTo(NavigationEventArgs e)
        {
        }

        /// <summary>Called before this page stops being the current page.</summary>
        protected virtual void OnNavigatedFrom(NavigationEventArgs e)
        {
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
                _themeSubscription = new ThemeSubscription(this, QueueSurfaceColor);
            }
        }

        // The theme event reaches a page before its window applies the new background, so
        // blending then would freeze the page on the previous theme's color. Posting the
        // update lets the whole notification, window included, finish first.
        private void QueueSurfaceColor()
        {
            if (IsHandleCreated && !IsDisposed)
            {
                BeginInvoke((Action)UpdateSurfaceColor);
            }
        }

        private void UpdateSurfaceColor()
        {
            if (!IsDisposed)
            {
                BackColor = SurfaceColor;
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
    /// Navigation host with parameter-aware history and configurable page caching.
    /// </summary>
    [System.ComponentModel.ToolboxItem(false)]
    public class FluentFrame : Panel
    {
        private sealed class NavigationKey : IEquatable<NavigationKey>
        {
            private readonly int _hashCode;

            public NavigationKey(Type pageType, object parameter)
            {
                PageType = pageType;
                Parameter = parameter;
                try
                {
                    _hashCode = pageType.GetHashCode() ^ (parameter == null ? 0 : parameter.GetHashCode());
                }
                catch
                {
                    _hashCode = pageType.GetHashCode();
                }
            }

            public Type PageType { get; private set; }
            public object Parameter { get; private set; }

            public bool Equals(NavigationKey other)
            {
                return other != null && PageType == other.PageType && object.Equals(Parameter, other.Parameter);
            }

            public override bool Equals(object obj)
            {
                return Equals(obj as NavigationKey);
            }

            public override int GetHashCode()
            {
                return _hashCode;
            }
        }

        private sealed class HistoryEntry
        {
            public NavigationKey Key;
        }

        private sealed class CacheEntry
        {
            public NavigationKey Key;
            public Control Page;
        }

        private readonly Dictionary<NavigationKey, CacheEntry> _cache = new Dictionary<NavigationKey, CacheEntry>();
        private readonly List<CacheEntry> _cacheOrder = new List<CacheEntry>();
        private readonly List<HistoryEntry> _history = new List<HistoryEntry>();
        private int _historyIndex = -1;
        private int _cacheSize = 10;
        private NavigationKey _currentKey;
        private ThemeSubscription _themeSubscription;

        public event EventHandler Navigated;

        public FluentFrame()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Padding = new Padding(1);
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Control CurrentPage { get; private set; }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Type CurrentPageType => _currentKey?.PageType;

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public object CurrentParameter => _currentKey?.Parameter;

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public NavigationCacheMode CurrentCacheMode { get; private set; }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool CanGoBack => _historyIndex > 0;

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool CanGoForward => _historyIndex >= 0 && _historyIndex < _history.Count - 1;

        /// <summary>Maximum number of Enabled pages retained in the cache. Required pages are never counted.</summary>
        [DefaultValue(10)]
        public int CacheSize
        {
            get { return _cacheSize; }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                _cacheSize = value;
                TrimCache(CurrentPage);
            }
        }

        /// <summary>Optional factory used instead of the page type's parameterless constructor.</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Func<Type, object, Control> PageFactory { get; set; }

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
            return Navigate(pageType, null);
        }

        public Control Navigate(Type pageType, object parameter)
        {
            return NavigateCore(pageType, parameter, NavigationMode.New, true);
        }

        public T Navigate<T>() where T : Control
        {
            return (T)Navigate(typeof(T));
        }

        public T Navigate<T>(object parameter) where T : Control
        {
            return (T)Navigate(typeof(T), parameter);
        }

        public void GoBack()
        {
            if (CanGoBack)
            {
                _historyIndex--;
                NavigateCore(_history[_historyIndex], NavigationMode.Back);
            }
        }

        public void GoForward()
        {
            if (CanGoForward)
            {
                _historyIndex++;
                NavigateCore(_history[_historyIndex], NavigationMode.Forward);
            }
        }

        private void NavigateCore(HistoryEntry entry, NavigationMode mode)
        {
            NavigateCore(entry.Key.PageType, entry.Key.Parameter, mode, false);
        }

        private Control NavigateCore(Type pageType, object parameter, NavigationMode mode, bool addToHistory)
        {
            if (pageType == null || !typeof(Control).IsAssignableFrom(pageType))
            {
                throw new ArgumentException("pageType must be a Control type.", nameof(pageType));
            }

            var key = new NavigationKey(pageType, parameter);
            bool samePage = _currentKey != null && _currentKey.Equals(key) && CurrentPage != null && !CurrentPage.IsDisposed;

            if (samePage)
            {
                NotifyPage(CurrentPage, true, new NavigationEventArgs(pageType, parameter, NavigationMode.Refresh, CurrentCacheMode));
                OnNavigated(new NavigationEventArgs(pageType, parameter, NavigationMode.Refresh, CurrentCacheMode));
                ScheduleRepaint();
                return CurrentPage;
            }

            Control target = GetOrCreatePage(key);

            if (CurrentPage != null && !CurrentPage.IsDisposed)
            {
                NotifyPage(CurrentPage, false, new NavigationEventArgs(CurrentPageType, CurrentParameter, mode, CurrentCacheMode));
                if (CurrentCacheMode == NavigationCacheMode.Disabled || (CurrentCacheMode == NavigationCacheMode.Enabled && _cacheSize == 0))
                {
                    DisposePage(CurrentPage);
                }
            }

            if (addToHistory)
            {
                if (_historyIndex >= 0 && _historyIndex < _history.Count - 1)
                {
                    _history.RemoveRange(_historyIndex + 1, _history.Count - _historyIndex - 1);
                }

                _history.Add(new HistoryEntry { Key = key });
                _historyIndex = _history.Count - 1;
            }

            _currentKey = key;
            CurrentPage = target;
            CurrentCacheMode = GetCacheMode(target);
            target.Dock = DockStyle.Fill;
            target.BringToFront();
            target.Visible = true;

            foreach (Control other in Controls)
            {
                if (other != target)
                {
                    other.Visible = false;
                }
            }

            NotifyPage(target, true, new NavigationEventArgs(pageType, parameter, mode, CurrentCacheMode));
            OnNavigated(new NavigationEventArgs(pageType, parameter, mode, CurrentCacheMode));
            ScheduleRepaint();
            return target;
        }

        private Control GetOrCreatePage(NavigationKey key)
        {
            CacheEntry cached;
            if (_cache.TryGetValue(key, out cached))
            {
                if (!cached.Page.IsDisposed)
                {
                    _cacheOrder.Remove(cached);
                    _cacheOrder.Add(cached);
                    return cached.Page;
                }

                // Callers may dispose a page they were handed back; drop the stale entry
                // and fall through to creating a fresh one.
                _cacheOrder.Remove(cached);
                _cache.Remove(key);
            }

            Control page = PageFactory == null ? Activator.CreateInstance(key.PageType) as Control : PageFactory(key.PageType, key.Parameter);
            if (page == null)
            {
                throw new InvalidOperationException("The page factory did not create a Control.");
            }

            page.Dock = DockStyle.Fill;
            Controls.Add(page);
            page.Visible = false;

            NavigationCacheMode mode = GetCacheMode(page);
            if (mode != NavigationCacheMode.Disabled && (mode == NavigationCacheMode.Required || _cacheSize > 0))
            {
                var entry = new CacheEntry { Key = key, Page = page };
                _cache[key] = entry;
                _cacheOrder.Add(entry);
                TrimCache(CurrentPage, page);
            }

            return page;
        }

        private NavigationCacheMode GetCacheMode(Control page)
        {
            var fluentPage = page as FluentPage;
            return fluentPage?.NavigationCacheMode ?? NavigationCacheMode.Enabled;
        }

        private void NotifyPage(Control page, bool entering, NavigationEventArgs e)
        {
            var aware = page as INavigationAware;
            if (aware == null)
            {
                return;
            }

            if (entering)
            {
                aware.OnNavigatedTo(e);
            }
            else
            {
                aware.OnNavigatedFrom(e);
            }
        }

        private void TrimCache(Control keep)
        {
            TrimCache(keep, null);
        }

        private void TrimCache(Control keepCurrent, Control keepTarget)
        {
            int enabledCount = 0;
            foreach (CacheEntry entry in _cacheOrder)
            {
                if (GetCacheMode(entry.Page) == NavigationCacheMode.Enabled)
                {
                    enabledCount++;
                }
            }

            for (int i = 0; i < _cacheOrder.Count && enabledCount > _cacheSize; i++)
            {
                CacheEntry entry = _cacheOrder[i];
                if (entry.Page == keepCurrent || entry.Page == keepTarget || GetCacheMode(entry.Page) != NavigationCacheMode.Enabled)
                {
                    continue;
                }

                _cacheOrder.RemoveAt(i);
                _cache.Remove(entry.Key);
                enabledCount--;
                i--;
                DisposePage(entry.Page);
            }
        }

        private void DisposePage(Control page)
        {
            if (page == null)
            {
                return;
            }

            if (_currentKey != null)
            {
                CacheEntry cached;
                if (_cache.TryGetValue(_currentKey, out cached) && cached.Page == page)
                {
                    _cacheOrder.Remove(cached);
                    _cache.Remove(_currentKey);
                }
            }

            Controls.Remove(page);
            page.Dispose();
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
                    if (CurrentPage != null)
                    {
                        CurrentPage.Invalidate(true);
                        CurrentPage.Refresh();
                    }
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
                _cache.Clear();
                _cacheOrder.Clear();
                _history.Clear();
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

    /// <summary>A non-selectable group header in a NavigationView menu.</summary>
    public class NavigationViewItemHeader : NavigationViewItem
    {
        public NavigationViewItemHeader()
        {
        }

        public NavigationViewItemHeader(string text)
        {
            Content = text;
        }
    }

    /// <summary>A non-selectable separator line in a NavigationView menu.</summary>
    public class NavigationViewItemSeparator : NavigationViewItem
    {
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
            if (item == null || _selectedItem == item || item is NavigationViewItemHeader || item is NavigationViewItemSeparator)
            {
                return;
            }

            _selectedItem = item;
            if (navigate && item.TargetPageType != null)
            {
                _frame.Navigate(item.TargetPageType, item.Tag);
            }

            OnSelectionChanged(EventArgs.Empty);
            Invalidate();
        }

        protected virtual void OnSelectionChanged(EventArgs e)
        {
            SelectionChanged?.Invoke(this, e);
        }

        private RectangleF HamburgerRect => new RectangleF(4, 4, 40, 40);

        private float GetItemHeight(NavigationViewItem item)
        {
            if (item is NavigationViewItemHeader)
            {
                return _isPaneOpen ? 34f : 0f;
            }

            if (item is NavigationViewItemSeparator)
            {
                return _isPaneOpen ? 16f : 12f;
            }

            return ItemHeight;
        }

        private float GetFooterHeight(int count)
        {
            float height = 0f;
            for (int i = 0; i < count; i++)
            {
                height += GetItemHeight(_footerItems[i]);
            }

            return height;
        }

        private RectangleF ItemRect(int index)
        {
            float y = HamburgerRowHeight;
            for (int i = 0; i < index; i++)
            {
                y += GetItemHeight(_menuItems[i]);
            }

            float height = GetItemHeight(_menuItems[index]);
            return new RectangleF(4, y, EffectivePaneWidth - 8, Math.Max(0, height - 2));
        }

        private RectangleF FooterItemRect(int index)
        {
            float y = Height - GetFooterHeight(_footerItems.Count) - 4;
            for (int i = 0; i < index; i++)
            {
                y += GetItemHeight(_footerItems[i]);
            }

            float height = GetItemHeight(_footerItems[index]);
            return new RectangleF(4, y, EffectivePaneWidth - 8, Math.Max(0, height - 2));
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
                if (_menuItems[i] is NavigationViewItemHeader || _menuItems[i] is NavigationViewItemSeparator)
                {
                    continue;
                }

                if (ItemRect(i).Contains(point))
                {
                    index = i;
                    return 1;
                }
            }

            for (int i = 0; i < _footerItems.Count; i++)
            {
                if (_footerItems[i] is NavigationViewItemHeader || _footerItems[i] is NavigationViewItemSeparator)
                {
                    continue;
                }

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
                DrawMenuItem(g, _menuItems[i], ItemRect(i), _hoverIndex == i);
            }

            // Separator above footer
            if (_footerItems.Count > 0)
            {
                float y = Height - GetFooterHeight(_footerItems.Count) - 8;
                using (var pen = new Pen(ControlHelper.BlendOverParent(this, Palette.DividerStrokeDefault), 1f))
                {
                    g.DrawLine(pen, 8, y, EffectivePaneWidth - 8, y);
                }
            }

            for (int i = 0; i < _footerItems.Count; i++)
            {
                DrawMenuItem(g, _footerItems[i], FooterItemRect(i), _hoverIndex == 1000 + i);
            }
        }

        private void DrawMenuItem(Graphics g, NavigationViewItem item, RectangleF rect, bool hovered)
        {
            var header = item as NavigationViewItemHeader;
            if (header != null)
            {
                if (!_isPaneOpen || rect.Height <= 0)
                {
                    return;
                }

                float textX = rect.X + 16;
                if (header.Icon != SymbolRegular.Empty)
                {
                    var iconRect = new RectangleF(rect.X + 12, rect.Y + (rect.Height - 16) / 2f, 16, 16);
                    FluentGraphics.DrawGlyph(g, FluentGraphics.Glyph((int)header.Icon), FluentTypography.IconRegular(12f), Palette.TextFillSecondary, iconRect);
                    textX = rect.X + 40;
                }

                using (var format = new StringFormat { LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap, Trimming = StringTrimming.EllipsisCharacter })
                {
                    FluentGraphics.DrawText(g, header.Content ?? string.Empty, Font, Palette.TextFillSecondary, new RectangleF(textX, rect.Y, rect.Right - textX - 4, rect.Height), format);
                }

                return;
            }

            if (item is NavigationViewItemSeparator)
            {
                float y = rect.Y + rect.Height / 2f;
                using (var pen = new Pen(ControlHelper.BlendOverParent(this, Palette.DividerStrokeDefault), 1f))
                {
                    g.DrawLine(pen, rect.X + 12, y, rect.Right - 12, y);
                }

                return;
            }

            DrawItem(g, item, rect, hovered);
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