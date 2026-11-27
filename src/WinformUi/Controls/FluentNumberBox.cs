using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using WinformUi.Drawing;
using WinformUi.Theming;

namespace WinformUi.Controls
{
    /// <summary>
    /// Fluent number box: text input with inline spin buttons,
    /// value clamping and mouse-wheel support.
    /// </summary>
    [System.ComponentModel.ToolboxItem(true)]
    public class FluentNumberBox : FluentTextInputBase
    {
        private double _value;
        private double _minimum = double.MinValue;
        private double _maximum = double.MaxValue;
        private double _smallChange = 1;
        private double _largeChange = 10;
        private int _spinHover; // 0 none, 1 up, 2 down
        private int _spinPressed;

        public event EventHandler ValueChanged;

        public FluentNumberBox()
        {
            InnerTextBox.TextAlign = HorizontalAlignment.Left;
            InnerTextBox.Leave += (s, e) => CommitText();
            InnerTextBox.KeyDown += OnInnerKeyDown;
            Text = "0";
        }

        [DefaultValue(0d)]
        [Category("Fluent")]
        public double Value
        {
            get { return _value; }
            set
            {
                double clamped = Math.Max(_minimum, Math.Min(_maximum, value));
                if (_value != clamped)
                {
                    _value = clamped;
                    InnerTextBox.Text = _value.ToString(CultureInfo.CurrentCulture);
                    OnValueChanged(EventArgs.Empty);
                }
            }
        }

        [DefaultValue(double.MinValue)]
        [Category("Fluent")]
        public double Minimum
        {
            get { return _minimum; }
            set { _minimum = value; Value = _value; }
        }

        [DefaultValue(double.MaxValue)]
        [Category("Fluent")]
        public double Maximum
        {
            get { return _maximum; }
            set { _maximum = value; Value = _value; }
        }

        [DefaultValue(1d)]
        [Category("Fluent")]
        public double SmallChange
        {
            get { return _smallChange; }
            set { _smallChange = value; }
        }

        [DefaultValue(10d)]
        [Category("Fluent")]
        public double LargeChange
        {
            get { return _largeChange; }
            set { _largeChange = value; }
        }

        protected virtual void OnValueChanged(EventArgs e)
        {
            ValueChanged?.Invoke(this, e);
        }

        protected override int ReservedRight => 56;

        private void OnInnerKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                CommitText();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Up)
            {
                Value = _value + _smallChange;
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Down)
            {
                Value = _value - _smallChange;
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.PageUp)
            {
                Value = _value + _largeChange;
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.PageDown)
            {
                Value = _value - _largeChange;
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void CommitText()
        {
            double parsed;
            if (double.TryParse(InnerTextBox.Text, NumberStyles.Float, CultureInfo.CurrentCulture, out parsed))
            {
                Value = parsed;
            }
            else
            {
                InnerTextBox.Text = _value.ToString(CultureInfo.CurrentCulture);
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            if (Enabled)
            {
                Value = _value + (e.Delta > 0 ? _smallChange : -_smallChange);
            }

            base.OnMouseWheel(e);
        }

        private RectangleF UpButtonRect => new RectangleF(Width - 54, 4, 24, Height - 8);
        private RectangleF DownButtonRect => new RectangleF(Width - 28, 4, 24, Height - 8);

        private int SpinButtonAt(Point point)
        {
            if (UpButtonRect.Contains(point)) return 1;
            if (DownButtonRect.Contains(point)) return 2;
            return 0;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            int button = SpinButtonAt(e.Location);
            if (button > 0 && Enabled)
            {
                _spinPressed = button;
                Value = _value + (button == 1 ? _smallChange : -_smallChange);
                Invalidate();
                return;
            }

            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            _spinPressed = 0;
            base.OnMouseUp(e);
            Invalidate();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int hover = SpinButtonAt(e.Location);
            if (_spinHover != hover)
            {
                _spinHover = hover;
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _spinHover = 0;
            base.OnMouseLeave(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            ControlHelper.PaintFullBackground(this, e.Graphics);
            base.OnPaint(e);

            Graphics g = e.Graphics;
            DrawSpinButton(g, UpButtonRect, 0xE70E, _spinHover == 1, _spinPressed == 1);
            DrawSpinButton(g, DownButtonRect, 0xE70D, _spinHover == 2, _spinPressed == 2);
        }

        private void DrawSpinButton(Graphics g, RectangleF rect, int glyph, bool hovered, bool pressed)
        {
            FluentPalette p = Palette;
            Color fill = Color.Transparent;
            if (Enabled)
            {
                if (pressed) fill = ControlHelper.BlendOverParent(this, p.SubtleFillTertiary);
                else if (hovered) fill = ControlHelper.BlendOverParent(this, p.SubtleFillSecondary);
            }

            if (fill.A > 0)
            {
                using (var brush = new SolidBrush(fill))
                {
                    FluentGraphics.FillRoundedRect(g, brush, rect, FluentGraphics.ControlCornerRadius);
                }
            }

            Color glyphColor = Enabled ? p.TextFillSecondary : p.TextFillDisabled;
            FluentGraphics.DrawGlyph(g, FluentGraphics.Glyph(glyph), FluentTypography.Icon(8f), glyphColor, rect);
        }
    }

    /// <summary>
    /// Fluent auto-suggest box: typing filters a suggestion
    /// flyout; choosing a suggestion fills the text.
    /// </summary>
    [System.ComponentModel.ToolboxItem(true)]
    public class FluentAutoSuggestBox : FluentTextInputBase
    {
        private IList<string> _items = new List<string>();
        private FluentPopupForm _popup;
        private FluentDropDownList _list;
        private bool _suppressSuggestions;

        public event EventHandler SuggestionChosen;

        public FluentAutoSuggestBox()
        {
            InnerTextBox.TextChanged += OnInnerTextChanged;
            InnerTextBox.KeyDown += OnInnerKeyDown;
            InnerTextBox.LostFocus += (s, e) => ClosePopup();
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IList<string> Items
        {
            get { return _items; }
            set { _items = value ?? new List<string>(); }
        }

        protected override int ReservedRight => 32;

        private void OnInnerTextChanged(object sender, EventArgs e)
        {
            if (_suppressSuggestions || !InnerTextBox.Focused)
            {
                return;
            }

            UpdateSuggestions();
        }

        private void UpdateSuggestions()
        {
            string query = InnerTextBox.Text ?? string.Empty;
            if (query.Length == 0)
            {
                ClosePopup();
                return;
            }

            var matches = new List<object>();
            foreach (string item in _items)
            {
                if (item != null && item.IndexOf(query, StringComparison.CurrentCultureIgnoreCase) >= 0)
                {
                    matches.Add(item);
                }
            }

            if (matches.Count == 0)
            {
                ClosePopup();
                return;
            }

            if (_popup == null)
            {
                _popup = new FluentPopupForm { NoActivate = true };
                _list = new FluentDropDownList { TabStop = false, Dock = DockStyle.Fill };
                _list.ItemClicked += (s, e) => ChooseSuggestion(_list.SelectedItem as string);
                _popup.Padding = new Padding(4);
                _popup.Controls.Add(_list);
                _popup.FormClosed += (s, e) => { _popup = null; _list = null; };
            }

            _list.Items = matches;
            _list.SelectedIndex = -1;
            int height = Math.Min(matches.Count * _list.ItemHeight + 8, 280);
            _popup.ShowBelow(this, height);
        }

        private void ChooseSuggestion(string text)
        {
            if (text == null)
            {
                return;
            }

            _suppressSuggestions = true;
            InnerTextBox.Text = text;
            InnerTextBox.SelectionStart = text.Length;
            _suppressSuggestions = false;
            ClosePopup();
            SuggestionChosen?.Invoke(this, EventArgs.Empty);
        }

        private void ClosePopup()
        {
            _popup?.Close();
        }

        private void OnInnerKeyDown(object sender, KeyEventArgs e)
        {
            if (_list == null)
            {
                return;
            }

            switch (e.KeyCode)
            {
                case Keys.Down:
                    if (_list.SelectedIndex < _list.Items.Count - 1) _list.SelectedIndex++;
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    break;
                case Keys.Up:
                    if (_list.SelectedIndex > 0) _list.SelectedIndex--;
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    break;
                case Keys.Enter:
                    if (_list.SelectedIndex >= 0)
                    {
                        ChooseSuggestion(_list.SelectedItem as string);
                        e.Handled = true;
                        e.SuppressKeyPress = true;
                    }

                    break;
                case Keys.Escape:
                    ClosePopup();
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    break;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            ControlHelper.PaintFullBackground(this, e.Graphics);
            base.OnPaint(e);
            Color glyphColor = Enabled ? Palette.TextFillSecondary : Palette.TextFillDisabled;
            FluentGraphics.DrawGlyph(e.Graphics, FluentGraphics.Glyph(0xE721), FluentTypography.Icon(11f), glyphColor, new RectangleF(Width - 28, (Height - 16) / 2f, 16, 16));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ClosePopup();
            }

            base.Dispose(disposing);
        }
    }
}