using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GamePackBuilder.GUI
{
    /// <summary>Один конфликт: поле в базе отличается от поля в gamelist.xml.</summary>
    public class GamelistConflict
    {
        public string SystemId { get; set; } = "";
        public string RelativePath { get; set; } = "";
        public string GameTitle { get; set; } = "";
        public string FieldName { get; set; } = "";
        public string FieldLabel { get; set; } = "";
        public string OldValue { get; set; } = "";
        public string NewValue { get; set; } = "";
    }

    /// <summary>Результат импорта gamelist.xml в базу.</summary>
    public class GamelistImportResult
    {
        /// <summary>Сколько игр получили данные из gamelist (без конфликтов).</summary>
        public int FilledSilently { get; set; }

        /// <summary>Сколько игр создано в базе заново (их там вообще не было).</summary>
        public int CreatedNew { get; set; }

        /// <summary>Список конфликтов — пользователь решает, что применить.</summary>
        public List<GamelistConflict> Conflicts { get; set; } = new();

        public bool HasConflicts => Conflicts.Count > 0;
        public bool HasChanges => FilledSilently > 0 || CreatedNew > 0;
    }

    /// <summary>
    /// Импорт gamelist.xml в нашу базу GameInfoStore.
    /// Пустые поля заполняются молча, расхождения собираются в список конфликтов.
    /// </summary>
    internal static class GamelistImporter
    {
        public static GamelistImportResult ImportForSystem(
            GameSystem system,
            string systemFolder,
            IEnumerable<string> gameFullPaths)
        {
            var result = new GamelistImportResult();

            var reader = new GamelistReader();
            if (!reader.TryLoad(systemFolder))
                return result;

            GameInfoStore.Load();

            foreach (var fullPath in gameFullPaths)
            {
                string relPath = Path.GetRelativePath(systemFolder, fullPath);
                var entry = reader.FindByGamePath(relPath);
                if (entry == null) continue;

                var info = GameInfoStore.Get(system.Id, relPath);
                bool isNew = string.IsNullOrWhiteSpace(info.Title)
                          && string.IsNullOrWhiteSpace(info.Description)
                          && info.Screenshots.Count == 0;

                string gameName = Directory.Exists(fullPath)
                    ? Path.GetFileName(fullPath)
                    : Path.GetFileNameWithoutExtension(fullPath);

                // Сначала обогатим медиа с диска, чтобы не создавать ложных конфликтов по путям.
                MediaScanner.EnrichGameInfo(info, systemFolder, gameName);

                bool changed = false;

                // ---- Текстовые поля ----
                changed |= MergeField(info, "Title", "Название",
                    entry.Name, v => info.Title, v => info.Title = v, result, system, relPath, entry.Name);
                changed |= MergeField(info, "Year", "Год",
                    ExtractYear(entry.ReleaseDate), v => info.Year, v => info.Year = v, result, system, relPath, entry.Name);
                changed |= MergeField(info, "Developer", "Разработчик",
                    entry.Developer, v => info.Developer, v => info.Developer = v, result, system, relPath, entry.Name);
                changed |= MergeField(info, "Genre", "Жанр",
                    entry.Genre, v => info.Genre, v => info.Genre = v, result, system, relPath, entry.Name);
                changed |= MergeField(info, "Players", "Игроков",
                    entry.Players, v => info.Players, v => info.Players = v, result, system, relPath, entry.Name);
                changed |= MergeField(info, "Description", "Описание",
                    entry.Description, v => info.Description, v => info.Description = v, result, system, relPath, entry.Name);

                // ---- Медиа (просто заполняем пустое) ----
                if (string.IsNullOrWhiteSpace(info.CoverPath))
                {
                    string p = GamelistReader.ResolveMediaPath(systemFolder, entry.Thumbnail);
                    if (File.Exists(p)) { info.CoverPath = p; changed = true; }
                }

                if (info.Screenshots.Count == 0)
                {
                    string p = GamelistReader.ResolveMediaPath(systemFolder, entry.Image);
                    if (File.Exists(p)) { info.Screenshots.Add(p); changed = true; }
                }

                if (string.IsNullOrWhiteSpace(info.WheelPath))
                {
                    string p = GamelistReader.ResolveMediaPath(systemFolder, entry.Marquee);
                    if (File.Exists(p)) { info.WheelPath = p; changed = true; }
                }

                if (string.IsNullOrWhiteSpace(info.FanartPath))
                {
                    string p = GamelistReader.ResolveMediaPath(systemFolder, entry.Fanart);
                    if (File.Exists(p)) { info.FanartPath = p; changed = true; }
                }

                if (string.IsNullOrWhiteSpace(info.VideoPath))
                {
                    string p = GamelistReader.ResolveMediaPath(systemFolder, entry.Video);
                    if (File.Exists(p)) { info.VideoPath = p; changed = true; }
                }

                if (string.IsNullOrWhiteSpace(info.ManualPath))
                {
                    string p = GamelistReader.ResolveMediaPath(systemFolder, entry.Manual);
                    if (File.Exists(p)) { info.ManualPath = p; changed = true; }
                }

                if (changed)
                {
                    GameInfoStore.Set(system.Id, relPath, info);
                    if (isNew) result.CreatedNew++;
                    else result.FilledSilently++;
                }
            }

            // Сохраняем базу, если что-то изменилось.
            if (result.HasChanges)
                GameInfoStore.Save();

            return result;
        }

        /// <summary>
        /// Сравнивает одно поле. Возвращает true, если база изменилась
        /// (пустое поле заполнено значением из gamelist).
        /// Если поле заполнено и отличается — добавляет конфликт, но НЕ меняет.
        /// </summary>
        private static bool MergeField(
            GameInfo info,
            string fieldName,
            string fieldLabel,
            string gamelistValue,
            Func<GameInfo, string> getter,
            Action<string> setter,
            GamelistImportResult result,
            GameSystem system,
            string relPath,
            string gameTitle)
        {
            if (string.IsNullOrWhiteSpace(gamelistValue)) return false;

            string current = getter(info);

            // Поле пустое — заполняем молча.
            if (string.IsNullOrWhiteSpace(current))
            {
                setter(gamelistValue);
                return true;
            }

            // Поле совпадает — ничего не делаем.
            if (string.Equals(current.Trim(), gamelistValue.Trim(), StringComparison.OrdinalIgnoreCase))
                return false;

            // Есть конфликт — запоминаем, но НЕ трогаем.
            result.Conflicts.Add(new GamelistConflict
            {
                SystemId = system.Id,
                RelativePath = relPath,
                GameTitle = gameTitle,
                FieldName = fieldName,
                FieldLabel = fieldLabel,
                OldValue = current,
                NewValue = gamelistValue
            });

            return false;
        }

        /// <summary>Из "20001120T000000" получить "2000".</summary>
        private static string ExtractYear(string releaseDate)
        {
            if (string.IsNullOrWhiteSpace(releaseDate) || releaseDate.Length < 4)
                return "";
            return releaseDate.Substring(0, 4);
        }
        /// <summary>
        /// Применить выбранные пользователем значения из конфликтов к базе.
        /// </summary>
        public static void ApplyAccepted(List<GamelistConflict> accepted)
        {
            if (accepted == null || accepted.Count == 0) return;

            GameInfoStore.Load();

            foreach (var c in accepted)
            {
                var info = GameInfoStore.Get(c.SystemId, c.RelativePath);
                if (info == null) continue;

                switch (c.FieldName)
                {
                    case "Title": info.Title = c.NewValue; break;
                    case "Year": info.Year = c.NewValue; break;
                    case "Developer": info.Developer = c.NewValue; break;
                    case "Genre": info.Genre = c.NewValue; break;
                    case "Players": info.Players = c.NewValue; break;
                    case "Description": info.Description = c.NewValue; break;
                }

                GameInfoStore.Set(c.SystemId, c.RelativePath, info);
            }

            GameInfoStore.Save();
        }
    }
}