using System;
using System.IO;
using System.Text.Json;

namespace GamePackBuilder.GUI
{
    /// <summary>
    /// Настройки приложения. Сохраняются в %LOCALAPPDATA%\GamePackBuilder\config.json
    /// </summary>
    public class AppSettings
    {
        /// <summary>Откуда берём игры (например, N:\3.Console\Games\)</summary>
        public string SourcePath { get; set; } = "";

        /// <summary>Куда копируем готовый пак (например, E:\_Pack_\ или F:\EASYROMS\roms\)</summary>
        public string TargetPath { get; set; } = "";

        /// <summary>Тема приложения: "light" или "dark".</summary>
        public string Theme { get; set; } = "light";

        // ---------- Внутреннее ----------

        private static string ConfigDir =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                         "GamePackBuilder");

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
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
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