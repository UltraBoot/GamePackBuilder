using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GamePackBuilder.GUI
{
    /// <summary>
    /// Информация об одной игре, которую ведёт пользователь.
    /// </summary>
    public class GameInfo
    {
        public string Title { get; set; } = "";
        public string Year { get; set; } = "";
        public string Developer { get; set; } = "";
        public string Genre { get; set; } = "";
        public string Players { get; set; } = "";
        public string Description { get; set; } = "";

        /// <summary>Путь к локальному файлу обложки (jpg/png). Может быть пустым.</summary>
        public string CoverPath { get; set; } = "";

        /// <summary>Пути к локальным скриншотам.</summary>
        public List<string> Screenshots { get; set; } = new();
        /// <summary>
        /// Имена полей, которые были заполнены автоматически с ScreenScraper.
        /// При повторном скрапинге эти поля обновляются, а правки пользователя — нет.
        /// Пустой список означает, что все данные введены вручную.
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
            Screenshots.Count == 0;
    }
}