using AutoMapper;
using NexusAs.Application.DTOs.Categories;
using NexusAs.Application.DTOs.Common;
using NexusAs.Application.Interfaces;
using NexusAs.Domain.Entities;
using NexusAs.Domain.Exceptions;

namespace NexusAs.Application.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public CategoryService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<PagedResponseDto<CategoryDto>> GetAllAsync(
            int pageNumber, int pageSize, string? search = null)
        {
            var categories = await _unitOfWork.Categories.FindAsync(c =>
                c.IsActive &&
                (search == null || c.Name.Contains(search)));

            var totalRecords = categories.Count();
            var paged = categories
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize);

            return new PagedResponseDto<CategoryDto>
            {
                Data = _mapper.Map<IEnumerable<CategoryDto>>(paged),
                TotalRecords = totalRecords,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<CategoryDto?> GetByIdAsync(int id)
        {
            var category = await _unitOfWork.Categories.GetByIdAsync(id);
            if (category == null)
                throw new NotFoundException(nameof(Category), id);
            return _mapper.Map<CategoryDto>(category);
        }

        public async Task<CategoryDto> CreateAsync(CreateCategoryDto dto)
        {
            var exists = await _unitOfWork.Categories
                .ExistsAsync(c => c.Name == dto.Name && c.IsActive);
            if (exists)
                throw new BusinessException($"Ya existe una categoría con el nombre '{dto.Name}'.");

            var category = _mapper.Map<Category>(dto);
            await _unitOfWork.Categories.AddAsync(category);
            await _unitOfWork.SaveChangesAsync();
            return _mapper.Map<CategoryDto>(category);
        }

        public async Task<CategoryDto> UpdateAsync(int id, UpdateCategoryDto dto)
        {
            var category = await _unitOfWork.Categories.GetByIdAsync(id);
            if (category == null)
                throw new NotFoundException(nameof(Category), id);

            var exists = await _unitOfWork.Categories
                .ExistsAsync(c => c.Name == dto.Name && c.Id != id && c.IsActive);
            if (exists)
                throw new BusinessException($"Ya existe una categoría con el nombre '{dto.Name}'.");

            _mapper.Map(dto, category);
            _unitOfWork.Categories.Update(category);
            await _unitOfWork.SaveChangesAsync();
            return _mapper.Map<CategoryDto>(category);
        }

        public async Task DeleteAsync(int id)
        {
            var category = await _unitOfWork.Categories.GetByIdAsync(id);
            if (category == null)
                throw new NotFoundException(nameof(Category), id);

            var hasProducts = await _unitOfWork.Products
                .ExistsAsync(p => p.CategoryId == id && p.IsActive);
            if (hasProducts)
                throw new BusinessException("No se puede eliminar una categoría que tiene productos activos.");

            _unitOfWork.Categories.Delete(category);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}