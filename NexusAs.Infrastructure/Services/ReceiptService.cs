using Microsoft.EntityFrameworkCore;
using NexusAs.Application.Interfaces;
using NexusAs.Domain.Enums;
using NexusAs.Infrastructure.Data;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace NexusAs.Infrastructure.Services
{
    public class ReceiptService : IReceiptService
    {
        private readonly NexusAsDbContext _context;
        private readonly ReportStyleHelper _reportStyle;

        public ReceiptService(NexusAsDbContext context, ReportStyleHelper reportStyle)
        {
            _context = context;
            _reportStyle = reportStyle;
        }

        public async Task<byte[]> GenerateSaleReceiptAsync(int saleId)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var sale = await _context.Sales
                .Include(s => s.Customer)
                .Include(s => s.User)
                .Include(s => s.SaleDetails)
                    .ThenInclude(sd => sd.Product)
                .Include(s => s.Credit)
                .Include(s => s.Credit)
                    .ThenInclude(c => c!.Installments)
                .FirstOrDefaultAsync(s => s.Id == saleId);

            if (sale == null)
                throw new Exception($"Venta con id {saleId} no encontrada.");

            var logoBytes = File.Exists(_reportStyle.LogoPath)
                ? File.ReadAllBytes(_reportStyle.LogoPath) : null;

            var document = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A5);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(10).FontColor(_reportStyle.ColorPrincipal));

                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            if (logoBytes != null)
                                row.ConstantItem(60).Image(logoBytes).FitArea();

                            row.RelativeItem().Column(titleCol =>
                            {
                                titleCol.Item().PaddingLeft(8)
                                    .Text("AS Accesorios")
                                    .FontSize(16).Bold().FontColor(_reportStyle.ColorPrincipal);
                                titleCol.Item().PaddingLeft(8)
                                    .Text("Tenis • Cuidado de Piel • y más")
                                    .FontSize(8).FontColor(_reportStyle.ColorAcento).Italic();
                                titleCol.Item().PaddingLeft(8).PaddingTop(2)
                                    .Text("Recibo de Venta")
                                    .FontSize(11).Bold().FontColor(_reportStyle.ColorAcento);
                            });
                        });

                        col.Item().PaddingTop(6)
                            .LineHorizontal(2).LineColor(_reportStyle.ColorAcento);

                        col.Item().PaddingTop(6)
                            .Background(_reportStyle.ColorFondoSuave).Padding(6)
                            .Row(row =>
                            {
                                row.RelativeItem().Column(left =>
                                {
                                    left.Item().Text($"Factura: {sale.SaleNumber}")
                                        .Bold().FontSize(11);
                                    left.Item().Text($"Fecha: {sale.Date:dd/MM/yyyy HH:mm}")
                                        .FontSize(9);
                                    left.Item().Text($"Vendedor: {sale.User?.FullName ?? ""}")
                                        .FontSize(9);
                                });

                                if (sale.Customer != null)
                                {
                                    row.RelativeItem().Column(right =>
                                    {
                                        right.Item().Text("Cliente:").Bold().FontSize(9);
                                        right.Item().Text(sale.Customer.Name).FontSize(9);
                                        if (!string.IsNullOrEmpty(sale.Customer.Phone))
                                            right.Item()
                                                .Text($"Tel: {sale.Customer.Phone}")
                                                .FontSize(9);
                                        if (!string.IsNullOrEmpty(sale.Customer.Document))
                                            right.Item()
                                                .Text($"Doc: {sale.Customer.Document}")
                                                .FontSize(9);
                                    });
                                }
                            });

                        col.Item().PaddingTop(4)
                            .LineHorizontal(1).LineColor(_reportStyle.ColorAcento);
                    });

                    page.Content().PaddingTop(8).Column(col =>
                    {
                        col.Item().Text("Detalle de Productos")
                            .FontSize(11).Bold().FontColor(_reportStyle.ColorPrincipal);

                        col.Item().PaddingTop(4).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(3);
                                c.RelativeColumn(1);
                                c.RelativeColumn(2);
                                c.RelativeColumn(2);
                            });

                            table.Header(header =>
                            {
                                foreach (var title in new[]
                                    { "Producto", "Cant.", "Precio Unit.", "Subtotal" })
                                {
                                    header.Cell().Background(_reportStyle.ColorPrincipal)
                                        .Padding(4).Text(title)
                                        .FontColor(Colors.White).Bold().FontSize(9);
                                }
                            });

                            var details = sale.SaleDetails.ToList();
                            foreach (var detail in details)
                            {
                                var bg = details.IndexOf(detail) % 2 == 0
                                     ? "#FFFFFF" : _reportStyle.ColorFondoSuave;
                                table.Cell().Background(bg).Padding(4)
                                    .Text(detail.Product?.Name ?? "").FontSize(9);
                                table.Cell().Background(bg).Padding(4)
                                    .Text(detail.Quantity.ToString()).FontSize(9);
                                table.Cell().Background(bg).Padding(4)
                                    .Text($"${detail.UnitPrice:N0}").FontSize(9);
                                table.Cell().Background(bg).Padding(4)
                                    .Text($"${detail.Subtotal:N0}").FontSize(9);
                            }
                        });

                        col.Item().PaddingTop(8).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(3);
                                c.RelativeColumn(2);
                            });

                            table.Cell().Text("Subtotal:").Bold();
                            table.Cell().AlignRight().Text($"${sale.Subtotal:N0}");

                            if (sale.Discount > 0)
                            {
                                table.Cell().Text("Descuento:")
                                    .Bold().FontColor(_reportStyle.ColorExito);
                                table.Cell().AlignRight()
                                    .Text($"-${sale.Discount:N0}")
                                    .FontColor(_reportStyle.ColorExito);
                            }

                            table.Cell().Text("TOTAL:").Bold().FontSize(13);
                            table.Cell().AlignRight()
                                .Text($"${sale.Total:N0}")
                                .Bold().FontSize(13).FontColor(_reportStyle.ColorAcento);

                            table.Cell().Text("Método de pago:").Bold();
                            table.Cell().AlignRight()
                                .Text(sale.PaymentMethod == PaymentMethod.Cash
                                    ? "Contado ✓" : "Crédito");
                        });

                        if (sale.Credit != null)
                        {
                            col.Item().PaddingTop(10)
                                .Border(1).BorderColor(_reportStyle.ColorAcento)
                                .Padding(8).Column(creditCol =>
            {
                                    creditCol.Item()
                                        .Text("Estado del Crédito")
                                        .Bold().FontSize(11).FontColor(_reportStyle.ColorAcento);

                                    creditCol.Item().PaddingTop(4).Table(t =>
                                    {
                                        t.ColumnsDefinition(c =>
                                        {
                                            c.RelativeColumn(2);
                                            c.RelativeColumn(2);
                                        });

                                        t.Cell().Text("Valor total:").Bold();
                                        t.Cell().AlignRight()
                                            .Text($"${sale.Credit.TotalAmount:N0}");

                                        t.Cell().Text("Total abonado:")
                                            .Bold().FontColor(_reportStyle.ColorExito);
                                        t.Cell().AlignRight()
                                            .Text($"${sale.Credit.PaidAmount:N0}")
                                            .FontColor(_reportStyle.ColorExito);

                                        t.Cell().Text("Saldo pendiente:")
                                            .Bold().FontColor(_reportStyle.ColorAlerta);
                                        t.Cell().AlignRight()
                                            .Text($"${sale.Credit.PendingAmount:N0}")
                                            .Bold().FontColor(_reportStyle.ColorAlerta);

                                        t.Cell().Text("Estado:").Bold();
                                        t.Cell().AlignRight().Text(
                                            sale.Credit.Status == CreditStatus.Paid
                                                ? "✓ PAGADO COMPLETAMENTE" :
                                            sale.Credit.Status == CreditStatus.Partial
                                                ? "Pago parcial" : "Pendiente de pago");
                                    });

                                    if (sale.Credit.NumberOfInstallments > 1)
                                    {
                                        creditCol.Item().PaddingTop(8)
                                            .Text($"Plan de pago — {sale.Credit.NumberOfInstallments} cuotas")
                                            .Bold().FontSize(9).FontColor(_reportStyle.ColorPrincipal);

                                        creditCol.Item().PaddingTop(4).Table(it =>
                                        {
                                            it.ColumnsDefinition(c =>
                                            {
                                                c.RelativeColumn(1);
                                                c.RelativeColumn(2);
                                                c.RelativeColumn(2);
                                            });

                                            it.Header(header =>
                                            {
                                                foreach (var title in new[] { "Cuota", "Valor", "Estado" })
                                                {
                                                    header.Cell().Background(_reportStyle.ColorPrincipal)
                                                        .Padding(3).Text(title)
                                                        .FontColor(Colors.White).Bold().FontSize(8);
                                                }
                                            });

                                            var installments = sale.Credit.Installments
                                                .OrderBy(i => i.Number).ToList();
                                            foreach (var inst in installments)
                                            {
                                                var bg = installments.IndexOf(inst) % 2 == 0
                                                    ? "#FFFFFF" : _reportStyle.ColorFondoSuave;
                                                it.Cell().Background(bg).Padding(3)
                                                    .Text($"#{inst.Number}").FontSize(8);
                                                it.Cell().Background(bg).Padding(3)
                                                    .Text($"${inst.Amount:N0}").FontSize(8);
                                                it.Cell().Background(bg).Padding(3)
                                                    .Text(inst.IsPaid ? "Pagada" : "Pendiente")
                                                    .FontSize(8)
                                                    .FontColor(inst.IsPaid ? _reportStyle.ColorExito : _reportStyle.ColorAlerta);
                                            }
                                        });
                                    }
                                });
                        }

                        col.Item().PaddingTop(10)
                            .Border(1).BorderColor(_reportStyle.ColorAcento)
                            .Padding(6).Column(noteCol =>
                            {
                                noteCol.Item().Text("NOTA IMPORTANTE:")
                                    .Bold().FontSize(8).FontColor(_reportStyle.ColorPrincipal);
                                noteCol.Item().Text(
                                    "Este documento es un recibo interno de AS Accesorios " +
                                    "y NO constituye factura electrónica de venta ante la DIAN. " +
                                    "No tiene validez tributaria ni fiscal. Para facturación " +
                                    "electrónica oficial, consulte con su contador.")
                                    .FontSize(8).FontColor(_reportStyle.ColorPrincipal).Italic();
                            });
                    });

                    page.Footer().Column(col =>
                    {
                        col.Item().LineHorizontal(1).LineColor(_reportStyle.ColorAcento);
                        col.Item().PaddingTop(4).AlignCenter()
                            .Text($"AS Accesorios — {DateTime.Now:dd/MM/yyyy HH:mm} — ¡Gracias por su compra!")
                            .FontSize(8).FontColor(_reportStyle.ColorAcento).Italic();
                    });
                });
            });

            return document.GeneratePdf();
        }
    }
}
