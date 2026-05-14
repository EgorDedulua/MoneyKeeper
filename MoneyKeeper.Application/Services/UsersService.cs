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

        public UsersService(IUsersRepository userRepository, IPasswordHasher passwordHasher, IJwtService jwtService)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _jwtService = jwtService;
        }

        public async Task<Result<AuthenticationResult>> Register(RegisterUserCommand command, CancellationToken cancellationToken)
        {
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
            return Result<AuthenticationResult>.Success(response);
        }

        public async Task<Result<AuthenticationResult>> Login(LoginUserCommand command, CancellationToken cancellationToken)
        {
            User user = (await _userRepository.GetByLoginAsync(command.Login, cancellationToken))!;
            string token = _jwtService.Generate(user);
            AuthenticationResult response = new AuthenticationResult(user.Login, user.UserName, user.Id, user.CreatedAt, token);
            return Result<AuthenticationResult>.Success(response);
        }
    }
}
