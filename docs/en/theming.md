# Theme System

Themes in WinformUi are driven globally by `WinformUi.Theming.ApplicationThemeManager`:
a WinUI-aligned Fluent palette (`FluentPalette`) plus system accent color derivation rules.
Every Fluent control subscribes to theme changes internally and repaints automatically on a
switch, so no per-control handling is required.

## Basic API

```csharp
// Apply once at startup (pick one of the three)
ApplicationThemeManager.Apply(ApplicationTheme.Light);
ApplicationThemeManager.Apply(ApplicationTheme.Dark);
ApplicationThemeManager.Apply(ApplicationTheme.System);   // Follow the system
ApplicationThemeManager.ApplySystemTheme();               // Same as above

// Custom accent color (by default reads the Windows system accent color)
ApplicationThemeManager.Apply(ApplicationTheme.Dark, Color.FromArgb(0, 120, 212));

// Current state
ApplicationTheme theme = ApplicationThemeManager.CurrentTheme;     // Includes System
ApplicationTheme real  = ApplicationThemeManager.EffectiveTheme;   // Light/Dark resolved from System
FluentPalette palette  = ApplicationThemeManager.Palette;
Color accent           = ApplicationThemeManager.AccentColor;

// Refresh after system settings change (only effective when CurrentTheme == System;
// FluentWindow calls it automatically on WM_SETTINGCHANGE / WM_DWMCOLORIZATIONCOLORCHANGED)
ApplicationThemeManager.RefreshSystemColors();

// Subscribe to changes to refresh self-drawn content
ApplicationThemeManager.ThemeChanged += (s, e) => myControl.Invalidate();
```

`Apply` rebuilds the palette, raises `ThemeChanged`, and calls `Invalidate(true)` on the
entire control tree of `Application.OpenForms` — mixed-in native WinForms controls get their
backgrounds refreshed along with everything else.

## Palette Tokens

Each property of `FluentPalette` corresponds to one Fluent design token. Colors are expressed
as `#AARRGGBB`; tokens with alpha must be blended over the parent background (the library does
this internally via `ControlHelper.BlendOverParent`; when consuming colors yourself you can
compose them with `AccentColor.Blend(top, bottom)`).

Common tokens (light / dark):

| Token | Light | Dark | Purpose |
| --- | --- | --- | --- |
| `ApplicationBackground` | `#FAFAFA` | `#202020` | Solid fallback background for windows |
| `TextFillPrimary` | `#E4000000` | `#FFFFFF` | Body text |
| `TextFillSecondary` | `#9E000000` | `#C5FFFFFF` | Secondary text |
| `TextFillTertiary` | `#72000000` | `#87FFFFFF` | Placeholder / weak text |
| `TextFillDisabled` | `#5C000000` | `#5DFFFFFF` | Disabled text |
| `TextPlaceholder` | `#9E000000` | `#87FFFFFF` | Input field placeholder |
| `TextOnAccentPrimary` | Black/white picked by accent brightness | Same as left | Text on the accent color |
| `TextOnAccentSelectedText` | `#FFFFFF` | `#FFFFFF` | Selected text color |
| `ControlFillDefault` | `#B3FFFFFF` | `#0FFFFFFF` | Default control fill |
| `ControlFillSecondary` | `#80F9F9F9` | `#15FFFFFF` | Hover fill |
| `ControlFillTertiary` | `#4DF9F9F9` | `#08FFFFFF` | Pressed fill |
| `ControlFillDisabled` | `#4DF9F9F9` | `#0BFFFFFF` | Disabled fill |
| `ControlFillInputActive` | `#FFFFFF` | `#B31E1E1E` | Focused input fill |
| `ControlStrongFillDefault` | `#72000000` | `#8BFFFFFF` | Strong fills such as scrollbar thumbs |
| `ControlSolidFillDefault` | `#FFFFFF` | `#454545` | Opaque solid fill |
| `SubtleFillSecondary` | `#09000000` | `#0FFFFFFF` | Weak fills such as list item hover |
| `SubtleFillTertiary` | `#06000000` | `#0AFFFFFF` | List item pressed |
| `ControlStrokeDefault` | `#0F000000` | `#12FFFFFF` | Control border |
| `ControlStrokeSecondary` | `#29000000` | `#18FFFFFF` | Heavier bottom border |
| `ControlStrongStrokeDefault` | `#72000000` | `#8BFFFFFF` | Strong border |
| `CardStrokeDefault` | `#0F000000` | `#19000000` | Card border |
| `CardBackgroundFillDefault` | `#B3FFFFFF` | `#0DFFFFFF` | Card background |
| `CardBackgroundFillSecondary` | `#80F6F6F6` | `#08FFFFFF` | Secondary card / page background |
| `DividerStrokeDefault` | `#0F000000` | `#15FFFFFF` | Divider line |
| `SmokeFillDefault` | `#4D000000` | `#4D000000` | Dialog overlay |
| `LayerFillDefault` | `#80FFFFFF` | `#4C3A3A3A` | Popup layer background |
| `SolidBackgroundFillBase` | `#F3F3F3` | `#202020` | Solid base background |
| `SystemFillAttention` | Accent color | Accent color | Attention / information |
| `SystemFillSuccess` | `#0F7B0F` | `#6CCB5F` | Success |
| `SystemFillCaution` | `#9D5D00` | `#FCE100` | Warning |
| `SystemFillCritical` | `#C42B1C` | `#FF99A4` | Error / danger |
| `FocusStrokeOuter` | `#E4000000` | `#FFFFFF` | Keyboard focus outer ring |
| `FocusStrokeInner` | `#B3FFFFFF` | `#B3000000` | Keyboard focus inner ring |

The complete token list is in the source file `src/WinformUi/Theming/FluentPalette.cs`
(about 90 tokens, including the Acrylic / Mica layers, the OnImage series, etc.).

## Accent Color Derivation

`AccentColor` (in `WinformUi.Theming`) is responsible for:

- `GetSystemAccent()`: reads the registry value `HKCU\Software\Microsoft\Windows\DWM\AccentColor`
  (the DWORD is `0xAABBGGRR`), falling back to `#0078D4` on failure.
- `IsSystemThemeDark()`: reads `AppsUseLightTheme`.
- Derived variants: in the light theme, Dark1/2/3 of the system accent color
  (lightness -0.16/-0.32/-0.48); in the dark theme, Light1/2/3 (+0.18/+0.34/+0.50),
  corresponding to `SystemAccentColorPrimary/Secondary/Tertiary`.
- `AccentFillDefault` = Secondary; `AccentFillSecondary/Tertiary` are further blended over
  `SolidBackgroundFillBase` at 0.9/0.8 opacity.
- `AccentFillSelectedTextBackground` = the raw system accent color (text selection background).
- `TextOnAccentPrimary` automatically picks black or white based on the brightness of
  Secondary, guaranteeing contrast.

To use a custom accent color, just pass it to `Apply(theme, accent)`; all derivations are
recomputed automatically.

## Typography

`FluentTypography` provides static fonts aligned with the WinUI type scale (sizes in pt,
comments in DIP):

| Member | Size | Style |
| --- | --- | --- |
| `Caption` | 9pt (12 DIP) | Regular |
| `Body` | 10.5pt (14 DIP) | Regular |
| `BodyStrong` | 10.5pt | Bold |
| `BodyLarge` | 13.5pt (18 DIP) | Regular |
| `Subtitle` | 15pt (20 DIP) | Bold |
| `Title` | 21pt (28 DIP) | Bold |
| `TitleLarge` | 30pt (40 DIP) | Bold |
| `Display` | 51pt (68 DIP) | Bold |
| `IconRegular(sizePt)` / `IconFilled(sizePt)` | 12pt by default | Embedded icon font |

The font family prefers Segoe UI Variable / Segoe UI; the icon font is loaded from an embedded
resource into a private font collection (`ResolvedFamily` / `ResolvedIconFamily` show the actual
resolution result) and does not pollute system fonts.

## Integrating Custom Controls with the Theme

Two approaches:

```csharp
// Approach 1: derive from FluentControlBase, which brings theme subscription and state visuals
public class MyControl : FluentControlBase
{
    protected override void OnPaint(PaintEventArgs e)
    {
        var p = ApplicationThemeManager.Palette;
        // Paint using p.* colors; repaints automatically on theme switch
    }
}

// Approach 2: any control holds a ThemeSubscription
public class MyPanel : Panel
{
    private ThemeSubscription _sub;
    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        _sub = new ThemeSubscription(this, () => Invalidate());
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing) { _sub?.Dispose(); _sub = null; }
        base.Dispose(disposing);
    }
}
```

Painting helpers live in `WinformUi.Drawing.FluentGraphics`:
`SetupGraphics` (anti-aliasing / text rendering hints), `FillRoundedRect` / `RoundedRect`
(control corner radius `ControlCornerRadius`), `DrawElevationBorder` (the Fluent elevation
border that is lighter on top and darker at the bottom), `DrawText` (GDI text with an alpha
fix that avoids transparent holes under Mica), and `DrawGlyph` (icon glyph rendering).

## Window Background and Theme Integration

`FluentWindow` automatically toggles dark-mode title bars (`DwmApi.SetDarkMode`) and Win11
rounded corners on theme changes, and refreshes the system accent color on `WM_SETTINGCHANGE`.
For background types see [window.md](window.md).
