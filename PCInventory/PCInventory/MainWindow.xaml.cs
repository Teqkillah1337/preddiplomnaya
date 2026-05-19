using Microsoft.Win32;
using PCInventory.Models;
using PCInventory.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace PCInventory
{
    public partial class MainWindow : Window
    {
        private ObservableCollection<ComputerInfo> _computers;
        private NetworkScanner _scanner;
        private InfoCollector _infoCollector;
        private PassportExporter _passportExporter;
        private DataExporter _dataExporter;
        private CancellationTokenSource _scanCancellation;
        private AppSettings _settings;
        private string _settingsPath;
        private System.Windows.Threading.DispatcherTimer _autoRefreshTimer;

        public MainWindow()
        {
            InitializeComponent();
            InitializeServices();
            AttachEventHandlers();
            LoadSettings();
            LoadSavedData();
            SetupAutoRefresh();
            SetupSearch();
        }

        private void InitializeServices()
        {
            _computers = new ObservableCollection<ComputerInfo>();
            dgComputers.ItemsSource = _computers;

            _scanner = new NetworkScanner(100, true);
            _infoCollector = new InfoCollector();
            _passportExporter = new PassportExporter();
            _dataExporter = new DataExporter();

            _scanner.ComputerFound += OnComputerFound;
            _scanner.ProgressChanged += OnScanProgress;
            _scanner.StatusChanged += OnScanStatusChanged;
        }

        private void AttachEventHandlers()
        {
            btnScanNetwork.Click += BtnScanNetwork_Click;
            btnStopScan.Click += BtnStopScan_Click;
            btnCollectAll.Click += BtnCollectAll_Click;
            btnUpdateSelected.Click += BtnUpdateSelected_Click;
            btnExportAll.Click += BtnExportAll_Click;
            btnExportSelected.Click += BtnExportSelected_Click;
            btnSettings.Click += BtnSettings_Click;
            btnAbout.Click += BtnAbout_Click;
            btnCompare.Click += BtnCompare_Click;
            btnGeneratePassports.Click += BtnGeneratePassports_Click;
            btnRefresh.Click += BtnRefresh_Click;
            btnWakeOnLAN.Click += BtnWakeOnLAN_Click;
            btnClearList.Click += BtnClearList_Click;

            dgComputers.MouseDoubleClick += DgComputers_MouseDoubleClick;
            Closing += MainWindow_Closing;
        }

        private void SetupSearch()
        {
            txtSearch.TextChanged += (s, e) => ApplyFilter();
        }

        private void ApplyFilter()
        {
            if (txtSearch == null || _computers == null) return;

            string searchText = txtSearch.Text.ToLower();
            if (string.IsNullOrWhiteSpace(searchText))
            {
                dgComputers.ItemsSource = _computers;
            }
            else
            {
                var filtered = _computers.Where(c =>
                    c.ComputerName.ToLower().Contains(searchText) ||
                    c.IPAddress.ToLower().Contains(searchText)).ToList();
                dgComputers.ItemsSource = filtered;
            }
        }

        private void SetupAutoRefresh()
        {
            _autoRefreshTimer = new System.Windows.Threading.DispatcherTimer();
            _autoRefreshTimer.Tick += async (s, e) =>
            {
                if (_settings.AutoRefresh && _computers.Any())
                {
                    await CollectDataFromComputers(_computers.ToList());
                }
            };
            _autoRefreshTimer.Interval = TimeSpan.FromMinutes(_settings.UpdateInterval);
            if (_settings.AutoRefresh) _autoRefreshTimer.Start();
        }

        private void LoadSettings()
        {
            try
            {
                string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PCInventory");
                if (!Directory.Exists(appDataPath))
                    Directory.CreateDirectory(appDataPath);

                _settingsPath = Path.Combine(appDataPath, "settings.json");
                _settings = AppSettings.Load(_settingsPath);

                if (_scanner != null) _scanner.Timeout = _settings.NetworkTimeout;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка загрузки настроек: {ex.Message}");
                _settings = new AppSettings();
            }
        }

        private void SaveSettings()
        {
            try
            {
                _settings?.Save(_settingsPath);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка сохранения настроек: {ex.Message}");
            }
        }

        private void LoadSavedData()
        {
            try
            {
                string dataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "PCInventory", "computers.json");

                if (File.Exists(dataPath))
                {
                    string json = File.ReadAllText(dataPath);
                    var savedComputers = Newtonsoft.Json.JsonConvert.DeserializeObject<ObservableCollection<ComputerInfo>>(json);
                    if (savedComputers != null)
                    {
                        foreach (var comp in savedComputers)
                        {
                            _computers.Add(comp);
                        }
                        UpdateStatistics();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка загрузки данных: {ex.Message}");
            }
        }

        private void SaveCurrentData()
        {
            try
            {
                string dataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "PCInventory", "computers.json");

                string directory = Path.GetDirectoryName(dataPath);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                string json = Newtonsoft.Json.JsonConvert.SerializeObject(_computers, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(dataPath, json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка сохранения данных: {ex.Message}");
            }
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveCurrentData();
            SaveSettings();
            _autoRefreshTimer?.Stop();
        }

        private void OnComputerFound(ScanResult result)
        {
            Dispatcher.Invoke(() =>
            {
                if (!_computers.Any(c => c.IPAddress == result.IPAddress))
                {
                    _computers.Add(new ComputerInfo
                    {
                        ComputerName = result.ComputerName,
                        IPAddress = result.IPAddress,
                        Status = "Онлайн",
                        MACAddress = result.MACAddress,
                        LastUpdate = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"),
                        IsSelected = false
                    });
                    UpdateStatistics();
                    ApplyFilter();
                }
            });
        }

        private void OnScanProgress(int current, int total)
        {
            Dispatcher.Invoke(() =>
            {
                double percent = (double)current / total * 100;
                txtProgress.Text = $"Сканирование: {current}/{total} ({percent:F1}%)";
            });
        }

        private void OnScanStatusChanged(string status)
        {
            Dispatcher.Invoke(() => txtProgress.Text = status);
        }

        private void UpdateStatistics()
        {
            if (txtTotalCount == null) return;

            int total = _computers.Count;
            int online = _computers.Count(c => c.Status.Contains("Онлайн") || c.Status.Contains("Данные собраны"));
            int offline = _computers.Count(c => c.Status == "Оффлайн");

            txtTotalCount.Text = total.ToString();
            txtOnlineCount.Text = online.ToString();
            txtOfflineCount.Text = offline.ToString();
        }

        private async void BtnScanNetwork_Click(object sender, RoutedEventArgs e)
        {
            var scanWindow = new NetworkScanWindow(_settings);
            scanWindow.Owner = this;

            if (scanWindow.ShowDialog() == true)
            {
                btnScanNetwork.IsEnabled = false;
                btnStopScan.IsEnabled = true;

                try
                {
                    _scanCancellation = new CancellationTokenSource();
                    await _scanner.ScanNetworkAsync(scanWindow.StartIP, scanWindow.EndIP, scanWindow.Timeout, _scanCancellation.Token);
                    MessageBox.Show($"Сканирование завершено!\nНайдено {_computers.Count} компьютеров.",
                        "Результат", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (OperationCanceledException)
                {
                    txtProgress.Text = "Сканирование отменено";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при сканировании: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    btnScanNetwork.IsEnabled = true;
                    btnStopScan.IsEnabled = false;
                    txtProgress.Text = "Готов";
                }
            }
        }

        private void BtnStopScan_Click(object sender, RoutedEventArgs e)
        {
            _scanner.StopScan();
            _scanCancellation?.Cancel();
            txtProgress.Text = "Сканирование остановлено";
        }

        private async void BtnCollectAll_Click(object sender, RoutedEventArgs e)
        {
            var allComputers = _computers.ToList();

            if (!allComputers.Any())
            {
                MessageBox.Show("Нет компьютеров для сбора данных. Сначала выполните сканирование сети.",
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            await CollectDataFromComputers(allComputers);
        }

        private async void BtnUpdateSelected_Click(object sender, RoutedEventArgs e)
        {
            var selectedComputers = _computers.Where(c => c.IsSelected).ToList();

            if (!selectedComputers.Any())
            {
                MessageBox.Show("Выберите компьютеры для сбора данных (поставьте галочки в колонке 'Выбор').",
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            await CollectDataFromComputers(selectedComputers);
        }

        private async Task CollectDataFromComputers(System.Collections.Generic.List<ComputerInfo> computers)
        {
            var progressWindow = new ProgressWindow();
            progressWindow.Owner = this;
            progressWindow.SetTotal(computers.Count);
            progressWindow.Show();

            int successCount = 0;
            int errorCount = 0;
            int current = 0;

            foreach (var computer in computers)
            {
                current++;
                progressWindow.SetProgress(current);
                progressWindow.SetStatus($"Сбор данных с {computer.ComputerName} ({computer.IPAddress})...");

                try
                {
                    PassportData oldPassport = computer.Tag as PassportData;

                    var passport = await Task.Run(() => _infoCollector.CollectFullInfo(computer.IPAddress, _settings.Username, _settings.Password, _settings.Domain));

                    if (passport == null)
                    {
                        throw new Exception("Не удалось подключиться к компьютеру");
                    }

                    passport.ComputerName = computer.ComputerName;
                    passport.IPAddress = computer.IPAddress;

                    computer.Tag = passport;

                    if (oldPassport != null)
                    {
                        computer.History.Insert(0, oldPassport);
                        if (computer.History.Count > 10) computer.History.RemoveAt(10);
                    }

                    if (passport.Hardware.ContainsKey("Процессор"))
                        computer.CPU = passport.Hardware["Процессор"];
                    if (passport.Hardware.ContainsKey("Общий объем памяти"))
                        computer.RAM = passport.Hardware["Общий объем памяти"];
                    if (passport.Hardware.ContainsKey("Материнская плата"))
                        computer.Motherboard = passport.Hardware["Материнская плата"];
                    if (passport.Hardware.ContainsKey("Производитель ПК"))
                        computer.Manufacturer = passport.Hardware["Производитель ПК"];
                    if (passport.Hardware.ContainsKey("Модель ПК"))
                        computer.Model = passport.Hardware["Модель ПК"];
                    if (passport.Hardware.ContainsKey("Серийный номер"))
                        computer.SerialNumber = passport.Hardware["Серийный номер"];

                    if (passport.Software.ContainsKey("Операционная система"))
                        computer.OS = passport.Software["Операционная система"];

                    var disks = new StringBuilder();
                    foreach (var kv in passport.Hardware)
                    {
                        if (kv.Key.StartsWith("Диск") || kv.Key.StartsWith("Логический диск"))
                            disks.AppendLine(kv.Value);
                    }
                    computer.DiskInfo = disks.Length > 0 ? disks.ToString().Trim() : "Не определено";

                    var gpu = new StringBuilder();
                    foreach (var kv in passport.Hardware)
                    {
                        if (kv.Key.StartsWith("Видеокарта"))
                            gpu.AppendLine(kv.Value);
                    }
                    computer.GPU = gpu.Length > 0 ? gpu.ToString().Trim() : "Не определено";

                    computer.UniqueId = passport.UniqueId;
                    computer.Status = "Данные собраны";
                    computer.LastUpdate = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");

                    successCount++;
                }
                catch (Exception ex)
                {
                    errorCount++;
                    computer.Status = $"Ошибка: {ex.Message}";
                }

                await Task.Delay(1);
            }

            progressWindow.Close();

            await Dispatcher.InvokeAsync(() =>
            {
                dgComputers.Items.Refresh();
                UpdateStatistics();
                SaveCurrentData();
            });

            MessageBox.Show($"Сбор данных завершен!\n\nУспешно: {successCount}\nОшибок: {errorCount}",
                "Результат", MessageBoxButton.OK, MessageBoxImage.Information);

            txtProgress.Text = "Готов";
        }

        private void BtnGeneratePassports_Click(object sender, RoutedEventArgs e)
        {
            var selectedComputers = _computers.Where(c => c.IsSelected && c.Tag != null).ToList();

            if (!selectedComputers.Any())
            {
                MessageBox.Show("Выберите компьютеры для формирования сведений.\n\n" +
                    "Убедитесь, что:\n" +
                    "1. Вы поставили галочки в колонке 'Выбор'\n" +
                    "2. Данные с этих компьютеров уже собраны",
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var htmlGenerator = new HtmlPassportGenerator();
            int createdCount = 0;

            foreach (var computer in selectedComputers)
            {
                var invWindow = new InventoryNumberWindow();
                invWindow.Owner = this;

                if (invWindow.ShowDialog() == true)
                {
                    try
                    {
                        var passport = computer.Tag as PassportData;
                        if (passport != null)
                        {
                            string outputPath = _passportExporter.ExportPath;
                            string htmlPath = htmlGenerator.SaveToFile(computer, passport, invWindow.InventoryNumber, outputPath);
                            createdCount++;
                            computer.Status = "Сведения сформированы";
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }

            if (createdCount > 0)
            {
                var msgResult = MessageBox.Show($"✅ Создано паспортов: {createdCount}\n\nПапка: {_passportExporter.ExportPath}\n\nОткрыть папку?",
                    "Сведения созданы", MessageBoxButton.YesNo, MessageBoxImage.Information);

                if (msgResult == MessageBoxResult.Yes)
                {
                    System.Diagnostics.Process.Start(_passportExporter.ExportPath);
                }
            }

            txtProgress.Text = "Готов";
            dgComputers.Items.Refresh();
        }

        private async void BtnExportAll_Click(object sender, RoutedEventArgs e)
        {
            var computersWithData = _computers.Where(c => c.Tag != null).ToList();

            if (!computersWithData.Any())
            {
                MessageBox.Show("Нет данных для экспорта.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var formatObj = cmbExportFormat.SelectedItem as ComboBoxItem;
            string format = "HTML";
            if (formatObj != null && formatObj.Tag != null)
                format = formatObj.Tag.ToString();

            if (format == "HTML")
            {
                int createdCount = 0;

                foreach (var computer in computersWithData)
                {
                    var invWindow = new InventoryNumberWindow();
                    invWindow.Owner = this;

                    if (invWindow.ShowDialog() == true)
                    {
                        try
                        {
                            var passport = computer.Tag as PassportData;
                            if (passport != null)
                            {
                                string filePath = _passportExporter.ExportToHtml(passport, invWindow.InventoryNumber);
                                createdCount++;
                                computer.Status = "Сведения сформированы";
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }

                if (createdCount > 0)
                {
                    var result = MessageBox.Show($"Создано файлов: {createdCount}\n\nПапка: {_passportExporter.ExportPath}\n\nОткрыть папку?",
                        "Сведения созданы", MessageBoxButton.YesNo, MessageBoxImage.Information);
                    if (result == MessageBoxResult.Yes)
                        System.Diagnostics.Process.Start(_passportExporter.ExportPath);
                }
            }
            else if (format == "Excel")
            {
                var invWindow = new InventoryNumberWindow();
                invWindow.Owner = this;
                invWindow.Title = "Инвентарный номер";

                if (invWindow.ShowDialog() == true)
                {
                    var saveDialog = new SaveFileDialog
                    {
                        Filter = "Excel|*.xlsx",
                        FileName = string.IsNullOrEmpty(invWindow.InventoryNumber)
                            ? $"Export_{DateTime.Now:yyyyMMdd_HHmmss}"
                            : $"Export_инв{invWindow.InventoryNumber}_{DateTime.Now:yyyyMMdd_HHmmss}"
                    };

                    if (saveDialog.ShowDialog() == true)
                    {
                        var progressWindow = new ProgressWindow();
                        progressWindow.Owner = this;
                        progressWindow.SetTotal(computersWithData.Count);
                        progressWindow.Show();

                        try
                        {
                            string result = await _dataExporter.ExportToExcel(computersWithData, saveDialog.FileName, progressWindow);
                            progressWindow.Close();
                            MessageBox.Show($"✅ Файл: {result}", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        catch (Exception ex)
                        {
                            progressWindow.Close();
                            MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            else if (format == "JSON")
            {
                var invWindow = new InventoryNumberWindow();
                invWindow.Owner = this;
                invWindow.Title = "Инвентарный номер";

                if (invWindow.ShowDialog() == true)
                {
                    string fileName;
                    if (computersWithData.Count == 1)
                    {
                        fileName = string.IsNullOrEmpty(invWindow.InventoryNumber)
                            ? $"сведения_{computersWithData[0].ComputerName}_{DateTime.Now:yyyyMMdd_HHmmss}"
                            : $"сведения_инв{invWindow.InventoryNumber}_{DateTime.Now:yyyyMMdd_HHmmss}";
                    }
                    else
                    {
                        fileName = string.IsNullOrEmpty(invWindow.InventoryNumber)
                            ? $"PCInventory_Export_{DateTime.Now:yyyyMMdd_HHmmss}"
                            : $"PCInventory_Export_инв{invWindow.InventoryNumber}_{DateTime.Now:yyyyMMdd_HHmmss}";
                    }

                    var saveDialog = new SaveFileDialog
                    {
                        Filter = "JSON|*.json",
                        FileName = fileName
                    };

                    if (saveDialog.ShowDialog() == true)
                    {
                        var progressWindow = new ProgressWindow();
                        progressWindow.Owner = this;
                        progressWindow.SetTotal(computersWithData.Count);
                        progressWindow.Show();

                        try
                        {
                            string result = await _dataExporter.ExportToJson(computersWithData, saveDialog.FileName, progressWindow);
                            progressWindow.Close();
                            MessageBox.Show($"Файл: {result}", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        catch (Exception ex)
                        {
                            progressWindow.Close();
                            MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }

            txtProgress.Text = "Готов";
            dgComputers.Items.Refresh();
        }

        private async void BtnExportSelected_Click(object sender, RoutedEventArgs e)
        {
            var selectedComputers = _computers.Where(c => c.IsSelected && c.Tag != null).ToList();

            if (!selectedComputers.Any())
            {
                MessageBox.Show("Выберите компьютеры для экспорта.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var formatObj = cmbExportFormat.SelectedItem as ComboBoxItem;
            string format = "HTML";
            if (formatObj != null && formatObj.Tag != null)
                format = formatObj.Tag.ToString();

            if (format == "HTML")
            {
                int createdCount = 0;

                foreach (var computer in selectedComputers)
                {
                    var invWindow = new InventoryNumberWindow();
                    invWindow.Owner = this;

                    if (invWindow.ShowDialog() == true)
                    {
                        try
                        {
                            var passport = computer.Tag as PassportData;
                            if (passport != null)
                            {
                                string filePath = _passportExporter.ExportToHtml(passport, invWindow.InventoryNumber);
                                createdCount++;
                                computer.Status = "Сведения сформированы";
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }

                if (createdCount > 0)
                {
                    var result = MessageBox.Show($"Создано файлов: {createdCount}\n\nПапка: {_passportExporter.ExportPath}\n\nОткрыть папку?",
                        "Сведения созданы", MessageBoxButton.YesNo, MessageBoxImage.Information);
                    if (result == MessageBoxResult.Yes)
                        System.Diagnostics.Process.Start(_passportExporter.ExportPath);
                }
            }
            else if (format == "Excel")
            {
                var invWindow = new InventoryNumberWindow();
                invWindow.Owner = this;
                invWindow.Title = "Инвентарный номер";

                if (invWindow.ShowDialog() == true)
                {
                    var saveDialog = new SaveFileDialog
                    {
                        Filter = "Excel|*.xlsx",
                        FileName = string.IsNullOrEmpty(invWindow.InventoryNumber)
                            ? $"PCInventory_Export_{DateTime.Now:yyyyMMdd_HHmmss}"
                            : $"PCInventory_Export_инв{invWindow.InventoryNumber}_{DateTime.Now:yyyyMMdd_HHmmss}"
                    };

                    if (saveDialog.ShowDialog() == true)
                    {
                        var progressWindow = new ProgressWindow();
                        progressWindow.Owner = this;
                        progressWindow.SetTotal(selectedComputers.Count);
                        progressWindow.Show();

                        try
                        {
                            string result = await _dataExporter.ExportToExcel(selectedComputers, saveDialog.FileName, progressWindow);
                            progressWindow.Close();
                            MessageBox.Show($"Файл: {result}", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        catch (Exception ex)
                        {
                            progressWindow.Close();
                            MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            else if (format == "JSON")
            {
                var invWindow = new InventoryNumberWindow();
                invWindow.Owner = this;
                invWindow.Title = "Инвентарный номер";

                if (invWindow.ShowDialog() == true)
                {
                    string fileName;
                    if (selectedComputers.Count == 1)
                    {
                        fileName = string.IsNullOrEmpty(invWindow.InventoryNumber)
                            ? $"сведения_{selectedComputers[0].ComputerName}_{DateTime.Now:yyyyMMdd_HHmmss}"
                            : $"сведения_инв{invWindow.InventoryNumber}_{DateTime.Now:yyyyMMdd_HHmmss}";
                    }
                    else
                    {
                        fileName = string.IsNullOrEmpty(invWindow.InventoryNumber)
                            ? $"PCInventory_Export_{DateTime.Now:yyyyMMdd_HHmmss}"
                            : $"PCInventory_Export_инв{invWindow.InventoryNumber}_{DateTime.Now:yyyyMMdd_HHmmss}";
                    }

                    var saveDialog = new SaveFileDialog
                    {
                        Filter = "JSON|*.json",
                        FileName = fileName
                    };

                    if (saveDialog.ShowDialog() == true)
                    {
                        var progressWindow = new ProgressWindow();
                        progressWindow.Owner = this;
                        progressWindow.SetTotal(selectedComputers.Count);
                        progressWindow.Show();

                        try
                        {
                            string result = await _dataExporter.ExportToJson(selectedComputers, saveDialog.FileName, progressWindow);
                            progressWindow.Close();
                            MessageBox.Show($"Файл: {result}", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        catch (Exception ex)
                        {
                            progressWindow.Close();
                            MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }

            txtProgress.Text = "Готов";
            dgComputers.Items.Refresh();
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            dgComputers.Items.Refresh();
            UpdateStatistics();
            ApplyFilter();
            txtProgress.Text = "Список обновлен";
        }

        private void BtnClearList_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Очистить список компьютеров?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _computers.Clear();
                UpdateStatistics();
                ApplyFilter();
                txtProgress.Text = "Список очищен";
                SaveCurrentData();
            }
        }

        private void BtnWakeOnLAN_Click(object sender, RoutedEventArgs e)
        {
            var selectedComputers = _computers.Where(c => c.IsSelected && !string.IsNullOrEmpty(c.MACAddress)).ToList();

            if (!selectedComputers.Any())
            {
                MessageBox.Show("Выберите компьютеры с MAC-адресами.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int successCount = 0;
            foreach (var computer in selectedComputers)
            {
                if (WakeOnLanService.SendMagicPacket(computer.MACAddress))
                {
                    successCount++;
                    computer.Status = "Wake-on-LAN отправлен";
                }
            }

            MessageBox.Show($"Успешно: {successCount}\nНе удалось: {selectedComputers.Count - successCount}",
                "Wake-on-LAN", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow(_settings);
            settingsWindow.Owner = this;
            if (settingsWindow.ShowDialog() == true)
            {
                _settings = settingsWindow.Settings;
                SaveSettings();
                if (_scanner != null) _scanner.Timeout = _settings.NetworkTimeout;
                _autoRefreshTimer.Interval = TimeSpan.FromMinutes(_settings.UpdateInterval);
                if (_settings.AutoRefresh && !_autoRefreshTimer.IsEnabled)
                    _autoRefreshTimer.Start();
                else if (!_settings.AutoRefresh && _autoRefreshTimer.IsEnabled)
                    _autoRefreshTimer.Stop();
            }
        }

        private void BtnAbout_Click(object sender, RoutedEventArgs e)
        {
            var aboutWindow = new AboutWindow();
            aboutWindow.Owner = this;
            aboutWindow.ShowDialog();
        }

        private async void BtnCompare_Click(object sender, RoutedEventArgs e)
        {
            var selectedComputers = _computers.Where(c => c.IsSelected).ToList();

            if (selectedComputers.Count != 1)
            {
                MessageBox.Show("Выберите ОДИН компьютер для сравнения.", "Сравнение", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var computer = selectedComputers[0];
            var currentPassport = computer.Tag as PassportData;

            if (currentPassport == null)
            {
                var result = MessageBox.Show($"Для {computer.ComputerName} нет данных. Собрать сейчас?", "Нет данных",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    await CollectDataFromComputers(new List<ComputerInfo> { computer });
                    currentPassport = computer.Tag as PassportData;
                }

                if (currentPassport == null)
                {
                    MessageBox.Show("Не удалось собрать данные.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            var openFileDialog = new OpenFileDialog
            {
                Filter = "JSON файлы (*.json)|*.json",
                InitialDirectory = _passportExporter.ExportPath
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    string json = File.ReadAllText(openFileDialog.FileName);
                    PassportData savedPassport = null;

                    if (json.TrimStart().StartsWith("["))
                    {
                        var list = Newtonsoft.Json.JsonConvert.DeserializeObject<List<PassportData>>(json);
                        if (list != null && list.Count > 0)
                        {
                            savedPassport = list.FirstOrDefault(p => p.ComputerName == computer.ComputerName);
                            if (savedPassport == null) savedPassport = list[0];
                        }
                    }
                    else
                    {
                        savedPassport = Newtonsoft.Json.JsonConvert.DeserializeObject<PassportData>(json);
                    }

                    if (savedPassport == null)
                    {
                        MessageBox.Show("Не удалось загрузить паспорт.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    var differences = currentPassport.CompareTo(savedPassport);
                    string datesInfo = $"Текущий: {currentPassport.CollectedAt:dd.MM.yyyy HH:mm}\nСохраненный: {savedPassport.CollectedAt:dd.MM.yyyy HH:mm}";

                    var compareWindow = new CompareWindow(computer.ComputerName, datesInfo, differences);
                    compareWindow.Owner = this;
                    compareWindow.ShowDialog();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void DgComputers_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Получаем элемент, на который кликнули
            var hit = e.OriginalSource as DependencyObject;

            // Ищем родительский элемент DataGridRow
            while (hit != null && !(hit is DataGridRow))
            {
                hit = VisualTreeHelper.GetParent(hit);
            }

            var row = hit as DataGridRow;
            if (row != null)
            {
                var computer = row.Item as ComputerInfo;
                if (computer != null)
                {
                    var detailsWindow = new ComputerDetailsWindow(computer, _settings, _infoCollector, _passportExporter);
                    detailsWindow.Owner = this;
                    detailsWindow.ShowDialog();

                    UpdateStatistics();
                    SaveCurrentData();
                    txtProgress.Text = "Готов";
                }
            }
            else
            {
                // Если не нашли строку, пробуем через SelectedItem
                var computer = dgComputers.SelectedItem as ComputerInfo;
                if (computer != null)
                {
                    var detailsWindow = new ComputerDetailsWindow(computer, _settings, _infoCollector, _passportExporter);
                    detailsWindow.Owner = this;
                    detailsWindow.ShowDialog();

                    UpdateStatistics();
                    SaveCurrentData();
                    txtProgress.Text = "Готов";
                }
                else
                {
                    MessageBox.Show("Не удалось определить компьютер. Попробуйте сначала выделить строку, а потом дважды кликнуть.");
                }
            }
        }
    }
}