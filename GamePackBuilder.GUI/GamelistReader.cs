using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace GamePackBuilder.GUI
{
    /// <summary>Одна запись игры из gamelist.xml.</summary>
    public class GamelistEntry
    {
        public string Path { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Developer { get; set; } = "";
        public string Publisher { get; set; } = "";
        public string Genre { get; set; } = "";
        public string Players { get; set; } = "";
        public string ReleaseDate { get; set; } = "";
        public string Region { get; set; } = "";
        public string Lang { get; set; } = "";

        // Медиа (относительные пути от папки системы).
        public string Image { get; set; } = "";       // скриншот
        public string Thumbnail { get; set; } = "";   // обложка
        public string Marquee { get; set; } = "";     // логотип
        public string Fanart { get; set; } = "";
        public string Video { get; set; } = "";
        public string Manual { get; set; } = "";

        /// <summary>Ключ игры — относительный путь без "./" в начале и без разделителей платформы.</summary>
        public string NormalizedPath =>
            Path.Replace('\\', '/').TrimStart('.').TrimStart('/');
    }

    /// <summary>
    /// Читает gamelist.xml (формат EmulationStation) и позволяет искать данные по имени ROM-файла.
    /// </summary>
    internal class GamelistReader
    {
        private readonly Dictionary<string, GamelistEntry> _byPath =
            new(StringComparer.OrdinalIgnoreCase);

        /// <summary>Загрузить gamelist.xml из папки системы. Если файла нет — просто ничего не делает.</summary>
        public bool TryLoad(string systemFolder)
        {
            string file = Path.Combine(systemFolder, "gamelist.xml");
            if (!File.Exists(file)) return false;

            try
            {
                var doc = XDocument.Load(file);
                var root = doc.Root;
                if (root == null) return false;

                foreach (var g in root.Elements("game"))
                {
                    var entry = new GamelistEntry
                    {
                        Path = (string?)g.Element("path") ?? "",
                        Name = (string?)g.Element("name") ?? "",
                        Description = (string?)g.Element("desc") ?? "",
                        Developer = (string?)g.Element("developer") ?? "",
                        Publisher = (string?)g.Element("publisher") ?? "",
                        Genre = (string?)g.Element("genre") ?? "",
                        Players = (string?)g.Element("players") ?? "",
                        ReleaseDate = (string?)g.Element("releasedate") ?? "",
                        Region = (string?)g.Element("region") ?? "",
                        Lang = (string?)g.Element("lang") ?? "",
                        Image = (string?)g.Element("image") ?? "",
                        Thumbnail = (string?)g.Element("thumbnail") ?? "",
                        Marquee = (string?)g.Element("marquee") ?? "",
                        Fanart = (string?)g.Element("fanart") ?? "",
                        Video = (string?)g.Element("video") ?? "",
                        Manual = (string?)g.Element("manual") ?? "",
                    };

                    if (string.IsNullOrWhiteSpace(entry.Path)) continue;

                    string key = entry.NormalizedPath;
                    if (!_byPath.ContainsKey(key))
                        _byPath[key] = entry;
                }

                return _byPath.Count > 0;
            }
            catch (Exception ex)
            {
                DebugLog.Write($"GamelistReader: ошибка чтения {file}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Найти запись по относительному пути игры.
        /// Пробует несколько вариантов: точное совпадение, без расширения, только имя файла.
        /// </summary>
        public GamelistEntry? FindByGamePath(string relativePath)
        {
            if (_byPath.Count == 0) return null;

            string key = relativePath.Replace('\\', '/').TrimStart('.', '/');

            // 1. Точное совпадение.
            if (_byPath.TryGetValue(key, out var exact)) return exact;

            // 2. Без расширения.
            string noExt = Path.ChangeExtension(key, null);
            var byNoExt = _byPath.FirstOrDefault(kv =>
                string.Equals(Path.ChangeExtension(kv.Key, null), noExt, StringComparison.OrdinalIgnoreCase));
            if (byNoExt.Value != null) return byNoExt.Value;

            // 3. По имени файла (если у нас путь вложенный, а в gamelist.xml — плоский).
            string fileName = Path.GetFileName(key);
            var byName = _byPath.FirstOrDefault(kv =>
                string.Equals(Path.GetFileName(kv.Key), fileName, StringComparison.OrdinalIgnoreCase));
            if (byName.Value != null) return byName.Value;

            // 4. По имени без расширения.
            string fileNameNoExt = Path.GetFileNameWithoutExtension(fileName);
            var byNameNoExt = _byPath.FirstOrDefault(kv =>
                string.Equals(Path.GetFileNameWithoutExtension(kv.Key), fileNameNoExt,
                    StringComparison.OrdinalIgnoreCase));
            return byNameNoExt.Value;
        }

        /// <summary>Путь к медиафайлу из записи gamelist.xml, приведённый к абсолютному пути.</summary>
        public static string ResolveMediaPath(string systemFolder, string relativeFromGamelist)
        {
            if (string.IsNullOrWhiteSpace(relativeFromGamelist)) return "";

            string cleaned = relativeFromGamelist.Replace('\\', '/').TrimStart('.', '/');
            return Path.GetFullPath(Path.Combine(systemFolder, cleaned));
        }
    }
}