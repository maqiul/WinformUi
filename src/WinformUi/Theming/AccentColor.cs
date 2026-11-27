using System;
using System.Drawing;
using Microsoft.Win32;

namespace WinformUi.Theming
{
    /// <summary>
    /// Reads the Windows accent color and derives the Fluent accent variants
    /// (Dark1/2/3 for light theme, Light1/2/3 for dark theme).
    /// </summary>
    public static class AccentColor
    {
        public static readonly Color Default = Color.FromArgb(255, 0, 120, 212); // #0078D4

        public static Color GetSystemAccent()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\DWM"))
                {
                    if (key != null)
                    {
                        object value = key.GetValue("AccentColor");
                        if (value is int dword)
                        {
                            // DWORD is 0xAABBGGRR
                            uint v = unchecked((uint)dword);
                            return Color.FromArgb(255, (int)(v & 0xFF), (int)((v >> 8) & 0xFF), (int)((v >> 16) & 0xFF));
                        }
                    }
                }
            }
            catch
            {
                // Registry access may fail in restricted environments.
            }

            return Default;
        }

        public static bool IsSystemThemeDark()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    if (key != null)
                    {
                        object value = key.GetValue("AppsUseLightTheme");
                        if (value is int i)
                        {
                            return i == 0;
                        }
                    }
                }
            }
            catch
            {
            }

            return false;
        }

        internal static void FillAccent(FluentPalette palette, Color accent)
        {
            bool dark = palette.Theme == ApplicationTheme.Dark;

            Color primary = dark ? Shift(accent, +0.18f) : Shift(accent, -0.16f);
            Color secondary = dark ? Shift(accent, +0.34f) : Shift(accent, -0.32f);
            Color tertiary = dark ? Shift(accent, +0.50f) : Shift(accent, -0.48f);

            palette.SystemAccentColor = accent;
            palette.SystemAccentColorPrimary = primary;
            palette.SystemAccentColorSecondary = secondary;
            palette.SystemAccentColorTertiary = tertiary;

            // Accent.xaml: AccentFillColorDefault = SystemAccentColorSecondary,
            // Secondary/Tertiary are 0.9 / 0.8 opacity over the layer beneath.
            Color baseLayer = palette.SolidBackgroundFillBase;
            palette.AccentFillDefault = secondary;
            palette.AccentFillSecondary = Blend(secondary, baseLayer, 0.9f);
            palette.AccentFillTertiary = Blend(secondary, baseLayer, 0.8f);

            palette.AccentTextFillPrimary = secondary;
            palette.AccentTextFillSecondary = tertiary;
            palette.AccentTextFillTertiary = primary;
            palette.AccentFillSelectedTextBackground = accent;
            palette.SystemFillAttention = accent;

            // Choose readable text over the accent based on luminance.
            bool accentIsLight = Luminance(secondary) > 0.55f;
            palette.TextOnAccentPrimary = accentIsLight ? Color.FromArgb(228, 0, 0, 0) : Color.White;
            palette.TextOnAccentSecondary = accentIsLight ? Color.FromArgb(128, 0, 0, 0) : Color.FromArgb(179, 255, 255, 255);
            palette.TextOnAccentDisabled = accentIsLight ? Color.FromArgb(119, 0, 0, 0) : Color.FromArgb(163, 255, 255, 255);
        }

        private static Color Shift(Color color, float amount)
        {
            float h, s, v;
            ToHsv(color, out h, out s, out v);
            v = Clamp01(v + amount);
            if (amount > 0)
            {
                s = Clamp01(s - amount * 0.35f);
            }

            return FromHsv(h, s, v);
        }

        private static float Luminance(Color c)
        {
            return (0.2126f * c.R + 0.7152f * c.G + 0.0722f * c.B) / 255f;
        }

        internal static Color Blend(Color top, Color bottom, float opacity)
        {
            int a = (int)Math.Round(top.A * opacity);
            return Blend(Color.FromArgb(a, top.R, top.G, top.B), bottom);
        }

        /// <summary>Alpha-composites <paramref name="top"/> over an opaque <paramref name="bottom"/>.</summary>
        public static Color Blend(Color top, Color bottom)
        {
            if (top.A == 255)
            {
                return top;
            }

            if (top.A == 0)
            {
                return bottom;
            }

            float a = top.A / 255f;
            int r = (int)Math.Round(top.R * a + bottom.R * (1 - a));
            int g = (int)Math.Round(top.G * a + bottom.G * (1 - a));
            int b = (int)Math.Round(top.B * a + bottom.B * (1 - a));
            return Color.FromArgb(255, r, g, b);
        }

        private static float Clamp01(float v)
        {
            return v < 0 ? 0 : (v > 1 ? 1 : v);
        }

        private static void ToHsv(Color c, out float h, out float s, out float v)
        {
            float r = c.R / 255f, g = c.G / 255f, b = c.B / 255f;
            float max = Math.Max(r, Math.Max(g, b));
            float min = Math.Min(r, Math.Min(g, b));
            float delta = max - min;

            v = max;
            s = max == 0 ? 0 : delta / max;

            if (delta == 0)
            {
                h = 0;
            }
            else if (max == r)
            {
                h = 60 * (((g - b) / delta) % 6);
            }
            else if (max == g)
            {
                h = 60 * (((b - r) / delta) + 2);
            }
            else
            {
                h = 60 * (((r - g) / delta) + 4);
            }

            if (h < 0)
            {
                h += 360;
            }
        }

        private static Color FromHsv(float h, float s, float v)
        {
            float c = v * s;
            float x = c * (1 - Math.Abs((h / 60f) % 2 - 1));
            float m = v - c;
            float r, g, b;

            if (h < 60) { r = c; g = x; b = 0; }
            else if (h < 120) { r = x; g = c; b = 0; }
            else if (h < 180) { r = 0; g = c; b = x; }
            else if (h < 240) { r = 0; g = x; b = c; }
            else if (h < 300) { r = x; g = 0; b = c; }
            else { r = c; g = 0; b = x; }

            return Color.FromArgb(255,
                (int)Math.Round((r + m) * 255),
                (int)Math.Round((g + m) * 255),
                (int)Math.Round((b + m) * 255));
        }
    }
}