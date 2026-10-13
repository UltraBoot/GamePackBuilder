using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace GamePackBuilder.GUI
{
    /// <summary>
    /// Диалог разрешения конфликтов между базой и gamelist.xml.
    /// Показывает список расхождений с галочками — какие значения применить из gamelist.
    /// </summary>
    public class ConflictsForm : Form
    {
        private DataGridView _grid = null!;
        private Button _btnApply = null!;
        private Button _btnSkipAll = null!;
        private Label _lblHint = null!;

        public List<GamelistConflict> Conflicts { get; }
        public HashSet<GamelistConflict> Accepted { get; } = new();

        public ConflictsForm(List<GamelistConflict> conflicts)
        {
            Conflicts = conflicts;
            BuildUI();
            ThemeManager.Apply(this);
            FillGrid();
        }

        private void BuildUI()
        {
            Text = "Конфликты с gamelist.xml";
            Size = new Size(1100, 620);
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(800, 500);
            Font = new Font("Segoe UI", 10F);

            _lblHint = new Label
            {
                Dock = DockStyle.Top,
                Height = 60,
                Padding = new Padding(16, 8, 16, 0),
                Text = "В базе уже есть данные, которые отличаются от gamelist.xml.\r\n" +
                       "Отметь галочками, какие значения взять из gamelist. Снятые галочки — оставить как в базе.\r\n" +
                       "Двойной клик по строке — быстрое переключение галочки."
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

            _grid.Columns.Add(new DataGridViewCheckBoxColumn
            {
                Name = "Apply",
                HeaderText = "Взять",
                FillWeight = 6
            });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Game",
                HeaderText = "Игра",
                FillWeight = 22,
                ReadOnly = true
            });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Field",
                HeaderText = "Поле",
                FillWeight = 12,
                ReadOnly = true
            });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Old",
                HeaderText = "Сейчас в базе",
                FillWeight = 30,
                ReadOnly = true
            });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "New",
                HeaderText = "Из gamelist.xml",
                FillWeight = 30,
                ReadOnly = true
            });

            _grid.CellDoubleClick += (_, e) =>
            {
                if (e.RowIndex < 0) return;
                var cell = _grid.Rows[e.RowIndex].Cells["Apply"];
                bool cur = cell.Value is bool b && b;
                cell.Value = !cur;
            };

            // ---- Кнопки ----
            var panelBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 56,
                Padding = new Padding(16, 10, 16, 10)
            };

            var btnApplyAll = new Button
            {
                Text = "Отметить все",
                Width = 140,
                Height = 32,
                Dock = DockStyle.Left
            };
            btnApplyAll.Click += (_, _) =>
            {
                foreach (DataGridViewRow row in _grid.Rows)
                    row.Cells["Apply"].Value = true;
            };

            var btnUncheckAll = new Button
            {
                Text = "Снять все",
                Width = 140,
                Height = 32,
                Dock = DockStyle.Left
            };
            btnUncheckAll.Click += (_, _) =>
            {
                foreach (DataGridViewRow row in _grid.Rows)
                    row.Cells["Apply"].Value = false;
            };

            _btnSkipAll = new Button
            {
                Text = "Пропустить всё",
                Width = 140,
                Height = 32,
                Dock = DockStyle.Right
            };
            _btnSkipAll.Click += (_, _) =>
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };

            _btnApply = new Button
            {
                Text = "Применить отмеченное",
                Width = 200,
                Height = 32,
                Dock = DockStyle.Right
            };
            _btnApply.Click += (_, _) =>
            {
                Accepted.Clear();
                foreach (DataGridViewRow row in _grid.Rows)
                {
                    if (row.Cells["Apply"].Value is bool b && b && row.Tag is GamelistConflict c)
                        Accepted.Add(c);
                }
                DialogResult = DialogResult.OK;
                Close();
            };

            panelBottom.Controls.Add(_btnApply);
            panelBottom.Controls.Add(_btnSkipAll);
            panelBottom.Controls.Add(btnUncheckAll);
            panelBottom.Controls.Add(btnApplyAll);

            Controls.Add(_grid);
            Controls.Add(panelBottom);
            Controls.Add(_lblHint);

            AcceptButton = _btnApply;
            CancelButton = _btnSkipAll;
        }

        private void FillGrid()
        {
            foreach (var c in Conflicts)
            {
                int idx = _grid.Rows.Add(
                    true,
                    c.GameTitle,
                    c.FieldLabel,
                    Truncate(c.OldValue, 300),
                    Truncate(c.NewValue, 300));

                _grid.Rows[idx].Tag = c;
                _grid.Rows[idx].Cells["Old"].ToolTipText = c.OldValue;
                _grid.Rows[idx].Cells["New"].ToolTipText = c.NewValue;
                _grid.Rows[idx].Cells["Game"].ToolTipText = c.GameTitle;
            }
        }

        private static string Truncate(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) return "";
            s = s.Replace("\r", " ").Replace("\n", " ");
            return s.Length <= max ? s : s.Substring(0, max) + "…";
        }
    }
}