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
        /// </summary>
        public static List<SystemScanResult> Scan(string sourcePath)
        {
            var results = new List<SystemScanResult>();

            if (string.IsNullOrWhiteSpace(sourcePath) || !Directory.Exists(sourcePath))
                return results;

            foreach (var dir in Directory.GetDirectories(sourcePath))
            {
                string folderName = Path.GetFileName(dir);
                var system = GameSystems.FindBySourceFolder(folderName);
                if (system == null) continue;

                var result = new SystemScanResult
                {
                    System = system,
                    SourcePath = dir
                };

                if (system.GamesAreFolders)
                {
                    // Игра = папка, внутри которой есть файл с нужным расширением.
                    foreach (var sub in Directory.GetDirectories(dir))
                    {
                        bool hasGameFile = Directory
                            .GetFiles(sub, "*", SearchOption.AllDirectories)
                            .Any(f => MatchesExtension(f, system.PrimaryExtensions));

                        if (hasGameFile)
                            result.Folders.Add(sub);
                    }

                    // Плюс «плоские» игры прямо в корне системы (например, единичные .chd).
                    var rootFiles = Directory
                        .GetFiles(dir, "*", SearchOption.TopDirectoryOnly)
                        .Where(f => MatchesExtension(f, system.PrimaryExtensions));
                    result.Files.AddRange(rootFiles);
                }
                else
                {
                    // Игра = файл. Рекурсивно по всем подпапкам.
                    var files = Directory
                        .GetFiles(dir, "*", SearchOption.AllDirectories)
                        .Where(f => MatchesExtension(f, system.PrimaryExtensions));
                    result.Files.AddRange(files);
                }

                results.Add(result);
            }

            return results
                .OrderBy(r => r.System.SourceFolder, StringComparer.OrdinalIgnoreCase)
                .ToList();
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