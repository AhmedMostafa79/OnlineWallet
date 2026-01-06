using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OnlineWallet.Domain.Models; 
namespace OnlineWallet.Infrastructure.Interfaces
{
    /// <summary>
    /// Repository interface for user data operations.
    /// Extends generic repository with user-specific functionality.
    /// </summary>
    public interface  IUsersRepository : IRepository<User>
    {
        /// <summary>
        /// Retrieves all users with customer role.
        /// </summary>
        /// <returns>All customer users</returns>
        public Task<IEnumerable<User>> GetAllCustomersAsync();

        /// <summary>
        /// Finds a user by email address.
        /// </summary>
        /// <param name="email">Email to search for</param>
        /// <returns>User if found, null otherwise</returns>
        public Task<User?> FindByEmailAsync(string email);


    }
}
