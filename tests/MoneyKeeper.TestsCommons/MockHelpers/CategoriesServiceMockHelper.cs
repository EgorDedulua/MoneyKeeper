using MoneyKeeper.Application.Contracts.Category;
using MoneyKeeper.Core.Common.Repositories;
using Moq;
using MoneyKeeper.Core.Enums;
using MoneyKeeper.Core.Models;

namespace MoneyKeeper.TestsCommons.MockHelpers
{
    public static class CategoriesServiceMockHelper
    {
        public static CategoryCreationCommand CategoryCreationCommand => new(1, "Зарплата", "Описание", CategoryType.Income);

        public static CategoryDeletionCommand CategoryDeletionCommand => new(1, 1);

        public static CategoryUpdateCommand CategoryUpdateCommand => new(1, 1, "Еда", null, CategoryType.Consumption);

        public static void SetupExistsWithName(this Mock<ICategoriesRepository> mock, string name, int userId, bool value)
        {
            mock.Setup(r => r.ExistsWithName(name, userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(value);
        }

        public static void SetupExistsAnotherWithName(this Mock<ICategoriesRepository> mock, string name, int categoryId, int userId, bool value)
        {
            mock.Setup(r => r.ExistsAnotherWithName(name, categoryId, userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(value);
        }

        public static void SetupAreAnyOperationsWithCategory(this Mock<IOperationsRepository> mock, int categoryId, bool value)
        {
            mock.Setup(r => r.AreAnyOperationsWithCategoryAsync(categoryId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(value);
        }

        public static void SetupGetById(this Mock<ICategoriesRepository> mock, int categoryId, Category category)
        {
            mock.Setup(r => r.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(category);
        }

        public static void SetupUpdate(this Mock<ICategoriesRepository> mock,  Category category)
        {
            mock.Setup(r => r.UpdateAsync(category, It.IsAny<CancellationToken>()))
                .ReturnsAsync(category);
        }

        public static void SetupGetAllByUserId(this Mock<ICategoriesRepository> mock, int userId, IQueryable<Category> returnValue)
        {
            mock.Setup(r => r.GetAllByUserId(userId)).Returns(returnValue);
        }

        public static void SetupGetAllPaged(this Mock<ICategoriesRepository> mock)
        {
            mock.Setup(r => r.GetAllPagedAsync(It.IsAny<IQueryable<Category>>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((IQueryable<Category> query, int page, int pageSize, CancellationToken ct) =>
                {
                    List<Category> all = query.ToList();
                    int totalCount = all.Count;
                    List<Category> paged = all.Skip((page - 1) * pageSize).Take(pageSize).ToList();
                    return (paged, totalCount);
                });
        }
    }
}
