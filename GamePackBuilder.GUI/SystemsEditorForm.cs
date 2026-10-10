using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace GamePackBuilder.GUI
{
    public partial class SystemsEditorForm : Form
    {
        private DataGridView _grid = null!;
        private Button _btnAdd = null!;
        private Button _btnDelete = null!;
        private Button _btnUp = null!;
        private Button _btnDown = null!;
        private Button _btnSave = null!;
        private Button _btnCancel = null!;
        private Label _lblHint = null!;
        private Label _lblPath = null!;

        public SystemsEditorForm()
        {
            InitializeComponent();
            BuildUI();
            ThemeManager.Apply(this);
            LoadSystemsIntoGrid();
        }

        private void BuildUI()
        {
            Text = "Редактор систем";
            Size = new Size(1150, 620);
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(900, 500);
            Font = new Font("Segoe UI", 10F);

            // ---- Верхняя подсказка ----
            _lblHint = new Label
            {
                Text = "Двойной клик по ячейке — редактирование. Расширения указывай через запятую: .zip, .nes, .unf\r\n" +
                       "Галочка «Игры — папки» — для систем, где одна игра это целая папка (Dreamcast, Saturn, Master System).",
                Dock = DockStyle.Top,
                Height = 48,
                Padding = new Padding(16, 6, 16, 0),
                ForeColor = Color.DimGray
            };

            // ---- Таблица ----
            _grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9.5F),
                ShowCellToolTips = true
            };

            // Столбцы
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Id",
                HeaderText = "ID",
                FillWeight = 12
            });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "DisplayName",
                HeaderText = "Отображаемое имя",
                FillWeight = 25
            });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "SourceFolder",
                HeaderText = "Папка-источник",
                FillWeight = 18
            });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "TargetFolder",
                HeaderText = "Папка-назначение",
                FillWeight = 18
            });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Extensions",
                HeaderText = "Расширения (через запятую)",
                FillWeight = 22
            });

            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ScreenScraperId",
                HeaderText = "ScreenScraper ID",
                FillWeight = 10
            });
            _grid.Columns["ScreenScraperId"].HeaderCell.ToolTipText =
                "Числовой ID системы в базе ScreenScraper.\n\n" +
                "Примеры:\n" +
                "  Mega Drive = 1\n" +
                "  NES = 3\n" +
                "  SNES = 4\n" +
                "  PlayStation 1 = 12\n" +
                "  Dreamcast = 23\n\n" +
                "Список всех ID: https://www.screenscraper.fr/api2/systemesListe.php?devid=...&output=json\n\n" +
                "Оставь пустым (или 0), если скрапинг для этой системы не нужен.";

            // Отдельно создаём колонку «Игры — папки», чтобы задать подсказку заголовку
            var folderCol = new DataGridViewCheckBoxColumn
            {
                Name = "GamesAreFolders",
                HeaderText = "Игры — папки",
                FillWeight = 10,
                FalseValue = false,
                TrueValue = true
            };
            folderCol.HeaderCell.ToolTipText =
                "Отметь галочкой, если одна игра — это целая папка с несколькими файлами внутри.\n\n" +
                "Пример: Dreamcast (Airforce Delta\\Airforce Delta.gdi + 30 треков),\n" +
                "Saturn (.cue + .bin-треки), Sega Master System (.sms внутри папки).\n\n" +
                "Для NES, SNES, GBA, PS1 и других — оставь пустым.";
            _grid.Columns.Add(folderCol);

            // ---- Панель кнопок ----
            var panelButtons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                Padding = new Padding(16, 8, 16, 8),
                FlowDirection = FlowDirection.LeftToRight
            };

            _btnAdd = new Button { Text = "Добавить", Width = 120, Height = 32 };
            _btnAdd.Click += (s, e) => AddRow();

            _btnDelete = new Button { Text = "Удалить", Width = 120, Height = 32 };
            _btnDelete.Click += (s, e) => DeleteSelected();

            _btnUp = new Button { Text = "Вверх", Width = 90, Height = 32 };
            _btnUp.Click += (s, e) => MoveSelected(-1);

            _btnDown = new Button { Text = "Вниз", Width = 90, Height = 32 };
            _btnDown.Click += (s, e) => MoveSelected(1);

            _btnSave = new Button { Text = "Сохранить", Width = 130, Height = 32 };
            _btnSave.Click += BtnSave_Click;

            _btnCancel = new Button { Text = "Отмена", Width = 110, Height = 32 };
            _btnCancel.Click += (s, e) => Close();

            panelButtons.Controls.Add(_btnAdd);
            panelButtons.Controls.Add(_btnDelete);
            panelButtons.Controls.Add(_btnUp);
            panelButtons.Controls.Add(_btnDown);
            panelButtons.Controls.Add(_btnSave);
            panelButtons.Controls.Add(_btnCancel);

            // ---- Путь к файлу ----
            _lblPath = new Label
            {
                Text = "Файл: " + GameSystems.GetSystemsFilePath(),
                Dock = DockStyle.Bottom,
                Height = 24,
                Padding = new Padding(16, 4, 16, 0),
                ForeColor = Color.Gray,
                AutoEllipsis = true
            };

            Controls.Add(_grid);
            Controls.Add(panelButtons);
            Controls.Add(_lblPath);
            Controls.Add(_lblHint);
        }

        // ============================================================
        // ЗАГРУЗКА / СОХРАНЕНИЕ
        // ============================================================
        private void LoadSystemsIntoGrid()
        {
            _grid.Rows.Clear();

            GameSystems.Reload();

            // Сброс подсказки на всякий случай.
            _lblHint.ForeColor = Color.DimGray;
            _lblHint.Text =
                "Двойной клик по ячейке — редактирование. Расширения указывай через запятую: .zip, .nes, .unf\r\n" +
                "Галочка «Игры — папки» — для систем, где одна игра это целая папка (Dreamcast, Saturn, Master System).";

            const string folderTooltip =
                "Отметь галочкой, если одна игра — это целая папка с несколькими файлами внутри.\r\n\r\n" +
                "Примеры:\r\n" +
                "✓ Dreamcast — папка с .gdi + 30 треков\r\n" +
                "✓ Saturn — папка с .cue + .bin-треки\r\n" +
                "✓ Sega Master System — папка с .sms внутри\r\n\r\n" +
                "Для NES, SNES, GBA, PS1 и других — оставь пустым.";

            int enriched = 0;

            foreach (var s in GameSystems.All)
            {
                int ssId = s.ScreenScraperSystemId;
                string extensions = string.Join(", ", s.PrimaryExtensions);
                string displayName = s.DisplayName;
                string targetFolder = s.TargetFolder;

                // Если чего-то не хватает — попробуем подставить из встроенной базы.
                if (ssId == 0 || string.IsNullOrWhiteSpace(extensions))
                {
                    var baseEntry = SystemsBase.FindByFolder(s.SourceFolder);
                    if (baseEntry != null)
                    {
                        bool changed = false;

                        if (ssId == 0 && baseEntry.ScreenScraperId > 0)
                        {
                            ssId = baseEntry.ScreenScraperId;
                            changed = true;
                        }

                        if (string.IsNullOrWhiteSpace(extensions) && baseEntry.PrimaryExtensions.Count > 0)
                        {
                            extensions = string.Join(", ", baseEntry.PrimaryExtensions);
                            changed = true;
                        }

                        if (changed) enriched++;
                    }
                }

                int idx = _grid.Rows.Add(
                    s.Id,
                    displayName,
                    s.SourceFolder,
                    targetFolder,
                    extensions,
                    ssId == 0 ? "" : ssId.ToString(),
                    s.GamesAreFolders);

                _grid.Rows[idx].Cells["GamesAreFolders"].ToolTipText = folderTooltip;
            }

            if (enriched > 0)
            {
                _lblHint.ForeColor = Color.DodgerBlue;
                _lblHint.Text =
                    $"Из встроенной базы подставлены ID ScreenScraper и/или расширения для {enriched} систем.\r\n" +
                    "Проверьте значения и нажмите «Сохранить».";
            }
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            // Валидация
            var systems = new List<GameSystem>();
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var folders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (DataGridViewRow row in _grid.Rows)
            {
                if (row.IsNewRow) continue;

                string id = (row.Cells["Id"].Value?.ToString() ?? "").Trim();
                string displayName = (row.Cells["DisplayName"].Value?.ToString() ?? "").Trim();
                string sourceFolder = (row.Cells["SourceFolder"].Value?.ToString() ?? "").Trim();
                string targetFolder = (row.Cells["TargetFolder"].Value?.ToString() ?? "").Trim();
                string extStr = (row.Cells["Extensions"].Value?.ToString() ?? "").Trim();
                string ssIdStr = (row.Cells["ScreenScraperId"].Value?.ToString() ?? "").Trim();
                bool isFolder = row.Cells["GamesAreFolders"].Value is bool b && b;

                int.TryParse(ssIdStr, out int ssId);

                // Пропускаем полностью пустые строки
                if (string.IsNullOrWhiteSpace(id) &&
                    string.IsNullOrWhiteSpace(displayName) &&
                    string.IsNullOrWhiteSpace(sourceFolder))
                    continue;

                if (string.IsNullOrWhiteSpace(id))
                {
                    Warn("Не указан ID системы в одной из строк.");
                    return;
                }
                if (string.IsNullOrWhiteSpace(sourceFolder))
                {
                    Warn($"Не указана папка-источник для системы «{id}».");
                    return;
                }
                if (!ids.Add(id))
                {
                    Warn($"Дублируется ID системы: {id}");
                    return;
                }
                if (!folders.Add(sourceFolder))
                {
                    Warn($"Дублируется папка-источник: {sourceFolder}");
                    return;
                }

                var extensions = extStr
                    .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .Where(x => x.Length > 0)
                    .Select(x => x.StartsWith(".") ? x : "." + x)
                    .ToArray();

                systems.Add(new GameSystem
                {
                    Id = id,
                    DisplayName = string.IsNullOrWhiteSpace(displayName) ? id : displayName,
                    SourceFolder = sourceFolder,
                    TargetFolder = string.IsNullOrWhiteSpace(targetFolder) ? id : targetFolder,
                    PrimaryExtensions = extensions,
                    GamesAreFolders = isFolder,
                    ScreenScraperSystemId = ssId
                });
            }

            if (systems.Count == 0)
            {
                Warn("Нечего сохранять — таблица пуста.");
                return;
            }

            // Запись JSON вручную — с отступами и правильным порядком
            string path = GameSystems.GetSystemsFilePath();
            try
            {
                var lines = new List<string>();
                lines.Add("{");
                lines.Add("  \"systems\": [");

                for (int i = 0; i < systems.Count; i++)
                {
                    var s = systems[i];
                    string extList = string.Join(", ", s.PrimaryExtensions.Select(x => $"\"{x}\""));
                    string comma = i < systems.Count - 1 ? "," : "";

                    lines.Add("    {");
                    lines.Add($"      \"id\": \"{Escape(s.Id)}\",");
                    lines.Add($"      \"displayName\": \"{Escape(s.DisplayName)}\",");
                    lines.Add($"      \"sourceFolder\": \"{Escape(s.SourceFolder)}\",");
                    lines.Add($"      \"targetFolder\": \"{Escape(s.TargetFolder)}\",");
                    lines.Add($"      \"primaryExtensions\": [{extList}],");
                    lines.Add($"      \"gamesAreFolders\": {(s.GamesAreFolders ? "true" : "false")},");
                    lines.Add($"      \"screenScraperSystemId\": {s.ScreenScraperSystemId}");
                    lines.Add("    }" + comma);
                }

                lines.Add("  ]");
                lines.Add("}");

                File.WriteAllLines(path, lines, System.Text.Encoding.UTF8);

                GameSystems.Reload();
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось сохранить: " + ex.Message,
                                "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static string Escape(string s)
        {
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        // ============================================================
        // РАБОТА С ТАБЛИЦЕЙ
        // ============================================================
        private void AddRow()
        {
            int idx = _grid.Rows.Add("", "", "", "", "", "", false);
            _grid.CurrentCell = _grid.Rows[idx].Cells["Id"];
            _grid.BeginEdit(true);
        }

        private void DeleteSelected()
        {
            if (_grid.SelectedRows.Count == 0) return;

            var row = _grid.SelectedRows[0];
            string id = row.Cells["Id"].Value?.ToString() ?? "";

            var answer = MessageBox.Show(
                $"Удалить систему «{id}»?",
                "Подтверждение",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (answer == DialogResult.Yes)
                _grid.Rows.Remove(row);
        }

        private void MoveSelected(int direction)
        {
            if (_grid.SelectedRows.Count == 0) return;

            int idx = _grid.SelectedRows[0].Index;
            int newIdx = idx + direction;
            if (newIdx < 0 || newIdx >= _grid.Rows.Count) return;

            // Меняем местами значения всех ячеек
            var rowA = _grid.Rows[idx];
            var rowB = _grid.Rows[newIdx];

            for (int c = 0; c < _grid.Columns.Count; c++)
            {
                var tmp = rowA.Cells[c].Value;
                rowA.Cells[c].Value = rowB.Cells[c].Value;
                rowB.Cells[c].Value = tmp;
            }

            _grid.ClearSelection();
            rowB.Selected = true;
            _grid.CurrentCell = rowB.Cells[0];
        }

        private void Warn(string text)
        {
            MessageBox.Show(text, "Проверка данных",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}