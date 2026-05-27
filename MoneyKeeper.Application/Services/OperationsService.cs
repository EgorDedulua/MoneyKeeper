using MoneyKeeper.Application.Common;
using MoneyKeeper.Application.Common.Services;
using MoneyKeeper.Application.Contracts.Operation;
using MoneyKeeper.Application.Filters;
using MoneyKeeper.Application.Sorting;
using MoneyKeeper.Core.Common;
using MoneyKeeper.Core.Common.Repositories;
using MoneyKeeper.Core.Enums;
using MoneyKeeper.Core.Models;

namespace MoneyKeeper.Application.Services
{
    public class OperationsService : IOperationsService
    {
        private readonly IOperationsRepository _operationsRepository;
        private readonly IAccountsRepository _accountsRepository;
        private readonly ICategoriesRepository _categoriesRepository;
        private readonly ITransitionsRepository _transitionsRepository;
        private readonly IBalanceChangingsRepository _balanceChangingsRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICommonBalanceOperationsRepository _commonBalanceOperationsRepository;

        public OperationsService(IOperationsRepository operationsRepository, 
            IAccountsRepository accountsRepository, ICategoriesRepository categoriesRepository,
            IUnitOfWork unitOfWork, ITransitionsRepository transitionsRepository, IBalanceChangingsRepository balanceChangingsRepository,
            ICommonBalanceOperationsRepository commonBalanceOperationsRepository)
        {
            _operationsRepository = operationsRepository; 
            _accountsRepository = accountsRepository;
            _categoriesRepository = categoriesRepository;
            _unitOfWork = unitOfWork;
            _transitionsRepository = transitionsRepository;
            _balanceChangingsRepository = balanceChangingsRepository;
            _commonBalanceOperationsRepository = commonBalanceOperationsRepository;
        }

        public async Task<Result<PagedResult<OperationResponse>>> GetAll(OperationQueryParameters parameters, 
            int userId, CancellationToken cancellationToken)
        {
            List<int> userAccountsIds = _accountsRepository
                .GetAllByUserId(userId)
                .Select(a => a.Id)
                .ToList();

            List<int> userCategoryIds = _categoriesRepository
                .GetAllByUserId(userId)
                .Select(c => c.Id)
                .ToList();

            List<int> requestedAccountIds = new List<int>();
            if (parameters.AccountIds.Any())
                requestedAccountIds = userAccountsIds.Intersect(parameters.AccountIds).ToList();
            else
                requestedAccountIds = userAccountsIds;

            List<int> requestedCategoryIds = new List<int>();
            if (parameters.CategoryIds.Any())
                requestedCategoryIds = userCategoryIds.Intersect(parameters.CategoryIds).ToList();
            else
                requestedCategoryIds = userCategoryIds;

            if (!requestedAccountIds.Any() || !requestedCategoryIds.Any())
                return Result<PagedResult<OperationResponse>>.Success
                    (new PagedResult<OperationResponse>(new List<OperationResponse>(), 0, parameters.Page, parameters.PageSize));

            OperationFilter filter = new OperationFilter()
            {
                AccountIds = requestedAccountIds,
                CategoryIds = requestedCategoryIds,
                MinSum = parameters.MinSum,
                MaxSum = parameters.MaxSum,
                FromDate = parameters.FromDate,
                ToDate = parameters.ToDate,
            };

            IQueryable<Operation> query = _operationsRepository.GetAllByUserId(userId);
            query = filter.ApplyTo(query);
            query = query.ApplySorting(parameters.SortBy);

            var (items, totalCount) = await _operationsRepository
                .GetAllPagedAsync(query, parameters.Page, parameters.PageSize, cancellationToken);

            List<OperationResponse> responseItems = items
                .Select(o => new OperationResponse(o.Id, o.AccountId, o.CategoryId, o.Sum, o.Description, o.Date, o.OldAccountBalance, o.NewAccountBalance, 
                    o.Account.Name, o.Category.Name, o.Category.Type))
                .ToList();

            return Result<PagedResult<OperationResponse>>.Success
                (new PagedResult<OperationResponse>(responseItems, totalCount, parameters.Page, parameters.PageSize));
        }

        public async Task<Result<OperationResponse>> Add(OperationCreationCommand command, CancellationToken cancellationToken)
        {
            Account account = (await _accountsRepository.GetByIdAsync(command.AccountId, cancellationToken))!;
            Category category = (await _categoriesRepository.GetByIdAsync(command.CategoryId, cancellationToken))!;

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                Result<OperationResponse> result = await AddOperation(category, account, command.Sum, command.Description, cancellationToken);
                if (!result.IsSuccess)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return result;
                }

                await _unitOfWork.CommitTransactionAsync();
                return result;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                Console.WriteLine(ex.Message);
                return Result<OperationResponse>.Failure
                    (Error.InternalServerError("Неизветсная ошибка при добавлении операции", ErrorCodes.UNKNOWN_OPERATION_CREATION_ERROR));
            }
        }

        public async Task<Result<bool>> Delete(int operationId, CancellationToken cancellationToken)
        {
            Operation operationToDelete = (await _operationsRepository.GetByIdAsync(operationId, cancellationToken))!;
            Account account = operationToDelete.Account;
            Category category = operationToDelete.Category;

            if (await _operationsRepository.AreAnyConsumptionOperationsAfterAsync(account.Id, operationToDelete.Date, cancellationToken)
                || await _transitionsRepository.AreAnySourceTransitionsAfterAsync(account.Id, operationToDelete.Date, cancellationToken)
                || await _balanceChangingsRepository.AreAnyAfterAsync(account.Id, operationToDelete.Date, cancellationToken))
            {
                return Result<bool>.Failure
                    (Error.UnprocessableEntity($"Невозможно отменить операцию с id {operationToDelete.Id}, так как после нее были потрачены деньги путем изменения баланса " +
                    $", создания операции по трате денег или переводом с этого счета", ErrorCodes.OPERATION_CANCELING_DENIED));
            }

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                await _operationsRepository.DeleteAsync(operationToDelete.Id, cancellationToken);
                await _commonBalanceOperationsRepository.RecalculateAllTailsAsync
                    (account.Id, operationToDelete.Date, operationToDelete.OldAccountBalance, cancellationToken);
                await _unitOfWork.CommitTransactionAsync();

                return Result<bool>.Success(true);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                return Result<bool>.Failure
                    (Error.InternalServerError("Неизвестная ошибка при удалении операции", ErrorCodes.UNKNOWN_OPERATION_DELETING_ERROR));
            }
        }

        public async Task<Result<OperationResponse>> Update(OperationUpdateCommand command, CancellationToken cancellationToken = default)
        {
            Operation operationToUpdate = (await _operationsRepository.GetByIdAsync(command.OperationId, cancellationToken))!;
            Account account = (await _accountsRepository.GetByIdAsync(command.AccountId))!;
            Category category = (await _categoriesRepository.GetByIdAsync(command.CategoryId))!;

            if (await _balanceChangingsRepository.AreAnyAfterAsync(operationToUpdate.AccountId, operationToUpdate.Date, cancellationToken)
                || await _operationsRepository.AreAnyConsumptionOperationsAfterAsync(operationToUpdate.AccountId, operationToUpdate.Date, cancellationToken)
                || await _transitionsRepository.AreAnySourceTransitionsAfterAsync(operationToUpdate.AccountId, operationToUpdate.Date, cancellationToken))
            {
                return Result<OperationResponse>.Failure
                    (Error.UnprocessableEntity($"Невозможно отменить операцию с id {command.OperationId}, так как после нее было ручное изменение баланса, были потрачены деньги или был перевод с этого счета",
                        ErrorCodes.OPERATION_UPDATING_DENIED));
            }

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                if (account.Id != operationToUpdate.AccountId)
                {
                    await _operationsRepository.DeleteAsync(operationToUpdate.Id, cancellationToken);
                    await _commonBalanceOperationsRepository.RecalculateAllTailsAsync
                        (operationToUpdate.AccountId, operationToUpdate.Date, operationToUpdate.OldAccountBalance, cancellationToken);

                    Result<OperationResponse> result = await AddOperation(category, account, command.Sum, command.Description, cancellationToken);
                    if (!result.IsSuccess)
                    {
                        await _unitOfWork.RollbackTransactionAsync();
                        return result;
                    }

                    await _unitOfWork.CommitTransactionAsync();
                    return result;
                }

                if (command.Sum != operationToUpdate.Sum || category.Id != operationToUpdate.CategoryId)
                {
                    decimal newAccountBalance;
                    if (category.Type == CategoryType.Income)
                        newAccountBalance = operationToUpdate.OldAccountBalance + command.Sum;
                    else
                    {
                        if (command.Sum > operationToUpdate.OldAccountBalance)
                        {
                            return Result<OperationResponse>.Failure
                                (Error.UnprocessableEntity($"Невозможно изменить операцию с id {operationToUpdate.Id}, так как баланс счета станет меньше нуля после изменения суммы операции",
                                    ErrorCodes.OPERATION_UPDATING_DENIED));
                        }
                        newAccountBalance = operationToUpdate.OldAccountBalance - command.Sum;
                    }
                    operationToUpdate.NewAccountBalance = newAccountBalance;
                    await _commonBalanceOperationsRepository.RecalculateAllTailsAsync
                        (account.Id, operationToUpdate.Date, newAccountBalance, cancellationToken);
                }

                operationToUpdate.CategoryId = category.Id;
                operationToUpdate.Description = command.Description;
                operationToUpdate.Sum = command.Sum;

                Operation updatedOperation = 
                    await _operationsRepository.UpdateAsync(operationToUpdate, cancellationToken);

                await _unitOfWork.CommitTransactionAsync();

                return Result<OperationResponse>.Success
                    (new OperationResponse(updatedOperation.Id, updatedOperation.AccountId, updatedOperation.CategoryId, updatedOperation.Sum, updatedOperation.Description, updatedOperation.Date,
                        updatedOperation.OldAccountBalance, updatedOperation.NewAccountBalance, updatedOperation.Account.Name, updatedOperation.Category.Name, updatedOperation.Category.Type));
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                return Result<OperationResponse>.Failure
                    (Error.InternalServerError("Неизвестная ошибка при обновлении операции", ErrorCodes.UNKNOWN_OPERATION_UPDATING_ERROR));
            }
        }

        private async Task<Result<OperationResponse>> AddOperation(Category category, Account account, decimal sum, string? description, 
            CancellationToken cancellationToken)
        {
            decimal? newBalance;
            decimal oldAccountBalance = account.Balance;

            if (category.Type == CategoryType.Consumption)
            {
                newBalance = await _accountsRepository.TryWithdrawAsync(account.Id, sum, cancellationToken);
                if (newBalance is null)
                    return Result<OperationResponse>.Failure
                        (Error.UnprocessableEntity($"На счете с id {account.Id} недостаточно средств для снятия суммы {sum} рублей",
                            ErrorCodes.NOT_ENOUGH_MONEY));
            }
            else
            {
                newBalance = await _accountsRepository.TryDepositAsync(account.Id, sum, cancellationToken);
                if (newBalance is null)
                    return Result<OperationResponse>.Failure
                        (Error.NotFound($"Не найден счет с id {account.Id}", ErrorCodes.ACCOUNT_NOT_FOUND));
            }
            Operation operation = new Operation
            {
                AccountId = account.Id,
                CategoryId = category!.Id,
                Sum = sum,
                Description = description,
                OldAccountBalance = oldAccountBalance,
                NewAccountBalance = (decimal)newBalance,
            };
            await _operationsRepository.AddAsync(operation, cancellationToken);

            return Result<OperationResponse>.Success
                    (new OperationResponse(operation.Id, operation.AccountId, operation.CategoryId, operation.Sum, operation.Description,
                        operation.Date, operation.OldAccountBalance, operation.NewAccountBalance, account.Name, category.Name, category.Type));
        }
    }
}
