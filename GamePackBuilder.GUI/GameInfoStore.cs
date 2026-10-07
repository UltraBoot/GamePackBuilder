using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace GamePackBuilder.GUI
{
    /// <summary>
    /// Хранилище пользовательских данных об играх.
    /// Один JSON на всё приложение: %LOCALAPPDATA%\GamePackBuilder\gameinfo.json
    /// Ключ — "systemId|относительный_путь".
    /// </summary>
    public static class GameInfoStore
    {
        private static Dictionary<string, GameInfo> _data = new();

        private static string StoreDir =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                         "GamePackBuilder");

        private static string StoreFile => Path.Combine(StoreDir, "gameinfo.json");

        public static string GetStoreFilePath() => StoreFile;

        public static void Load()
        {
            try
            {
                if (!File.Exists(StoreFile))
                {
                    _data = new Dictionary<string, GameInfo>();
                    return;
                }

                string json = File.ReadAllText(StoreFile);
                _data = JsonSerializer.Deserialize<Dictionary<string, GameInfo>>(json)
                        ?? new Dictionary<string, GameInfo>();
            }
            catch
            {
                _data = new Dictionary<string, GameInfo>();
            }
        }

        public static void Save()
        {
            try
            {
                Directory.CreateDirectory(StoreDir);
                string json = JsonSerializer.Serialize(_data,
                    new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(StoreFile, json);
            }
            catch
            {
                // Молча — не критично
            }
        }

        public static GameInfo Get(string systemId, string relativePath)
        {
            string key = MakeKey(systemId, relativePath);
            if (_data.TryGetValue(key, out var info))
                return info;
            return new GameInfo();
        }

        public static void Set(string systemId, string relativePath, GameInfo info)
        {
            string key = MakeKey(systemId, relativePath);

            if (info == null || info.IsEmpty)
                _data.Remove(key);
            else
                _data[key] = info;
        }

        private static string MakeKey(string systemId, string relPath)
        {
            return systemId + "|" + relPath.Replace('\\', '/').ToLowerInvariant();
        }
    }
}