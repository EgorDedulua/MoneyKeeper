using FluentValidation;
using MoneyKeeper.Application.Common;
using MoneyKeeper.Contracts.User;
using MoneyKeeper.Core.Common.Repositories;

namespace MoneyKeeper.Validators.User
{
    public class RegisterRequestValidator : AbstractValidator<RegisterUserRequest>
    {
        public RegisterRequestValidator(IUsersRepository usersRepository)
        {
            RuleFor(x => x.Name)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage("Имя профиля не может быть пустым").WithErrorCode(ErrorCodes.NAME_IS_EMPTY)
                .MaximumLength(50).WithMessage("Имя профиля не может быть длиннее 50 символов").WithErrorCode(ErrorCodes.NAME_IS_TOO_LONG);

            RuleFor(x => x.Password)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage("Пароль не может быть пустым").WithErrorCode(ErrorCodes.PASSWORD_IS_EMPTY)
                .MinimumLength(8).WithMessage("Пароль не может быть короче 8 символов").WithErrorCode(ErrorCodes.PASSWORD_IS_TOO_SHORT)
                .MaximumLength(50).WithMessage("Пароль не может быть длиннее 50 символов").WithErrorCode(ErrorCodes.PASSWORD_IS_TOO_LONG);

            RuleFor(x => x.Login)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage("Логин не может быть пустым").WithErrorCode(ErrorCodes.LOGIN_IS_EMPTY)
                .MinimumLength(4).WithMessage("Логин не может быть короче 4 символов").WithErrorCode(ErrorCodes.LOGIN_IS_TOO_SHORT)
                .MaximumLength(50).WithMessage("Логин не может быть длиннее 50 символов").WithErrorCode(ErrorCodes.LOGIN_IS_TOO_LONG);
        }
    }
}
