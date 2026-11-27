using System;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace WinformUi.Theming
{
    /// <summary>
    /// Loads the embedded FluentSystemIcons fonts;
    /// SymbolRegular/SymbolFilled codepoints only resolve against these families.
    /// </summary>
    internal static class FluentIconFonts
    {
        private static PrivateFontCollection _collection;
        private static IntPtr _regularMemory;
        private static IntPtr _filledMemory;

        internal static FontFamily RegularFamily { get; private set; }

        internal static FontFamily FilledFamily { get; private set; }

        static FluentIconFonts()
        {
            try
            {
                Assembly assembly = typeof(FluentIconFonts).Assembly;
                _collection = new PrivateFontCollection();
                RegularFamily = Add(assembly, "WinformUi.Resources.Fonts.FluentSystemIcons-Regular.ttf", ref _regularMemory);
                FilledFamily = Add(assembly, "WinformUi.Resources.Fonts.FluentSystemIcons-Filled.ttf", ref _filledMemory);
            }
            catch
            {
                _collection = null;
                RegularFamily = null;
                FilledFamily = null;
            }
        }

        private static FontFamily Add(Assembly assembly, string resourceName, ref IntPtr memory)
        {
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    return null;
                }

                var bytes = new byte[stream.Length];
                int read = 0;
                while (read < bytes.Length)
                {
                    int n = stream.Read(bytes, read, bytes.Length - read);
                    if (n == 0)
                    {
                        break;
                    }

                    read += n;
                }

                // AddMemoryFont requires the buffer to stay alive for the process lifetime.
                memory = Marshal.AllocHGlobal(bytes.Length);
                Marshal.Copy(bytes, 0, memory, bytes.Length);
                _collection.AddMemoryFont(memory, bytes.Length);
                return _collection.Families[_collection.Families.Length - 1];
            }
        }
    }
}