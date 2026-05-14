using FluentValidation;
using Microsoft.EntityFrameworkCore;
using MoneyKeeper.Application.Common;
using MoneyKeeper.Application.Contracts.Category;
using MoneyKeeper.Core.Common.Repositories;

namespace MoneyKeeper.Validators.Category
{
    public class CategoryCreationValidator : AbstractValidator<CategoryCreationCommand>
    {
        public CategoryCreationValidator(ICategoriesRepository categoriesRepository) 
        {
            RuleFor(command => command)
                .MustAsync(async (command, cancellationToken) =>
                {
                    return !(await categoriesRepository
                    .GetAllByUserId(command.UserId)
                    .AnyAsync(c => c.Name == command.Name, cancellationToken));
                })
                .WithMessage("Категория с таким именем уже существует").WithErrorCode(ErrorCodes.CATEGORY_NAME_ALREADY_EXISTS);
        }
    }
}
