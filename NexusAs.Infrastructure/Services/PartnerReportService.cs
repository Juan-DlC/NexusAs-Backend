using Microsoft.EntityFrameworkCore;
using NexusAs.Application.DTOs.Partners;
using NexusAs.Application.Interfaces;
using NexusAs.Domain.Enums;
using NexusAs.Infrastructure.Data;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace NexusAs.Infrastructure.Services
{
    public class PartnerReportService : IPartnerReportService
    {
        private readonly NexusAsDbContext _context;
        private readonly ReportStyleHelper _reportStyle;

        public PartnerReportService(NexusAsDbContext context, ReportStyleHelper reportStyle)
        {
            _context = context;
            _reportStyle = reportStyle;
        }

        public async Task<byte[]> GeneratePartnerStatementPdfAsync(
    int partnerConfigId, DateTime from, DateTime to)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var config = await _context.PartnerConfigs
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == partnerConfigId);

            if (config == null)
                throw new Exception($"Configuración de socia {partnerConfigId} no encontrada.");

            var sales = await _context.PartnerSales
                .Include(ps => ps.Product)
                .Include(ps => ps.Sale)
                .Where(ps => ps.PartnerConfigId == partnerConfigId
                    && ps.Date >= from && ps.Date <= to)
                .ToListAsync();

            var liquidations = await _context.PartnerLiquidations
                .Where(pl => pl.PartnerConfigId == partnerConfigId
                    && pl.Date >= from && pl.Date <= to)
                .ToListAsync();

            var totalDebt = sales.Sum(s => s.PartnerPrice * s.Quantity);
            var totalPaid = liquidations
                .Where(l => l.Type == Domain.Enums.LiquidationType.Payment)
                .Sum(l => l.Amount);
            var totalEarnings = sales.Sum(s => s.PartnerEarning);

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
                                    .Text("Estado de Cuenta — Socia")
                                    .FontSize(13).Bold().FontColor(_reportStyle.ColorAcento);
                                titleCol.Item().PaddingLeft(8)
                                    .Text($"Socia: {config.User?.FullName ?? ""}")
                                    .FontSize(11).FontColor(_reportStyle.ColorPrincipal);
                                titleCol.Item().PaddingLeft(8)
                                    .Text($"Período: {from:dd/MM/yyyy} — {to:dd/MM/yyyy}")
                                    .FontSize(10);
                            });
                        });
                        col.Item().PaddingTop(6).LineHorizontal(2).LineColor(_reportStyle.ColorAcento);
                    });

                    page.Content().PaddingTop(10).Column(col =>
                    {
                        // Resumen
                        col.Item().Background(_reportStyle.ColorFondoSuave).Padding(8).Column(res =>
                        {
                            res.Item().Text("Resumen").FontSize(13).Bold().FontColor(_reportStyle.ColorAcento);
                            res.Item().PaddingTop(4).Table(table =>
                            {
                                table.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn();
                                    c.RelativeColumn();
                                });
                                table.Cell().Text("Total deuda con AS:").Bold();
                                table.Cell().AlignRight().Text($"${totalDebt:N0}");
                                table.Cell().Text("Total abonado:").Bold().FontColor(_reportStyle.ColorExito);
                                table.Cell().AlignRight()
                                    .Text($"${totalPaid:N0}").FontColor(_reportStyle.ColorExito);
                                table.Cell().Text("Saldo pendiente:").Bold().FontColor(_reportStyle.ColorAlerta);
                                table.Cell().AlignRight()
                                    .Text($"${totalDebt - totalPaid:N0}").Bold().FontColor(_reportStyle.ColorAlerta);
                                table.Cell().Text("Mis ganancias estimadas:").Bold();
                                table.Cell().AlignRight().Text($"${totalEarnings:N0}");
                            });
                        });

                        // Productos tomados
                        col.Item().PaddingTop(12)
                            .Text("Productos Tomados").FontSize(12).Bold();
                        col.Item().PaddingTop(4).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(3);
                                c.RelativeColumn(1);
                                c.RelativeColumn(2);
                                c.RelativeColumn(2);
                                c.RelativeColumn(2);
                            });
                            table.Header(header =>
                            {
                                foreach (var title in new[]
                                    { "Producto", "Cant.", "Precio AS", "Total", "Factura" })
                                {
                                    header.Cell().Background(_reportStyle.ColorPrincipal)
                                        .Padding(4).Text(title)
                                        .FontColor(Colors.White).Bold().FontSize(9);
                                }
                            });
                            foreach (var sale in sales)
                            {
                                var bg = sales.IndexOf(sale) % 2 == 0
                                    ? "#FFFFFF" : _reportStyle.ColorFondoSuave;
                                table.Cell().Background(bg).Padding(4)
                                    .Text(sale.Product?.Name ?? "").FontSize(9);
                                table.Cell().Background(bg).Padding(4)
                                    .Text(sale.Quantity.ToString()).FontSize(9);
                                table.Cell().Background(bg).Padding(4)
                                    .Text($"${sale.PartnerPrice:N0}").FontSize(9);
                                table.Cell().Background(bg).Padding(4)
                                    .Text($"${sale.PartnerPrice * sale.Quantity:N0}").FontSize(9);
                                table.Cell().Background(bg).Padding(4)
                                    .Text(sale.Sale?.SaleNumber ?? "").FontSize(9);
                            }
                        });

                        // Mis ganancias
                        col.Item().PaddingTop(12)
                            .Text("Mis Ganancias Estimadas").FontSize(12).Bold();
                        col.Item().PaddingTop(4).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(3);
                                c.RelativeColumn(2);
                                c.RelativeColumn(2);
                                c.RelativeColumn(2);
                            });
                            table.Header(header =>
                            {
                                foreach (var title in new[]
                                    { "Producto", "Precio AS", "Precio Sugerido", "Ganancia Est." })
                                {
                                    header.Cell().Background(_reportStyle.ColorPrincipal)
                                        .Padding(4).Text(title)
                                        .FontColor(Colors.White).Bold().FontSize(9);
                                }
                            });
                            foreach (var sale in sales)
                            {
                                var bg = sales.IndexOf(sale) % 2 == 0
                                    ? "#FFFFFF" : _reportStyle.ColorFondoSuave;
                                table.Cell().Background(bg).Padding(4)
                                    .Text(sale.Product?.Name ?? "").FontSize(9);
                                table.Cell().Background(bg).Padding(4)
                                    .Text($"${sale.PartnerPrice:N0}").FontSize(9);
                                table.Cell().Background(bg).Padding(4)
                                    .Text($"${sale.SalePrice:N0}").FontSize(9);
                                table.Cell().Background(bg).Padding(4)
                                    .Text($"${sale.PartnerEarning:N0}")
                                    .FontSize(9).FontColor(_reportStyle.ColorExito);
                            }
                        });

                        // Abonos
                        if (liquidations.Any())
                        {
                            col.Item().PaddingTop(12)
                                .Text("Abonos Registrados").FontSize(12).Bold();
                            col.Item().PaddingTop(4).Table(table =>
                            {
                                table.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn(2);
                                    c.RelativeColumn(2);
                                    c.RelativeColumn(3);
                                });
                                table.Header(header =>
                                {
                                    foreach (var title in new[] { "Fecha", "Monto", "Notas" })
                                    {
                                        header.Cell().Background(_reportStyle.ColorPrincipal)
                                            .Padding(4).Text(title)
                                            .FontColor(Colors.White).Bold().FontSize(9);
                                    }
                                });
                                foreach (var liq in liquidations)
                                {
                                    var bg = liquidations.IndexOf(liq) % 2 == 0
                                        ? "#FFFFFF" : _reportStyle.ColorFondoSuave;
                                    table.Cell().Background(bg).Padding(4)
                                        .Text(liq.Date.ToString("dd/MM/yyyy")).FontSize(9);
                                    table.Cell().Background(bg).Padding(4)
                                        .Text($"${liq.Amount:N0}")
                                        .FontSize(9).FontColor(_reportStyle.ColorExito);
                                    table.Cell().Background(bg).Padding(4)
                                        .Text(liq.Notes ?? "-").FontSize(9);
                                }
                            });
                        }

                        // Nota
                        col.Item().PaddingTop(12)
                            .Border(1).BorderColor(_reportStyle.ColorAcento)
                            .Padding(6).Column(noteCol =>
                            {
                                noteCol.Item().Text("NOTA:")
                                    .Bold().FontSize(8).FontColor(_reportStyle.ColorPrincipal);
                                noteCol.Item().Text(
                                    "Las ganancias mostradas son estimadas basadas en el precio " +
                                    "sugerido de venta. El valor real depende del precio al que " +
                                    "cada producto fue vendido por la socia.")
                                    .FontSize(8).FontColor(_reportStyle.ColorPrincipal).Italic();
                            });
                    });

                    page.Footer().Column(col =>
                    {
                        col.Item().LineHorizontal(1).LineColor(_reportStyle.ColorAcento);
                        col.Item().PaddingTop(4).AlignCenter()
                            .Text($"AS Accesorios — Generado: {DateTime.Now:dd/MM/yyyy HH:mm}")
                            .FontSize(8).FontColor(_reportStyle.ColorAcento).Italic();
                    });
                });
            });

            return document.GeneratePdf();
        }

        public async Task<AllianceReportDto> GetAllianceReportAsync(DateTime from, DateTime to)
        {
            // Stock actual de productos de alianza
            var stock = await _context.Products
                .Where(p => p.IsActive && p.IsPartnership)
                .Select(p => new AllianceStockItemDto
                {
                    ProductName = p.Name,
                    Code = p.Code,
                    CurrentStock = p.Stock
                })
                .ToListAsync();

            // Ventas de productos de alianza en el período (todas, sin importar el vendedor)
            var saleDetails = await _context.SaleDetails
                .Include(sd => sd.Product)
                .Include(sd => sd.Sale)
                    .ThenInclude(s => s!.User)
                .Where(sd => sd.Product!.IsPartnership &&
                    sd.Sale!.Date >= from && sd.Sale.Date <= to && sd.Sale.IsActive)
                .ToListAsync();

            // IDs de SaleDetail que corresponden a ventas hechas por una socia
            var partnerSaleIds = await _context.PartnerSales
                .Where(ps => ps.IsPartnership && ps.Date >= from && ps.Date <= to)
                .Select(ps => ps.SaleId)
                .ToListAsync();

            var sold = saleDetails.Select(sd => new AllianceSoldItemDto
            {
                Date = sd.Sale!.Date,
                SaleNumber = sd.Sale.SaleNumber,
                ProductName = sd.Product!.Name,
                Quantity = sd.Quantity,
                UnitPrice = sd.UnitPrice,
                Total = sd.Subtotal,
                SellerName = sd.Sale.User?.FullName ?? "",
                SoldByPartner = partnerSaleIds.Contains(sd.SaleId)
            }).OrderByDescending(s => s.Date).ToList();

            return new AllianceReportDto
            {
                Stock = stock,
                Sold = sold,
                TotalSoldAmount = sold.Sum(s => s.Total),
                TotalSoldUnits = sold.Sum(s => s.Quantity)
            };
        }
    }
}
