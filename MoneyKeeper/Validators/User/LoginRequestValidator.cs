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

            RuleFor(request => request)
                .MustAsync(async (request, cancellationToken) =>
                {
                    Core.Models.User? user = await usersRepository.GetByLoginAsync(request.Login, cancellationToken);
                    return (user is not null && passwordHasher.Verify(user.Password, request.Password));
                })
                .WithMessage("Неверный логин или пароль").WithErrorCode(ErrorCodes.INVALID_LOGIN_OR_PASSWORD);
        }
    }
}
