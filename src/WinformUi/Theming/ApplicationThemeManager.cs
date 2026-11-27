using System;
using System.Drawing;
using System.Windows.Forms;

namespace WinformUi.Theming
{
    /// <summary>
    /// Global theme entry point: switches light/dark palettes and notifies subscribers.
    /// Call <see cref="Apply(ApplicationTheme)"/> once at startup; every Fluent control
    /// picks up the current <see cref="Palette"/> and repaints on <see cref="ThemeChanged"/>.
    /// </summary>
    public static class ApplicationThemeManager
    {
        private static FluentPalette _palette = BuildPalette(ApplicationTheme.Light, Theming.AccentColor.GetSystemAccent());

        public static event EventHandler ThemeChanged;

        public static ApplicationTheme CurrentTheme { get; private set; } = ApplicationTheme.Light;

        /// <summary>The theme actually in effect (System is resolved to Light/Dark).</summary>
        public static ApplicationTheme EffectiveTheme => _palette.Theme;

        public static FluentPalette Palette => _palette;

        public static Color AccentColor => _palette.SystemAccentColor;

        public static void Apply(ApplicationTheme theme)
        {
            Apply(theme, Theming.AccentColor.GetSystemAccent());
        }

        public static void Apply(ApplicationTheme theme, Color accent)
        {
            CurrentTheme = theme;
            _palette = BuildPalette(theme, accent);
            OnThemeChanged();
        }

        public static void ApplySystemTheme()
        {
            Apply(Theming.ApplicationTheme.System);
        }

        /// <summary>
        /// Re-reads the OS accent / app theme (useful after WM_SETTINGCHANGE).
        /// </summary>
        public static void RefreshSystemColors()
        {
            if (CurrentTheme == ApplicationTheme.System)
            {
                _palette = BuildPalette(ApplicationTheme.System, Theming.AccentColor.GetSystemAccent());
                OnThemeChanged();
            }
        }

        private static FluentPalette BuildPalette(ApplicationTheme theme, Color accent)
        {
            ApplicationTheme resolved = theme == ApplicationTheme.System
                ? (Theming.AccentColor.IsSystemThemeDark() ? ApplicationTheme.Dark : ApplicationTheme.Light)
                : theme;

            FluentPalette palette = resolved == ApplicationTheme.Dark
                ? FluentPalette.CreateDark()
                : FluentPalette.CreateLight();

            Theming.AccentColor.FillAccent(palette, accent);
            return palette;
        }

        private static void OnThemeChanged()
        {
            ThemeChanged?.Invoke(null, EventArgs.Empty);

            // Repaint everything, including plain WinForms controls hosted on Fluent forms.
            foreach (Form form in Application.OpenForms)
            {
                InvalidateTree(form);
            }
        }

        private static void InvalidateTree(Control control)
        {
            control.Invalidate(true);
            foreach (Control child in control.Controls)
            {
                InvalidateTree(child);
            }
        }
    }
}