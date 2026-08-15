using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyKeeper.Application.Common.Auth;
using MoneyKeeper.Application.Common.Services;
using MoneyKeeper.Application.Contracts.User;
using MoneyKeeper.Contracts.User;
using MoneyKeeper.Core.Common;
using MoneyKeeper.Extensions;

namespace MoneyKeeper.Controllers
{
    [Route("api/user")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUsersService _userService;
        private readonly ILogger<UsersController> _logger;
        private readonly ICurrentUserService _currentUserService;

        public UsersController(IUsersService userService, ILogger<UsersController> logger, ICurrentUserService currentUserService) 
        { 
            _userService = userService;
            _logger = logger;
            _currentUserService = currentUserService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterUserRequest request, CancellationToken cancellationToken)
        {
            Result<AuthenticationResult> result = 
                await _userService.Register(new RegisterUserCommand(request.Login, request.Name, request.Password), cancellationToken);

            if (result.IsSuccess)
            {
                SetTokenCookie(HttpContext, result.Value.Token);

                return Ok(new UserResponse(result.Value.Id, result.Value.Name, result.Value.Login, result.Value.CreatedAt));
            }

            _logger.LogWarning
                ("Ошибка регистрации нового пользователя с логином {Login}: {ErrorCode}", request.Login, result.Error!.ErrorCode);
            return result.ToErrorActionResult(this);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginUserRequest request, CancellationToken cancellationToken)
        {
            Result<AuthenticationResult> result =
                await _userService.Login(new LoginUserCommand(request.Login, request.Password), cancellationToken);

            if (result.IsSuccess)
            {
                SetTokenCookie(HttpContext, result.Value.Token);

                return Ok(new UserResponse(result.Value.Id, result.Value.Name, result.Value.Login, result.Value.CreatedAt));
            }

            _logger.LogWarning
                ("Ошибка входа пользователя с логином {Login}: {ErrorCode}", request.Login, result.Error!.ErrorCode);
            return result.ToErrorActionResult(this);
        }

        [HttpPost("logout")]
        [Authorize]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("tasty-cookies", new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(7)
            });

            _logger.LogInformation("Пользователь с id {UserId} вышел со свой учётной записи", _currentUserService.UserId);
            return NoContent();
        }

        private void SetTokenCookie(HttpContext context, string token)
        {
            context.Response.Cookies.Append("tasty-cookies", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(7)
            });
        }
    }
}
