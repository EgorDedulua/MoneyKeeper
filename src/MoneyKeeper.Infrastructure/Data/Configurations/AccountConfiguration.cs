using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MoneyKeeper.Core.Models;

namespace MoneyKeeper.Infrastructure.Data.Configurations
{
    public class AccountConfiguration : IEntityTypeConfiguration<Account>
    {
        public void Configure(EntityTypeBuilder<Account> builder)
        {
            builder.Property(a => a.Name).HasMaxLength(100);
            builder
                .HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId);
        }
    }
}
