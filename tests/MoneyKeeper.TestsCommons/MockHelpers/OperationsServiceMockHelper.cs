using MoneyKeeper.Application.Contracts.Operation;
using MoneyKeeper.Core.Common.Repositories;
using MoneyKeeper.Core.Models;
using Moq;

namespace MoneyKeeper.TestsCommons.MockHelpers
{
    public static class OperationsServiceMockHelper
    {
        public static OperationCreationCommand OperationCreationCommand => new(1, 10, 5, 100, "Тест");

        public static OperationDeletionCommand OperationDeletionCommand => new(1, 1);

        public static OperationUpdateCommand OperationUpdateCommand => new(1, 1, 10, 5, 150, "Обновление");

        public static void SetupGetAccountById(this Mock<IAccountsRepository> mock, int accountId, Account account)
        {
            mock.Setup(r => r.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(account);
        }

        public static void SetupGetCategoryById(this Mock<ICategoriesRepository> mock, int categoryId, Category category)
        {
            mock.Setup(r => r.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(category);
        }

        public static void SetupTryWithdraw(this Mock<IAccountsRepository> mock, int accountId, decimal amount, decimal? result)
        {
            mock.Setup(r => r.TryWithdrawAsync(accountId, amount, It.IsAny<CancellationToken>()))
                .ReturnsAsync(result);
        }

        public static void SetupTryDeposit(this Mock<IAccountsRepository> mock, int accountId, decimal amount, decimal? result)
        {
            mock.Setup(r => r.TryDepositAsync(accountId, amount, It.IsAny<CancellationToken>()))
                .ReturnsAsync(result);
        }

        public static void SetupGetOperationById(this Mock<IOperationsRepository> mock, int operationId, Operation operation)
        {
            mock.Setup(r => r.GetByIdAsync(operationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(operation);
        }

        public static void SetupAreAnyConsumptionOperationsAfterAsync(this Mock<IOperationsRepository> mock, int accountId, DateTime date, bool result)
        {
            mock.Setup(r => r.AreAnyConsumptionOperationsAfterAsync(accountId, date, It.IsAny<CancellationToken>()))
                .ReturnsAsync(result);
        }

        public static void SetupAreAnySourceTransitionsAfterAsync(this Mock<ITransitionsRepository> mock, int accountId, DateTime date, bool result)
        {
            mock.Setup(r => r.AreAnySourceTransitionsAfterAsync(accountId, date, It.IsAny<CancellationToken>()))
                .ReturnsAsync(result);
        }

        public static void SetupAreAnyBalanceChangingsAfterAsync(this Mock<IBalanceChangingsRepository> mock, int accountId, DateTime date, bool result)
        {
            mock.Setup(r => r.AreAnyAfterAsync(accountId, date, It.IsAny<CancellationToken>()))
                .ReturnsAsync(result);
        }

        public static void SetupGetAllByUserIdForAccounts(this Mock<IAccountsRepository> mock, int userId, List<Account> accounts)
        {
            mock.Setup(r => r.GetAllByUserId(userId))
                .Returns(accounts.AsQueryable());
        }

        public static void SetupGetAllByUserIdForCategories(this Mock<ICategoriesRepository> mock, int userId, List<Category> categories)
        {
            mock.Setup(r => r.GetAllByUserId(userId))
                .Returns(categories.AsQueryable());
        }

        public static void SetupGetAllByUserIdForOperations(this Mock<IOperationsRepository> mock, int userId, IQueryable<Operation> operations)
        {
            mock.Setup(r => r.GetAllByUserId(userId))
                .Returns(operations);
        }

        public static void SetupGetAllPagedAsyncRealistic(this Mock<IOperationsRepository> mock)
        {
            mock.Setup(r => r.GetAllPagedAsync(It.IsAny<IQueryable<Operation>>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((IQueryable<Operation> query, int page, int pageSize, CancellationToken ct) =>
                {
                    List<Operation> all = query.ToList();
                    int totalCount = all.Count;
                    List<Operation> paged = all.Skip((page - 1) * pageSize).Take(pageSize).ToList();
                    return (paged, totalCount);
                });
        }
    }
}