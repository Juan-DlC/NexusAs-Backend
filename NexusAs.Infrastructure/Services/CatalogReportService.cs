using NexusAs.Application.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace NexusAs.Infrastructure.Services
{
    public class CatalogReportService : ICatalogReportService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ReportStyleHelper _reportStyle;

        public CatalogReportService(IUnitOfWork unitOfWork, ReportStyleHelper reportStyle)
        {
            _unitOfWork = unitOfWork;
            _reportStyle = reportStyle;
        }

        public async Task<byte[]> GenerateCatalogPdfAsync(int? categoryId = null)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var products = await _unitOfWork.Products.FindAsync(p =>
                p.IsActive &&
                (categoryId == null || p.CategoryId == categoryId));
            var productsList = products.ToList();

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
                                    .Text("Tenis • Cuidado de Piel • y más")
                                    .FontSize(9).FontColor(_reportStyle.ColorAcento).Italic();
                                titleCol.Item().PaddingLeft(8).PaddingTop(2)
                                    .Text("Catálogo de Productos")
                                    .FontSize(13).Bold().FontColor(_reportStyle.ColorAcento);
                                titleCol.Item().PaddingLeft(8)
                                    .Text($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}")
                                    .FontSize(9).FontColor(_reportStyle.ColorPrincipal);
                            });
                        });
                        col.Item().PaddingTop(6)
                            .LineHorizontal(2).LineColor(_reportStyle.ColorAcento);
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
                                    header.Cell().Background(_reportStyle.ColorPrincipal)
                                        .Padding(5).Text(title)
                                        .FontColor(Colors.White).Bold();
                                }
                            });

                            foreach (var product in productsList)
                            {
                                var bg = productsList.IndexOf(product) % 2 == 0
                                    ? "#FFFFFF" : _reportStyle.ColorFondoSuave;
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
                        col.Item().LineHorizontal(1).LineColor(_reportStyle.ColorAcento);
                        col.Item().PaddingTop(4).Row(row =>
                        {
                            row.RelativeItem().Text("AS Accesorios")
                                .FontSize(8).FontColor(_reportStyle.ColorAcento).Italic();
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
                                .FontSize(8).FontColor(_reportStyle.ColorPrincipal).Italic();
                        });
                    });
                });
            });

            return document.GeneratePdf();
        }
    }
}
