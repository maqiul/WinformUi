using System;
using System.Windows.Forms;
using WinformUi.Theming;

namespace WinformUi.Gallery
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            ApplicationThemeManager.Apply(ApplicationTheme.System);
            var form = new MainForm();
            if (args.Length > 0)
            {
                form.Shown += (s, e) => form.SelectPage(args[0]);
            }

            Application.Run(form);
        }
    }
}