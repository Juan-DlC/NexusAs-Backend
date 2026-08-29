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
                .Include(s => s.ProcessedByUser)  // BUG 1 FIX: Incluir quien procesó la venta
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
                                    // BUG 1 FIX: Mostrar quien procesó la venta, no la socia
                                    var vendedor = sale.ProcessedByUser?.FullName ?? sale.User?.FullName ?? "Sistema";
                                    left.Item().Text($"Vendedor: {vendedor}")
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
                                c.ConstantColumn(60);  // Código
                                c.RelativeColumn(3);   // Producto
                                c.RelativeColumn(1);   // Cant.
                                c.RelativeColumn(2);   // Precio Unit.
                                c.RelativeColumn(2);   // Subtotal
                            });

                            table.Header(header =>
                            {
                                foreach (var title in new[]
                                    { "Código", "Producto", "Cant.", "Precio Unit.", "Subtotal" })
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
                                    .Text(detail.Product?.Code ?? "").FontSize(9);
                                
                                table.Cell().Background(bg).Padding(4).Column(col =>
                                {
                                    col.Item().Text(detail.Product?.Name ?? "").FontSize(9);
                                    if (!string.IsNullOrEmpty(detail.Product?.Description))
                                        col.Item().Text(detail.Product.Description)
                                            .FontSize(7).Italic().FontColor("#888888");
                                });
                                
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
                                .Text(sale.PaymentMethodEntity?.Code == "CASH"
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

        public async Task<byte[]> GeneratePartnerSaleReceiptAsync(int saleId)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var sale = await _context.Sales
                .Include(s => s.User)
                .Include(s => s.PaymentMethodEntity)
                .Include(s => s.SaleDetails)
                    .ThenInclude(sd => sd.Product)
                .Include(s => s.Credit)
                .FirstOrDefaultAsync(s => s.Id == saleId);

            if (sale == null)
                throw new Exception($"Venta con id {saleId} no encontrada.");

            var seller = sale.User;

            // Cargar logo
            byte[]? logoBytes = null;
            if (File.Exists(_reportStyle.LogoPath))
                logoBytes = File.ReadAllBytes(_reportStyle.LogoPath);

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(35);
                    page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(9));

                    page.Header().Element(header =>
                    {
                        header.Row(row =>
                        {
                            // Logo
                            row.ConstantItem(70).Element(logo =>
                            {
                                if (logoBytes != null)
                                    logo.Image(logoBytes).FitArea();
                                else
                                    logo.Text("AS").Bold().FontSize(20).FontColor(_reportStyle.ColorPrincipal);
                            });

                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("AS ACCESORIOS").Bold().FontSize(14).FontColor(_reportStyle.ColorPrincipal);
                                col.Item().Text("FACTURA SOCIA VENDEDORA").Bold().FontSize(11).FontColor(_reportStyle.ColorAcento);
                                col.Item().Height(4);
                                col.Item().Text($"Factura: {sale.SaleNumber}").Bold();
                                col.Item().Text($"Fecha: {sale.Date:dd/MM/yyyy}");
                                col.Item().Text($"Socia: {seller?.FullName ?? "Sin nombre"}");
                                col.Item().Text($"Método de pago: {sale.PaymentMethodEntity?.Name ?? "Contado"}");
                                if (!string.IsNullOrEmpty(sale.Notes))
                                    col.Item().Text($"Notas: {sale.Notes}");
                            });
                        });
                    });

                    page.Content().PaddingTop(20).Element(content =>
                    {
                        content.Column(col =>
                        {
                            // Tabla de productos
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn(3);  // Producto
                                    c.ConstantColumn(40); // Cant.
                                    c.RelativeColumn(2);  // Te cuesta c/u
                                    c.RelativeColumn(2);  // Total a pagar
                                    c.RelativeColumn(2);  // Precio sugerido
                                });

                                // Header de tabla
                                static IContainer HeaderCell(IContainer container) =>
                                    container.Background("#2C3E50").Padding(5).AlignCenter();

                                table.Header(header =>
                                {
                                    header.Cell().Element(HeaderCell).Text("Producto").Bold().FontColor(Colors.White).FontSize(8);
                                    header.Cell().Element(HeaderCell).Text("Cant.").Bold().FontColor(Colors.White).FontSize(8);
                                    header.Cell().Element(HeaderCell).Text("Te cuesta c/u").Bold().FontColor(Colors.White).FontSize(8);
                                    header.Cell().Element(HeaderCell).Text("Total a pagar").Bold().FontColor(Colors.White).FontSize(8);
                                    header.Cell().Element(HeaderCell).Text("Precio sugerido").Bold().FontColor(Colors.White).FontSize(8);
                                });

                                // Filas de productos
                                bool alternate = false;
                                foreach (var detail in sale.SaleDetails)
                                {
                                    var bg = alternate ? _reportStyle.ColorFondoSuave : "#FFFFFF";
                                    alternate = !alternate;

                                    // CORRECCIÓN: detail.UnitPrice YA ES el precio que se le dio a la socia
                                    var partnerPrice = detail.UnitPrice;
                                    var suggestedPrice = detail.Product?.SalePrice ?? detail.UnitPrice;
                                    var totalToPay = partnerPrice * detail.Quantity;

                                    IContainer DataCell(IContainer c) =>
                                        c.Background(bg).Padding(5);

                                    table.Cell().Element(DataCell).Column(nameCol =>
                                    {
                                        nameCol.Item().Text(detail.Product?.Name ?? "-").FontSize(8);
                                        if (detail.Product?.IsPartnership == true)
                                            nameCol.Item().Text("Alianza").FontSize(7).FontColor(_reportStyle.ColorAcento);
                                    });

                                    table.Cell().Element(DataCell).AlignCenter().Text(detail.Quantity.ToString()).FontSize(8);
                                    table.Cell().Element(DataCell).AlignRight().Text($"${partnerPrice:N0}").FontSize(8).Bold().FontColor(_reportStyle.ColorPrincipal);
                                    table.Cell().Element(DataCell).AlignRight().Text($"${totalToPay:N0}").FontSize(8).Bold();
                                    table.Cell().Element(DataCell).AlignRight().Text($"${suggestedPrice:N0}").FontSize(8).FontColor(_reportStyle.ColorExito);
                                }
                            });

                            // Espacio
                            col.Item().Height(16);

                            // Resumen financiero
                            col.Item().Background(_reportStyle.ColorFondoSuave).Padding(12).Column(summary =>
                            {
                                summary.Item().Row(row =>
                                {
                                    row.RelativeItem().Text("TOTAL A PAGAR A AS:").Bold().FontSize(11);
                                    row.ConstantItem(120).AlignRight().Text($"${sale.Total:N0}").Bold().FontSize(13).FontColor(_reportStyle.ColorPrincipal);
                                });

                                // Mostrar crédito si existe
                                if (sale.Credit != null)
                                {
                                    col.Item().Height(8);
                                    summary.Item().Row(row =>
                                    {
                                        row.RelativeItem().Text("Abonado:").FontSize(9);
                                        row.ConstantItem(120).AlignRight().Text($"${sale.Credit.PaidAmount:N0}").FontSize(9).FontColor(_reportStyle.ColorExito);
                                    });
                                    summary.Item().Row(row =>
                                    {
                                        row.RelativeItem().Text("Saldo pendiente:").Bold().FontSize(10);
                                        row.ConstantItem(120).AlignRight().Text($"${sale.Credit.PendingAmount:N0}").Bold().FontSize(11).FontColor(_reportStyle.ColorAlerta);
                                    });
                                }
                            });

                            // Nota informativa
                            col.Item().Height(12);
                            col.Item().Background("#EFF8FF").Padding(10).Column(note =>
                            {
                                note.Item().Text("ℹ️ Información de precios").Bold().FontSize(8).FontColor(_reportStyle.ColorPrincipal);
                                note.Item().Height(4);
                                note.Item().Text("• Te cuesta c/u: Precio al que AS te entrega cada producto").FontSize(7).FontColor("#555555");
                                note.Item().Text("• Precio sugerido: Precio recomendado de venta al público").FontSize(7).FontColor("#555555");
                            });
                        });
                    });

                    page.Footer().AlignCenter().Text($"Documento generado el {DateTime.Now:dd/MM/yyyy HH:mm} — AS Accesorios")
                        .FontSize(7).FontColor(Colors.Grey.Medium);
                });
            });

            return document.GeneratePdf();
        }
    }
}
