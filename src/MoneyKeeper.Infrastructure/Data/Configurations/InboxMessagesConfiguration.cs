using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MoneyKeeper.Core.Models;

namespace MoneyKeeper.Infrastructure.Data.Configurations
{
    public class InboxMessagesConfiguration : IEntityTypeConfiguration<InboxMessage>
    {
        public void Configure(EntityTypeBuilder<InboxMessage> builder)
        {
            builder.HasKey(m => m.Id);

            builder.Property(m => m.Id).ValueGeneratedNever();

            builder.Property(m => m.MesssageType).HasMaxLength(200).IsRequired();
        }
    }
}
