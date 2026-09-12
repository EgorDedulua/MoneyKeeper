using Microsoft.EntityFrameworkCore;
using MoneyKeeper.Core.Models;
using MoneyKeeper.Infrastructure.Data.Configurations;

namespace MoneyKeeper.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Account> Accounts { get; set; }

    public DbSet<BalanceChanging> BalanceChangings { get; set; }

    public DbSet<Category> Categories { get; set; }

    public DbSet<Operation> Operations { get; set; }

    public DbSet<Transition> Transitions { get; set; }

    public DbSet<User> Users { get; set; }

    public DbSet<InboxMessage> InboxMessages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new AccountConfiguration());
        modelBuilder.ApplyConfiguration(new CategoryConfiguration());
        modelBuilder.ApplyConfiguration(new OperationConfiguration());
        modelBuilder.ApplyConfiguration(new TransitionConfiguration());
        modelBuilder.ApplyConfiguration(new BalanceChangingsConfiguration());
        modelBuilder.ApplyConfiguration(new InboxMessagesConfiguration());
    }
}
