using Microsoft.Win32;
using PCInventory.Models;
using PCInventory.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PCInventory
{
    public partial class ComputerDetailsWindow : Window
    {
        private ComputerInfo _computer;
        private AppSettings _settings;
        private InfoCollector _infoCollector;
        private PassportExporter _passportExporter;

        public ComputerDetailsWindow(ComputerInfo computer, AppSettings settings, InfoCollector infoCollector, PassportExporter passportExporter)
        {
            InitializeComponent();
            _computer = computer;
            _settings = settings;
            _infoCollector = infoCollector;
            _passportExporter = passportExporter;
            LoadComputerData();

            btnUpdateInfo.Click += BtnUpdateInfo_Click;
            btnExportThis.Click += BtnExportThis_Click;
            btnSavePassport.Click += BtnSavePassport_Click;
        }

        private void LoadComputerData()
        {
            txtComputerName.Text = _computer.ComputerName;
            txtIP.Text = _computer.IPAddress;
            txtMAC.Text = _computer.MACAddress ?? "Не определен";
            txtStatus.Text = _computer.Status;
            txtStatus.Foreground = _computer.Status.Contains("Онлайн") || _computer.Status.Contains("собраны") ?
                System.Windows.Media.Brushes.Green : System.Windows.Media.Brushes.Red;

            txtCPUModel.Text = _computer.CPU ?? "Не определено";
            txtRAM.Text = _computer.RAM ?? "Не определено";
            txtMotherboard.Text = _computer.Motherboard ?? "Не определено";
            txtGPU.Text = _computer.GPU ?? "Не определено";
            txtDisks.Text = _computer.DiskInfo ?? "Не определено";
            txtOS.Text = _computer.OS ?? "Не определено";
            if (!string.IsNullOrEmpty(_computer.OSVersion))
                txtOS.Text += $" (Версия: {_computer.OSVersion})";
            if (!string.IsNullOrEmpty(_computer.OSArchitecture))
                txtOS.Text += $" [{_computer.OSArchitecture}]";

            txtManufacturer.Text = _computer.Manufacturer ?? "Не определено";
            txtModel.Text = _computer.Model ?? "Не определено";
            txtSerialNumber.Text = _computer.SerialNumber ?? "Не определено";
            txtLastUpdate.Text = _computer.LastUpdate ?? "Не определено";

            LoadAdditionalInfo();
        }

        private void LoadAdditionalInfo()
        {
            var tabControl = FindName("tabControl") as TabControl;
            if (tabControl == null) return;

            // Очищаем существующие вкладки, кроме первых двух (аппаратура и система)
            while (tabControl.Items.Count > 2)
            {
                tabControl.Items.RemoveAt(2);
            }

            // Вкладка с ПО (если есть данные)
            if (!string.IsNullOrEmpty(_computer?.InstalledPrograms) || !string.IsNullOrEmpty(_computer?.Antivirus))
            {
                var softwareTab = new TabItem { Header = "📦 Программное обеспечение" };
                var stackPanel = new StackPanel { Margin = new Thickness(10) };

                if (!string.IsNullOrEmpty(_computer?.Antivirus))
                {
                    var border = new Border
                    {
                        Background = Brushes.White,
                        BorderBrush = Brushes.LightGray,
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(8),
                        Margin = new Thickness(0, 0, 0, 10),
                        Padding = new Thickness(15),
                        Child = new StackPanel
                        {
                            Children =
                    {
                        new TextBlock { Text = "Антивирус", FontWeight = FontWeights.Bold, FontSize = 14, Margin = new Thickness(0, 0, 0, 10) },
                        new TextBlock { Text = _computer.Antivirus, TextWrapping = TextWrapping.Wrap }
                    }
                        }
                    };
                    stackPanel.Children.Add(border);
                }

                if (!string.IsNullOrEmpty(_computer?.InstalledPrograms))
                {
                    var border = new Border
                    {
                        Background = Brushes.White,
                        BorderBrush = Brushes.LightGray,
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(8),
                        Margin = new Thickness(0, 0, 0, 10),
                        Padding = new Thickness(15),
                        Child = new StackPanel
                        {
                            Children =
                    {
                        new TextBlock { Text = "Установленные программы", FontWeight = FontWeights.Bold, FontSize = 14, Margin = new Thickness(0, 0, 0, 10) },
                        new TextBlock { Text = _computer.InstalledPrograms, TextWrapping = TextWrapping.Wrap }
                    }
                        }
                    };
                    stackPanel.Children.Add(border);
                }

                softwareTab.Content = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = stackPanel };
                tabControl.Items.Add(softwareTab);
            }

            // Вкладка с драйверами
            if (!string.IsNullOrEmpty(_computer?.Drivers))
            {
                var driversTab = new TabItem { Header = "🔧 Драйверы" };
                var stackPanel = new StackPanel { Margin = new Thickness(10) };

                var border = new Border
                {
                    Background = Brushes.White,
                    BorderBrush = Brushes.LightGray,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(15),
                    Child = new StackPanel
                    {
                        Children =
                {
                    new TextBlock { Text = "Драйверы устройств", FontWeight = FontWeights.Bold, FontSize = 14, Margin = new Thickness(0, 0, 0, 10) },
                    new TextBlock { Text = _computer.Drivers, TextWrapping = TextWrapping.Wrap }
                }
                    }
                };
                stackPanel.Children.Add(border);

                driversTab.Content = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = stackPanel };
                tabControl.Items.Add(driversTab);
            }

            // Вкладка с периферией
            if (!string.IsNullOrEmpty(_computer?.USBDevices) || !string.IsNullOrEmpty(_computer?.Printers) || !string.IsNullOrEmpty(_computer?.Monitors))
            {
                var peripheralsTab = new TabItem { Header = "🖨️ Периферия" };
                var stackPanel = new StackPanel { Margin = new Thickness(10) };

                if (!string.IsNullOrEmpty(_computer?.USBDevices))
                {
                    var border = new Border
                    {
                        Background = Brushes.White,
                        BorderBrush = Brushes.LightGray,
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(8),
                        Margin = new Thickness(0, 0, 0, 10),
                        Padding = new Thickness(15),
                        Child = new StackPanel
                        {
                            Children =
                    {
                        new TextBlock { Text = "USB устройства", FontWeight = FontWeights.Bold, FontSize = 14, Margin = new Thickness(0, 0, 0, 10) },
                        new TextBlock { Text = _computer.USBDevices, TextWrapping = TextWrapping.Wrap }
                    }
                        }
                    };
                    stackPanel.Children.Add(border);
                }

                if (!string.IsNullOrEmpty(_computer?.Printers))
                {
                    var border = new Border
                    {
                        Background = Brushes.White,
                        BorderBrush = Brushes.LightGray,
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(8),
                        Margin = new Thickness(0, 0, 0, 10),
                        Padding = new Thickness(15),
                        Child = new StackPanel
                        {
                            Children =
                    {
                        new TextBlock { Text = "Принтеры", FontWeight = FontWeights.Bold, FontSize = 14, Margin = new Thickness(0, 0, 0, 10) },
                        new TextBlock { Text = _computer.Printers, TextWrapping = TextWrapping.Wrap }
                    }
                        }
                    };
                    stackPanel.Children.Add(border);
                }

                if (!string.IsNullOrEmpty(_computer?.Monitors))
                {
                    var border = new Border
                    {
                        Background = Brushes.White,
                        BorderBrush = Brushes.LightGray,
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(8),
                        Padding = new Thickness(15),
                        Child = new StackPanel
                        {
                            Children =
                    {
                        new TextBlock { Text = "Мониторы", FontWeight = FontWeights.Bold, FontSize = 14, Margin = new Thickness(0, 0, 0, 10) },
                        new TextBlock { Text = _computer.Monitors, TextWrapping = TextWrapping.Wrap }
                    }
                        }
                    };
                    stackPanel.Children.Add(border);
                }

                peripheralsTab.Content = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = stackPanel };
                tabControl.Items.Add(peripheralsTab);
            }

            // Вкладка с историей изменений
            if (_computer?.History != null && _computer.History.Count > 0)
            {
                var historyTab = new TabItem { Header = "📜 История" };
                var stackPanel = new StackPanel { Margin = new Thickness(10) };

                for (int i = 0; i < Math.Min(5, _computer.History.Count); i++)
                {
                    var oldPassport = _computer.History[i];
                    if (oldPassport == null) continue;

                    var currentPassport = _computer.Tag as PassportData;

                    var diff = new List<string>();
                    if (oldPassport != null && currentPassport != null)
                    {
                        diff = oldPassport.CompareTo(currentPassport);
                    }

                    var border = new Border
                    {
                        Background = Brushes.White,
                        BorderBrush = Brushes.LightGray,
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(8),
                        Margin = new Thickness(0, 0, 0, 10),
                        Padding = new Thickness(15),
                        Tag = oldPassport
                    };

                    var innerStack = new StackPanel();
                    innerStack.Children.Add(new TextBlock
                    {
                        Text = $"Скан от {oldPassport.CollectedAt:dd.MM.yyyy HH:mm:ss}",
                        FontWeight = FontWeights.Bold,
                        FontSize = 14,
                        Margin = new Thickness(0, 0, 0, 10)
                    });

                    if (diff.Count > 0)
                    {
                        innerStack.Children.Add(new TextBlock
                        {
                            Text = $"Изменений: {diff.Count}",
                            Foreground = Brushes.Orange,
                            Margin = new Thickness(0, 0, 0, 5)
                        });

                        foreach (var change in diff.Take(5))
                        {
                            innerStack.Children.Add(new TextBlock
                            {
                                Text = change,
                                FontSize = 11,
                                TextWrapping = TextWrapping.Wrap,
                                Margin = new Thickness(15, 2, 0, 2)
                            });
                        }

                        if (diff.Count > 5)
                        {
                            innerStack.Children.Add(new TextBlock
                            {
                                Text = $"... и еще {diff.Count - 5} изменений",
                                FontStyle = FontStyles.Italic,
                                Margin = new Thickness(15, 2, 0, 2)
                            });
                        }
                    }
                    else
                    {
                        innerStack.Children.Add(new TextBlock
                        {
                            Text = "✓ Изменений не обнаружено",
                            Foreground = Brushes.Green
                        });
                    }

                    border.Child = innerStack;
                    stackPanel.Children.Add(border);
                }

                if (stackPanel.Children.Count > 0)
                {
                    historyTab.Content = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = stackPanel };
                    tabControl.Items.Add(historyTab);
                }
            }
        }

        private async void BtnUpdateInfo_Click(object sender, RoutedEventArgs e)
        {
            btnUpdateInfo.IsEnabled = false;
            btnUpdateInfo.Content = "Обновление...";

            try
            {
                var passport = await System.Threading.Tasks.Task.Run(() =>
                    _infoCollector.CollectFullInfo(_computer.IPAddress, _settings.Username, _settings.Password, _settings.Domain));

                if (passport != null)
                {
                    // Сохраняем старый паспорт в историю
                    if (_computer.Tag != null)
                    {
                        _computer.History.Insert(0, _computer.Tag as PassportData);
                        if (_computer.History.Count > 10) _computer.History.RemoveAt(10);
                    }

                    _computer.Tag = passport;
                    _computer.LastUpdate = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
                    _computer.Status = "Данные собраны";

                    // Обновляем отображаемые данные
                    LoadComputerData();

                    // Перезагружаем дополнительные вкладки
                    LoadAdditionalInfo();

                    MessageBox.Show("Данные успешно обновлены!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    throw new Exception("Не удалось подключиться к компьютеру");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                _computer.Status = $"Ошибка: {ex.Message}";
            }
            finally
            {
                btnUpdateInfo.IsEnabled = true;
                btnUpdateInfo.Content = "Обновить";
            }
        }

        private async void BtnExportThis_Click(object sender, RoutedEventArgs e)
        {
            var passport = _computer.Tag as PassportData;
            if (passport == null)
            {
                MessageBox.Show("Нет данных для экспорта. Сначала соберите информацию.",
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var invWindow = new InventoryNumberWindow();
            invWindow.Owner = this;

            if (invWindow.ShowDialog() == true)
            {
                try
                {
                    await System.Threading.Tasks.Task.Run(() =>
                    {
                        string outputPath = _passportExporter.ExportPath;
                        string htmlPath = _passportExporter.ExportToHtml(passport, invWindow.InventoryNumber);
                        string pdfPath = _passportExporter.ExportToPdf(passport);
                    });

                    MessageBox.Show($"Сведения компьютера {_computer.ComputerName} успешно созданы!",
                        "Экспорт завершен", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при экспорте: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        private void BtnSavePassport_Click(object sender, RoutedEventArgs e)
        {
            var passport = _computer.Tag as PassportData;
            if (passport == null)
            {
                MessageBox.Show("Нет данных для сохранения.", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var saveDialog = new SaveFileDialog
            {
                Filter = "JSON файлы (*.json)|*.json",
                FileName = $"сведения_{_computer.ComputerName}_{DateTime.Now:yyyyMMdd_HHmmss}.json",
                InitialDirectory = _passportExporter.ExportPath
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    // Сохраняем ОДИН паспорт (не массив!)
                    string json = Newtonsoft.Json.JsonConvert.SerializeObject(passport, Newtonsoft.Json.Formatting.Indented);
                    File.WriteAllText(saveDialog.FileName, json);
                    MessageBox.Show($"Сведения сохранены:\n{saveDialog.FileName}", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        private void BtnSavePassportForCompare_Click(object sender, RoutedEventArgs e)
        {
            var passport = _computer.Tag as PassportData;
            if (passport == null)
            {
                MessageBox.Show("Нет данных для сохранения. Сначала соберите информацию.", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var saveDialog = new SaveFileDialog
            {
                Title = "Сохранить сведения для сравнения",
                Filter = "JSON файлы (*.json)|*.json",
                FileName = $"сведения_{_computer.ComputerName}_{DateTime.Now:yyyyMMdd_HHmmss}.json",
                InitialDirectory = _passportExporter.ExportPath
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    var cleanPassport = new PassportData
                    {
                        UniqueId = passport.UniqueId,
                        CollectedAt = passport.CollectedAt,
                        ComputerName = passport.ComputerName,
                        IPAddress = passport.IPAddress,
                        Hardware = passport.Hardware,
                        Software = passport.Software,
                        Drivers = passport.Drivers,
                        Peripherals = passport.Peripherals
                    };

                    string json = Newtonsoft.Json.JsonConvert.SerializeObject(cleanPassport, Newtonsoft.Json.Formatting.Indented);
                    File.WriteAllText(saveDialog.FileName, json);

                    MessageBox.Show($"Сведения сохранены для сравнения:\n{saveDialog.FileName}", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}