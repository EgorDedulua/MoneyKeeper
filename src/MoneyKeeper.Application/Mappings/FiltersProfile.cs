using AutoMapper;
using MoneyKeeper.Application.Contracts.Account;
using MoneyKeeper.Application.Contracts.BalanceChanging;
using MoneyKeeper.Application.Contracts.Category;
using MoneyKeeper.Application.Contracts.Operation;
using MoneyKeeper.Application.Contracts.Transition;
using MoneyKeeper.Application.Filters;

namespace MoneyKeeper.Application.Mappings
{
    public class FiltersProfile : Profile
    {
        public FiltersProfile()
        {
            CreateMap<AccountQueryParameters, AccountsFilter>();
            CreateMap<OperationQueryParameters, OperationsFilter>();
            CreateMap<CategoryQueryParameters, CategoriesFilter>();
            CreateMap<TransitionQueryParameters, TransitionsFilter>();
            CreateMap<BalanceChangingQueryParameters, BalanceChangingsFilter>();
        }
    }
}
