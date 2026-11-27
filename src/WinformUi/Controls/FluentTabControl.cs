using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using WinformUi.Drawing;
using WinformUi.Theming;

namespace WinformUi.Controls
{
    /// <summary>A tab page in <see cref="FluentTabControl"/>.</summary>
    public class FluentTabPage
    {
        public string Text { get; set; }
        public Control Content { get; set; }
        public object Tag { get; set; }

        public FluentTabPage()
        {
        }

        public FluentTabPage(string text, Control content)
        {
            Text = text;
            Content = content;
        }
    }

    /// <summary>
    /// Fluent tab control: header strip where the
    /// selected tab merges into the content surface.
    /// </summary>
    public class FluentTabControl : Control
    {
        private const int HeaderHeight = 36;

        private readonly List<FluentTabPage> _tabPages = new List<FluentTabPage>();
        private ThemeSubscription _themeSubscription;
        private int _selectedIndex = -1;
        private int _hoverIndex = -1;
        private readonly List<RectangleF> _headerRects = new List<RectangleF>();

        public event EventHandler SelectedIndexChanged;

        protected override void OnPaintBackground(PaintEventArgs e) => ControlHelper.PaintFullBackground(this, e.Graphics);

        public FluentTabControl()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            Font = FluentTypography.Body;
            Size = new Size(400, 250);
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public List<FluentTabPage> TabPages => _tabPages;

        [DefaultValue(-1)]
        [Category("Fluent")]
        public int SelectedIndex
        {
            get { return _selectedIndex; }
            set
            {
                int clamped = value < -1 ? -1 : (value >= _tabPages.Count ? _tabPages.Count - 1 : value);
                if (_selectedIndex != clamped)
                {
                    _selectedIndex = clamped;
                    UpdateContentVisibility();
                    OnSelectedIndexChanged(EventArgs.Empty);
                    Invalidate();
                }
            }
        }

        protected FluentPalette Palette => ApplicationThemeManager.Palette;

        public FluentTabPage AddTab(string text, Control content)
        {
            var page = new FluentTabPage(text, content);
            _tabPages.Add(page);
            if (content != null)
            {
                content.Dock = DockStyle.None;
                content.Visible = false;
                Controls.Add(content);
            }

            if (_selectedIndex < 0)
            {
                SelectedIndex = 0;
            }

            LayoutContent();
            Invalidate();
            return page;
        }

        protected virtual void OnSelectedIndexChanged(EventArgs e)
        {
            SelectedIndexChanged?.Invoke(this, e);
        }

        private void UpdateContentVisibility()
        {
            for (int i = 0; i < _tabPages.Count; i++)
            {
                if (_tabPages[i].Content != null)
                {
                    _tabPages[i].Content.Visible = i == _selectedIndex;
                    if (i == _selectedIndex)
                    {
                        _tabPages[i].Content.BringToFront();
                    }
                }
            }
        }

        private void LayoutContent()
        {
            foreach (var page in _tabPages)
            {
                if (page.Content != null)
                {
                    page.Content.Bounds = new Rectangle(1, HeaderHeight, Math.Max(0, Width - 2), Math.Max(0, Height - HeaderHeight - 1));
                }
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            LayoutContent();
            Invalidate();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (_themeSubscription == null)
            {
                _themeSubscription = new ThemeSubscription(this, () => Invalidate());
            }

            LayoutContent();
            UpdateContentVisibility();
        }

        private void MeasureHeaders(Graphics g)
        {
            _headerRects.Clear();
            float x = 4;
            foreach (var page in _tabPages)
            {
                SizeF size = TextRenderer.MeasureText(page.Text ?? string.Empty, Font);
                float width = size.Width + 28;
                _headerRects.Add(new RectangleF(x, 4, width, HeaderHeight - 4));
                x += width + 2;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int hover = -1;
            for (int i = 0; i < _headerRects.Count; i++)
            {
                if (_headerRects[i].Contains(e.Location))
                {
                    hover = i;
                    break;
                }
            }

            if (_hoverIndex != hover)
            {
                _hoverIndex = hover;
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hoverIndex = -1;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
            {
                for (int i = 0; i < _headerRects.Count; i++)
                {
                    if (_headerRects[i].Contains(e.Location))
                    {
                        SelectedIndex = i;
                        break;
                    }
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            ControlHelper.PaintFullBackground(this, e.Graphics);
            Graphics g = e.Graphics;
            FluentGraphics.SetupGraphics(g);
            MeasureHeaders(g);

            Color surface = ControlHelper.BlendOverParent(this, Palette.SolidBackgroundFillTertiary);

            // Sync content BackColor to the painted surface: a plain panel's GDI
            // erase with a non-opaque backcolor leaves alpha-0 holes that the DWM
            // backdrop shows through instead of this surface.
            foreach (var page in _tabPages)
            {
                if (page.Content != null && page.Content.BackColor != surface)
                {
                    page.Content.BackColor = surface;
                }
            }

            // Content surface
            if (_selectedIndex >= 0)
            {
                var contentRect = new RectangleF(0.5f, HeaderHeight - 4.5f, Width - 1f, Height - HeaderHeight + 4f);
                using (var brush = new SolidBrush(surface))
                using (var pen = new Pen(Palette.CardStrokeDefault, 1f))
                {
                    FluentGraphics.FillRoundedRect(g, brush, contentRect, FluentGraphics.ControlCornerRadius);
                    FluentGraphics.DrawRoundedRect(g, pen, contentRect, FluentGraphics.ControlCornerRadius);
                }
            }

            using (var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap })
            {
                for (int i = 0; i < _tabPages.Count; i++)
                {
                    RectangleF rect = _headerRects[i];
                    bool selected = i == _selectedIndex;
                    bool hovered = i == _hoverIndex;

                    if (selected)
                    {
                        // Selected tab merges with the content surface below.
                        var tabRect = new RectangleF(rect.X, rect.Y, rect.Width, rect.Height + 5);
                        using (var brush = new SolidBrush(surface))
                        using (var path = FluentGraphics.RoundedRect(tabRect, FluentGraphics.ControlCornerRadius))
                        {
                            g.FillPath(brush, path);
                        }

                        using (var pen = new Pen(Palette.CardStrokeDefault, 1f))
                        {
                            g.DrawLine(pen, rect.X, rect.Bottom + 1, rect.X, rect.Y + 4);
                            g.DrawLine(pen, rect.Right, rect.Y + 4, rect.Right, rect.Bottom + 1);
                            using (var topPath = new System.Drawing.Drawing2D.GraphicsPath())
                            {
                                topPath.AddArc(rect.X, rect.Y, 8, 8, 180, 90);
                                topPath.AddArc(rect.Right - 8, rect.Y, 8, 8, 270, 90);
                                g.DrawPath(pen, topPath);
                            }
                        }
                    }
                    else if (hovered)
                    {
                        using (var brush = new SolidBrush(ControlHelper.BlendOverParent(this, Palette.SubtleFillSecondary)))
                        {
                            FluentGraphics.FillRoundedRect(g, brush, rect, FluentGraphics.ControlCornerRadius);
                        }
                    }

                    Color textColor = selected ? Palette.TextFillPrimary : Palette.TextFillSecondary;
                    FluentGraphics.DrawText(g, _tabPages[i].Text ?? string.Empty, selected ? FluentTypography.BodyStrong : Font, textColor, rect, format);
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

    /// <summary>Fluent breadcrumb bar: chevron-separated path items.</summary>
    [System.ComponentModel.ToolboxItem(true)]
    public class FluentBreadcrumbBar : FluentControlBase
    {
        private readonly List<string> _items = new List<string>();
        private int _hoverIndex = -1;
        private readonly List<RectangleF> _itemRects = new List<RectangleF>();

        public event EventHandler<int> ItemClicked;

        public FluentBreadcrumbBar()
        {
            Size = new Size(300, 28);
            Cursor = Cursors.Hand;
            TabStop = false;
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public List<string> Items => _items;

        public void SetPath(params string[] items)
        {
            _items.Clear();
            _items.AddRange(items);
            Invalidate();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int hover = -1;
            for (int i = 0; i < _itemRects.Count; i++)
            {
                if (_itemRects[i].Contains(e.Location))
                {
                    hover = i;
                    break;
                }
            }

            if (_hoverIndex != hover)
            {
                _hoverIndex = hover;
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hoverIndex = -1;
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button == MouseButtons.Left && _hoverIndex >= 0)
            {
                ItemClicked?.Invoke(this, _hoverIndex);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            ControlHelper.PaintFullBackground(this, e.Graphics);
            Graphics g = e.Graphics;
            FluentGraphics.SetupGraphics(g);
            _itemRects.Clear();

            float x = 0;
            using (var format = new StringFormat { LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap })
            {
                for (int i = 0; i < _items.Count; i++)
                {
                    bool current = i == _items.Count - 1;
                    Font font = current ? FluentTypography.BodyStrong : Font;
                    SizeF size = TextRenderer.MeasureText(_items[i], font);
                    var rect = new RectangleF(x, 0, size.Width + 12, Height);
                    _itemRects.Add(rect);

                    if (_hoverIndex == i && !current)
                    {
                        using (var brush = new SolidBrush(ControlHelper.BlendOverParent(this, Palette.SubtleFillSecondary)))
                        {
                            FluentGraphics.FillRoundedRect(g, brush, rect, FluentGraphics.ControlCornerRadius);
                        }
                    }

                    Color color = current ? Palette.TextFillPrimary : (_hoverIndex == i ? Palette.TextFillSecondary : Palette.TextFillPrimary);
                    FluentGraphics.DrawText(g, _items[i], font, color, new RectangleF(rect.X + 6, 0, rect.Width - 12, Height), format);

                    x += rect.Width;

                    if (!current)
                    {
                        FluentGraphics.DrawGlyph(g, FluentGraphics.Glyph(0xE76C), FluentTypography.Icon(8f), Palette.TextFillSecondary, new RectangleF(x, (Height - 12) / 2f, 12, 12));
                        x += 14;
                    }
                }
            }
        }
    }

    /// <summary>Fluent status bar: bottom strip with divider.</summary>
    public class FluentStatusBar : Panel
    {
        private ThemeSubscription _themeSubscription;

        protected override void OnPaintBackground(PaintEventArgs e) => ControlHelper.PaintFullBackground(this, e.Graphics);

        public FluentStatusBar()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            Dock = DockStyle.Bottom;
            Height = 28;
            Font = FluentTypography.Caption;
            Padding = new Padding(8, 0, 8, 0);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (_themeSubscription == null)
            {
                _themeSubscription = new ThemeSubscription(this, () =>
                {
                    BackColor = ControlHelper.BlendOverParent(this, ApplicationThemeManager.Palette.SolidBackgroundFillSecondary);
                    Invalidate();
                });
            }

            BackColor = ControlHelper.BlendOverParent(this, ApplicationThemeManager.Palette.SolidBackgroundFillSecondary);
        }

        protected override void OnParentChanged(EventArgs e)
        {
            base.OnParentChanged(e);
            BackColor = ControlHelper.BlendOverParent(this, ApplicationThemeManager.Palette.SolidBackgroundFillSecondary);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            ControlHelper.PaintFullBackground(this, e.Graphics);
            FluentPalette p = ApplicationThemeManager.Palette;
            Color fill = ControlHelper.BlendOverParent(this, p.SolidBackgroundFillSecondary);
            using (var brush = new SolidBrush(fill))
            {
                e.Graphics.FillRectangle(brush, ClientRectangle);
            }

            using (var pen = new Pen(ControlHelper.BlendOverParent(this, p.DividerStrokeDefault), 1f))
            {
                e.Graphics.DrawLine(pen, 0, 0, Width, 0);
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