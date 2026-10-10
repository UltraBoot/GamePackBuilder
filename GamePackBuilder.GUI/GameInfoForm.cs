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
                    "Открой «Редактор систем» в главном окне и укажи числовой ID " +
                    "(например, для Mega Drive — 1, для NES — 3).\r\n\r\n" +
                    "Полный список ID можно посмотреть по адресу:\r\n" +
                    "https://www.screenscraper.fr/api2/systemesListe.php?devid=...&output=json",
                    "Нет ID", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string romName = Path.GetFileName(_filePath);
            _btnScrape.Enabled = false;
            _progress.Visible = true;
            _progress.Style = ProgressBarStyle.Marquee;
            DebugLog.Write($"Scrape: system={_systemId}, ssId={system.ScreenScraperSystemId}, rom={romName}");

            try
            {
                var client = new ScreenScraperClient();
                var response = await client.GetGameInfoByNameAsync(
                    s.ScreenScraperUser, s.ScreenScraperPassword, system.ScreenScraperSystemId, romName);

                var g = response.Response?.Game;
                if (g == null)
                {
                    MessageBox.Show(this, "Игра не найдена в базе ScreenScraper.",
                        "Не найдено", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    DebugLog.Write("Игра не найдена.");
                    return;
                }

                int filled = 0;

                // Заполняем пустые ИЛИ ранее скрапнутые поля (но не правки пользователя).
                if (CanSet("Title", _info.ScraperFields, _txtTitle.Text) && g.Names?.Count > 0)
                {
                    _txtTitle.Text = g.Names[0].Text ?? "";
                    MarkScraped("Title");
                    filled++;
                }

                if (CanSet("Year", _info.ScraperFields, _txtYear.Text))
                {
                    var date = PickByRegion(g.Dates) ?? g.Dates?[0];
                    if (!string.IsNullOrWhiteSpace(date?.Text))
                    {
                        _txtYear.Text = date!.Text!.Length >= 4 ? date.Text.Substring(0, 4) : date.Text;
                        MarkScraped("Year");
                        filled++;
                    }
                }

                if (CanSet("Developer", _info.ScraperFields, _txtDeveloper.Text) && g.Developer != null)
                {
                    _txtDeveloper.Text = g.Developer.Text ?? "";
                    MarkScraped("Developer");
                    filled++;
                }

                if (CanSet("Genre", _info.ScraperFields, _txtGenre.Text) && g.Genres?.Count > 0)
                {
                    string genre = g.Genres[0].Names?.Find(n => n.Language == "en")?.Text
                                ?? g.Genres[0].Names?[0].Text
                                ?? "";
                    _txtGenre.Text = genre;
                    MarkScraped("Genre");
                    filled++;
                }

                if (CanSet("Players", _info.ScraperFields, _txtPlayers.Text) && g.Players != null)
                {
                    _txtPlayers.Text = g.Players.Text ?? "";
                    MarkScraped("Players");
                    filled++;
                }

                if (CanSet("Description", _info.ScraperFields, _txtDescription.Text) && g.Descriptions?.Count > 0)
                {
                    string desc = g.Descriptions.Find(d => d.Language == "en")?.Text
                               ?? g.Descriptions[0].Text
                               ?? "";
                    _txtDescription.Text = desc;
                    MarkScraped("Description");
                    filled++;
                }

                _lblFilePath.Text = $"Получено с ScreenScraper. Заполнено пустых полей: {filled}.";
                _lblFilePath.ForeColor = Color.Green;
                DebugLog.Write($"Scrape успешен. Заполнено полей: {filled}.");

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
        private static ScreenScraperLocalizedText? PickByRegion(
            System.Collections.Generic.List<ScreenScraperLocalizedText>? list)
        {
            if (list == null || list.Count == 0) return null;
            string[] order = { "wor", "us", "eu", "jp" };
            foreach (var reg in order)
            {
                var hit = list.Find(x => x.Region == reg);
                if (hit != null) return hit;
            }
            return null;
        }
    }
}