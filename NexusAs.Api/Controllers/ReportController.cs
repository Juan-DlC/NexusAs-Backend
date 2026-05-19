using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexusAs.Application.Interfaces;

namespace NexusAs.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class ReportController : ControllerBase
    {
        private readonly IReportService _reportService;

        public ReportController(IReportService reportService)
        {
            _reportService = reportService;
        }

        [HttpGet("sales/pdf")]
        public async Task<IActionResult> GetSalesPdf(
            [FromQuery] DateTime from,
            [FromQuery] DateTime to)
        {
            var pdf = await _reportService.GenerateSalesReportPdfAsync(from, to);
            return File(pdf, "application/pdf",
                $"Ventas_{from:yyyyMMdd}_{to:yyyyMMdd}.pdf");
        }

        [HttpGet("sales/excel")]
        public async Task<IActionResult> GetSalesExcel(
            [FromQuery] DateTime from,
            [FromQuery] DateTime to)
        {
            var excel = await _reportService.GenerateSalesReportExcelAsync(from, to);
            return File(excel,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Ventas_{from:yyyyMMdd}_{to:yyyyMMdd}.xlsx");
        }

        [HttpGet("catalog/pdf")]
        public async Task<IActionResult> GetCatalogPdf(
            [FromQuery] int? categoryId = null)
        {
            var pdf = await _reportService.GenerateCatalogPdfAsync(categoryId);
            return File(pdf, "application/pdf", "Catalogo_NexusAs.pdf");
        }
    }
}