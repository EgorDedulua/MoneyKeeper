using MoneyKeeper.Application.Contracts.BalanceChanging;
using MoneyKeeper.Core.Common;
using MoneyKeeper.Core.Common.Repositories;
using MoneyKeeper.Core.Models;
using Moq;

namespace MoneyKeeper.TestsCommons.MockHelpers
{
    public static class BalanceChangingsServiceMockHelper
    {
        public static BalanceChangingCreationCommand CreationCommand => new(1, 10, 500);

        public static BalanceChangingDeletionCommand DeletionCommand => new(1, 1); 

        public static BalanceChangingUpdateCommand UpdateCommand => new(1, 1, 10, 600); 

        public static Result<BalanceChangingResponse> SuccessResult =>
            Result<BalanceChangingResponse>.Success(new BalanceChangingResponse(1, 10, DateTime.Now, 200, 500, "Test"));

        public static void SetupGetBalanceChangingById(this Mock<IBalanceChangingsRepository> mock, int id, BalanceChanging balanceChanging)
        {
            mock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(balanceChanging);
        }

        public static void SetupAddBalanceChanging(this Mock<IBalanceChangingsRepository> mock)
        {
            mock.Setup(r => r.AddAsync(It.IsAny<BalanceChanging>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
        }

        public static void SetupDeleteBalanceChanging(this Mock<IBalanceChangingsRepository> mock)
        {
            mock.Setup(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
        }

        public static void SetupUpdateBalanceChanging(this Mock<IBalanceChangingsRepository> mock, BalanceChanging updated)
        {
            mock.Setup(r => r.UpdateAsync(It.IsAny<BalanceChanging>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(updated);
        }

        public static void SetupGetAllByUserIdBalanceChangings(this Mock<IBalanceChangingsRepository> mock, int userId, IQueryable<BalanceChanging> changings)
        {
            mock.Setup(r => r.GetAllByUserId(userId)).Returns(changings);
        }

        public static void SetupGetAllPagedAsyncRealistic(this Mock<IBalanceChangingsRepository> mock)
        {
            mock.Setup(r => r.GetAllPagedAsync(It.IsAny<IQueryable<BalanceChanging>>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((IQueryable<BalanceChanging> query, int page, int pageSize, CancellationToken ct) =>
                {
                    List<BalanceChanging> all = query.ToList();
                    int totalCount = all.Count;
                    List<BalanceChanging> paged = all.Skip((page - 1) * pageSize).Take(pageSize).ToList();
                    return (paged, totalCount);
                });
        }

        public static void SetupUpdateBalanceAsync(this Mock<IAccountsRepository> mock, int accountId, decimal newBalance)
        {
            mock.Setup(r => r.UpdateBalanceAsync(accountId, newBalance, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
        }
    }
}