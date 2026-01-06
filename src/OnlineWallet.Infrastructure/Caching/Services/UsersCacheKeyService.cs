using OnlineWallet.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlineWallet.Infrastructure.Caching.Services
{
    /// <summary>
    /// Service responsible for generating and managing cache keys for User entities.
    /// Provides consistent cache key naming conventions and key generation patterns
    /// to support efficient caching of user data across various query patterns.
    /// </summary>
    public class UsersCacheKeyService
    {
        /// <summary>
        /// Generates a cache key for retrieving a user by their unique identifier.
        /// </summary>
        /// <param name="userId">The unique identifier of the user (GUID)</param>
        /// <returns>
        /// Cache key in the format: "user:id:{userId}"
        /// Example: "user:id:12345678-1234-1234-1234-123456789012"
        /// </returns>
        public string ByIdKey(Guid userId) => $"user:id:{userId}";

        /// <summary>
        /// Generates a cache key for retrieving a user by their email address.
        /// </summary>
        /// <param name="email">The email address of the user</param>
        /// <returns>
        /// Cache key in the format: "user:email:{email}"
        /// Example: "user:email:john.doe@example.com"
        /// </returns>
        public string ByEmailKey(string email) => $"user:email:{email}";

        /// <summary>
        /// Generates a cache key for retrieving all users in the system.
        /// </summary>
        /// <returns>
        /// Cache key in the format: "users:all"
        /// </returns>
        public string AllUsersKey() => $"users:all";

        /// <summary>
        /// Generates a cache key for retrieving all users with the Customer role.
        /// </summary>
        /// <returns>
        /// Cache key in the format: "customers:all"
        /// Used for caching the collection of users specifically with Customer role.
        public string AllCustomersKey() => $"customers:all";

        /// <summary>
        /// Generates a complete list of all cache keys associated with a specific user.
        /// </summary>
        /// <param name="user">The user for which to generate cache keys</param>
        /// <returns>
        /// A list containing all cache keys related to the specified user, including:
        /// </returns>
        public List<string> AllKeys(User user) =>
            new List<string>
            {
                ByIdKey(user.Id),
                ByEmailKey(user.Email),
                AllUsersKey()
            };
    }

}
