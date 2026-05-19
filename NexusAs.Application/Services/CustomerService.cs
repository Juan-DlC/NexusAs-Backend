using AutoMapper;
using NexusAs.Application.DTOs.Common;
using NexusAs.Application.DTOs.Customers;
using NexusAs.Application.Interfaces;
using NexusAs.Domain.Entities;
using NexusAs.Domain.Exceptions;

namespace NexusAs.Application.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public CustomerService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<PagedResponseDto<CustomerDto>> GetAllAsync(
    int pageNumber, int pageSize, string? search = null)
        {
            var customers = await _unitOfWork.Customers.FindAsync(c =>
                c.IsActive &&
                (search == null ||
                 c.Name.Contains(search) ||
                 (c.Phone != null && c.Phone.Contains(search)) ||
                 (c.Document != null && c.Document.Contains(search))));

            var totalRecords = customers.Count();
            var paged = customers
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize);

            return new PagedResponseDto<CustomerDto>
            {
                Data = _mapper.Map<IEnumerable<CustomerDto>>(paged),
                TotalRecords = totalRecords,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<CustomerDto?> GetByIdAsync(int id)
        {
            var customer = await _unitOfWork.Customers.GetByIdAsync(id);
            if (customer == null)
                throw new NotFoundException(nameof(Customer), id);
            return _mapper.Map<CustomerDto>(customer);
        }

        public async Task<CustomerDto> CreateAsync(CreateCustomerDto dto)
        {
            if (dto.Document != null)
            {
                var exists = await _unitOfWork.Customers
                    .ExistsAsync(c => c.Document == dto.Document && c.IsActive);
                if (exists)
                    throw new BusinessException(
                        $"Ya existe un cliente con el documento '{dto.Document}'.");
            }

            var customer = _mapper.Map<Customer>(dto);
            await _unitOfWork.Customers.AddAsync(customer);
            await _unitOfWork.SaveChangesAsync();
            return _mapper.Map<CustomerDto>(customer);
        }

        public async Task<CustomerDto> UpdateAsync(int id, UpdateCustomerDto dto)
        {
            var customer = await _unitOfWork.Customers.GetByIdAsync(id);
            if (customer == null)
                throw new NotFoundException(nameof(Customer), id);

            if (dto.Document != null)
            {
                var exists = await _unitOfWork.Customers
                    .ExistsAsync(c => c.Document == dto.Document &&
                                     c.Id != id && c.IsActive);
                if (exists)
                    throw new BusinessException(
                        $"Ya existe un cliente con el documento '{dto.Document}'.");
            }

            _mapper.Map(dto, customer);
            _unitOfWork.Customers.Update(customer);
            await _unitOfWork.SaveChangesAsync();
            return _mapper.Map<CustomerDto>(customer);
        }

        public async Task DeleteAsync(int id)
        {
            var customer = await _unitOfWork.Customers.GetByIdAsync(id);
            if (customer == null)
                throw new NotFoundException(nameof(Customer), id);

            _unitOfWork.Customers.Delete(customer);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}