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

            // BUG 4 FIX: Calcular deuda SOLO de créditos pendientes (no todas las ventas)
            var salesWithDebt = await _context.Sales
                .Include(s => s.Credit)
                .Include(s => s.PaymentMethodEntity)
                .Where(s => s.UserId == config.UserId && s.IsActive
                    && s.Credit != null && s.Credit.Status != CreditStatus.Paid)
                .OrderByDescending(s => s.Date)
                .ToListAsync();

            decimal totalDebt = salesWithDebt
                .Where(s => s.Credit != null)
                .Sum(s => s.Credit!.TotalAmount);
            decimal totalPaid = salesWithDebt
                .Where(s => s.Credit != null)
                .Sum(s => s.Credit!.PaidAmount);
            decimal pendingDebt = totalDebt - totalPaid;

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
                            // Logo alineado verticalmente
                            if (logoBytes != null)
                                row.ConstantItem(100).AlignMiddle().Image(logoBytes).FitArea();
                            
                            row.RelativeItem().PaddingLeft(12).AlignMiddle().Column(titleCol =>
                            {
                                titleCol.Item()
                                    .Text("ESTADO DE CUENTA — SOCIA")
                                    .FontSize(11).Bold().FontColor(_reportStyle.ColorTextoSecundario);
                                titleCol.Item().PaddingTop(3)
                                    .Text($"Socia: {config.User?.FullName ?? ""}")
                                    .FontSize(12).Bold().FontColor(_reportStyle.ColorPrincipal);
                                titleCol.Item().PaddingTop(1)
                                    .Text($"Período: {from:dd/MM/yyyy} — {to:dd/MM/yyyy}")
                                    .FontSize(9).FontColor(_reportStyle.ColorTextoSecundario);
                            });
                        });
                        col.Item().PaddingTop(6).LineHorizontal(1).LineColor(_reportStyle.ColorAcento);
                    });

                    page.Content().PaddingTop(8).Column(col =>
                    {
                        // Resumen solo con deuda de créditos
                        col.Item().Background(_reportStyle.ColorFilasAlternas).Padding(6).Column(res =>
                        {
                            res.Item().Text("Resumen de Deuda").FontSize(11).Bold().FontColor(_reportStyle.ColorPrincipal);
                            res.Item().PaddingTop(3).Table(table =>
                            {
                                table.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn();
                                    c.RelativeColumn();
                                });
                                table.Cell().Text("Total deuda (créditos):").Bold().FontSize(9);
                                table.Cell().AlignRight().Text($"${totalDebt:N0}").FontSize(9);
                                table.Cell().Text("Total abonado:").Bold().FontSize(9).FontColor(_reportStyle.ColorExito);
                                table.Cell().AlignRight()
                                    .Text($"${totalPaid:N0}").FontSize(9).FontColor(_reportStyle.ColorExito);
                                table.Cell().Text("Saldo pendiente:").Bold().FontSize(10).FontColor(_reportStyle.ColorAlerta);
                                table.Cell().AlignRight()
                                    .Text($"${pendingDebt:N0}").Bold().FontSize(10).FontColor(_reportStyle.ColorAlerta);
                            });
                        });

                        // Facturas con saldo pendiente - tabla mejorada
                        col.Item().PaddingTop(8)
                            .Text("Facturas con Saldo Pendiente").FontSize(11).Bold();
                        
                        if (salesWithDebt.Any())
                        {
                            col.Item().PaddingTop(4).Table(table =>
                            {
                                table.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn(2); // Factura
                                    c.RelativeColumn(2); // Fecha
                                    c.RelativeColumn(2); // Método
                                    c.RelativeColumn(2); // Total
                                    c.RelativeColumn(2); // Abonado
                                    c.RelativeColumn(2); // Pendiente
                                });
                                table.Header(header =>
                                {
                                    foreach (var title in new[]
                                        { "Factura", "Fecha", "Método", "Total", "Abonado", "Pendiente" })
                                    {
                                        header.Cell().Background(_reportStyle.ColorPrincipal)
                                            .Padding(4).Text(title)
                                            .FontColor(Colors.White).Bold().FontSize(9);
                                    }
                                });
                                
                                decimal totalFacturas = 0;
                                decimal totalAbonado = 0;
                                decimal totalPendiente = 0;

                                foreach (var saleWithDebt in salesWithDebt)
                                {
                                    var paidAmount = saleWithDebt.Credit?.PaidAmount ?? 0;
                                    var pendingAmount = saleWithDebt.Credit?.PendingAmount ?? 0;
                                    
                                    totalFacturas += saleWithDebt.Total;
                                    totalAbonado += paidAmount;
                                    totalPendiente += pendingAmount;

                                    var bg = salesWithDebt.IndexOf(saleWithDebt) % 2 == 0
                                        ? "#FFFFFF" : _reportStyle.ColorFondoSuave;
                                    
                                    table.Cell().Background(bg).Padding(4)
                                        .Text(saleWithDebt.SaleNumber).FontSize(9);
                                    table.Cell().Background(bg).Padding(4)
                                        .Text(saleWithDebt.Date.ToString("dd/MM/yyyy")).FontSize(9);
                                    table.Cell().Background(bg).Padding(4)
                                        .Text(saleWithDebt.PaymentMethodEntity?.Name ?? "Crédito").FontSize(9);
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
                                table.Cell().Background(_reportStyle.ColorPrincipal).Padding(4).Text("");
                                table.Cell().Background(_reportStyle.ColorPrincipal).Padding(4).Text("");
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

                        // Productos tomados (del período)
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
                                .Text("Abonos Registrados (Período)").FontSize(12).Bold();
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
