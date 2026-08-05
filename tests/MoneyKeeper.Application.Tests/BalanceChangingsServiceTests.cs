using AutoMapper;
using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;
using MoneyKeeper.Application.Common;
using MoneyKeeper.Application.Common.Services;
using MoneyKeeper.Application.Common.Validation;
using MoneyKeeper.Application.Contracts.BalanceChanging;
using MoneyKeeper.Application.Mappings;
using MoneyKeeper.Application.Services;
using MoneyKeeper.Core.Common;
using MoneyKeeper.Core.Common.Repositories;
using MoneyKeeper.Core.Models;
using MoneyKeeper.TestsCommons;
using MoneyKeeper.TestsCommons.MockHelpers;
using Moq;

namespace MoneyKeeper.Application.Tests
{
    public class BalanceChangingsServiceTests
    {
        private readonly Mock<IBalanceChangingsRepository> _balanceChangingsRepoMock = new();
        private readonly Mock<IAccountsRepository> _accountsRepoMock = new();
        private readonly Mock<IOperationsRepository> _operationsRepoMock = new();
        private readonly Mock<ITransitionsRepository> _transitionsRepoMock = new();
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly Mock<ICommonBalanceOperationsRepository> _commonBalanceRepoMock = new();
        private readonly Mock<IValidator<IAccountOwnershipValidationModel>> _accountOwnershipValidatorMock = new();
        private readonly Mock<IValidator<IBalanceChangingOwnershipValidationModel>> _balanceChangingOwnershipValidatorMock = new();
        private readonly IMapper _mapper;

        public BalanceChangingsServiceTests()
        {
            MapperConfiguration config = new MapperConfiguration(
                cfg =>
                {
                    cfg.AddProfile<FiltersProfile>();
                    cfg.AddProfile<ModelsProfile>();
                },
                new NullLoggerFactory()
            );

            _mapper = config.CreateMapper();
        }

        private BalanceChangingsService CreateService()
        {
            return new BalanceChangingsService(
                _balanceChangingsRepoMock.Object,
                _operationsRepoMock.Object,
                _transitionsRepoMock.Object,
                _unitOfWorkMock.Object,
                _accountsRepoMock.Object,
                _commonBalanceRepoMock.Object,
                _accountOwnershipValidatorMock.Object,
                _balanceChangingOwnershipValidatorMock.Object,
                _mapper
            );
        }

        [Fact]
        public async Task Add_ValidationFails_ReturnsError()
        {
            BalanceChangingCreationCommand command = BalanceChangingsServiceMockHelper.CreationCommand;
            _accountOwnershipValidatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidationTestData.AccountNotFound);

            BalanceChangingsService service = CreateService();
            Result<BalanceChangingResponse> result = await service.Add(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error!.ErrorCode.Should().Be(ErrorCodes.ACCOUNT_NOT_FOUND);
            _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(), Times.Never);
        }

        [Fact]
        public async Task Add_Success()
        {
            BalanceChangingCreationCommand command = BalanceChangingsServiceMockHelper.CreationCommand;
            Account account = new Account { Id = 10, Balance = 200, Name = "Test" };
            _accountOwnershipValidatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidationTestData.ValidResult);
            _accountsRepoMock.SetupGetAccountById(10, account);
            _accountsRepoMock.SetupUpdateBalanceAsync(10, 500);
            _balanceChangingsRepoMock.SetupAddBalanceChanging();
            _unitOfWorkMock.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);

            BalanceChangingsService service = CreateService();
            Result<BalanceChangingResponse> result = await service.Add(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            _accountsRepoMock.Verify(r => r.UpdateBalanceAsync(10, 500, It.IsAny<CancellationToken>()), Times.Once);
            _balanceChangingsRepoMock.Verify(r => r.AddAsync(It.IsAny<BalanceChanging>(), It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task Add_ExceptionThrown_ReturnsInternalError()
        {
            BalanceChangingCreationCommand command = BalanceChangingsServiceMockHelper.CreationCommand;
            Account account = new Account { Id = 10, Balance = 200 };
            _accountOwnershipValidatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidationTestData.ValidResult);
            _accountsRepoMock.SetupGetAccountById(10, account);
            _unitOfWorkMock.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.CommitTransactionAsync()).ThrowsAsync(new InvalidOperationException());
            _unitOfWorkMock.Setup(u => u.RollbackTransactionAsync()).Returns(Task.CompletedTask);

            BalanceChangingsService service = CreateService();
            Result<BalanceChangingResponse> result = await service.Add(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error!.ErrorCode.Should().Be(ErrorCodes.UNKNOWN_BALANCE_CHANGING_CREATION_ERROR);
            _unitOfWorkMock.Verify(u => u.RollbackTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task Delete_ValidationFails_ReturnsError()
        {
            BalanceChangingDeletionCommand command = BalanceChangingsServiceMockHelper.DeletionCommand;
            _balanceChangingOwnershipValidatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidationTestData.UserAccessDenied);

            BalanceChangingsService service = CreateService();
            Result<bool> result = await service.Delete(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.Error.ErrorCode.Should().Be(ErrorCodes.USER_ACCESS_DENIED);
            _balanceChangingsRepoMock.Verify(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Delete_SubsequentOperationsExist_ReturnsError()
        {
            BalanceChangingDeletionCommand command = BalanceChangingsServiceMockHelper.DeletionCommand;
            BalanceChanging balanceChanging = new BalanceChanging
            {
                Id = 1,
                AccountId = 10,
                Account = new Account { Id = 10 },
                Date = DateTime.Now
            };
            _balanceChangingOwnershipValidatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidationTestData.ValidResult);
            _balanceChangingsRepoMock.SetupGetBalanceChangingById(1, balanceChanging);
            _operationsRepoMock.SetupAreAnyConsumptionOperationsAfterAsync(10, balanceChanging.Date, true);

            BalanceChangingsService service = CreateService();
            Result<bool> result = await service.Delete(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.Error.ErrorCode.Should().Be(ErrorCodes.MONEY_CANNOT_BE_RESTORED);
            _balanceChangingsRepoMock.Verify(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Delete_Success()
        {
            BalanceChangingDeletionCommand command = BalanceChangingsServiceMockHelper.DeletionCommand;
            BalanceChanging balanceChanging = new BalanceChanging
            {
                Id = 1,
                AccountId = 10,
                Account = new Account { Id = 10 },
                Date = DateTime.Now,
                OldAccountBalance = 200
            };
            _balanceChangingOwnershipValidatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidationTestData.ValidResult);
            _balanceChangingsRepoMock.SetupGetBalanceChangingById(1, balanceChanging);
            _operationsRepoMock.SetupAreAnyConsumptionOperationsAfterAsync(10, balanceChanging.Date, false);
            _transitionsRepoMock.SetupAreAnySourceTransitionsAfterAsync(10, balanceChanging.Date, false);
            _balanceChangingsRepoMock.SetupAreAnyBalanceChangingsAfterAsync(10, balanceChanging.Date, false);
            _balanceChangingsRepoMock.SetupDeleteBalanceChanging();
            _commonBalanceRepoMock.SetupRecalculateAllTailsAsync();
            _unitOfWorkMock.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);

            BalanceChangingsService service = CreateService();
            Result<bool> result = await service.Delete(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            _balanceChangingsRepoMock.Verify(r => r.DeleteAsync(1, It.IsAny<CancellationToken>()), Times.Once);
            _commonBalanceRepoMock.Verify(r => r.RecalculateAllTailsAsync(10, balanceChanging.Date, 200, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Delete_ExceptionInTransaction_ReturnsInternalError()
        {
            BalanceChangingDeletionCommand command = BalanceChangingsServiceMockHelper.DeletionCommand;
            BalanceChanging balanceChanging = new BalanceChanging
            {
                Id = 1,
                AccountId = 10,
                Account = new Account { Id = 10 },
                Date = DateTime.Now,
                OldAccountBalance = 200
            };
            _balanceChangingOwnershipValidatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidationTestData.ValidResult);
            _balanceChangingsRepoMock.SetupGetBalanceChangingById(1, balanceChanging);
            _operationsRepoMock.SetupAreAnyConsumptionOperationsAfterAsync(10, balanceChanging.Date, false);
            _transitionsRepoMock.SetupAreAnySourceTransitionsAfterAsync(10, balanceChanging.Date, false);
            _balanceChangingsRepoMock.SetupAreAnyBalanceChangingsAfterAsync(10, balanceChanging.Date, false);
            _unitOfWorkMock.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.CommitTransactionAsync()).ThrowsAsync(new InvalidOperationException());
            _unitOfWorkMock.Setup(u => u.RollbackTransactionAsync()).Returns(Task.CompletedTask);

            BalanceChangingsService service = CreateService();
            Result<bool> result = await service.Delete(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.Error.ErrorCode.Should().Be(ErrorCodes.UNKNOWN_BALANCE_CHANGING_DELETION_ERROR);
        }

        [Fact]
        public async Task GetAll_ValidRequest_ReturnsPagedResult()
        {
            int userId = 1;
            BalanceChangingQueryParameters parameters = new BalanceChangingQueryParameters();
            List<Account> userAccounts = new List<Account> { new Account { Id = 10 } };
            IQueryable<BalanceChanging> changings = new List<BalanceChanging>
            {
                new BalanceChanging { Id = 1, AccountId = 10, Account = new Account { Name = "A" } }
            }.AsQueryable();
            _accountsRepoMock.SetupGetAllByUserIdAccounts(userId, userAccounts);
            _balanceChangingsRepoMock.SetupGetAllByUserIdBalanceChangings(userId, changings);
            _balanceChangingsRepoMock.SetupGetAllPagedAsyncRealistic();

            BalanceChangingsService service = CreateService();
            Result<PagedResult<BalanceChangingResponse>> result = await service.GetAll(parameters, userId, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Items.Should().HaveCount(1);
            result.Value.TotalCount.Should().Be(1);
        }

        [Fact]
        public async Task GetAll_NoAccounts_ReturnsEmpty()
        {
            int userId = 1;
            BalanceChangingQueryParameters parameters = new BalanceChangingQueryParameters();
            List<Account> userAccounts = new List<Account>();
            _accountsRepoMock.SetupGetAllByUserIdAccounts(userId, userAccounts);

            BalanceChangingsService service = CreateService();
            Result<PagedResult<BalanceChangingResponse>> result = await service.GetAll(parameters, userId, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Items.Should().BeEmpty();
            result.Value.TotalCount.Should().Be(0);
        }

        [Fact]
        public async Task Update_ValidationFails_ReturnsError()
        {
            BalanceChangingUpdateCommand command = BalanceChangingsServiceMockHelper.UpdateCommand;
            _balanceChangingOwnershipValidatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidationTestData.ValidResult);
            _accountOwnershipValidatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidationTestData.BalanceChangingNotFound);

            BalanceChangingsService service = CreateService();
            Result<BalanceChangingResponse> result = await service.Update(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.Error.ErrorCode.Should().Be(ErrorCodes.BALANCE_CHANGING_NOT_FOUND);
        }

        [Fact]
        public async Task Update_SubsequentOperationsExist_ReturnsError()
        {
            BalanceChangingUpdateCommand command = BalanceChangingsServiceMockHelper.UpdateCommand;
            BalanceChanging balanceChanging = new BalanceChanging
            {
                Id = 1,
                AccountId = 10,
                Date = DateTime.Now
            };
            Account account = new Account { Id = 10, Balance = 300 };
            _balanceChangingOwnershipValidatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidationTestData.ValidResult);
            _accountOwnershipValidatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidationTestData.ValidResult);
            _balanceChangingsRepoMock.SetupGetBalanceChangingById(1, balanceChanging);
            _accountsRepoMock.SetupGetAccountById(10, account);
            _operationsRepoMock.SetupAreAnyConsumptionOperationsAfterAsync(10, balanceChanging.Date, true);

            BalanceChangingsService service = CreateService();
            Result<BalanceChangingResponse> result = await service.Update(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.Error.ErrorCode.Should().Be(ErrorCodes.MONEY_CANNOT_BE_RESTORED);
        }

        [Fact]
        public async Task Update_ChangeAccount_Success()
        {
            BalanceChangingUpdateCommand command = new BalanceChangingUpdateCommand(1, 1, 20, 800);
            BalanceChanging balanceChanging = new BalanceChanging
            {
                Id = 1,
                AccountId = 10,
                Date = DateTime.Now,
                OldAccountBalance = 200,
                NewAccountBalance = 500
            };
            Account oldAccount = new Account { Id = 10, Name = "Old" };
            Account newAccount = new Account { Id = 20, Balance = 400, Name = "New" };
            _balanceChangingOwnershipValidatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidationTestData.ValidResult);
            _accountOwnershipValidatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidationTestData.ValidResult);
            _balanceChangingsRepoMock.SetupGetBalanceChangingById(1, balanceChanging);
            _accountsRepoMock.SetupGetAccountById(10, oldAccount); 
            _accountsRepoMock.SetupGetAccountById(20, newAccount); 
            _operationsRepoMock.SetupAreAnyConsumptionOperationsAfterAsync(10, balanceChanging.Date, false);
            _transitionsRepoMock.SetupAreAnySourceTransitionsAfterAsync(10, balanceChanging.Date, false);
            _balanceChangingsRepoMock.SetupAreAnyBalanceChangingsAfterAsync(10, balanceChanging.Date, false);
            _commonBalanceRepoMock.SetupRecalculateAllTailsAsync();
            _balanceChangingsRepoMock.SetupDeleteBalanceChanging();
            _accountsRepoMock.SetupUpdateBalanceAsync(20, 800);
            _balanceChangingsRepoMock.SetupAddBalanceChanging();
            _unitOfWorkMock.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);

            BalanceChangingsService service = CreateService();
            Result<BalanceChangingResponse> result = await service.Update(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            _balanceChangingsRepoMock.Verify(r => r.DeleteAsync(1, It.IsAny<CancellationToken>()), Times.Once);
            _commonBalanceRepoMock.Verify(r => r.RecalculateAllTailsAsync(10, balanceChanging.Date, 200, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Update_SameAccount_ChangeValue_Success()
        {
            BalanceChangingUpdateCommand command = BalanceChangingsServiceMockHelper.UpdateCommand; 
            BalanceChanging existing = new BalanceChanging
            {
                Id = 1,
                AccountId = 10,
                Date = DateTime.Now,
                OldAccountBalance = 200,
                NewAccountBalance = 500,
                Account = new Account { Id = 10, Name = "Test" }
            };

            _accountOwnershipValidatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidationTestData.ValidResult);
            _balanceChangingOwnershipValidatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidationTestData.ValidResult);
            _balanceChangingsRepoMock.SetupGetBalanceChangingById(1, existing);
            _accountsRepoMock.SetupGetAccountById(10, new Account { Id = 10 });
            _operationsRepoMock.SetupAreAnyConsumptionOperationsAfterAsync(10, existing.Date, false);
            _transitionsRepoMock.SetupAreAnySourceTransitionsAfterAsync(10, existing.Date, false);
            _balanceChangingsRepoMock.SetupAreAnyBalanceChangingsAfterAsync(10, existing.Date, false);
            _unitOfWorkMock.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.RollbackTransactionAsync()).Returns(Task.CompletedTask);
            _commonBalanceRepoMock.SetupRecalculateAllTailsAsync();
            BalanceChanging updated = new BalanceChanging
            {
                Id = 1,
                AccountId = 10,
                Date = existing.Date,
                OldAccountBalance = 200,
                NewAccountBalance = 600,
                Account = new Account { Id = 10, Name = "Test" }
            };
            _balanceChangingsRepoMock.SetupUpdateBalanceChanging(updated);

            BalanceChangingsService service = CreateService();
            Result<BalanceChangingResponse> result = await service.Update(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.NewAccountBalance.Should().Be(600);
        }

        [Fact]
        public async Task Update_ExceptionInTransaction_ReturnsInternalError()
        {
            BalanceChangingUpdateCommand command = new BalanceChangingUpdateCommand(1, 1, 10, 900);
            BalanceChanging balanceChanging = new BalanceChanging
            {
                Id = 1,
                AccountId = 10,
                Date = DateTime.Now,
                OldAccountBalance = 200,
                NewAccountBalance = 500
            };
            Account account = new Account { Id = 10, Balance = 700 };
            _balanceChangingOwnershipValidatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidationTestData.ValidResult);
            _accountOwnershipValidatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidationTestData.ValidResult);
            _balanceChangingsRepoMock.SetupGetBalanceChangingById(1, balanceChanging);
            _accountsRepoMock.SetupGetAccountById(10, account);
            _operationsRepoMock.SetupAreAnyConsumptionOperationsAfterAsync(10, balanceChanging.Date, false);
            _transitionsRepoMock.SetupAreAnySourceTransitionsAfterAsync(10, balanceChanging.Date, false);
            _balanceChangingsRepoMock.SetupAreAnyBalanceChangingsAfterAsync(10, balanceChanging.Date, false);
            _unitOfWorkMock.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.CommitTransactionAsync()).ThrowsAsync(new InvalidOperationException());
            _unitOfWorkMock.Setup(u => u.RollbackTransactionAsync()).Returns(Task.CompletedTask);

            BalanceChangingsService service = CreateService();
            Result<BalanceChangingResponse> result = await service.Update(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.Error.ErrorCode.Should().Be(ErrorCodes.UNKNOWN_BALANCE_UPDATING_DELETION_ERROR);
        }
    }
}