using AutoMapper;
using NexusAs.Application.DTOs.Common;
using NexusAs.Application.DTOs.Suppliers;
using NexusAs.Application.Interfaces;
using NexusAs.Domain.Entities;
using NexusAs.Domain.Exceptions;

namespace NexusAs.Application.Services
{
    public class SupplierService : ISupplierService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public SupplierService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<PagedResponseDto<SupplierDto>> GetAllAsync(
            int pageNumber, int pageSize, string? search = null)
        {
            var suppliers = await _unitOfWork.Suppliers.FindAsync(s =>
                s.IsActive &&
                (search == null || s.Name.Contains(search)));

            var totalRecords = suppliers.Count();
            var paged = suppliers
                .OrderBy(s => s.Name)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize);

            return new PagedResponseDto<SupplierDto>
            {
                Data = _mapper.Map<IEnumerable<SupplierDto>>(paged),
                TotalRecords = totalRecords,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<SupplierDto?> GetByIdAsync(int id)
        {
            var supplier = await _unitOfWork.Suppliers.GetByIdAsync(id);
            if (supplier == null)
                throw new NotFoundException(nameof(Supplier), id);
            return _mapper.Map<SupplierDto>(supplier);
        }

        public async Task<SupplierDto> CreateAsync(CreateSupplierDto dto)
        {
            var exists = await _unitOfWork.Suppliers
                .ExistsAsync(s => s.Name == dto.Name && s.IsActive);
            if (exists)
                throw new BusinessException($"Ya existe un proveedor con el nombre '{dto.Name}'.");

            var supplier = _mapper.Map<Supplier>(dto);
            await _unitOfWork.Suppliers.AddAsync(supplier);
            await _unitOfWork.SaveChangesAsync();
            return _mapper.Map<SupplierDto>(supplier);
        }

        public async Task<SupplierDto> UpdateAsync(int id, UpdateSupplierDto dto)
        {
            var supplier = await _unitOfWork.Suppliers.GetByIdAsync(id);
            if (supplier == null)
                throw new NotFoundException(nameof(Supplier), id);

            var exists = await _unitOfWork.Suppliers
                .ExistsAsync(s => s.Name == dto.Name && s.Id != id && s.IsActive);
            if (exists)
                throw new BusinessException($"Ya existe un proveedor con el nombre '{dto.Name}'.");

            _mapper.Map(dto, supplier);
            _unitOfWork.Suppliers.Update(supplier);
            await _unitOfWork.SaveChangesAsync();
            return _mapper.Map<SupplierDto>(supplier);
        }

        public async Task DeleteAsync(int id)
        {
            var supplier = await _unitOfWork.Suppliers.GetByIdAsync(id);
            if (supplier == null)
                throw new NotFoundException(nameof(Supplier), id);

            // Soft delete: solo marcar como inactivo
            _unitOfWork.Suppliers.Delete(supplier);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
