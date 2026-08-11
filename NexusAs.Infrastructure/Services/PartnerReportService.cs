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

            // Ventas del período (para el detalle)
            var sales = await _context.PartnerSales
                .Include(ps => ps.Product)
                .Include(ps => ps.Sale)
                .Where(ps => ps.PartnerConfigId == partnerConfigId
                    && ps.Date >= from && ps.Date <= to && ps.IsActive)
                .OrderByDescending(ps => ps.Date)
                .ToListAsync();

            // Liquidaciones del período (para el detalle)
            var liquidations = await _context.PartnerLiquidations
                .Where(pl => pl.PartnerConfigId == partnerConfigId
                    && pl.Date >= from && pl.Date <= to && pl.IsActive)
                .OrderBy(pl => pl.Date)
                .ToListAsync();

            // Totales GENERALES (sin filtro de fecha)
            var allSales = await _context.PartnerSales
                .Where(ps => ps.PartnerConfigId == partnerConfigId && ps.IsActive)
                .ToListAsync();
            
            var allLiquidations = await _context.PartnerLiquidations
                .Where(pl => pl.PartnerConfigId == partnerConfigId && pl.IsActive)
                .ToListAsync();

            var totalDebt = allSales.Sum(s => s.PartnerPrice * s.Quantity);
            var totalPaid = allLiquidations
                .Where(l => l.Type == LiquidationType.Payment)
                .Sum(l => l.Amount);
            var totalEarnings = sales.Sum(s => s.PartnerEarning);

            // Facturas con saldo pendiente (para la nueva sección)
            var salesWithPendingDebt = await _context.Sales
                .Include(s => s.Credit)
                .Where(s => s.UserId == config.UserId && s.IsActive)
                .Where(s => s.Credit == null || s.Credit.Status != CreditStatus.Paid)
                .OrderByDescending(s => s.Date)
                .ToListAsync();

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
                            });
                        });

                        // Facturas con saldo pendiente
                        col.Item().PaddingTop(12)
                            .Text("Facturas con saldo pendiente").FontSize(12).Bold();
                        
                        if (salesWithPendingDebt.Any())
                        {
                            col.Item().PaddingTop(4).Table(table =>
                            {
                                table.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn(2); // Factura
                                    c.RelativeColumn(2); // Fecha
                                    c.RelativeColumn(2); // Total
                                    c.RelativeColumn(2); // Abonado
                                    c.RelativeColumn(2); // Pendiente
                                });
                                table.Header(header =>
                                {
                                    foreach (var title in new[]
                                        { "Factura", "Fecha", "Total", "Abonado", "Pendiente" })
                                    {
                                        header.Cell().Background(_reportStyle.ColorPrincipal)
                                            .Padding(4).Text(title)
                                            .FontColor(Colors.White).Bold().FontSize(9);
                                    }
                                });
                                
                                decimal totalFacturas = 0;
                                decimal totalAbonado = 0;
                                decimal totalPendiente = 0;

                                foreach (var saleWithDebt in salesWithPendingDebt)
                                {
                                    var paidAmount = saleWithDebt.Credit?.PaidAmount ?? 0;
                                    var pendingAmount = saleWithDebt.Total - paidAmount;
                                    
                                    totalFacturas += saleWithDebt.Total;
                                    totalAbonado += paidAmount;
                                    totalPendiente += pendingAmount;

                                    var bg = salesWithPendingDebt.IndexOf(saleWithDebt) % 2 == 0
                                        ? "#FFFFFF" : _reportStyle.ColorFondoSuave;
                                    
                                    table.Cell().Background(bg).Padding(4)
                                        .Text(saleWithDebt.SaleNumber).FontSize(9);
                                    table.Cell().Background(bg).Padding(4)
                                        .Text(saleWithDebt.Date.ToString("dd/MM/yyyy")).FontSize(9);
                                    table.Cell().Background(bg).Padding(4)
                                        .Text($"${saleWithDebt.Total:N0}").FontSize(9);
                                    table.Cell().Background(bg).Padding(4)
                                        .Text($"${paidAmount:N0}")
                                        .FontSize(9).FontColor(_reportStyle.ColorExito);
                                    table.Cell().Background(bg).Padding(4)
                                        .Text($"${pendingAmount:N0}")
                                        .FontSize(9).FontColor(_reportStyle.ColorAlerta);
                                }

                                // Fila de totales
                                table.Cell().Background(_reportStyle.ColorPrincipal).Padding(4)
                                    .Text("TOTAL").FontColor(Colors.White).Bold().FontSize(9);
                                table.Cell().Background(_reportStyle.ColorPrincipal).Padding(4)
                                    .Text("").FontSize(9);
                                table.Cell().Background(_reportStyle.ColorPrincipal).Padding(4)
                                    .Text($"${totalFacturas:N0}").FontColor(Colors.White).Bold().FontSize(9);
                                table.Cell().Background(_reportStyle.ColorPrincipal).Padding(4)
                                    .Text($"${totalAbonado:N0}").FontColor(Colors.White).Bold().FontSize(9);
                                table.Cell().Background(_reportStyle.ColorPrincipal).Padding(4)
                                    .Text($"${totalPendiente:N0}").FontColor(Colors.White).Bold().FontSize(9);
                            });
                        }
                        else
                        {
                            col.Item().PaddingTop(4).Text("No hay facturas con saldo pendiente.")
                                .FontSize(10).Italic().FontColor(_reportStyle.ColorAcento);
                        }

                        // Productos tomados
                        col.Item().PaddingTop(12)
                            .Text("Productos Tomados (Período)").FontSize(12).Bold();
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
