using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace GamePackBuilder.GUI
{
    public class GameSystem
    {
        public string Id { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string SourceFolder { get; set; } = "";
        public string TargetFolder { get; set; } = "";
        public string[] PrimaryExtensions { get; set; } = Array.Empty<string>();
        public bool GamesAreFolders { get; set; } = false;

        /// <summary>
        /// Числовой ID системы в базе ScreenScraper (например, Mega Drive = 1).
        /// 0 означает «не задан» — скрапинг для этой системы работать не будет.
        /// </summary>
        public int ScreenScraperSystemId { get; set; } = 0;

        public override string ToString() => DisplayName;
    }

    /// <summary>
    /// Реестр игровых систем. Загружается из %LOCALAPPDATA%\GamePackBuilder\systems.json.
    /// </summary>
    public static class GameSystems
    {
        public static IReadOnlyList<GameSystem> All { get; private set; } = new List<GameSystem>();

        private static string SystemsDir => AppPaths.DataDir;

        private static string SystemsFile => Path.Combine(SystemsDir, "systems.json");

        public static string GetSystemsFilePath() => SystemsFile;

        /// <summary>Перезагрузить список систем из файла.</summary>
        public static void Reload()
        {
            try
            {
                if (!File.Exists(SystemsFile))
                {
                    All = new List<GameSystem>();
                    return;
                }

                string json = File.ReadAllText(SystemsFile);
                var wrapper = JsonSerializer.Deserialize<SystemsWrapper>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                All = wrapper?.Systems?.ToList() ?? new List<GameSystem>();
            }
            catch
            {
                All = new List<GameSystem>();
            }
        }

        public static GameSystem? FindBySourceFolder(string folderName)
        {
            return All.FirstOrDefault(s =>
                string.Equals(s.SourceFolder, folderName, StringComparison.OrdinalIgnoreCase));
        }

        public static GameSystem? FindById(string id)
        {
            return All.FirstOrDefault(s =>
                string.Equals(s.Id, id, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>Сохранить текущий список систем в файл.</summary>
        public static void Save()
        {
            try
            {
                Directory.CreateDirectory(SystemsDir);

                var wrapper = new SystemsWrapper
                {
                    Systems = All.ToArray()
                };

                string json = JsonSerializer.Serialize(wrapper,
                    new JsonSerializerOptions { WriteIndented = true });

                File.WriteAllText(SystemsFile, json);
            }
            catch
            {
                // Не критично — если не сохранилось, в следующий раз попробуем снова.
            }
        }

        /// <summary>Добавить систему, если её ещё нет. Возвращает true, если добавили.</summary>
        public static bool Add(GameSystem system)
        {
            if (FindById(system.Id) != null) return false;

            var list = All.ToList();
            list.Add(system);
            All = list;
            return true;
        }

        /// <summary>Заменить список систем целиком (нужно для редактирования).</summary>
        public static void ReplaceAll(IEnumerable<GameSystem> systems)
        {
            All = systems.ToList();
        }

        private class SystemsWrapper
        {
            public GameSystem[] Systems { get; set; } = Array.Empty<GameSystem>();
        }
    }
}