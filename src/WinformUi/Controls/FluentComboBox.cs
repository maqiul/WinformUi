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
    /// Fluent combo box: rounded shell with chevron,
    /// flyout list with hover fill and accent selection pill.
    /// </summary>
    [System.ComponentModel.Designer(typeof(WinformUi.Design.FluentListControlDesigner))]
    public class FluentComboBox : Control
    {
        private IList _items = new List<object>();
        private readonly ListBinding _binding;
        private int _selectedIndex = -1;
        private Func<object, string> _textSelector;
        private ThemeSubscription _themeSubscription;
        private bool _hovered;
        private bool _droppedDown;
        private FluentPopupForm _popup;
        private FluentDropDownList _list;

        public event EventHandler SelectedIndexChanged;
        public event EventHandler DropDownOpened;
        public event EventHandler DropDownClosed;

        protected override void OnPaintBackground(PaintEventArgs e) => ControlHelper.PaintFullBackground(this, e.Graphics);

        public FluentComboBox()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            Font = FluentTypography.Body;
            Size = new Size(200, 32);
            Cursor = Cursors.Hand;
            TabStop = true;

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
                if (_selectedIndex >= _items.Count)
                {
                    _selectedIndex = -1;
                }

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

        /// <summary>Optional item-to-text converter (DisplayMember equivalent).</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Func<object, string> TextSelector
        {
            get { return _textSelector; }
            set { _textSelector = value; Invalidate(); }
        }

        [DefaultValue(-1)]
        [Category("Fluent")]
        public int SelectedIndex
        {
            get { return _selectedIndex; }
            set
            {
                int clamped = value < -1 ? -1 : (value >= _items.Count ? _items.Count - 1 : value);
                if (_selectedIndex != clamped)
                {
                    _selectedIndex = clamped;
                    OnSelectedIndexChanged(EventArgs.Empty);
                    Invalidate();
                }

                _binding.SetPosition(clamped);
            }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public object SelectedItem
        {
            get { return _selectedIndex >= 0 && _selectedIndex < _items.Count ? _items[_selectedIndex] : null; }
            set
            {
                int index = value == null ? -1 : _items.IndexOf(value);
                SelectedIndex = index;
            }
        }

        [Browsable(false)]
        public bool DroppedDown => _droppedDown;

        protected FluentPalette Palette => ApplicationThemeManager.Palette;

        protected virtual void OnSelectedIndexChanged(EventArgs e)
        {
            SelectedIndexChanged?.Invoke(this, e);
        }

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
            _items = _binding.CurrentItems;
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

        public string SelectedText => SelectedItem == null ? string.Empty : GetItemText(SelectedItem);

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (_themeSubscription == null)
            {
                _themeSubscription = new ThemeSubscription(this, () => Invalidate());
            }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _hovered = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hovered = false;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left && Enabled)
            {
                if (!Focused)
                {
                    Focus();
                }

                if (_droppedDown)
                {
                    CloseDropDown();
                }
                else
                {
                    OpenDropDown();
                }
            }
        }

        protected override bool IsInputKey(Keys keyData)
        {
            switch (keyData & Keys.KeyCode)
            {
                case Keys.Up:
                case Keys.Down:
                case Keys.Enter:
                case Keys.Space:
                case Keys.F4:
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
                case Keys.F4:
                case Keys.Space:
                case Keys.Enter:
                    if (_droppedDown) CloseDropDown();
                    else OpenDropDown();
                    e.Handled = true;
                    break;
                case Keys.Down:
                    if (!_droppedDown && _selectedIndex < _items.Count - 1) SelectedIndex++;
                    e.Handled = true;
                    break;
                case Keys.Up:
                    if (!_droppedDown && _selectedIndex > 0) SelectedIndex--;
                    e.Handled = true;
                    break;
                case Keys.Escape:
                    if (_droppedDown) CloseDropDown();
                    e.Handled = true;
                    break;
            }
        }

        public void OpenDropDown()
        {
            if (_droppedDown || !Enabled || _items.Count == 0)
            {
                return;
            }

            _popup = new FluentPopupForm();
            _list = new FluentDropDownList
            {
                Items = _items,
                TextSelector = _textSelector ?? (DisplayMember.Length > 0 ? (Func<object, string>)_binding.GetItemText : null),
                SelectedIndex = _selectedIndex,
                Dock = DockStyle.Fill
            };
            _list.ItemClicked += (s, e) =>
            {
                SelectedIndex = _list.SelectedIndex;
                CloseDropDown();
            };
            _popup.FormClosed += (s, e) =>
            {
                _droppedDown = false;
                _popup = null;
                _list = null;
                DropDownClosed?.Invoke(this, EventArgs.Empty);
                Invalidate();
            };

            var padding = new Padding(4);
            _list.Bounds = new Rectangle(padding.Left, padding.Top, 0, 0);
            _popup.Padding = padding;
            _popup.Controls.Add(_list);
            _list.Dock = DockStyle.Fill;

            int height = Math.Min(_items.Count * _list.ItemHeight + 8, 320);
            _droppedDown = true;
            _popup.ShowBelow(this, height);
            _list.Focus();
            DropDownOpened?.Invoke(this, EventArgs.Empty);
            Invalidate();
        }

        public void CloseDropDown()
        {
            if (_popup != null)
            {
                _popup.Close();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            ControlHelper.PaintFullBackground(this, e.Graphics);
            Graphics g = e.Graphics;
            FluentGraphics.SetupGraphics(g);

            Color fill;
            if (!Enabled)
            {
                fill = ControlHelper.BlendOverParent(this, Palette.ControlFillDisabled);
            }
            else if (_hovered || _droppedDown)
            {
                fill = ControlHelper.BlendOverParent(this, Palette.ControlFillSecondary);
            }
            else
            {
                fill = ControlHelper.BlendOverParent(this, Palette.ControlFillDefault);
            }

            var bounds = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
            using (var brush = new SolidBrush(fill))
            {
                FluentGraphics.FillRoundedRect(g, brush, bounds, FluentGraphics.ControlCornerRadius);
            }

            FluentGraphics.DrawElevationBorder(g, bounds, FluentGraphics.ControlCornerRadius, Palette);

            if (Focused && Enabled)
            {
                using (var brush = new SolidBrush(Palette.SystemAccentColorPrimary))
                using (var path = FluentGraphics.RoundedRect(bounds, FluentGraphics.ControlCornerRadius))
                {
                    g.SetClip(path);
                    g.FillRectangle(brush, bounds.X, bounds.Bottom - 2f, bounds.Width, 2f);
                    g.ResetClip();
                }
            }

            // Selected text
            string text = SelectedText;
            Color textColor = Enabled ? Palette.TextFillPrimary : Palette.TextFillDisabled;
            using (var format = new StringFormat { LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap, Trimming = StringTrimming.EllipsisCharacter })
            {
                FluentGraphics.DrawText(g, text, Font, textColor, new RectangleF(12, 0, Width - 44, Height), format);
            }

            // Chevron glyph E70D
            Color chevronColor = Enabled ? Palette.TextFillSecondary : Palette.TextFillDisabled;
            FluentGraphics.DrawGlyph(g, FluentGraphics.Glyph(0xE70D), FluentTypography.Icon(9f), chevronColor, new RectangleF(Width - 28, (Height - 16) / 2f, 16, 16));

            if (Focused && ShowFocusCues && !_droppedDown)
            {
                FluentGraphics.DrawFocusVisual(g, new RectangleF(1.5f, 1.5f, Width - 3f, Height - 3f), FluentGraphics.ControlCornerRadius, Palette);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                CloseDropDown();
                _themeSubscription?.Dispose();
                _themeSubscription = null;
            }

            base.Dispose(disposing);
        }
    }
}