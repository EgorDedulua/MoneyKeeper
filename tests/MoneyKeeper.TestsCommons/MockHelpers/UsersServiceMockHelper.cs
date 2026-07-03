using MoneyKeeper.Application.Common.Auth;
using MoneyKeeper.Application.Contracts.User;
using MoneyKeeper.Core.Common.Repositories;
using MoneyKeeper.Core.Models;
using Moq;

namespace MoneyKeeper.TestsCommons.MockHelpers
{
    public static class UsersServiceMockHelper
    {
        public static RegisterUserCommand RegisterUserCommand = new("Login", "Name", "12345678");

        public static LoginUserCommand LoginUserCommand = new("Login", "12345678");

        public static void SetupGetByLogin(this Mock<IUsersRepository> mock, string login, User? value)
        {
            mock.Setup(r => r.GetByLoginAsync(login, It.IsAny<CancellationToken>()))
                .ReturnsAsync(value);
        }

        public static void SetupVerifyPassword(this Mock<IPasswordHasher> mock, string hash, string password, bool value)
        {
            mock.Setup(h => h.Verify(hash, password))
                .Returns(value);
        }
    }
}
