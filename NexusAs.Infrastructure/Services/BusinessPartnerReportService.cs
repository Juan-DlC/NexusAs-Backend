using Microsoft.EntityFrameworkCore;
using NexusAs.Application.Interfaces;
using NexusAs.Domain.Exceptions;
using NexusAs.Infrastructure.Data;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace NexusAs.Infrastructure.Services
{
    public class BusinessPartnerReportService : IBusinessPartnerReportService
    {
        private readonly NexusAsDbContext _context;

        public BusinessPartnerReportService(NexusAsDbContext context)
        {
            _context = context;
        }

        public async Task<byte[]> GenerateLiquidationPdfAsync(int liquidationId)
        {
            // Configurar licencia de QuestPDF
            QuestPDF.Settings.License = LicenseType.Community;

            var liquidation = await _context.BusinessPartnerLiquidations
                .Include(l => l.BusinessPartner)
                .Include(l => l.ProcessedByUser)
                .Include(l => l.Details)
                    .ThenInclude(d => d.Sale)
                .FirstOrDefaultAsync(l => l.Id == liquidationId);

            if (liquidation == null)
                throw new NotFoundException("Liquidación", liquidationId);

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);

                    page.Header().Column(column =>
                    {
                        column.Item().Text("LIQUIDACIÓN DE SOCIO COMERCIAL")
                            .FontSize(20)
                            .Bold()
                            .FontColor("#2C3E50");

                        column.Item().PaddingTop(10).LineHorizontal(2).LineColor("#3498DB");
                    });

                    page.Content().PaddingTop(20).Column(column =>
                    {
                        // Información básica
                        column.Item().Background("#ECF0F1").Padding(15).Column(info =>
                        {
                            info.Item().Text($"Número: {liquidation.LiquidationNumber}").Bold();
                            info.Item().Text($"Socio: {liquidation.BusinessPartner.Name}").Bold();
                            info.Item().Text($"Fecha: {liquidation.LiquidationDate:dd/MM/yyyy}");
                            info.Item().Text($"Período: {liquidation.FromDate:dd/MM/yyyy} - {liquidation.ToDate:dd/MM/yyyy}");
                        });

                        // Tabla de facturas
                        column.Item().PaddingTop(20).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(100); // Factura
                                columns.RelativeColumn();    // Fecha
                                columns.RelativeColumn();    // Costo Total
                                columns.RelativeColumn();    // Venta Total
                                columns.RelativeColumn();    // Ganancia
                            });

                            // Header
                            table.Header(header =>
                            {
                                header.Cell().Background("#3498DB").Padding(8).Text("Factura").FontColor(Colors.White).Bold();
                                header.Cell().Background("#3498DB").Padding(8).Text("Fecha").FontColor(Colors.White).Bold();
                                header.Cell().Background("#3498DB").Padding(8).Text("Costo").FontColor(Colors.White).Bold();
                                header.Cell().Background("#3498DB").Padding(8).Text("Venta").FontColor(Colors.White).Bold();
                                header.Cell().Background("#3498DB").Padding(8).Text("Ganancia").FontColor(Colors.White).Bold();
                            });

                            // Agrupar por factura
                            var salesSummary = liquidation.Details
                                .GroupBy(d => d.SaleId)
                                .Select(g => new
                                {
                                    SaleNumber = g.First().Sale.SaleNumber,
                                    Date = g.First().Sale.Date,
                                    TotalCost = g.Sum(d => d.Quantity * d.CostPrice),
                                    TotalRevenue = g.Sum(d => d.Quantity * d.SalePrice),
                                    TotalProfit = g.Sum(d => d.RemainingProfit)
                                })
                                .OrderBy(s => s.Date)
                                .ToList();

                            foreach (var sale in salesSummary)
                            {
                                table.Cell().Padding(8).Text(sale.SaleNumber);
                                table.Cell().Padding(8).Text(sale.Date.ToString("dd/MM/yyyy"));
                                table.Cell().Padding(8).AlignRight().Text($"${sale.TotalCost:N0}");
                                table.Cell().Padding(8).AlignRight().Text($"${sale.TotalRevenue:N0}");
                                table.Cell().Padding(8).AlignRight().Text($"${sale.TotalProfit:N0}").Bold();
                            }
                        });

                        // Resumen
                        column.Item().PaddingTop(20).Background("#ECF0F1").Padding(15).Column(summary =>
                        {
                            var totalCost = liquidation.Details.Sum(d => d.Quantity * d.CostPrice);
                            var totalRevenue = liquidation.Details.Sum(d => d.Quantity * d.SalePrice);
                            var totalProfit = liquidation.Details.Sum(d => d.RemainingProfit);
                            var partnerAmount = liquidation.Details.Sum(d => d.BusinessPartnerAmount);
                            var asAmount = liquidation.Details.Sum(d => d.AsAmount);

                            summary.Item().Text("RESUMEN FINANCIERO").FontSize(16).Bold();
                            summary.Item().PaddingTop(10).Row(row =>
                            {
                                row.RelativeItem().Text("Total Costo:");
                                row.RelativeItem().AlignRight().Text($"${totalCost:N0}").Bold();
                            });
                            summary.Item().Row(row =>
                            {
                                row.RelativeItem().Text("Total Venta:");
                                row.RelativeItem().AlignRight().Text($"${totalRevenue:N0}").Bold();
                            });
                            summary.Item().Row(row =>
                            {
                                row.RelativeItem().Text("Ganancia Neta:");
                                row.RelativeItem().AlignRight().Text($"${totalProfit:N0}").Bold();
                            });

                            summary.Item().PaddingTop(10).LineHorizontal(2).LineColor("#3498DB");

                            summary.Item().PaddingTop(10).Row(row =>
                            {
                                row.RelativeItem().Text($"Socio ({liquidation.BusinessPartner.CommissionPercent}%):").Bold().FontSize(12);
                                row.RelativeItem().AlignRight().Text($"${partnerAmount:N0}").Bold().FontSize(14).FontColor("#27AE60");
                            });
                            summary.Item().Row(row =>
                            {
                                row.RelativeItem().Text($"AS Accesorios ({100 - liquidation.BusinessPartner.CommissionPercent}%):").Bold().FontSize(12);
                                row.RelativeItem().AlignRight().Text($"${asAmount:N0}").Bold().FontSize(14).FontColor("#2C3E50");
                            });
                        });
                    });

                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span($"Generado el {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(9).FontColor(Colors.Grey.Darken1);
                    });
                });
            });

            return document.GeneratePdf();
        }
    }
}
