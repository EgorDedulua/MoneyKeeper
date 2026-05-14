using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
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

        public UsersController(IUsersService userService) 
        { 
            _userService = userService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterUserRequest request, IValidator<RegisterUserRequest> validator, CancellationToken cancellationToken)
        {
            ValidationResult validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
                return validationResult.ToErrorActionResult();

            Result<AuthenticationResult> result = 
                await _userService.Register(new RegisterUserCommand(request.Login, request.Name, request.Password), cancellationToken);

            if (result.IsSuccess)
            {
                SetTokenCookie(HttpContext, result.Value.Token);

                return Ok(new UserResponse(result.Value.Id, result.Value.Name, result.Value.Login, result.Value.CreatedAt));
            }
            return result.ToErrorActionResult();
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginUserRequest request, IValidator<LoginUserRequest> validator, CancellationToken cancellationToken)
        {
            ValidationResult validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
                return validationResult.ToErrorActionResult();

            Result<AuthenticationResult> result =
                await _userService.Login(new LoginUserCommand(request.Login, request.Password), cancellationToken);

            if (result.IsSuccess)
            {
                SetTokenCookie(HttpContext, result.Value.Token);

                return Ok(new UserResponse(result.Value.Id, result.Value.Name, result.Value.Login, result.Value.CreatedAt));
            }
            return result.ToErrorActionResult();
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
