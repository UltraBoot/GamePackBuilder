namespace GamePackBuilder.GUI
{
    /// <summary>
    /// Набор того, что качать с ScreenScraper.
    /// </summary>
    public class MediaDownloadOptions
    {
        public bool Boxart { get; set; } = true;
        public bool Screenshots { get; set; } = true;
        public bool Wheel { get; set; }
        public bool Fanart { get; set; }
        public bool Video { get; set; }
        public bool Manual { get; set; }

        /// <summary>Профиль «Портативная консоль (ArkOS / RG52)» — минимум медиа.</summary>
        public static MediaDownloadOptions ForPortable() => new()
        {
            Boxart = true,
            Screenshots = true,
            Wheel = false,
            Fanart = false,
            Video = false,
            Manual = false
        };

        /// <summary>Профиль «Batocera / мини-ПК» — всё, кроме PDF.</summary>
        public static MediaDownloadOptions ForBatocera() => new()
        {
            Boxart = true,
            Screenshots = true,
            Wheel = true,
            Fanart = true,
            Video = true,
            Manual = false
        };

        /// <summary>Профиль «Полный набор» — всё, включая PDF.</summary>
        public static MediaDownloadOptions ForFull() => new()
        {
            Boxart = true,
            Screenshots = true,
            Wheel = true,
            Fanart = true,
            Video = true,
            Manual = true
        };

        public bool IsNone =>
            !Boxart && !Screenshots && !Wheel && !Fanart && !Video && !Manual;
    }
}