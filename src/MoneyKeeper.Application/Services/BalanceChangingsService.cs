using AutoMapper;
using FluentValidation;
using FluentValidation.Results;
using MoneyKeeper.Application.Common;
using MoneyKeeper.Application.Common.Services;
using MoneyKeeper.Application.Common.Validation;
using MoneyKeeper.Application.Contracts.BalanceChanging;
using MoneyKeeper.Application.Extensions;
using MoneyKeeper.Application.Filters;
using MoneyKeeper.Application.Sorting;
using MoneyKeeper.Core.Common;
using MoneyKeeper.Core.Common.Repositories;
using MoneyKeeper.Core.Models;

namespace MoneyKeeper.Application.Services
{
    public class BalanceChangingsService : IBalanceChangingsService
    {
        private readonly IBalanceChangingsRepository _balanceChangingsRepository;
        private readonly IAccountsRepository _accountsRepository;
        private readonly IOperationsRepository _operationsRepository;
        private readonly ITransitionsRepository _transitionsRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICommonBalanceOperationsRepository _commonBalanceOperationsRepository;
        private readonly IValidator<IAccountOwnershipValidationModel> _accountOwnershipValidator;
        private readonly IValidator<IBalanceChangingOwnershipValidationModel> _balanceChangingOwnershipValidator;
        private readonly IMapper _mapper;

        public BalanceChangingsService(IBalanceChangingsRepository balanceChangingsRepository, IOperationsRepository operationsRepository,
            ITransitionsRepository transitionsRepository, IUnitOfWork unitOfWork, IAccountsRepository accountsRepository, ICommonBalanceOperationsRepository commonBalanceOperationsRepository,
            IValidator<IAccountOwnershipValidationModel> accountOwnershipValidator, IValidator<IBalanceChangingOwnershipValidationModel> balanceChangingOwnershipValidator,
            IMapper mapper)
        {
            _balanceChangingsRepository = balanceChangingsRepository;
            _operationsRepository = operationsRepository;
            _transitionsRepository = transitionsRepository;
            _unitOfWork = unitOfWork;
            _accountsRepository = accountsRepository;
            _commonBalanceOperationsRepository = commonBalanceOperationsRepository;
            _accountOwnershipValidator = accountOwnershipValidator;
            _balanceChangingOwnershipValidator = balanceChangingOwnershipValidator;
            _mapper = mapper;
        }

        public async Task<Result<BalanceChangingResponse>> Add(BalanceChangingCreationCommand command, CancellationToken cancellationToken)
        {
            ValidationResult validationResult = await _accountOwnershipValidator.ValidateAsync(command, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Result<BalanceChangingResponse>.Failure(validationResult.ToError());
            }

            Account account = (await _accountsRepository.GetByIdAsync(command.AccountId, cancellationToken))!;
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                Result<BalanceChangingResponse> result = await AddBalanceChanging(account, command.NewAccountBalance, cancellationToken);
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
                return Result<BalanceChangingResponse>.Failure
                    (Error.InternalServerError("Неизветсная ошибка при добавлении изменения баланса", ErrorCodes.UNKNOWN_BALANCE_CHANGING_CREATION_ERROR));
            }
        }

        public async Task<Result<bool>> Delete(BalanceChangingDeletionCommand command, CancellationToken cancellationToken)
        {
            ValidationResult validationResult = await _balanceChangingOwnershipValidator.ValidateAsync(command, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Result<bool>.Failure(validationResult.ToError());
            }

            BalanceChanging balanceChangingToDelete = (await _balanceChangingsRepository.GetByIdAsync(command.BalanceChangingId, cancellationToken))!;
            Account account = balanceChangingToDelete.Account;

            if (await _operationsRepository.AreAnyConsumptionOperationsAfterAsync(account.Id, balanceChangingToDelete.Date, cancellationToken)
                || await _transitionsRepository.AreAnySourceTransitionsAfterAsync(account.Id, balanceChangingToDelete.Date, cancellationToken)
                || await _balanceChangingsRepository.AreAnyAfterAsync(account.Id, balanceChangingToDelete.Date, cancellationToken))
            {
                return Result<bool>.Failure
                    (Error.UnprocessableEntity($"Невозможно отменить изменение баланса с id {balanceChangingToDelete.Id}, так как после него были потрачены деньги путем изменения баланса " +
                    $", создания операции по трате денег или переводом с этого счета", ErrorCodes.MONEY_CANNOT_BE_RESTORED));
            }

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                await _balanceChangingsRepository.DeleteAsync(balanceChangingToDelete.Id, cancellationToken);
                await _commonBalanceOperationsRepository.RecalculateAllTailsAsync(account.Id, balanceChangingToDelete.Date,
                    balanceChangingToDelete.OldAccountBalance, cancellationToken);
                await _unitOfWork.CommitTransactionAsync();

                return Result<bool>.Success(true);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                return Result<bool>.Failure
                    (Error.InternalServerError("Неизвестная ошибка при удалении изменения баланса", ErrorCodes.UNKNOWN_BALANCE_CHANGING_DELETION_ERROR));
            }
        }

        public async Task<Result<PagedResult<BalanceChangingResponse>>> GetAll(BalanceChangingQueryParameters parameters, int userId, CancellationToken cancellationToken)
        {
            List<int> userAccountsIds = _accountsRepository
                .GetAllByUserId(userId)
                .Select(a => a.Id)
                .ToList();

            List<int> requestedAccountIds = new List<int>();
            if (parameters.AccountIds.Any())
                requestedAccountIds = userAccountsIds.Intersect(parameters.AccountIds).ToList();
            else
                requestedAccountIds = userAccountsIds;

            if (!requestedAccountIds.Any())
                return Result<PagedResult<BalanceChangingResponse>>.Success
                    (new PagedResult<BalanceChangingResponse>(new List<BalanceChangingResponse>(), 0, parameters.Page, parameters.PageSize));

            BalanceChangingsFilter filter = _mapper.Map<BalanceChangingsFilter>(parameters);
            
            IQueryable<BalanceChanging> query = _balanceChangingsRepository.GetAllByUserId(userId);
            query = filter.ApplyTo(query);
            query = query.ApplySorting(parameters.SortBy);

            var (items, totalCount) =
                await _balanceChangingsRepository.GetAllPagedAsync(query, parameters.Page, parameters.PageSize, cancellationToken);

            List<BalanceChangingResponse> responseItems = _mapper.Map<List<BalanceChangingResponse>>(items);

            return Result<PagedResult<BalanceChangingResponse>>.Success
                (new PagedResult<BalanceChangingResponse>(responseItems, totalCount, parameters.Page, parameters.PageSize));
        }

        public async Task<Result<BalanceChangingResponse>> Update(BalanceChangingUpdateCommand command, CancellationToken cancellationToken = default)
        {
            List<ValidationResult> validationResults = new List<ValidationResult>
            {
                await _balanceChangingOwnershipValidator.ValidateAsync(command, cancellationToken),
                await _accountOwnershipValidator.ValidateAsync(command, cancellationToken),
            };
            if (validationResults.Any(r => !r.IsValid))
            {
                return Result<BalanceChangingResponse>.Failure(validationResults.ToError()!);
            }

            BalanceChanging balanceChangingToUpdate = (await _balanceChangingsRepository.GetByIdAsync(command.BalanceChangingId))!;
            Account account = (await _accountsRepository.GetByIdAsync(command.AccountId))!;

            if (await _operationsRepository.AreAnyConsumptionOperationsAfterAsync(balanceChangingToUpdate.AccountId, balanceChangingToUpdate.Date, cancellationToken)
                || await _transitionsRepository.AreAnySourceTransitionsAfterAsync(balanceChangingToUpdate.AccountId, balanceChangingToUpdate.Date, cancellationToken)
                || await _balanceChangingsRepository.AreAnyAfterAsync(balanceChangingToUpdate.AccountId, balanceChangingToUpdate.Date, cancellationToken))
            {
                return Result<BalanceChangingResponse>.Failure
                    (Error.UnprocessableEntity($"Невозможно обновить изменение баланса с id {command.BalanceChangingId}, так как после него было ручное изменение баланса, были потрачены деньги или был перевод с этого счета",
                        ErrorCodes.MONEY_CANNOT_BE_RESTORED));
            }

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                if (balanceChangingToUpdate.AccountId != account.Id)
                {
                    await _balanceChangingsRepository.DeleteAsync(balanceChangingToUpdate.Id, cancellationToken);
                    await _commonBalanceOperationsRepository.RecalculateAllTailsAsync(balanceChangingToUpdate.AccountId, balanceChangingToUpdate.Date, 
                        balanceChangingToUpdate.OldAccountBalance, cancellationToken);
                    Result<BalanceChangingResponse> result = await AddBalanceChanging(account, command.NewAccountBalance, cancellationToken);
                    if (!result.IsSuccess)
                    {
                        await _unitOfWork.RollbackTransactionAsync();
                        return result;
                    }

                    await _unitOfWork.CommitTransactionAsync();
                    return result;
                }

                if (balanceChangingToUpdate.NewAccountBalance != command.NewAccountBalance)
                {
                    balanceChangingToUpdate.NewAccountBalance = command.NewAccountBalance;
                    await _commonBalanceOperationsRepository.RecalculateAllTailsAsync(balanceChangingToUpdate.AccountId, balanceChangingToUpdate.Date,
                        balanceChangingToUpdate.NewAccountBalance, cancellationToken);
                }

                BalanceChanging updatedBalanceChanging =
                    await _balanceChangingsRepository.UpdateAsync(balanceChangingToUpdate, cancellationToken);

                await _unitOfWork.CommitTransactionAsync();

                return Result<BalanceChangingResponse>.Success
                    (_mapper.Map<BalanceChangingResponse>(updatedBalanceChanging));
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                return Result<BalanceChangingResponse>.Failure
                    (Error.InternalServerError("Неизвестная ошибка при обовлении изменения баланса", ErrorCodes.UNKNOWN_BALANCE_UPDATING_DELETION_ERROR));
            }
        }

        private async Task<Result<BalanceChangingResponse>> AddBalanceChanging(Account account, decimal newAccountBalance, CancellationToken cancellationToken)
        {
            BalanceChanging balanceChanging = new BalanceChanging
            {
                AccountId = account.Id,
                OldAccountBalance = account.Balance,
                NewAccountBalance = newAccountBalance
            };

            await _accountsRepository.UpdateBalanceAsync(account.Id, newAccountBalance, cancellationToken);
            await _balanceChangingsRepository.AddAsync(balanceChanging, cancellationToken);

            return Result<BalanceChangingResponse>.Success
                (_mapper.Map<BalanceChangingResponse>(balanceChanging));
        }
    }
}
