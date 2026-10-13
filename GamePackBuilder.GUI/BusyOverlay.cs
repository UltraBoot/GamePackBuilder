using System.Drawing;
using System.Windows.Forms;

namespace GamePackBuilder.GUI
{
    /// <summary>
    /// Полупрозрачный оверлей поверх формы: сообщение + бегающий прогресс-бар.
    /// Показывает, что программа занята — и блокирует клики.
    /// </summary>
    public class BusyOverlay : Panel
    {
        private Label _lblMessage = null!;
        private ProgressBar _progress = null!;

        public BusyOverlay()
        {
            Dock = DockStyle.Fill;
            BackColor = Color.FromArgb(180, 30, 30, 30);
            Visible = false;

            _lblMessage = new Label
            {
                Text = "Подождите...",
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };

            _progress = new ProgressBar
            {
                Style = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 20,
                Height = 22,
                Width = 300,
                Dock = DockStyle.None
            };

            var innerPanel = new TableLayoutPanel
            {
                ColumnCount = 1,
                RowCount = 2,
                AutoSize = true,
                BackColor = Color.Transparent,
                Dock = DockStyle.Fill
            };
            innerPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            innerPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            innerPanel.Controls.Add(_lblMessage, 0, 0);

            var progressWrap = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            progressWrap.Controls.Add(_progress);
            progressWrap.Resize += (_, _) =>
            {
                _progress.Left = (progressWrap.Width - _progress.Width) / 2;
                _progress.Top = 0;
            };
            innerPanel.Controls.Add(progressWrap, 0, 1);

            Controls.Add(innerPanel);
        }

        public void Show(string message)
        {
            _lblMessage.Text = message;
            Visible = true;
            BringToFront();
        }

        public void Hide()
        {
            Visible = false;
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // Прозрачный чёрный фон.
            using var brush = new SolidBrush(BackColor);
            e.Graphics.FillRectangle(brush, ClientRectangle);
        }
    }
}