using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace GamePackBuilder.GUI
{
    public partial class GameInfoForm : Form
    {
        private readonly string _systemId;
        private readonly string _basePath;
        private readonly string _filePath;
        private readonly string _relativePath;

        private GameInfo _info = new();
        private GameInfo _backup = new();
        private bool _editMode = false;

        // Элементы управления
        private TextBox _txtTitle = null!;
        private PictureBox _picCover = null!;
        private ListView _lstScreenshots = null!;
        private ImageList _imageList = null!;
        private TextBox _txtYear = null!;
        private TextBox _txtDeveloper = null!;
        private TextBox _txtGenre = null!;
        private TextBox _txtPlayers = null!;
        private TextBox _txtDescription = null!;
        private Label _lblFilePath = null!;
        private Button _btnAddCover = null!;
        private Button _btnRemoveCover = null!;
        private Button _btnAddScreenshot = null!;
        private Button _btnRemoveScreenshot = null!;
        private Button _btnEdit = null!;
        private Button _btnSave = null!;
        private Button _btnCancel = null!;
        private Button _btnScrape = null!;
        private Button _btnClose = null!;
        private ProgressBar _progress = null!;
        private TabControl _tabs = null!;

        public GameInfoForm(string systemId, string basePath, string filePath)
        {
            _systemId = systemId;
            _basePath = basePath;
            _filePath = filePath;
            _relativePath = Path.GetRelativePath(basePath, filePath);

            InitializeComponent();
            BuildUI();
            ThemeManager.Apply(this);
            LoadInfo();
            SetEditMode(false);
        }

        // ============================================================
        // ЗАГРУЗКА
        // ============================================================
        private void LoadInfo()
        {
            GameInfoStore.Load();
            _info = GameInfoStore.Get(_systemId, _relativePath);

            if (string.IsNullOrWhiteSpace(_info.Title))
            {
                string name = Directory.Exists(_filePath)
                    ? Path.GetFileName(_filePath)
                    : Path.GetFileNameWithoutExtension(_filePath);
                _info.Title = name;
            }

            PushInfoToUi();
        }

        private void PushInfoToUi()
        {
            _txtTitle.Text = _info.Title;
            _txtYear.Text = _info.Year;
            _txtDeveloper.Text = _info.Developer;
            _txtGenre.Text = _info.Genre;
            _txtPlayers.Text = _info.Players;
            _txtDescription.Text = _info.Description;

            LoadCover();
            LoadScreenshots();
        }

        private void PullUiToInfo()
        {
            _info.Title = _txtTitle.Text.Trim();
            _info.Year = _txtYear.Text.Trim();
            _info.Developer = _txtDeveloper.Text.Trim();
            _info.Genre = _txtGenre.Text.Trim();
            _info.Players = _txtPlayers.Text.Trim();
            _info.Description = _txtDescription.Text;
        }

        // ============================================================
        // РЕДАКТИРОВАНИЕ
        // ============================================================
        private static GameInfo CloneInfo(GameInfo src) => new()
        {
            Title = src.Title,
            Year = src.Year,
            Developer = src.Developer,
            Genre = src.Genre,
            Players = src.Players,
            Description = src.Description,
            CoverPath = src.CoverPath,
            Screenshots = new System.Collections.Generic.List<string>(src.Screenshots)
        };

        private void CancelEdit()
        {
            _info = CloneInfo(_backup);
            PushInfoToUi();
            SetEditMode(false);
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            PullUiToInfo();

            GameInfoStore.Set(_systemId, _relativePath, _info);
            GameInfoStore.Save();

            SetEditMode(false);
            _lblFilePath.Text = "Сохранено: " + DateTime.Now.ToString("HH:mm:ss");
            _lblFilePath.ForeColor = Color.Green;

            var t = new System.Windows.Forms.Timer { Interval = 3000 };
            t.Tick += (s, ev) =>
            {
                t.Stop();
                t.Dispose();
                _lblFilePath.Text = "Файл: " + _filePath;
                _lblFilePath.ForeColor = Color.Gray;
            };
            t.Start();
        }

        // ============================================================
        // SCREENSCRAPER (заглушка)
        // ============================================================
        private void BtnScrape_Click(object? sender, EventArgs e)
        {
            MessageBox.Show(
                "Загрузка с ScreenScraper.fr будет здесь.\r\n\r\n" +
                "Прогресс-бар внизу покажет ход загрузки.\r\n" +
                "Сейчас кнопка не активна — это следующий шаг.",
                "В разработке",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}