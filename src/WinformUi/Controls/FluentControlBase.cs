using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using WinformUi.Theming;

namespace WinformUi.Controls
{
    /// <summary>
    /// Base class for fully owner-drawn Fluent controls.
    /// Provides theme subscription, mouse state tracking and DPI scaling.
    /// </summary>
    [System.ComponentModel.ToolboxItem(false)]
    public class FluentControlBase : Control
    {
        private ThemeSubscription _themeSubscription;
        private bool _isHovered;
        private bool _isPressed;

        protected override void OnPaintBackground(PaintEventArgs e) => ControlHelper.PaintFullBackground(this, e.Graphics);

        public FluentControlBase()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint
                | ControlStyles.UserPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw
                | ControlStyles.SupportsTransparentBackColor, true);
            Font = FluentTypography.Body;
            TabStop = true;
        }

        protected FluentPalette Palette => ApplicationThemeManager.Palette;

        protected float DpiScale => ControlHelper.DpiOf(this) / 96f;

        protected int Scale(int value)
        {
            return (int)Math.Round(value * DpiScale);
        }

        protected float ScaleF(float value)
        {
            return value * DpiScale;
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IsHovered
        {
            get { return _isHovered; }
            private set
            {
                if (_isHovered != value)
                {
                    _isHovered = value;
                    Invalidate();
                }
            }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IsPressed
        {
            get { return _isPressed; }
            private set
            {
                if (_isPressed != value)
                {
                    _isPressed = value;
                    Invalidate();
                }
            }
        }

        /// <summary>Effective visual state used by paint code.</summary>
        protected FluentVisualState VisualState
        {
            get
            {
                if (!Enabled)
                {
                    return FluentVisualState.Disabled;
                }

                if (IsPressed)
                {
                    return FluentVisualState.Pressed;
                }

                if (IsHovered)
                {
                    return FluentVisualState.Hover;
                }

                return FluentVisualState.Normal;
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (_themeSubscription == null)
            {
                _themeSubscription = new ThemeSubscription(this, OnThemeChanged);
            }

            ApplyTheme();
        }

        protected virtual void OnThemeChanged()
        {
            ApplyTheme();
            Invalidate();
        }

        /// <summary>Push palette colors into inherited properties (ForeColor etc.).</summary>
        protected virtual void ApplyTheme()
        {
            ForeColor = Palette.TextFillPrimary;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            IsHovered = true;
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            IsHovered = false;
            IsPressed = false;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
            {
                IsPressed = true;
                if (TabStop && !Focused)
                {
                    Focus();
                }
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            IsPressed = false;
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            Invalidate();
        }

        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            Invalidate();
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            IsPressed = false;
            Invalidate();
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

    public enum FluentVisualState
    {
        Normal,
        Hover,
        Pressed,
        Disabled
    }
}