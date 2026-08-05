using AutoMapper;
using MoneyKeeper.Application.Contracts.Account;
using MoneyKeeper.Application.Contracts.BalanceChanging;
using MoneyKeeper.Application.Contracts.Category;
using MoneyKeeper.Application.Contracts.Operation;
using MoneyKeeper.Application.Contracts.Transition;
using MoneyKeeper.Core.Models;

namespace MoneyKeeper.Application.Mappings
{
    public class ModelsProfile : Profile
    {
        public ModelsProfile()
        {
            CreateMap<Account, AccountResponse>();
            CreateMap<Category, CategoryResponse>();
            CreateMap<Operation, OperationResponse>()
                .ForMember(dest => dest.AccountName,
                    opt => opt.MapFrom(src => src.Account.Name))
                .ForMember(dest => dest.CategoryName,
                    opt => opt.MapFrom(src => src.Category.Name))
                .ForMember(dest => dest.CategoryType,
                    opt => opt.MapFrom(src => src.Category.Type));
            CreateMap<BalanceChanging, BalanceChangingResponse>()
                .ForMember(dest => dest.AccountName,
                    opt => opt.MapFrom(src => src.Account.Name));
            CreateMap<Transition, TransitionResponse>()
                .ForMember(dest => dest.SourceAccountName,
                    opt => opt.MapFrom(src => src.SourceAccount.Name))
                .ForMember(dest => dest.DestinationAccountName,
                    opt => opt.MapFrom(src => src.DestinationAccount.Name));
        }
    }
}
