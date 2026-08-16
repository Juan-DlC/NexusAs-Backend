using AutoMapper;
using NexusAs.Application.DTOs.Dashboard;
using NexusAs.Application.DTOs.Products;
using NexusAs.Application.Interfaces;
using NexusAs.Domain.Enums;

namespace NexusAs.Application.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ISaleRepository _saleRepository;

        public DashboardService(IUnitOfWork unitOfWork, IMapper mapper, ISaleRepository saleRepository)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _saleRepository = saleRepository;
        }

        public async Task<DashboardDto> GetSummaryAsync()
        {
            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);

            var salesList = await _unitOfWork.Sales.GetTodaySalesAsync(today, tomorrow);

            var pendingCredits = await _unitOfWork.Credits.FindAsync(c =>
                c.IsActive && c.Status != CreditStatus.Paid);

            var lowStockProducts = await _unitOfWork.Products.FindAsync(p =>
                p.IsActive && p.Stock <= p.MinStock);

            return new DashboardDto
            {
                TodaySales = salesList.Sum(s => s.Total),
                TodayCash = salesList
                    .Where(s => s.PaymentMethodEntity != null && s.PaymentMethodEntity.Code == "CASH")
                    .Sum(s => s.Total),
                TodayCredit = salesList
                    .Where(s => s.PaymentMethodEntity != null && s.PaymentMethodEntity.Code == "CREDIT")
                    .Sum(s => s.Total),
                TodayTransactions = salesList.Count(),
                PendingCredits = pendingCredits.Count(),
                PendingCreditsAmount = pendingCredits.Sum(c => c.PendingAmount),
                LowStockProducts = _mapper.Map<IEnumerable<ProductDto>>(lowStockProducts)
            };
        }

        // TAREA 2: Nuevo método con ventas recientes y PaymentMethodName
        public async Task<DashboardSummaryDto> GetDashboardSummaryAsync(int userId, string userRole)
        {
            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);

            // Ventas de hoy
            var todaySales = await _saleRepository.GetSalesForDashboardAsync(userId, userRole, today, tomorrow);

            // Créditos pendientes
            var pendingCredits = await _unitOfWork.Credits.FindAsync(c => 
                c.IsActive && c.Status != CreditStatus.Paid);

            // Productos con stock bajo
            var lowStockProducts = await _unitOfWork.Products.FindAsync(p => 
                p.IsActive && p.Stock <= p.MinStock);

            // Ventas recientes
            var recentSales = await _saleRepository.GetRecentSalesAsync(userId, userRole, 10);

            return new DashboardSummaryDto
            {
                TotalSalesToday = todaySales.Count(),
                TotalRevenueToday = todaySales.Sum(s => s.Total),
                LowStockCount = lowStockProducts.Count(),
                PendingCreditsCount = pendingCredits.Count(),
                PendingCreditsAmount = pendingCredits.Sum(c => c.PendingAmount),
                RecentSales = recentSales.Select(sale => new RecentSaleDto
                {
                    SaleNumber = sale.SaleNumber,
                    CustomerOrSellerName = sale.Customer?.Name ?? sale.User?.FullName ?? "Sin cliente",
                    Total = sale.Total,
                    PaymentMethodName = sale.PaymentMethodEntity?.Name ?? "Contado",
                    Date = sale.Date
                }).ToList()
            };
        }
    }
}
