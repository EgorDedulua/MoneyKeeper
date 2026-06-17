using FluentValidation;
using MoneyKeeper.Application.Common;
using MoneyKeeper.Application.Common.Auth;
using MoneyKeeper.Contracts.User;
using MoneyKeeper.Core.Common.Repositories;

namespace MoneyKeeper.Validators.User
{
    public class LoginRequestValidator : AbstractValidator<LoginUserRequest>
    {
        public LoginRequestValidator(IUsersRepository usersRepository, IPasswordHasher passwordHasher) 
        {
            RuleFor(x => x.Login)
                .NotEmpty().WithMessage("Логин не может быть пустым").WithErrorCode(ErrorCodes.LOGIN_IS_EMPTY);

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Пароль не может быть пустым").WithErrorCode(ErrorCodes.PASSWORD_IS_EMPTY);
        }
    }
}
