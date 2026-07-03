using MoneyKeeper.Application.Contracts.Account;
using MoneyKeeper.Application.Contracts.BalanceChanging;
using MoneyKeeper.Core.Common;
using MoneyKeeper.Core.Common.Repositories;
using MoneyKeeper.Core.Models;
using Moq;

namespace MoneyKeeper.TestsCommons.MockHelpers
{
    public static class AccountsServiceMockHelper
    {
        public static AccountCreationCommand AccountCreationCommand => new(1, "Карта", 500, null, "Описание");

        public static AccountDeletionCommand AccountDeletionCommand => new(1, 1);

        public static AccountUpdateCommand AccountUpdateCommand => new(1, 1, "Карта", 400, 300, null);

        public static Result<BalanceChangingResponse> BalanceChangingCreationResult =>
            Result<BalanceChangingResponse>.Success(new BalanceChangingResponse(1, 1, DateTime.Now, 400, 600, "Карта"));

        public static void SetupExistsWithName(this Mock<IAccountsRepository> mock, string name, int userId, bool value)
        {
            mock.Setup(r => r.ExistsWithNameAsync(name, userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(value);
        }

        public static void SetupExistsAnotherWithName(this Mock<IAccountsRepository> mock, string name, int accountId, int userId, bool value)
        {
            mock.Setup(r => r.ExistsAnotherWithNameAsync(name, accountId, userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(value);
        }

        public static void SetupGetById(this Mock<IAccountsRepository> mock, int accountId, Account account)
        {
            mock.Setup(r => r.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(account);
        }

        public static void SetupUpdate(this Mock<IAccountsRepository> mock, Account account)
        {
            mock.Setup(r => r.UpdateAsync(account, It.IsAny<CancellationToken>()))
                .ReturnsAsync(account);
        }

        public static void SetupGetAllByUserId(this Mock<IAccountsRepository> mock, int userId, IQueryable<Account> returnValue)
        {
            mock.Setup(r => r.GetAllByUserId(userId)).Returns(returnValue);
        }

        public static void SetupGetAllPagedAsyncRealistic(this Mock<IAccountsRepository> mock)
        {
            mock.Setup(r => r.GetAllPagedAsync(It.IsAny<IQueryable<Account>>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((IQueryable<Account> query, int page, int pageSize, CancellationToken ct) =>
                {
                    List<Account> all = query.ToList();
                    int totalCount = all.Count;
                    List<Account> paged = all.Skip((page - 1) * pageSize).Take(pageSize).ToList();
                    return (paged, totalCount);
                });
        }

        public static void SetupGetAllByUserIdAccounts(this Mock<IAccountsRepository> mock, int userId, List<Account> accounts)
        {
            mock.Setup(r => r.GetAllByUserId(userId)).Returns(accounts.AsQueryable());
        }
    }
}
