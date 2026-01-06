using Microsoft.Extensions.Caching.Distributed;
using OnlineWallet.Domain.Enums;
using OnlineWallet.Domain.Models;
using OnlineWallet.Infrastructure.Caching.Services;
using OnlineWallet.Infrastructure.Interfaces;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace OnlineWallet.Infrastructure.Caching.Decorators
{
    /// <summary>
    /// Decorator implementation of IUsersRepository that adds distributed caching functionality.
    /// Implements the Decorator pattern to wrap an existing users repository with caching capabilities.
    /// Provides optimized caching strategies for user data with role-based cache invalidation.
    /// </summary>
    public class CachedUsersRepository : IUsersRepository
    {
        private readonly IUsersRepository _decoratedRepo;
        private readonly IDistributedCache _cache;
        private readonly UsersCacheKeyService _keyService;

        /// <summary>
        /// Default cache entry options for user data.
        /// Uses sliding expiration of 10 minutes and absolute expiration of 1 hour.
        /// Longer sliding expiration compared to accounts due to user data stability.
        /// </summary>
        private readonly DistributedCacheEntryOptions _cacheOptions = new()
        {
            SlidingExpiration = TimeSpan.FromMinutes(10),
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
        };

        /// <summary>
        /// Initializes a new instance of CachedUsersRepository.
        /// </summary>
        /// <param name="decoratedRepo">The underlying users repository to decorate with caching</param>
        /// <param name="cache">Distributed cache implementation for storing cached user data</param>
        /// <param name="keyService">Service for generating user-specific cache keys</param>
        public CachedUsersRepository(IUsersRepository decoratedRepo, IDistributedCache cashe, UsersCacheKeyService keyService)
        {
            _decoratedRepo = decoratedRepo;
            _cache = cashe;
            _keyService = keyService;
        }

        /// <summary>
        /// Adds a new user to the underlying repository and invalidates related cache entries.
        /// Cache invalidation is performed with fire-and-forget semantics; failures are silently ignored.
        /// </summary>
        /// <param name="user">User to add to the system</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task AddAsync(User user)
        {
                await _decoratedRepo.AddAsync(user);
            try
            {
                // Invalidate lists (e.g., GetAll, GetAllCustomers) when adding a new user                await InvalidateUserCacheAsync(user);
            }
            catch { }// no need to throw exception as user addition succeeded
        }

        /// <summary>
        /// Retrieves a user by their unique identifier with caching support.
        /// First attempts to retrieve from cache; falls back to the underlying repository on cache miss or failure.
        /// Automatically caches retrieved users for future requests.
        /// </summary>
        /// <param name="id">User identifier (GUID)</param>
        /// <returns>User if found, null otherwise</returns>
        public async Task<User?> GetByIdAsync(Guid id)
        {
            var cacheKey = _keyService.ByIdKey(id);
            try
            {
                var cachedUser = await _cache.GetStringAsync(cacheKey);
                if (cachedUser != null)
                {
                    return JsonSerializer.Deserialize<User>(cachedUser);
                }
                var user = await _decoratedRepo.GetByIdAsync(id);

                if (user != null)
                {
                    await _cache.SetStringAsync(
                         cacheKey,
                         JsonSerializer.Serialize(user),
                         _cacheOptions
                         );
                }
                return user;
            }
            catch (Exception ex)
            {
                // Fallback to database if Redis/distributed cache is unavailable
                return await _decoratedRepo.GetByIdAsync(id);
            }
        }

        /// <summary>
        /// Finds a user by their email address with caching support.
        /// First attempts to retrieve from cache; falls back to the underlying repository on cache miss or failure.
        /// Automatically caches retrieved users for future lookups.
        /// </summary>
        /// <param name="email">Email address to search for</param>
        /// <returns>User if found with the specified email, null otherwise</returns>
        public async Task<User?> FindByEmailAsync(string email)
        {
            var cacheKey = _keyService.ByEmailKey(email);
            try
            {
                var cached = await _cache.GetStringAsync(cacheKey);
                if (cached != null)
                {
                    return JsonSerializer.Deserialize<User?>(cached);
                }
                var user = await _decoratedRepo.FindByEmailAsync(email);
                if (user != null)
                {
                    await _cache.SetStringAsync(
                        cacheKey,
                        JsonSerializer.Serialize(user),
                        _cacheOptions
                        );
                }
                return user;
            }
            catch (Exception ex)
            {
                // Fallback to database if Redis is unavailable
                return await _decoratedRepo.FindByEmailAsync(email);
            }
        }

        /// <summary>
        /// Retrieves all users from the system with caching support.
        /// First attempts to retrieve from cache; falls back to the underlying repository on cache miss or failure.
        /// Automatically caches the complete user list for future requests.
        /// </summary>
        /// <returns>All users in the system</returns>
        public async Task<IEnumerable<User>> GetAllAsync()
        {
            var cacheKey =_keyService.AllUsersKey();
            try
            {
                var cachedUsers = await _cache.GetStringAsync(cacheKey);
                if (cachedUsers != null)
                {
                    return JsonSerializer.Deserialize<IEnumerable<User>>(cachedUsers)
                          ?? Enumerable.Empty<User>();
                }
                var users = await _decoratedRepo.GetAllAsync();

                await _cache.SetStringAsync(cacheKey,
                    JsonSerializer.Serialize(users),
                    _cacheOptions
                    );
                return users;
            }
            catch (Exception ex)
            {
                // Fallback to database if Redis is unavailable
                return await _decoratedRepo.GetAllAsync();
            }
        }

        /// <summary>
        /// Retrieves all users with the Customer role with caching support.
        /// First attempts to retrieve from cache; falls back to the underlying repository on cache miss or failure.
        /// Automatically caches the customer list for future requests.
        /// </summary>
        /// <returns>All users with Customer role</returns>
        public async Task<IEnumerable<User>> GetAllCustomersAsync()
        {
            var cacheKey = _keyService.AllCustomersKey();
            try
            {
                var cachedCustomers = await _cache.GetStringAsync(cacheKey);
                if (cachedCustomers != null)
                {
                    return JsonSerializer.Deserialize<IEnumerable<User>>(cachedCustomers)
                        ?? Enumerable.Empty<User>();
                }
                var customers = await _decoratedRepo.GetAllCustomersAsync();
                await _cache.SetStringAsync(
                    cacheKey,
                    JsonSerializer.Serialize(customers),
                    _cacheOptions
                    );

                return customers;
            }
            catch (Exception ex)
            {
                // Fallback to database if Redis is unavailable
                return await _decoratedRepo.GetAllCustomersAsync();
            }
        }

        /// <summary>
        /// Updates an existing user in the underlying repository and invalidates related cache entries.
        /// Cache invalidation is performed asynchronously with fire-and-forget semantics.
        /// Failures in cache invalidation do not affect the update operation.
        /// </summary>
        /// <param name="user">User to update</param>
        public void Update(User user)
        {
            _decoratedRepo.Update(user);
            _ = Task.Run(async () =>
            {
                try
                {
                    await InvalidateUserCacheAsync(user);
                }
                catch { } // Update already completed successfully, no need to throw

            });
        }

        /// <summary>
        /// Removes a user from the underlying repository and invalidates related cache entries.
        /// Cache invalidation is performed asynchronously with fire-and-forget semantics.
        /// Failures in cache invalidation do not affect the deletion operation.
        /// </summary>
        /// <param name="user">User to delete</param>
        public void Delete(User user)
        {
            _decoratedRepo.Delete(user);

            _ = Task.Run(async () =>
            {
                try
                {
                    await InvalidateUserCacheAsync(user);
                }
                catch { }  // Deletion already completed successfully, no need to throw
            });
        }

        /// <summary>
        /// Invalidates all cache entries related to a specific user.
        /// Called when a user is added, updated, or deleted to maintain cache consistency.
        /// Includes role-based cache invalidation (e.g., invalidates customer list if user is a customer).
        /// </summary>
        /// <param name="user">User for which to invalidate cache entries</param>
        /// <returns>Task representing the asynchronous operation</returns>
        private async Task InvalidateUserCacheAsync(User user) 
        {
           List<string> keysToInvalidate = _keyService.AllKeys(user);

            if (user.Role == UserRole.Customer)
                keysToInvalidate.Add(_keyService.AllCustomersKey());

            var tasks=keysToInvalidate.Select(key=>_cache.RemoveAsync(key));
            await Task.WhenAll(tasks);
        }

    }
}
