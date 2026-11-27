using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using WinformUi.Controls;
using WinformUi.Theming;

namespace WinformUi.Gallery
{
    /// <summary>FluentIcon / FluentPictureBox / FluentToolStrip / FluentRichTextBox / FluentCodeBlock / FluentScrollBar.</summary>
    public class WidgetsPage : GalleryPage
    {
        public WidgetsPage() : base("Widgets")
        {
            AddDescription("Icon glyphs, rounded pictures, tool strips, rich text and code highlighting.");

            AddSection("FluentIcon");
            var iconRow = AddRow();
            SymbolRegular[] samples =
            {
                SymbolRegular.Home24, SymbolRegular.Play24, SymbolRegular.Code24,
                SymbolRegular.Calendar24, SymbolRegular.Settings24, SymbolRegular.Alert24
            };
            foreach (SymbolRegular symbol in samples)
            {
                iconRow.Controls.Add(WithMargin(new FluentIcon { Symbol = symbol, Size = new Size(24, 24) }, 12));
            }

            iconRow.Controls.Add(WithMargin(new FluentIcon { Glyph = "★", Size = new Size(24, 24), ForeColor = Color.Goldenrod }, 12));

            AddSection("FluentPictureBox");
            var picRow = AddRow();
            picRow.Controls.Add(WithMargin(new FluentPictureBox { Image = MakeSwatch(Color.SteelBlue), CornerRadius = 12, Size = new Size(96, 72) }, 12));
            picRow.Controls.Add(WithMargin(new FluentPictureBox { Image = MakeSwatch(Color.DarkOrange), CornerRadius = 0, Size = new Size(96, 72) }, 12));

            AddSection("FluentToolStrip");
            var strip = new FluentToolStrip();
            strip.Items.Add(new ToolStripButton("New"));
            strip.Items.Add(new ToolStripButton("Open"));
            strip.Items.Add(new ToolStripSeparator());
            var extra = new ToolStripDropDownButton("More");
            extra.DropDownItems.Add(new ToolStripMenuItem("Rename"));
            extra.DropDownItems.Add(new ToolStripMenuItem("Delete"));
            strip.Items.Add(extra);
            var stripRow = AddRow();
            strip.AutoSize = false;
            strip.Width = 420;
            stripRow.Controls.Add(strip);

            AddSection("FluentRichTextBox + FluentScrollBar");
            var mixRow = AddRow();
            var rich = WithMargin(new FluentRichTextBox { Size = new Size(320, 150) }, 12);
            rich.AppendText("Rich text shell with native editing and Fluent chrome.\nSecond line for scrolling.");
            var list = new ListBox
            {
                Size = new Size(180, 150),
                BorderStyle = BorderStyle.None,
                BackColor = ApplicationThemeManager.Palette.SolidBackgroundFillTertiary,
                ForeColor = ApplicationThemeManager.Palette.TextFillPrimary,
                DrawMode = DrawMode.OwnerDrawFixed,
                ItemHeight = 24
            };
            for (int i = 0; i < 24; i++)
            {
                list.Items.Add("Row " + (i + 1));
            }

            var scrollbar = new FluentScrollBar
            {
                Orientation = Orientation.Vertical,
                Size = new Size(14, 150),
                Minimum = 0,
                Maximum = 24,
                SmallChange = 1,
                LargeChange = 6
            };
            list.DrawItem += (s, e) =>
            {
                e.DrawBackground();
                TextRenderer.DrawText(e.Graphics, list.Items[e.Index].ToString(), list.Font,
                    new Rectangle(e.Bounds.X, e.Bounds.Y, e.Bounds.Width - 20, e.Bounds.Height),
                    ApplicationThemeManager.Palette.TextFillPrimary,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
            };
            list.SelectedIndexChanged += (s, e) => scrollbar.Value = Math.Min(list.SelectedIndex, Math.Max(0, scrollbar.Maximum - scrollbar.LargeChange + 1));
            scrollbar.ValueChanged += (s, e) =>
            {
                if (list.TopIndex != scrollbar.Value)
                {
                    list.TopIndex = scrollbar.Value;
                }
            };
            list.MouseWheel += (s, e) => scrollbar.Value -= Math.Sign(e.Delta);
            var holder = new SurfacePanel { Size = new Size(180 + 14 + 4, 150) };
            list.Location = new Point(0, 0);
            scrollbar.Location = new Point(184, 0);
            holder.Controls.Add(list);
            holder.Controls.Add(scrollbar);
            mixRow.Controls.Add(rich);
            mixRow.Controls.Add(WithMargin(holder, 0));

            AddSection("FluentCodeBlock");
            var codeRow = AddRow();
            var code = WithMargin(new FluentCodeBlock
            {
                Size = new Size(420, 170),
                Language = SyntaxLanguage.CSharp,
                Code = "using System;\n\nclass Demo\n{\n    // commented line\n    static void Main()\n    {\n        var msg = \"hello, fluent\";\n        Console.WriteLine(msg); // print\n    }\n}"
            }, 12);
            var xaml = new FluentCodeBlock
            {
                Size = new Size(320, 170),
                Language = SyntaxLanguage.Xaml,
                Code = "<Window Title=\"Sample\">\n  <!-- a comment -->\n  <Grid Margin=\"12\">\n    <Button Content=\"Go\" Click=\"OnGo\" />\n  </Grid>\n</Window>"
            };
            codeRow.Controls.Add(code);
            codeRow.Controls.Add(xaml);
        }

        private static Bitmap MakeSwatch(Color color)
        {
            var bmp = new Bitmap(96, 72);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(color);
                using (var pen = new Pen(Color.White, 3))
                {
                    g.DrawEllipse(pen, 18, 12, 60, 48);
                }
            }

            return bmp;
        }
    }

    /// <summary>FluentTabView / FluentGridView / FluentTreeGrid / layout panels / toast / tray / title bar.</summary>
    public class LayoutsPage : GalleryPage
    {
        private FluentNotifyIcon _tray;

        public LayoutsPage() : base("Layouts")
        {
            AddDescription("Tabs, tile and tree grids, panel layouts, toasts, tray icon and a custom title bar window.");

            AddSection("FluentTabView");
            var tabRow = AddRow();
            var tabs = WithMargin(new FluentTabView { Size = new Size(430, 200) }, 12);
            var overview = tabs.AddTab(null, "Overview");
            overview.Controls.Add(new FluentLabel { Text = "Drag tabs to reorder, click + to add, × to close.", AutoSize = true, Location = new Point(14, 14) });
            var details = tabs.AddTab(null, "Details");
            details.Controls.Add(new FluentLabel { Text = "Second page content.", AutoSize = true, Location = new Point(14, 14) });
            tabs.AddPage(new FluentTabViewPage { Text = "Third" });
            tabRow.Controls.Add(tabs);

            var tabNotes = WithMargin(new FluentLabel
            {
                Text = "TabAdding / TabClosing are cancelable; CanReorderTabs and CanAddTabs can disable interaction.",
                AutoSize = true
            });
            tabRow.Controls.Add(tabNotes);

            AddSection("FluentGridView");
            var gridRow = AddRow();
            var grid = WithMargin(new FluentGridView { Size = new Size(300, 210), TileSize = new Size(88, 72) }, 12);
            var apps = new List<string>();
            for (int i = 1; i <= 24; i++)
            {
                apps.Add("App " + i);
            }

            grid.Items = apps;
            grid.SelectedIndex = 2;
            var gridState = new FluentLabel { Text = "Selected: App 3", AutoSize = true, MaximumSize = new Size(220, 0) };
            grid.ItemClicked += (s, e) => gridState.Text = "Selected: " + e.Item;
            gridRow.Controls.Add(grid);
            gridRow.Controls.Add(gridState);

            AddSection("FluentTreeGrid");
            var treeRow = AddRow();
            var tree = WithMargin(new FluentTreeGrid { Size = new Size(430, 220) }, 12);
            tree.Columns.Add(new FluentTreeGridColumn("Name", 200));
            tree.Columns.Add(new FluentTreeGridColumn("Type", 0));
            tree.Columns.Add(new FluentTreeGridColumn("Size", 80));
            var src = new FluentTreeGridNode("src") { IsExpanded = true };
            src.Cells.Add("folder");
            src.Cells.Add("—");
            var controls = new FluentTreeGridNode("Controls") { IsExpanded = true };
            controls.Cells.Add("folder");
            controls.Cells.Add("—");
            controls.Nodes.Add(Node("FluentTabView.cs", "code", "21 KB"));
            controls.Nodes.Add(Node("FluentGridView.cs", "code", "17 KB"));
            controls.Nodes.Add(Node("FluentTreeGrid.cs", "code", "14 KB"));
            src.Nodes.Add(controls);
            src.Nodes.Add(Node("Program.cs", "code", "1 KB"));
            var docs = new FluentTreeGridNode("docs");
            docs.Cells.Add("folder");
            docs.Cells.Add("—");
            docs.Nodes.Add(Node("readme.md", "doc", "9 KB"));
            docs.Nodes.Add(Node("window.md", "doc", "6 KB"));
            src.Nodes.Add(docs);
            tree.RootNodes.Add(src);
            tree.RootNodes.Add(Node("WinformUi.slnx", "solution", "2 KB"));
            tree.Rebuild();
            var treeState = new FluentLabel { Text = "Selection via ↑/↓, expand via ←/→", AutoSize = true };
            tree.SelectedNodeChanged += (s, e) => treeState.Text = tree.SelectedNode == null ? "no selection" : "Selected: " + tree.SelectedNode.Text;
            treeRow.Controls.Add(tree);
            treeRow.Controls.Add(treeState);

            AddSection("FluentWrapPanel / FluentUniformGrid");
            var panelRow = AddRow();
            var wrap = WithMargin(new FluentWrapPanel { Size = new Size(240, 96) }, 12);
            for (int i = 0; i < 5; i++)
            {
                wrap.Controls.Add(new FluentButton { Text = "Wrap " + i, AutoSize = true });
            }

            var uniform = new FluentUniformGrid { Size = new Size(240, 96), Columns = 3 };
            for (int i = 0; i < 6; i++)
            {
                uniform.Controls.Add(new FluentLabel { Text = "Cell " + (i + 1), AutoSize = false, TextAlign = ContentAlignment.MiddleCenter });
            }

            panelRow.Controls.Add(wrap);
            panelRow.Controls.Add(uniform);

            AddSection("Toast / Tray / Title bar");
            var notifyRow = AddRow();
            var toastBtn = WithMargin(new FluentButton { Text = "Show toast", Appearance = ControlAppearance.Primary, AutoSize = true }, 12);
            toastBtn.Click += (s, e) => FluentToast.Show("Saved", "Your layout was saved to the gallery.", InfoBarSeverity.Success, TimeSpan.FromSeconds(4));
            var warnToast = WithMargin(new FluentButton { Text = "Warning toast", Appearance = ControlAppearance.Caution, AutoSize = true }, 12);
            warnToast.Click += (s, e) => FluentToast.Show("Offline", "Changes will sync once the connection returns.", InfoBarSeverity.Warning);
            var trayBtn = WithMargin(new FluentButton { Text = "Register tray icon", AutoSize = true }, 12);
            trayBtn.Click += (s, e) =>
            {
                if (_tray == null)
                {
                    _tray = new FluentNotifyIcon
                    {
                        TooltipText = "WinformUi gallery tray",
                        Icon = SystemIcons.Application
                    };
                    _tray.LeftClick += (sender, args) => FluentToast.Show("Tray", "Left click received.", InfoBarSeverity.Informational);
                }

                _tray.Register();
                _tray.ShowBalloonTip(TimeSpan.FromSeconds(3), "WinformUi", "Tray icon registered — click it in the taskbar corner.");
                trayBtn.Text = "Show balloon again";
            };
            var barBtn = WithMargin(new FluentButton { Text = "Open title bar window", AutoSize = true }, 12);
            barBtn.Click += (s, e) => new TitleBarDemoForm().Show();
            notifyRow.Controls.AddRange(new Control[] { toastBtn, warnToast, trayBtn, barBtn });

            AddSection("FluentClientAreaBorder");
            var borderNote = AddDescription("FluentClientAreaBorder is a panel that pads its form while a borderless window is maximized; it backs the title bar window above.");
            borderNote.Margin = new Padding(0, 0, 0, 4);
        }

        private static FluentTreeGridNode Node(string name, string type, string size)
        {
            var node = new FluentTreeGridNode(name);
            node.Cells.Add(type);
            node.Cells.Add(size);
            return node;
        }
    }

    /// <summary>Demo form for FluentTitleBar (borderless, custom chrome).</summary>
    public class TitleBarDemoForm : Form
    {
        public TitleBarDemoForm()
        {
            Text = "Title bar demo";
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(560, 360);
            ShowInTaskbar = true;
            MinimumSize = new Size(360, 240);
            Font = FluentTypography.Body;
            BackColor = ApplicationThemeManager.Palette.ApplicationBackground;

            var bar = new FluentTitleBar { Title = "Title bar demo", ShowHelpButton = true };
            bar.HelpButtonClick += (s, e) => FluentToast.Show("Help", "Press F1 in a real app.", InfoBarSeverity.Informational);

            var body = new FluentClientAreaBorder { Dock = DockStyle.Fill };
            var label = new FluentLabel
            {
                Text = "Drag me · double-click to maximize · right-click for the system menu · edges resize.",
                AutoSize = true,
                Location = new Point(24, 24)
            };
            body.Controls.Add(label);
            // Fill docks last in reverse order: add body first, then the top-docked bar.
            Controls.Add(body);
            Controls.Add(bar);
        }
    }
}
