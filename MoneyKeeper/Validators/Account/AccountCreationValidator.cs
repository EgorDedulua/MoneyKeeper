using FluentValidation;
using Microsoft.EntityFrameworkCore;
using MoneyKeeper.Application.Common;
using MoneyKeeper.Application.Contracts.Account;
using MoneyKeeper.Core.Common.Repositories;

namespace MoneyKeeper.Validators.Account
{
    public class AccountCreationValidator : AbstractValidator<AccountCreationCommand>
    {
        public AccountCreationValidator(IAccountsRepository accountsRepository) 
        {
            RuleFor(command => command)
                .MustAsync(async (command, cancellationToken) =>
                {
                    return !(await accountsRepository
                        .GetAllByUserId(command.UserId)
                        .AnyAsync(a => a.Name == command.Name, cancellationToken));
                })
                .WithMessage("Счет с таким именем уже существует").WithErrorCode(ErrorCodes.ACCOUNT_NAME_ALREADY_EXISTS);
        }
    }
}
