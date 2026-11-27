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
    /// Borderless rounded popup host used by ComboBox / AutoSuggestBox / pickers.
    /// Closes on deactivation or Escape, has the Fluent flyout surface treatment.
    /// </summary>
    public class FluentPopupForm : Form
    {
        private const int WM_MOUSEACTIVATE = 0x0021;
        private const int MA_NOACTIVATE = 3;

        public FluentPopupForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            BackColor = ApplicationThemeManager.Palette.AcrylicBackgroundFillDefault;
            Font = FluentTypography.Body;
            KeyPreview = true;
            AutoScaleMode = AutoScaleMode.Dpi;
        }

        /// <summary>When true the popup keeps focus on its owner (used for suggestion lists while typing).</summary>
        [DefaultValue(false)]
        public bool NoActivate { get; set; }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= 0x20000; // CS_DROPSHADOW
                return cp;
            }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            ApplyRoundedRegion();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (IsHandleCreated)
            {
                ApplyRoundedRegion();
            }
        }

        private void ApplyRoundedRegion()
        {
            using (var path = FluentGraphics.RoundedRect(new RectangleF(0, 0, Width, Height), FluentGraphics.PopupCornerRadius))
            {
                Region = new Region(path);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            ControlHelper.PaintFullBackground(this, e.Graphics);
            base.OnPaint(e);
            FluentGraphics.SetupGraphics(e.Graphics);
            using (var pen = new Pen(ApplicationThemeManager.Palette.SurfaceStrokeDefault, 1f))
            {
                FluentGraphics.DrawRoundedRect(e.Graphics, pen, new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f), FluentGraphics.PopupCornerRadius);
            }
        }

        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);
            if (!NoActivate)
            {
                Close();
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Escape)
            {
                Close();
                e.Handled = true;
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (NoActivate && m.Msg == WM_MOUSEACTIVATE)
            {
                m.Result = (IntPtr)MA_NOACTIVATE;
                return;
            }

            base.WndProc(ref m);
        }

        /// <summary>Positions the popup below (or above) the anchor, keeping it on screen.</summary>
        public void ShowBelow(Control anchor, int popupHeight, int popupWidth = 0)
        {
            Screen screen = Screen.FromControl(anchor);
            Point location = anchor.PointToScreen(new Point(0, anchor.Height));
            int width = popupWidth > 0 ? popupWidth : Math.Max(anchor.Width, 120);

            if (location.Y + popupHeight > screen.WorkingArea.Bottom)
            {
                location = anchor.PointToScreen(new Point(0, -popupHeight));
            }

            if (location.X + width > screen.WorkingArea.Right)
            {
                location.X = screen.WorkingArea.Right - width;
            }

            Bounds = new Rectangle(location, new Size(width, popupHeight));
            Show(anchor.FindForm());
        }
    }

    /// <summary>
    /// Owner-drawn Fluent list used inside popups: 32px items, subtle hover fill,
    /// accent selection pill.
    /// </summary>
    [System.ComponentModel.ToolboxItem(true)]
    [System.ComponentModel.Designer(typeof(WinformUi.Design.FluentListControlDesigner))]
    public class FluentDropDownList : FluentControlBase
    {
        private IList _items = new List<object>();
        private readonly ListBinding _binding;
        private int _selectedIndex = -1;
        private int _hoverIndex = -1;
        private int _scrollOffset;
        private bool _userScrolled;
        private Func<object, string> _textSelector;

        public event EventHandler ItemClicked;
        public event EventHandler SelectedIndexChanged;

        public FluentDropDownList()
        {
            TabStop = true;
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
        public IList Items
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
                    _userScrolled = false;
                    EnsureVisibleIfSized(value);
                    OnSelectedIndexChanged(EventArgs.Empty);
                    Invalidate();
                }

                _binding.SetPosition(value);
            }
        }

        [Browsable(false)]
        public object SelectedItem
        {
            get { return _selectedIndex >= 0 && _selectedIndex < _items.Count ? _items[_selectedIndex] : null; }
        }

        protected virtual void OnItemClicked(EventArgs e)
        {
            ItemClicked?.Invoke(this, e);
        }

        protected virtual void OnSelectedIndexChanged(EventArgs e)
        {
            SelectedIndexChanged?.Invoke(this, e);
        }

        public int ItemHeight => 32;

        private string GetItemText(object item)
        {
            if (_textSelector != null)
            {
                return _textSelector(item);
            }

            return _binding.GetItemText(item);
        }

        private void OnBindingListChanged(object sender, EventArgs e)
        {
            IList current = _binding.CurrentItems;
            if (!ReferenceEquals(current, _items))
            {
                _items = current;
                _scrollOffset = 0;
            }

            if (_binding.DataSource != null)
            {
                SelectedIndex = _binding.Position;
            }
            else if (_selectedIndex >= _items.Count)
            {
                _selectedIndex = -1;
            }

            Invalidate();
        }

        private void EnsureVisibleIfSized(int index)
        {
            // Before layout the Height is the control default (< one row); scrolling
            // with it would push every item above the selection out of view.
            if (index >= 0 && Height >= ItemHeight)
            {
                EnsureVisible(index);
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (!_userScrolled)
            {
                _scrollOffset = 0;
                EnsureVisibleIfSized(_selectedIndex);
            }
        }

        private void EnsureVisible(int index)
        {
            if (index < 0)
            {
                return;
            }

            int top = index * ItemHeight;
            if (top < _scrollOffset)
            {
                _scrollOffset = top;
            }
            else if (top + ItemHeight > _scrollOffset + Height)
            {
                _scrollOffset = top + ItemHeight - Height;
            }
        }

        private int IndexAt(Point point)
        {
            int index = (point.Y + _scrollOffset) / ItemHeight;
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
            if (e.Button == MouseButtons.Left)
            {
                int index = IndexAt(e.Location);
                if (index >= 0)
                {
                    SelectedIndex = index;
                    OnItemClicked(EventArgs.Empty);
                }
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            _userScrolled = true;
            int maxOffset = Math.Max(0, _items.Count * ItemHeight - Height);
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
                case Keys.Enter:
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
                case Keys.Enter:
                    if (_selectedIndex >= 0) OnItemClicked(EventArgs.Empty);
                    e.Handled = true;
                    break;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            ControlHelper.PaintFullBackground(this, e.Graphics);
            Graphics g = e.Graphics;
            FluentGraphics.SetupGraphics(g);

            FluentPalette p = Palette;
            Color background = p.AcrylicBackgroundFillDefault;
            using (var brush = new SolidBrush(background))
            {
                g.FillRectangle(brush, ClientRectangle);
            }

            int first = _scrollOffset / ItemHeight;
            int last = Math.Min(_items.Count - 1, (_scrollOffset + Height) / ItemHeight);

            for (int i = first; i <= last; i++)
            {
                float y = i * ItemHeight - _scrollOffset;
                var itemRect = new RectangleF(4, y + 2, Width - 8, ItemHeight - 4);

                bool selected = i == _selectedIndex;
                bool hovered = i == _hoverIndex;

                Color fill = selected || hovered
                    ? AccentColor.Blend(p.SubtleFillSecondary, background)
                    : Color.Transparent;

                if (fill.A > 0)
                {
                    using (var brush = new SolidBrush(fill))
                    {
                        FluentGraphics.FillRoundedRect(g, brush, itemRect, FluentGraphics.ControlCornerRadius);
                    }
                }

                if (selected)
                {
                    using (var brush = new SolidBrush(p.SystemAccentColorPrimary))
                    {
                        FluentGraphics.FillRoundedRect(g, brush, new RectangleF(itemRect.X + 2, itemRect.Y + (itemRect.Height - 16) / 2f, 3, 16), 1.5f);
                    }
                }

                string text = GetItemText(_items[i]);
                using (var format = new StringFormat { LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap, Trimming = StringTrimming.EllipsisCharacter })
                {
                    FluentGraphics.DrawText(g, text, Font, p.TextFillPrimary, new RectangleF(itemRect.X + 12, itemRect.Y, itemRect.Width - 16, itemRect.Height), format);
                }
            }
        }
    }
}