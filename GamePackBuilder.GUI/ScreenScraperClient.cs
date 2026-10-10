using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace GamePackBuilder.GUI
{
    /// <summary>
    /// Минимальный HTTP-клиент для ScreenScraper API.
    /// Пока умеет только запрашивать информацию о пользователе и об игре.
    /// </summary>
    internal class ScreenScraperClient
    {
        private static readonly HttpClient Http = CreateHttp();

        private static HttpClient CreateHttp()
        {
            var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            client.DefaultRequestHeaders.Add("User-Agent", "GamePackBuilder/0.1");
            return client;
        }

        /// <summary>Проверка пользователя: возвращает информацию о лимитах.</summary>
        public async Task<ScreenScraperResponse> GetUserInfoAsync(string ssid, string sspassword)
        {
            string url = BuildUrl("ssuserInfos.php", ssid, sspassword);
            return await GetJsonAsync(url);
        }

        /// <summary>Данные игры по имени ROM-файла.</summary>
        public async Task<ScreenScraperResponse> GetGameInfoByNameAsync(
            string ssid, string sspassword, int systemId, string romName)
        {
            string url = BuildUrl("jeuInfos.php", ssid, sspassword) +
                         $"&systemeid={systemId}" +
                         $"&romtype=rom" +
                         $"&romnom={Uri.EscapeDataString(romName)}";
            return await GetJsonAsync(url);
        }

        private static string BuildUrl(string endpoint, string ssid, string sspassword)
        {
            return $"https://api.screenscraper.fr/api2/{endpoint}" +
                   $"?devid={ScreenScraperDevKeys.DevId}" +
                   $"&devpassword={ScreenScraperDevKeys.DevPassword}" +
                   $"&softname={ScreenScraperDevKeys.SoftName}" +
                   $"&output=json" +
                   $"&ssid={Uri.EscapeDataString(ssid)}" +
                   $"&sspassword={Uri.EscapeDataString(sspassword)}";
        }

        private static async Task<ScreenScraperResponse> GetJsonAsync(string url)
        {
            var response = await Http.GetAsync(url);
            string content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"HTTP {(int)response.StatusCode}: {content}");

            var parsed = JsonSerializer.Deserialize<ScreenScraperResponse>(content);
            if (parsed == null)
                throw new Exception("Не удалось разобрать JSON-ответ.");

            if (parsed.Header?.Success != "true")
                throw new Exception("API вернул ошибку: " + (parsed.Header?.Error ?? "неизвестно"));

            return parsed;
        }
    }
}