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
    /// <summary>Fluent list box with per-item checked, unchecked and indeterminate states.</summary>
    [System.ComponentModel.ToolboxItem(true)]
    [System.ComponentModel.Designer(typeof(WinformUi.Design.FluentListControlDesigner))]
    public class FluentCheckedListBox : FluentListBox
    {
        private readonly Dictionary<int, CheckState> _checkStates = new Dictionary<int, CheckState>();
        private IList _lastItems;
        private bool _checkOnClick = true;
        private bool _threeState;

        public event ItemCheckEventHandler ItemCheck;

        public FluentCheckedListBox()
        {
            _lastItems = Items;
        }

        [DefaultValue(true)]
        [Category("Behavior")]
        public bool CheckOnClick
        {
            get { return _checkOnClick; }
            set { _checkOnClick = value; }
        }

        [DefaultValue(false)]
        [Category("Behavior")]
        public bool ThreeState
        {
            get { return _threeState; }
            set { _threeState = value; }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IList CheckedItems
        {
            get
            {
                var result = new List<object>();
                for (int i = 0; i < Items.Count; i++)
                {
                    if (GetItemChecked(i))
                    {
                        result.Add(Items[i]);
                    }
                }

                return result;
            }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IEnumerable<int> CheckedIndices
        {
            get
            {
                var result = new List<int>();
                for (int i = 0; i < Items.Count; i++)
                {
                    if (GetItemChecked(i))
                    {
                        result.Add(i);
                    }
                }

                return result;
            }
        }

        public override IList Items
        {
            get { return base.Items; }
            set
            {
                base.Items = value;
                _checkStates.Clear();
                _lastItems = Items;
            }
        }

        protected override void OnBindingListChanged(object sender, EventArgs e)
        {
            IList before = _lastItems;
            base.OnBindingListChanged(sender, e);
            if (!ReferenceEquals(before, Items))
            {
                _checkStates.Clear();
            }
            else
            {
                var remove = new List<int>();
                foreach (int index in _checkStates.Keys)
                {
                    if (index < 0 || index >= Items.Count)
                    {
                        remove.Add(index);
                    }
                }

                foreach (int index in remove)
                {
                    _checkStates.Remove(index);
                }
            }

            _lastItems = Items;
        }

        public bool GetItemChecked(int index)
        {
            return GetItemCheckState(index) != CheckState.Unchecked;
        }

        public void SetItemChecked(int index, bool value)
        {
            SetItemCheckState(index, value ? CheckState.Checked : CheckState.Unchecked);
        }

        public CheckState GetItemCheckState(int index)
        {
            ValidateIndex(index);
            CheckState state;
            return _checkStates.TryGetValue(index, out state) ? state : CheckState.Unchecked;
        }

        public void SetItemCheckState(int index, CheckState value)
        {
            ValidateIndex(index);
            if (value < CheckState.Unchecked || value > CheckState.Indeterminate)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            CheckState current = GetItemCheckState(index);
            if (current == value)
            {
                return;
            }

            var args = new ItemCheckEventArgs(index, value, current);
            OnItemCheck(args);
            value = args.NewValue;
            if (value < CheckState.Unchecked || value > CheckState.Indeterminate)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (current == value)
            {
                return;
            }

            if (value == CheckState.Unchecked)
            {
                _checkStates.Remove(index);
            }
            else
            {
                _checkStates[index] = value;
            }

            Invalidate();
        }

        protected virtual void OnItemCheck(ItemCheckEventArgs e)
        {
            ItemCheck?.Invoke(this, e);
        }

        private void ValidateIndex(int index)
        {
            if (index < 0 || index >= Items.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        private void ToggleItem(int index)
        {
            if (!Enabled || index < 0 || index >= Items.Count)
            {
                return;
            }

            CheckState current = GetItemCheckState(index);
            CheckState next;
            if (_threeState)
            {
                next = current == CheckState.Unchecked ? CheckState.Checked
                    : current == CheckState.Checked ? CheckState.Indeterminate
                    : CheckState.Unchecked;
            }
            else
            {
                next = current == CheckState.Unchecked ? CheckState.Checked : CheckState.Unchecked;
            }

            SetItemCheckState(index, next);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            int index = IndexAt(e.Location);
            bool onCheck = index >= 0 && e.X >= 8 && e.X < 30;
            base.OnMouseUp(e);
            if (e.Button == MouseButtons.Left && _checkOnClick && onCheck)
            {
                ToggleItem(index);
            }
        }

        protected override bool IsInputKey(Keys keyData)
        {
            return keyData == Keys.Space || base.IsInputKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Space && !e.Handled)
            {
                ToggleItem(SelectedIndex);
                e.Handled = true;
            }
        }

        protected override void PaintItem(Graphics g, int index, RectangleF rect)
        {
            FluentVisualState state = Enabled ? FluentVisualState.Normal : FluentVisualState.Disabled;
            CheckState checkState = GetItemCheckState(index);
            bool checkedState = checkState != CheckState.Unchecked;
            float y = rect.Y + (rect.Height - 18f) / 2f;
            var box = new RectangleF(rect.X + 8f, y + 0.5f, 17f, 17f);

            Color fill = checkedState
                ? state == FluentVisualState.Disabled ? Palette.AccentFillDisabled : Palette.AccentFillDefault
                : ControlHelper.BlendOverParent(this, state == FluentVisualState.Disabled ? Palette.ControlAltFillDisabled : Palette.ControlAltFillSecondary);
            Color stroke = checkedState ? Color.Transparent
                : state == FluentVisualState.Disabled ? Palette.ControlStrongStrokeDisabled : Palette.ControlStrongStrokeDefault;

            using (var brush = new SolidBrush(fill))
            {
                FluentGraphics.FillRoundedRect(g, brush, box, FluentGraphics.ControlCornerRadius);
            }

            if (stroke.A > 0)
            {
                using (var pen = new Pen(stroke, 1f))
                {
                    FluentGraphics.DrawRoundedRect(g, pen, box, FluentGraphics.ControlCornerRadius);
                }
            }

            if (checkedState)
            {
                Color glyphColor = Enabled ? Palette.TextOnAccentPrimary : Palette.TextOnAccentDisabled;
                if (checkState == CheckState.Indeterminate)
                {
                    using (var brush = new SolidBrush(glyphColor))
                    {
                        g.FillRectangle(brush, box.X + 4f, box.Y + box.Height / 2f - 1f, box.Width - 8f, 2f);
                    }
                }
                else
                {
                    FluentGraphics.DrawGlyph(g, FluentGraphics.Glyph(0xE73E), FluentTypography.Icon(10f), glyphColor, box);
                }
            }

            using (var format = new StringFormat { LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap, Trimming = StringTrimming.EllipsisCharacter })
            {
                FluentGraphics.DrawText(g, GetItemText(Items[index]), Font,
                    Enabled ? Palette.TextFillPrimary : Palette.TextFillDisabled,
                    new RectangleF(rect.X + 34, rect.Y, rect.Width - 40, rect.Height), format);
            }
        }
    }
}
