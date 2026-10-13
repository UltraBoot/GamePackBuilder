using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;

namespace GamePackBuilder.GUI
{
    /// <summary>
    /// Загрузчик иконок приложения из встроенных ресурсов (embedded resources).
    /// PNG-файлы из папки Resources зашиты прямо в exe.
    /// Иконки кэшируются в памяти, при загрузке автоматически уменьшаются под нужный размер.
    /// </summary>
    internal static class AppIcons
    {
        // Размер по умолчанию для кнопок — 16×16 (соответствует системным).
        private const int DefaultIconSize = 16;

        private static readonly Dictionary<string, Image> _cache = new();

        /// <summary>
        /// Загрузить иконку по имени файла без расширения.
        /// Например: Get("folder") → Resources\folder.png.
        /// Картинка уменьшается до 16×16.
        /// </summary>
        public static Image? Get(string name) => Get(name, DefaultIconSize);

        /// <summary>
        /// Загрузить иконку указанного размера.
        /// </summary>
        public static Image? Get(string name, int size)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;

            string cacheKey = name + "@" + size;

            if (_cache.TryGetValue(cacheKey, out var cached))
                return cached;

            try
            {
                var asm = Assembly.GetExecutingAssembly();

                string? resourceName = asm.GetManifestResourceNames()
                    .FirstOrDefault(n =>
                        n.EndsWith("Resources." + name + ".png", StringComparison.OrdinalIgnoreCase));

                if (resourceName == null)
                {
                    DebugLog.Write($"AppIcons: не найден ресурс {name}.png");
                    return null;
                }

                using var stream = asm.GetManifestResourceStream(resourceName);
                if (stream == null) return null;

                using var ms = new MemoryStream();
                stream.CopyTo(ms);
                ms.Position = 0;

                using var original = Image.FromStream(ms);

                // Уменьшаем до нужного размера с сохранением пропорций.
                var resized = ResizeImage(original, size);

                _cache[cacheKey] = resized;
                return resized;
            }
            catch (Exception ex)
            {
                DebugLog.Write($"AppIcons: ошибка загрузки {name}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Уменьшить картинку до квадрата size × size. Пропорции сохраняются,
        /// картинка центрируется в квадрате, фон прозрачный.
        /// </summary>
        private static Bitmap ResizeImage(Image src, int size)
        {
            var bmp = new Bitmap(size, size);
            using var g = Graphics.FromImage(bmp);

            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
            g.Clear(Color.Transparent);

            // Сохраняем пропорции.
            float scale = Math.Min((float)size / src.Width, (float)size / src.Height);
            int w = (int)(src.Width * scale);
            int h = (int)(src.Height * scale);
            int x = (size - w) / 2;
            int y = (size - h) / 2;

            g.DrawImage(src, x, y, w, h);
            return bmp;
        }
    }
}