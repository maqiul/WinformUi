using System;
using System.Drawing;
using System.Windows.Forms;
using WinformUi.Controls;
using WinformUi.Theming;

namespace WinformUi.Gallery
{
    public class DialogsPage : GalleryPage
    {
        private readonly FluentErrorProvider _errorProvider = new FluentErrorProvider
        {
            BlinkStyle = System.Windows.Forms.ErrorBlinkStyle.BlinkIfDifferentError
        };

        public DialogsPage() : base("Dialogs & Feedback")
        {
            AddSection("Message box");
            var msgRow = AddRow();
            var infoBtn = WithMargin(new FluentButton { Text = "Information", Icon = SymbolRegular.Info24, AutoSize = true });
            infoBtn.Click += (s, e) => FluentMessageBox.Show(FindForm(), "The operation completed successfully.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
            var warnBtn = WithMargin(new FluentButton { Text = "Warning", Icon = SymbolRegular.Warning24, AutoSize = true, Appearance = ControlAppearance.Caution });
            warnBtn.Click += (s, e) => FluentMessageBox.Show(FindForm(), "Disk space is running low.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            var errBtn = WithMargin(new FluentButton { Text = "Error", Icon = SymbolRegular.ErrorCircle24, AutoSize = true, Appearance = ControlAppearance.Danger });
            errBtn.Click += (s, e) => FluentMessageBox.Show(FindForm(), "The file could not be opened.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            var askBtn = WithMargin(new FluentButton { Text = "Question", Icon = SymbolRegular.QuestionCircle24, AutoSize = true });
            askBtn.Click += (s, e) =>
            {
                DialogResult r = FluentMessageBox.Show(FindForm(), "Save changes before closing?", "Confirm", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                FluentSnackbar.Show(FindForm() as Form, "Message box result", r.ToString());
            };
            msgRow.Controls.AddRange(new Control[] { infoBtn, warnBtn, errBtn, askBtn });

            AddSection("Error provider");
            var errorRow = AddRow();
            var errorInput = WithMargin(new FluentTextBox { PlaceholderText = "Required value", Width = 240 }, 12);
            var validate = WithMargin(new FluentButton { Text = "Validate", Appearance = ControlAppearance.Primary, AutoSize = true }, 12);
            var errorState = WithMargin(new FluentLabel { Text = "Click Validate with an empty value", AutoSize = true });
            validate.Click += (s, e) =>
            {
                bool valid = errorInput.Text.Trim().Length > 0;
                _errorProvider.SetError(errorInput, valid ? null : "A value is required.");
                errorState.Text = valid ? "Valid" : "Validation failed";
            };
            errorInput.TextChanged += (s, e) =>
            {
                if (errorInput.Text.Trim().Length > 0)
                {
                    _errorProvider.ClearError(errorInput);
                    errorState.Text = "Valid";
                }
            };
            errorRow.Controls.AddRange(new Control[] { errorInput, validate, errorState });

            AddSection("Content dialog");
            var dlgRow = AddRow();
            var dlgBtn = WithMargin(new FluentButton { Text = "Open content dialog", Appearance = ControlAppearance.Primary, AutoSize = true });
            var dlgState = WithMargin(new FluentLabel { Text = "No result yet", AutoSize = true });
            dlgBtn.Click += (s, e) =>
            {
                var content = new Panel { Size = new Size(360, 90), BackColor = Color.Transparent };
                content.Controls.Add(new FluentLabel { Text = "Enter a name for the new item:", AutoSize = true, Location = new Point(0, 0) });
                content.Controls.Add(new FluentTextBox { Width = 360, PlaceholderText = "Item name", Location = new Point(0, 28) });
                var dialog = new FluentContentDialog
                {
                    Title = "Create item",
                    Content = content,
                    PrimaryButtonText = "Create",
                    SecondaryButtonText = "Cancel"
                };
                dialog.PrimaryButtonClick += (s2, e2) => dlgState.Text = "Primary clicked";
                dialog.SecondaryButtonClick += (s2, e2) => dlgState.Text = "Secondary clicked";
                dialog.CloseButtonClick += (s2, e2) => dlgState.Text = "Closed";
                dialog.Show(FindForm() as Form);
            };
            dlgRow.Controls.AddRange(new Control[] { dlgBtn, dlgState });

            AddSection("Flyout & loading");
            var flyRow = AddRow();
            var flyBtn = WithMargin(new FluentButton { Text = "Show flyout", AutoSize = true });
            flyBtn.Click += (s, e) =>
            {
                var content = new Panel { Size = new Size(240, 96) };
                content.Controls.Add(new FluentLabel { Text = "Flyout", AutoSize = true, UseSecondaryColor = false, Font = FluentTypography.BodyStrong, Location = new Point(12, 10) });
                content.Controls.Add(new FluentLabel { Text = "Light-dismiss popup anchored to a control.", AutoSize = true, MaximumSize = new Size(216, 0), Location = new Point(12, 36) });
                new FluentFlyout { Content = content }.ShowAt(flyBtn);
            };
            var loadBtn = WithMargin(new FluentButton { Text = "Loading screen (2s)", AutoSize = true });
            loadBtn.Click += (s, e) =>
            {
                FluentLoadingScreen screen = FluentLoadingScreen.Show(FindForm() as Form, "Working on it...");
                var timer = new Timer { Interval = 2000 };
                timer.Tick += (s2, e2) =>
                {
                    timer.Stop();
                    timer.Dispose();
                    screen.HideScreen();
                };
                timer.Start();
            };
            var snackBtn = WithMargin(new FluentButton { Text = "Snackbar", AutoSize = true });
            snackBtn.Click += (s, e) => FluentSnackbar.Show(FindForm() as Form, "Message sent", "Your message was delivered.", "Undo");
            flyRow.Controls.AddRange(new Control[] { flyBtn, loadBtn, snackBtn });

            AddSection("Info bars");
            foreach (InfoBarSeverity severity in new[] { InfoBarSeverity.Informational, InfoBarSeverity.Success, InfoBarSeverity.Warning, InfoBarSeverity.Error })
            {
                var bar = new FluentInfoBar
                {
                    Severity = severity,
                    Title = severity.ToString(),
                    Message = "This is an informational message for the user.",
                    IsClosable = true,
                    Width = 640,
                    Margin = new Padding(0, 0, 0, 8)
                };
                Root.Controls.Add(bar);
            }

            AddSection("Badges");
            var badgeRow = AddRow();
            badgeRow.Controls.Add(WithMargin(new FluentBadge { Content = "New" }));
            badgeRow.Controls.Add(WithMargin(new FluentBadge { Content = "Preview", Appearance = ControlAppearance.Info }));
            badgeRow.Controls.Add(WithMargin(new FluentBadge { Content = "Beta", Appearance = ControlAppearance.Caution }));
            badgeRow.Controls.Add(WithMargin(new FluentBadge { Content = "Deprecated", Appearance = ControlAppearance.Danger }));
            badgeRow.Controls.Add(WithMargin(new FluentInfoBadge()));
            badgeRow.Controls.Add(WithMargin(new FluentInfoBadge { Value = 3 }));
            badgeRow.Controls.Add(WithMargin(new FluentInfoBadge { Value = 12, Severity = InfoBadgeSeverity.Critical }));
            badgeRow.Controls.Add(WithMargin(new FluentInfoBadge { Value = 8, Severity = InfoBadgeSeverity.Success }));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _errorProvider.Dispose();
            }

            base.Dispose(disposing);
        }
    }

    public class MenusPage : GalleryPage
    {
        public MenusPage() : base("Menus & ToolTips")
        {
            AddSection("Menu strip");
            var menu = new FluentMenuStrip { Width = 620, Height = 30, Dock = DockStyle.None };
            var file = new ToolStripMenuItem("&File");
            file.DropDownItems.Add(new ToolStripMenuItem("New", null, (s, e) => FluentSnackbar.Show(FindForm() as Form, "File", "New clicked")));
            file.DropDownItems.Add(new ToolStripMenuItem("Open..."));
            file.DropDownItems.Add(new ToolStripMenuItem("Save"));
            file.DropDownItems.Add(new ToolStripSeparator());
            var recent = new ToolStripMenuItem("Open recent");
            recent.DropDownItems.Add("design-spec.docx");
            recent.DropDownItems.Add("budget.xlsx");
            file.DropDownItems.Add(recent);
            file.DropDownItems.Add(new ToolStripSeparator());
            file.DropDownItems.Add(new ToolStripMenuItem("Exit"));
            var edit = new ToolStripMenuItem("&Edit");
            var cut = new ToolStripMenuItem("Cut") { ShortcutKeys = Keys.Control | Keys.X };
            var copy = new ToolStripMenuItem("Copy") { ShortcutKeys = Keys.Control | Keys.C };
            var paste = new ToolStripMenuItem("Paste") { ShortcutKeys = Keys.Control | Keys.V };
            edit.DropDownItems.AddRange(new ToolStripItem[] { cut, copy, paste });
            var view = new ToolStripMenuItem("&View");
            var zoom = new ToolStripMenuItem("Zoom") { Checked = true, CheckOnClick = true };
            var statusBar = new ToolStripMenuItem("Status bar") { Checked = true, CheckOnClick = true };
            view.DropDownItems.AddRange(new ToolStripItem[] { zoom, statusBar });
            menu.Items.AddRange(new ToolStripItem[] { file, edit, view });
            Root.Controls.Add(WithMargin(menu));

            AddSection("Context menu");
            var ctxRow = AddRow();
            var ctxTarget = new FluentCard { Size = new Size(320, 90) };
            ctxTarget.Controls.Add(new FluentLabel { Text = "Right-click anywhere on this card", AutoSize = true, Location = new Point(16, 34) });
            var ctxMenu = new FluentContextMenuStrip();
            ctxMenu.Items.Add(new ToolStripMenuItem("Refresh", null, (s, e) => FluentSnackbar.Show(FindForm() as Form, "Context menu", "Refresh clicked")));
            ctxMenu.Items.Add(new ToolStripMenuItem("Rename"));
            ctxMenu.Items.Add(new ToolStripSeparator());
            var sortMenu = new ToolStripMenuItem("Sort by");
            sortMenu.DropDownItems.Add("Name");
            sortMenu.DropDownItems.Add("Date modified");
            sortMenu.DropDownItems.Add("Size");
            ctxMenu.Items.Add(sortMenu);
            ctxMenu.Items.Add(new ToolStripSeparator());
            ctxMenu.Items.Add(new ToolStripMenuItem("Delete"));
            ctxTarget.ContextMenuStrip = ctxMenu;
            ctxRow.Controls.Add(WithMargin(ctxTarget));

            AddSection("Tool tip");
            var tipRow = AddRow();
            var toolTip = new FluentToolTip();
            var tipBtn1 = WithMargin(new FluentButton { Text = "Hover me", AutoSize = true });
            toolTip.SetToolTip(tipBtn1, "This is a Fluent-styled tool tip with a rounded flyout surface.");
            var tipBtn2 = WithMargin(new FluentButton { Text = "Me too", Appearance = ControlAppearance.Primary, AutoSize = true });
            toolTip.SetToolTip(tipBtn2, "Tool tips follow the current theme automatically.");
            tipRow.Controls.AddRange(new Control[] { tipBtn1, tipBtn2 });
        }
    }

    public class SettingsPage : GalleryPage
    {
        public SettingsPage() : base("Settings")
        {
            AddSection("Theme");
            AddDescription("Choose the application theme. System follows the Windows personalization setting.");
            var themeRow = AddRow();
            var light = WithMargin(new FluentRadioButton { Text = "Light", Checked = ApplicationThemeManager.CurrentTheme == ApplicationTheme.Light });
            var dark = WithMargin(new FluentRadioButton { Text = "Dark", Checked = ApplicationThemeManager.CurrentTheme == ApplicationTheme.Dark });
            var system = WithMargin(new FluentRadioButton { Text = "System", Checked = ApplicationThemeManager.CurrentTheme == ApplicationTheme.System });
            light.CheckedChanged += (s, e) => { if (light.Checked) ApplicationThemeManager.Apply(ApplicationTheme.Light); };
            dark.CheckedChanged += (s, e) => { if (dark.Checked) ApplicationThemeManager.Apply(ApplicationTheme.Dark); };
            system.CheckedChanged += (s, e) => { if (system.Checked) ApplicationThemeManager.Apply(ApplicationTheme.System); };
            themeRow.Controls.AddRange(new Control[] { light, dark, system });

            AddSection("Window backdrop");
            AddDescription("Mica and Acrylic require Windows 11 build 22621 or later; older systems fall back to a solid background.");
            var backdropRow = AddRow();
            var backdropCombo = new FluentComboBox { Width = 200 };
            backdropCombo.Items.Add("Mica");
            backdropCombo.Items.Add("Acrylic");
            backdropCombo.Items.Add("Tabbed");
            backdropCombo.Items.Add("None");
            backdropCombo.SelectedIndex = 0;
            backdropCombo.SelectedIndexChanged += (s, e) =>
            {
                var main = FindForm() as MainForm;
                if (main == null)
                {
                    return;
                }

                WindowBackdropType type;
                switch (backdropCombo.SelectedIndex)
                {
                    case 1: type = WindowBackdropType.Acrylic; break;
                    case 2: type = WindowBackdropType.Tabbed; break;
                    case 3: type = WindowBackdropType.None; break;
                    default: type = WindowBackdropType.Mica; break;
                }

                main.SetBackdrop(type);
            };
            backdropRow.Controls.Add(WithMargin(backdropCombo));

            AddSection("Accent color");
            AddDescription("Pick a custom accent color, or reset to the Windows system accent.");
            var accentRow = AddRow();
            var picker = new FluentColorPicker();
            picker.ColorChanged += (s, e) => ApplicationThemeManager.Apply(ApplicationThemeManager.CurrentTheme, picker.Color);
            accentRow.Controls.Add(WithMargin(picker, 16));
            var resetBtn = new FluentButton { Text = "Use system accent", AutoSize = true };
            resetBtn.Click += (s, e) => ApplicationThemeManager.RefreshSystemColors();
            accentRow.Controls.Add(WithMargin(resetBtn));

            AddSection("About");
            var about = new FluentCard { Size = new Size(640, 96), Margin = new Padding(0, 0, 0, 8) };
            about.Controls.Add(new FluentLabel { Text = "WinformUi Gallery", AutoSize = true, UseSecondaryColor = false, Font = FluentTypography.Subtitle, Location = new Point(16, 12) });
            about.Controls.Add(new FluentLabel { Text = "A Fluent Design control library for Windows Forms. Targets net46, net6.0-windows, net8.0-windows and net10.0-windows.", AutoSize = true, MaximumSize = new Size(600, 0), Location = new Point(16, 46) });
            Root.Controls.Add(about);
        }
    }
}