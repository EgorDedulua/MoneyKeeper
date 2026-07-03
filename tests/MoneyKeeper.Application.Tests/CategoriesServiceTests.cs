using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.Logging;
using MoneyKeeper.Application.Common;
using MoneyKeeper.Application.Common.Validation;
using MoneyKeeper.Application.Contracts.Category;
using MoneyKeeper.Application.Services;
using MoneyKeeper.Application.Sorting;
using MoneyKeeper.Core.Common;
using MoneyKeeper.Core.Common.Repositories;
using MoneyKeeper.Core.Enums;
using MoneyKeeper.Core.Models;
using MoneyKeeper.TestsCommons;
using MoneyKeeper.TestsCommons.MockHelpers;
using Moq;
using System.Net;

namespace MoneyKeeper.Application.Tests
{
    public class CategoriesServiceTests
    {
        private readonly Mock<ICategoriesRepository> _categoriesRepositoryMock = new();
        private readonly Mock<IOperationsRepository> _operationsRepositoryMock = new();
        private readonly Mock<IValidator<ICategoryOwnershipValidationModel>> _categoryOwnershipvalidatorMock = new();
        private readonly Mock<ILogger<CategoriesService>> _loggerMock = new();

        private CategoriesService CreateService()
        {
            return new CategoriesService(
                _categoriesRepositoryMock.Object,
                _categoryOwnershipvalidatorMock.Object,
                _loggerMock.Object,
                _operationsRepositoryMock.Object
            );
        }

        [Fact]
        public async Task Add_WhenEverythingIsCorrect_CreatesNewCategory()
        {
            CategoryCreationCommand command = CategoriesServiceMockHelper.CategoryCreationCommand;
            _categoriesRepositoryMock.SetupExistsWithName(command.Name, command.UserId, false);
            CategoriesService service = CreateService();

            Result<CategoryResponse> result = await service.Add(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            _categoriesRepositoryMock.Verify(
                r => r.AddAsync(
                    It.Is<Category>(c =>
                        c.Name == command.Name &&
                        c.Type == command.Type &&
                        c.UserId == command.UserId &&
                        c.Description == command.Description),
                    It.IsAny<CancellationToken>()),
                Times.Once());
            result.Error.Should().BeNull();
        }

        [Fact]
        public async Task Add_WhenNameAlreadyTaken_ReturnsConflictError()
        {
            CategoryCreationCommand command = CategoriesServiceMockHelper.CategoryCreationCommand;
            _categoriesRepositoryMock.SetupExistsWithName(command.Name, command.UserId, true);
            CategoriesService service = CreateService();

            Result<CategoryResponse> result = await service.Add(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.Error.StatusCode.Should().Be((int)HttpStatusCode.Conflict);
            result.Error.ErrorCode.Should().Be(ErrorCodes.CATEGORY_NAME_ALREADY_EXISTS);
            _categoriesRepositoryMock.Verify(
                r => r.AddAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Delete_WhenEverythingIsCorrect_DeletesCategory()
        {
            CategoryDeletionCommand command = CategoriesServiceMockHelper.CategoryDeletionCommand;
            _categoryOwnershipvalidatorMock
               .Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
               .ReturnsAsync(ValidationTestData.ValidResult);
            _operationsRepositoryMock.SetupAreAnyOperationsWithCategory(command.CategoryId, false);
            CategoriesService service = CreateService();

            Result<bool> result = await service.Delete(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            _categoriesRepositoryMock
                .Verify(r => r.DeleteAsync(command.CategoryId, It.IsAny<CancellationToken>()), Times.Once());
            result.Error.Should().BeNull();
        }

        [Fact]
        public async Task Delete_WhenUserIsIncorrect_ReturnsForbiddenError()
        {
            CategoryDeletionCommand command = CategoriesServiceMockHelper.CategoryDeletionCommand;
            _categoryOwnershipvalidatorMock
               .Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
               .ReturnsAsync(ValidationTestData.UserAccessDenied);
            CategoriesService service = CreateService();

            Result<bool> result = await service.Delete(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.Error.StatusCode.Should().Be((int)HttpStatusCode.Forbidden);
            result.Error.ErrorCode.Should().Be(ErrorCodes.USER_ACCESS_DENIED);
            _categoriesRepositoryMock.Verify(
                r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Delete_WhenCategoryDoesNotExist_ReturnsNotFoundError()
        {
            CategoryDeletionCommand command = CategoriesServiceMockHelper.CategoryDeletionCommand;
            _categoryOwnershipvalidatorMock
               .Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
               .ReturnsAsync(ValidationTestData.CategoryNotFound);
            CategoriesService service = CreateService();

            Result<bool> result = await service.Delete(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.Error.StatusCode.Should().Be((int)HttpStatusCode.NotFound);
            result.Error.ErrorCode.Should().Be(ErrorCodes.CATEGORY_NOT_FOUND);
            _categoriesRepositoryMock.Verify(
                r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Delete_WhenOperationsWithCategoryExists_ReturnsUnprocessableEntityError()
        {
            CategoryDeletionCommand command = CategoriesServiceMockHelper.CategoryDeletionCommand;
            _categoryOwnershipvalidatorMock
               .Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
               .ReturnsAsync(ValidationTestData.ValidResult);
            _operationsRepositoryMock.SetupAreAnyOperationsWithCategory(command.CategoryId, true);
            CategoriesService service = CreateService();

            Result<bool> result = await service.Delete(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.Error.StatusCode.Should().Be((int)HttpStatusCode.UnprocessableEntity);
            result.Error.ErrorCode.Should().Be(ErrorCodes.CATEGORY_HAS_OPERATIONS);
            _categoriesRepositoryMock.Verify(
                r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Update_WhenEverythingIsCorrect_UpdatesCategory()
        {
            CategoryUpdateCommand command = CategoriesServiceMockHelper.CategoryUpdateCommand;
            _categoryOwnershipvalidatorMock
               .Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
               .ReturnsAsync(ValidationTestData.ValidResult);
            _categoriesRepositoryMock.SetupExistsAnotherWithName(command.Name, command.CategoryId, command.UserId, false);
            _operationsRepositoryMock.SetupAreAnyOperationsWithCategory(command.CategoryId, false);
            Category category = new Category
            {
                Id = command.CategoryId,
                UserId = command.UserId,
                Name = "Подарок",
                Description = "Описание",
                Type = CategoryType.Income
            };
            _categoriesRepositoryMock.SetupGetById(command.CategoryId, category);
            _categoriesRepositoryMock.SetupUpdate(category);
            CategoriesService service = CreateService();

            Result<CategoryResponse> result = await service.Update(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Name.Should().Be(command.Name);
            result.Value.Description.Should().Be(command.Description);
            result.Value.Type.Should().Be(command.Type);
            _categoriesRepositoryMock
                .Verify(r => r.UpdateAsync(category, It.IsAny<CancellationToken>()), Times.Once());
            result.Error.Should().BeNull();
        }

        [Fact]
        public async Task Update_WhenUserIsIncorrect_ReturnsForbiddenError()
        {
            CategoryUpdateCommand command = CategoriesServiceMockHelper.CategoryUpdateCommand;
            _categoryOwnershipvalidatorMock
               .Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
               .ReturnsAsync(ValidationTestData.UserAccessDenied);
            CategoriesService service = CreateService();

            Result<CategoryResponse> result = await service.Update(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.Error.StatusCode.Should().Be((int)HttpStatusCode.Forbidden);
            result.Error.ErrorCode.Should().Be(ErrorCodes.USER_ACCESS_DENIED);
            _categoriesRepositoryMock.Verify(
                r => r.UpdateAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Update_WhenCategoryDoesNotExist_ReturnsNotFoundError()
        {
            CategoryUpdateCommand command = CategoriesServiceMockHelper.CategoryUpdateCommand;
            _categoryOwnershipvalidatorMock
               .Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
               .ReturnsAsync(ValidationTestData.CategoryNotFound);
            CategoriesService service = CreateService();

            Result<CategoryResponse> result = await service.Update(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.Error.StatusCode.Should().Be((int)HttpStatusCode.NotFound);
            result.Error.ErrorCode.Should().Be(ErrorCodes.CATEGORY_NOT_FOUND);
            _categoriesRepositoryMock.Verify(
                r => r.UpdateAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Update_WhenNameAlreadyTaken_ReturnConflictError()
        {
            CategoryUpdateCommand command = CategoriesServiceMockHelper.CategoryUpdateCommand;
            _categoryOwnershipvalidatorMock
               .Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
               .ReturnsAsync(ValidationTestData.ValidResult);
            _categoriesRepositoryMock.SetupExistsAnotherWithName(command.Name, command.CategoryId, command.UserId, true);
            CategoriesService service = CreateService();

            Result<CategoryResponse> result = await service.Update(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.Error.StatusCode.Should().Be((int)HttpStatusCode.Conflict);
            result.Error.ErrorCode.Should().Be(ErrorCodes.CATEGORY_NAME_ALREADY_EXISTS);
            _categoriesRepositoryMock.Verify(
                r => r.UpdateAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Update_WhenCategoryIsChangedAndOperationsExists_ReturnsUnprocessableEntityError()
        {
            CategoryUpdateCommand command = CategoriesServiceMockHelper.CategoryUpdateCommand;
            _categoryOwnershipvalidatorMock
               .Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
               .ReturnsAsync(ValidationTestData.ValidResult);
            _categoriesRepositoryMock.SetupExistsAnotherWithName(command.Name, command.CategoryId, command.UserId, false);
            _operationsRepositoryMock.SetupAreAnyOperationsWithCategory(command.CategoryId, true);
            Category category = new Category
            {
                Id = command.CategoryId,
                UserId = command.UserId,
                Name = "Подарок",
                Description = "Описание",
                Type = CategoryType.Income
            };
            _categoriesRepositoryMock.SetupGetById(command.CategoryId, category);
            CategoriesService service = CreateService();

            Result<CategoryResponse> result = await service.Update(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.Error.StatusCode.Should().Be((int)HttpStatusCode.UnprocessableEntity);
            result.Error.ErrorCode.Should().Be(ErrorCodes.CATEGORY_HAS_OPERATIONS);
            _categoriesRepositoryMock.Verify(
                r => r.UpdateAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Update_WhenTypeUnchangedAndOperationsExist_UpdatesCategory()
        {
            CategoryUpdateCommand command = CategoriesServiceMockHelper.CategoryUpdateCommand;
            Category category = new Category
            {
                Id = command.CategoryId,
                UserId = command.UserId,
                Name = "Подарок",
                Description = "Описание",
                Type = CategoryType.Consumption
            };

            _categoryOwnershipvalidatorMock
                .Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidationTestData.ValidResult);
            _categoriesRepositoryMock.SetupExistsAnotherWithName(command.Name, command.CategoryId, command.UserId, false);
            _categoriesRepositoryMock.SetupGetById(command.CategoryId, category);
            _categoriesRepositoryMock.SetupUpdate(category);
            _operationsRepositoryMock.SetupAreAnyOperationsWithCategory(command.CategoryId, true);
            CategoriesService service = CreateService();

            Result<CategoryResponse> result = await service.Update(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Name.Should().Be(command.Name);
            _categoriesRepositoryMock.Verify(
                r => r.UpdateAsync(category, It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public async Task GetAll_WhenNoFilters_ReturnsAllUserCategories()
        {
            int userId = 1;
            CategoryQueryParameters parameters = new CategoryQueryParameters();
            IQueryable<Category> allCategories = new List<Category>
            {
                new Category { Id = 1, Name = "Еда", UserId = userId },
                new Category { Id = 2, Name = "Транспорт", UserId = userId }
            }.AsQueryable();
            _categoriesRepositoryMock.SetupGetAllByUserId(userId, allCategories);
            _categoriesRepositoryMock.SetupGetAllPaged();
            CategoriesService service = CreateService();

            Result<PagedResult<CategoryResponse>> result = await service.GetAll(parameters, userId, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Items.Should().HaveCount(2);
            result.Value.TotalCount.Should().Be(2);
            _categoriesRepositoryMock.Verify(r => r.GetAllByUserId(userId), Times.Once());
            _categoriesRepositoryMock.Verify(r => r.GetAllPagedAsync(It.IsAny<IQueryable<Category>>(), 1, 20, It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public async Task GetAll_WithIsOnlyIncomeFilter_ReturnsOnlyIncomeCategories()
        {
            int userId = 1;
            CategoryQueryParameters parameters = new CategoryQueryParameters { IsOnlyIncome = true };
            IQueryable<Category> allCategories = new List<Category>
            {
                new Category { Id = 1, Name = "Зарплата", Type = CategoryType.Income, UserId = userId },
                new Category { Id = 2, Name = "Кафе", Type = CategoryType.Consumption, UserId = userId }
            }.AsQueryable();
            _categoriesRepositoryMock.SetupGetAllByUserId(userId, allCategories);
            _categoriesRepositoryMock.SetupGetAllPaged();
            CategoriesService service = CreateService();

            Result<PagedResult<CategoryResponse>> result = await service.GetAll(parameters, userId, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Items.Should().HaveCount(1);
            result.Value.Items[0].Name.Should().Be("Зарплата");
        }

        [Fact]
        public async Task GetAll_WithNameSubstringFilter_ReturnsMatchingCategories()
        {
            int userId = 1;
            CategoryQueryParameters parameters = new CategoryQueryParameters { NameSubstring = "пиц" };
            IQueryable<Category> allCategories = new List<Category>
            {
                new Category { Id = 1, Name = "Пицца", UserId = userId },
                new Category { Id = 2, Name = "Напитки", UserId = userId },
                new Category { Id = 3, Name = "Пиццерия", UserId = userId }
            }.AsQueryable();
            _categoriesRepositoryMock.SetupGetAllByUserId(userId, allCategories);
            _categoriesRepositoryMock.SetupGetAllPaged();
            CategoriesService service = CreateService();

            Result<PagedResult<CategoryResponse>> result = await service.GetAll(parameters, userId, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Items.Should().HaveCount(2);
            result.Value.Items.Select(c => c.Name).Should().Contain(new[] { "Пицца", "Пиццерия" });
        }

        [Fact]
        public async Task GetAll_WithSortingByNameDescending_ReturnsSortedResult()
        {
            int userId = 1;
            CategoryQueryParameters parameters = new CategoryQueryParameters
            {
                SortBy = new List<SortCriterion> { new SortCriterion { Field = "Name", Descending = true } }
            };
            IQueryable<Category> allCategories = new List<Category>
            {
                new Category { Name = "Б", UserId = userId },
                new Category { Name = "А", UserId = userId }
            }.AsQueryable();
            _categoriesRepositoryMock.SetupGetAllByUserId(userId, allCategories);
            _categoriesRepositoryMock.SetupGetAllPaged();
            CategoriesService service = CreateService();

            Result<PagedResult<CategoryResponse>> result = await service.GetAll(parameters, userId, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Items.Should().HaveCount(2);
            result.Value.Items[0].Name.Should().Be("Б");
            result.Value.Items[1].Name.Should().Be("А");
        }

        [Fact]
        public async Task GetAll_WithPagination_ReturnsCorrectPage()
        {
            int userId = 1;
            CategoryQueryParameters parameters = new CategoryQueryParameters { Page = 2, PageSize = 1 };
            IQueryable<Category> allCategories = new List<Category>
            {
                new Category { Id = 1, Name = "А", UserId = userId },
                new Category { Id = 2, Name = "Б", UserId = userId }
            }.AsQueryable();

            _categoriesRepositoryMock.SetupGetAllByUserId(userId, allCategories);
            _categoriesRepositoryMock.SetupGetAllPaged();
            CategoriesService service = CreateService();

            Result<PagedResult<CategoryResponse>> result = await service.GetAll(parameters, userId, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Items.Should().HaveCount(1);
            result.Value.TotalCount.Should().Be(2);
            result.Value.Page.Should().Be(2);
            result.Value.PageSize.Should().Be(1);
        }

        [Fact]
        public async Task GetAll_WhenNoCategories_ReturnsEmptyList()
        {
            int userId = 1;
            CategoryQueryParameters parameters = new CategoryQueryParameters();
            IQueryable<Category> emptyQuery = new List<Category>().AsQueryable();
            _categoriesRepositoryMock.SetupGetAllByUserId(userId, emptyQuery);
            _categoriesRepositoryMock.SetupGetAllPaged();
            CategoriesService service = CreateService();

            Result<PagedResult<CategoryResponse>> result = await service.GetAll(parameters, userId, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Items.Should().BeEmpty();
            result.Value.TotalCount.Should().Be(0);
        }
    }
}
