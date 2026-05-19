using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using NexusAs.Application.DTOs.Auth;
using NexusAs.Application.Interfaces;
using NexusAs.Domain.Entities;
using NexusAs.Domain.Enums;
using NexusAs.Domain.Exceptions;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace NexusAs.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _configuration;

        public AuthService(IUnitOfWork unitOfWork, IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _configuration = configuration;
        }

        public async Task<TokenDto> LoginAsync(LoginDto dto)
        {
            var users = await _unitOfWork.Users
                .FindAsync(u => u.Username == dto.Username && u.IsActive);
            var user = users.FirstOrDefault();

            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                throw new BusinessException("Usuario o contraseña incorrectos.");

            return GenerateToken(user);
        }

        public async Task<TokenDto> CreateUserAsync(
            string username, string password, string fullName, string role)
        {
            var exists = await _unitOfWork.Users
                .ExistsAsync(u => u.Username == username);
            if (exists)
                throw new BusinessException($"El usuario '{username}' ya existe.");

            var userRole = role == "Admin" ? UserRole.Admin : UserRole.Seller;

            var user = new User
            {
                Username = username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                FullName = fullName,
                Role = userRole
            };

            await _unitOfWork.Users.AddAsync(user);
            await _unitOfWork.SaveChangesAsync();
            return GenerateToken(user);
        }

        private TokenDto GenerateToken(User user)
        {
            var secretKey = _configuration["JwtSettings:SecretKey"]!;
            var issuer = _configuration["JwtSettings:Issuer"]!;
            var audience = _configuration["JwtSettings:Audience"]!;
            var expirationHours = int.Parse(
                _configuration["JwtSettings:ExpirationHours"] ?? "8");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiration = DateTime.UtcNow.AddHours(expirationHours);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.GivenName, user.FullName),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: expiration,
                signingCredentials: credentials);

            return new TokenDto
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                Username = user.Username,
                FullName = user.FullName,
                Role = user.Role.ToString(),
                Expiration = expiration
            };
        }
    }
}