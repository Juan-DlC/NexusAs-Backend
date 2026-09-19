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
                .Include(s => s.PaymentMethodEntity)  // FIX: Incluir método de pago para el PDF
                .Include(s => s.SaleDetails)
                    .ThenInclude(sd => sd.Product)
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
                    page.Margin(1, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(10).FontColor(_reportStyle.ColorPrincipal));

                    page.Header().Column(col =>
                    {
                        // Encabezado compacto: Logo + Detalles de Factura
                        col.Item().Row(row =>
                        {
                            // Logo más pequeño
                            if (logoBytes != null)
                            {
                                row.ConstantItem(70).AlignMiddle().Image(logoBytes).FitArea();
                            }

                            row.RelativeItem().PaddingLeft(10).AlignMiddle().Column(infoCol =>
                            {
                                infoCol.Item().Text("RECIBO DE VENTA")
                                    .FontSize(8).Bold().FontColor(_reportStyle.ColorTextoSecundario);
                                
                                infoCol.Item().Text($"Factura: {sale.SaleNumber}")
                                    .Bold().FontSize(8).FontColor(_reportStyle.ColorPrincipal);
                                
                                infoCol.Item().Text($"Fecha: {sale.Date:dd/MM/yyyy HH:mm}")
                                    .FontSize(8).FontColor(_reportStyle.ColorTextoSecundario);
                                
                                var vendedor = sale.ProcessedByUser?.FullName ?? sale.User?.FullName ?? "Sistema";
                                infoCol.Item().Text($"Vendedor: {vendedor}")
                                    .FontSize(8).FontColor(_reportStyle.ColorTextoSecundario);
                            });
                        });

                        // Línea separadora más delgada
                        col.Item().PaddingTop(3).PaddingBottom(3)
                            .LineHorizontal(0.5f).LineColor(_reportStyle.ColorAcento);

                        // Información del cliente EN LÍNEA HORIZONTAL (si existe)
                        if (sale.Customer != null)
                        {
                            col.Item().PaddingBottom(3).Background("#F5F5F5").Padding(4).Row(row =>
                            {
                                row.AutoItem().Text("CLIENTE: ")
                                    .FontSize(8).Bold().FontColor(_reportStyle.ColorTextoSecundario);
                                
                                row.AutoItem().PaddingLeft(3).Text(sale.Customer.Name)
                                    .FontSize(8).FontColor(_reportStyle.ColorPrincipal);
                                
                                if (!string.IsNullOrEmpty(sale.Customer.Document))
                                {
                                    row.AutoItem().PaddingLeft(8).Text("•")
                                        .FontSize(8).FontColor(_reportStyle.ColorTextoSecundario);
                                    row.AutoItem().PaddingLeft(3).Text($"Doc: {sale.Customer.Document}")
                                        .FontSize(7).FontColor(_reportStyle.ColorTextoSecundario);
                                }
                                
                                if (!string.IsNullOrEmpty(sale.Customer.Phone))
                                {
                                    row.AutoItem().PaddingLeft(8).Text("•")
                                        .FontSize(8).FontColor(_reportStyle.ColorTextoSecundario);
                                    row.AutoItem().PaddingLeft(3).Text($"Tel: {sale.Customer.Phone}")
                                        .FontSize(7).FontColor(_reportStyle.ColorTextoSecundario);
                                }
                            });
                            
                            col.Item().PaddingBottom(2)
                                .LineHorizontal(0.5f).LineColor(_reportStyle.ColorBordes);
                        }
                    });

                    page.Content().Column(col =>
                    {
                        col.Item().Text("DETALLE DE PRODUCTOS")
                            .FontSize(9).Bold().FontColor(_reportStyle.ColorPrincipal);

                        col.Item().PaddingTop(2).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(55);  // Código más compacto
                                c.RelativeColumn(3);   // Producto
                                c.ConstantColumn(30);  // Cant.
                                c.RelativeColumn(2);   // Precio Unit.
                                c.RelativeColumn(2);   // Subtotal
                            });

                            table.Header(header =>
                            {
                                foreach (var title in new[]
                                    { "Código", "Producto", "Cant.", "Precio Unit.", "Subtotal" })
                                {
                                    header.Cell().Background(_reportStyle.ColorTablaHeader)
                                        .Padding(4).Text(title)
                                        .FontColor(Colors.White).Bold().FontSize(8);
                                }
                            });

                            var details = sale.SaleDetails.ToList();
                            foreach (var detail in details)
                            {
                                var bg = details.IndexOf(detail) % 2 == 0
                                     ? "#FFFFFF" : _reportStyle.ColorFilasAlternas;
                                
                                table.Cell().Background(bg).Padding(4)
                                    .Text(detail.Product?.Code ?? "").FontSize(7);
                                
                                table.Cell().Background(bg).Padding(4).Column(col =>
                                {
                                    col.Item().Text(detail.Product?.Name ?? "").FontSize(8);
                                    if (!string.IsNullOrEmpty(detail.Product?.Description))
                                        col.Item().Text(detail.Product.Description)
                                            .FontSize(6).Italic().FontColor(_reportStyle.ColorTextoSecundario);
                                });
                                
                                table.Cell().Background(bg).Padding(4).AlignCenter()
                                    .Text(detail.Quantity.ToString()).FontSize(8);
                                table.Cell().Background(bg).Padding(4).AlignRight()
                                    .Text($"${detail.UnitPrice:N0}").FontSize(8);
                                table.Cell().Background(bg).Padding(4).AlignRight()
                                    .Text($"${detail.Subtotal:N0}").FontSize(8).Bold();
                            }
                        });

                        // Notas de la venta (si existen) - Entre detalle y crédito
                        if (!string.IsNullOrEmpty(sale.Notes))
                        {
                            col.Item().PaddingTop(3)
                                .Border(1).BorderColor(_reportStyle.ColorBordes)
                                .Padding(4).Row(row =>
                                {
                                    row.AutoItem().PaddingRight(4).AlignTop()
                                        .Text("📝").FontSize(9);
                                    
                                    row.RelativeItem().Column(noteContent =>
                                    {
                                        noteContent.Item().Text("Nota")
                                            .Bold().FontSize(8).FontColor(_reportStyle.ColorPrincipal);
                                        noteContent.Item().PaddingTop(1).Text(sale.Notes)
                                            .FontSize(7).FontColor(_reportStyle.ColorTextoSecundario);
                                    });
                                });
                        }

                        // Estado del crédito (si aplica)
                        if (sale.Credit != null)
                        {
                            col.Item().PaddingTop(4)
                                .Border(1).BorderColor(_reportStyle.ColorBordes)
                                .Padding(5).Column(creditCol =>
                                {
                                    creditCol.Item()
                                        .Text("ESTADO DEL CRÉDITO")
                                        .Bold().FontSize(9).FontColor(_reportStyle.ColorPrincipal);

                                    creditCol.Item().PaddingTop(2).Table(t =>
                                    {
                                        t.ColumnsDefinition(c =>
                                        {
                                            c.RelativeColumn(2);
                                            c.RelativeColumn(2);
                                        });

                                        t.Cell().Text("Valor total:").FontSize(8).FontColor(_reportStyle.ColorTextoSecundario);
                                        t.Cell().AlignRight()
                                            .Text($"${sale.Credit.TotalAmount:N0}").FontSize(8);

                                        t.Cell().Text("Total abonado:")
                                            .FontSize(8).FontColor(_reportStyle.ColorExito);
                                        t.Cell().AlignRight()
                                            .Text($"${sale.Credit.PaidAmount:N0}")
                                            .FontSize(8).FontColor(_reportStyle.ColorExito);

                                        t.Cell().Text("Saldo pendiente:")
                                            .Bold().FontSize(9).FontColor(_reportStyle.ColorAlerta);
                                        t.Cell().AlignRight()
                                            .Text($"${sale.Credit.PendingAmount:N0}")
                                            .Bold().FontSize(9).FontColor(_reportStyle.ColorAlerta);

                                        t.Cell().PaddingTop(1).Text("Estado:").FontSize(8).FontColor(_reportStyle.ColorTextoSecundario);
                                        t.Cell().PaddingTop(1).AlignRight().Text(
                                            sale.Credit.Status == CreditStatus.Paid
                                                ? "PAGADO" :
                                            sale.Credit.Status == CreditStatus.Partial
                                                ? "Pago parcial" : "Pendiente")
                                            .FontSize(8).Bold();
                                    });

                                    if (sale.Credit.NumberOfInstallments > 1)
                                    {
                                        creditCol.Item().PaddingTop(3)
                                            .Text($"Plan de pago — {sale.Credit.NumberOfInstallments} cuotas")
                                            .Bold().FontSize(8).FontColor(_reportStyle.ColorPrincipal);

                                        creditCol.Item().PaddingTop(2).Table(it =>
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
                                                    header.Cell().Background(_reportStyle.ColorTablaHeader)
                                                        .Padding(3).Text(title)
                                                        .FontColor(Colors.White).Bold().FontSize(7);
                                                }
                                            });

                                            var installments = sale.Credit.Installments
                                                .OrderBy(i => i.Number).ToList();
                                            foreach (var inst in installments)
                                            {
                                                var bg = installments.IndexOf(inst) % 2 == 0
                                                    ? "#FFFFFF" : _reportStyle.ColorFilasAlternas;
                                                it.Cell().Background(bg).Padding(3)
                                                    .Text($"#{inst.Number}").FontSize(7);
                                                it.Cell().Background(bg).Padding(3).AlignRight()
                                                    .Text($"${inst.Amount:N0}").FontSize(7);
                                                it.Cell().Background(bg).Padding(3).AlignCenter()
                                                    .Text(inst.IsPaid ? "Pagada" : "Pendiente")
                                                    .FontSize(7)
                                                    .FontColor(inst.IsPaid ? _reportStyle.ColorExito : _reportStyle.ColorTextoSecundario);
                                            }
                                        });
                                    }
                                });
                        }

                        // Totales y método de pago
                        col.Item().PaddingTop(4).PaddingBottom(3)
                            .BorderTop(1).BorderColor(_reportStyle.ColorBordes)
                            .PaddingTop(3).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(3);
                                c.RelativeColumn(2);
                            });

                            table.Cell().Text("Subtotal:").FontSize(9).FontColor(_reportStyle.ColorTextoSecundario);
                            table.Cell().AlignRight().Text($"${sale.Subtotal:N0}").FontSize(9);

                            if (sale.Discount > 0)
                            {
                                table.Cell().Text("Descuento:")
                                    .FontSize(9).FontColor(_reportStyle.ColorExito);
                                table.Cell().AlignRight()
                                    .Text($"-${sale.Discount:N0}")
                                    .FontSize(9).FontColor(_reportStyle.ColorExito);
                            }

                            table.Cell().PaddingTop(2).Text("TOTAL:").Bold().FontSize(11).FontColor(_reportStyle.ColorPrincipal);
                            table.Cell().PaddingTop(2).AlignRight()
                                .Text($"${sale.Total:N0}")
                                .Bold().FontSize(11).FontColor(_reportStyle.ColorPrincipal);

                            table.Cell().PaddingTop(3).Text("Método de pago:").FontSize(8).FontColor(_reportStyle.ColorTextoSecundario);
                            table.Cell().PaddingTop(3).AlignRight()
                                .Text(sale.PaymentMethodEntity?.Code == "CASH"
                                    ? "Contado" : "Crédito")
                                .FontSize(8).FontColor(_reportStyle.ColorTextoSecundario);
                        });

                        col.Item().PaddingTop(4)
                            .Border(1).BorderColor(_reportStyle.ColorBordes)
                            .Padding(5).Column(noteCol =>
                            {
                                noteCol.Item().Text("NOTA IMPORTANTE")
                                    .Bold().FontSize(7).FontColor(_reportStyle.ColorPrincipal);
                                noteCol.Item().PaddingTop(2).Text(
                                    "Este documento es un recibo interno y NO constituye factura electrónica de venta ante la DIAN. " +
                                    "No tiene validez tributaria ni fiscal. Para facturación electrónica oficial, consulte con su contador.")
                                    .FontSize(6).FontColor(_reportStyle.ColorTextoSecundario);
                            });
                    });

                    // Footer removido - no mostrar fecha de generación
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
                        header.Column(col =>
                        {
                            col.Item().Row(row =>
                            {
                                // Logo alineado verticalmente
                                if (logoBytes != null)
                                {
                                    row.ConstantItem(100).AlignMiddle().Image(logoBytes).FitArea();
                                }

                                row.RelativeItem().PaddingLeft(12).AlignMiddle().Column(infoCol =>
                                {
                                    infoCol.Item().Text("FACTURA SOCIA VENDEDORA")
                                        .FontSize(10).Bold().FontColor(_reportStyle.ColorTextoSecundario);
                                    
                                    infoCol.Item().PaddingTop(3).Text($"Factura: {sale.SaleNumber}")
                                        .Bold().FontSize(12).FontColor(_reportStyle.ColorPrincipal);
                                    
                                    infoCol.Item().PaddingTop(1).Text($"Fecha: {sale.Date:dd/MM/yyyy}")
                                        .FontSize(9).FontColor(_reportStyle.ColorTextoSecundario);
                                    
                                    infoCol.Item().Text($"Socia: {seller?.FullName ?? "Sin nombre"}")
                                        .FontSize(9).FontColor(_reportStyle.ColorTextoSecundario);
                                    
                                    infoCol.Item().Text($"Método de pago: {sale.PaymentMethodEntity?.Name ?? "Contado"}")
                                        .FontSize(9).FontColor(_reportStyle.ColorTextoSecundario);
                                    
                                    if (!string.IsNullOrEmpty(sale.Notes))
                                        infoCol.Item().Text($"Notas: {sale.Notes}")
                                            .FontSize(8).Italic().FontColor(_reportStyle.ColorTextoSecundario);
                                });
                            });
                            
                            col.Item().PaddingTop(8)
                                .LineHorizontal(1).LineColor(_reportStyle.ColorAcento);
                        });
                    });

                    page.Content().PaddingTop(15).Element(content =>
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
                                table.Header(header =>
                                {
                                    header.Cell().Background(_reportStyle.ColorTablaHeader)
                                        .Padding(6).Text("Producto").Bold().FontColor(Colors.White).FontSize(9);
                                    header.Cell().Background(_reportStyle.ColorTablaHeader)
                                        .Padding(6).AlignCenter().Text("Cant.").Bold().FontColor(Colors.White).FontSize(9);
                                    header.Cell().Background(_reportStyle.ColorTablaHeader)
                                        .Padding(6).AlignRight().Text("Te cuesta c/u").Bold().FontColor(Colors.White).FontSize(9);
                                    header.Cell().Background(_reportStyle.ColorTablaHeader)
                                        .Padding(6).AlignRight().Text("Total a pagar").Bold().FontColor(Colors.White).FontSize(9);
                                    header.Cell().Background(_reportStyle.ColorTablaHeader)
                                        .Padding(6).AlignRight().Text("Precio sugerido").Bold().FontColor(Colors.White).FontSize(9);
                                });

                                // Filas de productos
                                bool alternate = false;
                                foreach (var detail in sale.SaleDetails)
                                {
                                    var bg = alternate ? _reportStyle.ColorFilasAlternas : "#FFFFFF";
                                    alternate = !alternate;

                                    var partnerPrice = detail.UnitPrice;
                                    var suggestedPrice = detail.Product?.SalePrice ?? detail.UnitPrice;
                                    var totalToPay = partnerPrice * detail.Quantity;

                                    table.Cell().Background(bg).Padding(6).Column(nameCol =>
                                    {
                                        nameCol.Item().Text(detail.Product?.Name ?? "-").FontSize(9);
                                        if (detail.Product?.IsPartnership == true)
                                            nameCol.Item().Text("Alianza").FontSize(7).FontColor(_reportStyle.ColorTextoSecundario);
                                    });

                                    table.Cell().Background(bg).Padding(6).AlignCenter().Text(detail.Quantity.ToString()).FontSize(9);
                                    table.Cell().Background(bg).Padding(6).AlignRight().Text($"${partnerPrice:N0}").FontSize(9).Bold();
                                    table.Cell().Background(bg).Padding(6).AlignRight().Text($"${totalToPay:N0}").FontSize(9).Bold().FontColor(_reportStyle.ColorPrincipal);
                                    table.Cell().Background(bg).Padding(6).AlignRight().Text($"${suggestedPrice:N0}").FontSize(9).FontColor(_reportStyle.ColorExito);
                                }
                            });

                            // Espacio
                            col.Item().Height(20);

                            // Resumen financiero
                            col.Item().Border(1).BorderColor(_reportStyle.ColorBordes).Padding(12).Column(summary =>
                            {
                                summary.Item().Row(row =>
                                {
                                    row.RelativeItem().Text("TOTAL A PAGAR A AS").Bold().FontSize(12).FontColor(_reportStyle.ColorPrincipal);
                                    row.ConstantItem(140).AlignRight().Text($"${sale.Total:N0}").Bold().FontSize(14).FontColor(_reportStyle.ColorPrincipal);
                                });

                                // Mostrar crédito si existe
                                if (sale.Credit != null)
                                {
                                    summary.Item().PaddingTop(10).LineHorizontal(1).LineColor(_reportStyle.ColorBordes);
                                    summary.Item().PaddingTop(8).Row(row =>
                                    {
                                        row.RelativeItem().Text("Abonado:").FontSize(9).FontColor(_reportStyle.ColorTextoSecundario);
                                        row.ConstantItem(140).AlignRight().Text($"${sale.Credit.PaidAmount:N0}").FontSize(9).FontColor(_reportStyle.ColorExito);
                                    });
                                    summary.Item().PaddingTop(2).Row(row =>
                                    {
                                        row.RelativeItem().Text("Saldo pendiente:").Bold().FontSize(10);
                                        row.ConstantItem(140).AlignRight().Text($"${sale.Credit.PendingAmount:N0}").Bold().FontSize(12).FontColor(_reportStyle.ColorAlerta);
                                    });
                                }
                            });

                            // Nota informativa
                            col.Item().PaddingTop(12).Border(1).BorderColor(_reportStyle.ColorBordes).Padding(10).Column(note =>
                            {
                                note.Item().Text("ℹ Información de precios").Bold().FontSize(8).FontColor(_reportStyle.ColorPrincipal);
                                note.Item().PaddingTop(4);
                                note.Item().Text("• Te cuesta c/u: Precio al que te entregamos cada producto").FontSize(7).FontColor(_reportStyle.ColorTextoSecundario);
                                note.Item().Text("• Precio sugerido: Precio recomendado de venta al público").FontSize(7).FontColor(_reportStyle.ColorTextoSecundario);
                            });
                        });
                    });

                    // Footer removido - no mostrar fecha de generación
                });
            });

            return document.GeneratePdf();
        }
    }
}
