using System;
using System.IO;

namespace GamePackBuilder.GUI
{
    /// <summary>
    /// Определяет единую папку для данных приложения.
    /// Приоритет:
    ///   1. Портативный режим — папка "Data" рядом с exe (если туда можно писать).
    ///   2. Fallback — %LOCALAPPDATA%\GamePackBuilder (например, если exe в Program Files).
    ///
    /// При первом запуске в новом расположении файлы из старого места копируются автоматически.
    /// </summary>
    public static class AppPaths
    {
        private const string AppFolderName = "GamePackBuilder";
        private const string PortableDataFolderName = "Data";

        private static readonly Lazy<string> _dataDir = new(DetectDataDir, isThreadSafe: true);

        /// <summary>Папка, в которую приложение пишет все свои файлы.</summary>
        public static string DataDir => _dataDir.Value;

        /// <summary>Идёт ли работа в портативном режиме (данные рядом с exe).</summary>
        public static bool IsPortable { get; private set; }

        private static string DetectDataDir()
        {
            string exeDir = AppContext.BaseDirectory;
            string portableDir = Path.Combine(exeDir, PortableDataFolderName);

            if (CanWriteTo(portableDir))
            {
                IsPortable = true;
                MigrateIfNeeded(portableDir);
                return portableDir;
            }

            // Fallback — пользовательская папка.
            string local = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                AppFolderName);
            Directory.CreateDirectory(local);
            IsPortable = false;
            return local;
        }

        /// <summary>
        /// Проверяет, можно ли писать в папку. Создаёт её при необходимости.
        /// </summary>
        private static bool CanWriteTo(string dir)
        {
            try
            {
                Directory.CreateDirectory(dir);
                string probe = Path.Combine(dir, ".write_probe");
                File.WriteAllText(probe, "ok");
                File.Delete(probe);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Если в %LOCALAPPDATA%\GamePackBuilder уже есть файлы (старая версия),
        /// а в новой портативной папке их ещё нет — копируем.
        /// </summary>
        private static void MigrateIfNeeded(string targetDir)
        {
            try
            {
                string oldDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    AppFolderName);

                if (!Directory.Exists(oldDir)) return;

                string[] knownFiles =
                {
                    "config.json",
                    "systems.json",
                    "gameinfo.json",
                    "cache.json",
                    "debug.log"
                };

                foreach (var name in knownFiles)
                {
                    string oldFile = Path.Combine(oldDir, name);
                    string newFile = Path.Combine(targetDir, name);

                    if (File.Exists(oldFile) && !File.Exists(newFile))
                        File.Copy(oldFile, newFile);
                }
            }
            catch
            {
                // Миграция не критична — не падаем, если что-то не получилось.
            }
        }
    }
}