using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OnlineWallet.Domain.Models;
using OnlineWallet.Infrastructure.Interfaces;

namespace OnlineWallet.Infrastructure.Repositories
{
    /// <summary>
    /// Entity Framework implementation of IAccountsRepository.
    /// Handles database operations for Account entities.
    /// </summary>
    public class AccountsRepository:IAccountsRepository
    {
        private readonly WalletDbContext _dbContext;

        /// <summary>
        /// Initializes a new instance of AccountsRepository.
        /// </summary>
        /// <param name="dbContext">Database context for data access</param>
        public AccountsRepository(WalletDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// Adds a new account to the database.
        /// </summary>
        /// <param name="account">Account to add</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task AddAsync(Account account)
        {
            await  _dbContext.Accounts.AddAsync(account);
        }

        /// <summary>
        /// Retrieves all accounts from the database.
        /// Uses AsNoTracking for read-only performance optimization.
        /// </summary>
        /// <returns>All accounts</returns>
        public async Task<IEnumerable<Account>> GetAllAsync()
        {
            return await _dbContext.Accounts
                .AsNoTracking()
                .ToListAsync();
        }

        /// <summary>
        /// Retrieves an account by its account number.
        /// </summary>
        /// <param name="accountNumber">Account identifier</param>
        /// <returns>Account if found, null otherwise</returns>
        public async Task<Account?> GetByIdAsync(Guid accountNumber)
        {
            return await _dbContext.Accounts.FindAsync(accountNumber); 
        }

        /// <summary>
        /// Retrieves all accounts belonging to a specific customer.
        /// Uses AsNoTracking for read-only performance optimization.
        /// </summary>
        /// <param name="customerId">Customer identifier</param>
        /// <returns>Customer's accounts</returns>
        public async Task<IEnumerable<Account>> GetByCustomerIdAsync(Guid customerId)
        {
            return await _dbContext.Accounts.
                Where(account => account.OwnerId == customerId)
                .AsNoTracking()
                .ToListAsync();
        }

        /// <summary>
        /// Updates an existing account in the database.
        /// </summary>
        /// <param name="account">Account to update</param>
        public void Update(Account account)
        {
            _dbContext.Accounts.Update(account);
        }

        /// <summary>
        /// Removes an account from the database.
        /// </summary>
        /// <param name="account">Account to delete</param>
        public void Delete(Account account)
        {
            _dbContext.Accounts.Remove(account);
        }

    }
}
