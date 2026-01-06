using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OnlineWallet.Domain.Models;
namespace OnlineWallet.Infrastructure.Interfaces
{
    /// <summary>
    /// Repository interface for account data operations.
    /// Extends generic repository with account-specific functionality.
    /// </summary>
    public interface IAccountsRepository:IRepository<Account>
    {
        /// <summary>
        /// Retrieves all accounts belonging to a specific customer.
        /// </summary>
        /// <param name="customerId">Customer identifier</param>
        /// <returns>Customer's accounts</returns>
        Task<IEnumerable<Account>> GetByCustomerIdAsync(Guid customerId);

    }
}
