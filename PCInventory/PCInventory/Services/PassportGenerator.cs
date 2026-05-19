using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PCInventory.Models;

namespace PCInventory.Services
{
    public class PassportGenerator
    {
        private readonly string _outputPath;

        public PassportGenerator(string outputPath = null)
        {
            _outputPath = outputPath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Passports");
            if (!Directory.Exists(_outputPath))
                Directory.CreateDirectory(_outputPath);
        }

        public async Task<string> GeneratePassportAsync(ComputerInfo computer, HardwareInfo hardware)
        {
            return await Task.Run(() =>
            {
                string filename = $"Passport_{computer.ComputerName}_{DateTime.Now:yyyyMMdd_HHmmss}.html";
                string fullPath = Path.Combine(_outputPath, filename);

                var html = new StringBuilder();
                html.AppendLine("<!DOCTYPE html>");
                html.AppendLine("<html>");
                html.AppendLine("<head>");
                html.AppendLine("<meta charset='UTF-8'>");
                html.AppendLine($"<title>Паспорт компьютера {computer.ComputerName}</title>");
                html.AppendLine(@"<style>
                    body { font-family: 'Segoe UI', Arial, sans-serif; margin: 0; padding: 20px; background: #f0f0f0; }
                    .passport { max-width: 900px; margin: 0 auto; background: white; border-radius: 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); overflow: hidden; }
                    .header { background: linear-gradient(135deg, #7B1FA2, #9C27B0); color: white; padding: 30px; text-align: center; }
                    .header h1 { margin: 0; font-size: 24px; }
                    .header p { margin: 10px 0 0; opacity: 0.9; }
                    .content { padding: 30px; }
                    .section { margin-bottom: 25px; border-bottom: 1px solid #e0e0e0; padding-bottom: 15px; }
                    .section-title { font-size: 18px; font-weight: bold; color: #7B1FA2; margin-bottom: 15px; display: flex; align-items: center; }
                    .section-title span { margin-right: 10px; font-size: 24px; }
                    .info-row { display: flex; margin-bottom: 10px; padding: 8px; background: #f9f9f9; border-radius: 5px; }
                    .info-label { width: 200px; font-weight: bold; color: #555; }
                    .info-value { flex: 1; color: #333; }
                    .disk-item { margin-bottom: 10px; padding: 10px; background: #f5f5f5; border-radius: 5px; }
                    .footer { background: #f9f9f9; padding: 15px; text-align: center; font-size: 12px; color: #666; }
                    .status-online { color: #4CAF50; font-weight: bold; }
                    .status-offline { color: #f44336; font-weight: bold; }
                </style>");
                html.AppendLine("</head>");
                html.AppendLine("<body>");
                html.AppendLine("<div class='passport'>");
                html.AppendLine("<div class='header'>");
                html.AppendLine($"<h1>Паспорт компьютера</h1>");
                html.AppendLine($"<p>{computer.ComputerName}</p>");
                html.AppendLine("</div>");
                html.AppendLine("<div class='content'>");

                // Основная информация
                html.AppendLine("<div class='section'>");
                html.AppendLine("<div class='section-title'><span>💻</span> Основная информация</div>");
                html.AppendLine($"<div class='info-row'><div class='info-label'>Имя компьютера:</div><div class='info-value'>{computer.ComputerName}</div></div>");
                html.AppendLine($"<div class='info-row'><div class='info-label'>IP адрес:</div><div class='info-value'>{computer.IPAddress}</div></div>");
                html.AppendLine($"<div class='info-row'><div class='info-label'>MAC адрес:</div><div class='info-value'>{computer.MACAddress ?? "Не определен"}</div></div>");
                html.AppendLine($"<div class='info-row'><div class='info-label'>Статус:</div><div class='info-value'><span class='{(computer.Status == "Онлайн" ? "status-online" : "status-offline")}'>{computer.Status}</span></div></div>");
                html.AppendLine($"<div class='info-row'><div class='info-label'>Производитель:</div><div class='info-value'>{computer.Manufacturer}</div></div>");
                html.AppendLine($"<div class='info-row'><div class='info-label'>Модель:</div><div class='info-value'>{computer.Model}</div></div>");
                html.AppendLine($"<div class='info-row'><div class='info-label'>Серийный номер:</div><div class='info-value'>{computer.SerialNumber}</div></div>");
                html.AppendLine("</div>");

                // Аппаратное обеспечение
                html.AppendLine("<div class='section'>");
                html.AppendLine("<div class='section-title'><span>🖥️</span> Аппаратное обеспечение</div>");
                html.AppendLine($"<div class='info-row'><div class='info-label'>Процессор:</div><div class='info-value'>{computer.CPU}</div></div>");
                html.AppendLine($"<div class='info-row'><div class='info-label'>Оперативная память:</div><div class='info-value'>{computer.RAM}</div></div>");
                html.AppendLine($"<div class='info-row'><div class='info-label'>Материнская плата:</div><div class='info-value'>{computer.Motherboard}</div></div>");
                html.AppendLine($"<div class='info-row'><div class='info-label'>Видеокарта:</div><div class='info-value'>{computer.GPU}</div></div>");
                html.AppendLine("</div>");

                // Диски
                if (!string.IsNullOrEmpty(computer.DiskInfo))
                {
                    html.AppendLine("<div class='section'>");
                    html.AppendLine("<div class='section-title'><span>💾</span> Диски</div>");
                    var disks = computer.DiskInfo.Split('\n');
                    foreach (var disk in disks)
                    {
                        if (!string.IsNullOrWhiteSpace(disk))
                            html.AppendLine($"<div class='disk-item'>{disk}</div>");
                    }
                    html.AppendLine("</div>");
                }

                // Операционная система
                html.AppendLine("<div class='section'>");
                html.AppendLine("<div class='section-title'><span>⚙️</span> Операционная система</div>");
                html.AppendLine($"<div class='info-row'><div class='info-label'>ОС:</div><div class='info-value'>{computer.OS}</div></div>");
                html.AppendLine($"<div class='info-row'><div class='info-label'>Последнее обновление:</div><div class='info-value'>{computer.LastUpdate}</div></div>");
                html.AppendLine("</div>");

                html.AppendLine("</div>");
                html.AppendLine($"<div class='footer'>Паспорт сформирован программой PCInventory {DateTime.Now:dd.MM.yyyy HH:mm:ss}</div>");
                html.AppendLine("</div>");
                html.AppendLine("</body>");
                html.AppendLine("</html>");

                File.WriteAllText(fullPath, html.ToString(), Encoding.UTF8);
                return fullPath;
            });
        }
    }
}