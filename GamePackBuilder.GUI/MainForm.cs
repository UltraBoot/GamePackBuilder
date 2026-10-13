using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GamePackBuilder.GUI
{
    public partial class MainForm : Form
    {
        // ============================================================
        // ПОЛЯ: элементы управления
        // ============================================================
        private TextBox _txtSource = null!;
        private TextBox _txtTarget = null!;
        private FlatIconButton _btnBrowseSource = null!;
        private FlatIconButton _btnBrowseTarget = null!;
        private Button _btnSave = null!;
        private Button _btnCheck = null!;
        private Button _btnScan = null!;
        private Button _btnSystemsEditor = null!;
        private Button _btnScreenScraper = null!;
        private Button _btnScrapeSystem = null!;
        private Button _btnPreviewGame = null!;
        private Button _btnOpenCard = null!;
        private ListBox _lstSystems = null!;
        private ListBox _lstGames = null!;
        private Label _lblStatus = null!;
        private Label _lblConfigPath = null!;

        // ============================================================
        // ПОЛЯ: данные
        // ============================================================
        private List<SystemScanResult> _lastScan = new();
        private AppSettings _settings = new();
        private bool _scanning = false;
        private GamePreviewForm? _previewForm = null;
        private bool _insideStrayFilesScan = false;
        private ProgressBar _busyProgress = null!;
        private DateTime _lastScanTime = DateTime.MinValue;

        /// <summary>Сколько игр было в каждой системе на прошлом скане (для отчёта изменений).</summary>
        private Dictionary<string, int> _previousCounts = new();

        /// <summary>Список папок без описания системы (для уведомления).</summary>
        private List<string> _unknownFolders = new();

        // ============================================================
        // КОНСТРУКТОР
        // ============================================================
        public MainForm()
        {
            InitializeComponent();
            _settings = AppSettings.Load();
            ThemeManager.LoadFromSettings();
            BuildUI();

            
            ThemeManager.Apply(this);
            Shown += MainForm_Shown;
            KeyPreview = true;
            KeyDown += MainForm_KeyDown;
        }

        private void BuildUI()
        {
            Text = "GamePackBuilder";
            Size = new Size(1200, 950);
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(1000, 750);
            Font = new Font("Segoe UI", 10F);

            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 8,
                Padding = new Padding(16)
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));

            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            table.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            table.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

            // Источник
            var lblSource = new Label { Text = "Источник игр:", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill };
            _txtSource = new TextBox { Dock = DockStyle.Fill };
            _btnBrowseSource = new FlatIconButton
            {
                Text = "Обзор",
                Icon = AppIcons.Get("folder"),
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 3, 0, 3)
            };
            _btnBrowseSource.Click += BtnBrowseSource_Click;

            // Назначение
            var lblTarget = new Label { Text = "Куда копировать:", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill };
            _txtTarget = new TextBox { Dock = DockStyle.Fill };
            _btnBrowseTarget = new FlatIconButton
            {
                Text = "Обзор",
                Icon = AppIcons.Get("folder"),
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 3, 0, 3)
            };
            _btnBrowseTarget.Click += BtnBrowseTarget_Click;

            // Кнопки
            _btnSave = new Button { Text = "Сохранить", Width = 130, Height = 32 };
            _btnSave.Click += BtnSave_Click;

            _btnCheck = new Button { Text = "Проверить пути", Width = 150, Height = 32 };
            _btnCheck.Click += BtnCheck_Click;

            _btnScan = new Button { Text = "Сканировать источник", Width = 200, Height = 32 };
            _btnScan.Click += BtnScan_Click;

            _btnSystemsEditor = new Button { Text = "Редактор систем", Width = 160, Height = 32 };
            _btnSystemsEditor.Click += BtnSystemsEditor_Click;

            _btnScreenScraper = new Button { Text = "ScreenScraper", Width = 160, Height = 32 };
            _btnScreenScraper.Click += BtnScreenScraper_Click;

            _btnScrapeSystem = new Button { Text = "Скачать медиа системы", Width = 200, Height = 32, Enabled = false };
            _btnScrapeSystem.Click += BtnScrapeSystem_Click;

            var panelButtons = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                Dock = DockStyle.Fill
            };
            panelButtons.Controls.Add(_btnSave);
            panelButtons.Controls.Add(_btnCheck);
            panelButtons.Controls.Add(_btnScan);
            panelButtons.Controls.Add(_btnSystemsEditor);
            panelButtons.Controls.Add(_btnScreenScraper);
            panelButtons.Controls.Add(_btnScrapeSystem);
            var _btnTheme = new Button { Text = "🌙", Width = 44, Height = 32, Font = new Font("Segoe UI", 12F) };
            _btnTheme.Click += (s, e) =>
            {
                ThemeManager.Toggle();
                ThemeManager.Apply(this);
                _btnTheme.Text = ThemeManager.Current.Name == "dark" ? "☀" : "🌙";
            };
            _btnTheme.Text = ThemeManager.Current.Name == "dark" ? "☀" : "🌙";
            panelButtons.Controls.Add(_btnTheme);

            // Путь конфига
            _lblConfigPath = new Label
            {
                Text = "Конфиг: " + AppSettings.GetConfigFilePath(),
                ForeColor = Color.Gray,
                AutoSize = true,
                Dock = DockStyle.Fill
            };

            // Подпись систем
            var lblSystems = new Label
            {
                Text = "Найденные системы (Escape — сбросить выбор, снова покажет сводку):",
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Fill,
                AutoSize = false,
                Height = 34,
                Padding = new Padding(0, 10, 0, 4)
            };

            // Список систем
            _lstSystems = new ListBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 9.5F),
                IntegralHeight = false,
                HorizontalScrollbar = true
            };
            _lstSystems.SelectedIndexChanged += LstSystems_SelectedIndexChanged;
            _lstSystems.KeyDown += (s, ev) =>
            {
                if (ev.KeyCode == Keys.Escape)
                {
                    _lstSystems.ClearSelected();
                    ev.Handled = true;
                }
            };

            // Подпись игр
            var lblGames = new Label
            {
                Text = "Игры выбранной системы / сводка:",
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Fill,
                AutoSize = false,
                Height = 34,
                Padding = new Padding(0, 10, 0, 4)
            };

            // Список игр
            _lstGames = new ListBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 9.5F),
                IntegralHeight = false,
                HorizontalScrollbar = true
            };
            _lstGames.DoubleClick += LstGames_DoubleClick;
            _lstGames.SelectedIndexChanged += LstGames_SelectedIndexChanged;

            // Раскладка
            table.Controls.Add(lblSource, 0, 0);
            table.Controls.Add(_txtSource, 1, 0);
            table.Controls.Add(_btnBrowseSource, 2, 0);

            table.Controls.Add(lblTarget, 0, 1);
            table.Controls.Add(_txtTarget, 1, 1);
            table.Controls.Add(_btnBrowseTarget, 2, 1);

            table.Controls.Add(new Label { Text = "" }, 0, 2);
            table.Controls.Add(panelButtons, 1, 2);
            table.Controls.Add(new Label { Text = "" }, 2, 2);

            table.Controls.Add(_lblConfigPath, 0, 3);
            table.SetColumnSpan(_lblConfigPath, 3);

            table.Controls.Add(lblSystems, 0, 4);
            table.SetColumnSpan(lblSystems, 3);

            table.Controls.Add(_lstSystems, 0, 5);
            table.SetColumnSpan(_lstSystems, 3);

            table.Controls.Add(lblGames, 0, 6);
            table.SetColumnSpan(lblGames, 3);

            // Контейнер: слева список игр, справа — вертикальная панель с кнопками.
            var gamesContainer = new Panel { Dock = DockStyle.Fill };

            var gamesButtonsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                Width = 140,
                FlowDirection = FlowDirection.TopDown,
                Padding = new Padding(6),
                WrapContents = false
            };

            _btnPreviewGame = new Button
            {
                Text = "Просмотр",
                Width = 120,
                Height = 36,
                Enabled = false
            };
            _btnPreviewGame.Click += BtnPreviewGame_Click;

            _btnOpenCard = new Button
            {
                Text = "Карточка",
                Width = 120,
                Height = 36,
                Enabled = false
            };
            _btnOpenCard.Click += BtnOpenCard_Click;

            gamesButtonsPanel.Controls.Add(_btnPreviewGame);
            gamesButtonsPanel.Controls.Add(_btnOpenCard);

            gamesContainer.Controls.Add(_lstGames);
            gamesContainer.Controls.Add(gamesButtonsPanel);

            table.Controls.Add(gamesContainer, 0, 7);
            table.SetColumnSpan(gamesContainer, 3);

            // Строка статуса
            _lblStatus = new Label
            {
                Text = "Готов.",
                Dock = DockStyle.Bottom,
                Height = 28,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(16, 0, 0, 0),
                BackColor = Color.FromArgb(240, 240, 240)
            };

            // Тонкая полоска загрузки над статусной строкой.
            _busyProgress = new ProgressBar
            {
                Dock = DockStyle.Bottom,
                Height = 4,
                Style = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 3,
                Visible = false
            };

            Controls.Add(table);
            Controls.Add(_busyProgress);
            Controls.Add(_lblStatus);
        }

        private void MainForm_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Control && e.Shift && e.KeyCode == Keys.D)
            {
                using var form = new DebugForm();
                form.ShowDialog(this);
                e.Handled = true;
            }
        }

        // ============================================================
        // ЗАПУСК
        // ============================================================
        private async void MainForm_Shown(object? sender, EventArgs e)
        {
            ThemeManager.Apply(this);
            // Загружаем список систем из JSON
            GameSystems.Reload();

            _txtSource.Text = _settings.SourcePath;
            _txtTarget.Text = _settings.TargetPath;

            if (string.IsNullOrWhiteSpace(_settings.SourcePath) || !Directory.Exists(_settings.SourcePath))
            {
                SetStatus("Укажи источник игр и нажми «Сканировать источник».", Color.Gray);
                return;
            }

            var cache = ScanCache.Load(_settings.SourcePath);
            if (cache != null)
            {
                _lastScan = cache.ToResults();
                _lastScanTime = cache.LastScan;

                // Восстанавливаем "предыдущие счётчики" из кэша — при первом показе нет изменений
                _previousCounts = _lastScan.ToDictionary(r => r.System.Id, r => r.TotalGames);

                RefreshSystemsList();
                ShowSummaryInGamesList();
                SetStatus($"Загружен кэш от {cache.LastScan:dd.MM.yyyy HH:mm}. Идёт проверка обновлений в фоне…", Color.DodgerBlue);
            }
            else
            {
                SetStatus("Первое сканирование, подожди…", Color.DodgerBlue);
            }

            await ScanInBackgroundAsync(_settings.SourcePath, isAutomatic: true);
        }

        // ============================================================
        // ОБРАБОТЧИКИ КНОПОК
        // ============================================================

        private void BtnBrowseSource_Click(object? sender, EventArgs e)
        {
            string? path = PickFolder("Выберите папку с играми (например, N:\\3.Console\\Games)", _txtSource.Text);
            if (path != null) _txtSource.Text = path;
        }

        private void BtnBrowseTarget_Click(object? sender, EventArgs e)
        {
            string? path = PickFolder("Выберите папку, куда копировать готовый пак", _txtTarget.Text);
            if (path != null) _txtTarget.Text = path;
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            try
            {
                _settings.SourcePath = _txtSource.Text.Trim();
                _settings.TargetPath = _txtTarget.Text.Trim();
                _settings.Save();
                SetStatus("Настройки сохранены.", Color.Green);
            }
            catch (Exception ex)
            {
                SetStatus("Ошибка сохранения: " + ex.Message, Color.Red);
            }
        }

        private void BtnCheck_Click(object? sender, EventArgs e)
        {
            string src = _txtSource.Text.Trim();
            string dst = _txtTarget.Text.Trim();

            if (string.IsNullOrWhiteSpace(src) || string.IsNullOrWhiteSpace(dst))
            {
                SetStatus("Укажи оба пути.", Color.OrangeRed);
                return;
            }

            if (!Directory.Exists(src))
            {
                SetStatus("Источник не найден: " + src, Color.Red);
                return;
            }

            SetStatus("Источник OK. Назначение будет создано при необходимости.", Color.Green);
        }

        private async void BtnScan_Click(object? sender, EventArgs e)
        {
            string src = _txtSource.Text.Trim();

            if (string.IsNullOrWhiteSpace(src) || !Directory.Exists(src))
            {
                SetStatus("Сначала укажи существующий источник игр.", Color.OrangeRed);
                return;
            }

            await ScanInBackgroundAsync(src, isAutomatic: false);
        }

        private void BtnSystemsEditor_Click(object? sender, EventArgs e)
        {
            using var editor = new SystemsEditorForm();
            var result = editor.ShowDialog(this);

            if (result != DialogResult.OK)
                return;

            // Системы уже перезагружены внутри редактора.
            // Пересканируем источник, чтобы обновить список.
            string src = _txtSource.Text.Trim();
            if (!string.IsNullOrWhiteSpace(src) && Directory.Exists(src))
            {
                _ = ScanInBackgroundAsync(src, isAutomatic: false);
            }
            else
            {
                SetStatus("Список систем обновлён.", Color.Green);
            }
        }
        private void BtnScreenScraper_Click(object? sender, EventArgs e)
        {
            using var form = new ScreenScraperSettingsForm(_settings);
            form.ShowDialog(this);
        }

        private void BtnScrapeSystem_Click(object? sender, EventArgs e)
        {
            int idx = _lstSystems.SelectedIndex;
            if (idx < 0 || idx >= _lastScan.Count)
            {
                SetStatus("Сначала выбери систему в верхнем списке.", Color.OrangeRed);
                return;
            }

            // Проверяем настройки ScreenScraper
            var s = AppSettings.Load();
            if (string.IsNullOrWhiteSpace(s.ScreenScraperUser) ||
                string.IsNullOrWhiteSpace(s.ScreenScraperPassword))
            {
                MessageBox.Show(this,
                    "Сначала заполни логин и пароль ScreenScraper (кнопка «ScreenScraper»).",
                    "Нет данных", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = _lastScan[idx];
            if (result.System.ScreenScraperSystemId <= 0)
            {
                MessageBox.Show(this,
                    $"Для системы «{result.System.DisplayName}» не задан ScreenScraper ID.\r\n\r\n" +
                    "Открой «Редактор систем» и укажи числовой ID.",
                    "Нет ID", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Спросим, что качать.
            using var optionsForm = new MediaDownloadOptionsForm();
            if (optionsForm.ShowDialog(this) != DialogResult.OK)
                return;

            using var form = new ScrapeSystemForm(
                result.System, result, s.ScreenScraperUser, s.ScreenScraperPassword,
                optionsForm.Options);
            form.ShowDialog(this);

            // После окончания — обновим список игр, чтобы подтянуть свежие данные.
            if (form.Completed)
            {
                SetStatus("Массовый скрапинг завершён.", Color.Green);
            }
        }

        // ============================================================
        // СКАНИРОВАНИЕ
        // ============================================================
        private async Task ScanInBackgroundAsync(string sourcePath, bool isAutomatic)
        {
            if (_scanning) return;

            _scanning = true;
            _btnScan.Enabled = false;
            _btnSystemsEditor.Enabled = false;

            _busyProgress.Visible = true;

            if (!isAutomatic)
                SetStatus("Сканирование…", Color.DodgerBlue);

            List<SystemScanResult> results;
            List<string> unknownFolders = new();
            try
            {
                (results, unknownFolders) = await Task.Run(() => ScanWithUnknowns(sourcePath));
            }
            catch (Exception ex)
            {
                _scanning = false;
                _btnScan.Enabled = true;
                _btnSystemsEditor.Enabled = true;
                _busyProgress.Visible = false;
                SetStatus("Ошибка сканирования: " + ex.Message, Color.Red);
                return;
            }

            // Считаем изменения
            var newCounts = results.ToDictionary(r => r.System.Id, r => r.TotalGames);
            var changes = new List<string>();
            if (_previousCounts.Count > 0)
            {
                var allIds = _previousCounts.Keys.Union(newCounts.Keys).Distinct();
                foreach (var id in allIds)
                {
                    int oldVal = _previousCounts.TryGetValue(id, out var ov) ? ov : 0;
                    int newVal = newCounts.TryGetValue(id, out var nv) ? nv : 0;
                    int diff = newVal - oldVal;
                    if (diff == 0) continue;

                    var sysName = GameSystems.FindById(id)?.DisplayName ?? id;
                    if (diff > 0) changes.Add($"[+] {sysName}  +{diff}");
                    else changes.Add($"[-] {sysName}  {diff}");
                }
            }

            // Сохраняем кэш
            var cache = ScanCache.FromResults(sourcePath, results);
            cache.Save();

            _previousCounts = newCounts;
            _lastScan = results;
            // Импортируем gamelist.xml в нашу базу (для каждой системы).
            await ImportGamelistsAsync(results);
            // Ищем "заблудшие" файлы в подпапках систем —
            // только при ручном сканировании, не при автозапуске.
            if (!isAutomatic)
                await DetectAndOfferStrayFilesAsync(results);
            _lastScanTime = DateTime.Now;
            _unknownFolders = unknownFolders;
            _lastChanges = changes;

            RefreshSystemsList();
            ShowSummaryInGamesList();

            _scanning = false;
            _btnScan.Enabled = true;
            _btnSystemsEditor.Enabled = true;
            _busyProgress.Visible = false;

            int newTotal = results.Sum(r => r.TotalGames);
            string unknownMsg = unknownFolders.Count > 0
                ? $" Также найдено {unknownFolders.Count} папок без описания системы."
                : "";

            if (isAutomatic)
                SetStatus($"Проверка завершена. Всего игр: {newTotal}.{unknownMsg}", Color.Green);
            else
                SetStatus($"Сканирование завершено. Систем: {results.Count}, всего игр: {newTotal}.{unknownMsg}", Color.Green);
        }

        /// <summary>Сканирует и отдельно возвращает список папок, для которых нет описания.</summary>
        private static (List<SystemScanResult> Results, List<string> UnknownFolders) ScanWithUnknowns(string sourcePath)
        {
            var results = GameScanner.Scan(sourcePath);
            var knownFolders = GameSystems.All
                .Select(s => s.SourceFolder)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var unknown = Directory.GetDirectories(sourcePath)
                .Select(Path.GetFileName)
                .Where(name => !string.IsNullOrEmpty(name))
                .Where(name => !knownFolders.Contains(name!))
                .Cast<string>()
                .OrderBy(n => n)
                .ToList();

            return (results, unknown);
        }

        /// <summary>
        /// Импортировать gamelist.xml каждой найденной системы в базу.
        /// Пустые поля заполняются молча, конфликты собираются и показываются пользователю.
        /// </summary>
        /// <summary>
        /// Импортировать gamelist.xml каждой системы в базу.
        /// Чтение XML — в фоне, чтобы не морозить UI.
        /// </summary>
        private async Task ImportGamelistsAsync(List<SystemScanResult> results)
        {
            List<GamelistConflict> allConflicts = await Task.Run(() =>
            {
                var conflicts = new List<GamelistConflict>();

                foreach (var r in results)
                {
                    var games = new List<string>();
                    games.AddRange(r.Files);
                    games.AddRange(r.Folders);
                    if (games.Count == 0) continue;

                    try
                    {
                        var importResult = GamelistImporter.ImportForSystem(
                            r.System, r.SourcePath, games);

                        conflicts.AddRange(importResult.Conflicts);
                    }
                    catch (Exception ex)
                    {
                        DebugLog.Write($"Ошибка импорта gamelist для {r.System.Id}: {ex.Message}");
                    }
                }

                return conflicts;
            });

            // Диалог конфликтов — только на UI-потоке.
            if (allConflicts.Count > 0)
            {
               
                using var dlg = new ConflictsForm(allConflicts);
                if (dlg.ShowDialog(this) == DialogResult.OK && dlg.Accepted.Count > 0)
                {
                    var accepted = dlg.Accepted.ToList();
                    await Task.Run(() => GamelistImporter.ApplyAccepted(accepted));
                }
                              
            }
        }

        // ============================================================
        // СПИСКИ
        // ============================================================
        private void RefreshSystemsList()
        {
            int prevIndex = _lstSystems.SelectedIndex;

            _lstSystems.BeginUpdate();
            try
            {
                _lstSystems.Items.Clear();

                for (int i = 0; i < _lastScan.Count; i++)
                {
                    var r = _lastScan[i];
                    string line = $"{i + 1,3}. {r.System.SourceFolder,-25} " +
                                  $"{r.System.DisplayName,-40} " +
                                  $"игр: {r.TotalGames,5}  (файлов: {r.Files.Count}, папок: {r.Folders.Count})";
                    _lstSystems.Items.Add(line);
                }
            }
            finally
            {
                _lstSystems.EndUpdate();
            }

            if (prevIndex >= 0 && prevIndex < _lstSystems.Items.Count)
                _lstSystems.SelectedIndex = prevIndex;
        }

        private void LstSystems_SelectedIndexChanged(object? sender, EventArgs e)
        {
            int idx = _lstSystems.SelectedIndex;

            if (_btnScrapeSystem != null)
                _btnScrapeSystem.Enabled = idx >= 0 && idx < _lastScan.Count;

            if (idx < 0 || idx >= _lastScan.Count)
            {
                ShowSummaryInGamesList();
                return;
            }

            ShowGamesOfSystem(_lastScan[idx]);
        }

        private void ShowGamesOfSystem(SystemScanResult result)
        {
            _lstGames.BeginUpdate();
            try
            {
                _lstGames.Items.Clear();

                string basePath = result.SourcePath;
                int n = 1;

                foreach (var folder in result.Folders.OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
                {
                    string rel = Path.GetRelativePath(basePath, folder);
                    _lstGames.Items.Add($"{n,4}. [папка] {rel}");
                    n++;
                }

                foreach (var file in result.Files.OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
                {
                    string rel = Path.GetRelativePath(basePath, file);
                    _lstGames.Items.Add($"{n,4}. {rel}");
                    n++;
                }

                SetStatus($"{result.System.DisplayName}: {result.TotalGames} игр " +
                          $"(файлов: {result.Files.Count}, папок: {result.Folders.Count}).",
                          Color.Green);
            }
            finally
            {
                _lstGames.EndUpdate();
            }
        }

        /// <summary>
        /// Показывает сводку в нижнем списке, когда система не выбрана.
        /// </summary>
        private void ShowSummaryInGamesList()
        {
            _lstGames.BeginUpdate();
            try
            {
                _lstGames.Items.Clear();

                if (_lastScan.Count == 0)
                {
                    SetStatus("Источник ещё не просканирован.", Color.Gray);
                    return;
                }

                int totalGames = _lastScan.Sum(r => r.TotalGames);

                // ---- Заголовок ----
                _lstGames.Items.Add("═══════════════════════════════════════════════════════════════");
                _lstGames.Items.Add($" Источник:          {_txtSource.Text.Trim()}");
                _lstGames.Items.Add($" Последний скан:    {_lastScanTime:dd.MM.yyyy HH:mm}");
                _lstGames.Items.Add($" Всего систем:      {_lastScan.Count}");
                _lstGames.Items.Add($" Всего игр:         {totalGames}");
                _lstGames.Items.Add("───────────────────────────────────────────────────────────────");

                // ---- Топ-3 ----
                _lstGames.Items.Add(" ТОП-3 системы по количеству игр:");
                var top3 = _lastScan.OrderByDescending(r => r.TotalGames).Take(3).ToList();
                for (int i = 0; i < top3.Count; i++)
                {
                    var r = top3[i];
                    _lstGames.Items.Add($"   {i + 1}. {r.System.DisplayName,-45} {r.TotalGames,6}");
                }

                // ---- Изменения ----
                _lstGames.Items.Add("───────────────────────────────────────────────────────────────");
                if (_lastChanges.Count > 0)
                {
                    _lstGames.Items.Add(" ИЗМЕНЕНИЯ С ПРОШЛОГО СКАНА:");
                    foreach (var c in _lastChanges)
                        _lstGames.Items.Add("   " + c);
                }
                else
                {
                    _lstGames.Items.Add(" ИЗМЕНЕНИЯ С ПРОШЛОГО СКАНА: нет");
                }

                // ---- Неизвестные папки ----
                if (_unknownFolders.Count > 0)
                {
                    _lstGames.Items.Add("───────────────────────────────────────────────────────────────");
                    _lstGames.Items.Add($" ПАПКИ БЕЗ ОПИСАНИЯ СИСТЕМЫ ({_unknownFolders.Count}):");
                    foreach (var u in _unknownFolders)
                        _lstGames.Items.Add("   • " + u);
                    _lstGames.Items.Add("   Добавь их в «Редактор систем» (systems.json).");
                }

                _lstGames.Items.Add("═══════════════════════════════════════════════════════════════");

                SetStatus($"Сводка по {_lastScan.Count} системам. Всего игр: {totalGames}.", Color.Green);
            }
            finally
            {
                _lstGames.EndUpdate();
            }
        }

        // ============================================================
        // ДВОЙНОЙ КЛИК ПО ИГРЕ
        // ============================================================
        private void LstGames_DoubleClick(object? sender, EventArgs e)
        {
            OpenCardForSelected();
        }

        /// <summary>Путь к выбранной в нижнем списке игре (файл или папка).</summary>
        private string? GetSelectedGamePath()
        {
            int idx = _lstGames.SelectedIndex;
            if (idx < 0) return null;
            if (_lstSystems.SelectedIndex < 0 || _lstSystems.SelectedIndex >= _lastScan.Count)
                return null;

            var result = _lastScan[_lstSystems.SelectedIndex];
            int folderCount = result.Folders.Count;

            var foldersSorted = result.Folders
                .OrderBy(f => f, StringComparer.OrdinalIgnoreCase).ToList();
            var filesSorted = result.Files
                .OrderBy(f => f, StringComparer.OrdinalIgnoreCase).ToList();

            if (idx < folderCount)
                return foldersSorted.ElementAtOrDefault(idx);

            return filesSorted.ElementAtOrDefault(idx - folderCount);
        }

        private void BtnPreviewGame_Click(object? sender, EventArgs e)
        {
            OpenPreviewForSelected();
        }

        private void BtnOpenCard_Click(object? sender, EventArgs e)
        {
            OpenCardForSelected();
        }

        private void OpenCardForSelected()
        {
            string? fullPath = GetSelectedGamePath();
            if (fullPath == null) return;

            var result = _lastScan[_lstSystems.SelectedIndex];

            using var info = new GameInfoForm(result.System.Id, result.SourcePath, fullPath);
            info.ShowDialog(this);
        }

        /// <summary>
        /// Открыть (или обновить) окно предпросмотра для выбранной игры.
        /// Включает обложку, скриншоты, логотип и фон.
        /// </summary>
        private void OpenPreviewForSelected()
        {
            string? fullPath = GetSelectedGamePath();
            if (fullPath == null) return;

            var result = _lastScan[_lstSystems.SelectedIndex];
            string relPath = Path.GetRelativePath(result.SourcePath, fullPath);

            GameInfoStore.Load();
            var info = GameInfoStore.Get(result.System.Id, relPath);
            // Подтягиваем медиа с диска, если в store ещё ничего нет.
            string gameName = Directory.Exists(fullPath)
                ? Path.GetFileName(fullPath)
                : Path.GetFileNameWithoutExtension(fullPath);

            MediaScanner.EnrichGameInfo(info, result.SourcePath, gameName);

            // Собираем список медиа: обложка → скриншоты → логотип → фон.
            var media = new List<string>();

            if (!string.IsNullOrWhiteSpace(info.CoverPath) && File.Exists(info.CoverPath))
                media.Add(info.CoverPath);

            foreach (var s in info.Screenshots)
                if (!string.IsNullOrWhiteSpace(s) && File.Exists(s))
                    media.Add(s);

            if (!string.IsNullOrWhiteSpace(info.WheelPath) && File.Exists(info.WheelPath))
                media.Add(info.WheelPath);

            if (!string.IsNullOrWhiteSpace(info.FanartPath) && File.Exists(info.FanartPath))
                media.Add(info.FanartPath);

            string title = !string.IsNullOrWhiteSpace(info.Title)
                ? info.Title
                : Path.GetFileName(fullPath);

            if (_previewForm == null || _previewForm.IsDisposed)
            {
                _previewForm = new GamePreviewForm();
                _previewForm.FormClosed += (_, _) => _previewForm = null;
                _previewForm.SetContent(title, media);
                _previewForm.Show(this);
            }
            else
            {
                _previewForm.SetContent(title, media);
                _previewForm.Activate();
            }
        }

        /// <summary>
        /// Одинарный выбор игры в нижнем списке — показать/обновить окно предпросмотра.
        /// Двойной клик — открыть карточку (обрабатывается отдельно).
        /// </summary>
        /// <summary>
        /// Одинарный клик по игре — просто выделение и включение кнопок справа.
        /// Само открытие превью или карточки — только по явным кнопкам.
        /// </summary>
        private void LstGames_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_scanning) return;

            bool has = GetSelectedGamePath() != null;
            _btnPreviewGame.Enabled = has;
            _btnOpenCard.Enabled = has;
        }

        // ============================================================
        // ВСПОМОГАТЕЛЬНОЕ
        // ============================================================
        private static string? PickFolder(string description, string current)
        {
            using var dlg = new FolderBrowserDialog
            {
                Description = description,
                UseDescriptionForTitle = true,
                ShowNewFolderButton = true
            };

            if (!string.IsNullOrWhiteSpace(current) && Directory.Exists(current))
                dlg.SelectedPath = current;

            return dlg.ShowDialog() == DialogResult.OK ? dlg.SelectedPath : null;
        }

        private void SetStatus(string text, Color color)
        {
            _lblStatus.Text = text;
            _lblStatus.ForeColor = color;
        }

        // Отслеживание изменений
        private List<string> _lastChanges = new();
        /// <summary>
        /// Найти "заблудшие" файлы в подпапках систем и предложить их перенести в корень.
        /// </summary>
        private async Task DetectAndOfferStrayFilesAsync(List<SystemScanResult> results)
        {
            // Защита от рекурсии: если уже идёт поиск "заблудших" файлов — не запускаем снова.
            if (_insideStrayFilesScan) return;
            _insideStrayFilesScan = true;

            try
            {
                await DetectAndOfferStrayFilesInternalAsync(results);
            }
            finally
            {
                _insideStrayFilesScan = false;
            }
        }

        private async Task DetectAndOfferStrayFilesInternalAsync(List<SystemScanResult> results)
        {
            var foundScans = await Task.Run(() =>
            {
                var list = new List<StrayScanResult>();
                foreach (var r in results)
                {
                    try
                    {
                        var s = StrayFilesScanner.Scan(r.System, r.SourcePath);
                        if (s.HasCandidates) list.Add(s);
                    }
                    catch (Exception ex)
                    {
                        DebugLog.Write($"Ошибка поиска заблудших файлов {r.System.Id}: {ex.Message}");
                    }
                }
                return list;
            });

            if (foundScans.Count == 0) return;

            int totalMoved = 0;
            int totalErrors = 0;

            foreach (var scan in foundScans)
            {
                using var dlg = new StrayFilesForm(scan);
                if (dlg.ShowDialog(this) == DialogResult.OK && dlg.Accepted.Count > 0)
                {
                    var accepted = dlg.Accepted;
                    var (moved, errors) = await Task.Run(() =>
                        StrayFilesScanner.TransferFiles(scan, accepted));

                    totalMoved += moved;
                    totalErrors += errors;
                }
            }

            if (totalMoved > 0 || totalErrors > 0)
            {
                DebugLog.Write($"Заблудшие файлы: перенесено={totalMoved}, ошибок={totalErrors}");

                // Пересканируем источник — файлы переехали, список изменился.
                if (totalMoved > 0)
                {
                    string src = _txtSource.Text.Trim();
                    if (!string.IsNullOrWhiteSpace(src) && Directory.Exists(src))
                    {
                        // Небольшая пауза, потом пересканируем.
                        await Task.Delay(100);
                        await ScanInBackgroundAsync(src, isAutomatic: true);
                    }
                }
            }
        }
    }

}