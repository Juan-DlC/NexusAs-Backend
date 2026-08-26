using AutoMapper;
using NexusAs.Application.DTOs.Common;
using NexusAs.Application.DTOs.Products;
using NexusAs.Application.Interfaces;
using NexusAs.Domain.Entities;
using NexusAs.Domain.Exceptions;

namespace NexusAs.Application.Services
{
    public class ProductService : IProductService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public ProductService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<PagedResponseDto<ProductDto>> GetAllAsync(
            int pageNumber, int pageSize, string? search = null,
            int? categoryId = null, bool? isPartnership = null,
            bool? inStock = null, bool? isActiveFilter = null)
        {
            var (products, totalRecords) = await _unitOfWork.Products.GetAllPagedAsync(
                search, categoryId, isPartnership, pageNumber, pageSize, inStock, isActiveFilter);

            return new PagedResponseDto<ProductDto>
            {
                Data = _mapper.Map<IEnumerable<ProductDto>>(products),
                TotalRecords = totalRecords,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<ProductDto?> GetByIdAsync(int id)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(id);
            if (product == null)
                throw new NotFoundException(nameof(Product), id);
            return _mapper.Map<ProductDto>(product);
        }

        public async Task<IEnumerable<ProductDto>> GetLowStockAsync()
        {
            var products = await _unitOfWork.Products
                .FindAsync(p => p.IsActive && p.Stock <= p.MinStock);
            return _mapper.Map<IEnumerable<ProductDto>>(products);
        }

        public async Task<ProductDto> CreateAsync(CreateProductDto dto)
        {
            var exists = await _unitOfWork.Products
                .ExistsAsync(p => p.Code == dto.Code && p.IsActive);
            if (exists)
                throw new BusinessException($"Ya existe un producto con el código '{dto.Code}'.");

            var categoryExists = await _unitOfWork.Categories
                .ExistsAsync(c => c.Id == dto.CategoryId && c.IsActive);
            if (!categoryExists)
                throw new BusinessException("La categoría seleccionada no existe.");

            // Validar proveedor si se proporciona
            if (dto.SupplierId.HasValue)
            {
                var supplierExists = await _unitOfWork.Suppliers
                    .ExistsAsync(s => s.Id == dto.SupplierId.Value && s.IsActive);
                if (!supplierExists)
                    throw new BusinessException("El proveedor seleccionado no existe.");
            }

            // Validar BusinessPartner si se proporciona
            if (dto.BusinessPartnerId.HasValue)
            {
                var businessPartnerExists = await _unitOfWork.BusinessPartners
                    .ExistsAsync(bp => bp.Id == dto.BusinessPartnerId.Value && bp.IsActive);
                if (!businessPartnerExists)
                    throw new BusinessException("El socio comercial seleccionado no existe.");
            }

            var product = _mapper.Map<Product>(dto);
            
            // Sincronizar IsPartnership con BusinessPartnerId
            product.IsPartnership = dto.BusinessPartnerId.HasValue;
            
            await _unitOfWork.Products.AddAsync(product);
            await _unitOfWork.SaveChangesAsync();
            return _mapper.Map<ProductDto>(product);
        }

        public async Task<ProductDto> UpdateAsync(int id, UpdateProductDto dto)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(id);
            if (product == null)
                throw new NotFoundException(nameof(Product), id);

            var exists = await _unitOfWork.Products
                .ExistsAsync(p => p.Code == dto.Code && p.Id != id && p.IsActive);
            if (exists)
                throw new BusinessException($"Ya existe un producto con el código '{dto.Code}'.");

            var categoryExists = await _unitOfWork.Categories
                .ExistsAsync(c => c.Id == dto.CategoryId && c.IsActive);
            if (!categoryExists)
                throw new BusinessException("La categoría seleccionada no existe.");

            // Validar proveedor si se proporciona
            if (dto.SupplierId.HasValue)
            {
                var supplierExists = await _unitOfWork.Suppliers
                    .ExistsAsync(s => s.Id == dto.SupplierId.Value && s.IsActive);
                if (!supplierExists)
                    throw new BusinessException("El proveedor seleccionado no existe.");
            }

            // Validar BusinessPartner si se proporciona
            if (dto.BusinessPartnerId.HasValue)
            {
                var businessPartnerExists = await _unitOfWork.BusinessPartners
                    .ExistsAsync(bp => bp.Id == dto.BusinessPartnerId.Value && bp.IsActive);
                if (!businessPartnerExists)
                    throw new BusinessException("El socio comercial seleccionado no existe.");
            }

            _mapper.Map(dto, product);
            
            // Sincronizar IsPartnership con BusinessPartnerId
            product.IsPartnership = dto.BusinessPartnerId.HasValue;
            
            _unitOfWork.Products.Update(product);
            await _unitOfWork.SaveChangesAsync();
            return _mapper.Map<ProductDto>(product);
        }

        public async Task DeleteAsync(int id)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(id);
            if (product == null)
                throw new NotFoundException(nameof(Product), id);

            _unitOfWork.Products.Delete(product);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<ProductDto> ToggleStatusAsync(int id)
        {
            var product = await _unitOfWork.Products.GetByIdIncludingInactiveAsync(id);
            if (product == null)
                throw new NotFoundException(nameof(Product), id);

            product.IsActive = !product.IsActive;
            _unitOfWork.Products.Update(product);
            await _unitOfWork.SaveChangesAsync();
            
            return _mapper.Map<ProductDto>(product);
        }
    }
}