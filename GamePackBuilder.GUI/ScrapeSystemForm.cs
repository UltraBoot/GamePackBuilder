using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GamePackBuilder.GUI
{
    /// <summary>
    /// Окно массового скрапинга системы: проходит по всем играм и качает данные с ScreenScraper.
    /// </summary>
    public class ScrapeSystemForm : Form
    {
        private Label _lblTitle = null!;
        private Label _lblCurrent = null!;
        private Label _lblStats = null!;
        private ProgressBar _progress = null!;
        private Button _btnCancel = null!;

        private readonly GameSystem _system;
        private readonly SystemScanResult _scanResult;
        private readonly string _user;
        private readonly string _password;
        private readonly CancellationTokenSource _cts = new();
        private readonly MediaDownloadOptions _options;

        // Статистика
        private int _total;
        private int _processed;
        private int _filledGames;
        private int _mediaDownloaded;
        private int _notFound;
        private int _errors;

        public bool Completed { get; private set; }

        public ScrapeSystemForm(
        GameSystem system,
        SystemScanResult scanResult,
        string user,
        string password,
        MediaDownloadOptions options)
        {
            _system = system;
            _scanResult = scanResult;
            _user = user;
            _password = password;
            _options = options;

            BuildUI();
            ThemeManager.Apply(this);
        }

        private void BuildUI()
        {
            Text = "Скачивание с ScreenScraper — " + _system.DisplayName;
            Size = new Size(560, 260);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            _lblTitle = new Label
            {
                Text = $"Система: {_system.DisplayName}",
                Location = new Point(16, 16),
                AutoSize = true,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold)
            };

            _lblCurrent = new Label
            {
                Text = "Подготовка...",
                Location = new Point(16, 48),
                Size = new Size(520, 22),
                AutoEllipsis = true
            };

            _progress = new ProgressBar
            {
                Location = new Point(16, 78),
                Size = new Size(520, 22),
                Minimum = 0,
                Maximum = 100,
                Value = 0
            };

            _lblStats = new Label
            {
                Text = "",
                Location = new Point(16, 108),
                Size = new Size(520, 60)
            };

            _btnCancel = new Button
            {
                Text = "Отмена",
                Location = new Point(436, 180),
                Size = new Size(100, 30)
            };
            _btnCancel.Click += (_, _) =>
            {
                _btnCancel.Enabled = false;
                _btnCancel.Text = "Останавливаю...";
                _cts.Cancel();
            };

            Controls.Add(_lblTitle);
            Controls.Add(_lblCurrent);
            Controls.Add(_progress);
            Controls.Add(_lblStats);
            Controls.Add(_btnCancel);

            Shown += async (_, _) => await RunAsync();
        }

        private async Task RunAsync()
        {
            var service = new ScreenScraperService();

            // Собираем полный список игр (файлы + папки).
            var games = new List<string>();
            games.AddRange(_scanResult.Files);
            games.AddRange(_scanResult.Folders);

            _total = games.Count;
            if (_total == 0)
            {
                _lblCurrent.Text = "В этой системе нет игр.";
                _btnCancel.Text = "Закрыть";
                _btnCancel.Enabled = true;
                _btnCancel.Click += (_, _) => Close();
                return;
            }

            GameInfoStore.Load();

            foreach (var gamePath in games)
            {
                if (_cts.IsCancellationRequested) break;

                string name = Directory.Exists(gamePath)
                    ? Path.GetFileName(gamePath)
                    : Path.GetFileName(gamePath);

                _lblCurrent.Text = $"({_processed + 1} / {_total})  {name}";
                _progress.Value = (int)(_processed * 100.0 / _total);
                Application.DoEvents();

                string relPath = Path.GetRelativePath(_scanResult.SourcePath, gamePath);
                var current = GameInfoStore.Get(_system.Id, relPath);

                try
                {
                    var result = await service.ScrapeGameAsync(
                    _system, current, gamePath, _scanResult.SourcePath,
                    _user, _password, _options);

                    if (result.GameNotFound)
                    {
                        _notFound++;
                    }
                    else if (!result.Success)
                    {
                        _errors++;
                    }
                    else if (result.Info != null)
                    {
                        if (result.TextFieldsFilled > 0 || result.MediaDownloaded > 0)
                            _filledGames++;
                        _mediaDownloaded += result.MediaDownloaded;

                        // Сохраняем результат сразу — чтобы при отмене не потерять.
                        GameInfoStore.Set(_system.Id, relPath, result.Info);
                        GameInfoStore.Save();
                    }
                }
                catch (Exception ex)
                {
                    _errors++;
                    DebugLog.Write($"Ошибка массового скрапа {name}: {ex.Message}");
                }

                _processed++;

                _lblStats.Text =
                    $"Обработано: {_processed} / {_total}\n" +
                    $"Заполнено игр: {_filledGames}    Медиа: {_mediaDownloaded}\n" +
                    $"Не найдено: {_notFound}    Ошибок: {_errors}";
            }

            _progress.Value = 100;

            if (_cts.IsCancellationRequested)
            {
                _lblCurrent.Text = "Остановлено пользователем.";
            }
            else
            {
                _lblCurrent.Text = "Готово.";
                Completed = true;
            }

            _btnCancel.Text = "Закрыть";
            _btnCancel.Enabled = true;
            _btnCancel.Click += (_, _) => Close();

            DebugLog.Write($"Массовый скрап: система={_system.Id}, всего={_total}, " +
                           $"заполнено={_filledGames}, медиа={_mediaDownloaded}, " +
                           $"не найдено={_notFound}, ошибок={_errors}, " +
                           $"отменён={_cts.IsCancellationRequested}");
        }
    }
}