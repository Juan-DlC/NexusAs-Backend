using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using NexusAs.Application.Interfaces;
using NexusAs.Domain.Enums;
using NexusAs.Infrastructure.Data;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace NexusAs.Infrastructure.Services
{
    public class SalesReportService : ISalesReportService
    {
        private readonly NexusAsDbContext _context;
        private readonly ReportStyleHelper _reportStyle;

        public SalesReportService(NexusAsDbContext context, ReportStyleHelper reportStyle)
        {
            _context = context;
            _reportStyle = reportStyle;
        }

        public async Task<byte[]> GenerateSalesReportPdfAsync(DateTime from, DateTime to)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var sales = await _context.Sales
                .Include(s => s.Customer)
                .Where(s => s.IsActive && s.Date >= from && s.Date <= to)
                .ToListAsync();
            var salesList = sales.ToList();

            var logoBytes = File.Exists(_reportStyle.LogoPath)
                ? File.ReadAllBytes(_reportStyle.LogoPath) : null;

            var document = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(11).FontColor(_reportStyle.ColorPrincipal));

                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            if (logoBytes != null)
                                row.ConstantItem(70).Image(logoBytes).FitArea();

                            row.RelativeItem().Column(titleCol =>
                            {
                                titleCol.Item().PaddingLeft(8)
                                    .Text("AS Accesorios")
                                    .FontSize(20).Bold().FontColor(_reportStyle.ColorPrincipal);
                                titleCol.Item().PaddingLeft(8)
                                    .Text("Tenis • Cuidado de Piel • y más")
                                    .FontSize(9).FontColor(_reportStyle.ColorAcento).Italic();
                                titleCol.Item().PaddingLeft(8).PaddingTop(2)
                                    .Text("Informe de Ventas")
                                    .FontSize(13).Bold().FontColor(_reportStyle.ColorAcento);
                                titleCol.Item().PaddingLeft(8)
                                    .Text($"Período: {from:dd/MM/yyyy} — {to:dd/MM/yyyy}")
                                    .FontSize(10).FontColor(_reportStyle.ColorPrincipal);
                            });
                        });
                        col.Item().PaddingTop(6)
                            .LineHorizontal(2).LineColor(_reportStyle.ColorAcento);
                    });

                    page.Content().PaddingTop(10).Column(col =>
                    {
                        // Resumen ejecutivo
                        col.Item().Background(_reportStyle.ColorFondoSuave).Padding(8).Column(res =>
                        {
                            res.Item().Text("Resumen Ejecutivo")
                                .FontSize(13).Bold().FontColor(_reportStyle.ColorAcento);
                            res.Item().PaddingTop(4).Table(table =>
                            {
                                table.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn();
                                    c.RelativeColumn();
                                });
                                table.Cell().Text("Total Ventas:").Bold();
                                table.Cell().AlignRight()
                                    .Text($"${salesList.Sum(s => s.Total):N0}");
                                table.Cell().Text("Total Contado:").Bold();
                                table.Cell().AlignRight()
                                    .Text($"${salesList.Where(s => s.PaymentMethodEntity != null && s.PaymentMethodEntity.Code == "CASH").Sum(s => s.Total):N0}");
                                table.Cell().Text("Total Crédito:").Bold();
                                table.Cell().AlignRight()
                                    .Text($"${salesList.Where(s => s.PaymentMethodEntity != null && s.PaymentMethodEntity.Code == "CREDIT").Sum(s => s.Total):N0}");
                                table.Cell().Text("Número de Transacciones:").Bold();
                                table.Cell().AlignRight()
                                    .Text($"{salesList.Count}");
                            });
                        });

                        col.Item().PaddingTop(15)
                            .Text("Detalle de Ventas")
                            .FontSize(13).Bold().FontColor(_reportStyle.ColorPrincipal);

                        col.Item().PaddingTop(4).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(80);
                                c.RelativeColumn();
                                c.ConstantColumn(70);
                                c.ConstantColumn(80);
                                c.ConstantColumn(80);
                            });

                            table.Header(header =>
                            {
                                foreach (var title in new[] { "Factura", "Cliente", "Fecha", "Método", "Total" })
                                {
                                    header.Cell().Background(_reportStyle.ColorPrincipal)
                                        .Padding(5).Text(title)
                                        .FontColor(Colors.White).Bold();
                                }
                            });

                            foreach (var sale in salesList)
                            {
                                var bg = salesList.IndexOf(sale) % 2 == 0
                                      ? "#FFFFFF" : _reportStyle.ColorFondoSuave;
                                table.Cell().Background(bg).Padding(5)
                                    .Text(sale.SaleNumber);
                                table.Cell().Background(bg).Padding(5)
                                    .Text(sale.Customer?.Name ?? "Sin cliente");
                                table.Cell().Background(bg).Padding(5)
                                    .Text(sale.Date.ToString("dd/MM/yy"));
                                table.Cell().Background(bg).Padding(5)
                                    .Text(sale.PaymentMethodEntity?.Code == "CASH"
                                        ? "Contado" : "Crédito");
                                table.Cell().Background(bg).Padding(5)
                                    .Text($"${sale.Total:N0}");
                            }
                        });
                    });

                    page.Footer().Column(col =>
                    {
                        col.Item().LineHorizontal(1).LineColor(_reportStyle.ColorAcento);
                        col.Item().PaddingTop(4).Row(row =>
                        {
                            row.RelativeItem().Text("AS Accesorios")
                                .FontSize(8).FontColor(_reportStyle.ColorAcento).Italic();
                            row.RelativeItem().AlignCenter()
                                .Text(x =>
                                {
                                    x.Span("Página ").FontSize(8);
                                    x.CurrentPageNumber().FontSize(8);
                                    x.Span(" de ").FontSize(8);
                                    x.TotalPages().FontSize(8);
                                });
                            row.RelativeItem().AlignRight()
                                .Text($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}")
                                .FontSize(8).FontColor(_reportStyle.ColorPrincipal);
                        });
                    });
                });
            });

            return document.GeneratePdf();
        }

        public async Task<byte[]> GenerateSalesReportExcelAsync(DateTime from, DateTime to)
        {
            var sales = await _context.Sales
                .Include(s => s.Customer)
                .Where(s => s.IsActive && s.Date >= from && s.Date <= to)
                .ToListAsync();
            var salesList = sales.ToList();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Ventas");

            ws.Cell(1, 1).Value = "AS Accesorios — Informe de Ventas";
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 16;
            ws.Cell(1, 1).Style.Font.FontColor = XLColor.FromHtml("#4A4A4A");
            ws.Range(1, 1, 1, 5).Merge();

            ws.Cell(2, 1).Value = $"Período: {from:dd/MM/yyyy} — {to:dd/MM/yyyy}";
            ws.Cell(2, 1).Style.Font.FontColor = XLColor.FromHtml("#C8956C");
            ws.Range(2, 1, 2, 5).Merge();

            ws.Cell(4, 1).Value = "Factura";
            ws.Cell(4, 2).Value = "Cliente";
            ws.Cell(4, 3).Value = "Fecha";
            ws.Cell(4, 4).Value = "Método de Pago";
            ws.Cell(4, 5).Value = "Total";

            var headerRange = ws.Range(4, 1, 4, 5);
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#4A4A4A");
            headerRange.Style.Font.FontColor = XLColor.White;
            headerRange.Style.Font.Bold = true;

            var row = 5;
            foreach (var sale in salesList)
            {
                var bgColor = row % 2 == 0
                    ? XLColor.FromHtml("#F5EDE8") : XLColor.White;
                ws.Cell(row, 1).Value = sale.SaleNumber;
                ws.Cell(row, 2).Value = sale.Customer?.Name ?? "Sin cliente";
                ws.Cell(row, 3).Value = sale.Date.ToString("dd/MM/yyyy");
                ws.Cell(row, 4).Value = sale.PaymentMethodEntity?.Code == "CASH"
                    ? "Contado" : "Crédito";
                ws.Cell(row, 5).Value = sale.Total;
                ws.Cell(row, 5).Style.NumberFormat.Format = "$#,##0";
                ws.Range(row, 1, row, 5).Style.Fill.BackgroundColor = bgColor;
                row++;
            }

            ws.Cell(row + 1, 4).Value = "TOTAL:";
            ws.Cell(row + 1, 4).Style.Font.Bold = true;
            ws.Cell(row + 1, 4).Style.Font.FontColor = XLColor.FromHtml("#4A4A4A");
            ws.Cell(row + 1, 5).FormulaA1 = $"=SUM(E5:E{row - 1})";
            ws.Cell(row + 1, 5).Style.Font.Bold = true;
            ws.Cell(row + 1, 5).Style.NumberFormat.Format = "$#,##0";
            ws.Cell(row + 1, 5).Style.Font.FontColor = XLColor.FromHtml("#C8956C");

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
    }
}
