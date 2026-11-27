namespace WinformUi.Theming
{
    public enum ApplicationTheme
    {
        Light,
        Dark,
        /// <summary>Follow the Windows app theme setting.</summary>
        System
    }

    public enum WindowBackdropType
    {
        /// <summary>Solid application background, no system effect.</summary>
        None,
        /// <summary>Mica (Windows 11 22H2+).</summary>
        Mica,
        /// <summary>Acrylic (Windows 11 22H2+).</summary>
        Acrylic,
        /// <summary>Mica Alt / Tabbed (Windows 11 22H2+).</summary>
        Tabbed
    }
}