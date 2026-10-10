using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GamePackBuilder.GUI
{
    /// <summary>
    /// Встроенная база популярных систем. Читается из systems-base.json
    /// (лежит рядом с exe, копируется при сборке).
    /// Помогает автоматически заполнить поля новой системы при сканировании.
    /// </summary>
    internal static class SystemsBase
    {
        private static List<BaseSystem>? _cache;

        private static string BaseFile =>
            Path.Combine(AppContext.BaseDirectory, "systems-base.json");

        /// <summary>Найти в базе систему по имени папки-источника.</summary>
        public static BaseSystem? FindByFolder(string folderName)
        {
            EnsureLoaded();
            if (_cache == null) return null;

            return _cache.FirstOrDefault(s =>
                s.SourceFolderAliases.Any(a =>
                    string.Equals(a, folderName, StringComparison.OrdinalIgnoreCase)));
        }

        private static void EnsureLoaded()
        {
            if (_cache != null) return;

            try
            {
                if (!File.Exists(BaseFile))
                {
                    _cache = new List<BaseSystem>();
                    return;
                }

                string json = File.ReadAllText(BaseFile);
                var wrapper = JsonSerializer.Deserialize<BaseWrapper>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                _cache = wrapper?.Systems?.ToList() ?? new List<BaseSystem>();
            }
            catch
            {
                _cache = new List<BaseSystem>();
            }
        }

        public class BaseSystem
        {
            [JsonPropertyName("screenScraperId")]
            public int ScreenScraperId { get; set; }

            [JsonPropertyName("id")]
            public string Id { get; set; } = "";

            [JsonPropertyName("displayName")]
            public string DisplayName { get; set; } = "";

            [JsonPropertyName("targetFolder")]
            public string TargetFolder { get; set; } = "";

            [JsonPropertyName("sourceFolderAliases")]
            public List<string> SourceFolderAliases { get; set; } = new();

            [JsonPropertyName("primaryExtensions")]
            public List<string> PrimaryExtensions { get; set; } = new();

            [JsonPropertyName("gamesAreFolders")]
            public bool GamesAreFolders { get; set; }
        }

        private class BaseWrapper
        {
            [JsonPropertyName("systems")]
            public BaseSystem[] Systems { get; set; } = Array.Empty<BaseSystem>();
        }
    }
}