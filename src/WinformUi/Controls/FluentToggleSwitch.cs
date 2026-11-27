using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using WinformUi.Drawing;
using WinformUi.Theming;

namespace WinformUi.Controls
{
    /// <summary>
    /// Fluent toggle switch: 40x20 pill with animated knob,
    /// optional On/Off content text.
    /// </summary>
    [System.ComponentModel.ToolboxItem(true)]
    public class FluentToggleSwitch : FluentControlBase
    {
        private const int TrackWidth = 40;
        private const int TrackHeight = 20;
        private const int TextGap = 10;

        private bool _checked;
        private string _onContent = "On";
        private string _offContent = "Off";
        private float _knobProgress; // 0 = off, 1 = on
        private readonly Timer _animationTimer;

        public event EventHandler CheckedChanged;

        public FluentToggleSwitch()
        {
            Size = new Size(110, 24);
            Cursor = Cursors.Hand;
            _animationTimer = new Timer { Interval = 15 };
            _animationTimer.Tick += OnAnimationTick;
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
                    _animationTimer.Start();
                    OnCheckedChanged(EventArgs.Empty);
                    Invalidate();
                }
            }
        }

        [DefaultValue("On")]
        [Category("Fluent")]
        public string OnContent
        {
            get { return _onContent; }
            set { _onContent = value; Invalidate(); }
        }

        [DefaultValue("Off")]
        [Category("Fluent")]
        public string OffContent
        {
            get { return _offContent; }
            set { _offContent = value; Invalidate(); }
        }

        protected virtual void OnCheckedChanged(EventArgs e)
        {
            CheckedChanged?.Invoke(this, e);
        }

        private void OnAnimationTick(object sender, EventArgs e)
        {
            const float step = 0.12f; // ~150ms transition
            float target = _checked ? 1f : 0f;

            if (Math.Abs(_knobProgress - target) <= step)
            {
                _knobProgress = target;
                _animationTimer.Stop();
            }
            else
            {
                _knobProgress += _checked ? step : -step;
            }

            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button == MouseButtons.Left && ClientRectangle.Contains(e.Location) && Enabled)
            {
                Checked = !_checked;
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
                Checked = !_checked;
                e.Handled = true;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            ControlHelper.PaintFullBackground(this, e.Graphics);
            Graphics g = e.Graphics;
            FluentGraphics.SetupGraphics(g);

            FluentVisualState state = VisualState;
            float y = (Height - TrackHeight) / 2f;
            var track = new RectangleF(0.5f, y + 0.5f, TrackWidth - 1f, TrackHeight - 1f);
            float radius = track.Height / 2f;

            Color trackFill;
            Color trackStroke = Color.Transparent;

            if (_checked)
            {
                switch (state)
                {
                    case FluentVisualState.Hover: trackFill = Palette.AccentFillSecondary; break;
                    case FluentVisualState.Pressed: trackFill = Palette.AccentFillTertiary; break;
                    case FluentVisualState.Disabled: trackFill = Palette.AccentFillDisabled; break;
                    default: trackFill = Palette.AccentFillDefault; break;
                }
            }
            else
            {
                switch (state)
                {
                    case FluentVisualState.Hover: trackFill = ControlHelper.BlendOverParent(this, Palette.ControlAltFillTertiary); break;
                    case FluentVisualState.Pressed: trackFill = ControlHelper.BlendOverParent(this, Palette.ControlAltFillQuarternary); break;
                    case FluentVisualState.Disabled: trackFill = ControlHelper.BlendOverParent(this, Palette.ControlAltFillDisabled); break;
                    default: trackFill = ControlHelper.BlendOverParent(this, Palette.ControlAltFillSecondary); break;
                }

                trackStroke = state == FluentVisualState.Disabled
                    ? Palette.ControlStrongStrokeDisabled
                    : Palette.ControlStrongStrokeDefault;
            }

            using (var brush = new SolidBrush(trackFill))
            {
                FluentGraphics.FillRoundedRect(g, brush, track, radius);
            }

            if (trackStroke.A > 0)
            {
                using (var pen = new Pen(trackStroke, 1f))
                {
                    FluentGraphics.DrawRoundedRect(g, pen, track, radius);
                }
            }

            // Knob: 12px off -> 14px on, slides left -> right.
            float knobSize = 12f + 2f * _knobProgress;
            if (state == FluentVisualState.Pressed)
            {
                knobSize += 1f;
            }

            float knobX = track.X + 4f + (track.Width - 8f - knobSize) * _knobProgress;
            float knobY = track.Y + (track.Height - knobSize) / 2f;

            Color knobColor;
            if (_checked)
            {
                knobColor = state == FluentVisualState.Disabled ? Palette.TextOnAccentDisabled : Palette.TextOnAccentPrimary;
            }
            else
            {
                knobColor = state == FluentVisualState.Disabled ? Palette.TextFillDisabled : Palette.TextFillSecondary;
            }

            using (var brush = new SolidBrush(knobColor))
            {
                g.FillEllipse(brush, knobX, knobY, knobSize, knobSize);
            }

            string content = _checked ? _onContent : _offContent;
            if (!string.IsNullOrEmpty(content))
            {
                Color textColor = Enabled ? Palette.TextFillPrimary : Palette.TextFillDisabled;
                using (var format = new StringFormat { LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap })
                {
                    FluentGraphics.DrawText(g, content, Font, textColor, new RectangleF(TrackWidth + TextGap, 0, Width - TrackWidth - TextGap, Height), format);
                }
            }

            if (Focused && ShowFocusCues)
            {
                FluentGraphics.DrawFocusVisual(g, new RectangleF(track.X - 2, track.Y - 2, track.Width + 4, track.Height + 4), radius, Palette);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _animationTimer.Stop();
                _animationTimer.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}