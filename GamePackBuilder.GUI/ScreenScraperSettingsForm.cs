using System;
using System.Drawing;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GamePackBuilder.GUI
{
    /// <summary>
    /// Окно настроек ScreenScraper: логин/пароль пользователя и проверка соединения.
    /// </summary>
    public class ScreenScraperSettingsForm : Form
    {
        private TextBox _txtUser = null!;
        private TextBox _txtPassword = null!;
        private Label _lblStatus = null!;
        private Button _btnCheck = null!;
        private Button _btnSave = null!;
        private Button _btnCancel = null!;

        private readonly AppSettings _settings;

        public ScreenScraperSettingsForm(AppSettings settings)
        {
            _settings = settings;
            BuildUI();
            LoadValues();
            ThemeManager.Apply(this);
        }

        private void BuildUI()
        {
            Text = "Настройки ScreenScraper";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(520, 220);

            var lblUser = new Label
            {
                Text = "Логин (ssid):",
                Location = new Point(15, 20),
                AutoSize = true
            };

            _txtUser = new TextBox
            {
                Location = new Point(160, 17),
                Width = 340
            };

            var lblPassword = new Label
            {
                Text = "Пароль (sspassword):",
                Location = new Point(15, 55),
                AutoSize = true
            };

            _txtPassword = new TextBox
            {
                Location = new Point(160, 52),
                Width = 340,
                UseSystemPasswordChar = true
            };

            _btnCheck = new Button
            {
                Text = "Проверить",
                Location = new Point(15, 100),
                Width = 120
            };
            _btnCheck.Click += async (_, _) => await CheckAsync();

            _lblStatus = new Label
            {
                Location = new Point(15, 140),
                Size = new Size(485, 40),
                Text = ""
            };

            _btnSave = new Button
            {
                Text = "Сохранить",
                Location = new Point(400, 180),
                Width = 100,
                DialogResult = DialogResult.None
            };
            _btnSave.Click += (_, _) => SaveAndClose();

            _btnCancel = new Button
            {
                Text = "Отмена",
                Location = new Point(300, 180),
                Width = 90,
                DialogResult = DialogResult.Cancel
            };

            Controls.Add(lblUser);
            Controls.Add(_txtUser);
            Controls.Add(lblPassword);
            Controls.Add(_txtPassword);
            Controls.Add(_btnCheck);
            Controls.Add(_lblStatus);
            Controls.Add(_btnSave);
            Controls.Add(_btnCancel);

            AcceptButton = _btnCheck;
            CancelButton = _btnCancel;
        }

        private void LoadValues()
        {
            _txtUser.Text = _settings.ScreenScraperUser;
            _txtPassword.Text = _settings.ScreenScraperPassword;
        }

        private async Task CheckAsync()
        {
            string user = _txtUser.Text.Trim();
            string pass = _txtPassword.Text;

            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
            {
                _lblStatus.ForeColor = Color.Firebrick;
                _lblStatus.Text = "Заполните логин и пароль.";
                return;
            }

            _btnCheck.Enabled = false;
            _lblStatus.ForeColor = Color.DimGray;
            _lblStatus.Text = "Проверка...";

            try
            {
                var client = new ScreenScraperClient();
                var response = await client.GetUserInfoAsync(user, pass);

                var u = response.Response?.User;
                if (u == null)
                {
                    _lblStatus.ForeColor = Color.Firebrick;
                    _lblStatus.Text = "API не вернул данные пользователя.";
                    return;
                }

                _lblStatus.ForeColor = Color.ForestGreen;
                _lblStatus.Text =
                    $"Успех. Уровень: {u.Level}, " +
                    $"запросов сегодня: {u.RequestsToday} / {u.MaxRequestsPerDay}, " +
                    $"потоков: {u.MaxThreads}";
            }
            catch (Exception ex)
            {
                _lblStatus.ForeColor = Color.Firebrick;
                _lblStatus.Text = "Ошибка: " + ex.Message;
            }
            finally
            {
                _btnCheck.Enabled = true;
            }
        }

        
        private void SaveAndClose()
        {
            _settings.ScreenScraperUser = _txtUser.Text.Trim();
            _settings.ScreenScraperPassword = _txtPassword.Text;
            try
            {
                _settings.Save();
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Не удалось сохранить настройки: " + ex.Message,
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}