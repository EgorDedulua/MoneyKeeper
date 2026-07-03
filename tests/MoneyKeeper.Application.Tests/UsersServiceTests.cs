using FluentAssertions;
using Microsoft.Extensions.Logging;
using MoneyKeeper.Application.Common;
using MoneyKeeper.Application.Common.Auth;
using MoneyKeeper.Application.Contracts.User;
using MoneyKeeper.Application.Services;
using MoneyKeeper.Core.Common;
using MoneyKeeper.Core.Common.Repositories;
using MoneyKeeper.Core.Models;
using MoneyKeeper.TestsCommons.MockHelpers;
using Moq;
using System.Net;

namespace MoneyKeeper.Application.Tests
{
    public class UsersServiceTests
    {
        private readonly Mock<IUsersRepository> _usersRepositoryMock = new();
        private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
        private readonly Mock<IJwtService> _jwtServiceMock = new();
        private readonly Mock<ILogger<UsersService>> _loggerMock = new();

        private UsersService CreateService()
        {
            return new UsersService(
                _usersRepositoryMock.Object,
                _passwordHasherMock.Object,
                _jwtServiceMock.Object,
                _loggerMock.Object
            );
        }

        [Fact]
        public async Task Register_WhenEverythingIsCorrect_RegistersUser()
        {
            RegisterUserCommand command = UsersServiceMockHelper.RegisterUserCommand;
            _usersRepositoryMock.SetupGetByLogin(command.Login, null);
            string token = "token";
            string hash = "hash";
            _passwordHasherMock.Setup(p => p.Hash(command.Password)).Returns(hash);
            _jwtServiceMock.Setup(j => j.Generate(It.IsAny<User>())).Returns(token);
            UsersService service = CreateService();

            Result<AuthenticationResult> result = await service.Register(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Error.Should().BeNull();
            result.Value.Name.Should().Be(command.Name);
            result.Value.Token.Should().Be(token);
            result.Value.Login.Should().Be(command.Login);
            _passwordHasherMock
                .Verify(h => h.Hash(command.Password), Times.Once);
            _usersRepositoryMock
                .Verify(r => r.AddAsync(It.Is<User>(u =>
                    u.Password == hash &&
                    u.Login == command.Login &&
                    u.UserName == command.Name
                )), Times.Once());
            _jwtServiceMock
                .Verify(j => j.Generate(It.Is<User>(u =>
                    u.Password == hash &&
                    u.Login == command.Login &&
                    u.UserName == command.Name
                )), Times.Once());
        }

        [Fact]
        public async Task Register_WhenUserLoginIsTaken_ReturnsConflictError()
        {
            RegisterUserCommand command = UsersServiceMockHelper.RegisterUserCommand;
            _usersRepositoryMock.SetupGetByLogin(command.Login, new User
            {
                Login = command.Login,
                Password = command.Password,
                UserName = command.Name
            });
            UsersService service = CreateService();

            Result<AuthenticationResult> result = await service.Register(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.Error.StatusCode.Should().Be((int)HttpStatusCode.Conflict);
            result.Error.ErrorCode.Should().Be(ErrorCodes.LOGIN_ALREADY_EXISTS);
            _passwordHasherMock
                .Verify(h => h.Hash(command.Password), Times.Never);
            _usersRepositoryMock
                .Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
            _jwtServiceMock
                .Verify(j => j.Generate(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task Login_WhenEverythingIsCorrect_LoginsUser()
        {
            LoginUserCommand command = UsersServiceMockHelper.LoginUserCommand;
            string hash = "hash";
            User user = new User
            {
                Login = command.Login,
                Password = hash,
                UserName = "Name"
            };
            _usersRepositoryMock.SetupGetByLogin(command.Login, user);
            _passwordHasherMock.SetupVerifyPassword(user.Password, command.Password, true);
            string token = "token";
            _jwtServiceMock.Setup(j => j.Generate(It.IsAny<User>())).Returns(token);
            UsersService service = CreateService();

            Result<AuthenticationResult> result = await service.Login(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Error.Should().BeNull();
            result.Value.Name.Should().Be(user.UserName);
            result.Value.Token.Should().Be(token);
            result.Value.Login.Should().Be(command.Login);
            _passwordHasherMock
                .Verify(h => h.Verify(user.Password, command.Password), Times.Once);
            _jwtServiceMock
                .Verify(j => j.Generate(It.Is<User>(u =>
                    u.Password == hash &&
                    u.Login == command.Login &&
                    u.UserName == user.UserName
                )), Times.Once());
        }

        [Fact]
        public async Task Login_WhenUserExistsButPasswordIsIncorrect_ReturnsUnauthorizedError()
        {
            LoginUserCommand command = UsersServiceMockHelper.LoginUserCommand;
            string hash = "hash";
            User user = new User
            {
                Login = command.Login,
                Password = hash,
                UserName = "Name"
            };
            _usersRepositoryMock.SetupGetByLogin(command.Login, user);
            _passwordHasherMock.SetupVerifyPassword(user.Password, command.Password, false);
            UsersService service = CreateService();

            Result<AuthenticationResult> result = await service.Login(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.Error.StatusCode.Should().Be((int)HttpStatusCode.Unauthorized);
            result.Error.ErrorCode.Should().Be(ErrorCodes.INVALID_LOGIN_OR_PASSWORD);
            _passwordHasherMock
                .Verify(h => h.Verify(user.Password, command.Password), Times.Once);
        }

        [Fact]
        public async Task Login_WhenUserDoesNotExist_ReturnsUnauthorizedError()
        {
            LoginUserCommand command = UsersServiceMockHelper.LoginUserCommand;
            _usersRepositoryMock.SetupGetByLogin(command.Login, null);
            UsersService service = CreateService();

            Result<AuthenticationResult> result = await service.Login(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.Error.StatusCode.Should().Be((int)HttpStatusCode.Unauthorized);
            result.Error.ErrorCode.Should().Be(ErrorCodes.INVALID_LOGIN_OR_PASSWORD);
            _passwordHasherMock
                .Verify(h => h.Verify(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }
    }
}
