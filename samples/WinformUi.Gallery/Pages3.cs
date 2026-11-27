using System;
using System.Drawing;
using System.Windows.Forms;
using WinformUi.Controls;
using WinformUi.Theming;

namespace WinformUi.Gallery
{
    public class CollectionsPage : GalleryPage
    {
        public CollectionsPage() : base("Collections")
        {
            AddSection("List box");
            var listRow = AddRow();
            var list = new FluentListBox { Size = new Size(240, 190) };
            foreach (string s in new[] { "Azure", "Bing", "Copilot", "Dynamics", "Edge", "GitHub", "LinkedIn", "Office", "OneDrive", "Outlook", "Surface", "Teams", "Visual Studio", "Windows", "Xbox" })
            {
                list.Items.Add(s);
            }
            list.SelectedIndex = 0;
            var listState = WithMargin(new FluentLabel { Text = "Azure", AutoSize = true });
            list.SelectedIndexChanged += (s, e) => listState.Text = list.SelectedItem != null ? list.SelectedItem.ToString() : "(none)";
            listRow.Controls.Add(WithMargin(list, 16));
            listRow.Controls.Add(listState);

            var multi = new FluentListBox { Size = new Size(240, 190), MultiSelect = true };
            foreach (string s in new[] { "Red", "Green", "Blue", "Cyan", "Magenta", "Yellow" })
            {
                multi.Items.Add(s);
            }
            var multiState = WithMargin(new FluentLabel { Text = "Multi-select: click items", AutoSize = true });
            multi.SelectedIndexChanged += (s, e) => multiState.Text = "Selected: " + string.Join(", ", System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(multi.SelectedIndices, i => multi.Items[i].ToString())));
            listRow.Controls.Add(WithMargin(multi, 16));
            listRow.Controls.Add(multiState);

            AddSection("Checked list box");
            var checkedRow = AddRow();
            var checkedList = new FluentCheckedListBox { Size = new Size(300, 190), ThreeState = true };
            foreach (string s in new[] { "Designer", "Toolbox", "Data binding", "Dark theme", "High DPI", "NuGet" })
            {
                checkedList.Items.Add(s);
            }
            checkedList.SetItemChecked(0, true);
            checkedList.SetItemCheckState(1, CheckState.Indeterminate);
            var checkedState = WithMargin(new FluentLabel { Text = "Checked: 0", AutoSize = true });
            checkedList.ItemCheck += (s, e) => checkedState.Text = "Checked: " + string.Join(", ", checkedList.CheckedIndices);
            checkedRow.Controls.Add(WithMargin(checkedList, 16));
            checkedRow.Controls.Add(checkedState);

            AddSection("List view (details)");
            var lvRow = AddRow();
            var listView = new FluentListView { Size = new Size(560, 200), DetailsView = true };
            listView.Columns.AddRange(new[] { "Name", "Publisher", "Year" });
            listView.SubItemSelector = item =>
            {
                var app = (string[])item;
                return app;
            };
            listView.Items.Add(new[] { "Windows Terminal", "Microsoft", "2019" });
            listView.Items.Add(new[] { "PowerToys", "Microsoft", "2019" });
            listView.Items.Add(new[] { "Visual Studio Code", "Microsoft", "2015" });
            listView.Items.Add(new[] { "Paint.NET", "dotPDN", "2004" });
            listView.Items.Add(new[] { "ShareX", "ShareX Team", "2007" });
            listView.Items.Add(new[] { "7-Zip", "Igor Pavlov", "1999" });
            listView.SelectedIndex = 0;
            lvRow.Controls.Add(WithMargin(listView));

            AddSection("Tree view");
            var treeRow = AddRow();
            var tree = new FluentTreeView { Size = new Size(300, 220) };
            var work = new FluentTreeNode("Work documents") { Icon = SymbolRegular.Folder24, IsExpanded = true };
            work.Nodes.Add(new FluentTreeNode("Quarterly report.docx") { Icon = SymbolRegular.Document24 });
            work.Nodes.Add(new FluentTreeNode("Budget.xlsx") { Icon = SymbolRegular.Document24 });
            var pictures = new FluentTreeNode("Pictures") { Icon = SymbolRegular.Image24, IsExpanded = true };
            var vacation = new FluentTreeNode("Vacation 2026") { Icon = SymbolRegular.Folder24 };
            vacation.Nodes.Add(new FluentTreeNode("beach.png") { Icon = SymbolRegular.Image24 });
            vacation.Nodes.Add(new FluentTreeNode("sunset.png") { Icon = SymbolRegular.Image24 });
            pictures.Nodes.Add(vacation);
            pictures.Nodes.Add(new FluentTreeNode("profile.jpg") { Icon = SymbolRegular.Image24 });
            tree.RootNodes.Add(work);
            tree.RootNodes.Add(pictures);
            tree.RootNodes.Add(new FluentTreeNode("Music") { Icon = SymbolRegular.MusicNote224 });
            tree.Rebuild();
            var treeState = WithMargin(new FluentLabel { Text = "Select a node", AutoSize = true });
            tree.SelectedNodeChanged += (s, e) => treeState.Text = tree.SelectedNode != null ? tree.SelectedNode.Text : "(none)";
            treeRow.Controls.Add(WithMargin(tree, 16));
            treeRow.Controls.Add(treeState);

            AddSection("Data grid view");
            var gridRow = AddRow();
            var grid = new FluentDataGridView { Size = new Size(640, 220) };
            grid.Columns.Add("Product", "Product");
            grid.Columns.Add("Category", "Category");
            grid.Columns.Add("Price", "Price");
            grid.Columns.Add("Stock", "Stock");
            grid.Rows.Add("Surface Pro", "Hardware", "$999", "128");
            grid.Rows.Add("Surface Laptop", "Hardware", "$1299", "64");
            grid.Rows.Add("Xbox Controller", "Gaming", "$59", "512");
            grid.Rows.Add("Arc Mouse", "Accessory", "$79", "230");
            grid.Rows.Add("Modern Webcam", "Accessory", "$99", "87");
            grid.Rows.Add("USB-C Dock", "Accessory", "$199", "42");
            gridRow.Controls.Add(WithMargin(grid));
        }
    }

    public class ContainersPage : GalleryPage
    {
        public ContainersPage() : base("Containers")
        {
            AddSection("Cards");
            var cardRow = AddRow();

            // Stack body below the title's measured bounds: fixed offsets clip
            // the first body line behind the title label at higher DPI scales.
            void FillCard(FluentCard c, string title, string body, int x)
            {
                var titleLabel = new FluentLabel { Text = title, AutoSize = true, UseSecondaryColor = false, Font = FluentTypography.Subtitle, Location = new Point(x, 12) };
                var bodyLabel = new FluentLabel { Text = body, AutoSize = true, MaximumSize = new Size(c.Width - 2 * x - 16, 0) };
                c.Controls.Add(titleLabel);
                c.Controls.Add(bodyLabel);
                bodyLabel.Location = new Point(x, titleLabel.Bottom + 6);
                c.Height = bodyLabel.Bottom + 12;
            }

            var card = new FluentCard { Size = new Size(280, 110), Padding = new Padding(16) };
            FillCard(card, "Card", "A simple content surface with a border and rounded corners.", 16);
            cardRow.Controls.Add(WithMargin(card, 12));

            var actionCard = new FluentCardAction { Size = new Size(280, 110) };
            FillCard(actionCard, "Card action", "Hover and press me — I respond like a button.", 16);
            var actionState = WithMargin(new FluentLabel { Text = "Clicks: 0", AutoSize = true });
            int clicks = 0;
            actionCard.Click += (s, e) => actionState.Text = "Clicks: " + (++clicks);
            cardRow.Controls.Add(WithMargin(actionCard, 12));
            cardRow.Controls.Add(actionState);

            var colorRow = AddRow();
            var colorCard = new FluentCardColor { Size = new Size(280, 90), AccentColor = Color.FromArgb(0x00, 0x78, 0xD4) };
            FillCard(colorCard, "Card color", "Accent strip highlights the card category.", 20);
            colorRow.Controls.Add(WithMargin(colorCard, 12));
            var colorCard2 = new FluentCardColor { Size = new Size(280, 90), AccentColor = Color.FromArgb(0x0F, 0x7B, 0x0F) };
            FillCard(colorCard2, "Success", "Different accent colors categorize content.", 20);
            colorRow.Controls.Add(WithMargin(colorCard2, 12));

            AddSection("Expander");
            var expRow = AddRow();
            var expander = new FluentExpander { Header = "Settings group", HeaderIcon = SymbolRegular.Settings24, Width = 420 };
            expander.ContentPanel.Padding = new Padding(12);
            expander.ContentPanel.Controls.Add(new FluentToggleSwitch { OnContent = "Enable feature", OffContent = "Enable feature", Checked = true, Width = 180, Location = new Point(12, 8) });
            expander.ContentPanel.Controls.Add(new FluentCheckBox { Text = "Show advanced options", Location = new Point(12, 44) });
            expRow.Controls.Add(WithMargin(expander, 16));

            var cardExpander = new FluentCardExpander { Header = "Card expander", HeaderIcon = SymbolRegular.Info24, Width = 420 };
            cardExpander.ContentPanel.Padding = new Padding(12);
            cardExpander.ContentPanel.Controls.Add(new FluentLabel
            {
                Text = "A FluentCard styled expander — the header and content share one card surface.",
                AutoSize = true,
                MaximumSize = new Size(390, 0),
                Location = new Point(12, 8)
            });
            expRow.Controls.Add(WithMargin(cardExpander));

            AddSection("Tab control");
            var tabRow = AddRow();
            var tabs = new FluentTabControl { Size = new Size(560, 220) };
            var tab1 = new SurfacePanel { Dock = DockStyle.Fill };
            tab1.Controls.Add(new FluentLabel { Text = "First tab content. Tabs merge into the content surface like WinUI.", AutoSize = true, Location = new Point(16, 16) });
            var tab2 = new SurfacePanel { Dock = DockStyle.Fill };
            tab2.Controls.Add(new FluentButton { Text = "A button on tab two", Appearance = ControlAppearance.Primary, Location = new Point(16, 16), AutoSize = true });
            var tab3 = new SurfacePanel { Dock = DockStyle.Fill };
            tab3.Controls.Add(new FluentTextBox { Width = 240, PlaceholderText = "Type on tab three...", Location = new Point(16, 16) });
            tabs.AddTab("Overview", tab1);
            tabs.AddTab("Actions", tab2);
            tabs.AddTab("Input", tab3);
            tabRow.Controls.Add(WithMargin(tabs));

            AddSection("Split container");
            var splitRow = AddRow();
            var split = new FluentSplitContainer
            {
                Size = new Size(640, 240),
                SplitterWidth = 8,
                Panel1MinSize = 120,
                Panel2MinSize = 120
            };
            split.Panel1.Controls.Add(new FluentLabel { Text = "Panel 1", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter });
            split.Panel2.Controls.Add(new FluentLabel { Text = "Panel 2", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter });
            split.SetSplitterRatio(0.4);
            var splitState = WithMargin(new FluentLabel { Text = "Ratio: 40%", AutoSize = true });
            split.SplitterMoved += (s, e) => splitState.Text = "Ratio: " + (int)(split.SplitterRatio * 100) + "%";
            splitRow.Controls.Add(WithMargin(split, 16));
            splitRow.Controls.Add(splitState);

            AddSection("Breadcrumb bar");
            var crumbRow = AddRow();
            var crumbs = new FluentBreadcrumbBar { Width = 420 };
            crumbs.SetPath("Home", "Documents", "Projects", "WinformUi");
            var crumbState = WithMargin(new FluentLabel { Text = "Click a segment", AutoSize = true });
            crumbs.ItemClicked += (s, i) => crumbState.Text = "Navigated to: " + crumbs.Items[i];
            crumbRow.Controls.AddRange(new Control[] { crumbs, crumbState });
        }
    }
}