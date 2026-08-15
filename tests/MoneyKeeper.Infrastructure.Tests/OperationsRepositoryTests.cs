using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MoneyKeeper.Core.Enums;
using MoneyKeeper.Core.Models;
using MoneyKeeper.Infrastructure.Data.Repositories;
using MoneyKeeper.Infrastructure.Tests.Common;

namespace MoneyKeeper.Infrastructure.Tests
{
    public class OperationsRepositoryTests : RepositoryTestsBase
    {
        private OperationsRepository _repository = null!;

        public OperationsRepositoryTests(Common.DbFixture fixture) : base(fixture)
        {
            _repository = new OperationsRepository(_context);
        }

        [Fact]
        public async Task AddAsync_ValidOperation_OperationIsSaved()
        {
            User user = await CreateTestUserAsync();
            Account account = new Account { UserId = user.Id, Name = "TestAccount", Balance = 500 };
            Category category = new Category { UserId = user.Id, Name = "TestCategory", Type = CategoryType.Income };
            _context.Accounts.Add(account);
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            Operation operation = new Operation
            {
                AccountId = account.Id,
                CategoryId = category.Id,
                Sum = 200,
                Description = "TestOp",
                OldAccountBalance = 500,
                NewAccountBalance = 700
            };

            await _repository.AddAsync(operation, CancellationToken.None);

            Operation? fromDb = await _context.Operations.FirstOrDefaultAsync(o => o.Description == "TestOp");
            fromDb.Should().NotBeNull();
            fromDb!.AccountId.Should().Be(account.Id);
            fromDb.CategoryId.Should().Be(category.Id);
            fromDb.Sum.Should().Be(200);
            fromDb.OldAccountBalance.Should().Be(500);
            fromDb.NewAccountBalance.Should().Be(700);
            fromDb.Date.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task DeleteAsync_ExistingOperation_OperationIsDeleted()
        {
            User user = await CreateTestUserAsync();
            Account account = new Account { UserId = user.Id, Name = "DelAcc", Balance = 0 };
            Category category = new Category { UserId = user.Id, Name = "DelCat", Type = CategoryType.Consumption };
            Operation operation = new Operation
            {
                AccountId = account.Id,
                CategoryId = category.Id,
                Sum = 50,
                OldAccountBalance = 100,
                NewAccountBalance = 50
            };
            operation.Account = account;
            operation.Category = category;
            _context.Accounts.Add(account);
            _context.Categories.Add(category);
            _context.Operations.Add(operation);
            await _context.SaveChangesAsync();

            await _repository.DeleteAsync(operation.Id, CancellationToken.None);

            Operation? fromDb = await _context.Operations.FirstOrDefaultAsync(o => o.Id == operation.Id);
            fromDb.Should().BeNull();
        }

        [Fact]
        public async Task GetByIdAsync_ExistingOperation_ReturnsOperationWithIncludes()
        {
            User user = await CreateTestUserAsync();
            Account account = new Account { UserId = user.Id, Name = "GetAcc" };
            Category category = new Category { UserId = user.Id, Name = "GetCat", Type = CategoryType.Income };
            Operation operation = new Operation
            {
                AccountId = account.Id,
                CategoryId = category.Id,
                Sum = 300,
                OldAccountBalance = 0,
                NewAccountBalance = 300
            };
            operation.Account = account;
            operation.Category = category;
            _context.Accounts.Add(account);
            _context.Categories.Add(category);
            _context.Operations.Add(operation);
            await _context.SaveChangesAsync();

            Operation? result = await _repository.GetByIdAsync(operation.Id, CancellationToken.None);

            result.Should().NotBeNull();
            result!.Account.Should().NotBeNull();
            result.Account.Name.Should().Be("GetAcc");
            result.Category.Should().NotBeNull();
            result.Category.Name.Should().Be("GetCat");
        }

        [Fact]
        public async Task GetByIdAsync_NonExistentOperation_ReturnsNull()
        {
            Operation? result = await _repository.GetByIdAsync(99999, CancellationToken.None);
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetAllByUserId_ReturnsOnlyUserOperations()
        {
            User user = await CreateTestUserAsync();
            User user2 = await CreateTestUserAsync(2);
            Account account1 = new Account { UserId = user.Id, Name = "U1Acc" };
            Account account2 = new Account { UserId = user2.Id, Name = "U2Acc" };
            Category category1 = new Category { UserId = user.Id, Name = "C1", Type = CategoryType.Income };
            Category category2 = new Category { UserId = user2.Id, Name = "C2", Type = CategoryType.Consumption };
            Operation operation1 = new Operation { Account = account1, Category = category1, AccountId = account1.Id, CategoryId = category1.Id, Sum = 10 };
            Operation operation2 = new Operation { Account = account2, Category = category2, AccountId = account2.Id, CategoryId = category2.Id, Sum = 20 };
            _context.Accounts.AddRange(account1, account2);
            _context.Categories.AddRange(category1, category2);
            _context.Operations.AddRange(operation1, operation2);
            await _context.SaveChangesAsync();

            IQueryable<Operation> operations = _repository.GetAllByUserId(1);
            List<Operation> result = await operations.ToListAsync();

            result.Should().HaveCount(1);
            result[0].Account.UserId.Should().Be(1);
        }

        [Fact]
        public async Task GetAllPagedAsync_ReturnsCorrectPage()
        {
            User user = await CreateTestUserAsync();
            Account account = new Account { UserId = user.Id, Name = "PageAcc" };
            Category category = new Category { UserId = user.Id, Name = "PageCat", Type = CategoryType.Income };
            Operation operation1 = new Operation { Account = account, Category = category, AccountId = account.Id, CategoryId = category.Id, Sum = 1 };
            Operation operation2 = new Operation { Account = account, Category = category, AccountId = account.Id, CategoryId = category.Id, Sum = 2 };
            Operation operation3 = new Operation { Account = account, Category = category, AccountId = account.Id, CategoryId = category.Id, Sum = 3 };
            _context.Accounts.Add(account);
            _context.Categories.Add(category);
            _context.Operations.AddRange(operation1, operation2, operation3);
            await _context.SaveChangesAsync();

            IQueryable<Operation> query = _context.Operations.Where(o => o.Account.UserId == user.Id);
            (List<Operation> items, int totalCount) = await _repository.GetAllPagedAsync(query, 2, 1, CancellationToken.None);

            items.Should().HaveCount(1);
            totalCount.Should().Be(3);
        }

        [Fact]
        public async Task UpdateAsync_ExistingOperation_UpdatesProperties()
        {
            User user = await CreateTestUserAsync();
            Account account = new Account { UserId = user.Id, Name = "UpdAcc" };
            Category category = new Category { UserId = 1, Name = "UpdCat", Type = CategoryType.Income };
            Operation operation = new Operation
            {
                AccountId = account.Id,
                CategoryId = category.Id,
                Sum = 100,
                Description = "Old",
                OldAccountBalance = 200,
                NewAccountBalance = 300
            };
            operation.Account = account;
            operation.Category = category;
            _context.Accounts.Add(account);
            _context.Categories.Add(category);
            _context.Operations.Add(operation);
            await _context.SaveChangesAsync();

            operation.Sum = 150;
            operation.Description = "New";
            operation.OldAccountBalance = 250;
            operation.NewAccountBalance = 400;
            Operation updated = await _repository.UpdateAsync(operation, CancellationToken.None);

            updated.Sum.Should().Be(150);
            updated.Description.Should().Be("New");
            updated.OldAccountBalance.Should().Be(250);
            updated.NewAccountBalance.Should().Be(400);
            updated.Account.Name.Should().Be("UpdAcc");
            updated.Category.Name.Should().Be("UpdCat");
        }

        [Fact]
        public async Task AreAnyConsumptionOperationsAfterAsync_WhenConsumptionExists_ReturnsTrue()
        {
            User user = await CreateTestUserAsync();
            Account account = new Account { UserId = user.Id, Name = "ConsAcc" };
            Category category = new Category { UserId = user.Id, Name = "ConsCat", Type = CategoryType.Consumption };
            Operation operation = new Operation
            {
                Account = account,
                Category = category,
                AccountId = account.Id,
                CategoryId = category.Id,
                Sum = 50,
                Date = DateTime.Now.AddMinutes(10)
            };
            _context.Accounts.Add(account);
            _context.Categories.Add(category);
            _context.Operations.Add(operation);
            await _context.SaveChangesAsync();

            bool result = await _repository.AreAnyConsumptionOperationsAfterAsync(account.Id, DateTime.Now, CancellationToken.None);

            result.Should().BeTrue();
        }

        [Fact]
        public async Task AreAnyConsumptionOperationsAfterAsync_NoConsumptionAfterDate_ReturnsFalse()
        {
            User user = await CreateTestUserAsync();
            Account account = new Account { UserId = user.Id, Name = "NoConsAcc" };
            Category category = new Category { UserId = user.Id, Name = "NoConsCat", Type = CategoryType.Income };
            Operation operation = new Operation
            {
                Account = account,
                Category = category,
                AccountId = account.Id,
                CategoryId = category.Id,
                Sum = 50,
                Date = DateTime.Now.AddMinutes(-10)
            };
            _context.Accounts.Add(account);
            _context.Categories.Add(category);
            _context.Operations.Add(operation);
            await _context.SaveChangesAsync();

            bool result = await _repository.AreAnyConsumptionOperationsAfterAsync(account.Id, DateTime.Now, CancellationToken.None);

            result.Should().BeFalse();
        }

        [Fact]
        public async Task AreAnyOperationsWithCategoryAsync_WhenOperationsExist_ReturnsTrue()
        {
            User user = await CreateTestUserAsync();
            Account account = new Account { UserId = user.Id, Name = "CatOpAcc" };
            Category category = new Category { UserId = user.Id, Name = "CatOpCat", Type = CategoryType.Income };
            Operation operation = new Operation
            {
                Account = account,
                Category = category,
                AccountId = account.Id,
                CategoryId = category.Id,
                Sum = 10
            };
            _context.Accounts.Add(account);
            _context.Categories.Add(category);
            _context.Operations.Add(operation);
            await _context.SaveChangesAsync();

            bool result = await _repository.AreAnyOperationsWithCategoryAsync(category.Id, CancellationToken.None);

            result.Should().BeTrue();
        }

        [Fact]
        public async Task AreAnyOperationsWithCategoryAsync_WhenNoOperations_ReturnsFalse()
        {
            User user = await CreateTestUserAsync();
            Category category = new Category { UserId = user.Id, Name = "EmptyCat", Type = CategoryType.Income };
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            bool result = await _repository.AreAnyOperationsWithCategoryAsync(category.Id, CancellationToken.None);

            result.Should().BeFalse();
        }
    }
}