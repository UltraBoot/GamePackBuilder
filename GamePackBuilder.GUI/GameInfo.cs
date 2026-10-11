using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GamePackBuilder.GUI
{
    /// <summary>
    /// Информация об одной игре. Пути к медиа хранятся отдельно для каждого типа.
    /// </summary>
    public class GameInfo
    {
        public string Title { get; set; } = "";
        public string Year { get; set; } = "";
        public string Developer { get; set; } = "";
        public string Genre { get; set; } = "";
        public string Players { get; set; } = "";
        public string Description { get; set; } = "";

        // ---------- Медиа ----------

        /// <summary>Путь к обложке (boxart).</summary>
        public string CoverPath { get; set; } = "";

        /// <summary>Пути к скриншотам (screenshot / art).</summary>
        public List<string> Screenshots { get; set; } = new();

        /// <summary>Путь к логотипу (wheel).</summary>
        public string WheelPath { get; set; } = "";

        /// <summary>Путь к видео.</summary>
        public string VideoPath { get; set; } = "";

        /// <summary>Путь к фону (fanart).</summary>
        public string FanartPath { get; set; } = "";

        /// <summary>Путь к мануалу (PDF).</summary>
        public string ManualPath { get; set; } = "";

        /// <summary>
        /// Имена полей, которые были заполнены автоматически с ScreenScraper.
        /// При повторном скрапинге эти поля обновляются, а правки пользователя — нет.
        /// </summary>
        public List<string> ScraperFields { get; set; } = new();

        [JsonIgnore]
        public bool IsEmpty =>
            string.IsNullOrWhiteSpace(Title) &&
            string.IsNullOrWhiteSpace(Year) &&
            string.IsNullOrWhiteSpace(Developer) &&
            string.IsNullOrWhiteSpace(Genre) &&
            string.IsNullOrWhiteSpace(Players) &&
            string.IsNullOrWhiteSpace(Description) &&
            string.IsNullOrWhiteSpace(CoverPath) &&
            Screenshots.Count == 0 &&
            string.IsNullOrWhiteSpace(WheelPath) &&
            string.IsNullOrWhiteSpace(VideoPath) &&
            string.IsNullOrWhiteSpace(FanartPath) &&
            string.IsNullOrWhiteSpace(ManualPath);
    }
}