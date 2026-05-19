using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using PCInventory.Models;

namespace PCInventory.Services
{
    public class DataExporter
    {
        public async Task<string> ExportToHtml(List<ComputerInfo> computers, string filePath, ProgressWindow progress = null)
        {
            return await Task.Run(() =>
            {
                var sb = new StringBuilder();

                sb.AppendLine("<!DOCTYPE html>");
                sb.AppendLine("<html><head><meta charset='utf-8'>");
                sb.AppendLine("<title>Инвентаризация компьютерной техники</title>");
                sb.AppendLine(@"<style>
                    body { font-family: 'Segoe UI', Arial, sans-serif; margin: 20px; background: #f0f0f0; }
                    .container { max-width: 1400px; margin: 0 auto; }
                    h1 { color: #7B1FA2; border-bottom: 2px solid #7B1FA2; padding-bottom: 10px; }
                    .summary { background: white; padding: 15px; border-radius: 8px; margin-bottom: 20px; box-shadow: 0 2px 5px rgba(0,0,0,0.1); }
                    table { width: 100%; border-collapse: collapse; background: white; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 5px rgba(0,0,0,0.1); }
                    th { background: #7B1FA2; color: white; padding: 12px; text-align: left; }
                    td { padding: 10px; border-bottom: 1px solid #e0e0e0; }
                    tr:hover { background: #f5f5f5; }
                    .online { color: green; font-weight: bold; }
                    .offline { color: red; }
                    .footer { margin-top: 20px; text-align: center; color: #666; font-size: 12px; }
                </style>");
                sb.AppendLine("</head><body>");
                sb.AppendLine("<div class='container'>");
                sb.AppendLine($"<h1>Инвентаризация компьютерной техники</h1>");
                sb.AppendLine($"<div class='summary'>");
                sb.AppendLine($"<p><strong>Дата формирования:</strong> {DateTime.Now:dd.MM.yyyy HH:mm:ss}</p>");
                sb.AppendLine($"<p><strong>Всего компьютеров:</strong> {computers.Count}</p>");
                sb.AppendLine($"<p><strong>С данными:</strong> {computers.Count(c => c.Tag != null)}</p>");
                sb.AppendLine($"</div>");

                sb.AppendLine(@"<table>
                    <thead>
                        <tr><th>Имя ПК</th><th>IP адрес</th><th>Статус</th><th>ОС</th><th>Процессор</th><th>ОЗУ</th><th>Последнее обновление</th></tr>
                    </thead>
                    <tbody>");

                int processed = 0;
                foreach (var computer in computers)
                {
                    processed++;
                    progress?.SetProgress(processed);

                    string statusClass = computer.Status.Contains("Онлайн") || computer.Status.Contains("собраны") ? "online" : "offline";
                    sb.AppendLine($@"<td>
                        <td>{EscapeHtml(computer.ComputerName)}</td>
                        <td>{EscapeHtml(computer.IPAddress)}</td>
                        <td class='{statusClass}'>{EscapeHtml(computer.Status)}</td>
                        <td>{EscapeHtml(computer.OS ?? "—")}</td>
                        <td>{EscapeHtml(computer.CPU ?? "—")}</td>
                        <td>{EscapeHtml(computer.RAM ?? "—")}</td>
                        <td>{EscapeHtml(computer.LastUpdate ?? "—")}</td>
                    </tr>");
                }

                sb.AppendLine("</tbody>");
                sb.AppendLine("</table>");
                sb.AppendLine($"<div class='footer'>Сформировано программой PCInventory</div>");
                sb.AppendLine("</div></body></html>");

                File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
                return filePath;
            });
        }

        public async Task<string> ExportToJson(List<ComputerInfo> computers, string filePath, ProgressWindow progress = null)
        {
            return await Task.Run(() =>
            {
                // Если выбран только один компьютер - сохраняем как одиночный паспорт
                if (computers.Count == 1)
                {
                    var passport = computers[0].Tag as PassportData;
                    if (passport != null)
                    {
                        string json = Newtonsoft.Json.JsonConvert.SerializeObject(passport, Newtonsoft.Json.Formatting.Indented);
                        File.WriteAllText(filePath, json, Encoding.UTF8);
                        return filePath;
                    }
                }

                // Если несколько компьютеров - сохраняем как массив
                var passports = new List<PassportData>();
                int processed = 0;

                foreach (var computer in computers)
                {
                    processed++;
                    progress?.SetProgress(processed);

                    var passport = computer.Tag as PassportData;
                    if (passport != null)
                    {
                        var cleanPassport = new PassportData
                        {
                            UniqueId = passport.UniqueId,
                            CollectedAt = passport.CollectedAt,
                            ComputerName = computer.ComputerName,
                            IPAddress = computer.IPAddress,
                            Hardware = passport.Hardware,
                            Software = passport.Software,
                            Drivers = passport.Drivers,
                            Peripherals = passport.Peripherals
                        };
                        passports.Add(cleanPassport);
                    }
                }

                string jsonArray = Newtonsoft.Json.JsonConvert.SerializeObject(passports, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(filePath, jsonArray, Encoding.UTF8);

                return filePath;
            });
        }

        public async Task<string> ExportToExcel(List<ComputerInfo> computers, string filePath, ProgressWindow progress = null)
        {
            return await Task.Run(() =>
            {
                using (var package = new ExcelPackage())
                {
                    // ========== ЛИСТ 1: ОСНОВНАЯ ИНФОРМАЦИЯ ==========
                    var mainSheet = package.Workbook.Worksheets.Add("Компьютеры");

                    // Заголовки
                    string[] headers = {
                "Имя ПК", "IP адрес", "Статус", "MAC адрес",
                "Производитель ПК", "Модель ПК", "Серийный номер",
                "Операционная система", "Версия ОС", "Архитектура",
                "Процессор", "Ядра", "Потоки", "Частота",
                "ОЗУ (общая)", "Модули памяти",
                "Материнская плата", "Версия BIOS",
                "Видеокарта", "Диски",
                "Антивирус", "Установленные программы (кол-во)",
                "Дата последнего обновления"
            };

                    // Стиль заголовков
                    for (int i = 0; i < headers.Length; i++)
                    {
                        var cell = mainSheet.Cells[1, i + 1];
                        cell.Value = headers[i];
                        cell.Style.Font.Bold = true;
                        cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        cell.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                        cell.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    int row = 2;
                    int processed = 0;

                    foreach (var computer in computers)
                    {
                        processed++;
                        progress?.SetProgress(processed);

                        var passport = computer.Tag as PassportData;

                        // Основная информация
                        mainSheet.Cells[row, 1].Value = computer.ComputerName;
                        mainSheet.Cells[row, 2].Value = computer.IPAddress;
                        mainSheet.Cells[row, 3].Value = computer.Status;
                        mainSheet.Cells[row, 4].Value = computer.MACAddress;
                        mainSheet.Cells[row, 5].Value = computer.Manufacturer;
                        mainSheet.Cells[row, 6].Value = computer.Model;
                        mainSheet.Cells[row, 7].Value = computer.SerialNumber;
                        mainSheet.Cells[row, 8].Value = computer.OS;
                        mainSheet.Cells[row, 9].Value = computer.OSVersion;
                        mainSheet.Cells[row, 10].Value = computer.OSArchitecture;
                        mainSheet.Cells[row, 11].Value = computer.CPU;
                        mainSheet.Cells[row, 12].Value = computer.CPUCores;
                        mainSheet.Cells[row, 13].Value = computer.CPUThreads;
                        mainSheet.Cells[row, 14].Value = computer.CPUClockSpeed;
                        mainSheet.Cells[row, 15].Value = computer.RAM;
                        mainSheet.Cells[row, 16].Value = computer.RAMModules;
                        mainSheet.Cells[row, 17].Value = computer.Motherboard;
                        mainSheet.Cells[row, 18].Value = computer.BIOS;
                        mainSheet.Cells[row, 19].Value = computer.GPU;
                        mainSheet.Cells[row, 20].Value = computer.DiskInfo;
                        mainSheet.Cells[row, 21].Value = computer.Antivirus;

                        // Количество программ
                        int programCount = 0;
                        if (passport != null && passport.Software != null)
                        {
                            programCount = passport.Software.Count(kv => kv.Key.StartsWith("Программа"));
                        }
                        mainSheet.Cells[row, 22].Value = programCount > 0 ? programCount.ToString() : "Нет данных";

                        mainSheet.Cells[row, 23].Value = computer.LastUpdate;

                        row++;
                    }

                    mainSheet.Cells.AutoFitColumns();

                    // ========== ЛИСТ 2: АППАРАТНАЯ КОНФИГУРАЦИЯ (ПОДРОБНО) ==========
                    var hardwareSheet = package.Workbook.Worksheets.Add("Аппаратура");
                    int hRow = 1;

                    // Заголовки
                    hardwareSheet.Cells[hRow, 1].Value = "Компьютер";
                    hardwareSheet.Cells[hRow, 2].Value = "Параметр";
                    hardwareSheet.Cells[hRow, 3].Value = "Значение";
                    hardwareSheet.Cells[hRow, 1, hRow, 3].Style.Font.Bold = true;
                    hardwareSheet.Cells[hRow, 1, hRow, 3].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    hardwareSheet.Cells[hRow, 1, hRow, 3].Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                    hRow++;

                    foreach (var computer in computers)
                    {
                        var passport = computer.Tag as PassportData;
                        if (passport != null && passport.Hardware.Count > 0)
                        {
                            // Заголовок компьютера
                            hardwareSheet.Cells[hRow, 1].Value = computer.ComputerName;
                            hardwareSheet.Cells[hRow, 1].Style.Font.Bold = true;
                            hardwareSheet.Cells[hRow, 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                            hardwareSheet.Cells[hRow, 1].Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
                            hRow++;

                            foreach (var kv in passport.Hardware)
                            {
                                hardwareSheet.Cells[hRow, 2].Value = kv.Key;
                                hardwareSheet.Cells[hRow, 3].Value = kv.Value;
                                hRow++;
                            }

                            hRow++; // пустая строка между компьютерами
                        }
                    }
                    hardwareSheet.Cells.AutoFitColumns();

                    // ========== ЛИСТ 3: ПРОГРАММНОЕ ОБЕСПЕЧЕНИЕ ==========
                    var softwareSheet = package.Workbook.Worksheets.Add("Программы");
                    int sRow = 1;

                    // Заголовки
                    softwareSheet.Cells[sRow, 1].Value = "Компьютер";
                    softwareSheet.Cells[sRow, 2].Value = "Программа";
                    softwareSheet.Cells[sRow, 3].Value = "Версия/Производитель";
                    softwareSheet.Cells[sRow, 1, sRow, 3].Style.Font.Bold = true;
                    softwareSheet.Cells[sRow, 1, sRow, 3].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    softwareSheet.Cells[sRow, 1, sRow, 3].Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                    sRow++;

                    foreach (var computer in computers)
                    {
                        var passport = computer.Tag as PassportData;
                        if (passport != null && passport.Software.Count > 0)
                        {
                            bool hasPrograms = false;

                            foreach (var kv in passport.Software)
                            {
                                if (kv.Key.StartsWith("Программа") && !string.IsNullOrEmpty(kv.Value))
                                {
                                    if (!hasPrograms)
                                    {
                                        softwareSheet.Cells[sRow, 1].Value = computer.ComputerName;
                                        softwareSheet.Cells[sRow, 1].Style.Font.Bold = true;
                                        hasPrograms = true;
                                    }

                                    softwareSheet.Cells[sRow, 2].Value = kv.Value;
                                    sRow++;
                                }
                            }

                            if (hasPrograms)
                                sRow++; // пустая строка между компьютерами
                        }
                    }
                    softwareSheet.Cells.AutoFitColumns();

                    // ========== ЛИСТ 4: ДРАЙВЕРЫ ==========
                    var driversSheet = package.Workbook.Worksheets.Add("Драйверы");
                    int dRow = 1;

                    // Заголовки
                    driversSheet.Cells[dRow, 1].Value = "Компьютер";
                    driversSheet.Cells[dRow, 2].Value = "Драйвер";
                    driversSheet.Cells[dRow, 3].Value = "Версия";
                    driversSheet.Cells[dRow, 1, dRow, 3].Style.Font.Bold = true;
                    driversSheet.Cells[dRow, 1, dRow, 3].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    driversSheet.Cells[dRow, 1, dRow, 3].Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                    dRow++;

                    foreach (var computer in computers)
                    {
                        var passport = computer.Tag as PassportData;
                        if (passport != null && passport.Drivers.Count > 0)
                        {
                            bool hasDrivers = false;

                            foreach (var kv in passport.Drivers)
                            {
                                if (!kv.Key.Contains("Всего") && !string.IsNullOrEmpty(kv.Value))
                                {
                                    if (!hasDrivers)
                                    {
                                        driversSheet.Cells[dRow, 1].Value = computer.ComputerName;
                                        driversSheet.Cells[dRow, 1].Style.Font.Bold = true;
                                        hasDrivers = true;
                                    }

                                    driversSheet.Cells[dRow, 2].Value = kv.Key;
                                    driversSheet.Cells[dRow, 3].Value = kv.Value;
                                    dRow++;
                                }
                            }

                            if (hasDrivers)
                                dRow++; // пустая строка между компьютерами
                        }
                    }
                    driversSheet.Cells.AutoFitColumns();

                    // ========== ЛИСТ 5: ПЕРИФЕРИЯ ==========
                    var peripheralsSheet = package.Workbook.Worksheets.Add("Периферия");
                    int pRow = 1;

                    // Заголовки
                    peripheralsSheet.Cells[pRow, 1].Value = "Компьютер";
                    peripheralsSheet.Cells[pRow, 2].Value = "Устройство";
                    peripheralsSheet.Cells[pRow, 3].Value = "Наименование";
                    peripheralsSheet.Cells[pRow, 1, pRow, 3].Style.Font.Bold = true;
                    peripheralsSheet.Cells[pRow, 1, pRow, 3].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    peripheralsSheet.Cells[pRow, 1, pRow, 3].Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                    pRow++;

                    foreach (var computer in computers)
                    {
                        var passport = computer.Tag as PassportData;
                        if (passport != null && passport.Peripherals.Count > 0)
                        {
                            bool hasPeripherals = false;

                            foreach (var kv in passport.Peripherals)
                            {
                                if (!string.IsNullOrEmpty(kv.Value))
                                {
                                    if (!hasPeripherals)
                                    {
                                        peripheralsSheet.Cells[pRow, 1].Value = computer.ComputerName;
                                        peripheralsSheet.Cells[pRow, 1].Style.Font.Bold = true;
                                        hasPeripherals = true;
                                    }

                                    peripheralsSheet.Cells[pRow, 2].Value = kv.Key;
                                    peripheralsSheet.Cells[pRow, 3].Value = kv.Value;
                                    pRow++;
                                }
                            }

                            if (hasPeripherals)
                                pRow++; // пустая строка между компьютерами
                        }
                    }
                    peripheralsSheet.Cells.AutoFitColumns();

                    // Сохраняем файл
                    var fileInfo = new FileInfo(filePath);
                    package.SaveAs(fileInfo);
                }

                return filePath;
            });
        }

        private string EscapeHtml(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return text.Replace("&", "&amp;")
                       .Replace("<", "&lt;")
                       .Replace(">", "&gt;")
                       .Replace("\"", "&quot;")
                       .Replace("'", "&#39;");
        }
    }
}