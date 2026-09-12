using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using MoneyKeeper.Core.Models;
using MoneyKeeper.Infrastructure.Data;

namespace MoneyKeeper.Infrastructure.Tests.Common
{
    public abstract class RepositoryTestsBase : IClassFixture<DbFixture>, IAsyncLifetime
    {
        protected readonly AppDbContext _context;
        private IDbContextTransaction _transaction = null!;

        protected RepositoryTestsBase(DbFixture fixture)
        {
            _context = new AppDbContext(fixture.DbOptions);
        }

        public virtual async Task InitializeAsync()
        {
            _transaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task DisposeAsync()
        {
            if (_transaction is not null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
            }

            if (_context is not null)
                await _context.DisposeAsync();
        }

        protected async Task<User> CreateTestUserAsync(int userId = 1)
        {
            User? existing = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (existing != null)
                return existing;

            User user = new User
            {
                Email = $"testuser{userId}@gmail.com"
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }
    }
}
