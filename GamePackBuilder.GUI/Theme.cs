using System.Drawing;

namespace GamePackBuilder.GUI
{
    /// <summary>
    /// Набор цветов для одной темы.
    /// </summary>
    public class Theme
    {
        public string Name { get; init; } = "light";

        public Color FormBackground { get; init; }
        public Color PanelBackground { get; init; }
        public Color ControlBackground { get; init; }
        public Color ControlText { get; init; }
        public Color BorderColor { get; init; }
        public Color DisabledText { get; init; }
        public Color Accent { get; init; }
        public Color StatusBackground { get; init; }
        public Color HeaderBackground { get; init; }

        /// <summary>Светлая тема (по умолчанию).</summary>
        public static Theme Light => new()
        {
            Name = "light",
            FormBackground = Color.White,
            PanelBackground = Color.FromArgb(245, 245, 245),
            ControlBackground = Color.White,
            ControlText = Color.Black,
            BorderColor = Color.FromArgb(200, 200, 200),
            DisabledText = Color.Gray,
            Accent = Color.FromArgb(0, 120, 215),
            StatusBackground = Color.FromArgb(240, 240, 240),
            HeaderBackground = Color.FromArgb(245, 245, 245)
        };

        /// <summary>Тёмная тема.</summary>
        public static Theme Dark => new()
        {
            Name = "dark",
            FormBackground = Color.FromArgb(30, 30, 30),
            PanelBackground = Color.FromArgb(37, 37, 38),
            ControlBackground = Color.FromArgb(45, 45, 48),
            ControlText = Color.FromArgb(224, 224, 224),
            BorderColor = Color.FromArgb(63, 63, 70),
            DisabledText = Color.FromArgb(150, 150, 150),
            Accent = Color.FromArgb(0, 122, 204),
            StatusBackground = Color.FromArgb(37, 37, 38),
            HeaderBackground = Color.FromArgb(37, 37, 38)
        };

        public static Theme ByName(string? name) =>
            string.Equals(name, "dark", System.StringComparison.OrdinalIgnoreCase)
                ? Dark
                : Light;
    }
}