using MoneyKeeper.Application.Common.Services;
using MoneyKeeper.Application.Contracts.Transition;
using MoneyKeeper.Core.Common;
using MoneyKeeper.Core.Common.Repositories;
using MoneyKeeper.Core.Models;
using Moq;

namespace MoneyKeeper.TestsCommons.MockHelpers
{
    public static class TransitionsServiceMockHelper
    {
        public static TransitionCreationCommand TransitionCreationCommand => new(1, 10, 20, 500, "Тестовый перевод");

        public static TransitionDeletionCommand TransitionDeletionCommand => new(1, 1);

        public static TransitionUpdateCommand TransitionUpdateCommand => new(1, 1, 10, 20, 500, "Обновление");

        public static void SetupGetTransitionById(this Mock<ITransitionsRepository> mock, int transitionId, Transition transition)
        {
            mock.Setup(r => r.GetByIdAsync(transitionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(transition);
        }

        public static void SetupGetAllByUserIdTransitions(this Mock<ITransitionsRepository> mock, int userId, IQueryable<Transition> transitions)
        {
            mock.Setup(r => r.GetAllByUserId(userId)).Returns(transitions);
        }

        public static void SetupGetAllPagedAsyncRealistic(this Mock<ITransitionsRepository> mock)
        {
            mock.Setup(r => r.GetAllPagedAsync(It.IsAny<IQueryable<Transition>>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((IQueryable<Transition> query, int page, int pageSize, CancellationToken ct) =>
                {
                    List<Transition> all = query.ToList();
                    int totalCount = all.Count;
                    List<Transition> paged = all.Skip((page - 1) * pageSize).Take(pageSize).ToList();
                    return (paged, totalCount);
                });
        }

        public static void SetupRecalculateAllTailsAsync(this Mock<ICommonBalanceOperationsRepository> mock)
        {
            mock.Setup(r => r.RecalculateAllTailsAsync(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
        }

        public static void SetupIsTailValidAfterChange(this Mock<ICommonBalanceOperationsRepository> mock, bool result)
        {
            mock.Setup(r => r.IsTailValidAfterChange(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(result);
        }
    }
}