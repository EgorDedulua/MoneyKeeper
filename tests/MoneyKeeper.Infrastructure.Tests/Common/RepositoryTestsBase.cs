using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using MoneyKeeper.Core.Models;
using MoneyKeeper.Infrastructure.Data;
using Testcontainers.MsSql;

namespace MoneyKeeper.Infrastructure.Tests.Common
{
    public abstract class RepositoryTestsBase : IAsyncLifetime
    {
        static RepositoryTestsBase()
        {
            string envFile = Path.Combine(AppContext.BaseDirectory, ".env.test");
            if (File.Exists(envFile))
                Env.Load(envFile);
        }

        protected readonly MsSqlContainer _container;
        protected AppDbContext _context = null!;

        protected RepositoryTestsBase()
        {
            string password = Environment.GetEnvironmentVariable("TEST_DB_PASSWORD")
                ?? throw new InvalidOperationException("TEST_DB_PASSWORD not set");

            _container = new MsSqlBuilder("mssql/server:2022-latest")
                .WithPassword(password)
                .WithAutoRemove(true)
                .WithCleanUp(true)
                .Build();
        }

        public virtual async Task InitializeAsync()
        {
            await _container.StartAsync();
            string connectionString = $"{_container.GetConnectionString()};Database=MoneyManagementTest;TrustServerCertificate=true;";
            DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer(connectionString)
                .Options;
            _context = new AppDbContext(options);
            await _context.Database.EnsureCreatedAsync();
        }

        public async Task DisposeAsync()
        {
            if (_context is not null)
                await _context.DisposeAsync();
            await _container.DisposeAsync();
        }

        protected async Task ClearDatabaseAsync()
        {
            _context.Operations.RemoveRange(_context.Operations);
            _context.Transitions.RemoveRange(_context.Transitions);
            _context.BalanceChangings.RemoveRange(_context.BalanceChangings);
            _context.Categories.RemoveRange(_context.Categories);
            _context.Accounts.RemoveRange(_context.Accounts);
            _context.Users.RemoveRange(_context.Users);
            await _context.SaveChangesAsync();
        }

        protected async Task<User> CreateTestUserAsync(int userId = 1)
        {
            User? existing = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (existing != null)
                return existing;

            User user = new User
            {
                Login = $"testuser{userId}",
                UserName = $"Test User {userId}",
                Password = "hashed_password"
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }
    }
}
