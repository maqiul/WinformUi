using System;
using System.Windows.Forms;

namespace WinformUi.Demo
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            var form = new MainForm();
            int tab;
            if (args.Length > 0 && int.TryParse(args[0], out tab))
            {
                form.Shown += (s, e) => form.SelectTab(tab);
            }

            Application.Run(form);
        }
    }
}