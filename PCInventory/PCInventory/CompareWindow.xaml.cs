using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using Microsoft.Win32;

namespace PCInventory
{
    public partial class CompareWindow : Window
    {
        private string _computerName;
        private string _datesInfo;
        private List<string> _differences;

        public CompareWindow(string computerName, string datesInfo, List<string> differences)
        {
            InitializeComponent();
            _computerName = computerName;
            _datesInfo = datesInfo;
            _differences = differences;

            // Подписываемся на события кнопок
            btnExportDiff.Click += BtnExportDiff_Click;
            btnClose.Click += (s, e) => Close();

            LoadResults();
        }

        private void LoadResults()
        {
            Title = $"Сравнение конфигураций - {_computerName}";
            txtTitle.Text = $"Сравнение конфигураций: {_computerName}";
            txtDatesInfo.Text = _datesInfo;

            if (_differences.Count == 0)
            {
                txtSummary.Text = "✅ Изменений не обнаружено!\n\nКонфигурация компьютера не изменилась.";
                txtSummary.Foreground = System.Windows.Media.Brushes.Green;
                lstDifferences.ItemsSource = new List<string> { "✓ Конфигурации идентичны. Изменений нет." };
            }
            else
            {
                txtSummary.Text = $"🔍 Обнаружено изменений: {_differences.Count}";
                txtSummary.Foreground = System.Windows.Media.Brushes.Orange;
                lstDifferences.ItemsSource = _differences;
            }
        }

        private void BtnExportDiff_Click(object sender, RoutedEventArgs e)
        {
            var saveDialog = new SaveFileDialog
            {
                Title = "Сохранить отчет о сравнении",
                Filter = "Текстовые файлы|*.txt|HTML файлы|*.html",
                FileName = $"Сравнение_{_computerName}_{DateTime.Now:yyyyMMdd_HHmmss}"
            };

            if (saveDialog.ShowDialog() == true)
            {
                string ext = Path.GetExtension(saveDialog.FileName).ToLower();

                if (ext == ".html")
                    ExportToHtml(saveDialog.FileName);
                else
                    ExportToTxt(saveDialog.FileName);

                MessageBox.Show($"Отчет сохранен:\n{saveDialog.FileName}",
                    "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ExportToTxt(string filePath)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"ОТЧЕТ О СРАВНЕНИИ КОНФИГУРАЦИЙ");
            sb.AppendLine($"Компьютер: {_computerName}");
            sb.AppendLine($"Дата сравнения: {DateTime.Now:dd.MM.yyyy HH:mm:ss}");
            sb.AppendLine($"Период: {_datesInfo}");
            sb.AppendLine(new string('=', 60));
            sb.AppendLine();

            if (_differences.Count == 0)
            {
                sb.AppendLine("РЕЗУЛЬТАТ: Конфигурации идентичны. Изменений не обнаружено.");
            }
            else
            {
                sb.AppendLine($"ОБНАРУЖЕНО ИЗМЕНЕНИЙ: {_differences.Count}");
                sb.AppendLine();
                for (int i = 0; i < _differences.Count; i++)
                {
                    sb.AppendLine($"{i + 1}. {_differences[i]}");
                    sb.AppendLine();
                }
            }

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        }

        private void ExportToHtml(string filePath)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html><head><meta charset='utf-8'>");
            sb.AppendLine("<title>Отчет о сравнении конфигураций</title>");
            sb.AppendLine(@"<style>
                body { font-family: 'Segoe UI', Arial, sans-serif; margin: 20px; background: #f0f0f0; }
                .container { max-width: 1000px; margin: 0 auto; background: white; border-radius: 8px; padding: 20px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }
                h1 { color: #7B1FA2; border-bottom: 2px solid #7B1FA2; padding-bottom: 10px; }
                .info { background: #f5f5f5; padding: 10px; border-radius: 6px; margin-bottom: 20px; }
                .diff-item { background: #FFF3E0; padding: 10px; margin: 8px 0; border-left: 4px solid #FF9800; border-radius: 4px; white-space: pre-wrap; font-family: monospace; }
                .no-diff { background: #E8F5E9; padding: 20px; text-align: center; color: #4CAF50; font-weight: bold; border-radius: 8px; }
                .footer { margin-top: 20px; text-align: center; color: #666; font-size: 12px; }
            </style>");
            sb.AppendLine("</head><body>");
            sb.AppendLine("<div class='container'>");
            sb.AppendLine($"<h1>Отчет о сравнении конфигураций</h1>");
            sb.AppendLine($"<div class='info'>");
            sb.AppendLine($"<p><strong>Компьютер:</strong> {_computerName}</p>");
            sb.AppendLine($"<p><strong>Дата сравнения:</strong> {DateTime.Now:dd.MM.yyyy HH:mm:ss}</p>");
            sb.AppendLine($"<p><strong>Период:</strong> {_datesInfo}</p>");
            sb.AppendLine($"</div>");

            if (_differences.Count == 0)
            {
                sb.AppendLine("<div class='no-diff'>✅ Конфигурации идентичны. Изменений не обнаружено.</div>");
            }
            else
            {
                sb.AppendLine($"<h3>Найдено различий: {_differences.Count}</h3>");
                foreach (var diff in _differences)
                {
                    sb.AppendLine($"<div class='diff-item'>{EscapeHtml(diff)}</div>");
                }
            }

            sb.AppendLine($"<div class='footer'>Сформировано программой PCInventory</div>");
            sb.AppendLine("</div></body></html>");

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        }

        private string EscapeHtml(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return text.Replace("&", "&amp;")
                       .Replace("<", "&lt;")
                       .Replace(">", "&gt;");
        }
    }
}