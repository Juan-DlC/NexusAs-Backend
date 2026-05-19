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

            services.AddControllers(options =>
            {
                options.Filters.Add<GlobalExceptionFilter>();
            });

            services.AddScoped<IReportService, ReportService>();

            return services;
        }
    }
}