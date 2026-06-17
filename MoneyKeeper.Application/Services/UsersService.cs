using Microsoft.Extensions.Logging;
using MoneyKeeper.Application.Common;
using MoneyKeeper.Application.Common.Auth;
using MoneyKeeper.Application.Common.Services;
using MoneyKeeper.Application.Contracts.User;
using MoneyKeeper.Core.Common;
using MoneyKeeper.Core.Common.Repositories;
using MoneyKeeper.Core.Models;

namespace MoneyKeeper.Application.Services
{
    public class UsersService : IUsersService
    {
        private readonly IUsersRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtService _jwtService;
        private readonly ILogger<UsersService> _logger;

        public UsersService(IUsersRepository userRepository, IPasswordHasher passwordHasher, IJwtService jwtService, ILogger<UsersService> logger)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _jwtService = jwtService;
            _logger = logger;
        }

        public async Task<Result<AuthenticationResult>> Register(RegisterUserCommand command, CancellationToken cancellationToken)
        {
            if (_userRepository.GetByLoginAsync(command.Login) is not null)
            {
                return Result<AuthenticationResult>.Failure
                    (Error.Conflict("Логин уже занят", ErrorCodes.LOGIN_ALREADY_EXISTS));
            }

            User user = new User
            {
                Login = command.Login,
                UserName = command.Name,
            };
            string passwordHash = _passwordHasher.Hash(command.Password);
            user.Password = passwordHash;
            await _userRepository.AddAsync(user, cancellationToken);
            string token = _jwtService.Generate(user);
            AuthenticationResult response = new AuthenticationResult(user.Login, user.UserName, user.Id, user.CreatedAt, token);

            _logger.LogInformation("Создан пользователь с id {UserId} и логином {Login}", response.Id, response.Login);
            return Result<AuthenticationResult>.Success(response);
        }

        public async Task<Result<AuthenticationResult>> Login(LoginUserCommand command, CancellationToken cancellationToken)
        {
            User? user = await _userRepository.GetByLoginAsync(command.Login, cancellationToken);
            if (user is null || !_passwordHasher.Verify(user.Password, command.Password))
            {
                return Result<AuthenticationResult>.Failure
                    (Error.Unauthorized("Неверный логин или пароль", ErrorCodes.INVALID_LOGIN_OR_PASSWORD));
            }
            string token = _jwtService.Generate(user);
            AuthenticationResult response = new AuthenticationResult(user.Login, user.UserName, user.Id, user.CreatedAt, token);

            _logger.LogInformation("Пользователь с id {UserId} и логином {Login} вошел в свою учётную запись", response.Id, response.Login);
            return Result<AuthenticationResult>.Success(response);
        }
    }
}
