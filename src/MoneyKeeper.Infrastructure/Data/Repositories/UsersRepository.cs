using Microsoft.EntityFrameworkCore;
using MoneyKeeper.Core.Common.Repositories;
using MoneyKeeper.Core.Models;

namespace MoneyKeeper.Infrastructure.Data.Repositories
{
    public class UsersRepository : IUsersRepository
    {
        private readonly AppDbContext _db;

        public UsersRepository(AppDbContext db) 
        {
            _db = db;
        }

        public async Task AddAsync(User user, CancellationToken cancellationToken)
        {
            await _db.Users.AddAsync(user, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteByIdAsync(int id, CancellationToken cancellationToken)
        {
            await _db.Users
                .Where(u => u.Id == id)
                .ExecuteDeleteAsync();
        }

        public async Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        }
    }
}
