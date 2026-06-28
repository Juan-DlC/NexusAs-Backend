using AutoMapper;
using NexusAs.Application.DTOs.Users;
using NexusAs.Application.Interfaces;
using NexusAs.Domain.Entities;
using NexusAs.Domain.Enums;
using NexusAs.Domain.Exceptions;

namespace NexusAs.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public UserService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<IEnumerable<UserDto>> GetAllAsync(bool includeInactive = false)
        {
            var users = includeInactive
                ? await _unitOfWork.Users.FindAsync(u => true)
                : await _unitOfWork.Users.GetAllAsync();

            return _mapper.Map<IEnumerable<UserDto>>(users);
        }

        public async Task<UserDto?> GetByIdAsync(int id)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(id);
            if (user == null)
                throw new NotFoundException(nameof(User), id);
            return _mapper.Map<UserDto>(user);
        }

        public async Task<UserDto> CreateAsync(CreateUserDto dto)
        {
            var exists = await _unitOfWork.Users
                .ExistsAsync(u => u.Username == dto.Username);
            if (exists)
                throw new BusinessException(
                    $"Ya existe un usuario con el nombre '{dto.Username}'.");
            if (!Enum.TryParse<UserRole>(dto.Role, out var role))
                throw new BusinessException(
                    "Rol inválido. Use: Admin, Seller o Partner.");
            var user = new User
            {
                Username = dto.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                FullName = dto.FullName,
                Role = role
            };
            await _unitOfWork.Users.AddAsync(user);
            await _unitOfWork.SaveChangesAsync();

            if (role == UserRole.Partner)
            {
                var partnerConfig = new PartnerConfig
                {
                    UserId = user.Id,
                    CommissionPercent = 50
                };
                await _unitOfWork.PartnerConfigs.AddAsync(partnerConfig);
                await _unitOfWork.SaveChangesAsync();
            }

            return _mapper.Map<UserDto>(user);
        }

        public async Task<UserDto> ToggleStatusAsync(int id)
        {
            var user = await _unitOfWork.Users.GetByIdIncludingInactiveAsync(id);
            if (user == null)
                throw new NotFoundException(nameof(User), id);

            user.IsActive = !user.IsActive;
            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync();
            return _mapper.Map<UserDto>(user);
        }

        public async Task ChangePasswordAsync(int userId, ChangePasswordDto dto)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user == null)
                throw new NotFoundException(nameof(User), userId);

            if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
                throw new BusinessException("La contraseña actual es incorrecta.");

            if (dto.NewPassword != dto.ConfirmPassword)
                throw new BusinessException(
                    "La nueva contraseña y la confirmación no coinciden.");

            if (dto.NewPassword.Length < 6)
                throw new BusinessException(
                    "La nueva contraseña debe tener mínimo 6 caracteres.");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}