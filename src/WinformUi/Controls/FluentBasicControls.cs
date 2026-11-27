using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using WinformUi.Theming;

namespace WinformUi.Controls
{
    /// <summary>Fluent label: secondary text color, theme aware.</summary>
    public class FluentLabel : Label
    {
        private ThemeSubscription _themeSubscription;
        private bool _useSecondaryColor = true;

        protected override void OnPaintBackground(PaintEventArgs e) => ControlHelper.PaintFullBackground(this, e.Graphics);

        protected override void OnPaint(PaintEventArgs e)
        {
            ControlHelper.PaintFullBackground(this, e.Graphics);
            base.OnPaint(e);
        }

        public FluentLabel()
        {
            Font = FluentTypography.Body;
            ForeColor = ApplicationThemeManager.Palette.TextFillSecondary;
            // GDI text leaves pixel alpha at 0, which the DWM system backdrop
            // composites as transparent; GDI+ writes real alpha.
            UseCompatibleTextRendering = true;
            UseMnemonic = false;
        }

        /// <summary>Use the secondary text token (default) instead of primary.</summary>
        [DefaultValue(true)]
        [Category("Fluent")]
        public bool UseSecondaryColor
        {
            get { return _useSecondaryColor; }
            set
            {
                _useSecondaryColor = value;
                ApplyTheme();
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (_themeSubscription == null)
            {
                _themeSubscription = new ThemeSubscription(this, ApplyTheme);
            }

            ApplyTheme();
        }

        private void ApplyTheme()
        {
            FluentPalette p = ApplicationThemeManager.Palette;
            ForeColor = Enabled
                ? (_useSecondaryColor ? p.TextFillSecondary : p.TextFillPrimary)
                : p.TextFillDisabled;
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            ApplyTheme();
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

    /// <summary>Fluent divider line.</summary>
    public class FluentSeparator : Control
    {
        private ThemeSubscription _themeSubscription;
        private Orientation _orientation = Orientation.Horizontal;

        protected override void OnPaintBackground(PaintEventArgs e) => ControlHelper.PaintFullBackground(this, e.Graphics);

        public FluentSeparator()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor, true);
            TabStop = false;
            Size = new Size(100, 1);
            BackColor = Color.Transparent;
        }

        [DefaultValue(Orientation.Horizontal)]
        [Category("Fluent")]
        public Orientation Orientation
        {
            get { return _orientation; }
            set
            {
                _orientation = value;
                Invalidate();
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (_themeSubscription == null)
            {
                _themeSubscription = new ThemeSubscription(this, () => Invalidate());
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            ControlHelper.PaintFullBackground(this, e.Graphics);
            Color line = ControlHelper.BlendOverParent(this, ApplicationThemeManager.Palette.DividerStrokeDefault);
            using (var pen = new Pen(line, 1f))
            {
                if (_orientation == Orientation.Horizontal)
                {
                    e.Graphics.DrawLine(pen, 0, Height / 2f, Width, Height / 2f);
                }
                else
                {
                    e.Graphics.DrawLine(pen, Width / 2f, 0, Width / 2f, Height);
                }
            }
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

    /// <summary>Fluent hyperlink button: accent text on subtle hover fill.</summary>
    public class FluentHyperlinkButton : FluentButton
    {
        private string _navigateUri;

        public FluentHyperlinkButton()
        {
            Appearance = ControlAppearance.Transparent;
            Cursor = Cursors.Hand;
            Padding = new Padding(8, 4, 8, 4);
        }

        [DefaultValue(null)]
        [Category("Fluent")]
        public string NavigateUri
        {
            get { return _navigateUri; }
            set { _navigateUri = value; }
        }

        protected override Color GetForegroundColor(FluentVisualState state)
        {
            FluentPalette p = ApplicationThemeManager.Palette;
            switch (state)
            {
                case FluentVisualState.Disabled: return p.AccentTextFillDisabled;
                case FluentVisualState.Hover: return p.AccentTextFillSecondary;
                case FluentVisualState.Pressed: return p.AccentTextFillTertiary;
                default: return p.AccentTextFillPrimary;
            }
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            if (!string.IsNullOrEmpty(_navigateUri))
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(_navigateUri) { UseShellExecute = true });
                }
                catch
                {
                    // Invalid URI or no shell handler; navigation is best-effort.
                }
            }
        }
    }

    /// <summary>Fluent toggle button: accent fill while checked.</summary>
    public class FluentToggleButton : FluentButton
    {
        private bool _checked;

        public event EventHandler CheckedChanged;

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
                    OnCheckedChanged(EventArgs.Empty);
                    Invalidate();
                }
            }
        }

        protected override bool IsCheckedVisual => _checked;

        protected virtual void OnCheckedChanged(EventArgs e)
        {
            CheckedChanged?.Invoke(this, e);
        }

        protected override void OnClick(EventArgs e)
        {
            Checked = !_checked;
            base.OnClick(e);
        }
    }
}