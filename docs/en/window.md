# FluentWindow Windows

`FluentWindow` (namespace `WinformUi.Controls`) is the Fluent-styled top-level form base class: it keeps the native title bar and system buttons, uses a solid Fluent background by default, and lets the title bar follow the light/dark theme while the window's four corners get rounded; on Windows 11 22H2 and later, the Mica/Acrylic system backdrop effects can be enabled selectively through `BackdropType`. Business forms inherit from it directly to get the complete window-level Fluent experience. The example snippets omit the `using` lines; when using them, import namespaces such as `System`, `System.Drawing`, `System.Windows.Forms`, `WinformUi.Controls` and `WinformUi.Theming`.

## FluentWindow

Fluent top-level window deriving from `Form`: a solid application background by default plus a themed native title bar; system backdrops (Mica/Acrylic/Tabbed) are optional.

### Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| BackdropType | WindowBackdropType | None | Type of system backdrop effect; solid color by default. The effect is enabled when set to Mica/Acrylic/Tabbed and the system supports it; changing it after the handle has been created re-applies it immediately and repaints the whole window. |
| IsBackdropApplied | bool (read-only) | false | Whether the real system backdrop is in effect; false on unsupported systems or when set to None. |
| Palette | FluentPalette (protected, read-only) | — | The current theme palette, i.e. `ApplicationThemeManager.Palette`, for use by subclasses when drawing. |

The `WindowBackdropType` enum (defined in `WinformUi.Theming`):

| Value | Effect | System requirement |
| --- | --- | --- |
| None | No system effect, solid application background | None |
| Mica | Mica: a soft material sampled from the desktop wallpaper | Windows 11 22H2 (build 22621)+ |
| Acrylic | Acrylic: a translucent blurred material | Windows 11 22H2 (build 22621)+ |
| Tabbed | Mica Alt / Tabbed: a Mica variant that also tints the title bar area | Windows 11 22H2 (build 22621)+ |

Constructor presets: `Font = FluentTypography.Body`, `StartPosition = CenterScreen`, `AutoScaleMode = Dpi`, `BackColor = Palette.ApplicationBackground` (once a backdrop effect is enabled, the base class switches to black as the compositing key on its own; see Notes).

### Events and Methods

No new public events or methods are declared by the class itself; all the key behavior happens automatically in the overridden protected members:

- `OnHandleCreated`: creates the theme subscription (`ThemeSubscription`) and applies the backdrop effect and the title bar theme.
- `OnShown`: applies the backdrop effect once more after the window becomes visible (in some situations the DWM attributes only take effect after being set a second time).
- `OnPaintBackground`: clears the screen with black when the backdrop is in effect (DWM composites black on the extended frame as transparent, letting the system material show through); otherwise fills with the solid `Palette.ApplicationBackground`.
- `WndProc`: on `WM_SETTINGCHANGE` (0x001A) or `WM_DWMCOLORIZATIONCOLORCHANGED` (0x0320) it calls `ApplicationThemeManager.RefreshSystemColors()` and re-applies the title bar theme; it tracks minimizing/restoring via `WM_SIZE` and, when restoring from minimized, calls `Invalidate(true)` to force a repaint of the whole window including child controls — windows that had system backdrop properties set have their composited surface discarded by DWM while minimized, and the system does not re-send `WM_PAINT` on restore, so without an explicit repaint a black screen would remain.
- `Dispose`: releases the theme subscription.

### Example

A complete form deriving from `FluentWindow`, together with the matching entry point code:

```csharp
public class MainWindow : FluentWindow
{
    private readonly FluentLabel _statusLabel;

    public MainWindow()
    {
        Text = "Fluent 窗口示例";
        ClientSize = new Size(900, 600);
        MinimumSize = new Size(640, 420);
        BackdropType = WindowBackdropType.Mica;

        // Transparent Fluent menu bar, so the Mica material shows straight through
        var menu = new FluentMenuStrip { Dock = DockStyle.Top };
        var file = new ToolStripMenuItem("文件(&F)");
        file.DropDownItems.Add(new ToolStripMenuItem("退出", null, (s, e) => Close()));
        menu.Items.Add(file);

        var switchButton = new FluentButton
        {
            Text = "切换 Mica / Acrylic",
            Appearance = ControlAppearance.Primary,
            AutoSize = true,
            Location = new Point(24, 56)
        };
        switchButton.Click += (s, e) =>
        {
            BackdropType = BackdropType == WindowBackdropType.Mica
                ? WindowBackdropType.Acrylic
                : WindowBackdropType.Mica;
            UpdateStatus();
        };

        _statusLabel = new FluentLabel
        {
            AutoSize = true,
            Location = new Point(24, 110)
        };

        Controls.Add(switchButton);
        Controls.Add(_statusLabel);
        Controls.Add(menu);
        MainMenuStrip = menu;

        ApplicationThemeManager.ThemeChanged += (s, e) => UpdateStatus();
    }

    private void UpdateStatus()
    {
        string backdrop = IsBackdropApplied
            ? BackdropType.ToString()
            : "纯色回退(系统背景不可用)";
        _statusLabel.Text = string.Format("主题:{0}   背景:{1}",
            ApplicationThemeManager.EffectiveTheme, backdrop);
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);   // The base class applies the backdrop effect a second time here
        UpdateStatus();
    }
}

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        ApplicationThemeManager.Apply(ApplicationTheme.System);   // Follow the Windows light/dark setting
        Application.Run(new MainWindow());
    }
}
```

### Notes

- **Solid color by default, Mica optional**: WinForms' GDI rendering leaves the alpha of pixels such as text at 0, and the full-client-area glass compositing channel overlays an additional offset copy of the content on top of those transparent pixels, which shows up as ghosted/doubled text on some Windows 11 versions; the text of native controls (ComboBox, for instance) is equally affected. That is why, since 1.2.1, the default is `BackdropType = None`, and the material effect has to be enabled explicitly.
- **Fallback on older systems**: internally the DWM system backdrop attributes are attempted through `DwmApi.TrySetBackdrop`. It returns false when the system is below Windows 11 22H2 (build 22621) or when the type is None; in that case `IsBackdropApplied` is false and the form falls back to the solid `Palette.ApplicationBackground` background, with no other impact on functionality or appearance. You can use `IsBackdropApplied` to inform the user in the UI (as in the example's status text).
- **Dark title bar and rounded corners**: on every handle creation and theme change, the DWM immersive dark title bar is set according to whether `ApplicationThemeManager.EffectiveTheme` is Dark; on Windows 11 (build 22000)+ the corner preference is also set (DWMWCP_ROUND). Note that the rounded-corner threshold (22000) is lower than the backdrop threshold (22621), so on Win11 21H2 you will see the combination "rounded corners + solid color fallback".
- **Automatic refresh**: when the user switches the light/dark mode or the accent color in the Windows settings, the system broadcasts `WM_SETTINGCHANGE` / `WM_DWMCOLORIZATIONCOLORCHANGED`; on receiving them the form calls `RefreshSystemColors()` — the palette is rebuilt and `ThemeChanged` broadcast only when `CurrentTheme == ApplicationTheme.System`, after which the title bar and the entire control tree repaint automatically. If the application explicitly locked Light/Dark, system switches do not affect it.
- When the backdrop is in effect, do not change `BackColor` to an opaque light color: the base class relies on the DWM compositing rule "black = transparent" to let the material show through. If you want a solid appearance, set `BackdropType = WindowBackdropType.None` directly.
