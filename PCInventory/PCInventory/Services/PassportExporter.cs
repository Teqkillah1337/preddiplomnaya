using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using iTextSharp.text;
using iTextSharp.text.pdf;
using PCInventory.Models;

namespace PCInventory.Services
{
    public class PassportExporter
    {
        private readonly string _exportPath;

        public string ExportPath => _exportPath;

        public PassportExporter(string exportPath = null)
        {
            _exportPath = exportPath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "СведенияОборудовании");
            if (!Directory.Exists(_exportPath))
                Directory.CreateDirectory(_exportPath);
        }

        public string ExportToHtml(PassportData passport, string inventoryNumber = null, string filename = null)
        {
            var sb = new StringBuilder();

            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html><head><meta charset='utf-8'>");
            sb.AppendLine("<title>Сведения о компьютере</title>");
            sb.AppendLine("<style>");
            sb.AppendLine("body { font-family: Arial, sans-serif; margin: 20px; background: #f5f5f5; }");
            sb.AppendLine(".passport { background: white; border: 2px solid #333; padding: 20px; max-width: 1000px; margin: 0 auto; }");
            sb.AppendLine(".header { text-align: center; border-bottom: 2px solid #333; padding-bottom: 10px; margin-bottom: 20px; }");
            sb.AppendLine(".inventory { background: #e8f5e8; padding: 5px 10px; border-radius: 4px; display: inline-block; margin-top: 5px; }");
            sb.AppendLine(".section { margin-bottom: 20px; }");
            sb.AppendLine(".section h2 { background: #333; color: white; padding: 5px 10px; margin: 0; }");
            sb.AppendLine(".section table { width: 100%; border-collapse: collapse; }");
            sb.AppendLine(".section td { padding: 8px; border: 1px solid #ddd; vertical-align: top; }");
            sb.AppendLine(".section td:first-child { font-weight: bold; width: 30%; background: #f9f9f9; }");
            sb.AppendLine(".footer { text-align: center; margin-top: 30px; font-style: italic; }");
            sb.AppendLine("</style>");
            sb.AppendLine("</head><body>");

            sb.AppendLine("<div class='passport'>");
            sb.AppendLine("<div class='header'>");
            sb.AppendLine("<h1>СВЕДЕНИЯ О КОМПЬЮТЕРЕ</h1>");

            if (!string.IsNullOrEmpty(inventoryNumber))
                sb.AppendLine($"<div class='inventory'>Инвентарный номер: {EscapeHtml(inventoryNumber)}</div>");

            sb.AppendLine($"<h2>№ {passport.UniqueId} от {passport.CollectedAt:dd.MM.yyyy}</h2>");
            sb.AppendLine("</div>");

            // Аппаратная конфигурация
            if (passport.Hardware.Count > 0)
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

            // Программное обеспечение
            if (passport.Software.Count > 0)
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

            // Драйверы
            if (passport.Drivers.Count > 0)
            {
                sb.AppendLine("<div class='section'>");
                sb.AppendLine("<h2>ДРАЙВЕРЫ УСТРОЙСТВ</h2>");
                sb.AppendLine("</table>");
                foreach (var kv in passport.Drivers)
                {
                    sb.AppendLine($"<tr><td>{EscapeHtml(kv.Key)}</td><td>{EscapeHtml(kv.Value)}</td></tr>");
                }
                sb.AppendLine("</table>");
                sb.AppendLine("</div>");
            }

            // Периферийные устройства
            if (passport.Peripherals.Count > 0)
            {
                sb.AppendLine("<div class='section'>");
                sb.AppendLine("<h2>ПЕРИФЕРИЙНЫЕ УСТРОЙСТВА</h2>");
                sb.AppendLine("<table>");
                foreach (var kv in passport.Peripherals)
                {
                    sb.AppendLine($"<tr><td>{EscapeHtml(kv.Key)}</td><td>{EscapeHtml(kv.Value)}</td></tr>");
                }
                sb.AppendLine("</tr>");
                sb.AppendLine("</div>");
            }

            sb.AppendLine("<div class='footer'>");
            sb.AppendLine($"<p>Сведения сформированы: {passport.CollectedAt:dd.MM.yyyy HH:mm:ss}</p>");
            sb.AppendLine("</div>");

            sb.AppendLine("</div>");
            sb.AppendLine("</body></html>");

            filename = filename ?? $"сведения_{passport.UniqueId}_{DateTime.Now:yyyyMMddHHmmss}.html";
            string fullPath = Path.Combine(_exportPath, filename);
            File.WriteAllText(fullPath, sb.ToString(), Encoding.UTF8);
            return fullPath;
        }

        public string ExportToPdf(PassportData passport, string filename = null)
        {
            filename = filename ?? $"сведения_{passport.UniqueId}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
            string fullPath = Path.Combine(_exportPath, filename);

            try
            {
                RegisterRussianFonts();

                using (var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write))
                {
                    var document = new Document(PageSize.A4, 40, 40, 40, 40);
                    var writer = PdfWriter.GetInstance(document, fs);
                    document.Open();

                    // Заголовок
                    var titleFont = FontFactory.GetFont("ArialUnicode", BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 20, Font.BOLD, BaseColor.BLACK);
                    var title = new Paragraph("СВЕДЕНИЯ О КОМПЬЮТЕРЕ", titleFont)
                    {
                        Alignment = Element.ALIGN_CENTER,
                        SpacingAfter = 10f
                    };
                    document.Add(title);

                    var subtitleFont = FontFactory.GetFont("ArialUnicode", BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 14, Font.BOLD, BaseColor.DARK_GRAY);
                    var subtitle = new Paragraph($"№ {passport.UniqueId} от {passport.CollectedAt:dd.MM.yyyy}", subtitleFont)
                    {
                        Alignment = Element.ALIGN_CENTER,
                        SpacingAfter = 25f
                    };
                    document.Add(subtitle);

                    // Аппаратная конфигурация
                    AddPdfSection(document, "АППАРАТНАЯ КОНФИГУРАЦИЯ", passport.Hardware);
                    AddPdfSection(document, "ПРОГРАММНОЕ ОБЕСПЕЧЕНИЕ", passport.Software);
                    AddPdfSection(document, "ДРАЙВЕРЫ УСТРОЙСТВ", passport.Drivers);
                    AddPdfSection(document, "ПЕРИФЕРИЙНЫЕ УСТРОЙСТВА", passport.Peripherals);

                    var footerFont = FontFactory.GetFont("ArialUnicode", BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 10, Font.ITALIC, BaseColor.DARK_GRAY);
                    var footer = new Paragraph($"Сведения сформированы: {passport.CollectedAt:dd.MM.yyyy HH:mm:ss}", footerFont)
                    {
                        Alignment = Element.ALIGN_CENTER,
                        SpacingBefore = 20f
                    };
                    document.Add(footer);

                    document.Close();
                }
                return fullPath;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании PDF: {ex.Message}", "Ошибка");
                return null;
            }
        }

        private void RegisterRussianFonts()
        {
            if (!FontFactory.IsRegistered("ArialUnicode"))
            {
                string[] fontPaths = {
                    @"C:\Windows\Fonts\arial.ttf",
                    @"C:\Windows\Fonts\arialuni.ttf",
                    @"C:\Windows\Fonts\times.ttf"
                };

                foreach (string fontPath in fontPaths)
                {
                    if (File.Exists(fontPath))
                    {
                        FontFactory.Register(fontPath, "ArialUnicode");
                        break;
                    }
                }
            }
        }

        private void AddPdfSection(Document document, string sectionTitle, Dictionary<string, string> data)
        {
            if (data.Count == 0) return;

            var sectionFont = FontFactory.GetFont("ArialUnicode", BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 12, Font.BOLD, BaseColor.WHITE);
            var sectionHeader = new Paragraph(sectionTitle, sectionFont) { SpacingBefore = 20f, SpacingAfter = 10f };

            var headerCell = new PdfPCell(sectionHeader)
            {
                BackgroundColor = new BaseColor(51, 51, 51),
                BorderWidth = 0,
                Padding = 8f,
                HorizontalAlignment = Element.ALIGN_LEFT
            };

            var headerTable = new PdfPTable(1) { WidthPercentage = 100 };
            headerTable.AddCell(headerCell);
            document.Add(headerTable);

            var dataTable = new PdfPTable(2) { WidthPercentage = 100 };
            dataTable.SetWidths(new float[] { 35, 65 });

            var boldFont = FontFactory.GetFont("ArialUnicode", BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 10, Font.BOLD, BaseColor.BLACK);
            var normalFont = FontFactory.GetFont("ArialUnicode", BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 10, Font.NORMAL, BaseColor.BLACK);

            foreach (var kv in data)
            {
                var nameCell = new PdfPCell(new Phrase(kv.Key, boldFont))
                {
                    BorderWidth = 0.5f,
                    BorderColor = new BaseColor(221, 221, 221),
                    Padding = 8f,
                    BackgroundColor = new BaseColor(249, 249, 249),
                    HorizontalAlignment = Element.ALIGN_LEFT
                };
                dataTable.AddCell(nameCell);

                var valueCell = new PdfPCell(new Phrase(kv.Value ?? "", normalFont))
                {
                    BorderWidth = 0.5f,
                    BorderColor = new BaseColor(221, 221, 221),
                    Padding = 8f,
                    HorizontalAlignment = Element.ALIGN_LEFT
                };
                dataTable.AddCell(valueCell);
            }

            document.Add(dataTable);
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