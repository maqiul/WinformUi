# 主题系统

WinformUi 的主题由 `WinformUi.Theming.ApplicationThemeManager` 全局驱动:
一份与 WinUI 对齐的 Fluent 调色板(`FluentPalette`)+ 系统强调色派生规则。
所有 Fluent 控件内部订阅主题变化,切换时自动重绘,无需逐个控件处理。

## 基本 API

```csharp
// 启动时应用一次(三选一)
ApplicationThemeManager.Apply(ApplicationTheme.Light);
ApplicationThemeManager.Apply(ApplicationTheme.Dark);
ApplicationThemeManager.Apply(ApplicationTheme.System);   // 跟随系统
ApplicationThemeManager.ApplySystemTheme();               // 同上

// 自定义强调色(默认读取 Windows 系统强调色)
ApplicationThemeManager.Apply(ApplicationTheme.Dark, Color.FromArgb(0, 120, 212));

// 当前状态
ApplicationTheme theme = ApplicationThemeManager.CurrentTheme;     // 含 System
ApplicationTheme real  = ApplicationThemeManager.EffectiveTheme;   // System 解析后的 Light/Dark
FluentPalette palette  = ApplicationThemeManager.Palette;
Color accent           = ApplicationThemeManager.AccentColor;

// 系统设置变化后刷新(仅 CurrentTheme == System 时生效;
// FluentWindow 收到 WM_SETTINGCHANGE / WM_DWMCOLORIZATIONCOLORCHANGED 会自动调用)
ApplicationThemeManager.RefreshSystemColors();

// 订阅变化,刷新自绘内容
ApplicationThemeManager.ThemeChanged += (s, e) => myControl.Invalidate();
```

`Apply` 会重建调色板、触发 `ThemeChanged`,并对 `Application.OpenForms`
整棵控件树 `Invalidate(true)`——原生 WinForms 控件混用时也能跟着刷新背景。

## 调色板令牌

`FluentPalette` 的每个属性对应一个 Fluent 设计令牌。颜色用 `#AARRGGBB`
表示,带透明度的令牌需要叠加在父背景上(库内部用 `ControlHelper.BlendOverParent`
自动完成;自己取用时可调用 `AccentColor.Blend(top, bottom)` 合成)。

常用令牌(浅色 / 深色):

| 令牌 | Light | Dark | 用途 |
| --- | --- | --- | --- |
| `ApplicationBackground` | `#FAFAFA` | `#202020` | 窗口纯色回退背景 |
| `TextFillPrimary` | `#E4000000` | `#FFFFFF` | 正文文本 |
| `TextFillSecondary` | `#9E000000` | `#C5FFFFFF` | 次级文本 |
| `TextFillTertiary` | `#72000000` | `#87FFFFFF` | 占位 / 弱文本 |
| `TextFillDisabled` | `#5C000000` | `#5DFFFFFF` | 禁用文本 |
| `TextPlaceholder` | `#9E000000` | `#87FFFFFF` | 输入框占位符 |
| `TextOnAccentPrimary` | 按强调色亮度选黑/白 | 同左 | 强调色上的文本 |
| `TextOnAccentSelectedText` | `#FFFFFF` | `#FFFFFF` | 选中文本颜色 |
| `ControlFillDefault` | `#B3FFFFFF` | `#0FFFFFFF` | 控件默认填充 |
| `ControlFillSecondary` | `#80F9F9F9` | `#15FFFFFF` | hover 填充 |
| `ControlFillTertiary` | `#4DF9F9F9` | `#08FFFFFF` | pressed 填充 |
| `ControlFillDisabled` | `#4DF9F9F9` | `#0BFFFFFF` | 禁用填充 |
| `ControlFillInputActive` | `#FFFFFF` | `#B31E1E1E` | 输入框聚焦填充 |
| `ControlStrongFillDefault` | `#72000000` | `#8BFFFFFF` | 滚动条滑块等强填充 |
| `ControlSolidFillDefault` | `#FFFFFF` | `#454545` | 不透明实心填充 |
| `SubtleFillSecondary` | `#09000000` | `#0FFFFFFF` | 列表项 hover 等弱填充 |
| `SubtleFillTertiary` | `#06000000` | `#0AFFFFFF` | 列表项 pressed |
| `ControlStrokeDefault` | `#0F000000` | `#12FFFFFF` | 控件描边 |
| `ControlStrokeSecondary` | `#29000000` | `#18FFFFFF` | 底部加重描边 |
| `ControlStrongStrokeDefault` | `#72000000` | `#8BFFFFFF` | 强描边 |
| `CardStrokeDefault` | `#0F000000` | `#19000000` | 卡片描边 |
| `CardBackgroundFillDefault` | `#B3FFFFFF` | `#0DFFFFFF` | 卡片背景 |
| `CardBackgroundFillSecondary` | `#80F6F6F6` | `#08FFFFFF` | 次级卡片 / 页背景 |
| `DividerStrokeDefault` | `#0F000000` | `#15FFFFFF` | 分隔线 |
| `SmokeFillDefault` | `#4D000000` | `#4D000000` | 对话框遮罩 |
| `LayerFillDefault` | `#80FFFFFF` | `#4C3A3A3A` | 弹出层背景 |
| `SolidBackgroundFillBase` | `#F3F3F3` | `#202020` | 实心基背景 |
| `SystemFillAttention` | 强调色 | 强调色 | 关注 / 信息提示 |
| `SystemFillSuccess` | `#0F7B0F` | `#6CCB5F` | 成功 |
| `SystemFillCaution` | `#9D5D00` | `#FCE100` | 警告 |
| `SystemFillCritical` | `#C42B1C` | `#FF99A4` | 错误 / 危险 |
| `FocusStrokeOuter` | `#E4000000` | `#FFFFFF` | 键盘焦点外圈 |
| `FocusStrokeInner` | `#B3FFFFFF` | `#B3000000` | 键盘焦点内圈 |

完整令牌见源码 `src/WinformUi/Theming/FluentPalette.cs`(约 90 个,
含 Acrylic / Mica 层、OnImage 系列等)。

## 强调色派生

`AccentColor`( `WinformUi.Theming` )负责:

- `GetSystemAccent()`:读注册表 `HKCU\Software\Microsoft\Windows\DWM\AccentColor`
  (DWORD 为 `0xAABBGGRR`),失败回退 `#0078D4`。
- `IsSystemThemeDark()`:读 `AppsUseLightTheme`。
- 派生变体:浅色主题对系统强调色做 Dark1/2/3(明度 -0.16/-0.32/-0.48),
  深色主题做 Light1/2/3(+0.18/+0.34/+0.50),对应
  `SystemAccentColorPrimary/Secondary/Tertiary`。
- `AccentFillDefault` = Secondary;`AccentFillSecondary/Tertiary` 再按
  0.9/0.8 不透明度叠在 `SolidBackgroundFillBase` 上。
- `AccentFillSelectedTextBackground` = 系统强调色原色(文本选区背景)。
- `TextOnAccentPrimary` 按 Secondary 亮度自动选黑或白,保证对比度。

自定义强调色时把颜色传给 `Apply(theme, accent)` 即可,全部派生自动重算。

## 字体排印

`FluentTypography` 提供与 WinUI 字号对齐的静态字体(单位 pt,注释为 DIP):

| 成员 | 大小 | 样式 |
| --- | --- | --- |
| `Caption` | 9pt(12 DIP) | Regular |
| `Body` | 10.5pt(14 DIP) | Regular |
| `BodyStrong` | 10.5pt | Bold |
| `BodyLarge` | 13.5pt(18 DIP) | Regular |
| `Subtitle` | 15pt(20 DIP) | Bold |
| `Title` | 21pt(28 DIP) | Bold |
| `TitleLarge` | 30pt(40 DIP) | Bold |
| `Display` | 51pt(68 DIP) | Bold |
| `IconRegular(sizePt)` / `IconFilled(sizePt)` | 默认 12pt | 内嵌图标字体 |

字体族优先 Segoe UI Variable / Segoe UI,图标字体从内嵌资源加载到私有字体集合
(`ResolvedFamily` / `ResolvedIconFamily` 可查实际解析结果),不污染系统字体。

## 自定义控件接入主题

两种姿势:

```csharp
// 姿势一:继承 FluentControlBase,自带主题订阅与状态视觉
public class MyControl : FluentControlBase
{
    protected override void OnPaint(PaintEventArgs e)
    {
        var p = ApplicationThemeManager.Palette;
        // 用 p.* 取色绘制,主题切换自动重绘
    }
}

// 姿势二:任意控件持有 ThemeSubscription
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

绘制辅助在 `WinformUi.Drawing.FluentGraphics`:
`SetupGraphics`(抗锯齿/文本渲染提示)、`FillRoundedRect` / `RoundedRect`
(控件圆角 `ControlCornerRadius`)、`DrawElevationBorder`(上浅下深的 Fluent
 elevation 描边)、`DrawText`(GDI 文本 + alpha 修复,Mica 下不留透明洞)、
`DrawGlyph`(图标符号绘制)。

## 窗口背景与主题联动

`FluentWindow` 在主题变化时自动切换标题栏深色模式(`DwmApi.SetDarkMode`)、
Win11 圆角,并在 `WM_SETTINGCHANGE` 时刷新系统强调色。背景类型见
[window.md](window.md)。