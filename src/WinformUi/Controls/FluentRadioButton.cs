using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using WinformUi.Drawing;
using WinformUi.Theming;

namespace WinformUi.Controls
{
    /// <summary>
    /// Fluent radio button: 20px circle, accent fill + white dot when checked.
    /// Radio behavior is automatic between FluentRadioButtons sharing the same parent.
    /// </summary>
    [System.ComponentModel.ToolboxItem(true)]
    public class FluentRadioButton : FluentControlBase
    {
        private const int CircleSize = 20;
        private const int TextGap = 8;

        private bool _checked;
        private bool _autoSize = true;

        public event EventHandler CheckedChanged;

        public FluentRadioButton()
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
                if (_checked != value)
                {
                    _checked = value;
                    if (value)
                    {
                        UncheckSiblings();
                    }

                    OnCheckedChanged(EventArgs.Empty);
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

        private void UncheckSiblings()
        {
            if (Parent == null)
            {
                return;
            }

            foreach (Control sibling in Parent.Controls)
            {
                if (sibling != this && sibling is FluentRadioButton radio && radio.Checked)
                {
                    radio.Checked = false;
                }
            }
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

            int width = CircleSize + (string.IsNullOrEmpty(Text) ? 0 : TextGap + (int)Math.Ceiling(textSize.Width));
            int height = Math.Max(CircleSize + 4, (int)Math.Ceiling(textSize.Height) + 4);
            Size = new Size(width, height);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button == MouseButtons.Left && ClientRectangle.Contains(e.Location) && Enabled)
            {
                Checked = true;
            }
        }

        protected override bool IsInputKey(Keys keyData)
        {
            return keyData == Keys.Space || base.IsInputKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Space && Enabled)
            {
                Checked = true;
                e.Handled = true;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            ControlHelper.PaintFullBackground(this, e.Graphics);
            Graphics g = e.Graphics;
            FluentGraphics.SetupGraphics(g);

            FluentVisualState state = VisualState;
            float y = (Height - CircleSize) / 2f;
            var circle = new RectangleF(0.5f, y + 0.5f, CircleSize - 1f, CircleSize - 1f);

            if (_checked)
            {
                Color fill;
                switch (state)
                {
                    case FluentVisualState.Hover: fill = Palette.AccentFillSecondary; break;
                    case FluentVisualState.Pressed: fill = Palette.AccentFillTertiary; break;
                    case FluentVisualState.Disabled: fill = Palette.AccentFillDisabled; break;
                    default: fill = Palette.AccentFillDefault; break;
                }

                using (var brush = new SolidBrush(fill))
                {
                    g.FillEllipse(brush, circle);
                }

                Color dotColor = state == FluentVisualState.Disabled ? Palette.TextOnAccentDisabled : Palette.TextOnAccentPrimary;
                float dotSize = state == FluentVisualState.Pressed ? 8f : 10f;
                using (var brush = new SolidBrush(dotColor))
                {
                    g.FillEllipse(brush,
                        circle.X + (circle.Width - dotSize) / 2f,
                        circle.Y + (circle.Height - dotSize) / 2f,
                        dotSize, dotSize);
                }
            }
            else
            {
                Color fill;
                switch (state)
                {
                    case FluentVisualState.Hover: fill = ControlHelper.BlendOverParent(this, Palette.ControlAltFillTertiary); break;
                    case FluentVisualState.Pressed: fill = ControlHelper.BlendOverParent(this, Palette.ControlAltFillQuarternary); break;
                    case FluentVisualState.Disabled: fill = ControlHelper.BlendOverParent(this, Palette.ControlAltFillDisabled); break;
                    default: fill = ControlHelper.BlendOverParent(this, Palette.ControlAltFillSecondary); break;
                }

                using (var brush = new SolidBrush(fill))
                {
                    g.FillEllipse(brush, circle);
                }

                Color stroke = state == FluentVisualState.Disabled || state == FluentVisualState.Pressed
                    ? Palette.ControlStrongStrokeDisabled
                    : Palette.ControlStrongStrokeDefault;
                using (var pen = new Pen(stroke, 1f))
                {
                    g.DrawEllipse(pen, circle);
                }
            }

            if (!string.IsNullOrEmpty(Text))
            {
                Color textColor = Enabled ? Palette.TextFillPrimary : Palette.TextFillDisabled;
                using (var format = new StringFormat { LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap })
                {
                    FluentGraphics.DrawText(g, Text, Font, textColor, new RectangleF(CircleSize + TextGap, 0, Width - CircleSize - TextGap, Height), format);
                }
            }

            if (Focused && ShowFocusCues)
            {
                FluentGraphics.DrawFocusVisual(g, new RectangleF(circle.X - 2, circle.Y - 2, circle.Width + 4, circle.Height + 4), circle.Width / 2f, Palette);
            }
        }
    }
}