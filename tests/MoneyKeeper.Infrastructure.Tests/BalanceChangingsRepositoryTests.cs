using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MoneyKeeper.Core.Models;
using MoneyKeeper.Infrastructure.Data.Repositories;
using MoneyKeeper.Infrastructure.Tests.Common;

namespace MoneyKeeper.Infrastructure.Tests
{
    public class BalanceChangingsRepositoryTests : RepositoryTestsBase
    {
        private BalanceChangingsRepository _repository = null!;

        public BalanceChangingsRepositoryTests(Common.DbFixture fixture) : base(fixture)
        {
            _repository = new BalanceChangingsRepository(_context);
        }

        [Fact]
        public async Task AddAsync_ValidBalanceChanging_BalanceChangingIsSaved()
        {
            User user = await CreateTestUserAsync();
            Account account = new Account { UserId = user.Id, Name = "TestAccount", Balance = 500 };
            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();

            BalanceChanging balanceChanging = new BalanceChanging
            {
                AccountId = account.Id,
                OldAccountBalance = 500,
                NewAccountBalance = 700
            };

            await _repository.AddAsync(balanceChanging, CancellationToken.None);

            BalanceChanging? fromDb = await _context.BalanceChangings
                .FirstOrDefaultAsync(b => b.AccountId == account.Id && b.NewAccountBalance == 700);
            fromDb.Should().NotBeNull();
            fromDb!.AccountId.Should().Be(account.Id);
            fromDb.OldAccountBalance.Should().Be(500);
            fromDb.NewAccountBalance.Should().Be(700);
            fromDb.Date.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task DeleteAsync_ExistingBalanceChanging_BalanceChangingIsDeleted()
        {
            User user = await CreateTestUserAsync();
            Account account = new Account { UserId = user.Id, Name = "DelAcc", Balance = 0 };
            BalanceChanging balanceChanging = new BalanceChanging
            {
                AccountId = account.Id,
                OldAccountBalance = 100,
                NewAccountBalance = 200
            };
            balanceChanging.Account = account;
            _context.Accounts.Add(account);
            _context.BalanceChangings.Add(balanceChanging);
            await _context.SaveChangesAsync();

            await _repository.DeleteAsync(balanceChanging.Id, CancellationToken.None);

            BalanceChanging? fromDb = await _context.BalanceChangings
                .FirstOrDefaultAsync(b => b.Id == balanceChanging.Id);
            fromDb.Should().BeNull();
        }

        [Fact]
        public async Task GetByIdAsync_ExistingBalanceChanging_ReturnsBalanceChangingWithAccount()
        {
            User user = await CreateTestUserAsync();
            Account account = new Account { UserId = user.Id, Name = "GetAcc" };
            BalanceChanging balanceChanging = new BalanceChanging
            {
                AccountId = account.Id,
                OldAccountBalance = 200,
                NewAccountBalance = 300
            };
            balanceChanging.Account = account;
            _context.Accounts.Add(account);
            _context.BalanceChangings.Add(balanceChanging);
            await _context.SaveChangesAsync();

            BalanceChanging? result = await _repository.GetByIdAsync(balanceChanging.Id, CancellationToken.None);

            result.Should().NotBeNull();
            result!.Account.Should().NotBeNull();
            result.Account.Name.Should().Be("GetAcc");
            result.OldAccountBalance.Should().Be(200);
            result.NewAccountBalance.Should().Be(300);
        }

        [Fact]
        public async Task GetByIdAsync_NonExistentBalanceChanging_ReturnsNull()
        {
            BalanceChanging? result = await _repository.GetByIdAsync(99999, CancellationToken.None);
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetAllByUserId_ReturnsOnlyUserBalanceChangings()
        {
            User user1 = await CreateTestUserAsync();
            User user2 = await CreateTestUserAsync(2);
            Account account1 = new Account { UserId = user1.Id, Name = "U1Acc" };
            Account account2 = new Account { UserId = user2.Id, Name = "U2Acc" };
            BalanceChanging bc1 = new BalanceChanging
            {
                Account = account1,
                AccountId = account1.Id,
                OldAccountBalance = 100,
                NewAccountBalance = 200
            };
            BalanceChanging bc2 = new BalanceChanging
            {
                Account = account2,
                AccountId = account2.Id,
                OldAccountBalance = 300,
                NewAccountBalance = 400
            };
            _context.Accounts.AddRange(account1, account2);
            _context.BalanceChangings.AddRange(bc1, bc2);
            await _context.SaveChangesAsync();

            IQueryable<BalanceChanging> changings = _repository.GetAllByUserId(1);
            List<BalanceChanging> result = await changings.ToListAsync();

            result.Should().HaveCount(1);
            result[0].Account.UserId.Should().Be(1);
        }

        [Fact]
        public async Task GetAllPagedAsync_ReturnsCorrectPage()
        {
            User user = await CreateTestUserAsync();
            Account account = new Account { UserId = user.Id, Name = "PageAcc" };
            BalanceChanging bc1 = new BalanceChanging
            {
                Account = account,
                AccountId = account.Id,
                OldAccountBalance = 100,
                NewAccountBalance = 200
            };
            BalanceChanging bc2 = new BalanceChanging
            {
                Account = account,
                AccountId = account.Id,
                OldAccountBalance = 200,
                NewAccountBalance = 300
            };
            BalanceChanging bc3 = new BalanceChanging
            {
                Account = account,
                AccountId = account.Id,
                OldAccountBalance = 300,
                NewAccountBalance = 400
            };
            _context.Accounts.Add(account);
            _context.BalanceChangings.AddRange(bc1, bc2, bc3);
            await _context.SaveChangesAsync();

            IQueryable<BalanceChanging> query = _context.BalanceChangings.Where(b => b.Account.UserId == user.Id);
            (List<BalanceChanging> items, int totalCount) = await _repository.GetAllPagedAsync(query, 2, 1, CancellationToken.None);

            items.Should().HaveCount(1);
            totalCount.Should().Be(3);
        }

        [Fact]
        public async Task UpdateAsync_ExistingBalanceChanging_UpdatesProperties()
        {
            User user = await CreateTestUserAsync();
            Account account = new Account { UserId = 1, Name = "UpdAcc" };
            BalanceChanging balanceChanging = new BalanceChanging
            {
                AccountId = account.Id,
                OldAccountBalance = 100,
                NewAccountBalance = 200
            };
            balanceChanging.Account = account;
            _context.Accounts.Add(account);
            _context.BalanceChangings.Add(balanceChanging);
            await _context.SaveChangesAsync();

            balanceChanging.OldAccountBalance = 150;
            balanceChanging.NewAccountBalance = 250;
            BalanceChanging updated = await _repository.UpdateAsync(balanceChanging, CancellationToken.None);

            updated.OldAccountBalance.Should().Be(150);
            updated.NewAccountBalance.Should().Be(250);
            updated.Account.Name.Should().Be("UpdAcc");
        }

        [Fact]
        public async Task AreAnyAfterAsync_WhenBalanceChangingExists_ReturnsTrue()
        {
            User user = await CreateTestUserAsync();
            Account account = new Account { UserId = user.Id, Name = "AfterAcc" };
            BalanceChanging balanceChanging = new BalanceChanging
            {
                Account = account,
                AccountId = account.Id,
                OldAccountBalance = 100,
                NewAccountBalance = 200,
                Date = DateTime.Now.AddMinutes(10)
            };
            _context.Accounts.Add(account);
            _context.BalanceChangings.Add(balanceChanging);
            await _context.SaveChangesAsync();

            bool result = await _repository.AreAnyAfterAsync(account.Id, DateTime.Now, CancellationToken.None);

            result.Should().BeTrue();
        }

        [Fact]
        public async Task AreAnyAfterAsync_NoBalanceChangingAfterDate_ReturnsFalse()
        {
            User user = await CreateTestUserAsync();
            Account account = new Account { UserId = user.Id, Name = "NoAfterAcc" };
            BalanceChanging balanceChanging = new BalanceChanging
            {
                Account = account,
                AccountId = account.Id,
                OldAccountBalance = 100,
                NewAccountBalance = 200,
                Date = DateTime.Now.AddMinutes(-10)
            };
            _context.Accounts.Add(account);
            _context.BalanceChangings.Add(balanceChanging);
            await _context.SaveChangesAsync();

            bool result = await _repository.AreAnyAfterAsync(account.Id, DateTime.Now, CancellationToken.None);

            result.Should().BeFalse();
        }
    }
}