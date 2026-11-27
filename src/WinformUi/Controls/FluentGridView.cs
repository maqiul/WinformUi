using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using WinformUi.Drawing;
using WinformUi.Theming;

namespace WinformUi.Controls
{
    /// <summary>Item click arguments carrying the clicked index and item.</summary>
    public class FluentGridViewItemClickEventArgs : EventArgs
    {
        public FluentGridViewItemClickEventArgs(int index, object item)
        {
            Index = index;
            Item = item;
        }

        public int Index { get; }
        public object Item { get; }
    }

    /// <summary>
    /// Virtualized Fluent tile view: equal-size rounded cards laid out in wrapping
    /// columns, only the visible tiles are painted, with single selection, an accent
    /// stroke, wheel scrolling, keyboard navigation and the same data-binding model
    /// as <see cref="FluentListBox"/>.
    /// </summary>
    [ToolboxItem(true)]
    [DefaultEvent("SelectedIndexChanged")]
    public class FluentGridView : FluentControlBase
    {
        private readonly ListBinding _binding;
        private IList _items = new List<object>();
        private Func<object, string> _textSelector;
        private Func<object, SymbolRegular> _iconSelector;
        private Size _tileSize = new Size(88, 88);
        private int _spacing = 6;
        private bool _stretchTiles = true;
        private int _selectedIndex = -1;
        private int _hoveredIndex = -1;
        private int _scrollOffset;

        public FluentGridView()
        {
            _binding = new ListBinding(this);
            _binding.UnboundItems = _items;
            _binding.ListChanged += OnBindingListChanged;
            _binding.PositionChanged += (s, e) => SelectedIndex = _binding.Position;
            Size = new Size(320, 220);
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

        /// <summary>Data items; replacing the list clears the selection and scrolling.</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IList Items
        {
            get
            {
                if (_binding.DataSource != null)
                {
                    throw new InvalidOperationException("Items cannot be assigned while DataSource is set.");
                }

                return _items;
            }
            set
            {
                if (_binding.DataSource != null)
                {
                    throw new InvalidOperationException("Assign DataSource instead of Items while bound.");
                }

                _items = value ?? new List<object>();
                _binding.UnboundItems = _items;
                SelectedIndex = -1;
                _scrollOffset = 0;
                Invalidate();
            }
        }

        [Category("Data")]
        [DefaultValue(null)]
        [RefreshProperties(RefreshProperties.All)]
        public object DataSource
        {
            get { return _binding.DataSource; }
            set
            {
                if (!Equals(_binding.DataSource, value))
                {
                    _binding.DataSource = value;
                    _binding.Rebind();
                }
            }
        }

        [Category("Data")]
        [DefaultValue("")]
        public string DataMember
        {
            get { return _binding.DataMember; }
            set
            {
                if (_binding.DataMember != value)
                {
                    _binding.DataMember = value ?? string.Empty;
                    _binding.Rebind();
                }
            }
        }

        [Category("Data")]
        [DefaultValue("")]
        public string DisplayMember
        {
            get { return _binding.DisplayMember; }
            set
            {
                _binding.DisplayMember = value ?? string.Empty;
                Invalidate();
            }
        }

        [Category("Data")]
        [DefaultValue("")]
        public string ValueMember
        {
            get { return _binding.ValueMember; }
            set { _binding.ValueMember = value ?? string.Empty; }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public object SelectedValue
        {
            get { return _binding.GetItemValue(SelectedItem); }
            set
            {
                for (int i = 0; i < _items.Count; i++)
                {
                    if (Equals(_binding.GetItemValue(_items[i]), value))
                    {
                        SelectedIndex = i;
                        return;
                    }
                }
            }
        }

        /// <summary>Custom caption extraction; falls back to DisplayMember/ToString.</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Func<object, string> TextSelector
        {
            get { return _textSelector; }
            set
            {
                _textSelector = value;
                Invalidate();
            }
        }

        /// <summary>Optional per-item glyph selector drawn above the caption.</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Func<object, SymbolRegular> IconSelector
        {
            get { return _iconSelector; }
            set
            {
                _iconSelector = value;
                Invalidate();
            }
        }

        [Category("Layout")]
        [DefaultValue(typeof(Size), "88, 88")]
        public Size TileSize
        {
            get { return _tileSize; }
            set
            {
                _tileSize = new Size(Math.Max(24, value.Width), Math.Max(24, value.Height));
                Invalidate();
            }
        }

        [Category("Layout")]
        [DefaultValue(6)]
        public int Spacing
        {
            get { return _spacing; }
            set
            {
                _spacing = Math.Max(0, value);
                Invalidate();
            }
        }

        [Category("Layout")]
        [DefaultValue(true)]
        [Description("Fill the row width with whole tiles instead of keeping a fixed tile width.")]
        public bool StretchTiles
        {
            get { return _stretchTiles; }
            set
            {
                _stretchTiles = value;
                Invalidate();
            }
        }

        [Category("Behavior")]
        [DefaultValue(-1)]
        public int SelectedIndex
        {
            get { return _selectedIndex; }
            set
            {
                int clamped = value < -1 || value >= Count ? -1 : value;
                if (_selectedIndex == clamped)
                {
                    return;
                }

                _selectedIndex = clamped;
                EnsureVisible(clamped);
                _binding.SetPosition(clamped);
                Invalidate();
                SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public object SelectedItem
        {
            get { return _selectedIndex >= 0 && _selectedIndex < Count ? _items[_selectedIndex] : null; }
            set
            {
                int index = value == null ? -1 : _items.IndexOf(value);
                if (index >= 0)
                {
                    SelectedIndex = index;
                }
            }
        }

        public event EventHandler SelectedIndexChanged;
        public event EventHandler<FluentGridViewItemClickEventArgs> ItemClicked;

        protected virtual void OnItemClicked(FluentGridViewItemClickEventArgs e)
        {
            ItemClicked?.Invoke(this, e);
        }

        private int Count => _items.Count;

        private string TextFor(object item)
        {
            return _textSelector != null ? _textSelector(item) : _binding.GetItemText(item);
        }

        private int _cachedCount = -1;
        private int _cachedWidth = -1;
        private int _cachedCols;
        private Size _cachedCell;
        private int _cachedSpacing;

        private void ComputeGridMetrics(out int cols, out Size cell, out int spacing)
        {
            if (_cachedCount == Count && _cachedWidth == Width)
            {
                cols = _cachedCols;
                cell = _cachedCell;
                spacing = _cachedSpacing;
                return;
            }

            int s = Scale(_spacing);
            int tileW = Scale(_tileSize.Width);
            int tileH = Scale(_tileSize.Height);
            int available = Math.Max(1, Width - s);
            int c = Math.Max(1, (available + s) / (tileW + s));
            int cellW = StretchTiles ? (available - (c - 1) * s) / c : tileW;
            var size = new Size(Math.Max(1, cellW), tileH);

            _cachedCount = Count;
            _cachedWidth = Width;
            _cachedCols = c;
            _cachedCell = size;
            _cachedSpacing = s;

            cols = c;
            cell = size;
            spacing = s;
        }

        private void EnsureVisible(int index)
        {
            if (index < 0)
            {
                return;
            }

            ComputeGridMetrics(out int cols, out Size cell, out int spacing);
            int row = index / cols;
            int top = spacing + row * (cell.Height + spacing);
            int bottom = top + cell.Height;
            if (top < _scrollOffset)
            {
                _scrollOffset = top;
            }
            else if (bottom > _scrollOffset + Height)
            {
                _scrollOffset = bottom - Height;
            }

            ClampScroll();
        }

        private void ClampScroll()
        {
            ComputeGridMetrics(out int cols, out Size cell, out int spacing);
            int rows = (Count + cols - 1) / cols;
            int total = rows == 0 ? 0 : spacing * 2 + rows * cell.Height + (rows - 1) * spacing;
            _scrollOffset = Math.Max(0, Math.Min(_scrollOffset, Math.Max(0, total - Height)));
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            ClampScroll();
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            ControlHelper.PaintFullBackground(this, g);
            FluentGraphics.SetupGraphics(g);
            ClampScroll();

            ComputeGridMetrics(out int cols, out Size cell, out int spacing);
            int rowHeight = cell.Height + spacing;
            int firstRow = Math.Max(0, _scrollOffset / Math.Max(1, rowHeight));
            int lastRow = (_scrollOffset + Height) / Math.Max(1, rowHeight) + 1;
            int rows = (Count + cols - 1) / cols;
            if (rows == 0)
            {
                return;
            }

            lastRow = Math.Min(lastRow, rows - 1);

            for (int row = firstRow; row <= lastRow; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    int index = row * cols + col;
                    if (index >= Count)
                    {
                        return;
                    }

                    PaintTile(g, index, new RectangleF(spacing + col * (cell.Width + spacing),
                        spacing + row * rowHeight - _scrollOffset, cell.Width, cell.Height));
                }
            }
        }


        /// <summary>Draws one tile; override for fully custom tile content.</summary>
        protected virtual void PaintTile(Graphics g, int index, RectangleF bounds)
        {
            FluentPalette p = Palette;
            bool selected = index == _selectedIndex;
            bool hovered = index == _hoveredIndex;

            Color fill = selected
                ? ControlHelper.BlendOverParent(this, p.ControlFillSecondary)
                : ControlHelper.BlendOverParent(this, hovered ? p.ControlFillSecondary : p.ControlFillDefault);
            using (var brush = new SolidBrush(fill))
            {
                FluentGraphics.FillRoundedRect(g, brush, bounds, Scale(6));
            }

            FluentGraphics.DrawElevationBorder(g, bounds, Scale(6), p);

            if (selected)
            {
                using (var pen = new Pen(p.SystemAccentColorPrimary, Scale(2)))
                {
                    FluentGraphics.DrawRoundedRect(g, pen, RectangleF.Inflate(bounds, -1, -1), Scale(6));
                }
            }

            object item = _items[index];
            string text = TextFor(item);
            SymbolRegular icon = _iconSelector != null ? _iconSelector(item) : SymbolRegular.Empty;

            float textHeight = Math.Min(bounds.Height - Scale(10), Scale(30));
            RectangleF textRect = new RectangleF(bounds.X + Scale(6), bounds.Bottom - Scale(6) - textHeight,
                bounds.Width - Scale(12), textHeight);
            RectangleF glyphRect = icon != SymbolRegular.Empty
                ? new RectangleF(bounds.X, bounds.Y + Scale(8), bounds.Width, bounds.Height - Scale(8) - textHeight - Scale(6))
                : new RectangleF(bounds.X, bounds.Y + Scale(8), bounds.Width, bounds.Height - Scale(8) - textHeight - Scale(4));

            if (icon != SymbolRegular.Empty)
            {
                FluentGraphics.DrawGlyph(g, FluentGraphics.Glyph((int)icon), FluentTypography.IconRegular(16f),
                    selected ? p.SystemAccentColorPrimary : p.TextFillSecondary, glyphRect);
            }

            using (var format = new StringFormat(StringFormatFlags.NoWrap))
            {
                format.Alignment = StringAlignment.Center;
                format.LineAlignment = StringAlignment.Far;
                format.Trimming = StringTrimming.EllipsisCharacter;
                format.FormatFlags |= StringFormatFlags.LineLimit;
                FluentGraphics.DrawText(g, text, FluentTypography.Caption,
                    Enabled ? (selected ? p.TextFillPrimary : p.TextFillSecondary) : p.TextFillDisabled,
                    textRect, format);
            }
        }

        private int HitTestIndex(Point pt)
        {
            ComputeGridMetrics(out int cols, out Size cell, out int spacing);
            int rowHeight = cell.Height + spacing;
            int rowWidth = cell.Width + spacing;
            int row = (pt.Y + _scrollOffset - spacing) / Math.Max(1, rowHeight);
            int col = (pt.X - spacing) / Math.Max(1, rowWidth);
            if (row < 0 || col < 0 || col >= cols)
            {
                return -1;
            }

            int index = row * cols + col;
            if (index >= Count)
            {
                return -1;
            }

            Rectangle rect = new Rectangle(spacing + col * rowWidth, spacing + row * rowHeight - _scrollOffset, cell.Width, cell.Height);
            return rect.Contains(pt) ? index : -1;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int index = HitTestIndex(e.Location);
            if (index != _hoveredIndex)
            {
                _hoveredIndex = index;
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (_hoveredIndex != -1)
            {
                _hoveredIndex = -1;
                Invalidate();
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
            {
                int index = HitTestIndex(e.Location);
                if (index >= 0)
                {
                    SelectedIndex = index;
                }
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button == MouseButtons.Left)
            {
                int index = HitTestIndex(e.Location);
                if (index >= 0)
                {
                    OnItemClicked(new FluentGridViewItemClickEventArgs(index, _items[index]));
                }
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            ComputeGridMetrics(out _, out Size cell, out int spacing);
            int rows = Math.Abs(e.Delta) / 120;
            if (rows == 0)
            {
                rows = 1;
            }

            _scrollOffset += (e.Delta > 0 ? -1 : 1) * rows * (cell.Height + spacing);
            ClampScroll();
            Invalidate();
        }

        protected override bool IsInputKey(Keys keyData)
        {
            switch (keyData)
            {
                case Keys.Left:
                case Keys.Right:
                case Keys.Up:
                case Keys.Down:
                case Keys.Home:
                case Keys.End:
                    return true;
            }

            return base.IsInputKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            ComputeGridMetrics(out int cols, out _, out _);
            int next = _selectedIndex;
            switch (e.KeyCode)
            {
                case Keys.Left:
                    next--;
                    break;
                case Keys.Right:
                    next++;
                    break;
                case Keys.Up:
                    next -= cols;
                    break;
                case Keys.Down:
                    next += cols;
                    break;
                case Keys.Home:
                    next = 0;
                    break;
                case Keys.End:
                    next = Count - 1;
                    break;
                default:
                    return;
            }

            if (next >= 0 && next < Count)
            {
                SelectedIndex = next;
                e.Handled = true;
            }
        }

        private void OnBindingListChanged(object sender, EventArgs e)
        {
            _items = _binding.CurrentItems;
            _cachedCount = -1;
            if (_selectedIndex >= _items.Count)
            {
                _selectedIndex = _items.Count - 1;
            }

            ClampScroll();
            Invalidate();
        }

        /// <summary>Re-resolves the binding (after the bound list changed in place).</summary>
        public void RefreshItems()
        {
            _binding.Rebind();
            _items = _binding.CurrentItems;
            Invalidate();
        }
    }
}
