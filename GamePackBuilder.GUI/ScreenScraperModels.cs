using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GamePackBuilder.GUI
{
    // ============================================================
    // Модели для десериализации JSON-ответов ScreenScraper API.
    // Имена C#-свойств — наши, а JsonPropertyName связывает их
    // с реальными полями в JSON (там французские названия).
    // ============================================================

    /// <summary>Корневой ответ API: header + response.</summary>
    public class ScreenScraperResponse
    {
        [JsonPropertyName("header")]
        public ScreenScraperHeader? Header { get; set; }

        [JsonPropertyName("response")]
        public ScreenScraperBody? Response { get; set; }
    }

    /// <summary>Блок header — статус ответа.</summary>
    public class ScreenScraperHeader
    {
        [JsonPropertyName("success")]
        public string? Success { get; set; }

        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }

    /// <summary>Блок response — внутри jeu и/или ssuser.</summary>
    public class ScreenScraperBody
    {
        [JsonPropertyName("jeu")]
        public ScreenScraperGame? Game { get; set; }

        [JsonPropertyName("ssuser")]
        public ScreenScraperUser? User { get; set; }
    }

    /// <summary>Информация о пользователе (лимиты, квота).</summary>
    public class ScreenScraperUser
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("niveau")]
        public string? Level { get; set; }

        [JsonPropertyName("maxthreads")]
        public string? MaxThreads { get; set; }

        [JsonPropertyName("requeststoday")]
        public string? RequestsToday { get; set; }

        [JsonPropertyName("maxrequestsperday")]
        public string? MaxRequestsPerDay { get; set; }

        [JsonPropertyName("maxrequestspermin")]
        public string? MaxRequestsPerMin { get; set; }
    }

    /// <summary>Игра целиком — то, что лежит в блоке "jeu".</summary>
    public class ScreenScraperGame
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("noms")]
        public List<ScreenScraperLocalizedText>? Names { get; set; }

        [JsonPropertyName("systeme")]
        public ScreenScraperIdText? System { get; set; }

        [JsonPropertyName("editeur")]
        public ScreenScraperIdText? Publisher { get; set; }

        [JsonPropertyName("developpeur")]
        public ScreenScraperIdText? Developer { get; set; }

        [JsonPropertyName("joueurs")]
        public ScreenScraperText? Players { get; set; }

        [JsonPropertyName("synopsis")]
        public List<ScreenScraperLocalizedText>? Descriptions { get; set; }

        [JsonPropertyName("dates")]
        public List<ScreenScraperLocalizedText>? Dates { get; set; }

        [JsonPropertyName("genres")]
        public List<ScreenScraperGenre>? Genres { get; set; }

        [JsonPropertyName("medias")]
        public List<ScreenScraperMedia>? Medias { get; set; }

        [JsonPropertyName("roms")]
        public List<ScreenScraperRom>? Roms { get; set; }
    }

    /// <summary>Просто "text" — например, число игроков.</summary>
    public class ScreenScraperText
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }

    /// <summary>"id" + "text" — например, система, издатель, разработчик.</summary>
    public class ScreenScraperIdText
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }

    /// <summary>Локализованный текст: регион/язык + сам текст.</summary>
    public class ScreenScraperLocalizedText
    {
        [JsonPropertyName("region")]
        public string? Region { get; set; }

        [JsonPropertyName("langue")]
        public string? Language { get; set; }

        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }

    /// <summary>Жанр — у него вложенный список переводов.</summary>
    public class ScreenScraperGenre
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("noms")]
        public List<ScreenScraperLocalizedText>? Names { get; set; }
    }

    /// <summary>Медиа: обложка, скриншот, видео и т.д.</summary>
    public class ScreenScraperMedia
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("parent")]
        public string? Parent { get; set; }

        [JsonPropertyName("region")]
        public string? Region { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("format")]
        public string? Format { get; set; }

        [JsonPropertyName("size")]
        public string? Size { get; set; }
    }

    /// <summary>ROM-файл с хешами — для поиска по CRC/MD5.</summary>
    public class ScreenScraperRom
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("romfilename")]
        public string? FileName { get; set; }

        [JsonPropertyName("romsize")]
        public string? Size { get; set; }

        [JsonPropertyName("romcrc")]
        public string? Crc { get; set; }

        [JsonPropertyName("rommd5")]
        public string? Md5 { get; set; }

        [JsonPropertyName("romsha1")]
        public string? Sha1 { get; set; }
    }
}