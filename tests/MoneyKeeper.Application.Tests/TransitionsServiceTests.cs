using FluentAssertions;
using FluentValidation;
using MoneyKeeper.Application.Common;
using MoneyKeeper.Application.Common.Services;
using MoneyKeeper.Application.Common.Validation;
using MoneyKeeper.Application.Contracts.Transition;
using MoneyKeeper.Application.Services;
using MoneyKeeper.Core.Common;
using MoneyKeeper.Core.Common.Repositories;
using MoneyKeeper.Core.Models;
using MoneyKeeper.TestsCommons;
using MoneyKeeper.TestsCommons.MockHelpers;
using Moq;

namespace MoneyKeeper.Application.Tests
{
    public class TransitionsServiceTests
    {
        private readonly Mock<ITransitionsRepository> _transitionsRepoMock = new();
        private readonly Mock<IAccountsRepository> _accountsRepoMock = new();
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly Mock<ICommonBalanceOperationsRepository> _commonBalanceRepoMock = new();
        private readonly Mock<IBalanceChangingsRepository> _balanceChangingsRepoMock = new();
        private readonly Mock<IOperationsRepository> _operationsRepoMock = new();
        private readonly Mock<IValidator<ITransitionAccountsValidationModel>> _transitionAccountsValidatorMock = new();
        private readonly Mock<IValidator<ITransitionOwnershipValidationModel>> _transitionOwnershipValidatorMock = new();

        private TransitionsService CreateService()
        {
            return new TransitionsService(
                _transitionsRepoMock.Object,
                _accountsRepoMock.Object,
                _unitOfWorkMock.Object,
                _commonBalanceRepoMock.Object,
                _balanceChangingsRepoMock.Object,
                _operationsRepoMock.Object,
                _transitionAccountsValidatorMock.Object,
                _transitionOwnershipValidatorMock.Object
            );
        }

        [Fact]
        public async Task GetAll_ValidRequest_ReturnsPagedTransitions()
        {
            int userId = 1;
            TransitionQueryParameters parameters = new TransitionQueryParameters();
            List<Account> userAccounts = new List<Account> { new Account { Id = 10 }, new Account { Id = 20 } };
            IQueryable<Transition> transitions = new List<Transition>
            {
                new Transition
                {
                    Id = 1, SourceAccountId = 10, DestinationAccountId = 20,
                    SourceAccount = new Account { Name = "A" }, DestinationAccount = new Account { Name = "B" }
                }
            }.AsQueryable();

            _accountsRepoMock.SetupGetAllByUserIdAccounts(userId, userAccounts);
            _transitionsRepoMock.SetupGetAllByUserIdTransitions(userId, transitions);
            _transitionsRepoMock.SetupGetAllPagedAsyncRealistic();

            TransitionsService service = CreateService();
            Result<PagedResult<TransitionResponse>> result = await service.GetAll(parameters, userId, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Items.Should().HaveCount(1);
            result.Value.TotalCount.Should().Be(1);
        }

        [Fact]
        public async Task GetAll_NoSourceAccounts_ReturnsEmpty()
        {
            int userId = 1;
            TransitionQueryParameters parameters = new TransitionQueryParameters();
            List<Account> userAccounts = new List<Account>(); 

            _accountsRepoMock.SetupGetAllByUserIdAccounts(userId, userAccounts);

            TransitionsService service = CreateService();
            Result<PagedResult<TransitionResponse>> result = await service.GetAll(parameters, userId, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Items.Should().BeEmpty();
            result.Value.TotalCount.Should().Be(0);
        }

        [Fact]
        public async Task Add_ValidationFails_ReturnsError()
        {
            TransitionCreationCommand command = TransitionsServiceMockHelper.TransitionCreationCommand;
            _transitionAccountsValidatorMock
                .Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidationTestData.AccountNotFound);

            TransitionsService service = CreateService();
            Result<TransitionResponse> result = await service.Add(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.Error.ErrorCode.Should().Be(ErrorCodes.ACCOUNT_NOT_FOUND);
            _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(), Times.Never);
        }

        [Fact]
        public async Task Add_InsufficientBalance_ReturnsError()
        {
            TransitionCreationCommand command = TransitionsServiceMockHelper.TransitionCreationCommand;
            Account sourceAccount = new Account { Id = 10, Balance = 100 };
            Account destinationAccount = new Account { Id = 20, Balance = 0 };

            _transitionAccountsValidatorMock
                .Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidationTestData.ValidResult);
            _accountsRepoMock.SetupGetAccountById(10, sourceAccount);
            _accountsRepoMock.SetupGetAccountById(20, destinationAccount);

            TransitionsService service = CreateService();
            Result<TransitionResponse> result = await service.Add(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.Error.ErrorCode.Should().Be(ErrorCodes.NOT_ENOUGH_MONEY);
            _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(), Times.Never);
        }

        [Fact]
        public async Task Add_Success()
        {
            TransitionCreationCommand command = TransitionsServiceMockHelper.TransitionCreationCommand;
            Account sourceAccount = new Account { Id = 10, Balance = 1000 };
            Account destinationAccount = new Account { Id = 20, Balance = 500 };

            _transitionAccountsValidatorMock
                .Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidationTestData.ValidResult);
            _accountsRepoMock.SetupGetAccountById(10, sourceAccount);
            _accountsRepoMock.SetupGetAccountById(20, destinationAccount);
            _accountsRepoMock.SetupTryWithdraw(10, 500, 500m);
            _accountsRepoMock.SetupTryDeposit(20, 500, 1000m);
            _unitOfWorkMock.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);

            TransitionsService service = CreateService();
            Result<TransitionResponse> result = await service.Add(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            _transitionsRepoMock.Verify(r => r.AddAsync(It.IsAny<Transition>(), It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task Add_WithdrawFails_ReturnsError()
        {
            TransitionCreationCommand command = TransitionsServiceMockHelper.TransitionCreationCommand;
            Account sourceAccount = new Account { Id = 10, Balance = 1000 };
            Account destinationAccount = new Account { Id = 20, Balance = 500 };

            _transitionAccountsValidatorMock
                .Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidationTestData.ValidResult);
            _accountsRepoMock.SetupGetAccountById(10, sourceAccount);
            _accountsRepoMock.SetupGetAccountById(20, destinationAccount);
            _accountsRepoMock.SetupTryWithdraw(10, 500, null);
            _unitOfWorkMock.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.RollbackTransactionAsync()).Returns(Task.CompletedTask);

            TransitionsService service = CreateService();
            Result<TransitionResponse> result = await service.Add(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.Error.ErrorCode.Should().Be(ErrorCodes.NOT_ENOUGH_MONEY);
            _unitOfWorkMock.Verify(u => u.RollbackTransactionAsync(), Times.Once);
            _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(), Times.Never);
        }

        [Fact]
        public async Task Add_ExceptionThrown_ReturnsInternalError()
        {
            TransitionCreationCommand command = TransitionsServiceMockHelper.TransitionCreationCommand;
            Account sourceAccount = new Account { Id = 10, Balance = 1000 };
            Account destinationAccount = new Account { Id = 20, Balance = 500 };

            _transitionAccountsValidatorMock
                .Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidationTestData.ValidResult);
            _accountsRepoMock.SetupGetAccountById(10, sourceAccount);
            _accountsRepoMock.SetupGetAccountById(20, destinationAccount);
            _accountsRepoMock.SetupTryWithdraw(10, 500, 500m);
            _accountsRepoMock.SetupTryDeposit(20, 500, 1000m);
            _unitOfWorkMock.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.CommitTransactionAsync()).ThrowsAsync(new InvalidOperationException());
            _unitOfWorkMock.Setup(u => u.RollbackTransactionAsync()).Returns(Task.CompletedTask);

            TransitionsService service = CreateService();
            Result<TransitionResponse> result = await service.Add(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.Error.ErrorCode.Should().Be(ErrorCodes.UNKNOWN_TRANSITION_DELETING_ERROR);
            _unitOfWorkMock.Verify(u => u.RollbackTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task Delete_ValidationFails_ReturnsError()
        {
            TransitionDeletionCommand command = TransitionsServiceMockHelper.TransitionDeletionCommand;
            _transitionOwnershipValidatorMock
                .Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidationTestData.UserAccessDenied);

            TransitionsService service = CreateService();
            Result<bool> result = await service.Delete(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.Error.ErrorCode.Should().Be(ErrorCodes.USER_ACCESS_DENIED);
            _transitionsRepoMock.Verify(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Delete_SubsequentOperationsExist_ReturnsError()
        {
            TransitionDeletionCommand command = TransitionsServiceMockHelper.TransitionDeletionCommand;
            Transition transition = new Transition
            {
                Id = 1,
                SourceAccountId = 10,
                DestinationAccountId = 20,
                Date = DateTime.Now,
                OldSourceAccountBalance = 200,
                OldDestinationAccountBalance = 100
            };
            Account destinationAccount = new Account { Id = 20 };

            _transitionOwnershipValidatorMock
                .Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidationTestData.ValidResult);
            _transitionsRepoMock.SetupGetTransitionById(1, transition);
            _accountsRepoMock.SetupGetAccountById(20, destinationAccount);
            _operationsRepoMock.SetupAreAnyConsumptionOperationsAfterAsync(20, transition.Date, true);

            TransitionsService service = CreateService();
            Result<bool> result = await service.Delete(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.Error.ErrorCode.Should().Be(ErrorCodes.MONEY_CANNOT_BE_RESTORED);
            _transitionsRepoMock.Verify(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Delete_Success()
        {
            TransitionDeletionCommand command = TransitionsServiceMockHelper.TransitionDeletionCommand;
            Transition transition = new Transition
            {
                Id = 1,
                SourceAccountId = 10,
                DestinationAccountId = 20,
                Date = DateTime.Now,
                OldSourceAccountBalance = 200,
                OldDestinationAccountBalance = 100
            };
            Account destinationAccount = new Account { Id = 20 };

            _transitionOwnershipValidatorMock
                .Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidationTestData.ValidResult);
            _transitionsRepoMock.SetupGetTransitionById(1, transition);
            _accountsRepoMock.SetupGetAccountById(20, destinationAccount);
            _operationsRepoMock.SetupAreAnyConsumptionOperationsAfterAsync(20, transition.Date, false);
            _transitionsRepoMock.SetupAreAnySourceTransitionsAfterAsync(20, transition.Date, false);
            _balanceChangingsRepoMock.SetupAreAnyBalanceChangingsAfterAsync(20, transition.Date, false);
            _unitOfWorkMock.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);
            _commonBalanceRepoMock.SetupRecalculateAllTailsAsync();

            TransitionsService service = CreateService();
            Result<bool> result = await service.Delete(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            _transitionsRepoMock.Verify(r => r.DeleteAsync(1, It.IsAny<CancellationToken>()), Times.Once);
            _commonBalanceRepoMock.Verify(r => r.RecalculateAllTailsAsync(20, transition.Date, 100, It.IsAny<CancellationToken>()), Times.Once);
            _commonBalanceRepoMock.Verify(r => r.RecalculateAllTailsAsync(10, transition.Date, 200, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Delete_ExceptionInTransaction_ReturnsInternalError()
        {
            TransitionDeletionCommand command = TransitionsServiceMockHelper.TransitionDeletionCommand;
            Transition transition = new Transition
            {
                Id = 1,
                SourceAccountId = 10,
                DestinationAccountId = 20,
                Date = DateTime.Now,
                OldSourceAccountBalance = 200,
                OldDestinationAccountBalance = 100
            };
            Account destinationAccount = new Account { Id = 20 };

            _transitionOwnershipValidatorMock
                .Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidationTestData.ValidResult);
            _transitionsRepoMock.SetupGetTransitionById(1, transition);
            _accountsRepoMock.SetupGetAccountById(20, destinationAccount);
            _operationsRepoMock.SetupAreAnyConsumptionOperationsAfterAsync(20, transition.Date, false);
            _transitionsRepoMock.SetupAreAnySourceTransitionsAfterAsync(20, transition.Date, false);
            _balanceChangingsRepoMock.SetupAreAnyBalanceChangingsAfterAsync(20, transition.Date, false);
            _unitOfWorkMock.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.CommitTransactionAsync()).ThrowsAsync(new InvalidOperationException());
            _unitOfWorkMock.Setup(u => u.RollbackTransactionAsync()).Returns(Task.CompletedTask);

            TransitionsService service = CreateService();
            Result<bool> result = await service.Delete(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.Error.ErrorCode.Should().Be(ErrorCodes.UNKNOWN_TRANSITION_DELETING_ERROR);
        }

        [Fact]
        public async Task Update_ValidationFails_ReturnsError()
        {
            TransitionUpdateCommand command = TransitionsServiceMockHelper.TransitionUpdateCommand;
            _transitionOwnershipValidatorMock
                .Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidationTestData.ValidResult);
            _transitionAccountsValidatorMock
                .Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidationTestData.TransitionNotFound);

            TransitionsService service = CreateService();
            Result<TransitionResponse> result = await service.Update(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.Error.ErrorCode.Should().Be(ErrorCodes.OPERATION_NOT_FOUND);
        }

        [Fact]
        public async Task Update_DestChanged_SubsequentOperationsExist_ReturnsError()
        {
            TransitionUpdateCommand command = new TransitionUpdateCommand(1, 1, 10, 30, 500, "Меняем получателя");
            Transition transition = new Transition
            {
                Id = 1,
                SourceAccountId = 10,     
                DestinationAccountId = 20, 
                Date = DateTime.Now
            };
            Account sourceAccount = new Account { Id = 10, Balance = 1000 }; 
            Account destinationAccount = new Account { Id = 30, Balance = 500 };

            _transitionOwnershipValidatorMock
                .Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidationTestData.ValidResult);
            _transitionAccountsValidatorMock
                .Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidationTestData.ValidResult);
            _transitionsRepoMock.SetupGetTransitionById(1, transition);
            _accountsRepoMock.SetupGetAccountById(10, sourceAccount);
            _accountsRepoMock.SetupGetAccountById(30, destinationAccount);
            _operationsRepoMock.SetupAreAnyConsumptionOperationsAfterAsync(20, transition.Date, true);

            _commonBalanceRepoMock.SetupRecalculateAllTailsAsync();
            _commonBalanceRepoMock.SetupIsTailValidAfterChange(true);

            TransitionsService service = CreateService();
            Result<TransitionResponse> result = await service.Update(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error!.ErrorCode.Should().Be(ErrorCodes.MONEY_CANNOT_BE_RESTORED);
        }

        [Fact]
        public async Task Update_SourceChanged_InsufficientBalance_ReturnsError()
        {
            TransitionUpdateCommand command = TransitionsServiceMockHelper.TransitionUpdateCommand;
            Transition transition = new Transition
            {
                Id = 1,
                SourceAccountId = 30,
                DestinationAccountId = 20,
                Date = DateTime.Now
            };
            Account sourceAccount = new Account { Id = 10, Balance = 200 };
            Account destinationAccount = new Account { Id = 20, Balance = 500 };

            _transitionOwnershipValidatorMock
                .Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidationTestData.ValidResult);
            _transitionAccountsValidatorMock
                .Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidationTestData.ValidResult);
            _transitionsRepoMock.SetupGetTransitionById(1, transition);
            _accountsRepoMock.SetupGetAccountById(10, sourceAccount);
            _accountsRepoMock.SetupGetAccountById(20, destinationAccount);

            TransitionsService service = CreateService();
            Result<TransitionResponse> result = await service.Update(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.Error.ErrorCode.Should().Be(ErrorCodes.NOT_ENOUGH_MONEY);
        }

        [Fact]
        public async Task Update_SameAccounts_ChangeSum_Success()
        {
            TransitionUpdateCommand command = new TransitionUpdateCommand(1, 1, 10, 20, 300, "Уменьшили");
            Transition transition = new Transition
            {
                Id = 1,
                SourceAccountId = 10,
                DestinationAccountId = 20,
                Sum = 500,
                OldSourceAccountBalance = 800,
                OldDestinationAccountBalance = 200,
                NewSourceAccountBalance = 300,
                NewDestinationAccountBalance = 700,
                Date = DateTime.Now
            };
            Account sourceAccount = new Account { Id = 10, Name = "Src" };
            Account destinationAccount = new Account { Id = 20, Name = "Dst" };

            _transitionOwnershipValidatorMock
                .Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidationTestData.ValidResult);
            _transitionAccountsValidatorMock
                .Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidationTestData.ValidResult);
            _transitionsRepoMock.SetupGetTransitionById(1, transition);
            _accountsRepoMock.SetupGetAccountById(10, sourceAccount);
            _accountsRepoMock.SetupGetAccountById(20, destinationAccount);
            _commonBalanceRepoMock.SetupIsTailValidAfterChange(true);
            _transitionsRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Transition>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(transition);
            _unitOfWorkMock.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);
            _commonBalanceRepoMock.SetupRecalculateAllTailsAsync();

            TransitionsService service = CreateService();
            Result<TransitionResponse> result = await service.Update(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            _transitionsRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Transition>(), It.IsAny<CancellationToken>()), Times.Once);
            _commonBalanceRepoMock.Verify(r => r.RecalculateAllTailsAsync(10, transition.Date, 500, It.IsAny<CancellationToken>()), Times.Once);
            _commonBalanceRepoMock.Verify(r => r.RecalculateAllTailsAsync(20, transition.Date, 500, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Update_SameAccounts_TailInvalid_ReturnsError()
        {
            TransitionUpdateCommand command = new TransitionUpdateCommand(1, 1, 10, 20, 1000, "Слишком много");
            Transition transition = new Transition
            {
                Id = 1,
                SourceAccountId = 10,
                DestinationAccountId = 20,
                Sum = 500,
                OldSourceAccountBalance = 2000, 
                OldDestinationAccountBalance = 200,
                Date = DateTime.Now
            };
            Account sourceAccount = new Account { Id = 10 };
            Account destinationAccount = new Account { Id = 20 };

            _transitionOwnershipValidatorMock
                .Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidationTestData.ValidResult);
            _transitionAccountsValidatorMock
                .Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidationTestData.ValidResult);
            _transitionsRepoMock.SetupGetTransitionById(1, transition);
            _accountsRepoMock.SetupGetAccountById(10, sourceAccount);
            _accountsRepoMock.SetupGetAccountById(20, destinationAccount);

            _commonBalanceRepoMock.SetupIsTailValidAfterChange(false);
            _unitOfWorkMock.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.RollbackTransactionAsync()).Returns(Task.CompletedTask);

            TransitionsService service = CreateService();
            Result<TransitionResponse> result = await service.Update(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error!.ErrorCode.Should().Be(ErrorCodes.NOT_ENOUGH_MONEY);
            _unitOfWorkMock.Verify(u => u.RollbackTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task Update_ExceptionInTransaction_ReturnsInternalError()
        {
            TransitionUpdateCommand command = new TransitionUpdateCommand(1, 1, 10, 20, 300, "Update");
            Transition transition = new Transition
            {
                Id = 1,
                SourceAccountId = 10,
                DestinationAccountId = 20,
                Sum = 500,
                OldSourceAccountBalance = 800,
                OldDestinationAccountBalance = 200,
                Date = DateTime.Now
            };
            Account sourceAccount = new Account { Id = 10 };
            Account destinationAccount = new Account { Id = 20 };

            _transitionOwnershipValidatorMock
                .Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidationTestData.ValidResult);
            _transitionAccountsValidatorMock
                .Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidationTestData.ValidResult);
            _transitionsRepoMock.SetupGetTransitionById(1, transition);
            _accountsRepoMock.SetupGetAccountById(10, sourceAccount);
            _accountsRepoMock.SetupGetAccountById(20, destinationAccount);
            _commonBalanceRepoMock.SetupIsTailValidAfterChange(true);
            _unitOfWorkMock.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.CommitTransactionAsync()).ThrowsAsync(new InvalidOperationException());
            _unitOfWorkMock.Setup(u => u.RollbackTransactionAsync()).Returns(Task.CompletedTask);

            TransitionsService service = CreateService();
            Result<TransitionResponse> result = await service.Update(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.Error.ErrorCode.Should().Be(ErrorCodes.UNKNOWN_TRANSITION_UPDATING_ERROR);
        }
    }
}