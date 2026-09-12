using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MoneyKeeper.Core.Models;

namespace MoneyKeeper.Infrastructure.Data.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder
                .HasKey(u => u.Id);

            builder
                .Property(u => u.Id)
                .ValueGeneratedNever();

            builder
                .Property(u => u.Email)
                .IsRequired();

            builder
                .HasIndex(u => u.Email)
                .IsUnique();
        }
    }
}
