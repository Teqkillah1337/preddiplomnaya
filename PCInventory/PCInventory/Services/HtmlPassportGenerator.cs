using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using PCInventory.Models;

namespace PCInventory.Services
{
    public class HtmlPassportGenerator
    {
        public string GeneratePassport(ComputerInfo computer, PassportData passport, string inventoryNumber = null)
        {
            var sb = new StringBuilder();

            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html><head><meta charset='utf-8'>");
            sb.AppendLine("<title>Паспорт компьютера</title>");
            sb.AppendLine("<style>");
            sb.AppendLine("body { font-family: Arial, sans-serif; margin: 20px; background: #f5f5f5; }");
            sb.AppendLine(".passport { background: white; border: 2px solid #333; padding: 20px; max-width: 1000px; margin: 0 auto; }");
            sb.AppendLine(".header { text-align: center; border-bottom: 2px solid #333; padding-bottom: 10px; margin-bottom: 20px; }");
            sb.AppendLine(".section { margin-bottom: 20px; }");
            sb.AppendLine(".section h2 { background: #333; color: white; padding: 5px 10px; margin: 0; }");
            sb.AppendLine(".section table { width: 100%; border-collapse: collapse; }");
            sb.AppendLine(".section td { padding: 8px; border: 1px solid #ddd; vertical-align: top; }");
            sb.AppendLine(".section td:first-child { font-weight: bold; width: 30%; background: #f9f9f9; }");
            sb.AppendLine(".footer { text-align: center; margin-top: 30px; font-style: italic; }");
            sb.AppendLine(".inventory { background: #e8f5e8; padding: 5px 10px; border-radius: 4px; display: inline-block; margin-top: 5px; }");
            sb.AppendLine("</style>");
            sb.AppendLine("</head><body>");

            sb.AppendLine("<div class='passport'>");

            sb.AppendLine("<div class='header'>");
            sb.AppendLine("<h1>ПАСПОРТ КОМПЬЮТЕРА</h1>");

            // Инвентарный номер
            if (!string.IsNullOrEmpty(inventoryNumber))
            {
                sb.AppendLine($"<div class='inventory'>Инвентарный номер: {EscapeHtml(inventoryNumber)}</div>");
            }

            sb.AppendLine($"<h2>№ {passport?.UniqueId ?? computer.UniqueId ?? "---"} от {DateTime.Now:dd.MM.yyyy}</h2>");
            sb.AppendLine("</div>");

            // АППАРАТНАЯ КОНФИГУРАЦИЯ
            if (passport?.Hardware.Count > 0)
            {
                sb.AppendLine("<div class='section'>");
                sb.AppendLine("<h2>АППАРАТНАЯ КОНФИГУРАЦИЯ</h2>");
                sb.AppendLine("<table>");
                foreach (var kv in passport.Hardware)
                {
                    sb.AppendLine($"<tr><td>{EscapeHtml(kv.Key)}</td><td>{EscapeHtml(kv.Value)}</td></tr>");
                }
                sb.AppendLine("</table>");
                sb.AppendLine("</div>");
            }
            else
            {
                // Если нет passport.Hardware, используем данные из ComputerInfo
                sb.AppendLine("<div class='section'>");
                sb.AppendLine("<h2>АППАРАТНАЯ КОНФИГУРАЦИЯ</h2>");
                sb.AppendLine("<table>");
                if (!string.IsNullOrEmpty(computer.CPU))
                    sb.AppendLine($"<tr><td>Процессор</td><td>{EscapeHtml(computer.CPU)}</td></tr>");
                if (!string.IsNullOrEmpty(computer.RAM))
                    sb.AppendLine($"<tr><td>Оперативная память</td><td>{EscapeHtml(computer.RAM)}</td></tr>");
                if (!string.IsNullOrEmpty(computer.Motherboard))
                    sb.AppendLine($"<tr><td>Материнская плата</td><td>{EscapeHtml(computer.Motherboard)}</td></tr>");
                if (!string.IsNullOrEmpty(computer.GPU))
                    sb.AppendLine($"<tr><td>Видеокарта</td><td>{EscapeHtml(computer.GPU)}</td></tr>");
                if (!string.IsNullOrEmpty(computer.DiskInfo))
                    sb.AppendLine($"<tr><td>Диски</td><td>{EscapeHtml(computer.DiskInfo)}</td></tr>");
                sb.AppendLine("</table>");
                sb.AppendLine("</div>");
            }

            // ПРОГРАММНОЕ ОБЕСПЕЧЕНИЕ
            if (passport?.Software.Count > 0)
            {
                sb.AppendLine("<div class='section'>");
                sb.AppendLine("<h2>ПРОГРАММНОЕ ОБЕСПЕЧЕНИЕ</h2>");
                sb.AppendLine("<table>");
                foreach (var kv in passport.Software)
                {
                    sb.AppendLine($"<tr><td>{EscapeHtml(kv.Key)}</td><td>{EscapeHtml(kv.Value)}</td></tr>");
                }
                sb.AppendLine("</table>");
                sb.AppendLine("</div>");
            }
            else if (!string.IsNullOrEmpty(computer.OS))
            {
                sb.AppendLine("<div class='section'>");
                sb.AppendLine("<h2>ПРОГРАММНОЕ ОБЕСПЕЧЕНИЕ</h2>");
                sb.AppendLine("<tr>");
                sb.AppendLine($"<tr><td>Операционная система</td><td>{EscapeHtml(computer.OS)}</td></tr>");
                sb.AppendLine("</table>");
                sb.AppendLine("</div>");
            }

            // ДРАЙВЕРЫ
            if (passport?.Drivers.Count > 0)
            {
                sb.AppendLine("<div class='section'>");
                sb.AppendLine("<h2>ДРАЙВЕРЫ УСТРОЙСТВ</h2>");
                sb.AppendLine("<td>");
                foreach (var kv in passport.Drivers)
                {
                    sb.AppendLine($"<tr><td>{EscapeHtml(kv.Key)}</td><td>{EscapeHtml(kv.Value)}</td></tr>");
                }
                sb.AppendLine("</table>");
                sb.AppendLine("</div>");
            }

            // ПЕРИФЕРИЙНЫЕ УСТРОЙСТВА
            if (passport?.Peripherals.Count > 0)
            {
                sb.AppendLine("<div class='section'>");
                sb.AppendLine("<h2>ПЕРИФЕРИЙНЫЕ УСТРОЙСТВА</h2>");
                sb.AppendLine("<table>");
                foreach (var kv in passport.Peripherals)
                {
                    sb.AppendLine($"<tr><td>{EscapeHtml(kv.Key)}</td><td>{EscapeHtml(kv.Value)}</td></tr>");
                }
                sb.AppendLine("</table>");
                sb.AppendLine("</div>");
            }

            sb.AppendLine("<div class='footer'>");
            sb.AppendLine($"<p>Паспорт сформирован: {DateTime.Now:dd.MM.yyyy HH:mm:ss}</p>");
            sb.AppendLine("</div>");

            sb.AppendLine("</div>");
            sb.AppendLine("</body></html>");

            return sb.ToString();
        }

        public string SaveToFile(ComputerInfo computer, PassportData passport, string inventoryNumber = null, string outputPath = null)
        {
            if (string.IsNullOrEmpty(outputPath))
            {
                outputPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ПаспортаПК");
            }

            if (!Directory.Exists(outputPath))
                Directory.CreateDirectory(outputPath);

            string invPart = string.IsNullOrEmpty(inventoryNumber) ? "" : $"_инв{inventoryNumber}";
            string filename = $"Паспорт_{computer.ComputerName}{invPart}_{DateTime.Now:yyyyMMdd_HHmmss}.html";
            string fullPath = Path.Combine(outputPath, filename);

            string html = GeneratePassport(computer, passport, inventoryNumber);
            File.WriteAllText(fullPath, html, Encoding.UTF8);

            return fullPath;
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