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

        public DashboardService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<DashboardDto> GetSummaryAsync()
        {
            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);

            var todaySales = await _unitOfWork.Sales.FindAsync(s =>
                s.IsActive && s.Date >= today && s.Date < tomorrow);

            var salesList = todaySales.ToList();

            var pendingCredits = await _unitOfWork.Credits.FindAsync(c =>
                c.IsActive && c.Status != CreditStatus.Paid);

            var lowStockProducts = await _unitOfWork.Products.FindAsync(p =>
                p.IsActive && p.Stock <= p.MinStock);

            return new DashboardDto
            {
                TodaySales = salesList.Sum(s => s.Total),
                TodayCash = salesList
                    .Where(s => s.PaymentMethod == PaymentMethod.Cash)
                    .Sum(s => s.Total),
                TodayCredit = salesList
                    .Where(s => s.PaymentMethod == PaymentMethod.Credit)
                    .Sum(s => s.Total),
                TodayTransactions = salesList.Count,
                PendingCredits = pendingCredits.Count(),
                PendingCreditsAmount = pendingCredits.Sum(c => c.PendingAmount),
                LowStockProducts = _mapper.Map<IEnumerable<ProductDto>>(lowStockProducts)
            };
        }
    }
}