using System.Drawing;
using System.Windows.Forms;

namespace GamePackBuilder.GUI
{
    /// <summary>
    /// Кнопка в плоском стиле: иконка слева, текст по центру.
    /// Решает баг WinForms, когда при FlatStyle.Flat иконка «прилипает» к низу.
    /// Поддерживает hover-эффект.
    /// </summary>
    public class FlatIconButton : Button
    {
        private Image? _icon;
        private bool _hovered;
        private bool _pressed;

        public FlatIconButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint
                   | ControlStyles.UserPaint
                   | ControlStyles.OptimizedDoubleBuffer
                   | ControlStyles.ResizeRedraw, true);

            FlatStyle = FlatStyle.Flat;
            UseVisualStyleBackColor = false;
        }

        /// <summary>Иконка слева от текста (16×16 обычно).</summary>
        public Image? Icon
        {
            get => _icon;
            set { _icon = value; Invalidate(); }
        }

        protected override void OnMouseEnter(System.EventArgs e)
        {
            base.OnMouseEnter(e);
            _hovered = true;
            Invalidate();
        }

        protected override void OnMouseLeave(System.EventArgs e)
        {
            base.OnMouseLeave(e);
            _hovered = false;
            _pressed = false;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs mevent)
        {
            base.OnMouseDown(mevent);
            _pressed = true;
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs mevent)
        {
            base.OnMouseUp(mevent);
            _pressed = false;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // Цвет фона с учётом состояния.
            // Определяем, тёмная ли у нас кнопка — тогда осветляем при hover,
            // иначе затемняем. Это работает и в тёмной, и в светлой теме.
            bool isDark = (BackColor.R * 0.299 + BackColor.G * 0.587 + BackColor.B * 0.114) < 128;
            Color hoverTarget = isDark ? Color.White : Color.Black;
            Color pressTarget = isDark ? Color.White : Color.Black;

            Color bg = BackColor;
            if (!Enabled)
                bg = BlendColors(BackColor, Color.Gray, 0.3f);
            else if (_pressed)
                bg = BlendColors(BackColor, pressTarget, isDark ? 0.25f : 0.15f);
            else if (_hovered)
                bg = BlendColors(BackColor, hoverTarget, isDark ? 0.15f : 0.08f);

            using (var back = new SolidBrush(bg))
                g.FillRectangle(back, ClientRectangle);

            // Граница.
            using (var pen = new Pen(FlatAppearance.BorderColor, 1))
                g.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);

            // Медиа: иконка + текст.
            int padding = 4;
            int iconW = _icon?.Width ?? 0;
            int iconH = _icon?.Height ?? 0;

            var textSize = g.MeasureString(Text, Font);
            int totalW = iconW + (iconW > 0 ? padding : 0) + (int)textSize.Width;
            int startX = (Width - totalW) / 2;

            if (_icon != null)
            {
                int iconY = (Height - iconH) / 2;
                g.DrawImage(_icon, startX, iconY, iconW, iconH);
                startX += iconW + padding;
            }

            int textY = (Height - (int)textSize.Height) / 2;
            Color textColor = Enabled ? ForeColor : SystemColors.GrayText;

            using var textBrush = new SolidBrush(textColor);
            g.DrawString(Text, Font, textBrush, startX, textY);

            if (Focused)
            {
                var rect = new Rectangle(1, 1, Width - 3, Height - 3);
                using var focusPen = new Pen(Color.FromArgb(80, 120, 200), 1)
                {
                    DashStyle = System.Drawing.Drawing2D.DashStyle.Dot
                };
                g.DrawRectangle(focusPen, rect);
            }
        }

        /// <summary>Смешать два цвета в указанной пропорции (0..1).</summary>
        private static Color BlendColors(Color a, Color b, float t)
        {
            if (t < 0) t = 0;
            if (t > 1) t = 1;

            return Color.FromArgb(
                a.A,
                (int)(a.R + (b.R - a.R) * t),
                (int)(a.G + (b.G - a.G) * t),
                (int)(a.B + (b.B - a.B) * t));
        }
    }
}