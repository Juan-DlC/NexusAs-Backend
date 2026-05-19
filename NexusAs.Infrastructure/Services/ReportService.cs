using ClosedXML.Excel;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using NexusAs.Application.Interfaces;
using NexusAs.Domain.Enums;
using NexusAs.Infrastructure.Data;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace NexusAs.Infrastructure.Services
{
    public class ReportService : IReportService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly NexusAsDbContext _context;
        private readonly string _logoPath;

        // Paleta de colores AS Accesorios
        private const string ColorPrincipal = "#4A4A4A";
        private const string ColorAcento = "#C8956C";
        private const string ColorFondoSuave = "#F5EDE8";
        private const string ColorExito = "#5C8A6B";
        private const string ColorAlerta = "#C0392B";

        public ReportService(
            IUnitOfWork unitOfWork,
            NexusAsDbContext context,
            IWebHostEnvironment env)
        {
            _unitOfWork = unitOfWork;
            _context = context;
            _logoPath = Path.Combine(env.WebRootPath, "images", "Logo.png");
        }

        public async Task<byte[]> GenerateSalesReportPdfAsync(DateTime from, DateTime to)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var sales = await _context.Sales
                .Include(s => s.Customer)
                .Where(s => s.IsActive && s.Date >= from && s.Date <= to)
                .ToListAsync();
            var salesList = sales.ToList();

            var logoBytes = File.Exists(_logoPath)
                ? File.ReadAllBytes(_logoPath) : null;

            var document = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(11).FontColor(ColorPrincipal));

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
                                    .FontSize(20).Bold().FontColor(ColorPrincipal);
                                titleCol.Item().PaddingLeft(8)
                                    .Text("Tenis • Cuidado de Piel • y más")
                                    .FontSize(9).FontColor(ColorAcento).Italic();
                                titleCol.Item().PaddingLeft(8).PaddingTop(2)
                                    .Text("Informe de Ventas")
                                    .FontSize(13).Bold().FontColor(ColorAcento);
                                titleCol.Item().PaddingLeft(8)
                                    .Text($"Período: {from:dd/MM/yyyy} — {to:dd/MM/yyyy}")
                                    .FontSize(10).FontColor(ColorPrincipal);
                            });
                        });
                        col.Item().PaddingTop(6)
                            .LineHorizontal(2).LineColor(ColorAcento);
                    });

                    page.Content().PaddingTop(10).Column(col =>
                    {
                        // Resumen ejecutivo
                        col.Item().Background(ColorFondoSuave).Padding(8).Column(res =>
                        {
                            res.Item().Text("Resumen Ejecutivo")
                                .FontSize(13).Bold().FontColor(ColorAcento);
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
                                    .Text($"${salesList.Where(s => s.PaymentMethod == PaymentMethod.Cash).Sum(s => s.Total):N0}");
                                table.Cell().Text("Total Crédito:").Bold();
                                table.Cell().AlignRight()
                                    .Text($"${salesList.Where(s => s.PaymentMethod == PaymentMethod.Credit).Sum(s => s.Total):N0}");
                                table.Cell().Text("Número de Transacciones:").Bold();
                                table.Cell().AlignRight()
                                    .Text($"{salesList.Count}");
                            });
                        });

                        col.Item().PaddingTop(15)
                            .Text("Detalle de Ventas")
                            .FontSize(13).Bold().FontColor(ColorPrincipal);

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
                                    header.Cell().Background(ColorPrincipal)
                                        .Padding(5).Text(title)
                                        .FontColor(Colors.White).Bold();
                                }
                            });

                            foreach (var sale in salesList)
                            {
                                var bg = salesList.IndexOf(sale) % 2 == 0
                                      ? "#FFFFFF" : ColorFondoSuave;
                                table.Cell().Background(bg).Padding(5)
                                    .Text(sale.SaleNumber);
                                table.Cell().Background(bg).Padding(5)
                                    .Text(sale.Customer?.Name ?? "Sin cliente");
                                table.Cell().Background(bg).Padding(5)
                                    .Text(sale.Date.ToString("dd/MM/yy"));
                                table.Cell().Background(bg).Padding(5)
                                    .Text(sale.PaymentMethod == PaymentMethod.Cash
                                        ? "Contado" : "Crédito");
                                table.Cell().Background(bg).Padding(5)
                                    .Text($"${sale.Total:N0}");
                            }
                        });
                    });

                    page.Footer().Column(col =>
                    {
                        col.Item().LineHorizontal(1).LineColor(ColorAcento);
                        col.Item().PaddingTop(4).Row(row =>
                        {
                            row.RelativeItem().Text("AS Accesorios")
                                .FontSize(8).FontColor(ColorAcento).Italic();
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
                                .FontSize(8).FontColor(ColorPrincipal);
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
                ws.Cell(row, 4).Value = sale.PaymentMethod == PaymentMethod.Cash
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

        public async Task<byte[]> GenerateCatalogPdfAsync(int? categoryId = null)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var products = await _unitOfWork.Products.FindAsync(p =>
                p.IsActive &&
                (categoryId == null || p.CategoryId == categoryId));
            var productsList = products.ToList();

            var logoBytes = File.Exists(_logoPath)
                ? File.ReadAllBytes(_logoPath) : null;

            var document = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(11).FontColor(ColorPrincipal));

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
                                    .FontSize(20).Bold().FontColor(ColorPrincipal);
                                titleCol.Item().PaddingLeft(8)
                                    .Text("Tenis • Cuidado de Piel • y más")
                                    .FontSize(9).FontColor(ColorAcento).Italic();
                                titleCol.Item().PaddingLeft(8).PaddingTop(2)
                                    .Text("Catálogo de Productos")
                                    .FontSize(13).Bold().FontColor(ColorAcento);
                                titleCol.Item().PaddingLeft(8)
                                    .Text($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}")
                                    .FontSize(9).FontColor(ColorPrincipal);
                            });
                        });
                        col.Item().PaddingTop(6)
                            .LineHorizontal(2).LineColor(ColorAcento);
                    });

                    page.Content().PaddingTop(10).Column(col =>
                    {
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(60);
                                c.RelativeColumn();
                                c.RelativeColumn(2);
                                c.ConstantColumn(80);
                            });

                            table.Header(header =>
                            {
                                foreach (var title in new[] { "Código", "Producto", "Descripción", "Precio" })
                                {
                                    header.Cell().Background(ColorPrincipal)
                                        .Padding(5).Text(title)
                                        .FontColor(Colors.White).Bold();
                                }
                            });

                            foreach (var product in productsList)
                            {
                                var bg = productsList.IndexOf(product) % 2 == 0
                                    ? "#FFFFFF" : ColorFondoSuave;
                                table.Cell().Background(bg).Padding(5)
                                    .Text(product.Code);
                                table.Cell().Background(bg).Padding(5)
                                    .Text(product.Name).Bold();
                                table.Cell().Background(bg).Padding(5)
                                    .Text(product.Description ?? "-");
                                table.Cell().Background(bg).Padding(5)
                                    .Text($"${product.SalePrice:N0}");
                            }
                        });
                    });

                    page.Footer().Column(col =>
                    {
                        col.Item().LineHorizontal(1).LineColor(ColorAcento);
                        col.Item().PaddingTop(4).Row(row =>
                        {
                            row.RelativeItem().Text("AS Accesorios")
                                .FontSize(8).FontColor(ColorAcento).Italic();
                            row.RelativeItem().AlignCenter()
                                .Text(x =>
                                {
                                    x.Span("Página ").FontSize(8);
                                    x.CurrentPageNumber().FontSize(8);
                                    x.Span(" de ").FontSize(8);
                                    x.TotalPages().FontSize(8);
                                });
                            row.RelativeItem().AlignRight()
                                .Text("Precios sujetos a cambio sin previo aviso")
                                .FontSize(8).FontColor(ColorPrincipal).Italic();
                        });
                    });
                });
            });

            return document.GeneratePdf();
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
                .FirstOrDefaultAsync(s => s.Id == saleId);

            if (sale == null)
                throw new Exception($"Venta con id {saleId} no encontrada.");

            var logoBytes = File.Exists(_logoPath)
                ? File.ReadAllBytes(_logoPath) : null;

            var document = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A5);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(10).FontColor(ColorPrincipal));

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
                                    .FontSize(16).Bold().FontColor(ColorPrincipal);
                                titleCol.Item().PaddingLeft(8)
                                    .Text("Tenis • Cuidado de Piel • y más")
                                    .FontSize(8).FontColor(ColorAcento).Italic();
                                titleCol.Item().PaddingLeft(8).PaddingTop(2)
                                    .Text("Recibo de Venta")
                                    .FontSize(11).Bold().FontColor(ColorAcento);
                            });
                        });

                        col.Item().PaddingTop(6)
                            .LineHorizontal(2).LineColor(ColorAcento);

                        col.Item().PaddingTop(6)
                            .Background(ColorFondoSuave).Padding(6)
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
                            .LineHorizontal(1).LineColor(ColorAcento);
                    });

                    page.Content().PaddingTop(8).Column(col =>
                    {
                        col.Item().Text("Detalle de Productos")
                            .FontSize(11).Bold().FontColor(ColorPrincipal);

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
                                    header.Cell().Background(ColorPrincipal)
                                        .Padding(4).Text(title)
                                        .FontColor(Colors.White).Bold().FontSize(9);
                                }
                            });

                            var details = sale.SaleDetails.ToList();
                            foreach (var detail in details)
                            {
                                var bg = details.IndexOf(detail) % 2 == 0
                                     ? "#FFFFFF" : ColorFondoSuave;
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
                                    .Bold().FontColor(ColorExito);
                                table.Cell().AlignRight()
                                    .Text($"-${sale.Discount:N0}")
                                    .FontColor(ColorExito);
                            }

                            table.Cell().Text("TOTAL:").Bold().FontSize(13);
                            table.Cell().AlignRight()
                                .Text($"${sale.Total:N0}")
                                .Bold().FontSize(13).FontColor(ColorAcento);

                            table.Cell().Text("Método de pago:").Bold();
                            table.Cell().AlignRight()
                                .Text(sale.PaymentMethod == PaymentMethod.Cash
                                    ? "Contado ✓" : "Crédito");
                        });

                        if (sale.Credit != null)
                        {
                            col.Item().PaddingTop(10)
                                .Border(1).BorderColor(ColorAcento)
                                .Background(ColorFondoSuave)
                                .Padding(8).Column(creditCol =>
                                {
                                    creditCol.Item()
                                        .Text("Estado del Crédito")
                                        .Bold().FontSize(11).FontColor(ColorAcento);

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
                                            .Bold().FontColor(ColorExito);
                                        t.Cell().AlignRight()
                                            .Text($"${sale.Credit.PaidAmount:N0}")
                                            .FontColor(ColorExito);

                                        t.Cell().Text("Saldo pendiente:")
                                            .Bold().FontColor(ColorAlerta);
                                        t.Cell().AlignRight()
                                            .Text($"${sale.Credit.PendingAmount:N0}")
                                            .Bold().FontColor(ColorAlerta);

                                        t.Cell().Text("Estado:").Bold();
                                        t.Cell().AlignRight().Text(
                                            sale.Credit.Status == CreditStatus.Paid
                                                ? "✓ PAGADO COMPLETAMENTE" :
                                            sale.Credit.Status == CreditStatus.Partial
                                                ? "Pago parcial" : "Pendiente de pago");
                                    });
                                });
                        }

                        col.Item().PaddingTop(10)
                            .Background(ColorFondoSuave)
                            .Border(1).BorderColor(ColorAcento)
                            .Padding(6).Column(noteCol =>
                            {
                                noteCol.Item().Text("NOTA IMPORTANTE:")
                                    .Bold().FontSize(8).FontColor(ColorPrincipal);
                                noteCol.Item().Text(
                                    "Este documento es un recibo interno de AS Accesorios " +
                                    "y NO constituye factura electrónica de venta ante la DIAN. " +
                                    "No tiene validez tributaria ni fiscal. Para facturación " +
                                    "electrónica oficial, consulte con su contador.")
                                    .FontSize(8).FontColor(ColorPrincipal).Italic();
                            });
                    });

                    page.Footer().Column(col =>
                    {
                        col.Item().LineHorizontal(1).LineColor(ColorAcento);
                        col.Item().PaddingTop(4).AlignCenter()
                            .Text($"AS Accesorios — {DateTime.Now:dd/MM/yyyy HH:mm} — ¡Gracias por su compra!")
                            .FontSize(8).FontColor(ColorAcento).Italic();
                    });
                });
            });

            return document.GeneratePdf();
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

            var logoBytes = File.Exists(_logoPath)
                ? File.ReadAllBytes(_logoPath) : null;

            var document = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(11).FontColor(ColorPrincipal));

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
                                    .FontSize(20).Bold().FontColor(ColorPrincipal);
                                titleCol.Item().PaddingLeft(8)
                                    .Text("Estado de Cuenta — Socia")
                                    .FontSize(13).Bold().FontColor(ColorAcento);
                                titleCol.Item().PaddingLeft(8)
                                    .Text($"Socia: {config.User?.FullName ?? ""}")
                                    .FontSize(11).FontColor(ColorPrincipal);
                                titleCol.Item().PaddingLeft(8)
                                    .Text($"Período: {from:dd/MM/yyyy} — {to:dd/MM/yyyy}")
                                    .FontSize(10);
                            });
                        });
                        col.Item().PaddingTop(6).LineHorizontal(2).LineColor(ColorAcento);
                    });

                    page.Content().PaddingTop(10).Column(col =>
                    {
                        // Resumen
                        col.Item().Background(ColorFondoSuave).Padding(8).Column(res =>
                        {
                            res.Item().Text("Resumen").FontSize(13).Bold().FontColor(ColorAcento);
                            res.Item().PaddingTop(4).Table(table =>
                            {
                                table.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn();
                                    c.RelativeColumn();
                                });
                                table.Cell().Text("Total deuda con AS:").Bold();
                                table.Cell().AlignRight().Text($"${totalDebt:N0}");
                                table.Cell().Text("Total abonado:").Bold().FontColor(ColorExito);
                                table.Cell().AlignRight()
                                    .Text($"${totalPaid:N0}").FontColor(ColorExito);
                                table.Cell().Text("Saldo pendiente:").Bold().FontColor(ColorAlerta);
                                table.Cell().AlignRight()
                                    .Text($"${totalDebt - totalPaid:N0}").Bold().FontColor(ColorAlerta);
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
                                    header.Cell().Background(ColorPrincipal)
                                        .Padding(4).Text(title)
                                        .FontColor(Colors.White).Bold().FontSize(9);
                                }
                            });
                            foreach (var sale in sales)
                            {
                                var bg = sales.IndexOf(sale) % 2 == 0
                                    ? "#FFFFFF" : ColorFondoSuave;
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
                                    header.Cell().Background(ColorPrincipal)
                                        .Padding(4).Text(title)
                                        .FontColor(Colors.White).Bold().FontSize(9);
                                }
                            });
                            foreach (var sale in sales)
                            {
                                var bg = sales.IndexOf(sale) % 2 == 0
                                    ? "#FFFFFF" : ColorFondoSuave;
                                table.Cell().Background(bg).Padding(4)
                                    .Text(sale.Product?.Name ?? "").FontSize(9);
                                table.Cell().Background(bg).Padding(4)
                                    .Text($"${sale.PartnerPrice:N0}").FontSize(9);
                                table.Cell().Background(bg).Padding(4)
                                    .Text($"${sale.SalePrice:N0}").FontSize(9);
                                table.Cell().Background(bg).Padding(4)
                                    .Text($"${sale.PartnerEarning:N0}")
                                    .FontSize(9).FontColor(ColorExito);
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
                                        header.Cell().Background(ColorPrincipal)
                                            .Padding(4).Text(title)
                                            .FontColor(Colors.White).Bold().FontSize(9);
                                    }
                                });
                                foreach (var liq in liquidations)
                                {
                                    var bg = liquidations.IndexOf(liq) % 2 == 0
                                        ? "#FFFFFF" : ColorFondoSuave;
                                    table.Cell().Background(bg).Padding(4)
                                        .Text(liq.Date.ToString("dd/MM/yyyy")).FontSize(9);
                                    table.Cell().Background(bg).Padding(4)
                                        .Text($"${liq.Amount:N0}")
                                        .FontSize(9).FontColor(ColorExito);
                                    table.Cell().Background(bg).Padding(4)
                                        .Text(liq.Notes ?? "-").FontSize(9);
                                }
                            });
                        }

                        // Nota
                        col.Item().PaddingTop(12)
                            .Background(ColorFondoSuave)
                            .Border(1).BorderColor(ColorAcento)
                            .Padding(6).Column(noteCol =>
                            {
                                noteCol.Item().Text("NOTA:")
                                    .Bold().FontSize(8).FontColor(ColorPrincipal);
                                noteCol.Item().Text(
                                    "Las ganancias mostradas son estimadas basadas en el precio " +
                                    "sugerido de venta. El valor real depende del precio al que " +
                                    "cada producto fue vendido por la socia.")
                                    .FontSize(8).FontColor(ColorPrincipal).Italic();
                            });
                    });

                    page.Footer().Column(col =>
                    {
                        col.Item().LineHorizontal(1).LineColor(ColorAcento);
                        col.Item().PaddingTop(4).AlignCenter()
                            .Text($"AS Accesorios — Generado: {DateTime.Now:dd/MM/yyyy HH:mm}")
                            .FontSize(8).FontColor(ColorAcento).Italic();
                    });
                });
            });

            return document.GeneratePdf();
        }
    }
}