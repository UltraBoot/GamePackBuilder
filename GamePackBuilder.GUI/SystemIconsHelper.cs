using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace GamePackBuilder.GUI
{
    /// <summary>
    /// Достаёт системные иконки Windows (папки, файлы).
    /// </summary>
    internal static class SystemIconsHelper
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SHGetFileInfo(
            string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

        private const uint SHGFI_ICON = 0x100;
        private const uint SHGFI_SMALLICON = 0x1;
        private const uint SHGFI_USEFILEATTRIBUTES = 0x10;
        private const uint FILE_ATTRIBUTE_DIRECTORY = 0x10;

        /// <summary>Системная иконка папки (маленькая).</summary>
        public static Icon? GetFolderIcon()
        {
            var shinfo = new SHFILEINFO();
            IntPtr result = SHGetFileInfo(
                "folder", FILE_ATTRIBUTE_DIRECTORY,
                ref shinfo, (uint)Marshal.SizeOf(shinfo),
                SHGFI_ICON | SHGFI_SMALLICON | SHGFI_USEFILEATTRIBUTES);

            if (result == IntPtr.Zero || shinfo.hIcon == IntPtr.Zero) return null;

            // Копируем — оригинальный handle освобождать нельзя.
            using var tmp = Icon.FromHandle(shinfo.hIcon);
            return (Icon)tmp.Clone();
        }
    }
}