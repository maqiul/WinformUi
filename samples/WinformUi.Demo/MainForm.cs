using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using WinformUi.Controls;
using WinformUi.Theming;

namespace WinformUi.Demo
{
    /// <summary>Surface-colored flow panel so transparent children blend against the page fill.</summary>
    internal class SurfaceFlow : FlowLayoutPanel
    {
        private ThemeSubscription _themeSubscription;

        public SurfaceFlow()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            ControlHelper.PaintFullBackground(this, e.Graphics);
            base.OnPaint(e);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (_themeSubscription == null)
            {
                _themeSubscription = new ThemeSubscription(this, () => Invalidate());
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_themeSubscription != null)
                {
                    _themeSubscription.Dispose();
                    _themeSubscription = null;
                }
            }

            base.Dispose(disposing);
        }
    }

    /// <summary>Scrollable page with a title, description and stacked section cards.</summary>
    internal class DemoPage : FluentPage
    {
        protected readonly SurfaceFlow Root;
        private readonly FluentScrollViewer _scroll;
        private readonly ThemeSubscription _rootThemeSync;

        public DemoPage(string title, string description)
        {
            Padding = new Padding(0);

            _scroll = new FluentScrollViewer { Dock = DockStyle.Fill };
            Root = new SurfaceFlow
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                BackColor = SurfaceColor,
                Padding = new Padding(28, 16, 28, 28)
            };
            _rootThemeSync = new ThemeSubscription(this, () => Root.BackColor = SurfaceColor);
            _scroll.Content = Root;
            Controls.Add(_scroll);

            Root.Controls.Add(new FluentLabel
            {
                Text = title,
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                AutoSize = true,
                Margin = new Padding(0, 4, 0, 4)
            });
            Root.Controls.Add(new FluentLabel
            {
                Text = description,
                AutoSize = true,
                MaximumSize = new Size(820, 0),
                Margin = new Padding(0, 0, 0, 16)
            });
        }

        protected SectionCard AddSection(string header, string subtitle)
        {
            var card = new SectionCard(header, subtitle) { Margin = new Padding(0, 0, 0, 14) };
            Root.Controls.Add(card);
            return card;
        }

        internal static FlowLayoutPanel NewRow()
        {
            return new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                AutoSize = true,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 2, 0, 2)
            };
        }

        protected static FluentLabel Note(string text)
        {
            return new FluentLabel { Text = text, AutoSize = true, Margin = new Padding(6, 10, 0, 0), ForeColor = Color.Gray };
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _rootThemeSync != null)
            {
                _rootThemeSync.Dispose();
            }

            base.Dispose(disposing);
        }
    }

    /// <summary>Rounded card with a bold header, optional subtitle and a vertical body flow.</summary>
    internal sealed class SectionCard : FluentCard
    {
        private readonly SurfaceFlow _body;

        public FlowLayoutPanel Body => _body;

        public SectionCard(string header, string subtitle)
        {
            Size = new Size(880, 120);

            Controls.Add(new FluentLabel
            {
                Text = header,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(20, 14)
            });

            int bodyTop = 48;
            if (!string.IsNullOrEmpty(subtitle))
            {
                Controls.Add(new FluentLabel
                {
                    Text = subtitle,
                    AutoSize = true,
                    ForeColor = Color.Gray,
                    Location = new Point(20, 40)
                });
                bodyTop = 68;
            }

            _body = new SurfaceFlow
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                BackColor = Color.Transparent,
                Location = new Point(20, bodyTop),
                MaximumSize = new Size(840, 0)
            };
            _body.Layout += (s, e) => UpdateHeight(bodyTop);
            Controls.Add(_body);
            UpdateHeight(bodyTop);
        }

        public FlowLayoutPanel AddRow()
        {
            var row = DemoPage.NewRow();
            _body.Controls.Add(row);
            return row;
        }

        private void UpdateHeight(int bodyTop)
        {
            int h = bodyTop + _body.PreferredSize.Height + 18;
            if (Height != h)
            {
                Height = h;
            }
        }
    }

    internal static class DemoAssets
    {
        public static readonly string[] Cities = { "北京", "上海", "广州", "深圳", "杭州", "成都" };

        public static List<City> CityData()
        {
            var list = new List<City>();
            var codes = new[] { "BJ", "SH", "GZ", "SZ", "HZ", "CD" };
            for (int i = 0; i < Cities.Length; i++)
            {
                list.Add(new City { Name = Cities[i], Code = codes[i] });
            }

            return list;
        }

        public static Bitmap MakeSwatch(Color color)
        {
            var bmp = new Bitmap(240, 144);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                Color light = FromHsv(color.GetHue(), color.GetSaturation() * 0.55f, Math.Min(1f, color.GetBrightness() + 0.3f));
                Color dark = FromHsv(color.GetHue(), Math.Min(1f, color.GetSaturation() + 0.15f), Math.Max(0.15f, color.GetBrightness() - 0.25f));
                using (var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                    new Rectangle(0, 0, bmp.Width, bmp.Height), light, dark, System.Drawing.Drawing2D.LinearGradientMode.ForwardDiagonal))
                {
                    g.FillRectangle(brush, 0, 0, bmp.Width, bmp.Height);
                }

                using (var brush = new SolidBrush(Color.FromArgb(70, 255, 255, 255)))
                {
                    g.FillEllipse(brush, 150, -40, 130, 130);
                }

                using (var brush = new SolidBrush(Color.FromArgb(50, 255, 255, 255)))
                {
                    g.FillEllipse(brush, -30, 90, 110, 110);
                }
            }

            return bmp;
        }

        private static Color FromHsv(float hue, float saturation, float value)
        {
            float c = value * saturation;
            float x = c * (1 - Math.Abs((hue / 60f) % 2 - 1));
            float m = value - c;
            float r, g, b;
            if (hue < 60) { r = c; g = x; b = 0; }
            else if (hue < 120) { r = x; g = c; b = 0; }
            else if (hue < 180) { r = 0; g = c; b = x; }
            else if (hue < 240) { r = 0; g = x; b = c; }
            else if (hue < 300) { r = x; g = 0; b = c; }
            else { r = c; g = 0; b = x; }
            return Color.FromArgb(255,
                (int)Math.Round((r + m) * 255),
                (int)Math.Round((g + m) * 255),
                (int)Math.Round((b + m) * 255));
        }

        public static void ShowLoading(Form owner)
        {
            var screen = FluentLoadingScreen.Show(owner, "正在处理,请稍候…");
            var timer = new Timer { Interval = 2000 };
            timer.Tick += (ts, te) =>
            {
                timer.Stop();
                timer.Dispose();
                screen.HideScreen();
            };
            timer.Start();
        }
    }

    public partial class MainForm : FluentWindow
    {
        private FluentNotifyIcon _tray;
        private ToolStripMenuItem _menuDark;
        private ToolStripStatusLabel _statusPage;
        private readonly List<NavigationViewItem> _navItems = new List<NavigationViewItem>();

        public MainForm()
        {
            InitializeComponent();

            bindingSource1.DataSource = DemoAssets.CityData();

            AddNavPageItem("基础控件", SymbolRegular.Apps24, typeof(BasicPage));
            AddNavPageItem("输入控件", SymbolRegular.ClipboardTextLtr24, typeof(InputPage));
            AddNavPageItem("列表与数据", SymbolRegular.DataUsageToolbox24, typeof(ListPage));
            AddNavPageItem("导航与容器", SymbolRegular.WindowApps24, typeof(ContainersPage));
            AddNavPageItem("日期与选择", SymbolRegular.CalendarDay16, typeof(PickerPage));
            AddNavPageItem("反馈与通知", SymbolRegular.Alert24, typeof(FeedbackPage));
            AddNavPageItem("对话框与菜单", SymbolRegular.Document24, typeof(DialogPage));
            AddNavPageItem("代码与滚动", SymbolRegular.Code24, typeof(CodePage));
            fluentNavigationView1.FooterMenuItems.Add(new NavigationViewItem("设置", SymbolRegular.Settings24, typeof(SettingsPage)));
            fluentNavigationView1.SelectionChanged += (s, e) =>
            {
                if (_statusPage != null && fluentNavigationView1.SelectedItem != null)
                {
                    _statusPage.Text = fluentNavigationView1.SelectedItem.Content;
                }
            };
            fluentNavigationView1.SelectItem(_navItems[0], true);

            BuildMenu();
            AddStatusStrip();
        }

        private void AddNavPageItem(string content, SymbolRegular icon, Type pageType)
        {
            var item = new NavigationViewItem(content, icon, pageType);
            fluentNavigationView1.MenuItems.Add(item);
            _navItems.Add(item);
        }

        /// <summary>Navigates to a category page by index (used by the screenshot harness).</summary>
        public void SelectTab(int index)
        {
            if (index >= 0 && index < _navItems.Count)
            {
                fluentNavigationView1.SelectItem(_navItems[index], true);
            }
        }

        internal void SetDark(bool dark, object source)
        {
            ApplicationThemeManager.Apply(dark ? ApplicationTheme.Dark : ApplicationTheme.Light);
            if (!ReferenceEquals(source, _menuDark) && _menuDark != null)
            {
                _menuDark.Checked = dark;
            }
        }

        internal void RegisterTray()
        {
            if (_tray == null)
            {
                _tray = new FluentNotifyIcon
                {
                    TooltipText = "WinformUi Demo 托盘图标",
                    Icon = SystemIcons.Application
                };
                _tray.LeftClick += (sender, args) => FluentToast.Show("托盘", "收到左键单击。", InfoBarSeverity.Informational);
                if (components != null)
                {
                    components.Add(_tray);
                }
            }

            _tray.Register();
            _tray.ShowBalloonTip(TimeSpan.FromSeconds(3), "WinformUi", "托盘图标已注册,试试点击它。");
        }

        private void BuildMenu()
        {
            var view = new ToolStripMenuItem("视图(&V)");
            for (int i = 0; i < _navItems.Count; i++)
            {
                int index = i;
                var item = new ToolStripMenuItem(_navItems[i].Content);
                item.Click += (s, e) => SelectTab(index);
                view.DropDownItems.Add(item);
            }

            view.DropDownItems.Add(new ToolStripSeparator());
            _menuDark = new ToolStripMenuItem("深色模式")
            {
                CheckOnClick = true,
                Checked = ApplicationThemeManager.EffectiveTheme == ApplicationTheme.Dark,
                ShortcutKeys = Keys.Control | Keys.D
            };
            _menuDark.CheckedChanged += (s, e) => SetDark(_menuDark.Checked, _menuDark);
            view.DropDownItems.Add(_menuDark);
            fluentMenuStrip1.Items.Add(view);

            var tools = new ToolStripMenuItem("工具(&T)");
            var chrome = new ToolStripMenuItem("标题栏演示窗");
            chrome.Click += (s, e) => new ChromeDemoForm().Show();
            tools.DropDownItems.Add(chrome);
            var tray = new ToolStripMenuItem("注册托盘图标");
            tray.Click += (s, e) => RegisterTray();
            tools.DropDownItems.Add(tray);
            var loading = new ToolStripMenuItem("加载遮罩(2 秒)");
            loading.Click += (s, e) => DemoAssets.ShowLoading(this);
            tools.DropDownItems.Add(loading);
            fluentMenuStrip1.Items.Add(tools);

            var help = new ToolStripMenuItem("帮助(&H)");
            var about = new ToolStripMenuItem("关于 WinformUi");
            about.Click += (s, e) => FluentMessageBox.Show(
                this,
                "WinformUi 1.4.0\r\n.NET Framework 4.6 WinForms Fluent Design 控件库演示。\r\n左侧导航菜单按控件种类分页。",
                "关于",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            help.DropDownItems.Add(about);
            fluentMenuStrip1.Items.Add(help);
        }

        private void AddStatusStrip()
        {
            var statusStrip = new FluentStatusStrip();
            _statusPage = new ToolStripStatusLabel("基础控件");
            statusStrip.Items.Add(_statusPage);
            statusStrip.Items.Add(new ToolStripStatusLabel("WinformUi 1.4.0 · FluentNavigationView 分页演示")
            {
                Spring = true,
                TextAlign = ContentAlignment.MiddleRight
            });
            statusStrip.Items.Add(new ToolStripProgressBar { Value = 68, Size = new Size(140, 16) });
            Controls.Add(statusStrip);
        }
    }

    internal sealed class BasicPage : DemoPage
    {
        private FluentToolTip _tip;

        public BasicPage()
            : base("基础控件", "按钮家族、标签、分隔线、图标、圆角图片与卡片。顶部菜单「视图」与左侧导航同步分页,深色模式 Ctrl+D 全局切换。")
        {
            var s1 = AddSection("FluentButton 外观", null);
            var row1 = s1.AddRow();
            var appearances = new[]
            {
                new { Text = "主要", Appearance = ControlAppearance.Primary },
                new { Text = "次要", Appearance = ControlAppearance.Secondary },
                new { Text = "信息", Appearance = ControlAppearance.Info },
                new { Text = "成功", Appearance = ControlAppearance.Success },
                new { Text = "警告", Appearance = ControlAppearance.Caution },
                new { Text = "危险", Appearance = ControlAppearance.Danger }
            };
            foreach (var def in appearances)
            {
                var btn = new FluentButton { Text = def.Text, Appearance = def.Appearance, Size = new Size(88, 32), Margin = new Padding(0, 0, 10, 0) };
                string captured = def.Text;
                btn.Click += (s, e) => FluentToast.Show("按钮", "点击了「" + captured + "」外观按钮。", InfoBarSeverity.Informational);
                row1.Controls.Add(btn);
            }

            row1.Controls.Add(new FluentButton { Text = "禁用", Enabled = false, Size = new Size(88, 32) });

            var s2 = AddSection("按钮家族", "图标按钮 / 按下切换 / 超链接 / 下拉 / 拆分");
            var row2 = s2.AddRow();
            row2.Controls.Add(new FluentButton { Text = "带图标", Icon = SymbolRegular.Play24, Appearance = ControlAppearance.Primary, Size = new Size(110, 32), Margin = new Padding(0, 0, 10, 0) });
            var toggleState = new FluentLabel { Text = "未按下", AutoSize = true, Margin = new Padding(6, 8, 10, 0) };
            var toggle = new FluentToggleButton { Text = "按下切换", Size = new Size(110, 32), Margin = new Padding(0, 0, 10, 0) };
            toggle.CheckedChanged += (s, e) => toggleState.Text = toggle.Checked ? "已按下" : "未按下";
            row2.Controls.Add(toggle);
            row2.Controls.Add(toggleState);
            var link = new FluentHyperlinkButton { Text = "超链接", Size = new Size(90, 32), Margin = new Padding(0, 0, 10, 0) };
            link.Click += (s, e) => FluentToast.Show("超链接", "FluentHyperlinkButton 点击事件。", InfoBarSeverity.Success);
            row2.Controls.Add(link);
            var flyout = new FluentContextMenuStrip();
            flyout.Items.Add("操作一");
            flyout.Items.Add("操作二");
            flyout.Items.Add(new ToolStripSeparator());
            flyout.Items.Add("退出");
            row2.Controls.Add(new FluentDropDownButton { Text = "下拉按钮", Flyout = flyout, Size = new Size(120, 32), Margin = new Padding(0, 0, 10, 0) });
            row2.Controls.Add(new FluentSplitButton { Text = "拆分按钮", Flyout = flyout, Size = new Size(120, 32) });

            var s3 = AddSection("标签 / 提示 / 分隔线", "FluentToolTip · FluentLabel · FluentSeparator");
            var row3 = s3.AddRow();
            var tipTarget = new FluentButton { Text = "悬停查看提示", Size = new Size(130, 32), Margin = new Padding(0, 0, 20, 0) };
            _tip = new FluentToolTip();
            _tip.SetToolTip(tipTarget, "FluentToolTip:弹出层跟随深浅主题。");
            row3.Controls.Add(tipTarget);
            row3.Controls.Add(new FluentLabel { Text = "正文标签 Body", AutoSize = true, Margin = new Padding(0, 8, 16, 0) });
            row3.Controls.Add(new FluentLabel { Text = "禁用标签", Enabled = false, AutoSize = true, Margin = new Padding(0, 8, 16, 0) });
            row3.Controls.Add(new FluentLabel { Text = "大标题", AutoSize = true, Font = new Font("Segoe UI", 16F, FontStyle.Bold) });
            var row3b = s3.AddRow();
            row3b.Controls.Add(new FluentSeparator { Size = new Size(760, 2) });

            var s4 = AddSection("FluentIcon 图标", "符号 / 填充 / 字形 / 图片四种来源");
            var row4 = s4.AddRow();
            row4.Controls.Add(new FluentIcon { Symbol = SymbolRegular.Home24, Size = new Size(20, 20), Margin = new Padding(0, 6, 14, 0) });
            row4.Controls.Add(new FluentIcon { Symbol = SymbolRegular.Play24, Size = new Size(24, 24), Margin = new Padding(0, 2, 14, 0) });
            row4.Controls.Add(new FluentIcon { Symbol = SymbolRegular.Code24, Size = new Size(28, 28), Margin = new Padding(0, 0, 14, 0) });
            row4.Controls.Add(new FluentIcon { FilledSymbol = SymbolFilled.Alert24, Size = new Size(32, 32), Margin = new Padding(0, 0, 14, 0) });
            row4.Controls.Add(new FluentIcon { Glyph = "", Size = new Size(32, 32), ForeColor = Color.Goldenrod, Margin = new Padding(0, 0, 14, 0) });
            row4.Controls.Add(new FluentIcon { Image = SystemIcons.Application.ToBitmap(), Size = new Size(32, 32) });

            var s5 = AddSection("FluentPictureBox 圆角图片", "CornerRadius 12 / 30 / 0");
            var row5 = s5.AddRow();
            row5.Controls.Add(new FluentPictureBox { Image = DemoAssets.MakeSwatch(Color.SteelBlue), CornerRadius = 12, Size = new Size(110, 66), Margin = new Padding(0, 0, 12, 0) });
            row5.Controls.Add(new FluentPictureBox { Image = DemoAssets.MakeSwatch(Color.DarkOrange), CornerRadius = 33, Size = new Size(66, 66), Margin = new Padding(0, 0, 12, 0) });
            row5.Controls.Add(new FluentPictureBox { Image = DemoAssets.MakeSwatch(Color.SeaGreen), CornerRadius = 0, Size = new Size(110, 66) });

            var s6 = AddSection("卡片家族", "FluentCard · FluentCardAction · FluentCardColor");
            var row6 = s6.AddRow();
            var card = new FluentCard { Size = new Size(200, 96), Margin = new Padding(0, 0, 12, 0) };
            card.Controls.Add(new FluentLabel { Text = "FluentCard\r\n子控件背景继承\r\n卡片主题", Location = new Point(14, 14), Size = new Size(170, 70) });
            row6.Controls.Add(card);
            var cardAction = new FluentCardAction { Size = new Size(200, 96), Margin = new Padding(0, 0, 12, 0) };
            cardAction.Controls.Add(new FluentLabel { Text = "FluentCardAction\r\n悬停高亮 · 可点击", Location = new Point(14, 14), Size = new Size(170, 70) });
            cardAction.Click += (s, e) => FluentToast.Show("卡片", "FluentCardAction 被点击。", InfoBarSeverity.Informational);
            row6.Controls.Add(cardAction);
            var cardColor = new FluentCardColor { Size = new Size(260, 96), AccentColor = Color.DarkOrange };
            cardColor.Controls.Add(new FluentLabel { Text = "FluentCardColor\r\n左侧强调色条", Location = new Point(18, 14), Size = new Size(230, 70) });
            row6.Controls.Add(cardColor);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _tip != null)
            {
                _tip.Dispose();
            }

            base.Dispose(disposing);
        }
    }

    internal sealed class InputPage : DemoPage
    {
        private bool _syncing;
        private FluentErrorProvider _errorProvider;
        private FluentToggleSwitch _switchDark;

        public InputPage()
            : base("输入控件", "文本、数值、下拉、勾选与评分控件;NumberBox 与 Slider 双向联动,深色开关与菜单同步。")
        {
            var s1 = AddSection("文本输入", "FluentTextBox · FluentPasswordBox · FluentMaskedTextBox");
            var row1 = s1.AddRow();
            var echo = new FluentLabel { Text = "回显: (空)", AutoSize = true, MaximumSize = new Size(160, 0), Margin = new Padding(8, 10, 0, 0) };
            var input = new FluentTextBox { PlaceholderText = "占位文本…", Size = new Size(220, 34), Margin = new Padding(0, 0, 12, 0) };
            input.TextChanged += (s, e) => echo.Text = "回显: " + input.Text;
            row1.Controls.Add(input);
            row1.Controls.Add(echo);
            row1.Controls.Add(new FluentPasswordBox { PlaceholderText = "密码…", Size = new Size(220, 34), Margin = new Padding(0, 0, 12, 0) });
            row1.Controls.Add(new FluentMaskedTextBox { Mask = "000-0000", PlaceholderText = "邮编 000-0000", Size = new Size(220, 34) });

            var s2 = AddSection("数值与滑块", "FluentNumberBox ↔ FluentSlider 双向联动");
            var row2 = s2.AddRow();
            var number = new FluentNumberBox { Minimum = 0, Maximum = 100, Value = 40, Size = new Size(110, 34), Margin = new Padding(0, 0, 12, 0) };
            var slider = new FluentSlider { Minimum = 0, Maximum = 100, Value = 40, Size = new Size(280, 32), Margin = new Padding(0, 2, 12, 0) };
            slider.ValueChanged += (s, e) =>
            {
                if (_syncing)
                {
                    return;
                }

                _syncing = true;
                number.Value = slider.Value;
                _syncing = false;
            };
            number.ValueChanged += (s, e) =>
            {
                if (_syncing)
                {
                    return;
                }

                _syncing = true;
                slider.Value = number.Value;
                _syncing = false;
            };
            row2.Controls.Add(number);
            row2.Controls.Add(slider);
            row2.Controls.Add(Note("拖动任一,另一同步"));

            var s3 = AddSection("下拉 / 自动建议", "FluentComboBox(未绑定/数据源绑定) · 原生 ComboBox 对比 · FluentAutoSuggestBox");
            var row3 = s3.AddRow();
            var combo = new FluentComboBox { Size = new Size(180, 34), Margin = new Padding(0, 0, 12, 0) };
            foreach (string city in DemoAssets.Cities)
            {
                combo.Items.Add(city);
            }

            combo.SelectedIndex = 0;
            row3.Controls.Add(combo);
            row3.Controls.Add(new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Size = new Size(150, 25), Font = new Font("Segoe UI", 10F), Items = { "北京", "上海", "广州" }, SelectedIndex = 0, Margin = new Padding(0, 4, 12, 0) });
            var boundCombo = new FluentComboBox { Size = new Size(180, 34), Margin = new Padding(0, 0, 12, 0) };
            boundCombo.DataSource = DemoAssets.CityData();
            boundCombo.DisplayMember = "Name";
            boundCombo.ValueMember = "Code";
            var boundLabel = new FluentLabel { Text = "Code: —", AutoSize = true, Margin = new Padding(0, 10, 12, 0) };
            boundCombo.SelectedIndexChanged += (s, e) => boundLabel.Text = "Code: " + boundCombo.SelectedValue;
            row3.Controls.Add(boundCombo);
            row3.Controls.Add(boundLabel);
            var suggest = new FluentAutoSuggestBox { PlaceholderText = "输入城市名…", Size = new Size(340, 34) };
            foreach (string city in DemoAssets.Cities)
            {
                suggest.Items.Add(city);
            }

            suggest.Margin = new Padding(0, 4, 0, 0);
            row3.Controls.Add(suggest);
            var row3b = s3.AddRow();
            row3b.Controls.Add(Note("绑定行: DataSource → DisplayMember Name / ValueMember Code"));

            var s4 = AddSection("勾选 / 单选 / 开关", "FluentCheckBox(含三态) · FluentRadioButton · FluentToggleSwitch");
            var row4 = s4.AddRow();
            row4.Controls.Add(new FluentCheckBox { Text = "启用通知", Checked = true, AutoSize = true, Margin = new Padding(0, 4, 16, 0) });
            var triStateLabel = new FluentLabel { Text = "状态: 未选", AutoSize = true, Margin = new Padding(6, 4, 24, 0) };
            var tri = new FluentCheckBox { Text = "三态", ThreeState = true, AutoSize = true, Margin = new Padding(0, 4, 8, 0) };
            tri.CheckStateChanged += (s, e) => triStateLabel.Text = "状态: " + tri.CheckState;
            row4.Controls.Add(tri);
            row4.Controls.Add(triStateLabel);
            var radioLabel = new FluentLabel { Text = "已选: 选项 A", AutoSize = true, Margin = new Padding(6, 4, 24, 0) };
            var radioA = new FluentRadioButton { Text = "选项 A", Checked = true, AutoSize = true, Margin = new Padding(0, 4, 12, 0) };
            var radioB = new FluentRadioButton { Text = "选项 B", AutoSize = true, Margin = new Padding(0, 4, 8, 0) };
            radioA.CheckedChanged += (s, e) => { if (radioA.Checked) radioLabel.Text = "已选: 选项 A"; };
            radioB.CheckedChanged += (s, e) => { if (radioB.Checked) radioLabel.Text = "已选: 选项 B"; };
            row4.Controls.Add(radioA);
            row4.Controls.Add(radioB);
            row4.Controls.Add(radioLabel);
            _switchDark = new FluentToggleSwitch { Text = "深色主题", Size = new Size(140, 32) };
            _switchDark.Checked = ApplicationThemeManager.EffectiveTheme == ApplicationTheme.Dark;
            _switchDark.CheckedChanged += (s, e) =>
            {
                var owner = FindForm() as MainForm;
                if (owner != null)
                {
                    owner.SetDark(_switchDark.Checked, _switchDark);
                }
            };
            row4.Controls.Add(_switchDark);

            var s5 = AddSection("评分 / 点赞 / 校验", "FluentRatingControl · FluentThumbRate · FluentErrorProvider");
            var row5 = s5.AddRow();
            var ratingLabel = new FluentLabel { Text = "星级: 3", AutoSize = true, Margin = new Padding(8, 6, 32, 0) };
            var rating = new FluentRatingControl { Value = 3, Size = new Size(146, 32), Margin = new Padding(0, 0, 0, 0) };
            rating.ValueChanged += (s, e) => ratingLabel.Text = "星级: " + rating.Value;
            row5.Controls.Add(rating);
            row5.Controls.Add(ratingLabel);
            var thumbLabel = new FluentLabel { Text = "点赞: 无", AutoSize = true, Margin = new Padding(8, 6, 32, 0) };
            var thumb = new FluentThumbRate { Margin = new Padding(0, 0, 0, 0) };
            thumb.ValueChanged += (s, e) => thumbLabel.Text = "点赞: " + (thumb.Value == 1 ? "赞" : thumb.Value == -1 ? "踩" : "无");
            row5.Controls.Add(thumb);
            row5.Controls.Add(thumbLabel);
            var validInput = new FluentTextBox { PlaceholderText = "必填字段", Size = new Size(200, 34) };
            _errorProvider = new FluentErrorProvider { BlinkStyle = ErrorBlinkStyle.NeverBlink };
            _errorProvider.SetError(validInput, "此字段为必填项");
            validInput.TextChanged += (s, e) => _errorProvider.SetError(validInput, validInput.Text.Length == 0 ? "此字段为必填项" : null);
            row5.Controls.Add(validInput);
            row5.Controls.Add(Note("清空即触发错误图标"));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _errorProvider != null)
            {
                _errorProvider.Dispose();
            }

            base.Dispose(disposing);
        }
    }

    internal sealed class ListPage : DemoPage
    {
        public ListPage()
            : base("列表与数据", "经典集合控件与数据网格;ListView 多列、GridView 磁贴、TreeGrid 树形表格。")
        {
            var s1 = AddSection("FluentListBox / FluentCheckedListBox", "单选列表 · 三态复选列表");
            var row1 = s1.AddRow();
            var list = new FluentListBox { Size = new Size(210, 150), Margin = new Padding(0, 0, 12, 0) };
            foreach (string city in DemoAssets.Cities)
            {
                list.Items.Add(city);
            }

            list.SelectedIndex = 0;
            var listLabel = new FluentLabel { Text = "已选: 北京", AutoSize = true, Margin = new Padding(6, 60, 24, 0) };
            list.SelectedIndexChanged += (s, e) => listLabel.Text = "已选: " + list.Items[list.SelectedIndex];
            row1.Controls.Add(list);
            row1.Controls.Add(listLabel);
            var checkedList = new FluentCheckedListBox { ThreeState = true, Size = new Size(210, 150) };
            checkedList.Items.Add("设计器");
            checkedList.Items.Add("工具箱");
            checkedList.Items.Add("数据绑定");
            checkedList.SetItemChecked(0, true);
            row1.Controls.Add(checkedList);

            var s2 = AddSection("FluentListView(详情视图)", "Columns + SubItemSelector 渲染多列");
            var row2 = s2.AddRow();
            var lv = new FluentListView { DetailsView = true, Size = new Size(360, 150) };
            lv.Columns.Add("城市");
            lv.Columns.Add("代码");
            lv.DataSource = DemoAssets.CityData();
            lv.DisplayMember = "Name";
            lv.SubItemSelector = item =>
            {
                var city = item as City;
                return city == null ? new string[0] : new[] { city.Name, city.Code };
            };
            row2.Controls.Add(lv);

            var s3 = AddSection("FluentTreeView", "RootNodes 树结构");
            var row3 = s3.AddRow();
            var tree = new FluentTreeView { Size = new Size(280, 150) };
            tree.RootNodes.Add(new FluentTreeNode("华东", new FluentTreeNode("上海"), new FluentTreeNode("杭州")));
            tree.RootNodes.Add(new FluentTreeNode("华北", new FluentTreeNode("北京")));
            tree.RootNodes.Add(new FluentTreeNode("华南", new FluentTreeNode("广州"), new FluentTreeNode("深圳")));
            tree.RootNodes[0].IsExpanded = true;
            row3.Controls.Add(tree);

            var s4 = AddSection("FluentGridView(磁贴)", "TileSize + IconSelector,点击选择");
            var row4 = s4.AddRow();
            var tiles = new FluentGridView { Size = new Size(360, 150), TileSize = new Size(84, 60), Margin = new Padding(0, 0, 12, 0) };
            tiles.IconSelector = item => SymbolRegular.Apps24;
            for (int i = 1; i <= 12; i++)
            {
                tiles.Items.Add("应用 " + i);
            }

            var tileLabel = new FluentLabel { Text = "点击磁贴选择", AutoSize = true, Margin = new Padding(6, 60, 0, 0) };
            tiles.ItemClicked += (s, e) => tileLabel.Text = "已选: " + e.Item;
            row4.Controls.Add(tiles);
            row4.Controls.Add(tileLabel);

            var s5 = AddSection("FluentDataGridView", "DataTable 直接绑定 DataSource");
            var row5 = s5.AddRow();
            var table = new System.Data.DataTable();
            table.Columns.Add("城市", typeof(string));
            table.Columns.Add("代码", typeof(string));
            table.Columns.Add("人口(百万)", typeof(double));
            table.Rows.Add("北京", "BJ", 21.8);
            table.Rows.Add("上海", "SH", 24.9);
            table.Rows.Add("广州", "GZ", 18.7);
            table.Rows.Add("深圳", "SZ", 17.6);
            table.Rows.Add("杭州", "HZ", 12.2);
            table.Rows.Add("成都", "CD", 21.2);
            var grid = new FluentDataGridView { Size = new Size(560, 190), RowHeadersVisible = false, DataSource = table };
            row5.Controls.Add(grid);

            var s6 = AddSection("FluentTreeGrid", "↑/↓ 选择,←/→ 展开收起");
            var row6 = s6.AddRow();
            var treeGrid = new FluentTreeGrid { Size = new Size(560, 170) };
            treeGrid.Columns.Add(new FluentTreeGridColumn("名称", 200));
            treeGrid.Columns.Add(new FluentTreeGridColumn("类型", 0));
            treeGrid.Columns.Add(new FluentTreeGridColumn("大小", 80));
            var src = new FluentTreeGridNode("src") { IsExpanded = true };
            src.Cells.Add("文件夹");
            src.Cells.Add("—");
            src.Nodes.Add(new FluentTreeGridNode("Controls") { Cells = { "文件夹", "—" } });
            src.Nodes.Add(new FluentTreeGridNode("Program.cs") { Cells = { "代码", "1 KB" } });
            treeGrid.RootNodes.Add(src);
            var readme = new FluentTreeGridNode("README.md");
            readme.Cells.Add("文档");
            readme.Cells.Add("9 KB");
            treeGrid.RootNodes.Add(readme);
            treeGrid.Rebuild();
            row6.Controls.Add(treeGrid);
        }
    }

    internal sealed class ContainersPage : DemoPage
    {
        public ContainersPage()
            : base("导航与容器", "页签、展开器、分隔容器、流式/等分面板、滚动容器与面包屑。")
        {
            var s1 = AddSection("FluentTabView", "点 + 新增,点 × 关闭,按住标签拖动重排");
            var row1 = s1.AddRow();
            var tabs = new FluentTabView { Size = new Size(420, 170) };
            var overview = tabs.AddTab(null, "总览");
            overview.Controls.Add(new FluentLabel { Text = "页签内容区 1", AutoSize = true, Location = new Point(10, 10) });
            var details = tabs.AddTab(null, "详情");
            details.Controls.Add(new FluentLabel { Text = "页签内容区 2", AutoSize = true, Location = new Point(10, 10) });
            row1.Controls.Add(tabs);

            var s2 = AddSection("FluentTabControl(内嵌)", "选中标签与内容面融合");
            var row2 = s2.AddRow();
            var tabs2 = new FluentTabControl { Size = new Size(420, 140) };
            var inner1 = new Panel { BackColor = Color.Transparent };
            inner1.Controls.Add(new FluentLabel { Text = "内嵌选项卡页 1", AutoSize = true, Location = new Point(10, 10) });
            var inner2 = new Panel { BackColor = Color.Transparent };
            inner2.Controls.Add(new FluentLabel { Text = "内嵌选项卡页 2", AutoSize = true, Location = new Point(10, 10) });
            tabs2.AddTab("页一", inner1);
            tabs2.AddTab("页二", inner2);
            row2.Controls.Add(tabs2);

            var s3 = AddSection("展开器", "FluentExpander · FluentCardExpander");
            var row3 = s3.AddRow();
            var expander = new FluentExpander { Header = "展开器:点击展开详情", Size = new Size(400, 48), Margin = new Padding(0, 0, 12, 0) };
            expander.ContentPanel.Controls.Add(new FluentLabel { Text = "内容面板背景随主题实时同步。", AutoSize = true });
            row3.Controls.Add(expander);
            var cardExpander = new FluentCardExpander { Header = "卡片式展开器", Size = new Size(400, 48) };
            cardExpander.ContentPanel.Controls.Add(new FluentLabel { Text = "FluentCardExpander 带卡片描边圆角。", AutoSize = true });
            row3.Controls.Add(cardExpander);

            var s4 = AddSection("FluentSplitContainer", "拖动分隔条,SetSplitterRatio 按比例定位");
            var row4 = s4.AddRow();
            var split = new FluentSplitContainer { Size = new Size(520, 150), Panel1MinSize = 60, Panel2MinSize = 60 };
            split.Panel1.Controls.Add(new FluentLabel { Text = "面板 1", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter });
            split.Panel2.Controls.Add(new FluentLabel { Text = "面板 2", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter });
            split.SetSplitterRatio(0.4);
            row4.Controls.Add(split);

            var s5 = AddSection("FluentWrapPanel / FluentUniformGrid", "自动换行 · 2×3 等分");
            var row5 = s5.AddRow();
            var wrap = new FluentWrapPanel { Size = new Size(320, 70), Margin = new Padding(0, 0, 12, 0) };
            for (int i = 0; i < 6; i++)
            {
                wrap.Controls.Add(new FluentButton { Text = "换行 " + i, AutoSize = true, Margin = new Padding(2) });
            }

            row5.Controls.Add(wrap);
            var uniform = new FluentUniformGrid { Size = new Size(300, 66), Rows = 2, Columns = 3 };
            for (int i = 0; i < 6; i++)
            {
                uniform.Controls.Add(new FluentLabel { Text = "格 " + (i + 1), TextAlign = ContentAlignment.MiddleCenter });
            }

            row5.Controls.Add(uniform);

            var s6 = AddSection("FluentScrollViewer / FluentBreadcrumbBar", "滚轮滚动内容区;点击面包屑项");
            var row6 = s6.AddRow();
            var scroller = new FluentScrollViewer { Size = new Size(240, 150), Margin = new Padding(0, 0, 12, 0) };
            var scrollContent = new Panel { Size = new Size(220, 380), BackColor = Color.Transparent };
            for (int i = 0; i < 10; i++)
            {
                scrollContent.Controls.Add(new FluentLabel { Text = "滚动内容行 " + (i + 1), AutoSize = true, Location = new Point(6, i * 36 + 6) });
            }

            scroller.Content = scrollContent;
            row6.Controls.Add(scroller);
            var crumbBox = new Panel { Size = new Size(420, 150) };
            var crumbs = new FluentBreadcrumbBar { Location = new Point(10, 10), Size = new Size(380, 30) };
            crumbs.SetPath("首页", "列表", "详情");
            var crumbLabel = new FluentLabel { Text = "点击面包屑项", AutoSize = true, Location = new Point(10, 48) };
            crumbs.ItemClicked += (s, index) => crumbLabel.Text = "点击了第 " + (index + 1) + " 级";
            crumbBox.Controls.Add(crumbs);
            crumbBox.Controls.Add(crumbLabel);
            row6.Controls.Add(crumbBox);

            var s7 = AddSection("标题栏演示窗", "FluentTitleBar + FluentClientAreaBorder 无边框窗口");
            var row7 = s7.AddRow();
            var chromeBtn = new FluentButton { Text = "打开标题栏演示窗", Appearance = ControlAppearance.Primary, Size = new Size(170, 32), Margin = new Padding(0, 0, 12, 0) };
            chromeBtn.Click += (s, e) => new ChromeDemoForm().Show();
            row7.Controls.Add(chromeBtn);
            row7.Controls.Add(Note("无边框拖拽 / 双击最大化 / 边缘缩放 / 右键系统菜单"));
        }
    }

    internal sealed class PickerPage : DemoPage
    {
        public PickerPage()
            : base("日期与选择", "日期、时间、日历与颜色选择器,下拉浮层与主题同步。")
        {
            var s1 = AddSection("日期 / 时间选择", "FluentDatePicker · FluentCalendarDatePicker · FluentTimePicker");
            var row1 = s1.AddRow();
            var datePick = new FluentDatePicker { Size = new Size(220, 34), Margin = new Padding(0, 0, 12, 0) };
            var dateLabel = new FluentLabel { Text = "已选: —", AutoSize = true, Margin = new Padding(0, 8, 24, 0) };
            datePick.DateSelected += (s, e) => dateLabel.Text = "已选: " + datePick.SelectedDate.ToString("yyyy-MM-dd");
            row1.Controls.Add(datePick);
            row1.Controls.Add(dateLabel);
            var calPick = new FluentCalendarDatePicker { PlaceholderText = "选择日期…", Size = new Size(220, 34), Margin = new Padding(0, 0, 12, 0) };
            row1.Controls.Add(calPick);
            var timePick = new FluentTimePicker { Size = new Size(220, 34), Margin = new Padding(0, 0, 12, 0) };
            var timeLabel = new FluentLabel { Text = "已选: —", AutoSize = true, Margin = new Padding(0, 8, 0, 0) };
            timePick.TimeSelected += (s, e) => timeLabel.Text = "已选: " + timePick.SelectedTime;
            row1.Controls.Add(timePick);
            row1.Controls.Add(timeLabel);

            var s2 = AddSection("FluentCalendar(内嵌日历)", "点选日期,月份可切换");
            var row2 = s2.AddRow();
            var calendar = new FluentCalendar { Size = new Size(300, 285), Margin = new Padding(0, 0, 12, 0) };
            var calLabel = new FluentLabel { Text = "日历选中: —", AutoSize = true, Margin = new Padding(6, 120, 0, 0) };
            calendar.DateSelected += (s, e) => calLabel.Text = "日历选中: " + (calendar.SelectedDate.HasValue ? calendar.SelectedDate.Value.ToString("yyyy-MM-dd") : "—");
            row2.Controls.Add(calendar);
            row2.Controls.Add(calLabel);

            var s3 = AddSection("FluentColorPicker", "ColorChanged 实时输出");
            var row3 = s3.AddRow();
            var color = new FluentColorPicker { Size = new Size(260, 264), Margin = new Padding(0, 0, 12, 0) };
            row3.Controls.Add(color);
            var colorSwatch = new Panel { Size = new Size(60, 60), BorderStyle = BorderStyle.None, BackColor = color.Color, Margin = new Padding(0, 8, 12, 0) };
            var colorLabel = new FluentLabel { Text = "", AutoSize = true, Margin = new Padding(0, 30, 0, 0) };
            color.ColorChanged += (s, e) =>
            {
                colorSwatch.BackColor = color.Color;
                colorLabel.Text = "#" + color.Color.R.ToString("X2") + color.Color.G.ToString("X2") + color.Color.B.ToString("X2");
            };
            row3.Controls.Add(colorSwatch);
            row3.Controls.Add(colorLabel);
        }
    }

    internal sealed class FeedbackPage : DemoPage
    {
        private FluentProgressBar _progress;
        private FluentProgressRing _ring;
        private FluentArc _arc;
        private FluentLabel _valueLabel;

        public FeedbackPage()
            : base("反馈与通知", "进度、信息栏、徽章、Snackbar、Toast、托盘气泡与加载遮罩。")
        {
            var s1 = AddSection("进度指示", "FluentProgressBar · FluentProgressRing · FluentArc");
            var row1 = s1.AddRow();
            var inc = new FluentButton { Text = "+10%", Size = new Size(80, 32), Margin = new Padding(0, 0, 10, 0) };
            inc.Click += (s, e) => UpdateProgress(Math.Min(100, _progress.Value + 10));
            var reset = new FluentButton { Text = "重置", Size = new Size(80, 32), Margin = new Padding(0, 0, 10, 0) };
            reset.Click += (s, e) => UpdateProgress(0);
            var ringBtn = new FluentButton { Text = "环开/关", Size = new Size(80, 32), Margin = new Padding(0, 0, 20, 0) };
            ringBtn.Click += (s, e) => _ring.IsActive = !_ring.IsActive;
            row1.Controls.Add(inc);
            row1.Controls.Add(reset);
            row1.Controls.Add(ringBtn);
            row1.Controls.Add(_ring = new FluentProgressRing { IsActive = true, Size = new Size(40, 40), Margin = new Padding(0, 0, 10, 0) });
            row1.Controls.Add(_arc = new FluentArc { Size = new Size(40, 40), Thickness = 4f, Margin = new Padding(0, 0, 10, 0) });
            row1.Controls.Add(_valueLabel = new FluentLabel { Text = "40%", AutoSize = true, Margin = new Padding(0, 12, 0, 0) });
            var row1b = s1.AddRow();
            row1b.Controls.Add(_progress = new FluentProgressBar { Minimum = 0, Maximum = 100, Value = 40, Size = new Size(560, 8) });

            var s2 = AddSection("FluentInfoBar", "四种严重级别实时切换");
            var row2 = s2.AddRow();
            var bar = new FluentInfoBar { Title = "提示", Message = "信息栏支持四种严重级别。", Severity = InfoBarSeverity.Informational, Size = new Size(420, 72), Margin = new Padding(0, 0, 12, 0) };
            row2.Controls.Add(bar);
            var severities = new[]
            {
                new { Text = "信息", Severity = InfoBarSeverity.Informational },
                new { Text = "成功", Severity = InfoBarSeverity.Success },
                new { Text = "警告", Severity = InfoBarSeverity.Warning },
                new { Text = "错误", Severity = InfoBarSeverity.Error }
            };
            foreach (var def in severities)
            {
                var btn = new FluentButton { Text = def.Text, Size = new Size(64, 30), Margin = new Padding(0, 0, 8, 0) };
                InfoBarSeverity sev = def.Severity;
                btn.Click += (s, e) => bar.Severity = sev;
                row2.Controls.Add(btn);
            }

            var s3 = AddSection("徽章", "FluentInfoBadge(数值) · FluentBadge(文字)");
            var row3 = s3.AddRow();
            var infoBadge = new FluentInfoBadge { Value = 3, Margin = new Padding(0, 4, 6, 0) };
            var plusBadge = new FluentButton { Text = "+", Size = new Size(32, 30), Margin = new Padding(0, 0, 4, 0) };
            plusBadge.Click += (s, e) => infoBadge.Value = infoBadge.Value + 1;
            var minusBadge = new FluentButton { Text = "−", Size = new Size(32, 30), Margin = new Padding(0, 0, 24, 0) };
            minusBadge.Click += (s, e) => infoBadge.Value = Math.Max(0, infoBadge.Value - 1);
            row3.Controls.Add(infoBadge);
            row3.Controls.Add(plusBadge);
            row3.Controls.Add(minusBadge);
            row3.Controls.Add(new FluentBadge { Content = "NEW", Appearance = ControlAppearance.Success, Size = new Size(52, 22), Margin = new Padding(0, 8, 12, 0) });
            row3.Controls.Add(new FluentBadge { Content = "限时", Appearance = ControlAppearance.Danger, Size = new Size(52, 22), Margin = new Padding(0, 8, 0, 0) });

            var s4 = AddSection("Snackbar / Toast", "窗口底部 Snackbar;右上角堆叠 Toast");
            var row4 = s4.AddRow();
            var snack = new FluentButton { Text = "弹出 Snackbar", Size = new Size(140, 32), Margin = new Padding(0, 0, 20, 0) };
            snack.Click += (s, e) =>
            {
                var owner = FindForm();
                if (owner != null)
                {
                    FluentSnackbar.Show(owner, "已保存", "文档已写入磁盘。", "撤销");
                }
            };
            row4.Controls.Add(snack);
            var toastDefs = new[]
            {
                new { Text = "Toast 信息", Severity = InfoBarSeverity.Informational, Message = "这是一条普通信息通知。" },
                new { Text = "Toast 成功", Severity = InfoBarSeverity.Success, Message = "操作已成功完成。" },
                new { Text = "Toast 警告", Severity = InfoBarSeverity.Warning, Message = "连接断开,稍后自动重试。" },
                new { Text = "Toast 错误", Severity = InfoBarSeverity.Error, Message = "无法保存,请检查权限。" }
            };
            foreach (var def in toastDefs)
            {
                var btn = new FluentButton { Text = def.Text, Size = new Size(110, 32), Margin = new Padding(0, 0, 8, 0) };
                InfoBarSeverity severity = def.Severity;
                string message = def.Message;
                btn.Click += (s, e) => FluentToast.Show(def.Text, message, severity, TimeSpan.FromSeconds(4));
                row4.Controls.Add(btn);
            }

            var s5 = AddSection("托盘 / 加载遮罩", "FluentNotifyIcon 气泡 · FluentLoadingScreen");
            var row5 = s5.AddRow();
            var trayBtn = new FluentButton { Text = "注册托盘图标", Size = new Size(150, 32), Margin = new Padding(0, 0, 12, 0) };
            trayBtn.Click += (s, e) =>
            {
                var owner = FindForm() as MainForm;
                if (owner != null)
                {
                    owner.RegisterTray();
                }
            };
            row5.Controls.Add(trayBtn);
            var loadBtn = new FluentButton { Text = "加载遮罩 2 秒", Size = new Size(150, 32) };
            loadBtn.Click += (s, e) =>
            {
                var owner = FindForm();
                if (owner != null)
                {
                    DemoAssets.ShowLoading(owner);
                }
            };
            row5.Controls.Add(loadBtn);
        }

        private void UpdateProgress(double value)
        {
            _progress.Value = value;
            _arc.EndAngle = (float)(value * 3.6);
            _valueLabel.Text = (int)value + "%";
        }
    }

    internal sealed class DialogPage : DemoPage
    {
        public DialogPage()
            : base("对话框与菜单", "消息框、内容对话框、Flyout、右键菜单与工具栏;顶部 FluentMenuStrip 见「视图/工具/帮助」。")
        {
            var s1 = AddSection("FluentMessageBox", "静态 Show,支持按钮组合与图标");
            var row1 = s1.AddRow();
            var msgBtn = new FluentButton { Text = "简单提示", Size = new Size(130, 32), Margin = new Padding(0, 0, 12, 0) };
            msgBtn.Click += (s, e) => FluentMessageBox.Show(FindForm(), "这是一条消息提示。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            row1.Controls.Add(msgBtn);
            var confirmLabel = new FluentLabel { Text = "结果: —", AutoSize = true, Margin = new Padding(8, 8, 0, 0) };
            var confirmBtn = new FluentButton { Text = "确认/取消", Appearance = ControlAppearance.Primary, Size = new Size(130, 32), Margin = new Padding(0, 0, 12, 0) };
            confirmBtn.Click += (s, e) =>
            {
                var result = FluentMessageBox.Show(FindForm(), "是否继续删除操作?", "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                confirmLabel.Text = "结果: " + result;
            };
            row1.Controls.Add(confirmBtn);
            row1.Controls.Add(confirmLabel);

            var s2 = AddSection("FluentContentDialog / FluentFlyout", "模态内容对话框;锚定控件旁弹出浮层");
            var row2 = s2.AddRow();
            var dialogBtn = new FluentButton { Text = "内容对话框", Size = new Size(130, 32), Margin = new Padding(0, 0, 12, 0) };
            dialogBtn.Click += (s, e) =>
            {
                var dialog = new FluentContentDialog
                {
                    Title = "订阅确认",
                    Content = new FluentLabel { Text = "内容对话框支持任意控件宿主,\r\n主/次/关闭三按钮。", AutoSize = true },
                    PrimaryButtonText = "订阅",
                    SecondaryButtonText = "以后再说",
                    CloseButtonText = "取消"
                };
                dialog.Show(FindForm());
            };
            row2.Controls.Add(dialogBtn);
            var flyoutBtn = new FluentButton { Text = "锚定弹出", Size = new Size(130, 32) };
            flyoutBtn.Click += (s, e) =>
            {
                var flyout = new FluentFlyout
                {
                    Content = new FluentLabel { Text = "FluentFlyout:锚定控件旁弹出浮层。", AutoSize = true, Padding = new Padding(10) }
                };
                flyout.ShowAt(flyoutBtn);
            };
            row2.Controls.Add(flyoutBtn);

            var s3 = AddSection("FluentContextMenuStrip / FluentToolStrip", "右键面板弹菜单;工具栏窄容器自动溢出");
            var row3 = s3.AddRow();
            var ctx = new FluentContextMenuStrip();
            ctx.Items.Add("复制");
            ctx.Items.Add("粘贴");
            ctx.Items.Add(new ToolStripSeparator());
            ctx.Items.Add("删除");
            var ctxHost = new Panel { Size = new Size(260, 90), BorderStyle = BorderStyle.FixedSingle, BackColor = Color.Transparent, Margin = new Padding(0, 0, 12, 0) };
            ctxHost.Controls.Add(new FluentLabel { Text = "在此区域单击右键", AutoSize = true, Location = new Point(80, 35) });
            ctxHost.ContextMenuStrip = ctx;
            row3.Controls.Add(ctxHost);
            var stripHost = new Panel { Size = new Size(300, 90), BorderStyle = BorderStyle.FixedSingle, BackColor = Color.Transparent };
            var strip = new FluentToolStrip { AutoSize = false, Size = new Size(296, 30), Location = new Point(0, 0) };
            strip.Items.Add(new ToolStripButton("新建") { DisplayStyle = ToolStripItemDisplayStyle.Text });
            strip.Items.Add(new ToolStripButton("打开") { DisplayStyle = ToolStripItemDisplayStyle.Text });
            strip.Items.Add(new ToolStripSeparator());
            var more = new ToolStripDropDownButton("更多") { DisplayStyle = ToolStripItemDisplayStyle.Text };
            more.DropDownItems.Add("重命名");
            more.DropDownItems.Add("删除");
            strip.Items.Add(more);
            for (int i = 0; i < 6; i++)
            {
                strip.Items.Add(new ToolStripButton("项" + (i + 1)) { DisplayStyle = ToolStripItemDisplayStyle.Text });
            }

            stripHost.Controls.Add(strip);
            stripHost.Controls.Add(new FluentLabel { Text = "右侧溢出 «", AutoSize = true, Location = new Point(80, 55) });
            row3.Controls.Add(stripHost);

            var s4 = AddSection("FluentMenuStrip", "本窗口顶部菜单栏");
            var row4 = s4.AddRow();
            row4.Controls.Add(new FluentLabel
            {
                Text = "「视图」分页跳转 + 深色模式(Ctrl+D)\r\n「工具」标题栏演示 / 托盘 / 加载遮罩\r\n「帮助」关于对话框\r\n菜单与弹出层随主题深浅自动切换。",
                AutoSize = true
            });
        }
    }

    internal sealed class CodePage : DemoPage
    {
        public CodePage()
            : base("代码与滚动", "语法高亮代码块、富文本外壳与独立滚动条。")
        {
            var s1 = AddSection("FluentCodeBlock(C#)", "正则着色,只读可选,支持复制源码");
            var row1 = s1.AddRow();
            var codeCs = new FluentCodeBlock
            {
                Language = SyntaxLanguage.CSharp,
                Size = new Size(430, 210),
                Margin = new Padding(0, 0, 12, 0),
                Code = "class Demo\n{\n    // 注释行\n    static void Main()\n    {\n        var msg = \"hello, fluent\";\n        System.Console.WriteLine(msg); // 打印\n    }\n}"
            };
            row1.Controls.Add(codeCs);
            var copyCs = new FluentButton { Text = "复制源码", Size = new Size(110, 32) };
            copyCs.Click += (s, e) =>
            {
                codeCs.CopySourceToClipboard();
                FluentToast.Show("已复制", "C# 源码已写入剪贴板。", InfoBarSeverity.Success);
            };
            row1.Controls.Add(copyCs);

            var s2 = AddSection("FluentCodeBlock(XAML)", null);
            var row2 = s2.AddRow();
            var codeXaml = new FluentCodeBlock
            {
                Language = SyntaxLanguage.Xaml,
                Size = new Size(430, 140),
                Code = "<StackPanel Orientation=\"Vertical\">\n    <Button Content=\"确定\" />\n    <!-- XAML 注释 -->\n</StackPanel>"
            };
            row2.Controls.Add(codeXaml);

            var s3 = AddSection("FluentRichTextBox", "颜色/粗体追加;取消勾选后无法框选");
            var row3 = s3.AddRow();
            var rich = new FluentRichTextBox { Size = new Size(340, 140), Margin = new Padding(0, 0, 12, 0) };
            var richSel = new FluentCheckBox { Text = "允许选中", Checked = true, AutoSize = true, Margin = new Padding(6, 60, 0, 0) };
            richSel.CheckedChanged += (s, e) => rich.IsTextSelectionEnabled = richSel.Checked;
            row3.Controls.Add(rich);
            row3.Controls.Add(richSel);
            var richFilled = false;
            rich.HandleCreated += (s, e) =>
            {
                if (!richFilled)
                {
                    richFilled = true;
                    rich.SelectionColorValue = Color.Crimson;
                    rich.SelectionFontValue = new Font("Segoe UI", 9.75F, FontStyle.Bold);
                    rich.AppendText("富文本外壳(可编辑)\n");
                    rich.SelectionColorValue = Color.FromArgb(0x33, 0x33, 0x33);
                    rich.SelectionFontValue = new Font("Segoe UI", 9.75F);
                    rich.AppendText("正文段落:支持颜色与粗体追加。\n");
                    rich.SelectionColorValue = Color.FromArgb(0, 0x99, 0x99);
                    rich.SelectionFontValue = new Font("Segoe UI", 9.75F, FontStyle.Bold);
                    rich.AppendText("焦点时底部显示强调条。");
                }
            };

            var s4 = AddSection("FluentScrollBar", "常规 / Dynamic 悬停显示 / 水平方向");
            var row4 = s4.AddRow();
            var vbar = new FluentScrollBar { Size = new Size(14, 120), Maximum = 100, LargeChange = 20, Margin = new Padding(0, 0, 6, 0) };
            var vbarDynamic = new FluentScrollBar { Size = new Size(14, 120), Maximum = 100, LargeChange = 20, Dynamic = true, Timeout = 1200, Margin = new Padding(0, 0, 12, 0) };
            var scrollValue = new FluentLabel { Text = "值: 0", AutoSize = true, Margin = new Padding(6, 50, 24, 0) };
            vbar.ValueChanged += (s, e) => scrollValue.Text = "值: " + vbar.Value;
            row4.Controls.Add(vbar);
            row4.Controls.Add(vbarDynamic);
            row4.Controls.Add(scrollValue);
            var hbar = new FluentScrollBar { Orientation = Orientation.Horizontal, Size = new Size(300, 14), Maximum = 100, LargeChange = 20, Margin = new Padding(0, 50, 0, 0) };
            row4.Controls.Add(hbar);
            row4.Controls.Add(Note("← 水平条"));
        }
    }

    internal sealed class SettingsPage : DemoPage
    {
        private FluentToggleSwitch _dark;

        public SettingsPage()
            : base("设置", "FluentNavigationView 底部 Footer 导航项示例:全局设置页。")
        {
            var s1 = AddSection("外观", "与顶部菜单「深色模式」双向同步");
            var row1 = s1.AddRow();
            _dark = new FluentToggleSwitch { Text = "深色主题", Size = new Size(140, 32) };
            _dark.Checked = ApplicationThemeManager.EffectiveTheme == ApplicationTheme.Dark;
            _dark.CheckedChanged += (s, e) =>
            {
                var owner = FindForm() as MainForm;
                if (owner != null)
                {
                    owner.SetDark(_dark.Checked, _dark);
                }
            };
            row1.Controls.Add(_dark);

            var s2 = AddSection("系统入口", null);
            var row2 = s2.AddRow();
            var trayBtn = new FluentButton { Text = "注册托盘图标", Size = new Size(150, 32), Margin = new Padding(0, 0, 12, 0) };
            trayBtn.Click += (s, e) =>
            {
                var owner = FindForm() as MainForm;
                if (owner != null)
                {
                    owner.RegisterTray();
                }
            };
            row2.Controls.Add(trayBtn);
            var chromeBtn = new FluentButton { Text = "打开标题栏演示窗", Appearance = ControlAppearance.Primary, Size = new Size(170, 32) };
            chromeBtn.Click += (s, e) => new ChromeDemoForm().Show();
            row2.Controls.Add(chromeBtn);

            var s3 = AddSection("关于", null);
            var row3 = s3.AddRow();
            row3.Controls.Add(new FluentLabel
            {
                Text = "WinformUi 1.4.0 · .NET Framework 4.6 · WinForms Fluent Design 控件库\r\n本演示按控件种类分页,由库自身控件渲染(主题/DPI 自适应)。",
                AutoSize = true
            });
        }
    }

    public class City
    {
        public string Name { get; set; }
        public string Code { get; set; }
    }
}
