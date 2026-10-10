using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GamePackBuilder.GUI
{
    public class SystemScanResult
    {
        public GameSystem System { get; set; } = null!;
        public string SourcePath { get; set; } = "";

        /// <summary>Игры в виде отдельных файлов (обычные системы).</summary>
        public List<string> Files { get; set; } = new();

        /// <summary>Игры в виде папок (Master System, Dreamcast, Saturn).</summary>
        public List<string> Folders { get; set; } = new();

        public int TotalGames => Files.Count + Folders.Count;

        public override string ToString() => $"{System.DisplayName}: {TotalGames} игр";
    }

    public static class GameScanner
    {
        /// <summary>
        /// Сканирует папку-источник рекурсивно. Находит известные системы и все игры в них.
        /// Новые папки, которых нет в systems.json, автоматически добавляются
        /// (с подстановкой данных из встроенной базы systems-base.json, если найдётся).
        /// </summary>
        public static List<SystemScanResult> Scan(string sourcePath)
        {
            var results = new List<SystemScanResult>();

            if (string.IsNullOrWhiteSpace(sourcePath) || !Directory.Exists(sourcePath))
                return results;

            bool addedNew = false;

            foreach (var dir in Directory.GetDirectories(sourcePath))
            {
                string folderName = Path.GetFileName(dir);
                if (string.IsNullOrWhiteSpace(folderName)) continue;

                var system = GameSystems.FindBySourceFolder(folderName);

                // Новая папка — попробуем создать систему.
                if (system == null)
                {
                    if (!HasContent(dir)) continue;   // пустые папки игнорируем

                    system = CreateSystemForFolder(folderName, dir);
                    if (GameSystems.Add(system))
                        addedNew = true;
                }

                var result = new SystemScanResult
                {
                    System = system,
                    SourcePath = dir
                };

                if (system.GamesAreFolders)
                {
                    foreach (var sub in Directory.GetDirectories(dir))
                    {
                        bool hasGameFile = Directory
                            .GetFiles(sub, "*", SearchOption.AllDirectories)
                            .Any(f => MatchesExtension(f, system.PrimaryExtensions));

                        if (hasGameFile)
                            result.Folders.Add(sub);
                    }

                    var rootFiles = Directory
                        .GetFiles(dir, "*", SearchOption.TopDirectoryOnly)
                        .Where(f => MatchesExtension(f, system.PrimaryExtensions));
                    result.Files.AddRange(rootFiles);
                }
                else
                {
                    var files = Directory
                        .GetFiles(dir, "*", SearchOption.AllDirectories)
                        .Where(f => MatchesExtension(f, system.PrimaryExtensions));
                    result.Files.AddRange(files);
                }

                results.Add(result);
            }

            // Если появились новые системы — сохраним systems.json.
            if (addedNew)
                GameSystems.Save();

            return results
                .OrderBy(r => r.System.SourceFolder, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>Есть ли в папке хоть какой-то файл или подпапка.</summary>
        private static bool HasContent(string dir)
        {
            try
            {
                return Directory.EnumerateFileSystemEntries(dir).Any();
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Создать GameSystem для найденной папки.
        /// Если папка есть в systems-base.json — заполняем всё оттуда.
        /// Иначе — черновая система, пользователь заполнит её в редакторе.
        /// </summary>
        private static GameSystem CreateSystemForFolder(string folderName, string fullPath)
        {
            var baseEntry = SystemsBase.FindByFolder(folderName);

            if (baseEntry != null)
            {
                return new GameSystem
                {
                    Id = baseEntry.Id,
                    DisplayName = baseEntry.DisplayName,
                    SourceFolder = folderName,
                    TargetFolder = string.IsNullOrWhiteSpace(baseEntry.TargetFolder)
                        ? baseEntry.Id
                        : baseEntry.TargetFolder,
                    PrimaryExtensions = baseEntry.PrimaryExtensions.ToArray(),
                    GamesAreFolders = baseEntry.GamesAreFolders,
                    ScreenScraperSystemId = baseEntry.ScreenScraperId
                };
            }

            // Черновая система. Автоматически подхватываем расширения
            // из файлов, которые лежат в папке — чтобы сразу что-то находилось.
            var autoExtensions = CollectExtensions(fullPath);

            return new GameSystem
            {
                Id = folderName,
                DisplayName = folderName,
                SourceFolder = folderName,
                TargetFolder = folderName.ToLowerInvariant().Replace(' ', '_'),
                PrimaryExtensions = autoExtensions,
                GamesAreFolders = false,
                ScreenScraperSystemId = 0
            };
        }

        /// <summary>
        /// Собрать все уникальные расширения файлов (рекурсивно), которые есть в папке.
        /// Например: .smc, .zip, .txt. Используется только для черновых систем.
        /// </summary>
        private static string[] CollectExtensions(string folderName)
        {
            try
            {
                if (!Directory.Exists(folderName)) return Array.Empty<string>();

                return Directory
                    .GetFiles(folderName, "*", SearchOption.AllDirectories)
                    .Select(f => Path.GetExtension(f).ToLowerInvariant())
                    .Where(e => !string.IsNullOrWhiteSpace(e))
                    .Distinct()
                    .OrderBy(e => e)
                    .ToArray();
            }
            catch
            {
                return Array.Empty<string>();
            }
        }
        

        /// <summary>
        /// Проверяет, соответствует ли файл хотя бы одному из расширений.
        /// Поддерживает составные расширения вроде ".p8.png".
        /// </summary>
        private static bool MatchesExtension(string filePath, string[] extensions)
        {
            string fileName = Path.GetFileName(filePath).ToLowerInvariant();
            string ext = Path.GetExtension(filePath).ToLowerInvariant();

            foreach (var e in extensions)
            {
                string el = e.ToLowerInvariant();

                // Обычное расширение типа ".zip"
                if (el == ext) return true;

                // Составное типа ".p8.png"
                if (fileName.EndsWith(el)) return true;
            }
            return false;
        }
    }
}