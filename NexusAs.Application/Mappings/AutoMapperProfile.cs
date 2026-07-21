using AutoMapper;
using NexusAs.Application.DTOs.Categories;
using NexusAs.Application.DTOs.Customers;
using NexusAs.Application.DTOs.Products;
using NexusAs.Domain.Entities;
using NexusAs.Application.DTOs.Sales;
using NexusAs.Application.DTOs.Credits;
using NexusAs.Application.DTOs.Stock;
using NexusAs.Application.DTOs.Users;

namespace NexusAs.Application.Mappings
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            // Category mappings
            CreateMap<Category, CategoryDto>();
            CreateMap<CreateCategoryDto, Category>();
            CreateMap<UpdateCategoryDto, Category>();

            // Product mappings
            CreateMap<Product, ProductDto>()
                .ForMember(dest => dest.CategoryName,
                    opt => opt.MapFrom(src => src.Category != null
                        ? src.Category.Name
                        : string.Empty));
            CreateMap<CreateProductDto, Product>();
            CreateMap<UpdateProductDto, Product>();

            // Customer mappings
            CreateMap<Customer, CustomerDto>();
            CreateMap<CreateCustomerDto, Customer>();
            CreateMap<UpdateCustomerDto, Customer>();

            CreateMap<Sale, SaleDto>()
            .ForMember(dest => dest.PaymentMethod,
                opt => opt.MapFrom(src => src.PaymentMethodEntity != null
                    ? src.PaymentMethodEntity.Name : string.Empty))
            .ForMember(dest => dest.CustomerName,
                opt => opt.MapFrom(src => src.Customer != null
                    ? src.Customer.Name : null))
            .ForMember(dest => dest.SellerName,
                opt => opt.MapFrom(src => src.User != null
                    ? src.User.FullName : string.Empty))
            .ForMember(dest => dest.Details,
                opt => opt.MapFrom(src => src.SaleDetails));

                    CreateMap<SaleDetail, SaleDetailDto>()
                        .ForMember(dest => dest.ProductName,
                            opt => opt.MapFrom(src => src.Product != null
                                ? src.Product.Name : string.Empty));

            CreateMap<User, UserDto>()
                .ForMember(dest => dest.Role,
                    opt => opt.MapFrom(src => src.Role.ToString()));

            //Credit
            CreateMap<Credit, CreditDto>()
                .ForMember(dest => dest.Status,
                    opt => opt.MapFrom(src => src.Status.ToString()))
                .ForMember(dest => dest.CustomerName,
                    opt => opt.MapFrom(src => src.Customer != null
                        ? src.Customer.Name : string.Empty))
                .ForMember(dest => dest.SaleNumber,
                    opt => opt.MapFrom(src => src.Sale != null
                        ? src.Sale.SaleNumber : string.Empty));


            //Stock
            CreateMap<StockMovement, StockMovementDto>()
                .ForMember(dest => dest.Type,
                    opt => opt.MapFrom(src => src.Type.ToString()))
                .ForMember(dest => dest.ProductName,
                    opt => opt.MapFrom(src => src.Product != null
                        ? src.Product.Name : string.Empty))
                .ForMember(dest => dest.UserName,
                    opt => opt.MapFrom(src => src.User != null
                        ? src.User.FullName : string.Empty));
        }
    }
}