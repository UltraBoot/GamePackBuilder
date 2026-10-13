using System.Drawing;
using System.Windows.Forms;

namespace GamePackBuilder.GUI
{
    /// <summary>
    /// Диалог выбора, какие медиа качать с ScreenScraper.
    /// Профиль (Portable / Batocera / Full) устанавливает галочки,
    /// но их можно менять вручную.
    /// </summary>
    public class MediaDownloadOptionsForm : Form
    {
        private ComboBox _cmbProfile = null!;
        private CheckBox _chkBoxart = null!;
        private CheckBox _chkScreenshots = null!;
        private CheckBox _chkWheel = null!;
        private CheckBox _chkFanart = null!;
        private CheckBox _chkVideo = null!;
        private CheckBox _chkManual = null!;
        private Button _btnOk = null!;
        private Button _btnCancel = null!;

        public MediaDownloadOptions Options { get; private set; } = MediaDownloadOptions.ForPortable();

        public MediaDownloadOptionsForm(MediaDownloadOptions? initial = null)
        {
            BuildUI();
            ThemeManager.Apply(this);

            if (initial != null)
                ApplyOptions(initial);
            else
                ApplyProfile(0);
        }

        private void BuildUI()
        {
            Text = "Что качать с ScreenScraper";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(440, 400);

            var lblProfile = new Label
            {
                Text = "Профиль:",
                Location = new Point(16, 20),
                AutoSize = true
            };

            _cmbProfile = new ComboBox
            {
                Location = new Point(120, 17),
                Width = 300,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cmbProfile.Items.Add("Портативная консоль (ArkOS / RG52)");
            _cmbProfile.Items.Add("Batocera / мини-ПК");
            _cmbProfile.Items.Add("Полный набор (с мануалами)");
            _cmbProfile.Items.Add("Свой набор");
            _cmbProfile.SelectedIndex = 0;
            _cmbProfile.SelectedIndexChanged += (_, _) =>
            {
                if (_cmbProfile.SelectedIndex < 3)
                    ApplyProfile(_cmbProfile.SelectedIndex);
            };

            var lblHint = new Label
            {
                Text = "Галочки можно менять вручную.",
                Location = new Point(16, 50),
                AutoSize = true,
                ForeColor = Color.Gray
            };

            int y = 82;

            _chkBoxart = MakeCheck("Обложки (boxart)", y); y += 32;
            _chkScreenshots = MakeCheck("Скриншоты (art, до 5 на игру)", y); y += 32;
            _chkWheel = MakeCheck("Логотипы (wheel)", y); y += 32;
            _chkFanart = MakeCheck("Фоны (fanart)", y); y += 32;
            _chkVideo = MakeCheck("Видео (mp4, тяжёлые)", y); y += 32;
            _chkManual = MakeCheck("Мануалы (PDF, тяжёлые)", y);

            var hintHeavy = new Label
            {
                Text = "Видео и мануалы занимают много места. Для RG52 Mini\nих обычно не качают.",
                Location = new Point(40, 290),
                AutoSize = true,
                ForeColor = Color.Gray
            };

            _btnOk = new Button
            {
                Text = "OK",
                Location = new Point(230, 355),
                Width = 90,
                DialogResult = DialogResult.None
            };
            _btnOk.Click += (_, _) =>
            {
                Options = ReadOptions();
                if (Options.IsNone)
                {
                    MessageBox.Show(this,
                        "Не выбрано ни одного типа медиа.",
                        "Пусто", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                DialogResult = DialogResult.OK;
                Close();
            };

            _btnCancel = new Button
            {
                Text = "Отмена",
                Location = new Point(330, 355),
                Width = 90,
                DialogResult = DialogResult.Cancel
            };

            Controls.Add(lblProfile);
            Controls.Add(_cmbProfile);
            Controls.Add(lblHint);
            Controls.Add(_chkBoxart);
            Controls.Add(_chkScreenshots);
            Controls.Add(_chkWheel);
            Controls.Add(_chkFanart);
            Controls.Add(_chkVideo);
            Controls.Add(_chkManual);
            Controls.Add(hintHeavy);
            Controls.Add(_btnOk);
            Controls.Add(_btnCancel);

            AcceptButton = _btnOk;
            CancelButton = _btnCancel;
        }

        private CheckBox MakeCheck(string text, int y)
        {
            return new CheckBox
            {
                Text = text,
                Location = new Point(40, y),
                AutoSize = true
            };
        }

        private void ApplyProfile(int index)
        {
            MediaDownloadOptions opts = index switch
            {
                0 => MediaDownloadOptions.ForPortable(),
                1 => MediaDownloadOptions.ForBatocera(),
                2 => MediaDownloadOptions.ForFull(),
                _ => ReadOptions()
            };
            ApplyOptions(opts);
        }

        private void ApplyOptions(MediaDownloadOptions o)
        {
            _chkBoxart.Checked = o.Boxart;
            _chkScreenshots.Checked = o.Screenshots;
            _chkWheel.Checked = o.Wheel;
            _chkFanart.Checked = o.Fanart;
            _chkVideo.Checked = o.Video;
            _chkManual.Checked = o.Manual;
        }

        private void InitializeComponent()
        {

        }

        private MediaDownloadOptions ReadOptions() => new()
        {
            Boxart = _chkBoxart.Checked,
            Screenshots = _chkScreenshots.Checked,
            Wheel = _chkWheel.Checked,
            Fanart = _chkFanart.Checked,
            Video = _chkVideo.Checked,
            Manual = _chkManual.Checked
        };
    }
}