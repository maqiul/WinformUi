using System;
using System.Drawing;
using System.Globalization;

namespace WinformUi.Theming
{
    /// <summary>
    /// Fluent Design color tokens for the light and dark palettes.
    /// Accent-dependent colors are filled in by <see cref="ApplicationThemeManager"/>.
    /// </summary>
    public sealed class FluentPalette
    {
        public ApplicationTheme Theme { get; internal set; }

        public Color ApplicationBackground { get; internal set; }
        public Color KeyboardFocusBorder { get; internal set; }

        // Text
        public Color TextFillPrimary { get; internal set; }
        public Color TextFillSecondary { get; internal set; }
        public Color TextFillTertiary { get; internal set; }
        public Color TextFillDisabled { get; internal set; }
        public Color TextPlaceholder { get; internal set; }
        public Color TextFillInverse { get; internal set; }
        public Color AccentTextFillDisabled { get; internal set; }
        public Color TextOnAccentSelectedText { get; internal set; }
        public Color TextOnAccentPrimary { get; internal set; }
        public Color TextOnAccentSecondary { get; internal set; }
        public Color TextOnAccentDisabled { get; internal set; }

        // Control fills
        public Color ControlFillDefault { get; internal set; }
        public Color ControlFillSecondary { get; internal set; }
        public Color ControlFillTertiary { get; internal set; }
        public Color ControlFillDisabled { get; internal set; }
        public Color ControlFillTransparent { get; internal set; }
        public Color ControlFillInputActive { get; internal set; }
        public Color ControlStrongFillDefault { get; internal set; }
        public Color ControlStrongFillDisabled { get; internal set; }
        public Color ControlSolidFillDefault { get; internal set; }

        public Color SubtleFillTransparent { get; internal set; }
        public Color SubtleFillSecondary { get; internal set; }
        public Color SubtleFillTertiary { get; internal set; }
        public Color SubtleFillDisabled { get; internal set; }

        public Color ControlAltFillTransparent { get; internal set; }
        public Color ControlAltFillSecondary { get; internal set; }
        public Color ControlAltFillTertiary { get; internal set; }
        public Color ControlAltFillQuarternary { get; internal set; }
        public Color ControlAltFillDisabled { get; internal set; }

        public Color ControlOnImageFillDefault { get; internal set; }
        public Color ControlOnImageFillSecondary { get; internal set; }
        public Color ControlOnImageFillTertiary { get; internal set; }
        public Color ControlOnImageFillDisabled { get; internal set; }

        public Color AccentFillDisabled { get; internal set; }

        // Strokes
        public Color ControlStrokeDefault { get; internal set; }
        public Color ControlStrokeSecondary { get; internal set; }
        public Color ControlStrokeTertiary { get; internal set; }
        public Color ControlStrokeOnAccentDefault { get; internal set; }
        public Color ControlStrokeOnAccentSecondary { get; internal set; }
        public Color ControlStrokeOnAccentTertiary { get; internal set; }
        public Color ControlStrokeOnAccentDisabled { get; internal set; }
        public Color ControlStrokeForStrongFillWhenOnImage { get; internal set; }
        public Color CardStrokeDefault { get; internal set; }
        public Color CardStrokeDefaultSolid { get; internal set; }
        public Color ControlStrongStrokeDefault { get; internal set; }
        public Color ControlStrongStrokeDisabled { get; internal set; }
        public Color SurfaceStrokeDefault { get; internal set; }
        public Color SurfaceStrokeFlyout { get; internal set; }
        public Color SurfaceStrokeInverse { get; internal set; }
        public Color DividerStrokeDefault { get; internal set; }
        public Color FocusStrokeOuter { get; internal set; }
        public Color FocusStrokeInner { get; internal set; }

        // Cards / layers
        public Color CardBackgroundFillDefault { get; internal set; }
        public Color CardBackgroundFillSecondary { get; internal set; }
        public Color SmokeFillDefault { get; internal set; }
        public Color LayerFillDefault { get; internal set; }
        public Color LayerFillAlt { get; internal set; }
        public Color LayerOnAcrylicFillDefault { get; internal set; }
        public Color LayerOnAccentAcrylicFillDefault { get; internal set; }
        public Color AcrylicBackgroundFillDefault { get; internal set; }
        public Color LayerOnMicaBaseAltFillDefault { get; internal set; }
        public Color LayerOnMicaBaseAltFillSecondary { get; internal set; }
        public Color LayerOnMicaBaseAltFillTertiary { get; internal set; }
        public Color LayerOnMicaBaseAltFillTransparent { get; internal set; }

        public Color SolidBackgroundFillBase { get; internal set; }
        public Color SolidBackgroundFillSecondary { get; internal set; }
        public Color SolidBackgroundFillTertiary { get; internal set; }
        public Color SolidBackgroundFillQuarternary { get; internal set; }
        public Color SolidBackgroundFillBaseAlt { get; internal set; }

        // System status colors
        public Color SystemFillAttention { get; internal set; }
        public Color SystemFillInformational { get; internal set; }
        public Color SystemFillSuccess { get; internal set; }
        public Color SystemFillCaution { get; internal set; }
        public Color SystemFillCritical { get; internal set; }
        public Color SystemFillNeutral { get; internal set; }
        public Color SystemFillSolidNeutral { get; internal set; }
        public Color SystemFillAttentionBackground { get; internal set; }
        public Color SystemFillSuccessBackground { get; internal set; }
        public Color SystemFillCautionBackground { get; internal set; }
        public Color SystemFillCriticalBackground { get; internal set; }
        public Color SystemFillNeutralBackground { get; internal set; }
        public Color SystemFillSolidAttentionBackground { get; internal set; }
        public Color SystemFillSolidNeutralBackground { get; internal set; }

        // Accent (computed by ApplicationThemeManager)
        public Color SystemAccentColor { get; internal set; }
        public Color SystemAccentColorPrimary { get; internal set; }
        public Color SystemAccentColorSecondary { get; internal set; }
        public Color SystemAccentColorTertiary { get; internal set; }
        public Color AccentFillDefault { get; internal set; }
        public Color AccentFillSecondary { get; internal set; }
        public Color AccentFillTertiary { get; internal set; }
        public Color AccentTextFillPrimary { get; internal set; }
        public Color AccentTextFillSecondary { get; internal set; }
        public Color AccentTextFillTertiary { get; internal set; }
        public Color AccentFillSelectedTextBackground { get; internal set; }

        internal static Color C(string hex)
        {
            // #AARRGGBB, or #RRGGBB meaning fully opaque
            string s = hex.TrimStart('#');
            if (s.Length == 6)
            {
                s = "FF" + s;
            }

            uint v = uint.Parse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            return Color.FromArgb((int)((v >> 24) & 0xFF), (int)((v >> 16) & 0xFF), (int)((v >> 8) & 0xFF), (int)(v & 0xFF));
        }

        public static FluentPalette CreateLight()
        {
            return new FluentPalette
            {
                Theme = ApplicationTheme.Light,
                ApplicationBackground = C("#FFFAFAFA"),
                KeyboardFocusBorder = C("#BE000000"),

                TextFillPrimary = C("#E4000000"),
                TextFillSecondary = C("#9E000000"),
                TextFillTertiary = C("#72000000"),
                TextFillDisabled = C("#5C000000"),
                TextPlaceholder = C("#9E000000"),
                TextFillInverse = C("#FFFFFF"),
                AccentTextFillDisabled = C("#5C000000"),
                TextOnAccentSelectedText = C("#FFFFFF"),
                TextOnAccentPrimary = C("#FFFFFF"),
                TextOnAccentSecondary = C("#B3FFFFFF"),
                TextOnAccentDisabled = C("#A3FFFFFF"),

                ControlFillDefault = C("#B3FFFFFF"),
                ControlFillSecondary = C("#80F9F9F9"),
                ControlFillTertiary = C("#4DF9F9F9"),
                ControlFillDisabled = C("#4DF9F9F9"),
                ControlFillTransparent = C("#00FFFFFF"),
                ControlFillInputActive = C("#FFFFFF"),
                ControlStrongFillDefault = C("#72000000"),
                ControlStrongFillDisabled = C("#51000000"),
                ControlSolidFillDefault = C("#FFFFFF"),

                SubtleFillTransparent = C("#00FFFFFF"),
                SubtleFillSecondary = C("#09000000"),
                SubtleFillTertiary = C("#06000000"),
                SubtleFillDisabled = C("#00FFFFFF"),

                ControlAltFillTransparent = C("#00FFFFFF"),
                ControlAltFillSecondary = C("#06000000"),
                ControlAltFillTertiary = C("#0F000000"),
                ControlAltFillQuarternary = C("#18000000"),
                ControlAltFillDisabled = C("#00FFFFFF"),

                ControlOnImageFillDefault = C("#C9FFFFFF"),
                ControlOnImageFillSecondary = C("#F3F3F3"),
                ControlOnImageFillTertiary = C("#EBEBEB"),
                ControlOnImageFillDisabled = C("#00FFFFFF"),

                AccentFillDisabled = C("#37000000"),

                ControlStrokeDefault = C("#0F000000"),
                ControlStrokeSecondary = C("#29000000"),
                ControlStrokeTertiary = C("#9E000000"),
                ControlStrokeOnAccentDefault = C("#14FFFFFF"),
                ControlStrokeOnAccentSecondary = C("#66000000"),
                ControlStrokeOnAccentTertiary = C("#37000000"),
                ControlStrokeOnAccentDisabled = C("#0F000000"),
                ControlStrokeForStrongFillWhenOnImage = C("#59FFFFFF"),
                CardStrokeDefault = C("#0F000000"),
                CardStrokeDefaultSolid = C("#EBEBEB"),
                ControlStrongStrokeDefault = C("#72000000"),
                ControlStrongStrokeDisabled = C("#37000000"),
                SurfaceStrokeDefault = C("#66757575"),
                SurfaceStrokeFlyout = C("#0F000000"),
                SurfaceStrokeInverse = C("#15FFFFFF"),
                DividerStrokeDefault = C("#0F000000"),
                FocusStrokeOuter = C("#E4000000"),
                FocusStrokeInner = C("#B3FFFFFF"),

                CardBackgroundFillDefault = C("#B3FFFFFF"),
                CardBackgroundFillSecondary = C("#80F6F6F6"),
                SmokeFillDefault = C("#4D000000"),
                LayerFillDefault = C("#80FFFFFF"),
                LayerFillAlt = C("#FFFFFF"),
                LayerOnAcrylicFillDefault = C("#40FFFFFF"),
                LayerOnAccentAcrylicFillDefault = C("#40FFFFFF"),
                AcrylicBackgroundFillDefault = C("#F9F9F9"),
                LayerOnMicaBaseAltFillDefault = C("#B3FFFFFF"),
                LayerOnMicaBaseAltFillSecondary = C("#0A000000"),
                LayerOnMicaBaseAltFillTertiary = C("#F9F9F9"),
                LayerOnMicaBaseAltFillTransparent = C("#00000000"),

                SolidBackgroundFillBase = C("#F3F3F3"),
                SolidBackgroundFillSecondary = C("#EEEEEE"),
                SolidBackgroundFillTertiary = C("#F9F9F9"),
                SolidBackgroundFillQuarternary = C("#FFFFFF"),
                SolidBackgroundFillBaseAlt = C("#DADADA"),

                SystemFillAttention = C("#0078D4"),
                SystemFillInformational = C("#8A8A8A"),
                SystemFillSuccess = C("#0F7B0F"),
                SystemFillCaution = C("#9D5D00"),
                SystemFillCritical = C("#C42B1C"),
                SystemFillNeutral = C("#72000000"),
                SystemFillSolidNeutral = C("#8A8A8A"),
                SystemFillAttentionBackground = C("#80F6F6F6"),
                SystemFillSuccessBackground = C("#DFF6DD"),
                SystemFillCautionBackground = C("#FFF4CE"),
                SystemFillCriticalBackground = C("#FDE7E9"),
                SystemFillNeutralBackground = C("#06000000"),
                SystemFillSolidAttentionBackground = C("#F7F7F7"),
                SystemFillSolidNeutralBackground = C("#F3F3F3"),
            };
        }

        public static FluentPalette CreateDark()
        {
            return new FluentPalette
            {
                Theme = ApplicationTheme.Dark,
                ApplicationBackground = C("#FF202020"),
                KeyboardFocusBorder = C("#87FFFFFF"),

                TextFillPrimary = C("#FFFFFF"),
                TextFillSecondary = C("#C5FFFFFF"),
                TextFillTertiary = C("#87FFFFFF"),
                TextFillDisabled = C("#5DFFFFFF"),
                TextPlaceholder = C("#87FFFFFF"),
                TextFillInverse = C("#E4000000"),
                AccentTextFillDisabled = C("#5DFFFFFF"),
                TextOnAccentSelectedText = C("#FFFFFF"),
                TextOnAccentPrimary = C("#000000"),
                TextOnAccentSecondary = C("#80000000"),
                TextOnAccentDisabled = C("#77000000"),

                ControlFillDefault = C("#0FFFFFFF"),
                ControlFillSecondary = C("#15FFFFFF"),
                ControlFillTertiary = C("#08FFFFFF"),
                ControlFillDisabled = C("#0BFFFFFF"),
                ControlFillTransparent = C("#00FFFFFF"),
                ControlFillInputActive = C("#B31E1E1E"),
                ControlStrongFillDefault = C("#8BFFFFFF"),
                ControlStrongFillDisabled = C("#3FFFFFFF"),
                ControlSolidFillDefault = C("#454545"),

                SubtleFillTransparent = C("#00FFFFFF"),
                SubtleFillSecondary = C("#0FFFFFFF"),
                SubtleFillTertiary = C("#0AFFFFFF"),
                SubtleFillDisabled = C("#00FFFFFF"),

                ControlAltFillTransparent = C("#00FFFFFF"),
                ControlAltFillSecondary = C("#19000000"),
                ControlAltFillTertiary = C("#0BFFFFFF"),
                ControlAltFillQuarternary = C("#12FFFFFF"),
                ControlAltFillDisabled = C("#00FFFFFF"),

                ControlOnImageFillDefault = C("#B31C1C1C"),
                ControlOnImageFillSecondary = C("#1A1A1A"),
                ControlOnImageFillTertiary = C("#131313"),
                ControlOnImageFillDisabled = C("#1E1E1E"),

                AccentFillDisabled = C("#28FFFFFF"),

                ControlStrokeDefault = C("#12FFFFFF"),
                ControlStrokeSecondary = C("#18FFFFFF"),
                ControlStrokeTertiary = C("#C5FFFFFF"),
                ControlStrokeOnAccentDefault = C("#14FFFFFF"),
                ControlStrokeOnAccentSecondary = C("#23000000"),
                ControlStrokeOnAccentTertiary = C("#37000000"),
                ControlStrokeOnAccentDisabled = C("#33000000"),
                ControlStrokeForStrongFillWhenOnImage = C("#6B000000"),
                CardStrokeDefault = C("#19000000"),
                CardStrokeDefaultSolid = C("#1C1C1C"),
                ControlStrongStrokeDefault = C("#8BFFFFFF"),
                ControlStrongStrokeDisabled = C("#28FFFFFF"),
                SurfaceStrokeDefault = C("#66757575"),
                SurfaceStrokeFlyout = C("#33000000"),
                SurfaceStrokeInverse = C("#0F000000"),
                DividerStrokeDefault = C("#15FFFFFF"),
                FocusStrokeOuter = C("#FFFFFF"),
                FocusStrokeInner = C("#B3000000"),

                CardBackgroundFillDefault = C("#0DFFFFFF"),
                CardBackgroundFillSecondary = C("#08FFFFFF"),
                SmokeFillDefault = C("#4D000000"),
                LayerFillDefault = C("#4C3A3A3A"),
                LayerFillAlt = C("#0DFFFFFF"),
                LayerOnAcrylicFillDefault = C("#09FFFFFF"),
                LayerOnAccentAcrylicFillDefault = C("#09FFFFFF"),
                AcrylicBackgroundFillDefault = C("#2C2C2C"),
                LayerOnMicaBaseAltFillDefault = C("#733A3A3A"),
                LayerOnMicaBaseAltFillSecondary = C("#0FFFFFFF"),
                LayerOnMicaBaseAltFillTertiary = C("#2C2C2C"),
                LayerOnMicaBaseAltFillTransparent = C("#00FFFFFF"),

                SolidBackgroundFillBase = C("#202020"),
                SolidBackgroundFillSecondary = C("#1C1C1C"),
                SolidBackgroundFillTertiary = C("#282828"),
                SolidBackgroundFillQuarternary = C("#2C2C2C"),
                SolidBackgroundFillBaseAlt = C("#0A0A0A"),

                SystemFillAttention = C("#4CC2FF"),
                SystemFillInformational = C("#9D9D9D"),
                SystemFillSuccess = C("#6CCB5F"),
                SystemFillCaution = C("#FCE100"),
                SystemFillCritical = C("#FF99A4"),
                SystemFillNeutral = C("#8BFFFFFF"),
                SystemFillSolidNeutral = C("#9D9D9D"),
                SystemFillAttentionBackground = C("#08FFFFFF"),
                SystemFillSuccessBackground = C("#393D1B"),
                SystemFillCautionBackground = C("#433519"),
                SystemFillCriticalBackground = C("#442726"),
                SystemFillNeutralBackground = C("#08FFFFFF"),
                SystemFillSolidAttentionBackground = C("#2E2E2E"),
                SystemFillSolidNeutralBackground = C("#2E2E2E"),
            };
        }
    }
}