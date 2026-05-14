using MoneyKeeper.Application.Common.Services;
using MoneyKeeper.Application.Contracts.Account;
using MoneyKeeper.Core.Common;
using MoneyKeeper.Core.Common.Repositories;
using MoneyKeeper.Core.Models;
using MoneyKeeper.Application.Contracts.BalanceChanging;
using MoneyKeeper.Application.Filters;
using MoneyKeeper.Application.Sorting;
using MoneyKeeper.Application.Common;

namespace MoneyKeeper.Application.Services
{
    public class AccountsService : IAccountsService
    {
        private readonly IAccountsRepository _accountsRepository;
        private readonly IBalanceChangingsService _balanceChangingsService;

        public AccountsService(IAccountsRepository accountsRepository, IBalanceChangingsService balanceChangingsService) 
        { 
            _accountsRepository = accountsRepository;
            _balanceChangingsService = balanceChangingsService;
        }

        public async Task<Result<AccountResponse>> Add(AccountCreationCommand command, CancellationToken cancellationToken)
        {
            Account account = new Account
            {
                UserId = command.UserId,
                Name = command.Name,
                Balance = command.Balance,
                Target = command.Target,
                Description = command.Description
            };

            await _accountsRepository.AddAsync(account, cancellationToken);
            return Result<AccountResponse>.Success(new AccountResponse(account.Id, account.Name, account.Balance, account.Target, account.Description, account.CreatedAt));
        }

        public async Task<Result<bool>> Delete(int accountId, CancellationToken cancellationToken)
        {
            await _accountsRepository.DeleteAsync(accountId, cancellationToken);
            return Result<bool>.Success(true);
        }

        public async Task<Result<PagedResult<AccountResponse>>> GetAll(AccountQueryParameters parameters,int userId, CancellationToken cancellationToken)
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
            Account account = (await _accountsRepository.GetByIdAsync(command.AccountId, cancellationToken))!;
            if (account.Balance != command.Balance)
            { 
                Result<BalanceChangingResponse> balanceChangingAddResult = 
                    await _balanceChangingsService.Add(new BalanceChangingCreationCommand(account.UserId, account.Id, command.Balance), cancellationToken);

                if (!balanceChangingAddResult.IsSuccess)
                    return Result<AccountResponse>.Failure(balanceChangingAddResult.Error!);
            }
            account.Name = command.Name;
            account.Target = command.Target;
            account.Description  = command.Description;

            Account updatedAccount = await _accountsRepository.UpdateAsync(account, cancellationToken);
            return Result<AccountResponse>.Success
                (new AccountResponse(updatedAccount.Id, updatedAccount.Name, updatedAccount.Balance, updatedAccount.Target, updatedAccount.Description, updatedAccount.CreatedAt));
        }
    }
}
