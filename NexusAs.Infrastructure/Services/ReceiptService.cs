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
                .Include(s => s.Returns.Where(r => r.IsActive))
                    .ThenInclude(r => r.ReturnDetails)
                .FirstOrDefaultAsync(s => s.Id == saleId);

            if (sale == null)
                throw new Exception($"Venta con id {saleId} no encontrada.");

            var logoBytes = File.Exists(_reportStyle.LogoPath)
                ? File.ReadAllBytes(_reportStyle.LogoPath) : null;

            var document = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.Letter);
                    page.Margin(0.6f, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(10).FontColor(_reportStyle.ColorPrincipal));

                    page.Header().Column(col =>
                    {
                        // Encabezado compacto: Logo + Detalles de Factura + Cliente (si existe)
                        col.Item().Row(row =>
                        {
                            // Logo más pequeño
                            if (logoBytes != null)
                            {
                                row.ConstantItem(70).AlignMiddle().Image(logoBytes).FitArea();
                            }

                            // Información de factura (columna izquierda)
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

                            // Información del cliente (columna derecha, si existe)
                            if (sale.Customer != null)
                            {
                                row.RelativeItem().PaddingLeft(10).AlignMiddle().Column(clientCol =>
                                {
                                    clientCol.Item().Text("CLIENTE:")
                                        .FontSize(8).Bold().FontColor(_reportStyle.ColorTextoSecundario);
                                    
                                    clientCol.Item().Text(sale.Customer.Name)
                                        .FontSize(8).FontColor(_reportStyle.ColorPrincipal);
                                    
                                    if (!string.IsNullOrEmpty(sale.Customer.Document))
                                    {
                                        clientCol.Item().Text($"Doc: {sale.Customer.Document}")
                                            .FontSize(7).FontColor(_reportStyle.ColorTextoSecundario);
                                    }
                                    
                                    if (!string.IsNullOrEmpty(sale.Customer.Phone))
                                    {
                                        clientCol.Item().Text($"Tel: {sale.Customer.Phone}")
                                            .FontSize(7).FontColor(_reportStyle.ColorTextoSecundario);
                                    }
                                });
                            }
                        });

                        // Línea separadora más delgada
                        col.Item().PaddingTop(2).PaddingBottom(2)
                            .LineHorizontal(0.5f).LineColor(_reportStyle.ColorAcento);
                    });

                    page.Content().Column(col =>
                    {
                        col.Item().Text("DETALLE DE PRODUCTOS")
                            .FontSize(9).Bold().FontColor(_reportStyle.ColorPrincipal);

                        col.Item().PaddingTop(2).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(70);  // Código - más espacio
                                c.RelativeColumn(4);   // Producto - más espacio
                                c.ConstantColumn(35);  // Cant. - reducido
                                c.ConstantColumn(70);  // Precio Unit. - reducido y fijo
                                c.ConstantColumn(70);  // Subtotal - reducido y fijo
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(_reportStyle.ColorTablaHeader)
                                    .Padding(4).AlignCenter().Text("Código")
                                    .FontColor(Colors.White).Bold().FontSize(8);
                                header.Cell().Background(_reportStyle.ColorTablaHeader)
                                    .Padding(4).AlignLeft().Text("Producto")
                                    .FontColor(Colors.White).Bold().FontSize(8);
                                header.Cell().Background(_reportStyle.ColorTablaHeader)
                                    .Padding(4).AlignCenter().Text("Cant.")
                                    .FontColor(Colors.White).Bold().FontSize(8);
                                header.Cell().Background(_reportStyle.ColorTablaHeader)
                                    .Padding(4).AlignCenter().Text("Precio Unit.")
                                    .FontColor(Colors.White).Bold().FontSize(8);
                                header.Cell().Background(_reportStyle.ColorTablaHeader)
                                    .Padding(4).AlignCenter().Text("Subtotal")
                                    .FontColor(Colors.White).Bold().FontSize(8);
                            });

                            var details = sale.SaleDetails.ToList();
                            
                            // Calcular cantidades devueltas por producto
                            var returnedQuantities = sale.Returns
                                .Where(r => r.IsActive)
                                .SelectMany(r => r.ReturnDetails)
                                .GroupBy(rd => rd.ProductId)
                                .ToDictionary(g => g.Key, g => g.Sum(rd => rd.Quantity));
                            
                            foreach (var detail in details)
                            {
                                // Calcular cantidad neta (vendida - devuelta)
                                var returnedQty = returnedQuantities.GetValueOrDefault(detail.ProductId, 0);
                                var netQuantity = detail.Quantity - returnedQty;
                                
                                // Solo mostrar productos con cantidad neta mayor a 0
                                if (netQuantity <= 0) continue;
                                
                                var bg = details.IndexOf(detail) % 2 == 0
                                     ? "#FFFFFF" : _reportStyle.ColorFilasAlternas;
                                
                                table.Cell().Background(bg).Padding(4).AlignCenter()
                                    .Text(detail.Product?.Code ?? "").FontSize(7);
                                
                                table.Cell().Background(bg).Padding(4).Column(col =>
                                {
                                    col.Item().Text(detail.Product?.Name ?? "").FontSize(8);
                                    if (!string.IsNullOrEmpty(detail.Product?.Description))
                                        col.Item().Text(detail.Product.Description)
                                            .FontSize(6).Italic().FontColor(_reportStyle.ColorTextoSecundario);
                                });
                                
                                table.Cell().Background(bg).Padding(4).AlignCenter()
                                    .Text(netQuantity.ToString()).FontSize(8);
                                table.Cell().Background(bg).Padding(4).AlignCenter()
                                    .Text($"${detail.UnitPrice:N0}").FontSize(8);
                                table.Cell().Background(bg).Padding(4).AlignCenter()
                                    .Text($"${detail.UnitPrice * netQuantity:N0}").FontSize(8).Bold();
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
                                                header.Cell().Background(_reportStyle.ColorTablaHeader)
                                                    .Padding(3).AlignCenter().Text("Cuota")
                                                    .FontColor(Colors.White).Bold().FontSize(7);
                                                header.Cell().Background(_reportStyle.ColorTablaHeader)
                                                    .Padding(3).AlignCenter().Text("Valor")
                                                    .FontColor(Colors.White).Bold().FontSize(7);
                                                header.Cell().Background(_reportStyle.ColorTablaHeader)
                                                    .Padding(3).AlignCenter().Text("Estado")
                                                    .FontColor(Colors.White).Bold().FontSize(7);
                                            });

                                            var installments = sale.Credit.Installments
                                                .OrderBy(i => i.Number).ToList();
                                            foreach (var inst in installments)
                                            {
                                                var bg = installments.IndexOf(inst) % 2 == 0
                                                    ? "#FFFFFF" : _reportStyle.ColorFilasAlternas;
                                                it.Cell().Background(bg).Padding(3).AlignCenter()
                                                    .Text($"#{inst.Number}").FontSize(7);
                                                it.Cell().Background(bg).Padding(3).AlignCenter()
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

                        // Totales y Nota Importante en dos columnas
                        col.Item().PaddingTop(4).PaddingBottom(3)
                            .BorderTop(1).BorderColor(_reportStyle.ColorBordes)
                            .PaddingTop(3).Row(row =>
                        {
                            // Columna izquierda: Nota importante
                            row.RelativeItem(2).PaddingRight(10)
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

                            // Columna derecha: Totales y método de pago
                            row.RelativeItem(1).Column(totalsCol =>
                            {
                                totalsCol.Item().Row(subtotalRow =>
                                {
                                    subtotalRow.RelativeItem().Text("Subtotal:")
                                        .FontSize(9).FontColor(_reportStyle.ColorTextoSecundario);
                                    subtotalRow.AutoItem().Text($"${sale.Subtotal:N0}")
                                        .FontSize(9);
                                });

                                if (sale.Discount > 0)
                                {
                                    totalsCol.Item().Row(discountRow =>
                                    {
                                        discountRow.RelativeItem().Text("Descuento:")
                                            .FontSize(9).FontColor(_reportStyle.ColorExito);
                                        discountRow.AutoItem().Text($"-${sale.Discount:N0}")
                                            .FontSize(9).FontColor(_reportStyle.ColorExito);
                                    });
                                }

                                totalsCol.Item().PaddingTop(2).Row(totalRow =>
                                {
                                    totalRow.RelativeItem().Text("TOTAL:")
                                        .Bold().FontSize(11).FontColor(_reportStyle.ColorPrincipal);
                                    totalRow.AutoItem().Text($"${sale.Total:N0}")
                                        .Bold().FontSize(11).FontColor(_reportStyle.ColorPrincipal);
                                });

                                totalsCol.Item().PaddingTop(3).Row(paymentRow =>
                                {
                                    paymentRow.RelativeItem().Text("Método de pago:")
                                        .FontSize(8).FontColor(_reportStyle.ColorTextoSecundario);
                                    paymentRow.AutoItem().Text(sale.PaymentMethodEntity?.Code == "CASH"
                                        ? "Contado" : "Crédito")
                                        .FontSize(8).FontColor(_reportStyle.ColorTextoSecundario);
                                });
                            });
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
                .Include(s => s.Returns.Where(r => r.IsActive))
                    .ThenInclude(r => r.ReturnDetails)
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
                    page.Size(PageSizes.Letter);
                    page.Margin(0.6f, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(10).FontColor(_reportStyle.ColorPrincipal));

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

                            // Información de factura
                            row.RelativeItem().PaddingLeft(10).AlignMiddle().Column(infoCol =>
                            {
                                infoCol.Item().Text("FACTURA MAYORISTA")
                                    .FontSize(8).Bold().FontColor(_reportStyle.ColorTextoSecundario);
                                
                                infoCol.Item().Text($"Factura: {sale.SaleNumber}")
                                    .Bold().FontSize(8).FontColor(_reportStyle.ColorPrincipal);
                                
                                infoCol.Item().Text($"Fecha: {sale.Date:dd/MM/yyyy HH:mm}")
                                    .FontSize(8).FontColor(_reportStyle.ColorTextoSecundario);
                                
                                infoCol.Item().Text($"Mayorista: {seller?.FullName ?? "Sin nombre"}")
                                    .FontSize(8).FontColor(_reportStyle.ColorTextoSecundario);
                            });
                        });

                        // Línea separadora más delgada
                        col.Item().PaddingTop(2).PaddingBottom(2)
                            .LineHorizontal(0.5f).LineColor(_reportStyle.ColorAcento);
                    });

                    page.Content().Column(col =>
                    {
                        col.Item().Text("DETALLE DE PRODUCTOS")
                            .FontSize(9).Bold().FontColor(_reportStyle.ColorPrincipal);

                        col.Item().PaddingTop(2).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(4);   // Producto
                                c.ConstantColumn(35);  // Cant.
                                c.ConstantColumn(80);  // Te cuesta c/u
                                c.ConstantColumn(80);  // Total a pagar
                                c.ConstantColumn(80);  // Precio sugerido
                            });

                            // Header de tabla centrado
                            table.Header(header =>
                            {
                                header.Cell().Background(_reportStyle.ColorTablaHeader)
                                    .Padding(4).AlignLeft().Text("Producto")
                                    .FontColor(Colors.White).Bold().FontSize(8);
                                header.Cell().Background(_reportStyle.ColorTablaHeader)
                                    .Padding(4).AlignCenter().Text("Cant.")
                                    .FontColor(Colors.White).Bold().FontSize(8);
                                header.Cell().Background(_reportStyle.ColorTablaHeader)
                                    .Padding(4).AlignCenter().Text("Te cuesta c/u")
                                    .FontColor(Colors.White).Bold().FontSize(8);
                                header.Cell().Background(_reportStyle.ColorTablaHeader)
                                    .Padding(4).AlignCenter().Text("Total a pagar")
                                    .FontColor(Colors.White).Bold().FontSize(8);
                                header.Cell().Background(_reportStyle.ColorTablaHeader)
                                    .Padding(4).AlignCenter().Text("Precio sugerido")
                                    .FontColor(Colors.White).Bold().FontSize(8);
                            });

                                // Calcular cantidades devueltas por producto
                                var returnedQuantities = sale.Returns
                                    .Where(r => r.IsActive)
                                    .SelectMany(r => r.ReturnDetails)
                                    .GroupBy(rd => rd.ProductId)
                                    .ToDictionary(g => g.Key, g => g.Sum(rd => rd.Quantity));

                                // Filas de productos
                                bool alternate = false;
                                foreach (var detail in sale.SaleDetails)
                                {
                                    // Calcular cantidad neta (vendida - devuelta)
                                    var returnedQty = returnedQuantities.GetValueOrDefault(detail.ProductId, 0);
                                    var netQuantity = detail.Quantity - returnedQty;
                                    
                                    // Solo mostrar productos con cantidad neta mayor a 0
                                    if (netQuantity <= 0) continue;
                                    
                                    var bg = alternate ? _reportStyle.ColorFilasAlternas : "#FFFFFF";
                                    alternate = !alternate;

                                    var partnerPrice = detail.UnitPrice;
                                    var suggestedPrice = detail.Product?.SalePrice ?? detail.UnitPrice;
                                    var totalToPay = partnerPrice * netQuantity;

                                    table.Cell().Background(bg).Padding(4).Column(nameCol =>
                                    {
                                        nameCol.Item().Text(detail.Product?.Name ?? "-").FontSize(8).Bold();
                                        if (!string.IsNullOrEmpty(detail.Product?.Description))
                                            nameCol.Item().Text(detail.Product.Description).FontSize(6).FontColor(_reportStyle.ColorTextoSecundario).Italic();
                                    });

                                    table.Cell().Background(bg).Padding(4).AlignCenter().Text(netQuantity.ToString()).FontSize(8);
                                    table.Cell().Background(bg).Padding(4).AlignCenter().Text($"${partnerPrice:N0}").FontSize(8).Bold();
                                    table.Cell().Background(bg).Padding(4).AlignCenter().Text($"${totalToPay:N0}").FontSize(8).Bold().FontColor(_reportStyle.ColorPrincipal);
                                    table.Cell().Background(bg).Padding(4).AlignCenter().Text($"${suggestedPrice:N0}").FontSize(8).FontColor(_reportStyle.ColorExito);
                                }
                        });

                        // Notas de la venta (si existen)
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
                                });
                        }

                        // Totales y método de pago con información de precios
                        col.Item().PaddingTop(4).PaddingBottom(3)
                            .BorderTop(1).BorderColor(_reportStyle.ColorBordes)
                            .PaddingTop(3).Row(row =>
                        {
                            // Columna izquierda: Información de precios
                            row.RelativeItem(2).PaddingRight(10)
                                .Border(1).BorderColor(_reportStyle.ColorBordes)
                                .Padding(5).Column(noteCol =>
                                {
                                    noteCol.Item().Text("ℹ INFORMACIÓN DE PRECIOS")
                                        .Bold().FontSize(7).FontColor(_reportStyle.ColorPrincipal);
                                    noteCol.Item().PaddingTop(2).Text(
                                        "• Te cuesta c/u: Precio al que te entregamos cada producto\n" +
                                        "• Precio sugerido: Precio recomendado de venta al público")
                                        .FontSize(6).FontColor(_reportStyle.ColorTextoSecundario);
                                });

                            // Columna derecha: Totales
                            row.RelativeItem(1).Column(totalsCol =>
                            {
                                totalsCol.Item().Row(totalRow =>
                                {
                                    totalRow.RelativeItem().Text("TOTAL A PAGAR:")
                                        .Bold().FontSize(11).FontColor(_reportStyle.ColorPrincipal);
                                    totalRow.AutoItem().Text($"${sale.Total:N0}")
                                        .Bold().FontSize(11).FontColor(_reportStyle.ColorPrincipal);
                                });

                                totalsCol.Item().PaddingTop(3).Row(paymentRow =>
                                {
                                    paymentRow.RelativeItem().Text("Método de pago:")
                                        .FontSize(8).FontColor(_reportStyle.ColorTextoSecundario);
                                    paymentRow.AutoItem().Text(sale.PaymentMethodEntity?.Code == "CASH"
                                        ? "Contado" : "Crédito")
                                        .FontSize(8).FontColor(_reportStyle.ColorTextoSecundario);
                                });
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
