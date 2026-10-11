using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GamePackBuilder.GUI
{
    /// <summary>Результат скрапинга одной игры.</summary>
    public class ScrapeResult
    {
        public bool Success { get; set; }
        public bool GameNotFound { get; set; }
        public string? ErrorMessage { get; set; }
        public int TextFieldsFilled { get; set; }
        public int MediaDownloaded { get; set; }
        public GameInfo? Info { get; set; }
    }

    /// <summary>
    /// Логика скрапинга с ScreenScraper: заполнение текстовых полей и скачивание медиа.
    /// Медиа сохраняются в структуру Batocera/ArkOS:
    ///   media/boxart/&lt;имя&gt;-boxart.jpg
    ///   media/art/&lt;имя&gt;-screenshot.jpg
    ///   media/wheel/&lt;имя&gt;-wheel.png
    ///   media/video/&lt;имя&gt;-video.mp4
    ///   media/fanart/&lt;имя&gt;-fanart.jpg
    ///   media/manual/&lt;имя&gt;-manual.pdf
    /// Существующие файлы не перезаписываются.
    /// </summary>
    internal class ScreenScraperService
    {
        private readonly ScreenScraperClient _client = new();

        public async Task<ScrapeResult> ScrapeGameAsync(
            GameSystem system,
            GameInfo currentInfo,
            string gameFilePath,
            string basePath,
            string user,
            string password,
            MediaDownloadOptions? downloadOptions = null)
        {
            downloadOptions ??= MediaDownloadOptions.ForPortable();

            var result = new ScrapeResult();

            if (system.ScreenScraperSystemId <= 0)
            {
                result.ErrorMessage = $"Для системы «{system.DisplayName}» не задан ScreenScraper ID.";
                return result;
            }

            string romName = Path.GetFileName(gameFilePath);
            var info = CloneInfo(currentInfo);

            try
            {
                var response = await _client.GetGameInfoByNameAsync(
                    user, password, system.ScreenScraperSystemId, romName);

                var g = response.Response?.Game;
                if (g == null)
                {
                    result.GameNotFound = true;
                    result.ErrorMessage = "Игра не найдена в базе ScreenScraper.";
                    return result;
                }

                // ---- Текстовые поля ----
                int filled = FillTextFields(info, g);
                result.TextFieldsFilled = filled;

                // ---- Медиа ----
                string gameName = Directory.Exists(gameFilePath)
                    ? Path.GetFileName(gameFilePath)
                    : Path.GetFileNameWithoutExtension(gameFilePath);

                int mediaCount = await DownloadMediaAsync(
                    g, info, basePath, gameName, downloadOptions);

                result.MediaDownloaded = mediaCount;
                result.Info = info;
                result.Success = true;
                return result;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = ex.Message;
                DebugLog.Write($"Ошибка скрапинга {romName}: {ex.Message}");
                return result;
            }
        }

        // ============================================================
        // ТЕКСТОВЫЕ ПОЛЯ
        // ============================================================
        private static int FillTextFields(GameInfo info, ScreenScraperGame g)
        {
            int filled = 0;

            if (CanSet("Title", info.ScraperFields, info.Title) && g.Names?.Count > 0)
            {
                info.Title = g.Names[0].Text ?? "";
                MarkScraped(info, "Title"); filled++;
            }
            if (CanSet("Year", info.ScraperFields, info.Year))
            {
                var date = PickByRegion(g.Dates) ?? g.Dates?[0];
                if (!string.IsNullOrWhiteSpace(date?.Text))
                {
                    info.Year = date!.Text!.Length >= 4 ? date.Text.Substring(0, 4) : date.Text;
                    MarkScraped(info, "Year"); filled++;
                }
            }
            if (CanSet("Developer", info.ScraperFields, info.Developer) && g.Developer != null)
            {
                info.Developer = g.Developer.Text ?? "";
                MarkScraped(info, "Developer"); filled++;
            }
            if (CanSet("Genre", info.ScraperFields, info.Genre) && g.Genres?.Count > 0)
            {
                string genre = g.Genres[0].Names?.Find(n => n.Language == "en")?.Text
                            ?? g.Genres[0].Names?[0].Text
                            ?? "";
                info.Genre = genre;
                MarkScraped(info, "Genre"); filled++;
            }
            if (CanSet("Players", info.ScraperFields, info.Players) && g.Players != null)
            {
                info.Players = g.Players.Text ?? "";
                MarkScraped(info, "Players"); filled++;
            }
            if (CanSet("Description", info.ScraperFields, info.Description) && g.Descriptions?.Count > 0)
            {
                string desc = g.Descriptions.Find(d => d.Language == "en")?.Text
                           ?? g.Descriptions[0].Text
                           ?? "";
                info.Description = desc;
                MarkScraped(info, "Description"); filled++;
            }

            return filled;
        }

        // ============================================================
        // МЕДИА
        // ============================================================
        private async Task<int> DownloadMediaAsync(
            ScreenScraperGame g,
            GameInfo info,
            string basePath,
            string gameName,
            MediaDownloadOptions opts)
        {
            int count = 0;

            // Обложка
            if (opts.Boxart)
            {
                var cover = PickMediaByRegion(g.Medias?.FindAll(m => m.Type == "box-2D") ?? new());
                string? path = await DownloadOneAsync(
                    cover, basePath, MediaScanner.BoxartDir, gameName, "-boxart");

                if (path != null)
                {
                    info.CoverPath = path;
                    count++;
                }
            }

            // Скриншоты (до 5)
            if (opts.Screenshots)
            {
                var allShots = g.Medias?.FindAll(m => m.Type == "ss") ?? new();
                var shots = PickMediaListByRegion(allShots, maxCount: 5);

                info.Screenshots.Clear();
                int shotIndex = 1;
                foreach (var shot in shots)
                {
                    string suffix = shotIndex == 1 ? "-screenshot" : $"-screenshot-{shotIndex}";
                    string? path = await DownloadOneAsync(
                        shot, basePath, MediaScanner.ArtDir, gameName, suffix);

                    if (path != null)
                    {
                        info.Screenshots.Add(path);
                        count++;
                        shotIndex++;
                    }
                }
            }

            // Wheel (логотип)
            if (opts.Wheel)
            {
                var wheel = PickMediaByRegion(g.Medias?.FindAll(m => m.Type == "wheel-hd") ?? new())
                         ?? PickMediaByRegion(g.Medias?.FindAll(m => m.Type == "wheel") ?? new());
                string? path = await DownloadOneAsync(
                    wheel, basePath, MediaScanner.WheelDir, gameName, "-wheel");

                if (path != null)
                {
                    info.WheelPath = path;
                    count++;
                }
            }

            // Fanart
            if (opts.Fanart)
            {
                var fanart = PickMediaByRegion(g.Medias?.FindAll(m => m.Type == "fanart") ?? new());
                string? path = await DownloadOneAsync(
                    fanart, basePath, MediaScanner.FanartDir, gameName, "-fanart");

                if (path != null)
                {
                    info.FanartPath = path;
                    count++;
                }
            }

            // Видео
            if (opts.Video)
            {
                var video = PickMediaByRegion(g.Medias?.FindAll(m => m.Type == "video-normalized") ?? new())
                         ?? PickMediaByRegion(g.Medias?.FindAll(m => m.Type == "video") ?? new());
                string? path = await DownloadOneAsync(
                    video, basePath, MediaScanner.VideoDir, gameName, "-video");

                if (path != null)
                {
                    info.VideoPath = path;
                    count++;
                }
            }

            // Мануал (PDF)
            if (opts.Manual)
            {
                var manual = PickMediaByRegion(g.Medias?.FindAll(m => m.Type == "manuel") ?? new());
                string? path = await DownloadOneAsync(
                    manual, basePath, MediaScanner.ManualDir, gameName, "-manual");

                if (path != null)
                {
                    info.ManualPath = path;
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Скачать один медиафайл в media/&lt;subDir&gt;/&lt;gameName&gt;&lt;suffix&gt;&lt;ext&gt;.
        /// Существующий файл не перезаписывается — возвращаем его путь как есть.
        /// Если URL пустой или скачивание не удалось — вернёт null.
        /// </summary>
        private async Task<string?> DownloadOneAsync(
            ScreenScraperMedia? media,
            string basePath,
            string subDir,
            string gameName,
            string suffix)
        {
            if (media == null || string.IsNullOrWhiteSpace(media.Url))
                return null;

            string ext = GetMediaExtension(media.Format, media.Url);
            string dir = Path.Combine(basePath, "media", subDir);
            string filePath = Path.Combine(dir, gameName + suffix + ext);

            // Файл уже есть — не перезаписываем, возвращаем существующий.
            if (File.Exists(filePath))
                return filePath;

            try
            {
                Directory.CreateDirectory(dir);
                byte[] bytes = await _client.DownloadFileAsync(media.Url!);

                // Двойная проверка на всякий случай.
                if (!File.Exists(filePath))
                    File.WriteAllBytes(filePath, bytes);

                return filePath;
            }
            catch (Exception ex)
            {
                DebugLog.Write($"Ошибка скачивания {subDir}/{gameName}{suffix}: {ex.Message}");
                return null;
            }
        }

        // ============================================================
        // ВСПОМОГАТЕЛЬНОЕ
        // ============================================================
        private static GameInfo CloneInfo(GameInfo src) => new()
        {
            Title = src.Title,
            Year = src.Year,
            Developer = src.Developer,
            Genre = src.Genre,
            Players = src.Players,
            Description = src.Description,
            CoverPath = src.CoverPath,
            Screenshots = new List<string>(src.Screenshots),
            WheelPath = src.WheelPath,
            VideoPath = src.VideoPath,
            FanartPath = src.FanartPath,
            ManualPath = src.ManualPath,
            ScraperFields = new List<string>(src.ScraperFields)
        };

        private static bool CanSet(string fieldName, List<string> scraperFields, string currentValue)
        {
            if (string.IsNullOrWhiteSpace(currentValue)) return true;
            return scraperFields.Contains(fieldName);
        }

        private static void MarkScraped(GameInfo info, string fieldName)
        {
            if (!info.ScraperFields.Contains(fieldName))
                info.ScraperFields.Add(fieldName);
        }

        private static ScreenScraperLocalizedText? PickByRegion(
            List<ScreenScraperLocalizedText>? list)
        {
            if (list == null || list.Count == 0) return null;
            string[] order = { "wor", "us", "eu", "jp" };
            foreach (var reg in order)
            {
                var hit = list.Find(x => x.Region == reg);
                if (hit != null) return hit;
            }
            return null;
        }

        private static ScreenScraperMedia? PickMediaByRegion(List<ScreenScraperMedia> list)
        {
            if (list.Count == 0) return null;
            string[] order = { "wor", "us", "eu", "jp" };
            foreach (var reg in order)
            {
                var hit = list.Find(m => m.Region == reg);
                if (hit != null) return hit;
            }
            return list[0];
        }

        private static List<ScreenScraperMedia> PickMediaListByRegion(
            List<ScreenScraperMedia> list, int maxCount)
        {
            if (list.Count == 0) return new List<ScreenScraperMedia>();

            int RegionRank(string? region) => region switch
            {
                "wor" => 0,
                "us" => 1,
                "eu" => 2,
                "jp" => 3,
                _ => 4
            };

            return list.OrderBy(m => RegionRank(m.Region)).Take(maxCount).ToList();
        }

        private static string GetMediaExtension(string? format, string url)
        {
            if (!string.IsNullOrWhiteSpace(format))
                return "." + format.TrimStart('.');

            string ext = Path.GetExtension(url);
            return string.IsNullOrWhiteSpace(ext) ? ".jpg" : ext;
        }
    }
}