using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MoneyKeeper.Core.Models;

namespace MoneyKeeper.Infrastructure.Data.Configurations
{
    public class BalanceChangingsConfiguration : IEntityTypeConfiguration<BalanceChanging>
    {
        public void Configure(EntityTypeBuilder<BalanceChanging> builder)
        {
            builder
                .HasOne(b => b.Account)
                .WithMany()
                .HasForeignKey(b => b.AccountId);
        }
    }
}
