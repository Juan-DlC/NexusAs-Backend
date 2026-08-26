using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexusAs.Application.Interfaces;
using NexusAs.Infrastructure.Data;
using NexusAs.Infrastructure.Filters;
using NexusAs.Infrastructure.Repositories;
using NexusAs.Infrastructure.Services;

namespace NexusAs.DI.Extensions
{
    public static class InfrastructureExtensions
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddDbContext<NexusAsDbContext>(options =>
                options.UseSqlServer(
                    configuration.GetConnectionString("NexusAsConnection")));

            services.AddScoped<IUnitOfWork, UnitOfWork>();
            
            // Registrar repositorios específicos requeridos por servicios
            services.AddScoped<ISaleRepository, SaleRepository>();
            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<ICreditRepository, CreditRepository>();
            services.AddScoped<IBusinessPartnerRepository, BusinessPartnerRepository>();

            services.AddControllers(options =>
            {
                options.Filters.Add<GlobalExceptionFilter>();
            });

            services.AddScoped<ReportStyleHelper>();
            services.AddScoped<ISalesReportService, SalesReportService>();
            services.AddScoped<ICatalogReportService, CatalogReportService>();
            services.AddScoped<IReceiptService, ReceiptService>();
            services.AddScoped<IPartnerReportService, PartnerReportService>();
            services.AddScoped<IBusinessPartnerReportService, BusinessPartnerReportService>();

            return services;
        }
    }
}