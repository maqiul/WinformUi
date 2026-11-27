# FluentWindow 窗口

`FluentWindow`(命名空间 `WinformUi.Controls`)是 Fluent 风格的顶层窗体基类:保留原生标题栏与系统按钮,默认使用纯色 Fluent 背景,并让标题栏跟随深浅主题、窗口四角圆角;在 Windows 11 22H2+ 上可通过 `BackdropType` 选择性开启 Mica/Acrylic 系统背景效果。业务窗体直接继承它即可获得完整的窗口级 Fluent 体验。示例代码省略 `using` 行,使用时请引入 `System`、`System.Drawing`、`System.Windows.Forms`、`WinformUi.Controls`、`WinformUi.Theming` 等命名空间。

## FluentWindow

继承自 `Form` 的 Fluent 顶层窗口:默认纯色应用背景 + 主题化原生标题栏;系统背景(Mica/Acrylic/Tabbed)为可选项。

### 属性

| 属性 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| BackdropType | WindowBackdropType | None | 系统背景效果类型;默认纯色。设为 Mica/Acrylic/Tabbed 且系统支持时启用;句柄已创建时修改会立即重新应用并整体重绘。 |
| IsBackdropApplied | bool(只读) | false | 真实的系统背景是否已生效;不支持的系统或设为 None 时为 false。 |
| Palette | FluentPalette(protected,只读) | — | 当前主题调色板,即 `ApplicationThemeManager.Palette`,供子类绘制时使用。 |

`WindowBackdropType` 枚举(定义于 `WinformUi.Theming`):

| 值 | 效果 | 系统要求 |
| --- | --- | --- |
| None | 无系统效果,纯色应用背景 | 无 |
| Mica | 云母:采样桌面壁纸的柔和材质 | Windows 11 22H2(build 22621)+ |
| Acrylic | 亚克力:半透明模糊材质 | Windows 11 22H2(build 22621)+ |
| Tabbed | Mica Alt / Tabbed:标题栏区域也着色的云母变体 | Windows 11 22H2(build 22621)+ |

构造函数预设:`Font = FluentTypography.Body`、`StartPosition = CenterScreen`、`AutoScaleMode = Dpi`、`BackColor = Palette.ApplicationBackground`(开启背景效果后基类会自行切换为黑色合成键,见备注)。

### 事件/方法

无自己声明的新公开事件或方法;关键行为都在重写的受保护成员中自动完成:

- `OnHandleCreated`:创建主题订阅(`ThemeSubscription`),应用背景效果与标题栏主题。
- `OnShown`:窗口可见后再应用一次背景效果(某些情况下 DWM 属性需要二次设置才生效)。
- `OnPaintBackground`:背景已生效时用黑色清屏(DWM 把扩展帧上的黑色合成为透明,透出系统材质);未生效时用 `Palette.ApplicationBackground` 纯色填充。
- `WndProc`:收到 `WM_SETTINGCHANGE`(0x001A)或 `WM_DWMCOLORIZATIONCOLORCHANGED`(0x0320)时调用 `ApplicationThemeManager.RefreshSystemColors()` 并重新应用标题栏主题;跟踪 `WM_SIZE` 的最小化/恢复,从最小化恢复时 `Invalidate(true)` 强制整窗(含子控件)重绘——设置过系统背景属性的窗口在最小化期间会被 DWM 丢弃合成表面,而恢复时系统不自动补发 `WM_PAINT`,不主动重绘会留下黑屏。
- `Dispose`:释放主题订阅。

### 示例

继承 `FluentWindow` 的完整窗体,以及配套的入口代码:

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

        // 透明 Fluent 菜单栏,直接透出 Mica 材质
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
        base.OnShown(e);   // 基类会在此二次应用背景效果
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
        ApplicationThemeManager.Apply(ApplicationTheme.System);   // 跟随 Windows 深浅色
        Application.Run(new MainWindow());
    }
}
```

### 备注

- **默认纯色、Mica 可选**:WinForms 的 GDI 渲染会把文本等像素的 alpha 留为 0,而全客户区 glass 合成通道会对这些透明像素再叠加一次带偏移的内容副本,在部分 Windows 11 版本上表现为文字重影;原生控件(如 ComboBox)的文本同样无法避免。因此 1.2.1 起默认 `BackdropType = None`,需要材质效果时再显式开启。
- **旧系统回退**:内部通过 `DwmApi.TrySetBackdrop` 尝试设置 DWM 系统背景属性,系统低于 Windows 11 22H2(build 22621)或类型为 None 时返回 false,此时 `IsBackdropApplied` 为 false,窗体自动改用 `Palette.ApplicationBackground` 纯色背景,功能与外观其余部分不受影响。可用 `IsBackdropApplied` 在 UI 上提示用户(如示例的状态文本)。
- **深色标题栏与圆角**:每次句柄创建和主题变化时,按 `ApplicationThemeManager.EffectiveTheme` 是否为 Dark 设置 DWM 沉浸式深色标题栏;Windows 11(build 22000)+ 上还会设置圆角偏好(DWMWCP_ROUND)。注意圆角门槛(22000)低于背景效果门槛(22621),Win11 21H2 上会出现"圆角 + 纯色回退"的组合。
- **自动刷新**:用户在 Windows 设置里切换深浅色或强调色时,系统广播 `WM_SETTINGCHANGE` / `WM_DWMCOLORIZATIONCOLORCHANGED`,窗体收到后调用 `RefreshSystemColors()`——仅当 `CurrentTheme == ApplicationTheme.System` 时重建调色板并广播 `ThemeChanged`,随后标题栏与整棵控件树自动重绘;若应用显式锁定了 Light/Dark,则系统切换不会影响它。
- 背景生效时不要把 `BackColor` 改成不透明浅色:基类依赖"黑色 = 透明"的 DWM 合成规则透出材质;需要纯色外观请直接设 `BackdropType = WindowBackdropType.None`。