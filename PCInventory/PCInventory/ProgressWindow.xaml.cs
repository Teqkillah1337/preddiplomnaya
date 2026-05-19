using System.Windows;

namespace PCInventory
{
    public partial class ProgressWindow : Window
    {
        private int _total = 100;

        public ProgressWindow()
        {
            InitializeComponent();
        }

        public void SetTotal(int total)
        {
            _total = total;
            pbProgress.Maximum = total;
        }

        public void SetProgress(int current)
        {
            Dispatcher.Invoke(() =>
            {
                pbProgress.Value = current;
                txtPercent.Text = $"{current}/{_total} ({100.0 * current / _total:F1}%)";
            });
        }

        public void SetStatus(string status)
        {
            Dispatcher.Invoke(() => txtStatus.Text = status);
        }
    }
}