using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace GamePackBuilder.GUI
{
    /// <summary>
    /// Кэш последнего сканирования. Позволяет показать список сразу при запуске.
    /// </summary>
    public class ScanCache
    {
        public DateTime LastScan { get; set; } = DateTime.MinValue;
        public string SourcePath { get; set; } = "";
        public List<CachedSystem> Systems { get; set; } = new();

        public class CachedSystem
        {
            public string SystemId { get; set; } = "";
            public List<string> Files { get; set; } = new();
            public List<string> Folders { get; set; } = new();
        }

        // ---------- Внутреннее ----------

        private static string CacheDir =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                         "GamePackBuilder");

        private static string CacheFile =>
            Path.Combine(CacheDir, "cache.json");

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = false
        };

        public static ScanCache? Load(string expectedSourcePath)
        {
            try
            {
                if (!File.Exists(CacheFile)) return null;

                string json = File.ReadAllText(CacheFile);
                var cache = JsonSerializer.Deserialize<ScanCache>(json);
                if (cache == null) return null;

                // Если источник сменился — кэш не подходит
                if (!string.Equals(cache.SourcePath, expectedSourcePath,
                                   StringComparison.OrdinalIgnoreCase))
                    return null;

                return cache;
            }
            catch
            {
                return null;
            }
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(CacheDir);
                string json = JsonSerializer.Serialize(this, JsonOptions);
                File.WriteAllText(CacheFile, json);
            }
            catch
            {
                // Кэш — не критично, молча игнорируем ошибки
            }
        }

        /// <summary>Построить кэш из результатов скана.</summary>
        public static ScanCache FromResults(string sourcePath, IEnumerable<SystemScanResult> results)
        {
            var cache = new ScanCache
            {
                LastScan = DateTime.Now,
                SourcePath = sourcePath
            };

            foreach (var r in results)
            {
                cache.Systems.Add(new CachedSystem
                {
                    SystemId = r.System.Id,
                    Files = r.Files.ToList(),
                    Folders = r.Folders.ToList()
                });
            }

            return cache;
        }

        /// <summary>Восстановить результаты скана из кэша (для показа в UI).</summary>
        public List<SystemScanResult> ToResults()
        {
            var list = new List<SystemScanResult>();

            foreach (var c in Systems)
            {
                var system = GameSystems.FindById(c.SystemId);
                if (system == null) continue;

                list.Add(new SystemScanResult
                {
                    System = system,
                    SourcePath = Path.Combine(SourcePath, system.SourceFolder),
                    Files = c.Files.ToList(),
                    Folders = c.Folders.ToList()
                });
            }

            return list
                .OrderBy(r => r.System.SourceFolder, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }
}