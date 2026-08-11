using AutoMapper;
using NexusAs.Application.DTOs.Categories;
using NexusAs.Application.DTOs.Customers;
using NexusAs.Application.DTOs.Products;
using NexusAs.Domain.Entities;
using NexusAs.Application.DTOs.Sales;
using NexusAs.Application.DTOs.Credits;
using NexusAs.Application.DTOs.Stock;
using NexusAs.Application.DTOs.Users;
using NexusAs.Application.DTOs.Suppliers;
using NexusAs.Application.DTOs.PaymentMethods;
using NexusAs.Application.DTOs.Returns;
using NexusAs.Application.DTOs.Partners;

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

            // PASO 1: PartnerConfig mappings con mapeo explícito de UserId
            CreateMap<PartnerConfig, PartnerConfigDto>()
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId))
                .ForMember(dest => dest.PartnerName, opt => opt.MapFrom(src => src.User != null ? src.User.FullName : string.Empty))
                .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.User != null ? src.User.Username : string.Empty));

            // Supplier mappings
            CreateMap<Supplier, SupplierDto>();
            CreateMap<CreateSupplierDto, Supplier>();
            CreateMap<UpdateSupplierDto, Supplier>();

            // PaymentMethod mappings
            CreateMap<PaymentMethodEntity, PaymentMethodDto>();

            // Product mappings
            CreateMap<Product, ProductDto>()
                .ForMember(dest => dest.CategoryName,
                    opt => opt.MapFrom(src => src.Category != null
                        ? src.Category.Name
                        : string.Empty))
                .ForMember(dest => dest.SupplierName,
                    opt => opt.MapFrom(src => src.Supplier != null
                        ? src.Supplier.Name
                        : null));
            
            // TAREA 5: Mapeo explícito de SupplierId para Create y Update
            CreateMap<CreateProductDto, Product>()
                .ForMember(dest => dest.SupplierId, opt => opt.MapFrom(src => src.SupplierId));
            
            CreateMap<UpdateProductDto, Product>()
                .ForMember(dest => dest.SupplierId, opt => opt.MapFrom(src => src.SupplierId));

            // Customer mappings
            CreateMap<Customer, CustomerDto>();
            CreateMap<CreateCustomerDto, Customer>();
            CreateMap<UpdateCustomerDto, Customer>();

            CreateMap<Sale, SaleDto>()
            .ForMember(dest => dest.PaymentMethodName,
                opt => opt.MapFrom(src => src.PaymentMethodEntity != null
                    ? src.PaymentMethodEntity.Name : string.Empty))
            .ForMember(dest => dest.CustomerName,
                opt => opt.MapFrom(src => src.Customer != null
                    ? src.Customer.Name : null))
            .ForMember(dest => dest.SellerName,
                opt => opt.MapFrom(src => src.User != null
                    ? src.User.FullName : string.Empty))
            .ForMember(dest => dest.Status,
                opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.Details,
                opt => opt.MapFrom(src => src.SaleDetails))
            .ForMember(dest => dest.Returns,
                opt => opt.MapFrom(src => src.Returns.Where(r => r.IsActive)));

                    CreateMap<SaleDetail, SaleDetailDto>()
                        .ForMember(dest => dest.ProductCode,
                            opt => opt.MapFrom(src => src.Product != null
                                ? src.Product.Code : string.Empty))
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

            //Return
            CreateMap<Return, ReturnDto>()
                .ForMember(dest => dest.Type,
                    opt => opt.MapFrom(src => src.Type.ToString()))
                .ForMember(dest => dest.SaleNumber,
                    opt => opt.MapFrom(src => src.Sale != null
                        ? src.Sale.SaleNumber : string.Empty))
                .ForMember(dest => dest.UserName,
                    opt => opt.MapFrom(src => src.User != null
                        ? src.User.FullName : string.Empty))
                .ForMember(dest => dest.Details,
                    opt => opt.MapFrom(src => src.ReturnDetails));

            CreateMap<ReturnDetail, ReturnDetailDto>()
                .ForMember(dest => dest.ProductName,
                    opt => opt.MapFrom(src => src.Product != null
                        ? src.Product.Name : string.Empty));
        }
    }
}