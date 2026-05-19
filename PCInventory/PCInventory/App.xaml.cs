using System.Windows;

namespace PCInventory
{
    public partial class App : Application
    {
        public App()
        {

            this.DispatcherUnhandledException += (sender, args) =>
            {
                MessageBox.Show($"Произошла ошибка: {args.Exception.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                args.Handled = true;
            };
        }
    }
}