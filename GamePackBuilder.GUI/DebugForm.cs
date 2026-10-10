using System;
using System.Drawing;
using System.Windows.Forms;

namespace GamePackBuilder.GUI
{
    /// <summary>
    /// Окно отладки. Открывается по Ctrl+Shift+D в главном окне.
    /// Показывает лог и позволяет запустить тестовые запросы.
    /// </summary>
    public class DebugForm : Form
    {
        private TextBox _txtLog = null!;

        public DebugForm()
        {
            BuildUI();
            RefreshLog();
            ThemeManager.Apply(this);
        }

        private void BuildUI()
        {
            Text = "Отладка";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(800, 500);
            MinimumSize = new Size(600, 400);

            _txtLog = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Both,
                WordWrap = false,
                ReadOnly = true,
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 9F)
            };

            var panelTop = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 40,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(6)
            };

            var btnRefresh = new Button { Text = "Обновить", Width = 100, Height = 28 };
            btnRefresh.Click += (_, _) => RefreshLog();

            var btnClear = new Button { Text = "Очистить лог", Width = 120, Height = 28 };
            btnClear.Click += (_, _) =>
            {
                DebugLog.Clear();
                RefreshLog();
            };

            var btnTestGame = new Button { Text = "Тест: Sonic 2 (Mega Drive)", Width = 200, Height = 28 };
            btnTestGame.Click += async (_, _) => await RunTestGameAsync();

            var btnOpenFile = new Button { Text = "Открыть файл лога", Width = 150, Height = 28 };
            btnOpenFile.Click += (_, _) =>
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = DebugLog.LogFilePath,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            panelTop.Controls.Add(btnRefresh);
            panelTop.Controls.Add(btnClear);
            panelTop.Controls.Add(btnTestGame);
            panelTop.Controls.Add(btnOpenFile);

            Controls.Add(_txtLog);
            Controls.Add(panelTop);
        }

        private void RefreshLog()
        {
            _txtLog.Text = DebugLog.ReadAll();
            _txtLog.SelectionStart = _txtLog.TextLength;
            _txtLog.ScrollToCaret();
        }

        private async System.Threading.Tasks.Task RunTestGameAsync()
        {
            string user = AppSettings.Load().ScreenScraperUser;
            string pass = AppSettings.Load().ScreenScraperPassword;

            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
            {
                MessageBox.Show(this,
                    "Сначала сохрани логин и пароль ScreenScraper в настройках.",
                    "Нет данных", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DebugLog.Write("=== Тест: jeuInfos (Sonic The Hedgehog 2) ===");
            RefreshLog();

            try
            {
                var client = new ScreenScraperClient();
                var response = await client.GetGameInfoByNameAsync(
                    user, pass,
                    systemId: 1,
                    romName: "Sonic The Hedgehog 2 (World).zip");

                var g = response.Response?.Game;
                if (g == null)
                {
                    DebugLog.Write("Игра не найдена.");
                }
                else
                {
                    DebugLog.Write($"ID:            {g.Id}");
                    DebugLog.Write($"Названий:      {g.Names?.Count ?? 0}");
                    DebugLog.Write($"Название[0]:   {g.Names?[0].Text}");
                    DebugLog.Write($"Система:       {g.System?.Text}");
                    DebugLog.Write($"Издатель:      {g.Publisher?.Text}");
                    DebugLog.Write($"Разработчик:   {g.Developer?.Text}");
                    DebugLog.Write($"Игроков:       {g.Players?.Text}");
                    DebugLog.Write($"Жанров:        {g.Genres?.Count ?? 0}");
                    DebugLog.Write($"Жанр[0] EN:    {g.Genres?[0].Names?.Find(n => n.Language == "en")?.Text}");
                    DebugLog.Write($"Описаний:      {g.Descriptions?.Count ?? 0}");
                    DebugLog.Write($"Дат:           {g.Dates?.Count ?? 0}");
                    DebugLog.Write($"Дата EU:       {g.Dates?.Find(d => d.Region == "eu")?.Text}");
                    DebugLog.Write($"Медиа:         {g.Medias?.Count ?? 0}");
                    DebugLog.Write($"ROM-ов:        {g.Roms?.Count ?? 0}");
                    DebugLog.Write($"ROM[0]:        {g.Roms?[0].FileName} (CRC {g.Roms?[0].Crc})");
                }

                DebugLog.Write("=== Тест завершён ===");
            }
            catch (Exception ex)
            {
                DebugLog.Write("ОШИБКА: " + ex.Message);
            }

            RefreshLog();
        }
    }
}