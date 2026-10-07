using System.Drawing;
using System.Windows.Forms;

namespace GamePackBuilder.GUI
{
    /// <summary>
    /// Применяет палитру темы рекурсивно ко всем контролам формы.
    /// </summary>
    public static class ThemeManager
    {
        /// <summary>Текущая активная тема.</summary>
        public static Theme Current { get; set; } = Theme.Light;

        /// <summary>Загрузить тему из настроек.</summary>
        public static void LoadFromSettings()
        {
            var s = AppSettings.Load();
            Current = Theme.ByName(s.Theme);
        }

        /// <summary>Переключить тему и сохранить в настройках.</summary>
        public static void Toggle()
        {
            Current = Current.Name == "dark" ? Theme.Light : Theme.Dark;

            var s = AppSettings.Load();
            s.Theme = Current.Name;
            s.Save();
        }

        /// <summary>
        /// Рекурсивно применяет тему к контролу и всем его детям.
        /// </summary>
        public static void Apply(Control root)
        {
            ApplyToControl(root);

            foreach (Control child in root.Controls)
                Apply(child);
        }

        private static void ApplyToControl(Control c)
        {
            var t = Current;

            if (c is Form f)
            {
                f.BackColor = t.FormBackground;
                f.ForeColor = t.ControlText;
                return;
            }

            if (c is Panel p)
            {
                // Панели внизу/сверху — используем PanelBackground
                p.BackColor = t.PanelBackground;
                p.ForeColor = t.ControlText;
                return;
            }

            if (c is TableLayoutPanel tlp)
            {
                tlp.BackColor = Color.Transparent;
                tlp.ForeColor = t.ControlText;
                return;
            }

            if (c is FlowLayoutPanel flp)
            {
                flp.BackColor = Color.Transparent;
                flp.ForeColor = t.ControlText;
                return;
            }

            if (c is Label lbl)
            {
                // Сохраняем красный/зелёный статус
                if (lbl.ForeColor != Color.Red &&
                    lbl.ForeColor != Color.Green &&
                    lbl.ForeColor != Color.OrangeRed &&
                    lbl.ForeColor != Color.DodgerBlue &&
                    lbl.ForeColor != Color.Green)
                {
                    lbl.ForeColor = t.ControlText;
                }
                lbl.BackColor = Color.Transparent;
                return;
            }

            if (c is TextBox tb)
            {
                tb.BackColor = t.ControlBackground;
                tb.ForeColor = t.ControlText;
                tb.BorderStyle = tb.ReadOnly ? BorderStyle.None : BorderStyle.FixedSingle;
                return;
            }

            if (c is Button btn)
            {
                btn.FlatStyle = FlatStyle.Flat;
                btn.FlatAppearance.BorderColor = t.BorderColor;
                btn.FlatAppearance.BorderSize = 1;
                btn.BackColor = t.ControlBackground;
                btn.ForeColor = t.ControlText;
                btn.UseVisualStyleBackColor = false;
                return;
            }

            if (c is ListBox lb)
            {
                lb.BackColor = t.ControlBackground;
                lb.ForeColor = t.ControlText;
                lb.BorderStyle = BorderStyle.FixedSingle;
                return;
            }

            if (c is CheckBox cb)
            {
                cb.BackColor = Color.Transparent;
                cb.ForeColor = t.ControlText;
                cb.UseVisualStyleBackColor = false;
                return;
            }

            if (c is ProgressBar)
            {
                // Прогресс-бар оставляем системным
                return;
            }

            if (c is TabControl tab)
            {
                tab.BackColor = t.PanelBackground;
                tab.ForeColor = t.ControlText;
                return;
            }

            if (c is TabPage page)
            {
                page.BackColor = t.FormBackground;
                page.ForeColor = t.ControlText;
                return;
            }

            if (c is PictureBox pic)
            {
                pic.BackColor = t.ControlBackground;
                return;
            }

            if (c is ListView lv)
            {
                lv.BackColor = t.ControlBackground;
                lv.ForeColor = t.ControlText;
                lv.BorderStyle = BorderStyle.FixedSingle;
                return;
            }

            if (c is DataGridView dgv)
            {
                dgv.BackgroundColor = t.ControlBackground;
                dgv.ForeColor = t.ControlText;
                dgv.GridColor = t.BorderColor;
                dgv.DefaultCellStyle.BackColor = t.ControlBackground;
                dgv.DefaultCellStyle.ForeColor = t.ControlText;
                dgv.DefaultCellStyle.SelectionBackColor = t.Accent;
                dgv.DefaultCellStyle.SelectionForeColor = Color.White;
                dgv.ColumnHeadersDefaultCellStyle.BackColor = t.PanelBackground;
                dgv.ColumnHeadersDefaultCellStyle.ForeColor = t.ControlText;
                dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = t.PanelBackground;
                dgv.EnableHeadersVisualStyles = false;
                return;
            }

            // Прочие — просто пробуем
            try
            {
                c.BackColor = t.ControlBackground;
                c.ForeColor = t.ControlText;
            }
            catch { }
        }
    }
}