using OnlineWallet.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlineWallet.Infrastructure.Caching.Services
{
    /// <summary>
    /// Service responsible for generating and managing cache keys for Account entities.
    /// Provides consistent cache key naming conventions and key generation patterns
    /// to ensure proper cache invalidation and data retrieval.
    /// </summary>
    public class AccountsCacheKeyService
    {
        /// <summary>
        /// Generates a cache key for retrieving an account by its unique identifier.
        /// </summary>
        /// <param name="accountId">The unique identifier of the account (GUID)</param>
        /// <returns>
        /// Cache key in the format: "account:id:{accountId}"
        /// Example: "account:id:12345678-1234-1234-1234-123456789012"
        /// </returns>
        public string ByAccountIdKey(Guid accountId) => $"account:id:{accountId}";

        /// <summary>
        /// Generates a cache key for retrieving all accounts belonging to a specific customer.
        /// </summary>
        /// <param name="customerId">The unique identifier of the customer (GUID)</param>
        /// <returns>
        /// Cache key in the format: "account:customer:{customerId}"
        /// Example: "account:customer:87654321-4321-4321-4321-210987654321"
        /// </returns>
        public string ByCustomerIdKey(Guid customerId) => $"account:customer:{customerId}";

        /// <summary>
        /// Generates a cache key for retrieving all accounts in the system.
        /// </summary>
        /// <returns>
        /// Cache key in the format: "accounts:all"
        /// Used for caching the complete collection of all accounts.
        /// </returns>
        public string AllAccountsKey() => $"accounts:all";

        /// <summary>
        /// Generates a complete list of all cache keys associated with a specific account.
        /// </summary>
        /// <param name="account">The account for which to generate cache keys</param>
        /// <returns>
        /// A list containing all cache keys related to the specified account, including:
        /// </returns>
        public List<string> AllKeys(Account account) =>
            new List<string>
            {
                ByAccountIdKey(account.AccountNumber),
                ByCustomerIdKey(account.OwnerId),
                AllAccountsKey()
            };
    }
}
