using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using WinformUi.Drawing;
using WinformUi.Theming;

namespace WinformUi.Controls
{
    /// <summary>
    /// Fluent check box: 20px rounded square, accent fill when checked,
    /// optional indeterminate state.
    /// </summary>
    [System.ComponentModel.ToolboxItem(true)]
    public class FluentCheckBox : FluentControlBase
    {
        private const int BoxSize = 20;
        private const int TextGap = 8;

        private bool _checked;
        private bool _threeState;
        private CheckState _checkState = CheckState.Unchecked;
        private bool _autoSize = true;

        public event EventHandler CheckedChanged;
        public event EventHandler CheckStateChanged;

        public FluentCheckBox()
        {
            Size = new Size(120, 24);
            Cursor = Cursors.Hand;
        }

        [DefaultValue(false)]
        [Category("Fluent")]
        public bool Checked
        {
            get { return _checked; }
            set
            {
                CheckState = value ? CheckState.Checked : CheckState.Unchecked;
            }
        }

        [DefaultValue(false)]
        [Category("Fluent")]
        public bool ThreeState
        {
            get { return _threeState; }
            set { _threeState = value; }
        }

        [DefaultValue(CheckState.Unchecked)]
        [Category("Fluent")]
        public CheckState CheckState
        {
            get { return _checkState; }
            set
            {
                if (_checkState != value)
                {
                    _checkState = value;
                    _checked = value != CheckState.Unchecked;
                    OnCheckedChanged(EventArgs.Empty);
                    OnCheckStateChanged(EventArgs.Empty);
                    Invalidate();
                }
            }
        }

        [DefaultValue(true)]
        public override bool AutoSize
        {
            get { return _autoSize; }
            set
            {
                _autoSize = value;
                if (value)
                {
                    UpdateAutoSize();
                }
            }
        }

        protected virtual void OnCheckedChanged(EventArgs e)
        {
            CheckedChanged?.Invoke(this, e);
        }

        protected virtual void OnCheckStateChanged(EventArgs e)
        {
            CheckStateChanged?.Invoke(this, e);
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            UpdateAutoSize();
            Invalidate();
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            UpdateAutoSize();
            Invalidate();
        }

        private void UpdateAutoSize()
        {
            if (!_autoSize)
            {
                return;
            }

            SizeF textSize = TextRenderer.MeasureText(Text ?? string.Empty, Font);

            int width = BoxSize + (string.IsNullOrEmpty(Text) ? 0 : TextGap + (int)Math.Ceiling(textSize.Width));
            int height = Math.Max(BoxSize + 4, (int)Math.Ceiling(textSize.Height) + 4);
            Size = new Size(width, height);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button == MouseButtons.Left && ClientRectangle.Contains(e.Location))
            {
                Toggle();
            }
        }

        protected override bool IsInputKey(Keys keyData)
        {
            return keyData == Keys.Space || base.IsInputKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Space)
            {
                Toggle();
                e.Handled = true;
            }
        }

        private void Toggle()
        {
            if (!Enabled)
            {
                return;
            }

            if (_threeState)
            {
                switch (_checkState)
                {
                    case CheckState.Unchecked: CheckState = CheckState.Checked; break;
                    case CheckState.Checked: CheckState = CheckState.Indeterminate; break;
                    default: CheckState = CheckState.Unchecked; break;
                }
            }
            else
            {
                CheckState = _checked ? CheckState.Unchecked : CheckState.Checked;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            ControlHelper.PaintFullBackground(this, e.Graphics);
            Graphics g = e.Graphics;
            FluentGraphics.SetupGraphics(g);

            FluentVisualState state = VisualState;
            bool isChecked = _checkState != CheckState.Unchecked;

            float y = (Height - BoxSize) / 2f;
            var box = new RectangleF(0.5f, y + 0.5f, BoxSize - 1f, BoxSize - 1f);

            Color fill;
            Color stroke;

            if (isChecked)
            {
                switch (state)
                {
                    case FluentVisualState.Hover: fill = Palette.AccentFillSecondary; break;
                    case FluentVisualState.Pressed: fill = Palette.AccentFillTertiary; break;
                    case FluentVisualState.Disabled: fill = Palette.AccentFillDisabled; break;
                    default: fill = Palette.AccentFillDefault; break;
                }

                stroke = Color.Transparent;
            }
            else
            {
                switch (state)
                {
                    case FluentVisualState.Hover: fill = ControlHelper.BlendOverParent(this, Palette.ControlAltFillTertiary); break;
                    case FluentVisualState.Pressed: fill = ControlHelper.BlendOverParent(this, Palette.ControlAltFillQuarternary); break;
                    case FluentVisualState.Disabled: fill = ControlHelper.BlendOverParent(this, Palette.ControlAltFillDisabled); break;
                    default: fill = ControlHelper.BlendOverParent(this, Palette.ControlAltFillSecondary); break;
                }

                stroke = state == FluentVisualState.Disabled
                    ? Palette.ControlStrongStrokeDisabled
                    : Palette.ControlStrongStrokeDefault;
            }

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

            if (isChecked)
            {
                Color glyphColor = state == FluentVisualState.Disabled
                    ? Palette.TextOnAccentDisabled
                    : Palette.TextOnAccentPrimary;

                if (_checkState == CheckState.Indeterminate)
                {
                    using (var brush = new SolidBrush(glyphColor))
                    {
                        g.FillRectangle(brush, box.X + 5, box.Y + box.Height / 2f - 1f, box.Width - 10, 2f);
                    }
                }
                else
                {
                    // Checkmark glyph E73E (Segoe Fluent Icons "CheckMark").
                    FluentGraphics.DrawGlyph(g, FluentGraphics.Glyph(0xE73E), FluentTypography.Icon(10f), glyphColor, box);
                }
            }

            if (!string.IsNullOrEmpty(Text))
            {
                Color textColor = Enabled ? Palette.TextFillPrimary : Palette.TextFillDisabled;
                using (var format = new StringFormat { LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap })
                {
                    FluentGraphics.DrawText(g, Text, Font, textColor, new RectangleF(BoxSize + TextGap, 0, Width - BoxSize - TextGap, Height), format);
                }
            }

            if (Focused && ShowFocusCues)
            {
                FluentGraphics.DrawFocusVisual(g, new RectangleF(box.X - 2, box.Y - 2, box.Width + 4, box.Height + 4), FluentGraphics.ControlCornerRadius, Palette);
            }
        }
    }
}