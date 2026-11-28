using System;
using System.Drawing;
using System.Windows.Forms;
using WinformUi.Controls;
using WinformUi.Interop;
using WinformUi.Theming;

namespace WinformUi.Gallery
{
    public class MainForm : FluentWindow
    {
        private readonly FluentNavigationView _navigation;
        private readonly FluentStatusStrip _statusStrip;
        private readonly ToolStripStatusLabel _statusLabel;

        public MainForm()
        {
            Text = "WinformUi Gallery";
            ClientSize = new Size(1100, 720);
            MinimumSize = new Size(860, 560);

            _navigation = new FluentNavigationView { Dock = DockStyle.Fill };
            _navigation.MenuItems.Add(new NavigationViewItem("Home", SymbolRegular.Home24, typeof(HomePage)));
            _navigation.MenuItems.Add(new NavigationViewItem("Buttons", SymbolRegular.Keyboard24, typeof(ButtonsPage)));
            _navigation.MenuItems.Add(new NavigationViewItem("Inputs", SymbolRegular.Code24, typeof(InputsPage)));
            _navigation.MenuItems.Add(new NavigationViewItem("Date & Time", SymbolRegular.Calendar24, typeof(DateTimePage)));
            _navigation.MenuItems.Add(new NavigationViewItem("Progress", SymbolRegular.Star24, typeof(ProgressPage)));
            _navigation.MenuItems.Add(new NavigationViewItem("Collections", SymbolRegular.List24, typeof(CollectionsPage)));
            _navigation.MenuItems.Add(new NavigationViewItem("Containers", SymbolRegular.Apps24, typeof(ContainersPage)));
            _navigation.MenuItems.Add(new NavigationViewItem("Dialogs", SymbolRegular.Alert24, typeof(DialogsPage)));
            _navigation.MenuItems.Add(new NavigationViewItem("Menus", SymbolRegular.Navigation24, typeof(MenusPage)));
            _navigation.MenuItems.Add(new NavigationViewItem("Widgets", SymbolRegular.ClipboardTextLtr24, typeof(WidgetsPage)));
            _navigation.MenuItems.Add(new NavigationViewItem("Layouts", SymbolRegular.DataUsageToolbox24, typeof(LayoutsPage)));
            _navigation.MenuItems.Add(new NavigationViewItem("Navigation", SymbolRegular.PanelLeft24, typeof(NavigationPage)));
            _navigation.FooterMenuItems.Add(new NavigationViewItem("Settings", SymbolRegular.Settings24, typeof(SettingsPage)));

            _statusStrip = new FluentStatusStrip();
            _statusLabel = new ToolStripStatusLabel
            {
                TextAlign = ContentAlignment.MiddleLeft,
                Font = FluentTypography.Caption
            };
            _statusStrip.Items.Add(_statusLabel);

            Controls.Add(_navigation);
            Controls.Add(_statusStrip);

            _navigation.SelectItem(_navigation.MenuItems[0], true);
            UpdateStatus();
            ApplicationThemeManager.ThemeChanged += (s, e) => UpdateStatus();
        }

        /// <summary>Selects a navigation page by its menu caption (used by the screenshot harness).</summary>
        public void SelectPage(string content)
        {
            foreach (NavigationViewItem item in _navigation.MenuItems)
            {
                if (string.Equals(item.Content, content, StringComparison.OrdinalIgnoreCase))
                {
                    _navigation.SelectItem(item, true);
                    return;
                }
            }
        }

        internal void UpdateStatus()
        {
            string backdrop = IsBackdropApplied ? BackdropType.ToString() : "Solid (backdrop unavailable)";
            _statusLabel.Text = string.Format("Theme: {0}   |   Backdrop: {1}   |   Windows build {2}",
                ApplicationThemeManager.EffectiveTheme, backdrop, NativeMethods.OsVersion.Build);
        }

        internal void SetBackdrop(WindowBackdropType type)
        {
            BackdropType = type;
            UpdateStatus();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            UpdateStatus();
        }
    }
}