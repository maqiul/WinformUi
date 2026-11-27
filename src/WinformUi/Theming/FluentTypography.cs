using System;
using System.Collections.Generic;
using System.Drawing;

namespace WinformUi.Theming
{
    /// <summary>
    /// Fluent typography ramp, sizes converted from DIP to points.
    /// </summary>
    public static class FluentTypography
    {
        // GDI+ realizes variable fonts unreliably: the same Regular request can
        // land on a heavier face depending on font creation order. Use the
        // static Segoe UI family, which GDI+ maps consistently.
        public const string FontFamily = "Segoe UI";
        public const string FallbackFamily = "Segoe UI";
        public const string IconFamily = "Segoe Fluent Icons";
        public const string IconFallbackFamily = "Segoe MDL2 Assets";

        private static string _resolvedFamily;
        private static string _resolvedIconFamily;
        private static readonly Dictionary<string, Font> Cache = new Dictionary<string, Font>();

        public static string ResolvedFamily
        {
            get
            {
                if (_resolvedFamily == null)
                {
                    _resolvedFamily = IsFontInstalled(FontFamily) ? FontFamily : FallbackFamily;
                }

                return _resolvedFamily;
            }
        }

        public static string ResolvedIconFamily
        {
            get
            {
                if (_resolvedIconFamily == null)
                {
                    _resolvedIconFamily = IsFontInstalled(IconFamily) ? IconFamily : IconFallbackFamily;
                }

                return _resolvedIconFamily;
            }
        }

        public static Font Caption => Get("Caption", 9f, FontStyle.Regular);          // 12 DIP
        public static Font Body => Get("Body", 10.5f, FontStyle.Regular);              // 14 DIP
        public static Font BodyStrong => Get("BodyStrong", 10.5f, FontStyle.Bold);
        public static Font BodyLarge => Get("BodyLarge", 13.5f, FontStyle.Regular);    // 18 DIP
        public static Font Subtitle => Get("Subtitle", 15f, FontStyle.Bold);           // 20 DIP
        public static Font Title => Get("Title", 21f, FontStyle.Bold);                 // 28 DIP
        public static Font TitleLarge => Get("TitleLarge", 30f, FontStyle.Bold);       // 40 DIP
        public static Font Display => Get("Display", 51f, FontStyle.Bold);             // 68 DIP

        public static Font Icon(float sizePt = 12f)
        {
            return Get("Icon_" + sizePt.ToString(System.Globalization.CultureInfo.InvariantCulture), sizePt, FontStyle.Regular, ResolvedIconFamily);
        }

        /// <summary>Font for SymbolRegular codepoints (embedded FluentSystemIcons).</summary>
        public static Font IconRegular(float sizePt = 12f)
        {
            System.Drawing.FontFamily family = FluentIconFonts.RegularFamily;
            return family != null ? GetPrivate("IconRegular_", sizePt, family) : Icon(sizePt);
        }

        /// <summary>Font for SymbolFilled codepoints (embedded FluentSystemIcons).</summary>
        public static Font IconFilled(float sizePt = 12f)
        {
            System.Drawing.FontFamily family = FluentIconFonts.FilledFamily;
            return family != null ? GetPrivate("IconFilled_", sizePt, family) : Icon(sizePt);
        }

        private static Font GetPrivate(string keyPrefix, float sizePt, System.Drawing.FontFamily family)
        {
            string cacheKey = keyPrefix + sizePt.ToString(System.Globalization.CultureInfo.InvariantCulture);
            Font font;
            if (!Cache.TryGetValue(cacheKey, out font))
            {
                font = new Font(family, sizePt, FontStyle.Regular, GraphicsUnit.Point);
                Cache[cacheKey] = font;
            }

            return font;
        }

        public static Font Get(string key, float sizePt, FontStyle style, string family = null)
        {
            string cacheKey = key + "|" + (family ?? ResolvedFamily);
            Font font;
            if (!Cache.TryGetValue(cacheKey, out font))
            {
                font = new Font(family ?? ResolvedFamily, sizePt, style, GraphicsUnit.Point);
                Cache[cacheKey] = font;
            }

            return font;
        }

        private static bool IsFontInstalled(string family)
        {
            try
            {
                using (var test = new Font(family, 10f, FontStyle.Regular, GraphicsUnit.Point))
                {
                    return string.Equals(test.Name, family, StringComparison.OrdinalIgnoreCase);
                }
            }
            catch
            {
                return false;
            }
        }
    }
}