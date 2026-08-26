using AutoMapper;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.Extensions.DependencyInjection;
using NexusAs.Application.Interfaces;
using NexusAs.Application.Mappings;
using NexusAs.Application.Services;
using NexusAs.Application.Validators;
using NexusAs.Infrastructure.Services;

namespace NexusAs.DI.Extensions
{
    public static class ApplicationExtensions
    {
        public static IServiceCollection AddApplication(
            this IServiceCollection services)
        {
            // registramos: servicios, AutoMapper, FluentValidation, MediatR
            services.AddAutoMapper(cfg =>
            {
                cfg.AddProfile<AutoMapperProfile>();
            });
            services.AddFluentValidationAutoValidation();
            services.AddValidatorsFromAssemblyContaining<LoginValidator>();
            services.AddScoped<ICategoryService, CategoryService>();
            services.AddScoped<IProductService, ProductService>();
            services.AddScoped<ISupplierService, SupplierService>();
            services.AddScoped<IPaymentMethodService, PaymentMethodService>();
            services.AddScoped<ICustomerService, CustomerService>();
            services.AddScoped<ISaleService, SaleService>();
            services.AddScoped<ICreditService, CreditService>();
            services.AddScoped<IStockService, StockService>();
            services.AddScoped<IDashboardService, DashboardService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IPartnerService, PartnerService>();
            services.AddScoped<IReturnService, ReturnService>();
            services.AddScoped<IBusinessPartnerService, BusinessPartnerService>();

            return services;
        }
    }
}