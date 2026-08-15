using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MoneyKeeper.Core.Models;
using MoneyKeeper.Infrastructure.Data.Repositories;
using MoneyKeeper.Infrastructure.Tests.Common;

namespace MoneyKeeper.Infrastructure.Tests
{
    public class AccountsRepositoryTests : RepositoryTestsBase
    {
        private AccountsRepository _repository = null!;

        public AccountsRepositoryTests(Common.DbFixture fixture) : base(fixture)
        {
            _repository = new AccountsRepository(_context);
        }

        [Fact]
        public async Task AddAsync_ValidAccount_AccountIsSaved()
        {
            User user = await CreateTestUserAsync();
            Account account = new Account
            {
                UserId = user.Id,
                Name = "Test Account",
                Balance = 100.50m,
                Target = 500m,
                Description = "Test Description"
            };

            await _repository.AddAsync(account, CancellationToken.None);

            Account? fromDb = await _context.Accounts.FirstOrDefaultAsync(a => a.Name == "Test Account");
            fromDb.Should().NotBeNull();
            fromDb!.UserId.Should().Be(user.Id);  
            fromDb.Balance.Should().Be(100.50m);
            fromDb.Target.Should().Be(500m);
            fromDb.Description.Should().Be("Test Description");
            fromDb.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task DeleteAsync_ExistingAccount_AccountIsDeleted()
        {
            User user = await CreateTestUserAsync();
            Account account = new Account { UserId = user.Id, Name = "ToDelete", Balance = 0 };
            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();

            await _repository.DeleteAsync(account.Id, CancellationToken.None);

            Account? fromDb = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == account.Id);
            fromDb.Should().BeNull();
        }

        [Fact]
        public async Task GetByIdAsync_ExistingAccount_ReturnsAccount()
        {
            User user = await CreateTestUserAsync();
            Account account = new Account { UserId = user.Id, Name = "Existing", Balance = 50 };
            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();

            Account? result = await _repository.GetByIdAsync(account.Id, CancellationToken.None);

            result.Should().NotBeNull();
            result!.Name.Should().Be("Existing");
            result.Balance.Should().Be(50);
        }

        [Fact]
        public async Task GetByIdAsync_NonExistentAccount_ReturnsNull()
        {
            Account? result = await _repository.GetByIdAsync(99999, CancellationToken.None);
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetAllByUserId_ReturnsOnlyUserAccounts()
        {
            User user1 = await CreateTestUserAsync();
            User user2 = await CreateTestUserAsync(2);
            Account account1 = new Account { UserId = user1.Id, Name = "User1 Account" };
            Account account2 = new Account { UserId = user2.Id, Name = "User2 Account" };
            _context.Accounts.AddRange(account1, account2);
            await _context.SaveChangesAsync();

            IQueryable<Account> accounts = _repository.GetAllByUserId(user1.Id);   
            List<Account> result = await accounts.ToListAsync();

            result.Should().HaveCount(1);
            result[0].Name.Should().Be("User1 Account");
        }

        [Fact]
        public async Task GetAllPagedAsync_ReturnsCorrectPage()
        {
            User user = await CreateTestUserAsync();
            Account account1 = new Account { UserId = user.Id, Name = "A1" };
            Account account2 = new Account { UserId = user.Id, Name = "A2" };
            Account account3 = new Account { UserId = user.Id, Name = "A3" };
            _context.Accounts.AddRange(account1, account2, account3);
            await _context.SaveChangesAsync();

            IQueryable<Account> query = _context.Accounts.Where(a => a.UserId == user.Id);
            (List<Account> items, int totalCount) = await _repository.GetAllPagedAsync(query, 2, 1, CancellationToken.None);

            items.Should().HaveCount(1);
            totalCount.Should().Be(3);
        }

        [Fact]
        public async Task UpdateAsync_ExistingAccount_UpdatesProperties()
        {
            User user = await CreateTestUserAsync();
            Account account = new Account { UserId = user.Id, Name = "Original", Target = 100, Description = "Old" };
            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();

            account.Name = "Updated";
            account.Target = 200;
            account.Description = "New";
            Account updated = await _repository.UpdateAsync(account, CancellationToken.None);

            updated.Name.Should().Be("Updated");
            updated.Target.Should().Be(200);
            updated.Description.Should().Be("New");
            Account? fromDb = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == account.Id);
            fromDb!.Name.Should().Be("Updated");
        }

        [Fact]
        public async Task TryWithdrawAsync_SufficientBalance_DecreasesBalance()
        {
            User user = await CreateTestUserAsync();
            Account account = new Account { UserId = user.Id, Name = "Rich", Balance = 500 };
            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();

            decimal? newBalance = await _repository.TryWithdrawAsync(account.Id, 200, CancellationToken.None);

            newBalance.Should().Be(300);
            Account? fromDb = await _context.Accounts
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == account.Id);
            fromDb!.Balance.Should().Be(300);
        }

        [Fact]
        public async Task TryWithdrawAsync_InsufficientBalance_ReturnsNull()
        {
            User user = await CreateTestUserAsync();
            Account account = new Account { UserId = user.Id, Name = "Poor", Balance = 50 };
            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();

            decimal? newBalance = await _repository.TryWithdrawAsync(account.Id, 100, CancellationToken.None);

            newBalance.Should().BeNull();
            Account? fromDb = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == account.Id);
            fromDb!.Balance.Should().Be(50);
        }

        [Fact]
        public async Task TryDepositAsync_ExistingAccount_IncreasesBalance()
        {
            User user = await CreateTestUserAsync();
            Account account = new Account { UserId = user.Id, Name = "Saver", Balance = 100 };
            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();

            decimal? newBalance = await _repository.TryDepositAsync(account.Id, 50, CancellationToken.None);

            newBalance.Should().Be(150);
            Account? fromDb = await _context.Accounts
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == account.Id);
            fromDb!.Balance.Should().Be(150);
        }

        [Fact]
        public async Task TryDepositAsync_NonExistentAccount_ReturnsNull()
        {
            decimal? newBalance = await _repository.TryDepositAsync(99999, 100, CancellationToken.None);

            newBalance.Should().BeNull();
        }

        [Fact]
        public async Task UpdateBalanceAsync_SetsNewBalance()
        {
            User user = await CreateTestUserAsync();
            Account account = new Account { UserId = user.Id, Name = "ToUpdate", Balance = 100 };
            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();

            await _repository.UpdateBalanceAsync(account.Id, 999, CancellationToken.None);

            Account? fromDb = await _context.Accounts
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == account.Id);
            fromDb!.Balance.Should().Be(999);
        }

        [Fact]
        public async Task ExistsAnotherWithNameAsync_SameNameDifferentId_ReturnsTrue()
        {
            User user = await CreateTestUserAsync();
            Account account1 = new Account { UserId = user.Id, Name = "Duplicate" };
            Account account2 = new Account { UserId = user.Id, Name = "Duplicate" };
            _context.Accounts.AddRange(account1, account2);
            await _context.SaveChangesAsync();

            bool exists = await _repository.ExistsAnotherWithNameAsync("Duplicate", account1.Id, user.Id, CancellationToken.None);

            exists.Should().BeTrue();
        }

        [Fact]
        public async Task ExistsAnotherWithNameAsync_OnlySameAccount_ReturnsFalse()
        {
            User user = await CreateTestUserAsync();
            Account account = new Account { UserId = user.Id, Name = "Unique" };
            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();

            bool exists = await _repository.ExistsAnotherWithNameAsync("Unique", account.Id, user.Id, CancellationToken.None);

            exists.Should().BeFalse();
        }

        [Fact]
        public async Task ExistsWithNameAsync_NameExists_ReturnsTrue()
        {
            User user = await CreateTestUserAsync();
            Account account = new Account { UserId = user.Id, Name = "Taken" };
            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();

            bool exists = await _repository.ExistsWithNameAsync("Taken", user.Id, CancellationToken.None);

            exists.Should().BeTrue();
        }

        [Fact]
        public async Task ExistsWithNameAsync_NameDoesNotExist_ReturnsFalse()
        {
            User user = await CreateTestUserAsync();
            bool exists = await _repository.ExistsWithNameAsync("Ghost", user.Id, CancellationToken.None);

            exists.Should().BeFalse();
        }
    }
}