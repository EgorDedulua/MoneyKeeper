using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MoneyKeeper.Core.Enums;
using MoneyKeeper.Core.Models;
using MoneyKeeper.Infrastructure.Data.Repositories;
using MoneyKeeper.Infrastructure.Tests.Common;

namespace MoneyKeeper.Infrastructure.Tests
{
    public class CategoriesRepositoryTests : RepositoryTestsBase
    {
        private CategoriesRepository _repository = null!;

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();
            _repository = new CategoriesRepository(_context);
            await ClearDatabaseAsync();
        }

        [Fact]
        public async Task AddAsync_ValidCategory_CategoryIsSaved()
        {
            User user = await CreateTestUserAsync();
            Category category = new Category
            {
                UserId = user.Id,
                Name = "TestCategory",
                Type = CategoryType.Income,
                Description = "TestDescription"
            };

            await _repository.AddAsync(category, CancellationToken.None);

            Category? fromDb = await _context.Categories.FirstOrDefaultAsync(c => c.Name == "TestCategory");
            fromDb.Should().NotBeNull();
            fromDb!.UserId.Should().Be(1);
            fromDb.Type.Should().Be(CategoryType.Income);
            fromDb.Description.Should().Be("TestDescription");
        }

        [Fact]
        public async Task DeleteAsync_ExistingCategory_CategoryIsDeleted()
        {
            User user = await CreateTestUserAsync();
            Category category = new Category { UserId = user.Id, Name = "ToDelete", Type = CategoryType.Consumption };
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            await _repository.DeleteAsync(category.Id, CancellationToken.None);

            Category? fromDb = await _context.Categories.FirstOrDefaultAsync(c => c.Id == category.Id);
            fromDb.Should().BeNull();
        }

        [Fact]
        public async Task GetByIdAsync_ExistingCategory_ReturnsCategory()
        {
            User user = await CreateTestUserAsync();
            Category category = new Category { UserId = user.Id, Name = "Existing", Type = CategoryType.Income };
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            Category? result = await _repository.GetByIdAsync(category.Id, CancellationToken.None);

            result.Should().NotBeNull();
            result!.Name.Should().Be("Existing");
            result.Type.Should().Be(CategoryType.Income);
        }

        [Fact]
        public async Task GetByIdAsync_NonExistentCategory_ReturnsNull()
        {
            Category? result = await _repository.GetByIdAsync(99999, CancellationToken.None);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetAllByUserId_ReturnsOnlyUserCategories()
        {
            User user1 = await CreateTestUserAsync();
            User user2 = await CreateTestUserAsync(2);
            Category category1 = new Category { UserId = user1.Id, Name = "User1Cat", Type = CategoryType.Income };
            Category category2 = new Category { UserId = user2.Id, Name = "User2Cat", Type = CategoryType.Consumption };
            _context.Categories.AddRange(category1, category2);
            await _context.SaveChangesAsync();

            IQueryable<Category> categories = _repository.GetAllByUserId(1);
            List<Category> result = await categories.ToListAsync();

            result.Should().HaveCount(1);
            result[0].Name.Should().Be("User1Cat");
        }

        [Fact]
        public async Task GetAllPagedAsync_ReturnsCorrectPage()
        {
            User user = await CreateTestUserAsync();
            Category category1 = new Category { UserId = user.Id, Name = "C1", Type = CategoryType.Income };
            Category category2 = new Category { UserId = user.Id, Name = "C2", Type = CategoryType.Consumption };
            Category category3 = new Category { UserId = user.Id, Name = "C3", Type = CategoryType.Income };
            _context.Categories.AddRange(category1, category2, category3);
            await _context.SaveChangesAsync();

            IQueryable<Category> query = _context.Categories.Where(c => c.UserId == user.Id);
            (List<Category> items, int totalCount) = await _repository.GetAllPagedAsync(query, 2, 1, CancellationToken.None);

            items.Should().HaveCount(1);
            totalCount.Should().Be(3);
        }

        [Fact]
        public async Task UpdateAsync_ExistingCategory_UpdatesProperties()
        {
            User user = await CreateTestUserAsync();
            Category category = new Category
            {
                UserId = user.Id,
                Name = "Original",
                Type = CategoryType.Income,
                Description = "Old"
            };
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            category.Name = "Updated";
            category.Type = CategoryType.Consumption;
            category.Description = "New";
            Category updated = await _repository.UpdateAsync(category, CancellationToken.None);

            updated.Name.Should().Be("Updated");
            updated.Type.Should().Be(CategoryType.Consumption);
            updated.Description.Should().Be("New");

            Category? fromDb = await _context.Categories.FirstOrDefaultAsync(c => c.Id == category.Id);
            fromDb!.Name.Should().Be("Updated");
        }

        [Fact]
        public async Task ExistsWithName_NameExists_ReturnsTrue()
        {
            User user = await CreateTestUserAsync();
            Category category = new Category { UserId = user.Id, Name = "Taken", Type = CategoryType.Income };
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            bool exists = await _repository.ExistsWithName("Taken", user.Id, CancellationToken.None);

            exists.Should().BeTrue();
        }

        [Fact]
        public async Task ExistsWithName_NameDoesNotExist_ReturnsFalse()
        {
            bool exists = await _repository.ExistsWithName("Ghost", 1, CancellationToken.None);

            exists.Should().BeFalse();
        }

        [Fact]
        public async Task ExistsAnotherWithName_SameNameDifferentId_ReturnsTrue()
        {
            User user = await CreateTestUserAsync();
            Category category1 = new Category { UserId = user.Id, Name = "Duplicate", Type = CategoryType.Income };
            Category category2 = new Category { UserId = user.Id, Name = "Duplicate", Type = CategoryType.Consumption };
            _context.Categories.AddRange(category1, category2);
            await _context.SaveChangesAsync();

            bool exists = await _repository.ExistsAnotherWithName("Duplicate", category1.Id, user.Id, CancellationToken.None);

            exists.Should().BeTrue();
        }

        [Fact]
        public async Task ExistsAnotherWithName_OnlySameCategory_ReturnsFalse()
        {
            User user = await CreateTestUserAsync();
            Category category = new Category { UserId = user.Id, Name = "Unique", Type = CategoryType.Income };
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            bool exists = await _repository.ExistsAnotherWithName("Unique", category.Id, user.Id, CancellationToken.None);

            exists.Should().BeFalse();
        }
    }
}