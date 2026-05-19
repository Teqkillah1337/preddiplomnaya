using System.Windows;

namespace PCInventory
{
    public partial class InventoryNumberWindow : Window
    {
        public string InventoryNumber { get; private set; }

        public InventoryNumberWindow()
        {
            InitializeComponent();
            btnOk.Click += BtnOk_Click;
            btnCancel.Click += BtnCancel_Click;
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            InventoryNumber = txtInventoryNumber.Text.Trim();
            System.Diagnostics.Debug.WriteLine($"OK нажат, InventoryNumber = '{InventoryNumber}'");
            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            InventoryNumber = "";
            System.Diagnostics.Debug.WriteLine($"Cancel нажат");
            DialogResult = false;
            Close();
        }
    }
}