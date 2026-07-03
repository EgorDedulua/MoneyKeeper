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
            user.CreatedAt = DateTime.Now;
            await _db.Users.AddAsync(user, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<User?> GetByLoginAsync(string login, CancellationToken cancellationToken)
        {
            return await _db.Users.FirstOrDefaultAsync(u => u.Login == login, cancellationToken);
        }
    }
}
