using System.Windows.Forms;

namespace GamePackBuilder.GUI
{
    /// <summary>
    /// TextBox, который не берёт фокус, пока находится в режиме ReadOnly.
    /// Нужен для отображения данных «как текст» — без моргающего курсора и выделения.
    /// В режиме редактирования (ReadOnly = false) ведёт себя как обычный TextBox.
    /// </summary>
    public class NoFocusTextBox : TextBox
    {
        private const int WM_SETFOCUS = 0x0007;

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_SETFOCUS && ReadOnly)
            {
                // Передаём фокус родителю, чтобы курсор не мигал и текст не выделялся.
                Parent?.Focus();
                return;
            }
            base.WndProc(ref m);
        }
    }
}