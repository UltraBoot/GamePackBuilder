using System.Drawing;
using System.Windows.Forms;

namespace GamePackBuilder.GUI
{
    public partial class GameInfoForm
    {
        private void BuildUI()
        {
            Text = "Информация об игре";
            Size = new Size(900, 660);
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(820, 580);
            Font = new Font("Segoe UI", 10F);
            BackColor = Color.White;

            // ---- Заголовок ----
            var topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 76,
                Padding = new Padding(16, 10, 16, 10),
                BackColor = Color.FromArgb(245, 245, 245)
            };

            var lblTitleCaption = new Label
            {
                Text = "Название игры:",
                Dock = DockStyle.Top,
                Height = 18,
                ForeColor = Color.Gray
            };

            _txtTitle = new NoFocusTextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 15F, FontStyle.Bold),
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(245, 245, 245),
                ForeColor = Color.Black,
                ReadOnly = true
            };

            topPanel.Controls.Add(_txtTitle);
            topPanel.Controls.Add(lblTitleCaption);

            // ---- Центр ----
            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(16, 8, 16, 8)
            };
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 340));
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            // ---- Вкладки с картинками ----
            _tabs = new TabControl { Dock = DockStyle.Fill };

            var coverTab = new TabPage("Обложка");
            var coverPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8) };

            _picCover = new PictureBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.FromArgb(250, 250, 250)
            };

            var coverButtons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 40,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(0, 4, 0, 0)
            };

            _btnAddCover = new Button { Text = "Выбрать…", Width = 110, Height = 28 };
            _btnAddCover.Click += BtnAddCover_Click;

            _btnRemoveCover = new Button { Text = "Убрать", Width = 90, Height = 28 };
            _btnRemoveCover.Click += BtnRemoveCover_Click;

            coverButtons.Controls.Add(_btnAddCover);
            coverButtons.Controls.Add(_btnRemoveCover);

            coverPanel.Controls.Add(_picCover);
            coverPanel.Controls.Add(coverButtons);
            coverTab.Controls.Add(coverPanel);

            var shotsTab = new TabPage("Скриншоты");
            var shotsPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8) };

            _imageList = new ImageList { ImageSize = new Size(120, 90), ColorDepth = ColorDepth.Depth32Bit };

            _lstScreenshots = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.LargeIcon,
                LargeImageList = _imageList,
                MultiSelect = true,
                BorderStyle = BorderStyle.FixedSingle
            };

            var shotButtons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 40,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(0, 4, 0, 0)
            };

            _btnAddScreenshot = new Button { Text = "Добавить…", Width = 120, Height = 28 };
            _btnAddScreenshot.Click += BtnAddScreenshot_Click;

            _btnRemoveScreenshot = new Button { Text = "Убрать", Width = 90, Height = 28 };
            _btnRemoveScreenshot.Click += BtnRemoveScreenshot_Click;

            shotButtons.Controls.Add(_btnAddScreenshot);
            shotButtons.Controls.Add(_btnRemoveScreenshot);

            shotsPanel.Controls.Add(_lstScreenshots);
            shotsPanel.Controls.Add(shotButtons);
            shotsTab.Controls.Add(shotsPanel);

            _tabs.TabPages.Add(coverTab);
            _tabs.TabPages.Add(shotsTab);

            // ---- Поля ----
            var fieldsPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 6,
                Padding = new Padding(16, 0, 0, 0)
            };
            fieldsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            fieldsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            for (int i = 0; i < 5; i++)
                fieldsPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            fieldsPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            _txtYear = MakeField();
            _txtDeveloper = MakeField();
            _txtGenre = MakeField();
            _txtPlayers = MakeField();
            _txtDescription = MakeField(multiline: true);

            fieldsPanel.Controls.Add(MakeCaption("Год:"), 0, 0);
            fieldsPanel.Controls.Add(_txtYear, 1, 0);

            fieldsPanel.Controls.Add(MakeCaption("Разработчик:"), 0, 1);
            fieldsPanel.Controls.Add(_txtDeveloper, 1, 1);

            fieldsPanel.Controls.Add(MakeCaption("Жанр:"), 0, 2);
            fieldsPanel.Controls.Add(_txtGenre, 1, 2);

            fieldsPanel.Controls.Add(MakeCaption("Игроков:"), 0, 3);
            fieldsPanel.Controls.Add(_txtPlayers, 1, 3);

            fieldsPanel.Controls.Add(new Label { Text = "" }, 0, 4);
            fieldsPanel.Controls.Add(new Label { Text = "" }, 1, 4);

            var descPanel = new Panel { Dock = DockStyle.Fill };
            descPanel.Controls.Add(_txtDescription);
            descPanel.Controls.Add(new Label
            {
                Text = "Описание:",
                Dock = DockStyle.Top,
                Height = 22,
                ForeColor = Color.Gray
            });

            fieldsPanel.Controls.Add(descPanel, 0, 5);
            fieldsPanel.SetColumnSpan(descPanel, 2);

            mainPanel.Controls.Add(_tabs, 0, 0);
            mainPanel.Controls.Add(fieldsPanel, 1, 0);

            // ---- Низ ----
            var bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 100,
                Padding = new Padding(16, 8, 16, 8),
                BackColor = Color.FromArgb(250, 250, 250)
            };

            _lblFilePath = new Label
            {
                Text = "Файл: " + _filePath,
                ForeColor = Color.Gray,
                Dock = DockStyle.Top,
                Height = 22,
                AutoEllipsis = true
            };

            _progress = new ProgressBar
            {
                Dock = DockStyle.Top,
                Height = 18,
                Style = ProgressBarStyle.Continuous,
                Value = 0,
                Visible = false
            };

            var buttonsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                Height = 40,
                Padding = new Padding(0, 6, 0, 0)
            };

            _btnClose = new Button { Text = "Закрыть", Width = 110, Height = 30 };
            _btnClose.Click += (s, e) => Close();

            _btnEdit = new Button { Text = "Редактировать", Width = 150, Height = 30 };
            _btnEdit.Click += (s, e) => SetEditMode(true);

            _btnScrape = new Button { Text = "Загрузить с ScreenScraper", Width = 220, Height = 30 };
            _btnScrape.Click += BtnScrape_Click;

            _btnSave = new Button { Text = "Сохранить", Width = 130, Height = 30 };
            _btnSave.Click += BtnSave_Click;

            _btnCancel = new Button { Text = "Отмена", Width = 110, Height = 30 };
            _btnCancel.Click += (s, e) => CancelEdit();

            buttonsPanel.Controls.Add(_btnClose);
            buttonsPanel.Controls.Add(_btnEdit);
            buttonsPanel.Controls.Add(_btnScrape);
            buttonsPanel.Controls.Add(_btnSave);
            buttonsPanel.Controls.Add(_btnCancel);

            bottomPanel.Controls.Add(_progress);
            bottomPanel.Controls.Add(_lblFilePath);
            bottomPanel.Controls.Add(buttonsPanel);

            Controls.Add(mainPanel);
            Controls.Add(bottomPanel);
            Controls.Add(topPanel);
        }

        private static Label MakeCaption(string text) => new Label
        {
            Text = text,
            TextAlign = ContentAlignment.MiddleLeft,
            Dock = DockStyle.Fill,
            ForeColor = Color.Gray
        };

        private static NoFocusTextBox MakeField(bool multiline = false) => new NoFocusTextBox
        {
            Dock = DockStyle.Fill,
            Multiline = multiline,
            ScrollBars = multiline ? ScrollBars.Vertical : ScrollBars.None,
            AcceptsReturn = multiline,
            WordWrap = multiline,
            BorderStyle = BorderStyle.None,
            ReadOnly = true
        };

        // ============================================================
        // РЕЖИМ РЕДАКТИРОВАНИЯ
        // ============================================================
        private void SetEditMode(bool editing)
        {
            _editMode = editing;

            if (editing)
            {
                _backup = CloneInfo(_info);
            }

            // Заголовок
            _txtTitle.ReadOnly = !editing;
            _txtTitle.BorderStyle = editing ? BorderStyle.FixedSingle : BorderStyle.None;

            // Поля
            foreach (var tb in new[] { _txtYear, _txtDeveloper, _txtGenre, _txtPlayers, _txtDescription })
            {
                tb.ReadOnly = !editing;
                tb.BorderStyle = editing ? BorderStyle.FixedSingle : BorderStyle.None;
            }

            // Кнопки картинок
            _btnAddCover.Visible = editing;
            _btnRemoveCover.Visible = editing;
            _btnAddScreenshot.Visible = editing;
            _btnRemoveScreenshot.Visible = editing;

            // Кнопки внизу
            _btnEdit.Visible = !editing;
            _btnScrape.Visible = !editing;
            _btnSave.Visible = editing;
            _btnCancel.Visible = editing;
            _btnClose.Visible = !editing;

            if (editing)
            {
                _lblFilePath.Text = "РЕЖИМ РЕДАКТИРОВАНИЯ — изменения не сохранятся, пока не нажмёшь «Сохранить»";
                _lblFilePath.ForeColor = Color.OrangeRed;
            }
            else
            {
                _lblFilePath.Text = "Файл: " + _filePath;
                _lblFilePath.ForeColor = Color.Gray;
            }

            // Применяем тему после всех изменений,
            // чтобы цвета полей соответствовали текущей теме.
            ThemeManager.Apply(this);
        }
    }
}