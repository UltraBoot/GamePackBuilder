using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GamePackBuilder.GUI
{
    /// <summary>Один файл-кандидат на перенос из подпапки системы в корень.</summary>
    /// <summary>Куда переносить файл.</summary>
    public enum StrayTarget
    {
        /// <summary>В корень системы.</summary>
        System,
        /// <summary>В общую папку _Other\&lt;система&gt;.</summary>
        Other,
        /// <summary>Не трогать.</summary>
        Skip
    }

    public class StrayFile
    {
        public string FullPath { get; set; } = "";
        public string SubFolderName { get; set; } = "";
        public long Size { get; set; }
        public bool IsDuplicate { get; set; }
        public string DuplicateOf { get; set; } = "";

        /// <summary>Куда пользователь решил перенести (по умолчанию — в _Other).</summary>
        public StrayTarget Target { get; set; } = StrayTarget.Other;
    }

    /// <summary>Результат поиска "заблудших" файлов в подпапках системы.</summary>
    public class StrayScanResult
    {
        public string SystemId { get; set; } = "";
        public string SystemFolder { get; set; } = "";
        public List<StrayFile> Candidates { get; set; } = new();

        public bool HasCandidates => Candidates.Count > 0;
    }

    /// <summary>
    /// Ищет игровые файлы в подпапках системы, которые сами не являются играми-папками.
    /// Например: 08_PlayStation1\1\Game.chd — файл в подпапке "1", которая не заявлена как система.
    /// Такие файлы предлагается перенести в корень системы.
    /// </summary>
    internal static class StrayFilesScanner
    {
        /// <summary>
        /// Найти кандидатов на перенос в подпапках системы.
        /// Исключает:
        ///   - известные папки (media, images, videos, gamelist.xml и т.д.);
        ///   - папки, которые сами являются "игрой-папкой" (GamesAreFolders = true);
        ///   - файлы, не подходящие под расширения системы.
        /// </summary>
        public static StrayScanResult Scan(GameSystem system, string systemFolder)
        {
            var result = new StrayScanResult
            {
                SystemId = system.Id,
                SystemFolder = systemFolder
            };

            if (string.IsNullOrWhiteSpace(systemFolder) || !Directory.Exists(systemFolder))
                return result;

            // Если система "игры-папки" — тут другая логика, не сканируем.
            if (system.GamesAreFolders)
                return result;

            if (system.PrimaryExtensions.Length == 0)
                return result;

            // Имена подпапок, которые точно НЕ являются местом для игр.
            var ignoredFolderNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "media", "images", "videos", "manuals", "downloaded_images",
                "_info", "_v1.4", "gamelist", "art", "boxart", "wheel",
                "fanart", "video", "manual", "bios", "saves", "savestates",
                "cheats", "screenshots", "covers", "configs", "system"
            };

            // Собираем имена файлов, которые уже есть в корне системы (для проверки дубликатов).
            var rootFiles = Directory
                .GetFiles(systemFolder, "*", SearchOption.TopDirectoryOnly)
                .ToDictionary(f => Path.GetFileName(f), f => f, StringComparer.OrdinalIgnoreCase);

            foreach (var subDir in Directory.GetDirectories(systemFolder))
            {
                string folderName = Path.GetFileName(subDir);
                if (string.IsNullOrWhiteSpace(folderName)) continue;

                // Папка из чёрного списка — пропускаем.
                if (ignoredFolderNames.Contains(folderName)) continue;

                // Служебные папки игнорируем.
                if (folderName.StartsWith("_")) continue;

                // Папка совпадает с именем системы/алиасом — пропускаем (это не "заблудший" файл).
                if (string.Equals(folderName, system.SourceFolder, StringComparison.OrdinalIgnoreCase))
                    continue;

                // Есть ли внутри файлы с нужными расширениями (рекурсивно).
                List<string> matchedFiles;
                try
                {
                    matchedFiles = Directory
                        .GetFiles(subDir, "*", SearchOption.AllDirectories)
                        .Where(f => MatchesExtension(f, system.PrimaryExtensions))
                        .ToList();
                }
                catch
                {
                    continue;
                }

                if (matchedFiles.Count == 0) continue;

                foreach (var file in matchedFiles)
                {
                    string fileName = Path.GetFileName(file);

                    var stray = new StrayFile
                    {
                        FullPath = file,
                        SubFolderName = folderName,
                        Size = new FileInfo(file).Length
                    };

                    // Проверка дубликата: файл с таким же именем уже есть в корне системы.
                    if (rootFiles.TryGetValue(fileName, out var existing))
                    {
                        var existingInfo = new FileInfo(existing);
                        if (existingInfo.Length == stray.Size)
                        {
                            stray.IsDuplicate = true;
                            stray.DuplicateOf = existing;
                        }
                    }

                    result.Candidates.Add(stray);
                }
            }

            // Сортируем: сначала по подпапке, потом по имени файла.
            result.Candidates = result.Candidates
                .OrderBy(c => c.SubFolderName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(c => Path.GetFileName(c.FullPath), StringComparer.OrdinalIgnoreCase)
                .ToList();

            return result;
        }

        private static bool MatchesExtension(string filePath, string[] extensions)
        {
            string fileName = Path.GetFileName(filePath).ToLowerInvariant();
            string ext = Path.GetExtension(filePath).ToLowerInvariant();

            foreach (var e in extensions)
            {
                string el = e.ToLowerInvariant();
                if (el == ext) return true;
                if (fileName.EndsWith(el)) return true;
            }
            return false;
        }
        /// <summary>
        /// Перенести отмеченные файлы: одни — в корень системы, другие — в _Other\&lt;система&gt;.
        /// Возвращает: (перенесено, ошибок).
        /// </summary>
        public static (int moved, int errors) TransferFiles(
            StrayScanResult scan, IEnumerable<StrayFile> files)
        {
            int moved = 0;
            int errors = 0;

            // Корень источника = родительская папка системы.
            string? sourceRoot = Directory.GetParent(scan.SystemFolder)?.FullName;

            string otherSystemDir = sourceRoot != null
                ? Path.Combine(sourceRoot, "_Other", Path.GetFileName(scan.SystemFolder))
                : "";

            foreach (var sf in files)
            {
                if (sf.IsDuplicate) continue;
                if (sf.Target == StrayTarget.Skip) continue;

                string targetDir = sf.Target == StrayTarget.System
                    ? scan.SystemFolder
                    : otherSystemDir;

                if (string.IsNullOrWhiteSpace(targetDir))
                {
                    errors++;
                    continue;
                }

                string target = Path.Combine(targetDir, Path.GetFileName(sf.FullPath));

                try
                {
                    if (File.Exists(target))
                    {
                        DebugLog.Write($"Пропущено (уже существует): {target}");
                        continue;
                    }

                    Directory.CreateDirectory(targetDir);
                    File.Move(sf.FullPath, target);
                    moved++;

                    DebugLog.Write($"Перенесён ({sf.Target}): {sf.FullPath} → {target}");
                }
                catch (Exception ex)
                {
                    errors++;
                    DebugLog.Write($"Ошибка переноса {sf.FullPath}: {ex.Message}");
                }
            }

            // Попробуем удалить опустевшие подпапки.
            TryRemoveEmptySubFolders(scan);

            return (moved, errors);
        }

        /// <summary>Удалить пустые подпапки (только если реально пусты — никаких данных не теряем).</summary>
        private static void TryRemoveEmptySubFolders(StrayScanResult scan)
        {
            try
            {
                foreach (var subDir in Directory.GetDirectories(scan.SystemFolder))
                {
                    string name = Path.GetFileName(subDir);

                    if (name.Equals("media", StringComparison.OrdinalIgnoreCase)) continue;
                    if (name.StartsWith("_")) continue;
                    if (name.StartsWith(".")) continue;

                    try
                    {
                        if (!Directory.EnumerateFileSystemEntries(subDir).Any())
                        {
                            Directory.Delete(subDir);
                            DebugLog.Write($"Удалена пустая папка: {subDir}");
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }
    }
}