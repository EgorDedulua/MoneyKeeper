using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MoneyKeeper.Core.Enums;
using MoneyKeeper.Core.Models;
using MoneyKeeper.Infrastructure.Data.Repositories;
using MoneyKeeper.Infrastructure.Tests.Common;

namespace MoneyKeeper.Infrastructure.Tests
{
    public class CommonBalanceOperationsRepositoryTests : RepositoryTestsBase
    {
        private CommonBalanceOperationsRepository _repository = null!;

        public CommonBalanceOperationsRepositoryTests(Common.DbFixture fixture) : base(fixture)
        {
            _repository = new CommonBalanceOperationsRepository(_context);
        }

        [Fact]
        public async Task RecalculateAllTailsAsync_NoEventsAfterDate_SetsBalanceToInitial()
        {
            User user = await CreateTestUserAsync();
            Account account = new Account { UserId = user.Id, Name = "TestAcc", Balance = 100 };
            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();

            await _repository.RecalculateAllTailsAsync(account.Id, DateTime.Now, 999m, CancellationToken.None);

            Account? updatedAccount = await _context.Accounts
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == account.Id);
            updatedAccount!.Balance.Should().Be(999m);
        }

        [Fact]
        public async Task RecalculateAllTailsAsync_WithIncomeOperation_AdjustsBalances()
        {
            User user = await CreateTestUserAsync();
            Account account = new Account { UserId = user.Id, Name = "Acc", Balance = 0 };
            Category category = new Category { UserId = user.Id, Name = "Cat", Type = CategoryType.Income };
            Operation operation = new Operation
            {
                AccountId = account.Id,
                CategoryId = category.Id,
                Sum = 200,
                Date = DateTime.Now.AddMinutes(5),
                OldAccountBalance = 0,
                NewAccountBalance = 0
            };
            operation.Account = account;
            operation.Category = category;
            _context.Accounts.Add(account);
            _context.Categories.Add(category);
            _context.Operations.Add(operation);
            await _context.SaveChangesAsync();

            await _repository.RecalculateAllTailsAsync(account.Id, DateTime.Now, 100m, CancellationToken.None);

            Operation? updatedOperation = await _context.Operations.FirstOrDefaultAsync(o => o.Id == operation.Id);
            updatedOperation!.OldAccountBalance.Should().Be(100m);
            updatedOperation.NewAccountBalance.Should().Be(300m);
            Account? updatedAccount = await _context.Accounts
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == account.Id);
            updatedAccount!.Balance.Should().Be(300m);
        }

        [Fact]
        public async Task RecalculateAllTailsAsync_WithConsumptionOperation_AdjustsBalances()
        {
            User user = await CreateTestUserAsync();
            Account account = new Account { UserId = user.Id, Name = "Acc", Balance = 0 };
            Category category = new Category { UserId = user.Id, Name = "Cat", Type = CategoryType.Consumption };
            Operation operation = new Operation
            {
                AccountId = account.Id,
                CategoryId = category.Id,
                Sum = 50,
                Date = DateTime.Now.AddMinutes(5),
                OldAccountBalance = 0,
                NewAccountBalance = 0
            };
            operation.Account = account;
            operation.Category = category;
            _context.Accounts.Add(account);
            _context.Categories.Add(category);
            _context.Operations.Add(operation);
            await _context.SaveChangesAsync();

            await _repository.RecalculateAllTailsAsync(account.Id, DateTime.Now, 200m, CancellationToken.None);

            Operation? updatedOperation = await _context.Operations.FirstOrDefaultAsync(o => o.Id == operation.Id);
            updatedOperation!.OldAccountBalance.Should().Be(200m);
            updatedOperation.NewAccountBalance.Should().Be(150m);
            Account? updatedAccount = await _context.Accounts
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == account.Id);
            updatedAccount!.Balance.Should().Be(150m);
        }

        [Fact]
        public async Task RecalculateAllTailsAsync_WithOutgoingTransition_AdjustsBalances()
        {
            User user = await CreateTestUserAsync();
            Account account = new Account { UserId = user.Id, Name = "Src", Balance = 0 };
            Account destination = new Account { UserId = user.Id, Name = "Dst", Balance = 0 };
            Transition transition = new Transition
            {
                SourceAccountId = account.Id,
                DestinationAccountId = destination.Id,
                Sum = 100,
                Date = DateTime.Now.AddMinutes(5),
                OldSourceAccountBalance = 0,
                NewSourceAccountBalance = 0,
                OldDestinationAccountBalance = 0,
                NewDestinationAccountBalance = 0
            };
            transition.SourceAccount = account;
            transition.DestinationAccount = destination;
            _context.Accounts.AddRange(account, destination);
            _context.Transitions.Add(transition);
            await _context.SaveChangesAsync();

            await _repository.RecalculateAllTailsAsync(account.Id, DateTime.Now, 500m, CancellationToken.None);

            Transition? updatedTransition = await _context.Transitions.FirstOrDefaultAsync(t => t.Id == transition.Id);
            updatedTransition!.OldSourceAccountBalance.Should().Be(500m);
            updatedTransition.NewSourceAccountBalance.Should().Be(400m);
            Account? updatedAccount = await _context.Accounts
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == account.Id);
            updatedAccount!.Balance.Should().Be(400m);
        }

        [Fact]
        public async Task RecalculateAllTailsAsync_WithIncomingTransition_AdjustsBalances()
        {
            User user = await CreateTestUserAsync();
            Account source = new Account { UserId = user.Id, Name = "Src", Balance = 0 };
            Account account = new Account { UserId = user.Id, Name = "Dst", Balance = 0 };
            Transition transition = new Transition
            {
                SourceAccountId = source.Id,
                DestinationAccountId = account.Id,
                Sum = 100,
                Date = DateTime.Now.AddMinutes(5),
                OldSourceAccountBalance = 0,
                NewSourceAccountBalance = 0,
                OldDestinationAccountBalance = 0,
                NewDestinationAccountBalance = 0
            };
            transition.SourceAccount = source;
            transition.DestinationAccount = account;
            _context.Accounts.AddRange(source, account);
            _context.Transitions.Add(transition);
            await _context.SaveChangesAsync();

            await _repository.RecalculateAllTailsAsync(account.Id, DateTime.Now, 200m, CancellationToken.None);

            Transition? updatedTransition = await _context.Transitions.FirstOrDefaultAsync(t => t.Id == transition.Id);
            updatedTransition!.OldDestinationAccountBalance.Should().Be(200m);
            updatedTransition.NewDestinationAccountBalance.Should().Be(300m);
            Account? updatedAccount = await _context.Accounts
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == account.Id);
            updatedAccount!.Balance.Should().Be(300m);
        }

        [Fact]
        public async Task RecalculateAllTailsAsync_WithBalanceChanging_AdjustsBalances()
        {
            User user = await CreateTestUserAsync();
            Account account = new Account { UserId = user.Id, Name = "Acc", Balance = 0 };
            BalanceChanging balanceChanging = new BalanceChanging
            {
                AccountId = account.Id,
                OldAccountBalance = 0,
                NewAccountBalance = 500,
                Date = DateTime.Now.AddMinutes(5)
            };
            balanceChanging.Account = account;
            _context.Accounts.Add(account);
            _context.BalanceChangings.Add(balanceChanging);
            await _context.SaveChangesAsync();

            await _repository.RecalculateAllTailsAsync(account.Id, DateTime.Now, 100m, CancellationToken.None);

            BalanceChanging? updatedBc = await _context.BalanceChangings.FirstOrDefaultAsync(b => b.Id == balanceChanging.Id);
            updatedBc!.OldAccountBalance.Should().Be(100m);
            updatedBc.NewAccountBalance.Should().Be(500m);
            Account? updatedAccount = await _context.Accounts
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == account.Id);
            updatedAccount!.Balance.Should().Be(500m);
        }

        [Fact]
        public async Task RecalculateAllTailsAsync_MultipleEvents_CorrectChain()
        {
            User user = await CreateTestUserAsync();
            Account account = new Account { UserId = user.Id, Name = "Acc", Balance = 0 };
            Category category = new Category { UserId = user.Id, Name = "Cat", Type = CategoryType.Income };
            Operation operation1 = new Operation
            {
                AccountId = account.Id,
                CategoryId = category.Id,
                Sum = 100,
                Date = DateTime.Now.AddMinutes(5),
                OldAccountBalance = 0,
                NewAccountBalance = 0
            };
            operation1.Account = account;
            operation1.Category = category;
            Operation operation2 = new Operation
            {
                AccountId = account.Id,
                CategoryId = category.Id,
                Sum = 50,
                Date = DateTime.Now.AddMinutes(10),
                OldAccountBalance = 0,
                NewAccountBalance = 0
            };
            operation2.Account = account;
            operation2.Category = category;
            _context.Accounts.Add(account);
            _context.Categories.Add(category);
            _context.Operations.AddRange(operation1, operation2);
            await _context.SaveChangesAsync();

            await _repository.RecalculateAllTailsAsync(account.Id, DateTime.Now, 200m, CancellationToken.None);

            Operation? updatedOp1 = await _context.Operations.FirstOrDefaultAsync(o => o.Id == operation1.Id);
            updatedOp1!.OldAccountBalance.Should().Be(200m);
            updatedOp1.NewAccountBalance.Should().Be(300m);
            Operation? updatedOp2 = await _context.Operations.FirstOrDefaultAsync(o => o.Id == operation2.Id);
            updatedOp2!.OldAccountBalance.Should().Be(300m);
            updatedOp2.NewAccountBalance.Should().Be(350m);
            Account? updatedAccount = await _context.Accounts
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == account.Id);
            updatedAccount!.Balance.Should().Be(350m);
        }

        [Fact]
        public async Task IsTailValidAfterChange_TailStaysPositive_ReturnsTrue()
        {
            User user = await CreateTestUserAsync();
            Account account = new Account { UserId = user.Id, Name = "Acc", Balance = 0 };
            Category category = new Category { UserId = user.Id, Name = "Cat", Type = CategoryType.Income };
            Operation operation = new Operation
            {
                AccountId = account.Id,
                CategoryId = category.Id,
                Sum = 100,
                Date = DateTime.Now.AddMinutes(5)
            };
            operation.Account = account;
            operation.Category = category;
            _context.Accounts.Add(account);
            _context.Categories.Add(category);
            _context.Operations.Add(operation);
            await _context.SaveChangesAsync();

            bool isValid = await _repository.IsTailValidAfterChange(account.Id, DateTime.Now, 50m, CancellationToken.None);

            isValid.Should().BeTrue();
        }

        [Fact]
        public async Task IsTailValidAfterChange_TailGoesNegative_ReturnsFalse()
        {
            User user = await CreateTestUserAsync();
            Account account = new Account { UserId = user.Id, Name = "Acc", Balance = 0 };
            Category category = new Category { UserId = user.Id, Name = "Cat", Type = CategoryType.Consumption };
            Operation operation = new Operation
            {
                AccountId = account.Id,
                CategoryId = category.Id,
                Sum = 100,
                Date = DateTime.Now.AddMinutes(5)
            };
            operation.Account = account;
            operation.Category = category;
            _context.Accounts.Add(account);
            _context.Categories.Add(category);
            _context.Operations.Add(operation);
            await _context.SaveChangesAsync();

            bool isValid = await _repository.IsTailValidAfterChange(account.Id, DateTime.Now, 50m, CancellationToken.None);

            isValid.Should().BeFalse();
        }

        [Fact]
        public async Task IsTailValidAfterChange_NoEvents_ReturnsTrue()
        {
            bool isValid = await _repository.IsTailValidAfterChange(1, DateTime.Now, 0m, CancellationToken.None);

            isValid.Should().BeTrue();
        }
    }
}