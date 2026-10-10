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
        private Button _btnBrowseSource = null!;
        private Button _btnBrowseTarget = null!;
        private Button _btnSave = null!;
        private Button _btnCheck = null!;
        private Button _btnScan = null!;
        private Button _btnSystemsEditor = null!;
        private Button _btnScreenScraper = null!;
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
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));

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
            _btnBrowseSource = new Button { Text = "Обзор…", Dock = DockStyle.Fill };
            _btnBrowseSource.Click += BtnBrowseSource_Click;

            // Назначение
            var lblTarget = new Label { Text = "Куда копировать:", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill };
            _txtTarget = new TextBox { Dock = DockStyle.Fill };
            _btnBrowseTarget = new Button { Text = "Обзор…", Dock = DockStyle.Fill };
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

            table.Controls.Add(_lstGames, 0, 7);
            table.SetColumnSpan(_lstGames, 3);

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

            Controls.Add(table);
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

        // ============================================================
        // СКАНИРОВАНИЕ
        // ============================================================
        private async Task ScanInBackgroundAsync(string sourcePath, bool isAutomatic)
        {
            if (_scanning) return;

            _scanning = true;
            _btnScan.Enabled = false;
            _btnSystemsEditor.Enabled = false;

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
            _lastScanTime = DateTime.Now;
            _unknownFolders = unknownFolders;
            _lastChanges = changes;

            RefreshSystemsList();
            ShowSummaryInGamesList();

            _scanning = false;
            _btnScan.Enabled = true;
            _btnSystemsEditor.Enabled = true;

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
            int idx = _lstGames.SelectedIndex;
            if (idx < 0) return;
            if (_lstSystems.SelectedIndex < 0) return;
            if (_lstSystems.SelectedIndex >= _lastScan.Count) return;

            var result = _lastScan[_lstSystems.SelectedIndex];

            string? fullPath = null;
            int folderCount = result.Folders.Count;

            var foldersSorted = result.Folders.OrderBy(f => f, StringComparer.OrdinalIgnoreCase).ToList();
            var filesSorted = result.Files.OrderBy(f => f, StringComparer.OrdinalIgnoreCase).ToList();

            if (idx < folderCount)
            {
                fullPath = foldersSorted.ElementAtOrDefault(idx);
            }
            else
            {
                int fileIdx = idx - folderCount;
                fullPath = filesSorted.ElementAtOrDefault(fileIdx);
            }

            if (fullPath == null)
            {
                SetStatus("Не удалось найти файл игры.", Color.Red);
                return;
            }

            using var info = new GameInfoForm(result.System.Id, result.SourcePath, fullPath);
            info.ShowDialog(this);
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
    }
}