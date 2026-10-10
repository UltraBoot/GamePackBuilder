using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace GamePackBuilder.GUI
{
    /// <summary>
    /// Хранилище пользовательских данных об играх.
    /// Каждая система — отдельный файл: Data\gameinfo\&lt;systemId&gt;.json
    /// Внутри файла ключ — относительный путь игры.
    /// Старый единый gameinfo.json при первом запуске автоматически разложится по файлам.
    /// </summary>
    public static class GameInfoStore
    {
        // systemId -> (relativePath -> GameInfo)
        private static Dictionary<string, Dictionary<string, GameInfo>> _data = new();

        private static string StoreDir => Path.Combine(AppPaths.DataDir, "gameinfo");
        private static string LegacyFile => Path.Combine(AppPaths.DataDir, "gameinfo.json");

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true
        };

        public static string GetStoreDirPath() => StoreDir;

        public static void Load()
        {
            _data = new Dictionary<string, Dictionary<string, GameInfo>>(StringComparer.OrdinalIgnoreCase);

            try
            {
                Directory.CreateDirectory(StoreDir);

                // Если есть старый единый файл — разложим его по файлам систем.
                if (File.Exists(LegacyFile))
                    MigrateLegacy();

                // Читаем все *.json из папки.
                foreach (var file in Directory.GetFiles(StoreDir, "*.json"))
                {
                    string systemId = Path.GetFileNameWithoutExtension(file);
                    try
                    {
                        string json = File.ReadAllText(file);
                        var map = JsonSerializer.Deserialize<Dictionary<string, GameInfo>>(json);
                        if (map != null)
                            _data[systemId] = map;
                    }
                    catch
                    {
                        // Битый файл — пропускаем, не падаем.
                    }
                }
            }
            catch
            {
                _data = new Dictionary<string, Dictionary<string, GameInfo>>(StringComparer.OrdinalIgnoreCase);
            }
        }

        public static void Save()
        {
            try
            {
                Directory.CreateDirectory(StoreDir);

                foreach (var kv in _data)
                {
                    string systemId = kv.Key;
                    var map = kv.Value;
                    string file = Path.Combine(StoreDir, SanitizeFileName(systemId) + ".json");

                    if (map.Count == 0)
                    {
                        // Пустая система — удаляем файл.
                        if (File.Exists(file)) File.Delete(file);
                        continue;
                    }

                    string json = JsonSerializer.Serialize(map, JsonOptions);
                    File.WriteAllText(file, json);
                }
            }
            catch
            {
                // Молча — не критично.
            }
        }

        public static GameInfo Get(string systemId, string relativePath)
        {
            if (_data.TryGetValue(systemId, out var map) &&
                map.TryGetValue(MakeKey(relativePath), out var info))
                return info;
            return new GameInfo();
        }

        public static void Set(string systemId, string relativePath, GameInfo info)
        {
            string key = MakeKey(relativePath);

            if (info == null || info.IsEmpty)
            {
                if (_data.TryGetValue(systemId, out var existing))
                    existing.Remove(key);
                return;
            }

            if (!_data.TryGetValue(systemId, out var map))
            {
                map = new Dictionary<string, GameInfo>(StringComparer.OrdinalIgnoreCase);
                _data[systemId] = map;
            }

            map[key] = info;
        }

        private static string MakeKey(string relPath)
        {
            return relPath.Replace('\\', '/').ToLowerInvariant();
        }

        private static string SanitizeFileName(string name)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }

        /// <summary>
        /// Разложить старый единый gameinfo.json по файлам систем.
        /// После миграции старый файл переименовывается в gameinfo.json.old.
        /// </summary>
        private static void MigrateLegacy()
        {
            try
            {
                string json = File.ReadAllText(LegacyFile);
                var old = JsonSerializer.Deserialize<Dictionary<string, GameInfo>>(json);
                if (old == null) return;

                foreach (var kv in old)
                {
                    // Ключ формата "systemId|relativePath".
                    int sep = kv.Key.IndexOf('|');
                    if (sep <= 0) continue;

                    string systemId = kv.Key.Substring(0, sep);
                    string relPath = kv.Key.Substring(sep + 1);

                    if (!_data.TryGetValue(systemId, out var map))
                    {
                        map = new Dictionary<string, GameInfo>(StringComparer.OrdinalIgnoreCase);
                        _data[systemId] = map;
                    }
                    map[MakeKey(relPath)] = kv.Value;
                }

                // Пишем новые файлы и убираем старый.
                Save();

                string backup = LegacyFile + ".old";
                if (File.Exists(backup)) File.Delete(backup);
                File.Move(LegacyFile, backup);
            }
            catch
            {
                // Если миграция не удалась — не критично, оставим как есть.
            }
        }
    }
}