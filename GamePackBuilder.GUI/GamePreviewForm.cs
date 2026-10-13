using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace GamePackBuilder.GUI
{
    /// <summary>
    /// Немодальное окно предпросмотра игры: название, обложка, скриншоты со стрелками.
    /// Открывается одинарным кликом по игре в главном окне.
    /// </summary>
    public class GamePreviewForm : Form
    {
        private Label _lblTitle = null!;
        private PictureBox _pic = null!;
        private Button _btnPrev = null!;
        private Button _btnNext = null!;
        private Label _lblCounter = null!;

        private readonly List<string> _media = new();
        private int _currentIndex;

        public GamePreviewForm()
        {
            BuildUI();
            ThemeManager.Apply(this);
            KeyPreview = true;
            KeyDown += (_, e) =>
            {
                if (e.KeyCode == Keys.Left) { ShowPrev(); e.Handled = true; }
                if (e.KeyCode == Keys.Right) { ShowNext(); e.Handled = true; }
                if (e.KeyCode == Keys.Escape) Close();
            };
        }

        private void BuildUI()
        {
            Text = "Просмотр игры";
            Size = new Size(760, 600);
            StartPosition = FormStartPosition.Manual;
            MinimumSize = new Size(520, 420);
            Font = new Font("Segoe UI", 10F);

            // ---- Заголовок ----
            _lblTitle = new Label
            {
                Dock = DockStyle.Top,
                Height = 40,
                Padding = new Padding(12, 8, 12, 0),
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                AutoEllipsis = true,
                Text = ""
            };

            // ---- Низ: счётчик + стрелки ----
            var bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 56,
                Padding = new Padding(12, 8, 12, 8)
            };

            _btnPrev = new Button
            {
                Text = "◀",
                Width = 60,
                Height = 40,
                Font = new Font("Segoe UI", 14F),
                Dock = DockStyle.Left
            };
            _btnPrev.Click += (_, _) => ShowPrev();

            _btnNext = new Button
            {
                Text = "▶",
                Width = 60,
                Height = 40,
                Font = new Font("Segoe UI", 14F),
                Dock = DockStyle.Right
            };
            _btnNext.Click += (_, _) => ShowNext();

            _lblCounter = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 11F),
                Text = "0 / 0"
            };

            bottomPanel.Controls.Add(_lblCounter);
            bottomPanel.Controls.Add(_btnNext);
            bottomPanel.Controls.Add(_btnPrev);

            // ---- Картинка ----
            _pic = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(250, 250, 250)
            };

            Controls.Add(_pic);
            Controls.Add(bottomPanel);
            Controls.Add(_lblTitle);
        }

        /// <summary>Установить содержимое окна. Вызывается из MainForm при клике по игре.</summary>
        public void SetContent(string title, List<string> mediaPaths)
        {
            Text = "Просмотр: " + title;
            _lblTitle.Text = title;

            _media.Clear();
            foreach (var p in mediaPaths)
                if (!string.IsNullOrWhiteSpace(p) && File.Exists(p))
                    _media.Add(p);

            _currentIndex = 0;
            ShowCurrent();
        }

        private void ShowPrev()
        {
            if (_media.Count == 0) return;
            _currentIndex = (_currentIndex - 1 + _media.Count) % _media.Count;
            ShowCurrent();
        }

        private void ShowNext()
        {
            if (_media.Count == 0) return;
            _currentIndex = (_currentIndex + 1) % _media.Count;
            ShowCurrent();
        }

        private void ShowCurrent()
        {
            // Счётчик и доступность стрелок.
            bool hasMultiple = _media.Count > 1;
            _btnPrev.Enabled = hasMultiple;
            _btnNext.Enabled = hasMultiple;

            if (_media.Count == 0)
            {
                _lblCounter.Text = "Нет медиа";
                ClearImage();
                return;
            }

            _lblCounter.Text = $"{_currentIndex + 1} / {_media.Count}";

            ClearImage();
            try
            {
                using var fs = new FileStream(_media[_currentIndex], FileMode.Open, FileAccess.Read);
                _pic.Image = Image.FromStream(fs);
            }
            catch
            {
                _lblCounter.Text = $"{_currentIndex + 1} / {_media.Count} (не удалось открыть)";
            }
        }

        private void ClearImage()
        {
            if (_pic.Image != null)
            {
                var old = _pic.Image;
                _pic.Image = null;
                old.Dispose();
            }
        }

        private void InitializeComponent()
        {

        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            // Позиционируем окно: по центру главного окна по горизонтали,
            // и на 60 px от его верха по вертикали.
            if (Owner != null)
            {
                int x = Owner.Left + (Owner.Width - Width) / 2;
                int y = Owner.Top + 60;

                // Не даём уехать за пределы экрана.
                var screen = Screen.FromControl(Owner).WorkingArea;
                if (x < screen.Left) x = screen.Left;
                if (y < screen.Top) y = screen.Top;
                if (x + Width > screen.Right) x = screen.Right - Width;
                if (y + Height > screen.Bottom) y = screen.Bottom - Height;

                Location = new Point(x, y);
            }
        }
    }
}