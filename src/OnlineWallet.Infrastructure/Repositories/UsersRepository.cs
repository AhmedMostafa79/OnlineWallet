using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OnlineWallet.Infrastructure.Interfaces;
using OnlineWallet.Domain.Models;
using OnlineWallet.Domain.Enums;
using Microsoft.EntityFrameworkCore;
namespace OnlineWallet.Infrastructure.Repositories
{
    /// <summary>
    /// Entity Framework implementation of IUsersRepository.
    /// Handles database operations for User entities.
    /// </summary>
    public class UsersRepository: IUsersRepository
    {
        private readonly WalletDbContext _dbContext;

        /// <summary>
        /// Initializes a new instance of UsersRepository.
        /// </summary>
        /// <param name="dbContext">Database context for data access</param>
        public UsersRepository(WalletDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// Adds a new user to the database.
        /// </summary>
        /// <param name="user">User to add</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task AddAsync(User user)
        {
            await _dbContext.Users.AddAsync(user);
        }

        /// <summary>
        /// Retrieves all users from the database.
        /// Uses AsNoTracking for read-only performance optimization.
        /// </summary>
        /// <returns>All users</returns>
        public async Task<IEnumerable<User>> GetAllAsync()
        {
            return await _dbContext.Users
                .AsNoTracking()
                .ToListAsync();
        }

        /// <summary>
        /// Retrieves all users with customer role.
        /// Uses AsNoTracking for read-only performance optimization.
        /// </summary>
        /// <returns>All customer users</returns>
        public async Task<IEnumerable<User>> GetAllCustomersAsync()
        {
            return await _dbContext.Users
                .Where(u=>u.Role==UserRole.Customer)
                .AsNoTracking()
                .ToListAsync();
        }

        /// <summary>
        /// Retrieves a user by their unique identifier.
        /// </summary>
        /// <param name="id">User identifier</param>
        /// <returns>User if found, null otherwise</returns>
        public async Task<User?> GetByIdAsync(Guid id)
        {
            return  await _dbContext.Users.FindAsync( id);
        }

        /// <summary>
        /// Finds a user by email address.
        /// </summary>
        /// <param name="email">Email to search for</param>
        /// <returns>User if found, null otherwise</returns>
        public async Task<User?> FindByEmailAsync(string email)
        {
            return await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        /// <summary>
        /// Updates an existing user in the database.
        /// </summary>
        /// <param name="user">User to update</param>
        public void Update(User user)
        {
           _dbContext.Users.Update(user);
        }

        /// <summary>
        /// Removes a user from the database.
        /// </summary>
        /// <param name="user">User to delete</param>
        public void  Delete(User user)
        {
            _dbContext.Users.Remove(user);
        }
    }
}
