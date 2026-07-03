using MoneyKeeper.Core.Models;

namespace MoneyKeeper.Application.Common.Auth
{
    public interface IJwtService
    {
        string Generate(User user);
    }
}
