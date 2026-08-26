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
        private readonly ReportStyleHelper _styleHelper;

        public BusinessPartnerReportService(NexusAsDbContext context, ReportStyleHelper styleHelper)
        {
            _context = context;
            _styleHelper = styleHelper;
        }

        public async Task<byte[]> GenerateLiquidationPdfAsync(int liquidationId)
        {
            var liquidation = await _context.BusinessPartnerLiquidations
                .Include(l => l.BusinessPartner)
                .Include(l => l.ProcessedByUser)
                .Include(l => l.Details)
                    .ThenInclude(d => d.Sale)
                        .ThenInclude(s => s.User)
                .Include(l => l.Details)
                    .ThenInclude(d => d.Sale)
                        .ThenInclude(s => s.PaymentMethodEntity)
                .Include(l => l.Details)
                    .ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(l => l.Id == liquidationId);

            if (liquidation == null)
                throw new NotFoundException("Liquidación", liquidationId);

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.Letter);
                    page.Margin(40);
                    page.DefaultTextStyle(x => x.FontSize(10).FontColor(_styleHelper.ColorPrincipal));

                    page.Header().Element(ComposeHeader);
                    page.Content().Element(content => ComposeContent(content, liquidation));
                    page.Footer().Element(ComposeFooter);
                });
            });

            return document.GeneratePdf();
        }

        private void ComposeHeader(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    if (File.Exists(_styleHelper.LogoPath))
                    {
                        column.Item().Height(60).AlignLeft().Image(_styleHelper.LogoPath);
                    }
                });

                row.RelativeItem().Column(column =>
                {
                    column.Item().AlignRight().Text("LIQUIDACIÓN DE SOCIO COMERCIAL")
                        .FontSize(18)
                        .Bold()
                        .FontColor(_styleHelper.ColorAcento);

                    column.Item().AlignRight().Text($"Generado el {DateTime.Now:dd/MM/yyyy HH:mm}")
                        .FontSize(9)
                        .FontColor(Colors.Grey.Darken2);
                });
            });
        }

        private void ComposeContent(IContainer container, Domain.Entities.BusinessPartnerLiquidation liquidation)
        {
            container.PaddingVertical(20).Column(column =>
            {
                column.Spacing(15);

                // Información de la liquidación
                column.Item().Element(c => ComposeLiquidationInfo(c, liquidation));

                // Tabla de ventas
                column.Item().Element(c => ComposeSalesTable(c, liquidation));

                // Resumen financiero
                column.Item().Element(c => ComposeFinancialSummary(c, liquidation));
            });
        }

        private void ComposeLiquidationInfo(IContainer container, Domain.Entities.BusinessPartnerLiquidation liquidation)
        {
            container.Background(_styleHelper.ColorFondoSuave).Padding(15).Column(column =>
            {
                column.Spacing(8);

                column.Item().Row(row =>
                {
                    row.RelativeItem().Text(text =>
                    {
                        text.Span("Número: ").Bold();
                        text.Span(liquidation.LiquidationNumber);
                    });

                    row.RelativeItem().Text(text =>
                    {
                        text.Span("Fecha: ").Bold();
                        text.Span(liquidation.LiquidationDate.ToString("dd/MM/yyyy"));
                    });
                });

                column.Item().Row(row =>
                {
                    row.RelativeItem().Text(text =>
                    {
                        text.Span("Socio Comercial: ").Bold();
                        text.Span(liquidation.BusinessPartner.Name);
                    });

                    row.RelativeItem().Text(text =>
                    {
                        text.Span("Período: ").Bold();
                        text.Span($"{liquidation.FromDate:dd/MM/yyyy} - {liquidation.ToDate:dd/MM/yyyy}");
                    });
                });

                if (!string.IsNullOrWhiteSpace(liquidation.Notes))
                {
                    column.Item().Text(text =>
                    {
                        text.Span("Notas: ").Bold();
                        text.Span(liquidation.Notes);
                    });
                }
            });
        }

        private void ComposeSalesTable(IContainer container, Domain.Entities.BusinessPartnerLiquidation liquidation)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(60);  // Factura
                    columns.ConstantColumn(65);  // Fecha
                    columns.RelativeColumn(1.5f); // Vendedor
                    columns.ConstantColumn(50);  // Pago
                    columns.ConstantColumn(70);  // Venta
                    columns.ConstantColumn(70);  // Costo
                    columns.ConstantColumn(70);  // G.Neta
                    columns.ConstantColumn(70);  // Socio
                    columns.ConstantColumn(70);  // AS
                });

                // Header
                table.Header(header =>
                {
                    header.Cell().Background(_styleHelper.ColorAcento).Padding(5).Text("Factura").FontColor(Colors.White).Bold().FontSize(9);
                    header.Cell().Background(_styleHelper.ColorAcento).Padding(5).Text("Fecha").FontColor(Colors.White).Bold().FontSize(9);
                    header.Cell().Background(_styleHelper.ColorAcento).Padding(5).Text("Vendedor").FontColor(Colors.White).Bold().FontSize(9);
                    header.Cell().Background(_styleHelper.ColorAcento).Padding(5).Text("Pago").FontColor(Colors.White).Bold().FontSize(9);
                    header.Cell().Background(_styleHelper.ColorAcento).Padding(5).Text("Venta").FontColor(Colors.White).Bold().FontSize(9);
                    header.Cell().Background(_styleHelper.ColorAcento).Padding(5).Text("Costo").FontColor(Colors.White).Bold().FontSize(9);
                    header.Cell().Background(_styleHelper.ColorAcento).Padding(5).Text("G.Neta").FontColor(Colors.White).Bold().FontSize(9);
                    header.Cell().Background(_styleHelper.ColorAcento).Padding(5).Text("Socio").FontColor(Colors.White).Bold().FontSize(9);
                    header.Cell().Background(_styleHelper.ColorAcento).Padding(5).Text("AS").FontColor(Colors.White).Bold().FontSize(9);
                });

                // Agrupar por SaleId para evitar repetir ventas
                var groupedBySale = liquidation.Details
                    .GroupBy(d => d.SaleId)
                    .Select(g => new
                    {
                        Sale = g.First().Sale,
                        TotalRevenue = g.Sum(d => d.Quantity * d.SalePrice),
                        TotalCost = g.Sum(d => d.Quantity * d.CostPrice),
                        NetProfit = g.Sum(d => d.RemainingProfit),
                        BusinessPartnerAmount = g.Sum(d => d.BusinessPartnerAmount),
                        AsAmount = g.Sum(d => d.AsAmount)
                    })
                    .OrderBy(x => x.Sale.Date);

                foreach (var item in groupedBySale)
                {
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(item.Sale.SaleNumber).FontSize(8);
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(item.Sale.Date.ToString("dd/MM/yy")).FontSize(8);
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(item.Sale.User?.FullName ?? "Sin vendedor").FontSize(8);
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(item.Sale.PaymentMethodEntity?.Name ?? "Contado").FontSize(8);
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"${item.TotalRevenue:N0}").FontSize(8);
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"${item.TotalCost:N0}").FontSize(8);
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"${item.NetProfit:N0}").FontSize(8);
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"${item.BusinessPartnerAmount:N0}").FontSize(8).FontColor(_styleHelper.ColorExito).Bold();
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"${item.AsAmount:N0}").FontSize(8);
                }
            });
        }

        private void ComposeFinancialSummary(IContainer container, Domain.Entities.BusinessPartnerLiquidation liquidation)
        {
            var totalRevenue = liquidation.Details.Sum(d => d.Quantity * d.SalePrice);
            var totalCost = liquidation.Details.Sum(d => d.Quantity * d.CostPrice);
            var grossProfit = liquidation.Details.Sum(d => d.GrossProfit);
            var partnerDiscount = liquidation.Details.Sum(d => d.PartnerCommissionAmount);
            var netProfit = liquidation.Details.Sum(d => d.RemainingProfit);
            var businessPartnerAmount = liquidation.Details.Sum(d => d.BusinessPartnerAmount);
            var asAmount = liquidation.Details.Sum(d => d.AsAmount);

            container.Background(_styleHelper.ColorFondoSuave).Padding(15).Column(column =>
            {
                column.Spacing(8);

                column.Item().Text("RESUMEN FINANCIERO").Bold().FontSize(12).FontColor(_styleHelper.ColorAcento);

                column.Item().PaddingTop(10).Row(row =>
                {
                    row.RelativeItem().Text("Total ventas brutas:");
                    row.ConstantItem(120).AlignRight().Text($"${totalRevenue:N0}").Bold();
                });

                column.Item().Row(row =>
                {
                    row.RelativeItem().Text("Total costo (inversión):");
                    row.ConstantItem(120).AlignRight().Text($"${totalCost:N0}").Bold();
                });

                column.Item().Row(row =>
                {
                    row.RelativeItem().Text("Ganancia bruta:");
                    row.ConstantItem(120).AlignRight().Text($"${grossProfit:N0}").Bold();
                });

                column.Item().Row(row =>
                {
                    row.RelativeItem().Text("Descuento socias vendedoras:");
                    row.ConstantItem(120).AlignRight().Text($"${partnerDiscount:N0}").Bold().FontColor(_styleHelper.ColorAlerta);
                });

                column.Item().Row(row =>
                {
                    row.RelativeItem().Text("Ganancia neta:");
                    row.ConstantItem(120).AlignRight().Text($"${netProfit:N0}").Bold();
                });

                column.Item().PaddingTop(5).LineHorizontal(2).LineColor(_styleHelper.ColorAcento);

                column.Item().PaddingTop(8).Row(row =>
                {
                    row.RelativeItem().Text($"Le corresponde a {liquidation.BusinessPartner.Name} ({liquidation.BusinessPartner.CommissionPercent}%):")
                        .Bold().FontSize(11);
                    row.ConstantItem(120).AlignRight().Text($"${businessPartnerAmount:N0}")
                        .Bold().FontSize(12).FontColor(_styleHelper.ColorExito);
                });

                column.Item().Row(row =>
                {
                    row.RelativeItem().Text($"Le corresponde a AS Accesorios ({100 - liquidation.BusinessPartner.CommissionPercent}%):")
                        .Bold().FontSize(11);
                    row.ConstantItem(120).AlignRight().Text($"${asAmount:N0}")
                        .Bold().FontSize(12).FontColor(_styleHelper.ColorPrincipal);
                });
            });
        }

        private void ComposeFooter(IContainer container)
        {
            container.AlignCenter().Text(text =>
            {
                text.Span($"Documento generado el {DateTime.Now:dd/MM/yyyy HH:mm}")
                    .FontSize(8)
                    .FontColor(Colors.Grey.Darken1);
            });
        }
    }
}
