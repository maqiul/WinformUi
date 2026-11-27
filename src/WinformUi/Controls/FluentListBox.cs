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
    /// <summary>
    /// Fluent items control: 32px rows, subtle hover,
    /// accent selection pill, single or multiple selection, wheel scrolling.
    /// </summary>
    [System.ComponentModel.ToolboxItem(true)]
    [System.ComponentModel.Designer(typeof(WinformUi.Design.FluentListControlDesigner))]
    public class FluentListBox : FluentControlBase
    {
        private IList _items = new List<object>();
        private readonly ListBinding _binding;
        private readonly HashSet<int> _selectedIndices = new HashSet<int>();
        private int _selectedIndex = -1;
        private int _hoverIndex = -1;
        private int _scrollOffset;
        private Func<object, string> _textSelector;
        private bool _multiSelect;

        public event EventHandler SelectedIndexChanged;
        public event EventHandler ItemClicked;

        public FluentListBox()
        {
            Size = new Size(220, 160);
            Cursor = Cursors.Hand;
            BackColor = Color.Transparent;

            _binding = new ListBinding(this);
            _binding.UnboundItems = _items;
            _binding.ListChanged += OnBindingListChanged;
            _binding.PositionChanged += (s, e) => SelectedIndex = _binding.Position;
        }

        private bool ShouldSerializeItems()
        {
            return DataSource == null && Items.Count > 0;
        }

        [Category("Data")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        [Localizable(true)]
        [Editor("System.Windows.Forms.Design.ListControlStringCollectionEditor, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", "System.Drawing.Design.UITypeEditor")]
        public virtual IList Items
        {
            get { return _items; }
            set
            {
                if (_binding.DataSource != null)
                {
                    throw new InvalidOperationException("Items collection cannot be modified when the DataSource property is set.");
                }

                _binding.UnboundItems = value ?? new List<object>();
                _items = _binding.UnboundItems;
                _selectedIndices.Clear();
                _selectedIndex = -1;
                _scrollOffset = 0;
                Invalidate();
            }
        }

        /// <summary>Bindable list source (IList, IListSource, BindingSource, DataTable, DataSet…).</summary>
        [Category("Data")]
        [DefaultValue(null)]
        [RefreshProperties(RefreshProperties.Repaint)]
        [AttributeProvider(typeof(IListSource))]
        [TypeConverter(typeof(WinformUi.Design.DataSourceReferenceConverter))]
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

        /// <summary>Member of a DataSet/ITypedList source to bind (e.g. a table name).</summary>
        [Category("Data")]
        [DefaultValue("")]
        [RefreshProperties(RefreshProperties.Repaint)]
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

        /// <summary>Property of the items used as display text.</summary>
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

        /// <summary>Property of the items used as <see cref="SelectedValue"/>.</summary>
        [Category("Data")]
        [DefaultValue("")]
        public string ValueMember
        {
            get { return _binding.ValueMember; }
            set { _binding.ValueMember = value ?? string.Empty; }
        }

        /// <summary>Value of the selected item's <see cref="ValueMember"/> (the item itself when unset).</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public object SelectedValue
        {
            get { return _binding.GetItemValue(SelectedItem); }
            set
            {
                if (value == null)
                {
                    SelectedIndex = -1;
                    return;
                }

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

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Func<object, string> TextSelector
        {
            get { return _textSelector; }
            set { _textSelector = value; Invalidate(); }
        }

        [DefaultValue(false)]
        [Category("Fluent")]
        public bool MultiSelect
        {
            get { return _multiSelect; }
            set
            {
                _multiSelect = value;
                if (!value && _selectedIndices.Count > 1)
                {
                    _selectedIndices.Clear();
                    if (_selectedIndex >= 0)
                    {
                        _selectedIndices.Add(_selectedIndex);
                    }

                    Invalidate();
                }
            }
        }

        [DefaultValue(-1)]
        [Browsable(false)]
        public int SelectedIndex
        {
            get { return _selectedIndex; }
            set
            {
                if (_selectedIndex != value)
                {
                    _selectedIndex = value;
                    _selectedIndices.Clear();
                    if (value >= 0)
                    {
                        _selectedIndices.Add(value);
                        EnsureVisible(value);
                    }

                    OnSelectedIndexChanged(EventArgs.Empty);
                    Invalidate();
                }

                _binding.SetPosition(value);
            }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public object SelectedItem
        {
            get { return _selectedIndex >= 0 && _selectedIndex < _items.Count ? _items[_selectedIndex] : null; }
            set { SelectedIndex = value == null ? -1 : _items.IndexOf(value); }
        }

        [Browsable(false)]
        public IEnumerable<int> SelectedIndices => _selectedIndices;

        protected virtual void OnSelectedIndexChanged(EventArgs e)
        {
            SelectedIndexChanged?.Invoke(this, e);
        }

        protected virtual void OnItemClicked(EventArgs e)
        {
            ItemClicked?.Invoke(this, e);
        }

        public virtual int ItemHeight => 32;

        /// <summary>Height of any fixed header area above the scrollable rows.</summary>
        protected virtual int RowsTop => 0;

        protected int ScrollOffset
        {
            get { return _scrollOffset; }
            set { _scrollOffset = value; }
        }

        protected string GetItemText(object item)
        {
            return _textSelector != null ? _textSelector(item) : _binding.GetItemText(item);
        }

        protected virtual void OnBindingListChanged(object sender, EventArgs e)
        {
            IList current = _binding.CurrentItems;
            bool listSwapped = !ReferenceEquals(current, _items);
            _items = current;

            if (_binding.DataSource != null)
            {
                if (listSwapped)
                {
                    _selectedIndices.Clear();
                    _selectedIndex = -1;
                    _scrollOffset = 0;
                }

                SelectedIndex = _binding.Position;
            }
            else if (listSwapped)
            {
                _selectedIndices.Clear();
                _selectedIndex = -1;
                _scrollOffset = 0;
            }
            else
            {
                _selectedIndices.RemoveWhere(i => i >= _items.Count);
                if (_selectedIndex >= _items.Count)
                {
                    _selectedIndex = -1;
                }
            }

            Invalidate();
        }

        private void EnsureVisible(int index)
        {
            if (index < 0) return;
            int viewport = Height - RowsTop;
            int top = index * ItemHeight;
            if (top < _scrollOffset) _scrollOffset = top;
            else if (top + ItemHeight > _scrollOffset + viewport) _scrollOffset = top + ItemHeight - viewport;
        }

        protected virtual int IndexAt(Point point)
        {
            if (point.Y < RowsTop) return -1;
            int index = (point.Y - RowsTop + _scrollOffset) / ItemHeight;
            return index >= 0 && index < _items.Count ? index : -1;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int index = IndexAt(e.Location);
            if (_hoverIndex != index)
            {
                _hoverIndex = index;
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
            if (e.Button != MouseButtons.Left) return;

            int index = IndexAt(e.Location);
            if (index < 0) return;

            if (_multiSelect && (ModifierKeys & Keys.Control) == Keys.Control)
            {
                if (!_selectedIndices.Add(index))
                {
                    _selectedIndices.Remove(index);
                }

                _selectedIndex = index;
            }
            else
            {
                _selectedIndex = index;
                _selectedIndices.Clear();
                _selectedIndices.Add(index);
            }

            OnSelectedIndexChanged(EventArgs.Empty);
            OnItemClicked(EventArgs.Empty);
            Invalidate();
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            int maxOffset = Math.Max(0, _items.Count * ItemHeight - (Height - RowsTop));
            _scrollOffset = Math.Max(0, Math.Min(maxOffset, _scrollOffset - e.Delta / 2));
            Invalidate();
        }

        protected override bool IsInputKey(Keys keyData)
        {
            switch (keyData & Keys.KeyCode)
            {
                case Keys.Up:
                case Keys.Down:
                case Keys.Home:
                case Keys.End:
                    return true;
                default:
                    return base.IsInputKey(keyData);
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            switch (e.KeyCode)
            {
                case Keys.Down:
                    if (_selectedIndex < _items.Count - 1) SelectedIndex++;
                    e.Handled = true;
                    break;
                case Keys.Up:
                    if (_selectedIndex > 0) SelectedIndex--;
                    e.Handled = true;
                    break;
                case Keys.Home:
                    if (_items.Count > 0) SelectedIndex = 0;
                    e.Handled = true;
                    break;
                case Keys.End:
                    if (_items.Count > 0) SelectedIndex = _items.Count - 1;
                    e.Handled = true;
                    break;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            ControlHelper.PaintFullBackground(this, e.Graphics);
            Graphics g = e.Graphics;
            FluentGraphics.SetupGraphics(g);
            PaintRows(g, Height - RowsTop);
        }

        /// <summary>Paints the scrollable row area with the given viewport height.</summary>
        protected void PaintRows(Graphics g, int viewportHeight, float originY = 0f)
        {
            FluentPalette p = Palette;
            Color background = ControlHelper.GetEffectiveBackground(this);

            int first = _scrollOffset / ItemHeight;
            int last = Math.Min(_items.Count - 1, (_scrollOffset + viewportHeight) / ItemHeight);

            for (int i = first; i <= last; i++)
            {
                float y = i * ItemHeight - _scrollOffset;
                var rect = new RectangleF(2, y + originY + 2, Width - 4, ItemHeight - 4);

                bool selected = _selectedIndices.Contains(i);
                bool hovered = i == _hoverIndex;

                if (selected || hovered)
                {
                    Color fill = AccentColor.Blend(p.SubtleFillSecondary, background);
                    using (var brush = new SolidBrush(fill))
                    {
                        FluentGraphics.FillRoundedRect(g, brush, rect, FluentGraphics.ControlCornerRadius);
                    }
                }

                if (selected)
                {
                    using (var brush = new SolidBrush(p.SystemAccentColorPrimary))
                    {
                        FluentGraphics.FillRoundedRect(g, brush, new RectangleF(rect.X + 2, rect.Y + (rect.Height - 16) / 2f, 3, 16), 1.5f);
                    }
                }

                PaintItem(g, i, rect);
            }

            if (Focused && ShowFocusCues && _selectedIndex >= 0)
            {
                float y = _selectedIndex * ItemHeight - _scrollOffset;
                if (y >= -ItemHeight && y < viewportHeight)
                {
                    var rect = new RectangleF(2, y + originY + 2, Width - 4, ItemHeight - 4);
                    FluentGraphics.DrawFocusVisual(g, rect, FluentGraphics.ControlCornerRadius, p);
                }
            }
        }

        /// <summary>Paints one row; override for custom item layouts.</summary>
        protected virtual void PaintItem(Graphics g, int index, RectangleF rect)
        {
            using (var format = new StringFormat { LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap, Trimming = StringTrimming.EllipsisCharacter })
            {
                FluentGraphics.DrawText(g, GetItemText(_items[index]), Font, Palette.TextFillPrimary, new RectangleF(rect.X + 12, rect.Y, rect.Width - 16, rect.Height), format);
            }
        }
    }

    /// <summary>
    /// Fluent list view: list mode identical to ListBox,
    /// details mode renders simple text columns under a header row.
    /// </summary>
    public class FluentListView : FluentListBox
    {
        private readonly List<string> _columns = new List<string>();
        private Func<object, string[]> _subItemSelector;
        private bool _detailsView;

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public List<string> Columns => _columns;

        /// <summary>Extracts column cells for details view.</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Func<object, string[]> SubItemSelector
        {
            get { return _subItemSelector; }
            set { _subItemSelector = value; Invalidate(); }
        }

        [DefaultValue(false)]
        [Category("Fluent")]
        public bool DetailsView
        {
            get { return _detailsView; }
            set { _detailsView = value; Invalidate(); }
        }

        public override int ItemHeight => _detailsView ? 30 : base.ItemHeight;

        private bool HasHeader => _detailsView && _columns.Count > 0;

        protected override int RowsTop => HasHeader ? 30 : 0;

        protected override void OnPaint(PaintEventArgs e)
        {
            ControlHelper.PaintFullBackground(this, e.Graphics);
            if (!HasHeader)
            {
                base.OnPaint(e);
                return;
            }

            Graphics g = e.Graphics;
            FluentGraphics.SetupGraphics(g);
            FluentPalette p = Palette;

            float[] widths = ColumnWidths();
            float x = 4;
            using (var format = new StringFormat { LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap })
            {
                for (int c = 0; c < _columns.Count; c++)
                {
                    FluentGraphics.DrawText(g, _columns[c], FluentTypography.Caption, p.TextFillSecondary, new RectangleF(x + 8, 0, widths[c] - 8, RowsTop), format);
                    x += widths[c];
                }
            }

            using (var pen = new Pen(ControlHelper.BlendOverParent(this, p.DividerStrokeDefault), 1f))
            {
                g.DrawLine(pen, 0, RowsTop - 1, Width, RowsTop - 1);
            }

            var state = g.Save();
            // GDI text ignores GDI+ world transforms, so rows are offset explicitly
            // instead of translating the graphics.
            g.SetClip(new Rectangle(0, RowsTop, Width, Height - RowsTop));
            PaintRows(g, Height - RowsTop, RowsTop);
            g.Restore(state);
        }

        private float[] ColumnWidths()
        {
            var widths = new float[_columns.Count];
            float each = (Width - 8) / _columns.Count;
            for (int i = 0; i < widths.Length; i++)
            {
                widths[i] = each;
            }

            return widths;
        }

        protected override void PaintItem(Graphics g, int index, RectangleF rect)
        {
            if (!HasHeader || _subItemSelector == null)
            {
                base.PaintItem(g, index, rect);
                return;
            }

            string[] cells = _subItemSelector(Items[index]) ?? new string[0];
            float[] widths = ColumnWidths();
            float x = rect.X;
            using (var format = new StringFormat { LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap, Trimming = StringTrimming.EllipsisCharacter })
            {
                for (int c = 0; c < _columns.Count; c++)
                {
                    string text = c < cells.Length ? cells[c] : string.Empty;
                    FluentGraphics.DrawText(g, text, Font, Palette.TextFillPrimary, new RectangleF(x + 10, rect.Y, widths[c] - 12, rect.Height), format);
                    x += widths[c];
                }
            }
        }
    }
}