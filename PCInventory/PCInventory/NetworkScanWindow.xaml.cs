using System.Windows;
using PCInventory.Models;

namespace PCInventory
{
    public partial class NetworkScanWindow : Window
    {
        private AppSettings _settings;

        public string StartIP => txtStartIP.Text;
        public string EndIP => txtEndIP.Text;
        public int Timeout => int.TryParse(txtTimeout.Text, out int t) ? t : _settings?.NetworkTimeout ?? 100;
        public bool ScanPorts => cbScanPorts.IsChecked == true;
        public bool ResolveNames => cbResolveNames.IsChecked == true;

        public NetworkScanWindow(AppSettings settings = null)
        {
            InitializeComponent();
            _settings = settings;

            if (_settings != null)
            {
                txtTimeout.Text = _settings.NetworkTimeout.ToString();
                cbResolveNames.IsChecked = true;
            }

            btnStartScan.Click += BtnStartScan_Click;
            btnCancel.Click += BtnCancel_Click;
        }

        private void BtnStartScan_Click(object sender, RoutedEventArgs e)
        {
            // Валидация IP адресов
            if (!IsValidIP(txtStartIP.Text))
            {
                MessageBox.Show("Введите корректный начальный IP-адрес", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!IsValidIP(txtEndIP.Text))
            {
                MessageBox.Show("Введите корректный конечный IP-адрес", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
            Close();
        }

        private bool IsValidIP(string ip)
        {
            return System.Net.IPAddress.TryParse(ip, out _);
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}