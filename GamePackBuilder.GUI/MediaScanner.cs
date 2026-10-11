using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GamePackBuilder.GUI
{
    /// <summary>
    /// Ищет медиафайлы игры в папке media/ системы.
    /// Поддерживает структуру Batocera/ArkOS:
    ///   media/boxart/&lt;имя&gt;-boxart.jpg
    ///   media/art/&lt;имя&gt;-screenshot.jpg
    ///   media/wheel/&lt;имя&gt;-wheel.png
    ///   media/video/&lt;имя&gt;-video.mp4
    ///   media/fanart/&lt;имя&gt;-fanart.jpg
    ///   media/manual/&lt;имя&gt;-manual.pdf
    /// Также понимает "плоскую" структуру (media/&lt;имя&gt;.jpg) — как старые версии программы писали.
    /// </summary>
    internal static class MediaScanner
    {
        // Имена подпапок внутри media/.
        public const string BoxartDir = "boxart";
        public const string ArtDir = "art";
        public const string WheelDir = "wheel";
        public const string VideoDir = "video";
        public const string FanartDir = "fanart";
        public const string ManualDir = "manual";

        /// <summary>
        /// Найти обложку игры. Ищем в media/boxart/, потом в media/ (плоская структура).
        /// </summary>
        public static string? FindCover(string systemFolder, string gameName)
        {
            return FindByPattern(systemFolder, gameName, BoxartDir, "-boxart",
                new[] { ".jpg", ".jpeg", ".png" })
                ?? FindByPattern(systemFolder, gameName, null, "",
                new[] { ".jpg", ".jpeg", ".png" });
        }

        /// <summary>Найти скриншоты игры. Возвращает список путей (до 10 штук).</summary>
        public static List<string> FindScreenshots(string systemFolder, string gameName)
        {
            var result = new List<string>();

            // Формат Batocera: media/art/<имя>-screenshot.jpg (может быть несколько: -screenshot-1, -screenshot-2)
            string artDir = Path.Combine(systemFolder, "media", ArtDir);
            if (Directory.Exists(artDir))
            {
                result.AddRange(Directory
                    .GetFiles(artDir, gameName + "-screenshot*", SearchOption.TopDirectoryOnly)
                    .Where(f => IsImage(f))
                    .OrderBy(f => f));

                // Иногда просто <имя>.jpg в art/
                if (result.Count == 0)
                {
                    result.AddRange(Directory
                        .GetFiles(artDir, gameName + ".*", SearchOption.TopDirectoryOnly)
                        .Where(f => IsImage(f))
                        .OrderBy(f => f));
                }
            }

            // Плоская структура: media/<имя>-01.jpg, -02.jpg
            if (result.Count == 0)
            {
                string mediaDir = Path.Combine(systemFolder, "media");
                if (Directory.Exists(mediaDir))
                {
                    result.AddRange(Directory
                        .GetFiles(mediaDir, gameName + "-*.*", SearchOption.TopDirectoryOnly)
                        .Where(f => IsImage(f))
                        .Where(f => !Path.GetFileName(f).Contains("-boxart") &&
                                    !Path.GetFileName(f).Contains("-wheel") &&
                                    !Path.GetFileName(f).Contains("-fanart") &&
                                    !Path.GetFileName(f).Contains("-manual") &&
                                    !Path.GetFileName(f).Contains("-video"))
                        .OrderBy(f => f));
                }
            }

            return result.Take(10).ToList();
        }

        /// <summary>Найти логотип (wheel).</summary>
        public static string? FindWheel(string systemFolder, string gameName)
        {
            return FindByPattern(systemFolder, gameName, WheelDir, "-wheel",
                new[] { ".png", ".jpg", ".jpeg" });
        }

        /// <summary>Найти видео.</summary>
        public static string? FindVideo(string systemFolder, string gameName)
        {
            return FindByPattern(systemFolder, gameName, VideoDir, "-video",
                new[] { ".mp4", ".mkv", ".webm", ".avi" });
        }

        /// <summary>Найти фон (fanart).</summary>
        public static string? FindFanart(string systemFolder, string gameName)
        {
            return FindByPattern(systemFolder, gameName, FanartDir, "-fanart",
                new[] { ".jpg", ".jpeg", ".png" });
        }

        /// <summary>Найти мануал.</summary>
        public static string? FindManual(string systemFolder, string gameName)
        {
            return FindByPattern(systemFolder, gameName, ManualDir, "-manual",
                new[] { ".pdf" });
        }

        // ---------- Внутреннее ----------

        /// <summary>
        /// Ищет файл в media/&lt;subdir&gt;/ с именем &lt;gameName&gt;&lt;suffix&gt;&lt;ext&gt;.
        /// Если subdir == null — ищет прямо в media/.
        /// </summary>
        private static string? FindByPattern(string systemFolder, string gameName,
            string? subdir, string suffix, string[] extensions)
        {
            string dir = subdir == null
                ? Path.Combine(systemFolder, "media")
                : Path.Combine(systemFolder, "media", subdir);

            if (!Directory.Exists(dir)) return null;

            foreach (var ext in extensions)
            {
                string candidate = Path.Combine(dir, gameName + suffix + ext);
                if (File.Exists(candidate)) return candidate;
            }

            // Регистронезависимый поиск (на случай других расширений или регистра).
            foreach (var ext in extensions)
            {
                var hit = Directory
                    .GetFiles(dir, "*" + ext, SearchOption.TopDirectoryOnly)
                    .FirstOrDefault(f => string.Equals(
                        Path.GetFileNameWithoutExtension(f),
                        gameName + suffix,
                        StringComparison.OrdinalIgnoreCase));
                if (hit != null) return hit;
            }

            return null;
        }

        private static bool IsImage(string path)
        {
            string ext = Path.GetExtension(path).ToLowerInvariant();
            return ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".bmp";
        }
        /// <summary>
        /// Дополняет GameInfo ссылками на существующие медиафайлы системы.
        /// Ничего НЕ перемещает и НЕ удаляет — только читает и заполняет пустые поля.
        /// </summary>
        public static void EnrichGameInfo(GameInfo info, string systemFolder, string gameName)
        {
            if (string.IsNullOrWhiteSpace(info.CoverPath))
            {
                var cover = FindCover(systemFolder, gameName);
                if (cover != null) info.CoverPath = cover;
            }

            if (info.Screenshots.Count == 0)
            {
                var shots = FindScreenshots(systemFolder, gameName);
                if (shots.Count > 0) info.Screenshots.AddRange(shots);
            }

            if (string.IsNullOrWhiteSpace(info.WheelPath))
            {
                var wheel = FindWheel(systemFolder, gameName);
                if (wheel != null) info.WheelPath = wheel;
            }

            if (string.IsNullOrWhiteSpace(info.VideoPath))
            {
                var video = FindVideo(systemFolder, gameName);
                if (video != null) info.VideoPath = video;
            }

            if (string.IsNullOrWhiteSpace(info.FanartPath))
            {
                var fanart = FindFanart(systemFolder, gameName);
                if (fanart != null) info.FanartPath = fanart;
            }

            if (string.IsNullOrWhiteSpace(info.ManualPath))
            {
                var manual = FindManual(systemFolder, gameName);
                if (manual != null) info.ManualPath = manual;
            }
        }
    }
}