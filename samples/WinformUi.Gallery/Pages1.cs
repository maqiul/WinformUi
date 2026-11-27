using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using WinformUi.Controls;
using WinformUi.Theming;

namespace WinformUi.Gallery
{
    public class HomePage : GalleryPage
    {
        public HomePage() : base("WinformUi")
        {
            AddDescription("A Fluent Design control library for Windows Forms. " +
                "All controls are owner-drawn with GDI+, follow the Windows light/dark theme and system accent color, " +
                "and support .NET Framework 4.6+, .NET 6, .NET 8 and .NET 10.");

            AddSection("Quick theme switch");
            var row = AddRow();
            var light = WithMargin(new FluentButton { Text = "Light", Icon = SymbolRegular.WeatherSunny24 });
            var dark = WithMargin(new FluentButton { Text = "Dark", Icon = SymbolRegular.WeatherMoon24 });
            var system = WithMargin(new FluentButton { Text = "System", Appearance = ControlAppearance.Primary });
            light.Click += (s, e) => ApplicationThemeManager.Apply(ApplicationTheme.Light);
            dark.Click += (s, e) => ApplicationThemeManager.Apply(ApplicationTheme.Dark);
            system.Click += (s, e) => ApplicationThemeManager.Apply(ApplicationTheme.System);
            row.Controls.AddRange(new Control[] { light, dark, system });

            AddSection("Highlights");
            var cards = AddRow();
            cards.Controls.Add(MakeCard("FluentWindow", "Mica / Acrylic system backdrop on Windows 11 22H2+, themed native caption, graceful solid fallback."));
            cards.Controls.Add(MakeCard("Theme system", "Full Fluent token palette for light & dark, system accent derivation, live theme switching."));
            cards.Controls.Add(MakeCard("56 controls", "Buttons, inputs, pickers, navigation, collections, dialogs, badges and more."));

            AddSection("Try it");
            var tryRow = AddRow();
            var snackbar = WithMargin(new FluentButton { Text = "Show snackbar", Appearance = ControlAppearance.Primary });
            snackbar.Click += (s, e) => FluentSnackbar.Show(FindForm(), "Hello from WinformUi", "This is a Fluent snackbar notification.", "Undo");
            var dialog = WithMargin(new FluentButton { Text = "Message box" });
            dialog.Click += (s, e) => FluentMessageBox.Show(FindForm(), "WinformUi renders Fluent dialogs with pure Windows Forms.", "FluentMessageBox", MessageBoxButtons.OKCancel, MessageBoxIcon.Information);
            tryRow.Controls.AddRange(new Control[] { snackbar, dialog });
        }

        private static FluentCard MakeCard(string title, string body)
        {
            var card = new FluentCard { Size = new Size(280, 130), Margin = new Padding(0, 0, 12, 12) };
            var titleLabel = new FluentLabel { Text = title, Font = FluentTypography.BodyStrong, UseSecondaryColor = false, AutoSize = true, Location = new Point(14, 12) };
            var bodyLabel = new FluentLabel { Text = body, AutoSize = true, MaximumSize = new Size(252, 0), Location = new Point(14, 38) };
            card.Controls.Add(titleLabel);
            card.Controls.Add(bodyLabel);
            return card;
        }
    }

    public class ButtonsPage : GalleryPage
    {
        public ButtonsPage() : base("Buttons")
        {
            AddSection("Button appearances");
            var row = AddRow();
            foreach (ControlAppearance appearance in new[] { ControlAppearance.Secondary, ControlAppearance.Primary, ControlAppearance.Info, ControlAppearance.Success, ControlAppearance.Caution, ControlAppearance.Danger, ControlAppearance.Transparent })
            {
                row.Controls.Add(WithMargin(new FluentButton { Text = appearance.ToString(), Appearance = appearance }));
            }

            AddSection("With icons");
            var iconRow = AddRow();
            iconRow.Controls.Add(WithMargin(new FluentButton { Text = "Save", Icon = SymbolRegular.Save24, Appearance = ControlAppearance.Primary }));
            iconRow.Controls.Add(WithMargin(new FluentButton { Text = "Delete", Icon = SymbolRegular.Delete24, Appearance = ControlAppearance.Danger }));
            iconRow.Controls.Add(WithMargin(new FluentButton { Text = "Settings", Icon = SymbolRegular.Settings24 }));
            iconRow.Controls.Add(WithMargin(new FluentButton { Text = "Disabled", Icon = SymbolRegular.Prohibited24, Enabled = false }));

            AddSection("Toggle & hyperlink");
            var toggleRow = AddRow();
            var toggle = WithMargin(new FluentToggleButton { Text = "Toggle me", Icon = SymbolRegular.Pin24 });
            var toggleState = WithMargin(new FluentLabel { Text = "Checked: False", AutoSize = true });
            toggle.CheckedChanged += (s, e) => toggleState.Text = "Checked: " + toggle.Checked;
            var link = WithMargin(new FluentHyperlinkButton { Text = "About WinformUi" });
            link.Click += (s, e) => FluentSnackbar.Show(FindForm() as Form, "WinformUi", "Fluent Design controls for Windows Forms.");
            toggleRow.Controls.AddRange(new Control[] { toggle, toggleState, link });

            AddSection("Split & drop-down");
            var splitRow = AddRow();
            var menu = new FluentContextMenuStrip();
            menu.Items.Add("Option one");
            menu.Items.Add("Option two");
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Option three");

            var split = WithMargin(new FluentSplitButton { Text = "Split button", Appearance = ControlAppearance.Primary, Flyout = menu, Size = new Size(160, 32) });
            var drop = WithMargin(new FluentDropDownButton { Text = "Drop-down", Flyout = menu, Size = new Size(150, 32) });
            var splitState = WithMargin(new FluentLabel { Text = "Click the main area", AutoSize = true });
            split.Click += (s, e) => splitState.Text = "Primary action clicked";
            splitRow.Controls.AddRange(new Control[] { split, drop, splitState });
        }
    }

    public class InputsPage : GalleryPage
    {
        public InputsPage() : base("Inputs")
        {
            AddSection("Text");
            var textRow = AddRow();
            textRow.Controls.Add(WithMargin(new FluentTextBox { PlaceholderText = "Standard text box", Width = 200 }));
            textRow.Controls.Add(WithMargin(new FluentTextBox { PlaceholderText = "With icon", Icon = SymbolRegular.Search24, Width = 200 }));
            textRow.Controls.Add(WithMargin(new FluentPasswordBox { PlaceholderText = "Password", Width = 190 }));
            textRow.Controls.Add(WithMargin(new FluentTextBox { PlaceholderText = "Disabled", Width = 150, Enabled = false }));

            AddSection("Masked input");
            var maskRow = AddRow();
            var dateMask = WithMargin(new FluentMaskedTextBox
            {
                Mask = "00/00/0000",
                PlaceholderText = "月/日/年",
                Icon = SymbolRegular.CalendarDay16,
                Width = 220
            }, 16);
            var phoneMask = WithMargin(new FluentMaskedTextBox
            {
                Mask = "(000) 000-0000",
                PlaceholderText = "电话号码",
                Icon = SymbolRegular.Call24,
                Width = 220
            }, 16);
            var maskState = WithMargin(new FluentLabel { Text = "Mask: 00/00/0000", AutoSize = true });
            dateMask.MaskInputRejected += (s, e) => maskState.Text = "请输入数字日期";
            phoneMask.MaskInputRejected += (s, e) => maskState.Text = "请输入数字电话号码";
            dateMask.TextChanged += (s, e) => maskState.Text = dateMask.MaskCompleted ? "日期输入完成" : "日期尚未输入完成";
            maskRow.Controls.AddRange(new Control[] { dateMask, phoneMask, maskState });

            AddSection("Number & suggestions");
            var numRow = AddRow();
            numRow.Controls.Add(WithMargin(new FluentNumberBox { Value = 42, Minimum = 0, Maximum = 1000, Width = 180 }));
            var suggest = WithMargin(new FluentAutoSuggestBox { PlaceholderText = "Type a fruit...", Width = 240 });
            suggest.Items = new List<string> { "Apple", "Apricot", "Banana", "Blueberry", "Cherry", "Grape", "Mango", "Orange", "Peach", "Pear", "Pineapple", "Strawberry" };
            numRow.Controls.Add(suggest);

            AddSection("Combo box");
            var comboRow = AddRow();
            var combo = WithMargin(new FluentComboBox { Width = 220 });
            combo.Items = new List<object> { "Segoe UI", "Segoe UI Variable", "Cascadia Code", "Consolas", "Arial" };
            combo.SelectedIndex = 0;
            var comboState = WithMargin(new FluentLabel { Text = "Selected: Segoe UI", AutoSize = true });
            combo.SelectedIndexChanged += (s, e) => comboState.Text = "Selected: " + combo.SelectedText;
            comboRow.Controls.AddRange(new Control[] { combo, comboState });

            AddSection("Check boxes");
            var checkRow = AddRow();
            checkRow.Controls.Add(WithMargin(new FluentCheckBox { Text = "Unchecked" }));
            checkRow.Controls.Add(WithMargin(new FluentCheckBox { Text = "Checked", Checked = true }));
            checkRow.Controls.Add(WithMargin(new FluentCheckBox { Text = "Three-state", ThreeState = true, CheckState = CheckState.Indeterminate }));
            checkRow.Controls.Add(WithMargin(new FluentCheckBox { Text = "Disabled", Checked = true, Enabled = false }));

            AddSection("Radio buttons");
            var radioRow = AddRow();
            radioRow.Controls.Add(WithMargin(new FluentRadioButton { Text = "Option A", Checked = true }));
            radioRow.Controls.Add(WithMargin(new FluentRadioButton { Text = "Option B" }));
            radioRow.Controls.Add(WithMargin(new FluentRadioButton { Text = "Option C" }));
            radioRow.Controls.Add(WithMargin(new FluentRadioButton { Text = "Disabled", Enabled = false }));

            AddSection("Toggle switches");
            var switchRow = AddRow();
            switchRow.Controls.Add(WithMargin(new FluentToggleSwitch { Checked = true }));
            switchRow.Controls.Add(WithMargin(new FluentToggleSwitch()));
            switchRow.Controls.Add(WithMargin(new FluentToggleSwitch { OnContent = "Enabled", OffContent = "Disabled", Checked = true, Width = 150 }));
            switchRow.Controls.Add(WithMargin(new FluentToggleSwitch { Enabled = false }));

            AddSection("Multi-line");
            var multiRow = AddRow();
            multiRow.Controls.Add(WithMargin(new FluentTextBox { Multiline = true, ScrollBars = ScrollBars.Vertical, Width = 460, Height = 96, PlaceholderText = "Multi-line text box..." }));
        }
    }
}