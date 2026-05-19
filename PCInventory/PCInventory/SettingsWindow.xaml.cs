using System;
using System.Windows;
using Microsoft.Win32;
using PCInventory.Models;

namespace PCInventory
{
    public partial class SettingsWindow : Window
    {
        public AppSettings Settings { get; private set; }

        public SettingsWindow(AppSettings settings)
        {
            InitializeComponent();
            Settings = settings ?? new AppSettings();
            LoadSettings();

            btnSave.Click += BtnSave_Click;
            btnCancel.Click += (s, e) => Close();
            btnBrowseFolder.Click += BtnBrowseFolder_Click;
        }

        private void LoadSettings()
        {
            txtNetworkTimeout.Text = Settings.NetworkTimeout.ToString();
            chkResolveNames.IsChecked = Settings.ResolveNames;
            chkUseWMI.IsChecked = Settings.UseWMI;

            txtExportPath.Text = string.IsNullOrEmpty(Settings.ExportPath)
                ? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                : Settings.ExportPath;

            chkExportHTML.IsChecked = Settings.ExportHTML;
            chkExportPDF.IsChecked = Settings.ExportPDF;
            chkExportJSON.IsChecked = Settings.ExportJSON;
            chkExportExcel.IsChecked = Settings.ExportExcel;

            chkAutoRefresh.IsChecked = Settings.AutoRefresh;
            txtRefreshInterval.Text = Settings.UpdateInterval.ToString();
        }

        private void BtnBrowseFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Title = "Выберите папку для сохранения",
                FileName = "Выберите папку",
                ValidateNames = false,
                CheckFileExists = false,
                CheckPathExists = true
            };

            if (dialog.ShowDialog() == true)
            {
                string folder = System.IO.Path.GetDirectoryName(dialog.FileName);
                if (!string.IsNullOrEmpty(folder))
                {
                    txtExportPath.Text = folder;
                }
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            Settings.NetworkTimeout = int.TryParse(txtNetworkTimeout.Text, out int timeout) ? timeout : 100;
            Settings.ResolveNames = chkResolveNames.IsChecked == true;
            Settings.UseWMI = chkUseWMI.IsChecked == true;

            Settings.ExportPath = txtExportPath.Text;
            Settings.ExportHTML = chkExportHTML.IsChecked == true;
            Settings.ExportPDF = chkExportPDF.IsChecked == true;
            Settings.ExportJSON = chkExportJSON.IsChecked == true;
            Settings.ExportExcel = chkExportExcel.IsChecked == true;

            Settings.AutoRefresh = chkAutoRefresh.IsChecked == true;
            Settings.UpdateInterval = int.TryParse(txtRefreshInterval.Text, out int interval) ? interval : 60;

            DialogResult = true;
            Close();
        }
    }
}