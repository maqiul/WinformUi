using System;
using System.Drawing;
using System.Windows.Forms;
using WinformUi.Controls;
using WinformUi.Theming;

namespace WinformUi.Demo
{
    /// <summary>FluentTitleBar + FluentClientAreaBorder 无边框演示窗体。</summary>
    public class ChromeDemoForm : Form
    {
        private readonly FluentTitleBar _bar;
        private readonly FluentLabel _saveState;

        public ChromeDemoForm()
        {
            Text = "标题栏演示";
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(640, 420);
            MinimumSize = new Size(420, 280);
            ShowInTaskbar = true;
            Icon = SystemIcons.Application;
            Font = FluentTypography.Body;
            BackColor = ApplicationThemeManager.Palette.ApplicationBackground;

            _saveState = new FluentLabel { Text = "未保存", AutoSize = true };

            _bar = new FluentTitleBar
            {
                Title = "标题栏演示",
                ShowHelpButton = true,
                TrailingContent = _saveState
            };
            _bar.HelpButtonClick += (s, e) => FluentToast.Show("帮助", "真实应用里这里会打开文档。", InfoBarSeverity.Informational);

            var body = new FluentClientAreaBorder { Dock = DockStyle.Fill };

            var hint = new FluentLabel
            {
                Text = "空白处拖拽移动 · 双击标题栏最大化 · 右键弹系统菜单 · 无边框时顶端/左右边缘可拉伸",
                AutoSize = true,
                Location = new Point(24, 20)
            };
            body.Controls.Add(hint);

            var darkLabel = new FluentLabel { Text = "深色主题", AutoSize = true, Location = new Point(24, 64) };
            var darkToggle = new FluentToggleSwitch
            {
                Location = new Point(100, 60),
                Size = new Size(44, 24),
                Checked = ApplicationThemeManager.EffectiveTheme == ApplicationTheme.Dark
            };
            darkToggle.CheckedChanged += (s, e) =>
                ApplicationThemeManager.Apply(darkToggle.Checked ? ApplicationTheme.Dark : ApplicationTheme.Light);
            body.Controls.Add(darkLabel);
            body.Controls.Add(darkToggle);

            var maxBtn = new FluentButton { Text = "最大化 / 还原", Location = new Point(24, 104), Size = new Size(140, 32) };
            maxBtn.Click += (s, e) =>
                WindowState = WindowState == FormWindowState.Maximized ? FormWindowState.Normal : FormWindowState.Maximized;
            body.Controls.Add(maxBtn);

            var saveBtn = new FluentButton
            {
                Text = "模拟保存",
                Appearance = ControlAppearance.Primary,
                Location = new Point(174, 104),
                Size = new Size(140, 32)
            };
            saveBtn.Click += (s, e) =>
            {
                _saveState.Text = "已保存 " + DateTime.Now.ToString("HH:mm:ss");
                FluentToast.Show("已保存", "标题栏右侧状态标签已更新。", InfoBarSeverity.Success, TimeSpan.FromSeconds(3));
            };
            body.Controls.Add(saveBtn);

            var leadBox = new FluentComboBox
            {
                Location = new Point(24, 152),
                Size = new Size(200, 32)
            };
            leadBox.Items.Add("视图 A");
            leadBox.Items.Add("视图 B");
            leadBox.SelectedIndex = 0;
            body.Controls.Add(leadBox);

            var previewLabel = new FluentLabel
            {
                Text = "LeadingContent 演示:标题栏插槽可放任意控件。",
                AutoSize = true,
                Location = new Point(24, 200)
            };
            body.Controls.Add(previewLabel);

            // WinForms Dock 次序:先加 Fill 内容,再加 Top 标题栏,否则 Fill 会盖住标题栏区域。
            Controls.Add(body);
            Controls.Add(_bar);
        }
    }
}
