using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GamePackBuilder.GUI
{
    /// <summary>
    /// Настройки приложения. Сохраняются в Data\config.json (портативно)
    /// или в %LOCALAPPDATA%\GamePackBuilder\config.json (если рядом с exe нет прав на запись).
    /// </summary>
    public class AppSettings
    {
        /// <summary>Откуда берём игры (например, N:\3.Console\Games\)</summary>
        public string SourcePath { get; set; } = "";

        /// <summary>Куда копируем готовый пак (например, E:\_Pack_\ или F:\EASYROMS\roms\)</summary>
        public string TargetPath { get; set; } = "";

        /// <summary>Тема приложения: "light" или "dark".</summary>
        public string Theme { get; set; } = "light";

        /// <summary>Логин пользователя на screenscraper.fr (ssid).</summary>
        public string ScreenScraperUser { get; set; } = "";

        /// <summary>
        /// Зашифрованный пароль пользователя. Именно это поле лежит в config.json.
        /// Расшифровывается в ScreenScraperPassword при загрузке.
        /// </summary>
        public string ScreenScraperPasswordEncrypted { get; set; } = "";

        /// <summary>
        /// Пароль пользователя в открытом виде — только в памяти.
        /// НЕ сериализуется в JSON.
        /// </summary>
        [JsonIgnore]
        public string ScreenScraperPassword { get; set; } = "";

        // ---------- Внутреннее ----------

        private static string ConfigDir => AppPaths.DataDir;

        private static string ConfigFile =>
            Path.Combine(ConfigDir, "config.json");

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true
        };

        /// <summary>Загрузить настройки из файла. Если файла нет — вернёт пустой объект.</summary>
        public static AppSettings Load()
        {
            try
            {
                if (!File.Exists(ConfigFile))
                    return new AppSettings();

                string json = File.ReadAllText(ConfigFile);
                var settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();

                // Расшифровываем пароль в память.
                settings.ScreenScraperPassword = SecretProtector.Unprotect(settings.ScreenScraperPasswordEncrypted);

                return settings;
            }
            catch
            {
                // Если файл битый — не падаем, возвращаем пустые настройки
                return new AppSettings();
            }
        }

        /// <summary>Сохранить настройки в файл.</summary>
        public void Save()
        {
            try
            {
                Directory.CreateDirectory(ConfigDir);

                // Шифруем пароль перед записью.
                ScreenScraperPasswordEncrypted = SecretProtector.Protect(ScreenScraperPassword);

                string json = JsonSerializer.Serialize(this, JsonOptions);
                File.WriteAllText(ConfigFile, json);
            }
            catch (Exception ex)
            {
                throw new IOException($"Не удалось сохранить настройки в {ConfigFile}: {ex.Message}", ex);
            }
        }

        /// <summary>Полный путь к файлу конфига (для отладки / отображения).</summary>
        public static string GetConfigFilePath() => ConfigFile;
    }
}