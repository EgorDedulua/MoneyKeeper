using Microsoft.AspNetCore.Http;
using MoneyKeeper.Application.Common.Auth;
using System.Security.Claims;

namespace MoneyKeeper.Infrastructure.Auth
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public int UserId
        {
            get
            {
                ClaimsPrincipal? user = _httpContextAccessor.HttpContext?.User;

                if (user?.Identity?.IsAuthenticated != true)
                    throw new UnauthorizedAccessException("Требуется аутентификация!");

                string? userIdClaim = user.FindFirstValue("id");

                if (userIdClaim is null)
                    throw new UnauthorizedAccessException("Claim 'id' не найден");

                if (!int.TryParse(userIdClaim, out int userId) || userId < 0)
                    throw new UnauthorizedAccessException("Неверный формат id пользователя");

                return userId;
            }
        }
    }
}
