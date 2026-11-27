using System;
using System.Drawing;
using System.Windows.Forms;
using WinformUi.Drawing;
using WinformUi.Interop;
using WinformUi.Theming;

namespace WinformUi.Controls
{
    /// <summary>Visual appearance variants for buttons and other controls.</summary>
    public enum ControlAppearance
    {
        Secondary,
        Primary,
        Info,
        Success,
        Caution,
        Danger,
        Transparent,
        Dark,
        Light
    }

    public static class ControlHelper
    {
#if NETFRAMEWORK
        private static bool _dpiForWindowMissing;
#endif

        /// <summary>
        /// Control.DeviceDpi requires .NET Framework 4.7+; on net46 query the OS
        /// (per-window DPI when available, system DPI otherwise).
        /// </summary>
        public static int DpiOf(Control control)
        {
#if NETFRAMEWORK
            if (control.IsHandleCreated && !_dpiForWindowMissing)
            {
                try
                {
                    int dpi = NativeMethods.GetDpiForWindow(control.Handle);
                    if (dpi > 0)
                    {
                        return dpi;
                    }
                }
                catch (EntryPointNotFoundException)
                {
                    _dpiForWindowMissing = true;
                }
                catch (DllNotFoundException)
                {
                    _dpiForWindowMissing = true;
                }
            }

            IntPtr hdc = NativeMethods.GetDC(IntPtr.Zero);
            try
            {
                return NativeMethods.GetDeviceCaps(hdc, 88); // LOGPIXELSX
            }
            finally
            {
                NativeMethods.ReleaseDC(IntPtr.Zero, hdc);
            }
#else
            return control.DeviceDpi;
#endif
        }
        /// <summary>
        /// Fluent tokens are often semi-transparent; composite them over the
        /// nearest opaque ancestor background so GDI+ paints a solid color.
        /// </summary>
        public static Color BlendOverParent(Control control, Color token)
        {
            if (token.A == 255)
            {
                return token;
            }

            Color background = GetEffectiveBackground(control);
            return AccentColor.Blend(token, background);
        }

        public static void PaintFullBackground(Control control, Graphics g)
        {
            // The double-buffered paint path never raises OnPaintBackground, and
            // Render() blits the entire reused buffer, so every OnPaint must clear
            // the whole buffer first or stale frames blit over good screen areas.
            // The graphics is clipped to the update region even inside the buffer.
            g.ResetClip();
            Color own = control.BackColor;
            if (control.FindForm() is FluentWindow window && window.IsBackdropApplied)
            {
                bool ownOpaque = own.A == 255 && own != Color.Black;
                bool opaqueAncestor = false;
                for (Control c = control.Parent; c != null; c = c.Parent)
                {
                    Color b = c.BackColor;
                    if (b.A == 255 && b != Color.Black && b != Color.Transparent)
                    {
                        opaqueAncestor = true;
                        break;
                    }
                }

                if (!ownOpaque && !opaqueAncestor)
                {
                    // No surface beneath: alpha 0 lets the DWM system backdrop
                    // show through. GDI+ skips alpha-0 fills, so seed the RGB
                    // directly: GDI text blends against the buffer RGB, which
                    // must approximate the backdrop instead of staying black.
                    Color seed = ApplicationThemeManager.Palette.ApplicationBackground;
                    FluentGraphics.SeedTransparentBackground(g, seed);
                    return;
                }
            }

            Color back = own.A == 255 ? own : GetEffectiveBackground(control);
            using (var brush = new SolidBrush(back))
            {
                g.FillRectangle(brush, 0, 0, control.Width, control.Height);
            }
        }

        public static Color GetEffectiveBackground(Control control)
        {
            // Ancestors inherit the backdrop window's black BackColor; that black is
            // DWM transparency, not a real surface, so translucent tokens must blend
            // over the theme background instead of it.
            bool backdropBlack = control.FindForm() is FluentWindow window && window.IsBackdropApplied;
            Control current = control.Parent;
            while (current != null)
            {
                Color back = current.BackColor;
                if (backdropBlack && back == Color.Black)
                {
                    current = current.Parent;
                    continue;
                }

                if (back.A == 255 && back != Color.Transparent)
                {
                    return back;
                }

                current = current.Parent;
            }

            return ApplicationThemeManager.Palette.ApplicationBackground;
        }

        /// <summary>Standard button-like fill for a visual state and appearance.</summary>
        public static Color GetFill(Control control, ControlAppearance appearance, FluentVisualState state, bool isChecked = false)
        {
            FluentPalette p = ApplicationThemeManager.Palette;

            switch (appearance)
            {
                case ControlAppearance.Primary:
                    switch (state)
                    {
                        case FluentVisualState.Hover: return p.AccentFillSecondary;
                        case FluentVisualState.Pressed: return p.AccentFillTertiary;
                        case FluentVisualState.Disabled: return p.AccentFillDisabled;
                        default: return p.AccentFillDefault;
                    }

                case ControlAppearance.Transparent:
                    switch (state)
                    {
                        case FluentVisualState.Hover: return BlendOverParent(control, p.SubtleFillSecondary);
                        case FluentVisualState.Pressed: return BlendOverParent(control, p.SubtleFillTertiary);
                        default: return Color.Transparent;
                    }

                case ControlAppearance.Info:
                    return StateColor(control, p.SystemFillAttention, p.SystemFillAttentionBackground, state);
                case ControlAppearance.Success:
                    return StateColor(control, p.SystemFillSuccess, p.SystemFillSuccessBackground, state);
                case ControlAppearance.Caution:
                    return StateColor(control, p.SystemFillCaution, p.SystemFillCautionBackground, state);
                case ControlAppearance.Danger:
                    return StateColor(control, p.SystemFillCritical, p.SystemFillCriticalBackground, state);

                case ControlAppearance.Dark:
                    return Color.FromArgb(255, 32, 32, 32);
                case ControlAppearance.Light:
                    return Color.FromArgb(255, 250, 250, 250);

                default:
                    if (isChecked)
                    {
                        switch (state)
                        {
                            case FluentVisualState.Hover: return p.AccentFillSecondary;
                            case FluentVisualState.Pressed: return p.AccentFillTertiary;
                            case FluentVisualState.Disabled: return p.AccentFillDisabled;
                            default: return p.AccentFillDefault;
                        }
                    }

                    switch (state)
                    {
                        case FluentVisualState.Hover: return BlendOverParent(control, p.ControlFillSecondary);
                        case FluentVisualState.Pressed: return BlendOverParent(control, p.ControlFillTertiary);
                        case FluentVisualState.Disabled: return BlendOverParent(control, p.ControlFillDisabled);
                        default: return BlendOverParent(control, p.ControlFillDefault);
                    }
            }
        }

        private static Color StateColor(Control control, Color strong, Color background, FluentVisualState state)
        {
            FluentPalette p = ApplicationThemeManager.Palette;
            switch (state)
            {
                case FluentVisualState.Hover: return AccentColor.Blend(strong, BlendOverParent(control, background));
                case FluentVisualState.Pressed: return AccentColor.Blend(Color.FromArgb(160, strong), BlendOverParent(control, background));
                case FluentVisualState.Disabled: return BlendOverParent(control, p.ControlFillDisabled);
                default: return BlendOverParent(control, background);
            }
        }

        public static Color GetForeground(ControlAppearance appearance, FluentVisualState state, bool isChecked = false)
        {
            FluentPalette p = ApplicationThemeManager.Palette;

            if (state == FluentVisualState.Disabled)
            {
                return appearance == ControlAppearance.Primary || isChecked
                    ? p.TextOnAccentDisabled
                    : p.TextFillDisabled;
            }

            if (appearance == ControlAppearance.Primary || isChecked)
            {
                return state == FluentVisualState.Pressed ? p.TextOnAccentSecondary : p.TextOnAccentPrimary;
            }

            if (appearance == ControlAppearance.Dark)
            {
                return Color.White;
            }

            if (appearance == ControlAppearance.Light)
            {
                return Color.FromArgb(228, 0, 0, 0);
            }

            return state == FluentVisualState.Pressed ? p.TextFillSecondary : p.TextFillPrimary;
        }
    }
}