using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace GamePackBuilder.GUI
{
    /// <summary>
    /// Диалог переноса "заблудших" файлов из подпапок системы.
    /// Для каждого файла можно выбрать: в корень системы, в _Other, или пропустить.
    /// </summary>
    public class StrayFilesForm : Form
    {
        private DataGridView _grid = null!;
        private Button _btnApply = null!;
        private Button _btnSkip = null!;
        private Label _lblHint = null!;
        private Label _lblStats = null!;

        private readonly StrayScanResult _scan;

        public List<StrayFile> Accepted { get; } = new();

        public StrayFilesForm(StrayScanResult scan)
        {
            _scan = scan;

            BuildUI();
            ThemeManager.Apply(this);
            FillGrid();
            UpdateStats();
        }

        private void BuildUI()
        {
            Text = "Найдены игры в подпапках";
            Size = new Size(1150, 650);
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(900, 550);
            Font = new Font("Segoe UI", 10F);

            _lblHint = new Label
            {
                Dock = DockStyle.Top,
                Height = 100,
                Padding = new Padding(16, 8, 16, 0),
                Text =
                    $"В подпапках системы «{_scan.SystemId}» найдены игровые файлы.\r\n" +
                    "Выберите для каждого, куда его перенести:\r\n" +
                    "  • «В систему» — в корень основной папки системы;\r\n" +
                    "  • «В _Other» — в отдельную папку со вспомогательными файлами (по умолчанию);\r\n" +
                    "  • «Пропустить» — оставить как есть."
            };

            _grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9.5F),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            // Выпадающий список с вариантами.
            var targetCol = new DataGridViewComboBoxColumn
            {
                Name = "Target",
                HeaderText = "Куда",
                FillWeight = 16,
                FlatStyle = FlatStyle.Flat,
                DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton
            };
            targetCol.Items.AddRange("В _Other", "В систему", "Пропустить");
            _grid.Columns.Add(targetCol);

            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "SubFolder",
                HeaderText = "Из папки",
                FillWeight = 15,
                ReadOnly = true
            });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "FileName",
                HeaderText = "Файл",
                FillWeight = 55,
                ReadOnly = true
            });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Size",
                HeaderText = "Размер",
                FillWeight = 10,
                ReadOnly = true
            });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Status",
                HeaderText = "Статус",
                FillWeight = 14,
                ReadOnly = true
            });

            _grid.CellValueChanged += (_, _) => UpdateStats();

            // ---- Панель статистики ----
            _lblStats = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 28,
                Padding = new Padding(16, 4, 16, 0),
                ForeColor = Color.DimGray
            };

            // ---- Кнопки массовых действий ----
            var panelTop = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 44,
                Padding = new Padding(16, 8, 16, 4),
                FlowDirection = FlowDirection.LeftToRight
            };

            var btnAllOther = new Button { Text = "Всё в _Other", Width = 140, Height = 30 };
            btnAllOther.Click += (_, _) => SetAllTargets(StrayTarget.Other);

            var btnAllSystem = new Button { Text = "Всё в систему", Width = 140, Height = 30 };
            btnAllSystem.Click += (_, _) => SetAllTargets(StrayTarget.System);

            var btnAllSkip = new Button { Text = "Всё пропустить", Width = 140, Height = 30 };
            btnAllSkip.Click += (_, _) => SetAllTargets(StrayTarget.Skip);

            panelTop.Controls.Add(btnAllOther);
            panelTop.Controls.Add(btnAllSystem);
            panelTop.Controls.Add(btnAllSkip);

            // ---- Кнопки подтверждения ----
            var panelBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 56,
                Padding = new Padding(16, 10, 16, 10)
            };

            _btnSkip = new Button
            {
                Text = "Отмена",
                Width = 130,
                Height = 32,
                Dock = DockStyle.Right
            };
            _btnSkip.Click += (_, _) =>
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };

            _btnApply = new Button
            {
                Text = "Применить",
                Width = 180,
                Height = 32,
                Dock = DockStyle.Right
            };
            _btnApply.Click += (_, _) => ApplyAndClose();

            panelBottom.Controls.Add(_btnApply);
            panelBottom.Controls.Add(_btnSkip);

            Controls.Add(_grid);
            Controls.Add(_lblStats);
            Controls.Add(panelTop);
            Controls.Add(panelBottom);
            Controls.Add(_lblHint);
        }

        private void SetAllTargets(StrayTarget target)
        {
            foreach (DataGridViewRow row in _grid.Rows)
            {
                if (row.Tag is StrayFile sf && sf.IsDuplicate) continue;
                row.Cells["Target"].Value = TargetToString(target);
            }
            UpdateStats();
        }

        private void FillGrid()
        {
            foreach (var sf in _scan.Candidates)
            {
                bool isDup = sf.IsDuplicate;

                int idx = _grid.Rows.Add(
                    TargetToString(sf.Target),
                    sf.SubFolderName,
                    Path.GetFileName(sf.FullPath),
                    FormatSize(sf.Size),
                    isDup ? "уже есть в корне" : "");

                _grid.Rows[idx].Tag = sf;
                _grid.Rows[idx].Cells["FileName"].ToolTipText = sf.FullPath;

                if (isDup)
                {
                    _grid.Rows[idx].Cells["Target"].ReadOnly = true;
                    _grid.Rows[idx].Cells["Target"].Value = "Пропустить";
                    _grid.Rows[idx].DefaultCellStyle.ForeColor = Color.Gray;
                }
            }
        }

        private void UpdateStats()
        {
            int total = _grid.Rows.Count;
            int toOther = 0, toSystem = 0;
            long bytesOther = 0, bytesSystem = 0;

            foreach (DataGridViewRow row in _grid.Rows)
            {
                if (row.Tag is not StrayFile sf) continue;
                string? v = row.Cells["Target"].Value?.ToString();

                if (v == "В _Other") { toOther++; bytesOther += sf.Size; }
                else if (v == "В систему") { toSystem++; bytesSystem += sf.Size; }
            }

            _lblStats.Text =
                $"Всего: {total}    " +
                $"В систему: {toSystem} ({FormatSize(bytesSystem)})    " +
                $"В _Other: {toOther} ({FormatSize(bytesOther)})";
        }

        private void ApplyAndClose()
        {
            Accepted.Clear();

            foreach (DataGridViewRow row in _grid.Rows)
            {
                if (row.Tag is not StrayFile sf) continue;
                if (sf.IsDuplicate) continue;

                string? v = row.Cells["Target"].Value?.ToString();
                sf.Target = v switch
                {
                    "В систему" => StrayTarget.System,
                    "В _Other" => StrayTarget.Other,
                    _ => StrayTarget.Skip
                };

                if (sf.Target != StrayTarget.Skip)
                    Accepted.Add(sf);
            }

            if (Accepted.Count == 0)
            {
                DialogResult = DialogResult.Cancel;
                Close();
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }

        private static string TargetToString(StrayTarget t) => t switch
        {
            StrayTarget.System => "В систему",
            StrayTarget.Other => "В _Other",
            _ => "Пропустить"
        };

        private void InitializeComponent()
        {

        }

        private static string FormatSize(long bytes)
        {
            if (bytes < 1024) return bytes + " Б";
            if (bytes < 1024 * 1024) return (bytes / 1024.0).ToString("0.0") + " КБ";
            if (bytes < 1024L * 1024 * 1024) return (bytes / (1024.0 * 1024)).ToString("0.0") + " МБ";
            return (bytes / (1024.0 * 1024 * 1024)).ToString("0.00") + " ГБ";
        }
    }
}