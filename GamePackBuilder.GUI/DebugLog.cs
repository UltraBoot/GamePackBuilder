using System;
using System.IO;

namespace GamePackBuilder.GUI
{
    /// <summary>
    /// Простой логгер для отладки. Пишет в %LOCALAPPDATA%\GamePackBuilder\debug.log
    /// и держит последние строки в памяти.
    /// </summary>
    internal static class DebugLog
    {
        private static readonly object Lock = new();

        public static string LogFilePath =>
    Path.Combine(AppPaths.DataDir, "debug.log");

        public static void Write(string message)
        {
            try
            {
                lock (Lock)
                {
                    var dir = Path.GetDirectoryName(LogFilePath)!;
                    Directory.CreateDirectory(dir);

                    string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}";
                    File.AppendAllText(LogFilePath, line);
                }
            }
            catch
            {
                // Логгер не должен ломать программу. Молча игнорируем ошибки.
            }
        }

        public static void Clear()
        {
            try
            {
                lock (Lock)
                {
                    if (File.Exists(LogFilePath))
                        File.Delete(LogFilePath);
                }
            }
            catch
            {
            }
        }

        public static string ReadAll()
        {
            try
            {
                lock (Lock)
                {
                    return File.Exists(LogFilePath) ? File.ReadAllText(LogFilePath) : "";
                }
            }
            catch (Exception ex)
            {
                return "Ошибка чтения лога: " + ex.Message;
            }
        }
    }
}