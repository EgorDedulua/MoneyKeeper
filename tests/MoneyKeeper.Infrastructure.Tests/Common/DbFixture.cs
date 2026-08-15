using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using MoneyKeeper.Infrastructure.Data;
using Testcontainers.MsSql;

namespace MoneyKeeper.Infrastructure.Tests.Common
{
    public class DbFixture : IAsyncLifetime
    {
        static DbFixture()
        {
            string envFile = Path.Combine(AppContext.BaseDirectory, ".env.test");
            if (File.Exists(envFile))
                Env.Load(envFile);
        }

        public MsSqlContainer Container { get; }

        public DbContextOptions<AppDbContext> DbOptions { get; private set; } = null!;

        public DbFixture()
        {
            string password = Environment.GetEnvironmentVariable("TEST_DB_PASSWORD")
                ?? throw new InvalidOperationException("TEST_DB_PASSWORD not set");

            Container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
                .WithEnvironment("ACCEPT_EULA", "Y")
                .WithPassword(password)
                .WithAutoRemove(true)
                .WithCleanUp(true)
                .Build();
        }

        public async Task InitializeAsync()
        {
            await Container.StartAsync();
            string connectionString = $"{Container.GetConnectionString()};Database=MoneyManagementTest;TrustServerCertificate=true;";
            DbOptions = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer(connectionString)
                .Options;
            using var context = new AppDbContext(DbOptions);
            await context.Database.EnsureCreatedAsync();
        }

        public async Task DisposeAsync()
        {
            await Container.DisposeAsync();
        }
    }
}
