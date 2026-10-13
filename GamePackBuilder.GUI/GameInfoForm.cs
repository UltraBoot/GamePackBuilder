using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Collections.Generic;

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
        private NoFocusTextBox _txtTitle = null!;
        private PictureBox _picCover = null!;
        private ListView _lstScreenshots = null!;
        private ImageList _imageList = null!;
        private NoFocusTextBox _txtYear = null!;
        private NoFocusTextBox _txtDeveloper = null!;
        private NoFocusTextBox _txtGenre = null!;
        private NoFocusTextBox _txtPlayers = null!;
        private NoFocusTextBox _txtDescription = null!;
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
        private PictureBox _previewBox = null!;
        private FlowLayoutPanel _mediaLinksPanel = null!;
        private Button _btnOpenWheel = null!;
        private Button _btnOpenFanart = null!;
        private Button _btnOpenVideo = null!;
        private Button _btnOpenManual = null!;

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

            string gameName = Directory.Exists(_filePath)
                ? Path.GetFileName(_filePath)
                : Path.GetFileNameWithoutExtension(_filePath);

            if (string.IsNullOrWhiteSpace(_info.Title))
                _info.Title = gameName;

            // Ищем уже существующие медиа в папке системы — но НЕ переносим, только читаем.
            MediaScanner.EnrichGameInfo(_info, _basePath, gameName);

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
            UpdateMediaLinks();
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
            _info.ScraperFields.Clear();

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
        // SCREENSCRAPER
        // ============================================================

        private async void BtnScrape_Click(object? sender, EventArgs e)
        {
            var s = AppSettings.Load();
            if (string.IsNullOrWhiteSpace(s.ScreenScraperUser) ||
                string.IsNullOrWhiteSpace(s.ScreenScraperPassword))
            {
                MessageBox.Show(this,
                    "Сначала заполни логин и пароль ScreenScraper в главном окне (кнопка «ScreenScraper»).",
                    "Нет данных", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var system = GameSystems.FindById(_systemId);
            if (system == null)
            {
                MessageBox.Show(this,
                    $"Система «{_systemId}» не найдена в systems.json.",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (system.ScreenScraperSystemId <= 0)
            {
                MessageBox.Show(this,
                    $"Для системы «{system.DisplayName}» не задан ScreenScraper ID.\r\n\r\n" +
                    "Открой «Редактор систем» в главном окне и укажи числовой ID.",
                    "Нет ID", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _btnScrape.Enabled = false;
            _progress.Visible = true;
            _progress.Style = ProgressBarStyle.Marquee;
            _progress.MarqueeAnimationSpeed = 25;
            _lblFilePath.Text = "Загрузка с ScreenScraper...";
            _lblFilePath.ForeColor = Color.DodgerBlue;
            DebugLog.Write($"Scrape (карточка): {_systemId}, rom={Path.GetFileName(_filePath)}");

            // Спросим у пользователя, что качать.
            using var optionsForm = new MediaDownloadOptionsForm();
            if (optionsForm.ShowDialog(this) != DialogResult.OK)
                return;

            try
            {
                var service = new ScreenScraperService();
                var result = await service.ScrapeGameAsync(
                    system, _info, _filePath, _basePath,
                    s.ScreenScraperUser, s.ScreenScraperPassword,
                    optionsForm.Options);

                if (result.GameNotFound)
                {
                    _lblFilePath.Text = "Игра не найдена в базе ScreenScraper.";
                    _lblFilePath.ForeColor = Color.OrangeRed;
                    return;
                }

                if (!result.Success || result.Info == null)
                {
                    _lblFilePath.Text = "Ошибка: " + (result.ErrorMessage ?? "неизвестно");
                    _lblFilePath.ForeColor = Color.Firebrick;
                    return;
                }

                _info = result.Info;
                PushInfoToUi();

                _lblFilePath.Text = $"Готово. Полей: {result.TextFieldsFilled}, медиа: {result.MediaDownloaded}.";
                _lblFilePath.ForeColor = Color.Green;

                if (!_editMode)
                    SetEditMode(true);
            }
            catch (Exception ex)
            {
                DebugLog.Write("Ошибка scrape: " + ex.Message);
                MessageBox.Show(this, "Ошибка при обращении к ScreenScraper:\r\n" + ex.Message,
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _btnScrape.Enabled = true;
                _progress.Visible = false;
                _progress.Style = ProgressBarStyle.Blocks;
            }
        }

        
        /// <summary>
        /// Можно ли заполнить поле: пустое или ранее заполнено скрапером.
        /// Если поле заполнено вручную (не в ScraperFields) — не трогаем.
        /// </summary>
        private static bool CanSet(string fieldName, List<string> scraperFields, string currentValue)
        {
            if (string.IsNullOrWhiteSpace(currentValue))
                return true;
            return scraperFields.Contains(fieldName);
        }

        /// <summary>Помечает поле как заполненное скрапером (один раз, без дублей).</summary>
        private void MarkScraped(string fieldName)
        {
            if (!_info.ScraperFields.Contains(fieldName))
                _info.ScraperFields.Add(fieldName);
        }
        /// <summary>Выбирает элемент с приоритетным регионом: wor -> us -> eu -> jp.</summary>
        
    }
}