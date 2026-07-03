using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.Logging;
using MoneyKeeper.Application.Common;
using MoneyKeeper.Application.Common.Services;
using MoneyKeeper.Application.Common.Validation;
using MoneyKeeper.Application.Contracts.Operation;
using MoneyKeeper.Application.Services;
using MoneyKeeper.Core.Common;
using MoneyKeeper.Core.Common.Repositories;
using MoneyKeeper.Core.Enums;
using MoneyKeeper.Core.Models;
using MoneyKeeper.TestsCommons;
using MoneyKeeper.TestsCommons.MockHelpers;
using Moq;

namespace MoneyKeeper.Application.Tests
{
    public class OperationsServiceTests
    {
        private readonly Mock<IOperationsRepository> _operationsRepoMock = new();
        private readonly Mock<IAccountsRepository> _accountsRepoMock = new();
        private readonly Mock<ICategoriesRepository> _categoriesRepoMock = new();
        private readonly Mock<ITransitionsRepository> _transitionsRepoMock = new();
        private readonly Mock<IBalanceChangingsRepository> _balanceChangingsRepoMock = new();
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly Mock<ICommonBalanceOperationsRepository> _commonBalanceRepoMock = new();
        private readonly Mock<IValidator<IOperationOwnershipValidationModel>> _operationValidatorMock = new();
        private readonly Mock<IValidator<ICategoryOwnershipValidationModel>> _categoryValidatorMock = new();
        private readonly Mock<IValidator<IAccountOwnershipValidationModel>> _accountValidatorMock = new();
        private readonly Mock<ILogger<OperationsService>> _loggerMock = new();

        private OperationsService CreateService()
        {
            return new OperationsService(
                _operationsRepoMock.Object,
                _accountsRepoMock.Object,
                _categoriesRepoMock.Object,
                _unitOfWorkMock.Object,
                _transitionsRepoMock.Object,
                _balanceChangingsRepoMock.Object,
                _commonBalanceRepoMock.Object,
                _operationValidatorMock.Object,
                _categoryValidatorMock.Object,
                _accountValidatorMock.Object,
                _loggerMock.Object
            );
        }

        [Fact]
        public async Task GetAll_NoFilters_ReturnsAllOperationsForUser()
        {
            int userId = 1;
            OperationQueryParameters parameters = new OperationQueryParameters();
            List<Account> userAccounts = new List<Account> { new Account { Id = 10, UserId = userId } };
            List<Category> userCategories = new List<Category> { new Category { Id = 5, UserId = userId } };
            IQueryable<Operation> operations = new List<Operation>
            {
                new Operation { Id = 1, AccountId = 10, CategoryId = 5, Account = new Account { Name = "A" }, Category = new Category { Name = "C", Type = CategoryType.Income } }
            }.AsQueryable();

            _accountsRepoMock.SetupGetAllByUserIdForAccounts(userId, userAccounts);
            _categoriesRepoMock.SetupGetAllByUserIdForCategories(userId, userCategories);
            _operationsRepoMock.SetupGetAllByUserIdForOperations(userId, operations);
            _operationsRepoMock.SetupGetAllPagedAsyncRealistic();

            OperationsService service = CreateService();
            Result<PagedResult<OperationResponse>> result = await service.GetAll(parameters, userId, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Items.Should().HaveCount(1);
            result.Value.TotalCount.Should().Be(1);
        }

        [Fact]
        public async Task GetAll_NoAccounts_ReturnsEmpty()
        {
            int userId = 1;
            OperationQueryParameters parameters = new OperationQueryParameters();
            List<Account> userAccounts = new List<Account>();
            List<Category> userCategories = new List<Category> { new Category { Id = 5 } };

            _accountsRepoMock.SetupGetAllByUserIdForAccounts(userId, userAccounts);
            _categoriesRepoMock.SetupGetAllByUserIdForCategories(userId, userCategories);

            OperationsService service = CreateService();
            Result<PagedResult<OperationResponse>> result = await service.GetAll(parameters, userId, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Items.Should().BeEmpty();
            result.Value.TotalCount.Should().Be(0);
        }

        [Fact]
        public async Task Add_ValidationFails_ReturnsError()
        {
            OperationCreationCommand command = OperationsServiceMockHelper.OperationCreationCommand;
            _categoryValidatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidationTestData.ValidResult);
            _accountValidatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidationTestData.AccountNotFound);

            OperationsService service = CreateService();
            Result<OperationResponse> result = await service.Add(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.Error.ErrorCode.Should().Be(ErrorCodes.ACCOUNT_NOT_FOUND);
            _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(), Times.Never);
        }

        [Fact]
        public async Task Add_Income_Success()
        {
            OperationCreationCommand command = OperationsServiceMockHelper.OperationCreationCommand;
            Account account = new Account { Id = 10, Balance = 500 };
            Category category = new Category { Id = 5, Type = CategoryType.Income };

            _categoryValidatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidationTestData.ValidResult);
            _accountValidatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidationTestData.ValidResult);
            _accountsRepoMock.SetupGetAccountById(10, account);
            _categoriesRepoMock.SetupGetCategoryById(5, category);
            _accountsRepoMock.SetupTryDeposit(10, 100, 600m);
            _unitOfWorkMock.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);

            OperationsService service = CreateService();
            Result<OperationResponse> result = await service.Add(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            _operationsRepoMock.Verify(r => r.AddAsync(It.IsAny<Operation>(), It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task Add_Consumption_InsufficientFunds_ReturnsError()
        {
            OperationCreationCommand command = OperationsServiceMockHelper.OperationCreationCommand;
            Account account = new Account { Id = 10, Balance = 50 };
            Category category = new Category { Id = 5, Type = CategoryType.Consumption };

            _categoryValidatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidationTestData.ValidResult);
            _accountValidatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidationTestData.ValidResult);
            _accountsRepoMock.SetupGetAccountById(10, account);
            _categoriesRepoMock.SetupGetCategoryById(5, category);
            _accountsRepoMock.SetupTryWithdraw(10, 100, null); // недостаточно
            _unitOfWorkMock.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.RollbackTransactionAsync()).Returns(Task.CompletedTask);

            OperationsService service = CreateService();
            Result<OperationResponse> result = await service.Add(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.Error.ErrorCode.Should().Be(ErrorCodes.NOT_ENOUGH_MONEY);
            _unitOfWorkMock.Verify(u => u.RollbackTransactionAsync(), Times.Once);
            _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(), Times.Never);
        }

        [Fact]
        public async Task Add_ExceptionThrown_ReturnsInternalError()
        {
            OperationCreationCommand command = OperationsServiceMockHelper.OperationCreationCommand;
            Account account = new Account { Id = 10, Balance = 500 };
            Category category = new Category { Id = 5, Type = CategoryType.Income };

            _categoryValidatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidationTestData.ValidResult);
            _accountValidatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidationTestData.ValidResult);
            _accountsRepoMock.SetupGetAccountById(10, account);
            _categoriesRepoMock.SetupGetCategoryById(5, category);
            _accountsRepoMock.SetupTryDeposit(10, 100, 600m);
            _unitOfWorkMock.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.CommitTransactionAsync()).ThrowsAsync(new InvalidOperationException());
            _unitOfWorkMock.Setup(u => u.RollbackTransactionAsync()).Returns(Task.CompletedTask);

            OperationsService service = CreateService();
            Result<OperationResponse> result = await service.Add(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.Error.ErrorCode.Should().Be(ErrorCodes.UNKNOWN_OPERATION_CREATION_ERROR);
            _unitOfWorkMock.Verify(u => u.RollbackTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task Delete_ValidationFails_ReturnsError()
        {
            OperationDeletionCommand command = OperationsServiceMockHelper.OperationDeletionCommand;
            _operationValidatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidationTestData.UserAccessDenied);

            OperationsService service = CreateService();
            Result<bool> result = await service.Delete(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.Error.ErrorCode.Should().Be(ErrorCodes.USER_ACCESS_DENIED);
            _operationsRepoMock.Verify(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Delete_SubsequentOperationsExist_ReturnsError()
        {
            OperationDeletionCommand command = OperationsServiceMockHelper.OperationDeletionCommand;
            Operation operation = new Operation { Id = 1, AccountId = 10, Account = new Account { Id = 10 }, Date = DateTime.Now };

            _operationValidatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidationTestData.ValidResult);
            _operationsRepoMock.SetupGetOperationById(1, operation);
            _operationsRepoMock.SetupAreAnyConsumptionOperationsAfterAsync(10, operation.Date, true);

            OperationsService service = CreateService();
            Result<bool> result = await service.Delete(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.Error.ErrorCode.Should().Be(ErrorCodes.MONEY_CANNOT_BE_RESTORED);
            _operationsRepoMock.Verify(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Delete_Success()
        {
            OperationDeletionCommand command = OperationsServiceMockHelper.OperationDeletionCommand;
            Operation operation = new Operation { Id = 1, AccountId = 10, Account = new Account { Id = 10 }, Date = DateTime.Now, OldAccountBalance = 200 };

            _operationValidatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidationTestData.ValidResult);
            _operationsRepoMock.SetupGetOperationById(1, operation);
            _operationsRepoMock.SetupAreAnyConsumptionOperationsAfterAsync(10, operation.Date, false);
            _transitionsRepoMock.SetupAreAnySourceTransitionsAfterAsync(10, operation.Date, false);
            _balanceChangingsRepoMock.SetupAreAnyBalanceChangingsAfterAsync(10, operation.Date, false);
            _unitOfWorkMock.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);

            OperationsService service = CreateService();
            Result<bool> result = await service.Delete(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            _operationsRepoMock.Verify(r => r.DeleteAsync(1, It.IsAny<CancellationToken>()), Times.Once);
            _commonBalanceRepoMock.Verify(r => r.RecalculateAllTailsAsync(10, operation.Date, 200, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Delete_ExceptionInTransaction_ReturnsInternalError()
        {
            OperationDeletionCommand command = OperationsServiceMockHelper.OperationDeletionCommand;
            Operation operation = new Operation { Id = 1, AccountId = 10, Account = new Account { Id = 10 }, Date = DateTime.Now, OldAccountBalance = 200 };

            _operationValidatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidationTestData.ValidResult);
            _operationsRepoMock.SetupGetOperationById(1, operation);
            _operationsRepoMock.SetupAreAnyConsumptionOperationsAfterAsync(10, operation.Date, false);
            _transitionsRepoMock.SetupAreAnySourceTransitionsAfterAsync(10, operation.Date, false);
            _balanceChangingsRepoMock.SetupAreAnyBalanceChangingsAfterAsync(10, operation.Date, false);
            _unitOfWorkMock.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.CommitTransactionAsync()).ThrowsAsync(new InvalidOperationException());
            _unitOfWorkMock.Setup(u => u.RollbackTransactionAsync()).Returns(Task.CompletedTask);

            OperationsService service = CreateService();
            Result<bool> result = await service.Delete(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.Error.ErrorCode.Should().Be(ErrorCodes.UNKNOWN_OPERATION_DELETING_ERROR);
        }

        [Fact]
        public async Task Update_ValidationFails_ReturnsError()
        {
            OperationUpdateCommand command = OperationsServiceMockHelper.OperationUpdateCommand;
            _operationValidatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidationTestData.ValidResult);
            _categoryValidatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidationTestData.UserAccessDenied);

            OperationsService service = CreateService();
            Result<OperationResponse> result = await service.Update(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.Error.ErrorCode.Should().Be(ErrorCodes.USER_ACCESS_DENIED);
        }

        [Fact]
        public async Task Update_SubsequentOperationsExist_ReturnsError()
        {
            OperationUpdateCommand command = OperationsServiceMockHelper.OperationUpdateCommand;
            Operation operation = new Operation { Id = 1, AccountId = 10, Date = DateTime.Now };

            _operationValidatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidationTestData.ValidResult);
            _categoryValidatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidationTestData.ValidResult);
            _accountValidatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidationTestData.ValidResult);
            _operationsRepoMock.SetupGetOperationById(1, operation);
            _accountsRepoMock.SetupGetAccountById(10, new Account { Id = 10 });
            _categoriesRepoMock.SetupGetCategoryById(5, new Category { Id = 5 });
            _operationsRepoMock.SetupAreAnyConsumptionOperationsAfterAsync(10, operation.Date, true);

            OperationsService service = CreateService();
            Result<OperationResponse> result = await service.Update(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.Error.ErrorCode.Should().Be(ErrorCodes.MONEY_CANNOT_BE_RESTORED);
        }

        [Fact]
        public async Task Update_SameAccount_DescriptionOnly_Success()
        {
            OperationUpdateCommand specificCommand = new OperationUpdateCommand(1, 1, 10, 5, 150, "Обновление");
            Operation operation = new Operation
            {
                Id = 1,
                AccountId = 10,
                CategoryId = 5,
                Sum = 150,
                OldAccountBalance = 200,
                NewAccountBalance = 50,
                Account = new Account { Id = 10, Name = "Acc" },
                Category = new Category { Id = 5, Name = "Cat", Type = CategoryType.Income },
                Date = DateTime.Now
            };
            Account account = new Account { Id = 10, Balance = 500 };
            Category category = new Category { Id = 5, Type = CategoryType.Income };

            _operationValidatorMock.Setup(v => v.ValidateAsync(specificCommand, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidationTestData.ValidResult);
            _categoryValidatorMock.Setup(v => v.ValidateAsync(specificCommand, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidationTestData.ValidResult);
            _accountValidatorMock.Setup(v => v.ValidateAsync(specificCommand, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidationTestData.ValidResult);
            _operationsRepoMock.SetupGetOperationById(1, operation);
            _accountsRepoMock.SetupGetAccountById(10, account);
            _categoriesRepoMock.SetupGetCategoryById(5, category);
            _operationsRepoMock.SetupAreAnyConsumptionOperationsAfterAsync(10, operation.Date, false);
            _transitionsRepoMock.SetupAreAnySourceTransitionsAfterAsync(10, operation.Date, false);
            _balanceChangingsRepoMock.SetupAreAnyBalanceChangingsAfterAsync(10, operation.Date, false);
            _operationsRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Operation>(), It.IsAny<CancellationToken>())).ReturnsAsync(operation);
            _unitOfWorkMock.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);

            OperationsService service = CreateService();
            Result<OperationResponse> result = await service.Update(specificCommand, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            _commonBalanceRepoMock.Verify(r => r.RecalculateAllTailsAsync(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Update_SameAccount_IncreaseConsumption_NotEnoughFunds_ReturnsError()
        {
            OperationUpdateCommand command = new OperationUpdateCommand(1, 1, 10, 5, 250, "Увеличили расход");
            Operation operation = new Operation
            {
                Id = 1,
                AccountId = 10,
                CategoryId = 5,
                Sum = 150,
                OldAccountBalance = 200,
                NewAccountBalance = 50,
                Account = new Account { Id = 10 },
                Category = new Category { Id = 5, Type = CategoryType.Consumption },
                Date = DateTime.Now
            };
            Account account = new Account { Id = 10 };
            Category category = new Category { Id = 5, Type = CategoryType.Consumption };

            _operationValidatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidationTestData.ValidResult);
            _categoryValidatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidationTestData.ValidResult);
            _accountValidatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidationTestData.ValidResult);
            _operationsRepoMock.SetupGetOperationById(1, operation);
            _accountsRepoMock.SetupGetAccountById(10, account);
            _categoriesRepoMock.SetupGetCategoryById(5, category);
            _operationsRepoMock.SetupAreAnyConsumptionOperationsAfterAsync(10, operation.Date, false);
            _transitionsRepoMock.SetupAreAnySourceTransitionsAfterAsync(10, operation.Date, false);
            _balanceChangingsRepoMock.SetupAreAnyBalanceChangingsAfterAsync(10, operation.Date, false);
            _unitOfWorkMock.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.RollbackTransactionAsync()).Returns(Task.CompletedTask);

            OperationsService service = CreateService();
            Result<OperationResponse> result = await service.Update(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.Error.ErrorCode.Should().Be(ErrorCodes.NOT_ENOUGH_MONEY);
            _unitOfWorkMock.Verify(u => u.RollbackTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task Update_ChangeAccount_Success()
        {
            OperationUpdateCommand command = new OperationUpdateCommand(1, 1, 20, 5, 100, "Перенос");
            Operation operation = new Operation
            {
                Id = 1,
                AccountId = 10,
                CategoryId = 5,
                Sum = 100,
                OldAccountBalance = 200,
                NewAccountBalance = 100,
                Account = new Account { Id = 10 },
                Category = new Category { Id = 5, Type = CategoryType.Income },
                Date = DateTime.Now
            };
            Account newAccount = new Account { Id = 20, Balance = 300 };
            Category category = new Category { Id = 5, Type = CategoryType.Income };

            _operationValidatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidationTestData.ValidResult);
            _categoryValidatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidationTestData.ValidResult);
            _accountValidatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidationTestData.ValidResult);
            _operationsRepoMock.SetupGetOperationById(1, operation);
            _accountsRepoMock.SetupGetAccountById(20, newAccount);
            _categoriesRepoMock.SetupGetCategoryById(5, category);
            _operationsRepoMock.SetupAreAnyConsumptionOperationsAfterAsync(10, operation.Date, false);
            _transitionsRepoMock.SetupAreAnySourceTransitionsAfterAsync(10, operation.Date, false);
            _balanceChangingsRepoMock.SetupAreAnyBalanceChangingsAfterAsync(10, operation.Date, false);
            _accountsRepoMock.SetupTryDeposit(20, 100, 400m);
            _unitOfWorkMock.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);

            OperationsService service = CreateService();
            Result<OperationResponse> result = await service.Update(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            _operationsRepoMock.Verify(r => r.DeleteAsync(1, It.IsAny<CancellationToken>()), Times.Once);
            _commonBalanceRepoMock.Verify(r => r.RecalculateAllTailsAsync(10, operation.Date, 200, It.IsAny<CancellationToken>()), Times.Once);
            _operationsRepoMock.Verify(r => r.AddAsync(It.IsAny<Operation>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Update_ExceptionInTransaction_ReturnsInternalError()
        {
            OperationUpdateCommand command = new OperationUpdateCommand(1, 1, 10, 5, 150, "Update");
            Operation operation = new Operation
            {
                Id = 1,
                AccountId = 10,
                CategoryId = 5,
                Sum = 150,
                OldAccountBalance = 200,
                NewAccountBalance = 50,
                Account = new Account { Id = 10 },
                Category = new Category { Id = 5, Type = CategoryType.Income },
                Date = DateTime.Now
            };

            _operationValidatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidationTestData.ValidResult);
            _categoryValidatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidationTestData.ValidResult);
            _accountValidatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidationTestData.ValidResult);
            _operationsRepoMock.SetupGetOperationById(1, operation);
            _accountsRepoMock.SetupGetAccountById(10, new Account { Id = 10 });
            _categoriesRepoMock.SetupGetCategoryById(5, new Category { Id = 5, Type = CategoryType.Income });
            _operationsRepoMock.SetupAreAnyConsumptionOperationsAfterAsync(10, operation.Date, false);
            _transitionsRepoMock.SetupAreAnySourceTransitionsAfterAsync(10, operation.Date, false);
            _balanceChangingsRepoMock.SetupAreAnyBalanceChangingsAfterAsync(10, operation.Date, false);
            _unitOfWorkMock.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.CommitTransactionAsync()).ThrowsAsync(new InvalidOperationException());
            _unitOfWorkMock.Setup(u => u.RollbackTransactionAsync()).Returns(Task.CompletedTask);

            OperationsService service = CreateService();
            Result<OperationResponse> result = await service.Update(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.Error.ErrorCode.Should().Be(ErrorCodes.UNKNOWN_OPERATION_UPDATING_ERROR);
        }
    }
}