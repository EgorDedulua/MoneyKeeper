using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using MoneyKeeper.Application.Common;
using MoneyKeeper.Application.Common.Services;
using MoneyKeeper.Application.Common.Validation;
using MoneyKeeper.Application.Contracts.Account;
using MoneyKeeper.Application.Contracts.BalanceChanging;
using MoneyKeeper.Application.Extensions;
using MoneyKeeper.Application.Filters;
using MoneyKeeper.Application.Sorting;
using MoneyKeeper.Core.Common;
using MoneyKeeper.Core.Common.Repositories;
using MoneyKeeper.Core.Models;

namespace MoneyKeeper.Application.Services
{
    public class AccountsService : IAccountsService
    {
        private readonly IAccountsRepository _accountsRepository;
        private readonly IBalanceChangingsService _balanceChangingsService;
        private readonly IValidator<IAccountOwnershipValidationModel> _accountOwnershipValidator;
        private readonly ILogger<AccountsService> _logger;
        public AccountsService(IAccountsRepository accountsRepository, IBalanceChangingsService balanceChangingsService,
            IValidator<IAccountOwnershipValidationModel> accountOwnershipValidator, ILogger<AccountsService> logger)
        {
            _accountsRepository = accountsRepository;
            _balanceChangingsService = balanceChangingsService;
            _accountOwnershipValidator = accountOwnershipValidator;
            _logger = logger;
        }

        public async Task<Result<AccountResponse>> Add(AccountCreationCommand command, CancellationToken cancellationToken)
        {
            if (await _accountsRepository.ExistsWithName(command.Name, command.UserId, cancellationToken))
            {
                return Result<AccountResponse>.Failure
                    (Error.Conflict($"Счет с именем {command.Name} уже существует", ErrorCodes.ACCOUNT_NAME_ALREADY_EXISTS));
            }

            Account account = new Account
            {
                UserId = command.UserId,
                Name = command.Name,
                Balance = command.Balance,
                Target = command.Target,
                Description = command.Description
            };

            await _accountsRepository.AddAsync(account, cancellationToken);
            _logger.LogInformation
                ("Пользователь с id {UserId} создал счёт с id {AccountId} и с именем {AccountName}", command.UserId, account.Id, command.Name);

            return Result<AccountResponse>.Success(new AccountResponse(account.Id, account.Name, account.Balance, account.Target, account.Description, account.CreatedAt));
        }

        public async Task<Result<bool>> Delete(AccountDeletionCommand command, CancellationToken cancellationToken)
        {
            ValidationResult validationResult = await _accountOwnershipValidator.ValidateAsync(command, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Result<bool>.Failure(validationResult.ToError());
            }

            await _accountsRepository.DeleteAsync(command.AccountId, cancellationToken);
            _logger.LogInformation
                ("Пользователь с id {UserId} удалил счёт с id {AccountId}", command.UserId, command.AccountId);

            return Result<bool>.Success(true);
        }

        public async Task<Result<PagedResult<AccountResponse>>> GetAll(AccountQueryParameters parameters, int userId, CancellationToken cancellationToken)
        {
            AccountFilter filter = new AccountFilter
            {
                MinBalance = parameters.MinBalance,
                MaxBalance = parameters.MaxBalance,
                FromDate = parameters.FromDate,
                ToDate = parameters.ToDate,
                NameSubstring = parameters.NameSubstring,
                HasTargetedBalance = parameters.HasTargetedBalance
            };

            IQueryable<Account> query = _accountsRepository.GetAllByUserId(userId);
            query = filter.ApplyTo(query);
            query = query.ApplySorting(parameters.SortBy);

            var (items, totalCount) = await _accountsRepository
                .GetAllPagedAsync(query, parameters.Page, parameters.PageSize, cancellationToken);

            List<AccountResponse> responseItems = items
                .Select(a => new AccountResponse(a.Id, a.Name, a.Balance, a.Target, a.Description, a.CreatedAt))
                .ToList();

            return Result<PagedResult<AccountResponse>>.Success
                (new PagedResult<AccountResponse>(responseItems, totalCount, parameters.Page, parameters.PageSize));
        }

        public async Task<Result<AccountResponse>> Update(AccountUpdateCommand command, CancellationToken cancellationToken)
        {
            ValidationResult validationResult = await _accountOwnershipValidator.ValidateAsync(command, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Result<AccountResponse>.Failure(validationResult.ToError());
            }

            if (await _accountsRepository
                .ExistsAnotherWithName(command.Name, command.AccountId, command.UserId, cancellationToken))
            {
                return Result<AccountResponse>.Failure
                    (Error.Conflict($"Счет с именем {command.Name} уже существует", ErrorCodes.ACCOUNT_NAME_ALREADY_EXISTS));
            }

            Account account = (await _accountsRepository.GetByIdAsync(command.AccountId, cancellationToken))!;
            if (account.Balance != command.Balance)
            {
                Result<BalanceChangingResponse> balanceChangingAddResult =
                    await _balanceChangingsService.Add(new BalanceChangingCreationCommand(account.UserId, account.Id, command.Balance), cancellationToken);

                if (!balanceChangingAddResult.IsSuccess)
                    return Result<AccountResponse>.Failure(balanceChangingAddResult.Error!);

                _logger.LogInformation
                    ("Пользователь с id {UserId} изменил баланс счёта с id {AccountId} на значение {NewBalance} и создал изменения баланса с id {BalanceChangingId}", 
                        command.UserId, command.AccountId, command.Balance, balanceChangingAddResult.Value.Id);
            }
            account.Name = command.Name;
            account.Target = command.Target;
            account.Description = command.Description;

            Account updatedAccount = await _accountsRepository.UpdateAsync(account, cancellationToken);
            _logger.LogInformation
                ("Пользователь с id {UserId} обновил счёт с id {AccountId}", command.UserId, command.AccountId);

            return Result<AccountResponse>.Success
                (new AccountResponse(updatedAccount.Id, updatedAccount.Name, updatedAccount.Balance, updatedAccount.Target, updatedAccount.Description, updatedAccount.CreatedAt));
        }
    }
}
