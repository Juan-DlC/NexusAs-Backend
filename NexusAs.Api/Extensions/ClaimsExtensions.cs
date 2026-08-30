using System.Security.Claims;

namespace NexusAs.Api.Extensions
{
    public static class ClaimsExtensions
    {
        /// <summary>
        /// Obtiene el UserId del ClaimsPrincipal de forma segura
        /// </summary>
        /// <param name="user">ClaimsPrincipal del usuario autenticado</param>
        /// <returns>UserId si existe, null si no</returns>
        public static int? GetUserId(this ClaimsPrincipal user)
        {
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return null;

            if (int.TryParse(userIdClaim, out int userId))
                return userId;

            return null;
        }

        /// <summary>
        /// Obtiene el UserId del ClaimsPrincipal o lanza excepción si no existe
        /// </summary>
        /// <param name="user">ClaimsPrincipal del usuario autenticado</param>
        /// <returns>UserId</returns>
        /// <exception cref="UnauthorizedAccessException">Si no hay userId válido</exception>
        public static int GetUserIdOrThrow(this ClaimsPrincipal user)
        {
            var userId = user.GetUserId();
            if (!userId.HasValue)
                throw new UnauthorizedAccessException("Usuario no autenticado o token inválido.");

            return userId.Value;
        }

        /// <summary>
        /// Obtiene el Role del usuario de forma segura
        /// </summary>
        /// <param name="user">ClaimsPrincipal del usuario autenticado</param>
        /// <returns>Rol del usuario o null si no existe</returns>
        public static string? GetUserRole(this ClaimsPrincipal user)
        {
            return user.FindFirst(ClaimTypes.Role)?.Value;
        }

        /// <summary>
        /// Obtiene el Role del usuario o lanza excepción si no existe
        /// </summary>
        /// <param name="user">ClaimsPrincipal del usuario autenticado</param>
        /// <returns>Rol del usuario</returns>
        /// <exception cref="UnauthorizedAccessException">Si no hay rol válido</exception>
        public static string GetUserRoleOrThrow(this ClaimsPrincipal user)
        {
            var role = user.GetUserRole();
            if (string.IsNullOrEmpty(role))
                throw new UnauthorizedAccessException("Usuario no tiene rol asignado.");

            return role;
        }

        /// <summary>
        /// Verifica si el usuario tiene un rol específico
        /// </summary>
        /// <param name="user">ClaimsPrincipal del usuario autenticado</param>
        /// <param name="role">Rol a verificar</param>
        /// <returns>True si el usuario tiene el rol, false si no</returns>
        public static bool HasRole(this ClaimsPrincipal user, string role)
        {
            return user.IsInRole(role);
        }

        /// <summary>
        /// Verifica si el usuario es Admin
        /// </summary>
        public static bool IsAdmin(this ClaimsPrincipal user)
        {
            return user.HasRole("Admin");
        }

        /// <summary>
        /// Verifica si el usuario es Partner
        /// </summary>
        public static bool IsPartner(this ClaimsPrincipal user)
        {
            return user.HasRole("Partner");
        }

        /// <summary>
        /// Verifica si el usuario es Seller
        /// </summary>
        public static bool IsSeller(this ClaimsPrincipal user)
        {
            return user.HasRole("Seller");
        }
    }
}
