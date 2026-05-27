using MoneyKeeper.Application.Common;
using MoneyKeeper.Application.Common.Services;
using MoneyKeeper.Application.Contracts.Transition;
using MoneyKeeper.Application.Filters;
using MoneyKeeper.Application.Sorting;
using MoneyKeeper.Core.Common;
using MoneyKeeper.Core.Common.Repositories;
using MoneyKeeper.Core.Models;

namespace MoneyKeeper.Application.Services
{
    public class TransitionsService : ITransitionsService
    {
        private readonly ITransitionsRepository _transitionsRepository;
        private readonly IAccountsRepository _accountsRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICommonBalanceOperationsRepository _commonBalanceOperationsRepository;
        private readonly IBalanceChangingsRepository _balanceChangingsRepository;
        private readonly IOperationsRepository _operationsRepository;

        public TransitionsService(ITransitionsRepository transitionsRepository, IAccountsRepository accountsRepository,
            IUnitOfWork unitOfWork, ICommonBalanceOperationsRepository commonBalanceOperationsRepository, IBalanceChangingsRepository balanceChangingsRepository,
            IOperationsRepository operationsRepository)
        {
            _transitionsRepository = transitionsRepository;
            _accountsRepository = accountsRepository;
            _unitOfWork = unitOfWork;
            _commonBalanceOperationsRepository = commonBalanceOperationsRepository;
            _balanceChangingsRepository = balanceChangingsRepository;
            _operationsRepository = operationsRepository;
        }

        public async Task<Result<TransitionResponse>> Add(TransitionCreationCommand command, CancellationToken cancellationToken)
        {
            Account sourceAccount = (await _accountsRepository.GetByIdAsync(command.SourceAccountId, cancellationToken))!;
            Account destinationAccount = (await _accountsRepository.GetByIdAsync(command.DestinationAccountId, cancellationToken))!;

            if (sourceAccount.Balance < command.Sum)
                return Result<TransitionResponse>.Failure
                    (Error.UnprocessableEntity($"На счете с id {sourceAccount.Id} недостаточно средств для снятия {command.Sum} рублей",
                        ErrorCodes.NOT_ENOUGH_MONEY));

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                Result<TransitionResponse> result = 
                    await AddTransition(sourceAccount, destinationAccount, command.Sum, command.Description, cancellationToken);

                if (!result.IsSuccess)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return result;
                }

                await _unitOfWork.CommitTransactionAsync();
                return result;
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                return Result<TransitionResponse>.Failure
                    (Error.InternalServerError("Неизвестная ошибка при добавлении перевода", ErrorCodes.UNKNOWN_TRANSITION_DELETING_ERROR));
            }
        }

        public async Task<Result<bool>> Delete(int transitionId, CancellationToken cancellationToken)
        {
            Transition transitionToDelete = (await _transitionsRepository.GetByIdAsync(transitionId, cancellationToken))!;
            Account destinationAccount = (await _accountsRepository.GetByIdAsync(transitionToDelete.DestinationAccountId, cancellationToken))!;


            if (await _operationsRepository.AreAnyConsumptionOperationsAfterAsync(destinationAccount.Id, transitionToDelete.Date, cancellationToken)
                || await _transitionsRepository.AreAnySourceTransitionsAfterAsync(destinationAccount.Id, transitionToDelete.Date, cancellationToken)
                || await _balanceChangingsRepository.AreAnyAfterAsync(destinationAccount.Id, transitionToDelete.Date, cancellationToken))
            {
                return Result<bool>.Failure
                    (Error.UnprocessableEntity($"Невозможно отменить перевод с id {transitionId}, так как после него на счете-получателе были потрачены деньги путем изменения баланса " +
                    $", создания операции по трате денег или переводом с этого счета", ErrorCodes.TRANSITION_CANCELING_DENIED));
            }

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                await _transitionsRepository.DeleteAsync(transitionId, cancellationToken);
                await _commonBalanceOperationsRepository.RecalculateAllTailsAsync(destinationAccount.Id, transitionToDelete.Date, transitionToDelete.OldDestinationAccountBalance, cancellationToken);
                await _commonBalanceOperationsRepository.RecalculateAllTailsAsync(transitionToDelete.SourceAccountId, transitionToDelete.Date, transitionToDelete.OldSourceAccountBalance, cancellationToken);
                await _unitOfWork.CommitTransactionAsync();

                return Result<bool>.Success(true);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                return Result<bool>.Failure
                    (Error.InternalServerError("Неизвестная ошибка при удалении перевода", ErrorCodes.UNKNOWN_TRANSITION_DELETING_ERROR));
            }
        }

        public async Task<Result<PagedResult<TransitionResponse>>> GetAll(TransitionQueryParameters parameters, int userId, CancellationToken cancellationToken)
        {
            List<int> userAccountIds = _accountsRepository
                .GetAllByUserId(userId)
                .Select(a => a.Id)
                .ToList();

            List<int> requestedSourceAccountIds = new List<int>();
            List<int> requestedDestinationAccountIds = new List<int>();
            if (parameters.SourceAccountIds.Any())
                requestedSourceAccountIds = userAccountIds.Intersect(parameters.SourceAccountIds).ToList();
            else
                requestedSourceAccountIds = userAccountIds;

            if (parameters.DestinationAccountIds.Any())
                requestedDestinationAccountIds = userAccountIds.Intersect(parameters.DestinationAccountIds).ToList();
            else
                requestedDestinationAccountIds = userAccountIds;

            if (!requestedSourceAccountIds.Any() || !requestedDestinationAccountIds.Any())
                return Result<PagedResult<TransitionResponse>>.Success
                    (new PagedResult<TransitionResponse>(new List<TransitionResponse>(), 0, parameters.Page, parameters.PageSize));

            TransitionsFilter filter = new TransitionsFilter()
            {
                SourceAccountIds = requestedSourceAccountIds,
                DestinationAccountIds = requestedDestinationAccountIds,
                MinSum = parameters.MinSum,
                MaxSum = parameters.MaxSum,
                FromDate = parameters.FromDate,
                ToDate = parameters.ToDate,
            };

            IQueryable<Transition> query = _transitionsRepository.GetAllByUserId(userId);
            query = filter.ApplyTo(query);
            query = query.ApplySorting(parameters.SortBy);

            var (items, totalCount) = await _transitionsRepository
                .GetAllPagedAsync(query, parameters.Page, parameters.PageSize, cancellationToken);

            List<TransitionResponse> responseItems = items
                .Select(t => new TransitionResponse(t.Id, t.SourceAccountId, t.DestinationAccountId, t.SourceAccount.Name, t.DestinationAccount.Name,
                    t.Sum, t.Description, t.OldSourceAccountBalance, t.NewSourceAccountBalance, t.OldDestinationAccountBalance, t.NewDestinationAccountBalance, t.Date))
                .ToList();

            return Result<PagedResult<TransitionResponse>>.Success
                (new PagedResult<TransitionResponse>(responseItems, totalCount, parameters.Page, parameters.PageSize));
        }

        public Task<Result<TransitionResponse>> Update(TransitionUpdateCommand command, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        private async Task<Result<TransitionResponse>> AddTransition(Account sourceAccount, Account destinationAccount, decimal sum, string? description, CancellationToken cancellationToken)
        {
            decimal oldSourceAccountBalance = sourceAccount.Balance;
            decimal oldDestinationAccountBalance = destinationAccount.Balance;

            Transition transition = new Transition()
            {
                SourceAccountId = sourceAccount.Id,
                DestinationAccountId = destinationAccount.Id,
                OldSourceAccountBalance = oldSourceAccountBalance,
                OldDestinationAccountBalance = oldDestinationAccountBalance,
                Sum = sum,
                Description = description,
                NewDestinationAccountBalance = oldDestinationAccountBalance + sum,
                NewSourceAccountBalance = oldSourceAccountBalance + sum,
            };

            await _accountsRepository.TryWithdrawAsync(sourceAccount.Id, sum, cancellationToken);
            await _accountsRepository.TryDepositAsync(destinationAccount.Id, sum, cancellationToken);
            await _transitionsRepository.AddAsync(transition, cancellationToken);

            return Result<TransitionResponse>.Success
                (new TransitionResponse(transition.Id, transition.SourceAccountId, transition.DestinationAccountId, sourceAccount.Name,
                    destinationAccount.Name, sum, description, oldSourceAccountBalance, transition.NewSourceAccountBalance, oldDestinationAccountBalance,
                    transition.NewDestinationAccountBalance, transition.Date));
        }
    }
}
