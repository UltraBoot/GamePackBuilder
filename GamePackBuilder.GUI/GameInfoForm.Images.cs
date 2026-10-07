using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace GamePackBuilder.GUI
{
    public partial class GameInfoForm
    {
        // ============================================================
        // ЗАГРУЗКА КАРТИНОК В UI
        // ============================================================
        private void LoadCover()
        {
            if (_picCover.Image != null)
            {
                var old = _picCover.Image;
                _picCover.Image = null;
                old.Dispose();
            }

            if (!string.IsNullOrWhiteSpace(_info.CoverPath) && File.Exists(_info.CoverPath))
            {
                try
                {
                    using var fs = new FileStream(_info.CoverPath, FileMode.Open, FileAccess.Read);
                    _picCover.Image = Image.FromStream(fs);
                }
                catch { }
            }
        }

        private void LoadScreenshots()
        {
            _imageList.Images.Clear();
            _lstScreenshots.Items.Clear();

            foreach (var path in _info.Screenshots)
            {
                if (!File.Exists(path)) continue;
                try
                {
                    using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
                    var img = Image.FromStream(fs);

                    string key = Path.GetFileName(path);
                    int suffix = 1;
                    while (_imageList.Images.ContainsKey(key))
                    {
                        key = Path.GetFileNameWithoutExtension(path) + "_" + suffix + Path.GetExtension(path);
                        suffix++;
                    }
                    _imageList.Images.Add(key, img);

                    var item = new ListViewItem(key) { Tag = path, ImageKey = key };
                    _lstScreenshots.Items.Add(item);
                }
                catch { }
            }
        }

        // ============================================================
        // ОБЛОЖКА
        // ============================================================
        private void BtnAddCover_Click(object? sender, EventArgs e)
        {
            using var dlg = new OpenFileDialog
            {
                Title = "Выбери файл обложки",
                Filter = "Изображения|*.jpg;*.jpeg;*.png;*.bmp;*.gif|Все файлы|*.*"
            };
            if (dlg.ShowDialog() != DialogResult.OK) return;

            _info.CoverPath = dlg.FileName;
            LoadCover();
        }

        private void BtnRemoveCover_Click(object? sender, EventArgs e)
        {
            _info.CoverPath = "";
            LoadCover();
        }

        // ============================================================
        // СКРИНШОТЫ
        // ============================================================
        private void BtnAddScreenshot_Click(object? sender, EventArgs e)
        {
            using var dlg = new OpenFileDialog
            {
                Title = "Выбери скриншоты",
                Filter = "Изображения|*.jpg;*.jpeg;*.png;*.bmp;*.gif|Все файлы|*.*",
                Multiselect = true
            };
            if (dlg.ShowDialog() != DialogResult.OK) return;

            foreach (var f in dlg.FileNames)
            {
                if (!_info.Screenshots.Contains(f))
                    _info.Screenshots.Add(f);
            }

            LoadScreenshots();
        }

        private void BtnRemoveScreenshot_Click(object? sender, EventArgs e)
        {
            if (_lstScreenshots.SelectedItems.Count == 0) return;

            var toRemove = _lstScreenshots.SelectedItems
                .Cast<ListViewItem>()
                .Select(i => i.Tag?.ToString() ?? "")
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();

            foreach (var path in toRemove)
                _info.Screenshots.Remove(path);

            LoadScreenshots();
        }
    }
}