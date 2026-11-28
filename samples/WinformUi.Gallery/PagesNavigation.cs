using System;
using System.Drawing;
using System.Windows.Forms;
using WinformUi.Controls;
using WinformUi.Theming;

namespace WinformUi.Gallery
{
    public sealed class NavigationPage : GalleryPage
    {
        private readonly FluentNavigationView _navigation;
        private readonly FluentLabel _status;

        public NavigationPage()
            : base("Navigation")
        {
            AddDescription("NavigationView 支持分组标题、分隔线、按 Tag 传递导航参数、历史前进后退以及页面缓存模式。");

            AddSection("分组菜单与参数导航");
            FlowLayoutPanel row = AddRow();
            var host = new SurfacePanel
            {
                Size = new Size(760, 380),
                Margin = new Padding(0, 0, 12, 8)
            };

            _navigation = new FluentNavigationView { Dock = DockStyle.Fill, PaneWidth = 190 };
            _navigation.MenuItems.Add(new NavigationViewItemHeader("概览"));
            _navigation.MenuItems.Add(new NavigationViewItem("Home", SymbolRegular.Home24, typeof(GalleryNavigationPage)) { Tag = "home" });
            _navigation.MenuItems.Add(new NavigationViewItem("参数 A", SymbolRegular.Code24, typeof(GalleryNavigationParameterPage)) { Tag = "A" });
            _navigation.MenuItems.Add(new NavigationViewItem("参数 B", SymbolRegular.DataUsageToolbox24, typeof(GalleryNavigationParameterPage)) { Tag = "B" });
            _navigation.MenuItems.Add(new NavigationViewItemSeparator());
            _navigation.MenuItems.Add(new NavigationViewItemHeader("缓存行为"));
            _navigation.MenuItems.Add(new NavigationViewItem("Disabled", SymbolRegular.Alert24, typeof(GalleryNavigationDisabledPage)));
            _navigation.FooterMenuItems.Add(new NavigationViewItem("Required", SymbolRegular.Star24, typeof(GalleryNavigationRequiredPage)));

            var navActions = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                Padding = new Padding(8),
                BackColor = Color.Transparent
            };
            var backButton = new FluentButton { Text = "后退", Appearance = ControlAppearance.Secondary, AutoSize = true, Margin = new Padding(0, 0, 8, 0), Enabled = false };
            var forwardButton = new FluentButton { Text = "前进", Appearance = ControlAppearance.Secondary, AutoSize = true, Enabled = false };
            _status = new FluentLabel { Text = "页面：-   参数：-   模式：-   缓存：-", AutoSize = true, Margin = new Padding(0, 0, 0, 12) };
            backButton.Click += (s, e) => _navigation.Frame.GoBack();
            forwardButton.Click += (s, e) => _navigation.Frame.GoForward();
            navActions.Controls.Add(backButton);
            navActions.Controls.Add(forwardButton);

            _navigation.Frame.Navigated += (s, e) =>
            {
                var args = e as NavigationEventArgs;
                if (args != null)
                {
                    _status.Text = string.Format(
                        "页面：{0}   参数：{1}   模式：{2}   缓存：{3}",
                        args.PageType == null ? "-" : args.PageType.Name,
                        args.Parameter ?? "-",
                        args.Mode,
                        args.CacheMode);
                    backButton.Enabled = _navigation.Frame.CanGoBack;
                    forwardButton.Enabled = _navigation.Frame.CanGoForward;
                }
            };

            host.Controls.Add(_navigation);
            host.Controls.Add(navActions);
            row.Controls.Add(host);
            row.Controls.Add(_status);
            _navigation.SelectItem(_navigation.MenuItems[1], true);
        }
    }

    internal sealed class GalleryNavigationPage : GalleryPage
    {
        private readonly FluentLabel _parameter;

        public GalleryNavigationPage()
            : base("Home")
        {
            AddDescription("这个页面通过 NavigationViewItem.Tag 接收导航参数。");
            _parameter = new FluentLabel { Text = "参数：-", AutoSize = true, Font = FluentTypography.BodyLarge };
            Root.Controls.Add(_parameter);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            _parameter.Text = "参数：" + (e.Parameter ?? "-");
            base.OnNavigatedTo(e);
        }
    }

    internal sealed class GalleryNavigationParameterPage : GalleryPage
    {
        private readonly FluentLabel _parameter;

        public GalleryNavigationParameterPage()
            : base("Parameter Page")
        {
            AddDescription("同一页面类型配合不同参数会分别进入历史与缓存。");
            _parameter = new FluentLabel { Text = "参数：-", AutoSize = true, Font = FluentTypography.BodyLarge };
            Root.Controls.Add(_parameter);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            _parameter.Text = "参数：" + (e.Parameter ?? "-") + "   来源：" + e.Mode;
            base.OnNavigatedTo(e);
        }
    }

    internal sealed class GalleryNavigationDisabledPage : GalleryPage
    {
        private static int _count;
        private readonly int _instance;

        public GalleryNavigationDisabledPage()
            : base("Disabled Cache")
        {
            _instance = ++_count;
            NavigationCacheMode = NavigationCacheMode.Disabled;
            AddDescription("Disabled 页面每次导航都会创建新实例，离开后释放。");
            Root.Controls.Add(new FluentLabel { Text = "实例：" + _instance, AutoSize = true, Font = FluentTypography.BodyLarge });
        }
    }

    internal sealed class GalleryNavigationRequiredPage : GalleryPage
    {
        private static int _count;
        private readonly int _instance;

        public GalleryNavigationRequiredPage()
            : base("Required Cache")
        {
            _instance = ++_count;
            NavigationCacheMode = NavigationCacheMode.Required;
            AddDescription("Required 页面始终复用同一实例，不会因 CacheSize 被淘汰。");
            Root.Controls.Add(new FluentLabel { Text = "实例：" + _instance, AutoSize = true, Font = FluentTypography.BodyLarge });
        }
    }
}
