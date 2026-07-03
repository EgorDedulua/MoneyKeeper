using MoneyKeeper.Core.Models;
using MoneyKeeper.Infrastructure.Data.Repositories;
using MoneyKeeper.Infrastructure.Tests.Common;
using Microsoft.EntityFrameworkCore;
using FluentAssertions;

namespace MoneyKeeper.Infrastructure.Tests
{
    public class UsersRepositoryTests : RepositoryTestsBase
    {
        private UsersRepository _repository = null!;

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();
            _repository = new UsersRepository(_context);
            await ClearDatabaseAsync();
        }

        [Fact]
        public async Task Add_UserIsSaved()
        {
            User user = new User()
            {
                Login = "testUser",
                Password = "hash",
                UserName = "Test"
            };

            await _repository.AddAsync(user, CancellationToken.None);

            User? result = await _context.Users.FirstOrDefaultAsync(u => u.Login == "testUser");
            result.Should().NotBeNull();
            result.Id.Should().NotBe(default);
            result.UserName.Should().Be(user.UserName);
            result.Password.Should().Be(user.Password);
            result.Login.Should().Be(user.Login);
            result.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task GetByLogin_WhenUserExists_ReturnsUser()
        {
            User user = new User()
            {
                Login = "Exist",
                UserName = "Existing",
                Password = "hash"
            };
            await _repository.AddAsync(user, CancellationToken.None);

            User? result = await _repository.GetByLoginAsync(user.Login, CancellationToken.None);

            result.Should().NotBeNull();
            result.Id.Should().NotBe(default);
            result.UserName.Should().Be(user.UserName);
            result.Password.Should().Be(user.Password);
            result.Login.Should().Be(user.Login);
        }

        [Fact]
        public async Task GetByLogin_WhenUserDoesNotExist_ReturnsNull()
        {
            User? result = await _repository.GetByLoginAsync("undefined", CancellationToken.None);

            result.Should().BeNull();
        }
    }
}
