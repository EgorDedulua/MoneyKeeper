using AutoMapper;
using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MoneyKeeper.Application.Common;
using MoneyKeeper.Application.Common.Services;
using MoneyKeeper.Application.Common.Validation;
using MoneyKeeper.Application.Contracts.Account;
using MoneyKeeper.Application.Contracts.BalanceChanging;
using MoneyKeeper.Application.Mappings;
using MoneyKeeper.Application.Services;
using MoneyKeeper.Application.Sorting;
using MoneyKeeper.Core.Common;
using MoneyKeeper.Core.Common.Repositories;
using MoneyKeeper.Core.Models;
using MoneyKeeper.TestsCommons;
using MoneyKeeper.TestsCommons.MockHelpers;
using Moq;
using System.Net;

namespace MoneyKeeper.Application.Tests
{
    public class AccountsServiceTests
    {
        private readonly Mock<IAccountsRepository> _accountsRepositoryMock = new();
        private readonly Mock<IBalanceChangingsService> _balanceChangingsServiceMock = new();
        private readonly Mock<IValidator<IAccountOwnershipValidationModel>> _accountOwnershipValidatorMock = new();
        private readonly Mock<ILogger<AccountsService>> _loggerMock = new();
        private readonly IMapper _mapper;

        public AccountsServiceTests()
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

        private AccountsService CreateService()
        {
            return new AccountsService(
                _accountsRepositoryMock.Object,
                _balanceChangingsServiceMock.Object,
                _accountOwnershipValidatorMock.Object,
                _loggerMock.Object,
                _mapper
            );
        }

        [Fact]
        public async Task Add_WhenEverythingIsCorrect_CreatesNewAccount()
        {
            AccountCreationCommand command = AccountsServiceMockHelper.AccountCreationCommand;
            _accountsRepositoryMock.SetupExistsWithName(command.Name, command.UserId, false);
            AccountsService service = CreateService();

            Result<AccountResponse> result = await service.Add(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            _accountsRepositoryMock.Verify(
                r => r.AddAsync(
                    It.Is<Account>(a =>
                        a.UserId == command.UserId &&
                        a.Name == command.Name &&
                        a.Balance == command.Balance &&
                        a.Description == command.Description &&
                        a.Target == command.Target),
                    It.IsAny<CancellationToken>()),
                Times.Once());
            result.Error.Should().BeNull();
        }

        [Fact]
        public async Task Add_WhenNameAlreadyTaken_ReturnsConflictError()
        {
            AccountCreationCommand command = AccountsServiceMockHelper.AccountCreationCommand;
            _accountsRepositoryMock.SetupExistsWithName(command.Name, command.UserId, true);
            AccountsService service = CreateService();

            Result<AccountResponse> result = await service.Add(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.Error.StatusCode.Should().Be((int)HttpStatusCode.Conflict);
            result.Error.ErrorCode.Should().Be(ErrorCodes.ACCOUNT_NAME_ALREADY_EXISTS);
            _accountsRepositoryMock.Verify(
                r => r.AddAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Delete_WhenEverythingIsCorrect_DeletesAccount()
        {
            AccountDeletionCommand command = AccountsServiceMockHelper.AccountDeletionCommand;
            _accountOwnershipValidatorMock
               .Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
               .ReturnsAsync(ValidationTestData.ValidResult);
            AccountsService service = CreateService();

            Result<bool> result = await service.Delete(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            _accountsRepositoryMock
                .Verify(r => r.DeleteAsync(command.AccountId, It.IsAny<CancellationToken>()), Times.Once());
            result.Error.Should().BeNull();
        }

        [Fact]
        public async Task Delete_WhenUserIsIncorrect_ReturnsForbiddenError()
        {
            AccountDeletionCommand command = AccountsServiceMockHelper.AccountDeletionCommand;
            _accountOwnershipValidatorMock
               .Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
               .ReturnsAsync(ValidationTestData.UserAccessDenied);
            AccountsService service = CreateService();

            Result<bool> result = await service.Delete(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.Error.StatusCode.Should().Be((int)HttpStatusCode.Forbidden);
            result.Error.ErrorCode.Should().Be(ErrorCodes.USER_ACCESS_DENIED);
            _accountsRepositoryMock.Verify(
                r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Delete_WhenAccountDoesNotExist_ReturnsNotFoundError()
        {
            AccountDeletionCommand command = AccountsServiceMockHelper.AccountDeletionCommand;
            _accountOwnershipValidatorMock
               .Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
               .ReturnsAsync(ValidationTestData.AccountNotFound);
            AccountsService service = CreateService();

            Result<bool> result = await service.Delete(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.Error.StatusCode.Should().Be((int)HttpStatusCode.NotFound);
            result.Error.ErrorCode.Should().Be(ErrorCodes.ACCOUNT_NOT_FOUND);
            _accountsRepositoryMock.Verify(
                r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Update_WhenEverythingIsCorrect_UpdatesAccount()
        {
            AccountUpdateCommand command = AccountsServiceMockHelper.AccountUpdateCommand;
            _accountOwnershipValidatorMock
                .Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidationTestData.ValidResult);
            _accountsRepositoryMock.SetupExistsAnotherWithName(command.Name, command.AccountId, command.UserId, false);
            Account account = new Account
            {
                Id = command.AccountId,
                UserId = command.UserId,
                Name = "Наличные",
                Balance = command.Balance,
                Target = null,
                Description = "Описание"
            };
            _accountsRepositoryMock.SetupGetById(command.AccountId, account);
            _accountsRepositoryMock.SetupUpdate(account);
            AccountsService service = CreateService();

            Result<AccountResponse> result = await service.Update(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Name.Should().Be(command.Name);
            result.Value.Target.Should().Be(command.Target);
            result.Value.Description.Should().Be(command.Description);
            _accountsRepositoryMock
                .Verify(r => r.UpdateAsync(account, It.IsAny<CancellationToken>()), Times.Once());
            result.Error.Should().BeNull();
        }

        [Fact]
        public async Task Update_WhenUserIsIncorrect_ReturnsForbiddenError()
        {
            AccountUpdateCommand command = AccountsServiceMockHelper.AccountUpdateCommand;
            _accountOwnershipValidatorMock
                .Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidationTestData.UserAccessDenied);
            AccountsService service = CreateService();

            Result<AccountResponse> result = await service.Update(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.Error.StatusCode.Should().Be((int)HttpStatusCode.Forbidden);
            result.Error.ErrorCode.Should().Be(ErrorCodes.USER_ACCESS_DENIED);
            _accountsRepositoryMock.Verify(
                r => r.UpdateAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Update_WhenAccountNotFound_ReturnsNotFoundError()
        {
            AccountUpdateCommand command = AccountsServiceMockHelper.AccountUpdateCommand;
            _accountOwnershipValidatorMock
                .Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidationTestData.AccountNotFound);
            AccountsService service = CreateService();

            Result<AccountResponse> result = await service.Update(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.Error.StatusCode.Should().Be((int)HttpStatusCode.NotFound);
            result.Error.ErrorCode.Should().Be(ErrorCodes.ACCOUNT_NOT_FOUND);
            _accountsRepositoryMock.Verify(
                r => r.UpdateAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Update_WhenNameAlreadyTaken_ReturnsConflictError()
        {
            AccountUpdateCommand command = AccountsServiceMockHelper.AccountUpdateCommand;
            _accountOwnershipValidatorMock
                .Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidationTestData.ValidResult);
            _accountsRepositoryMock.SetupExistsAnotherWithName(command.Name, command.AccountId, command.UserId, true);
            AccountsService service = CreateService();

            Result<AccountResponse> result = await service.Update(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.Error.StatusCode.Should().Be((int)HttpStatusCode.Conflict);
            result.Error.ErrorCode.Should().Be(ErrorCodes.ACCOUNT_NAME_ALREADY_EXISTS);
            _accountsRepositoryMock.Verify(
                r => r.UpdateAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Update_WhenBalanceIsChanged_CreatesBalanceChangingAndUpdates()
        {
            AccountUpdateCommand command = AccountsServiceMockHelper.AccountUpdateCommand;
            _accountOwnershipValidatorMock
                .Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidationTestData.ValidResult);
            _accountsRepositoryMock.SetupExistsAnotherWithName(command.Name, command.AccountId, command.UserId, false);
            Account account = new Account
            {
                Id = command.AccountId,
                UserId = command.UserId,
                Name = "Наличные",
                Balance = 600,
                Target = null,
                Description = "Описание"
            };
            _accountsRepositoryMock.SetupGetById(command.AccountId, account);
            _balanceChangingsServiceMock
                .Setup(bc => bc.Add(It.IsAny<BalanceChangingCreationCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(AccountsServiceMockHelper.BalanceChangingCreationResult);
            _accountsRepositoryMock.SetupUpdate(account);
            AccountsService service = CreateService();

            Result<AccountResponse> result = await service.Update(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Name.Should().Be(command.Name);
            result.Value.Balance.Should().Be(command.Balance);
            result.Value.Target.Should().Be(command.Target);
            result.Value.Description.Should().Be(command.Description);
            _balanceChangingsServiceMock
                .Verify(bc => bc.Add(It.IsAny<BalanceChangingCreationCommand>(), It.IsAny<CancellationToken>()), Times.Once());
            _accountsRepositoryMock
                .Verify(r => r.UpdateAsync(account, It.IsAny<CancellationToken>()), Times.Once());
            result.Error.Should().BeNull();
        }

        [Fact]
        public async Task Update_WhenBalanceChangedAndBalanceChangingFails_ReturnsErrorFromBalanceChanging()
        {
            AccountUpdateCommand command = AccountsServiceMockHelper.AccountUpdateCommand;
            _accountOwnershipValidatorMock
                .Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidationTestData.ValidResult);
            _accountsRepositoryMock.SetupExistsAnotherWithName(command.Name, command.AccountId, command.UserId, false);
            Account account = new Account
            {
                Id = command.AccountId,
                UserId = command.UserId,
                Name = "Наличные",
                Balance = 600,
                Target = null,
                Description = "Описание"
            };
            _accountsRepositoryMock.SetupGetById(command.AccountId, account);
            Result<BalanceChangingResponse> failedResult = Result<BalanceChangingResponse>.Failure(
                Error.UnprocessableEntity("Недостаточно средств", ErrorCodes.NOT_ENOUGH_MONEY));
            _balanceChangingsServiceMock
                .Setup(bc => bc.Add(It.IsAny<BalanceChangingCreationCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(failedResult);
            AccountsService service = CreateService();

            Result<AccountResponse> result = await service.Update(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.Error.ErrorCode.Should().Be(ErrorCodes.NOT_ENOUGH_MONEY);
            _accountsRepositoryMock.Verify(
                r => r.UpdateAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task GetAll_WhenNoFilters_ReturnsAllUserAccounts()
        {
            int userId = 1;
            AccountQueryParameters parameters = new AccountQueryParameters();
            IQueryable<Account> allAccounts = new List<Account>
            {
                new Account { Id = 1, Name = "Счёт 1", UserId = userId },
                new Account { Id = 2, Name = "Счёт 2", UserId = userId }
            }.AsQueryable();
            _accountsRepositoryMock.SetupGetAllByUserId(userId, allAccounts);
            _accountsRepositoryMock.SetupGetAllPagedAsyncRealistic();
            AccountsService service = CreateService();

            Result<PagedResult<AccountResponse>> result = await service.GetAll(parameters, userId, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Items.Should().HaveCount(2);
            result.Value.TotalCount.Should().Be(2);
            _accountsRepositoryMock.Verify(r => r.GetAllByUserId(userId), Times.Once());
            _accountsRepositoryMock.Verify(r => r.GetAllPagedAsync(It.IsAny<IQueryable<Account>>(), 1, 20, It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public async Task GetAll_WithMinBalanceFilter_ReturnsFilteredAccounts()
        {
            int userId = 1;
            AccountQueryParameters parameters = new AccountQueryParameters { MinBalance = 500 };
            IQueryable<Account> allAccounts = new List<Account>
            {
                new Account { Id = 1, Name = "Богатый", Balance = 1000, UserId = userId },
                new Account { Id = 2, Name = "Бедный", Balance = 100, UserId = userId }
            }.AsQueryable();
            _accountsRepositoryMock.SetupGetAllByUserId(userId, allAccounts);
            _accountsRepositoryMock.SetupGetAllPagedAsyncRealistic();
            AccountsService service = CreateService();

            Result<PagedResult<AccountResponse>> result = await service.GetAll(parameters, userId, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Items.Should().HaveCount(1);
            result.Value.Items[0].Name.Should().Be("Богатый");
        }

        [Fact]
        public async Task GetAll_WithNameSubstringFilter_ReturnsMatchingAccounts()
        {
            int userId = 1;
            AccountQueryParameters parameters = new AccountQueryParameters { NameSubstring = "основ" };
            IQueryable<Account> allAccounts = new List<Account>
            {
                new Account { Id = 1, Name = "Основной", UserId = userId },
                new Account { Id = 2, Name = "Запасной", UserId = userId }
            }.AsQueryable();
            _accountsRepositoryMock.SetupGetAllByUserId(userId, allAccounts);
            _accountsRepositoryMock.SetupGetAllPagedAsyncRealistic();
            AccountsService service = CreateService();

            Result<PagedResult<AccountResponse>> result = await service.GetAll(parameters, userId, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Items.Should().HaveCount(1);
            result.Value.Items[0].Name.Should().Be("Основной");
        }

        [Fact]
        public async Task GetAll_WithHasTargetedBalanceTrue_ReturnsOnlyTargetedAccounts()
        {
            int userId = 1;
            AccountQueryParameters parameters = new AccountQueryParameters { HasTargetedBalance = true };
            IQueryable<Account> allAccounts = new List<Account>
            {
                new Account { Id = 1, Name = "С целью", Target = 1000, UserId = userId },
                new Account { Id = 2, Name = "Без цели", Target = null, UserId = userId }
            }.AsQueryable();
            _accountsRepositoryMock.SetupGetAllByUserId(userId, allAccounts);
            _accountsRepositoryMock.SetupGetAllPagedAsyncRealistic();
            AccountsService service = CreateService();

            Result<PagedResult<AccountResponse>> result = await service.GetAll(parameters, userId, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Items.Should().HaveCount(1);
            result.Value.Items[0].Name.Should().Be("С целью");
        }

        [Fact]
        public async Task GetAll_WithHasTargetedBalanceFalse_ReturnsOnlyUntargetedAccounts()
        {
            int userId = 1;
            AccountQueryParameters parameters = new AccountQueryParameters { HasTargetedBalance = false };
            IQueryable<Account> allAccounts = new List<Account>
            {
                new Account { Id = 1, Name = "С целью", Target = 1000, UserId = userId },
                new Account { Id = 2, Name = "Без цели", Target = null, UserId = userId }
            }.AsQueryable();
            _accountsRepositoryMock.SetupGetAllByUserId(userId, allAccounts);
            _accountsRepositoryMock.SetupGetAllPagedAsyncRealistic();
            AccountsService service = CreateService();

            Result<PagedResult<AccountResponse>> result = await service.GetAll(parameters, userId, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Items.Should().HaveCount(1);
            result.Value.Items[0].Name.Should().Be("Без цели");
        }

        [Fact]
        public async Task GetAll_WithSortingByNameDescending_ReturnsSortedResult()
        {
            int userId = 1;
            AccountQueryParameters parameters = new AccountQueryParameters
            {
                SortBy = new List<SortCriterion> { new SortCriterion { Field = "Name", Descending = true } }
            };
            IQueryable<Account> allAccounts = new List<Account>
            {
                new Account { Name = "Б", UserId = userId },
                new Account { Name = "А", UserId = userId }
            }.AsQueryable();
            _accountsRepositoryMock.SetupGetAllByUserId(userId, allAccounts);
            _accountsRepositoryMock.SetupGetAllPagedAsyncRealistic();
            AccountsService service = CreateService();

            Result<PagedResult<AccountResponse>> result = await service.GetAll(parameters, userId, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Items.Should().HaveCount(2);
            result.Value.Items[0].Name.Should().Be("Б");
            result.Value.Items[1].Name.Should().Be("А");
        }

        [Fact]
        public async Task GetAll_WithPagination_ReturnsCorrectPage()
        {
            int userId = 1;
            AccountQueryParameters parameters = new AccountQueryParameters { Page = 2, PageSize = 1 };
            IQueryable<Account> allAccounts = new List<Account>
            {
                new Account { Id = 1, Name = "А", UserId = userId },
                new Account { Id = 2, Name = "Б", UserId = userId }
            }.AsQueryable();
            _accountsRepositoryMock.SetupGetAllByUserId(userId, allAccounts);
            _accountsRepositoryMock.SetupGetAllPagedAsyncRealistic();
            AccountsService service = CreateService();

            Result<PagedResult<AccountResponse>> result = await service.GetAll(parameters, userId, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Items.Should().HaveCount(1);
            result.Value.TotalCount.Should().Be(2);
            result.Value.Page.Should().Be(2);
            result.Value.PageSize.Should().Be(1);
        }

        [Fact]
        public async Task GetAll_WhenNoAccounts_ReturnsEmptyList()
        {
            int userId = 1;
            AccountQueryParameters parameters = new AccountQueryParameters();
            IQueryable<Account> emptyQuery = new List<Account>().AsQueryable();

            _accountsRepositoryMock.SetupGetAllByUserId(userId, emptyQuery);
            _accountsRepositoryMock.SetupGetAllPagedAsyncRealistic();
            AccountsService service = CreateService();

            Result<PagedResult<AccountResponse>> result = await service.GetAll(parameters, userId, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Items.Should().BeEmpty();
            result.Value.TotalCount.Should().Be(0);
        }
    }
}
