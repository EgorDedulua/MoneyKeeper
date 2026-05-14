using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MoneyKeeper.Core.Models;

namespace MoneyKeeper.Infrastructure.Data.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.Property(u => u.Login).HasMaxLength(50);
            builder.Property(u => u.Password).HasMaxLength(256);
            builder.Property(u => u.UserName).HasMaxLength(50);
        }
    }
}
