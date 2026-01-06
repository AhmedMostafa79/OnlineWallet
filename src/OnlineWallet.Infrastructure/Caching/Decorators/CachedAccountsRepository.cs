using Microsoft.Extensions.Caching.Distributed;
using OnlineWallet.Domain.Enums;
using OnlineWallet.Domain.Models;
using OnlineWallet.Infrastructure.Caching.Services;
using OnlineWallet.Infrastructure.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace OnlineWallet.Infrastructure.Caching.Decorators
{
    /// <summary>
    /// Decorator implementation of IAccountsRepository that adds distributed caching functionality.
    /// Implements the Decorator pattern to wrap an existing repository with caching capabilities.
    /// </summary>
    public class CachedAccountsRepository : IAccountsRepository
    {
        private readonly IAccountsRepository _decoratedRepo;
        private readonly IDistributedCache _cache;
        private readonly AccountsCacheKeyService _keyService;

        /// <summary>
        /// Default cache entry options for account data.
        /// Uses sliding expiration of 5 minutes and absolute expiration of 1 hour.
        /// </summary>
        private readonly DistributedCacheEntryOptions _cacheOptions = new()
        {
            SlidingExpiration = TimeSpan.FromMinutes(5),
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
        };

        /// <summary>
        /// Initializes a new instance of CachedAccountsRepository.
        /// </summary>
        /// <param name="decoratedRepo">The underlying repository to decorate with caching</param>
        /// <param name="cache">Distributed cache implementation for storing cached data</param>
        /// <param name="keyService">Service for generating cache keys</param>
        public CachedAccountsRepository(IAccountsRepository decoratedRepo, IDistributedCache cache,
         AccountsCacheKeyService keyService)
        {
            _decoratedRepo = decoratedRepo;
            _cache = cache;
            _keyService = keyService;
        }

        /// <summary>
        /// Adds a new account to the underlying repository and invalidates related cache entries.
        /// Cache invalidation is performed with fire-and-forget semantics; failures are silently ignored.
        /// </summary>
        /// <param name="account">Account to add</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task AddAsync(Account account)
        {
            await _decoratedRepo.AddAsync(account);
            try
            {
                await InvalidateAccountAsync(account);
            }
            catch { }
        }

        /// <summary>
        /// Retrieves an account by its ID with caching support.
        /// First attempts to retrieve from cache; falls back to the underlying repository on cache miss or failure.
        /// Automatically caches retrieved accounts for future requests.
        /// </summary>
        /// <param name="id">Account identifier (GUID)</param>
        /// <returns>Account if found, null otherwise</returns>
        public async Task<Account?> GetByIdAsync(Guid id)
        {
            var cacheKey=_keyService.ByAccountIdKey(id);
            try
            {
                var cachedAccount = await _cache.GetStringAsync(cacheKey);
                if (cachedAccount != null) {
                    return JsonSerializer.Deserialize<Account>(cachedAccount);
                }
                var account= await _decoratedRepo.GetByIdAsync(id);
                if (account != null) { 
                    await _cache.SetStringAsync(
                        cacheKey,
                        JsonSerializer.Serialize(account),
                        _cacheOptions
                        );
                }
                return account;
            }
            catch (Exception ex) {
            
                return await _decoratedRepo.GetByIdAsync(id);
            }
        }

        /// <summary>
        /// Retrieves all accounts with caching support.
        /// First attempts to retrieve from cache; falls back to the underlying repository on cache miss or failure.
        /// Automatically caches retrieved account collections for future requests.
        /// </summary>
        /// <returns>All accounts</returns>
        public async Task<IEnumerable<Account>> GetAllAsync()
        {
            var cacheKey= _keyService.AllAccountsKey();
            try
            {
                var cachedAccounts= await _cache.GetStringAsync(cacheKey);
                if (cachedAccounts != null) {
                    return JsonSerializer.Deserialize<IEnumerable<Account>>(cachedAccounts)
                        ?? Enumerable.Empty<Account>();
                }
                var accounts= await _decoratedRepo.GetAllAsync();
            
                await _cache.SetStringAsync(cacheKey,JsonSerializer.Serialize(accounts), _cacheOptions);

                return accounts;
            }
            catch (Exception ex) {
                return await _decoratedRepo.GetAllAsync();
            }
        }

        /// <summary>
        /// Retrieves all accounts belonging to a specific customer with caching support.
        /// First attempts to retrieve from cache; falls back to the underlying repository on cache miss or failure.
        /// Automatically caches retrieved customer account collections for future requests.
        /// </summary>
        /// <param name="customerId">Customer identifier (GUID)</param>
        /// <returns>Customer's accounts</returns>
        public async Task<IEnumerable<Account>> GetByCustomerIdAsync(Guid customerId)
        {
            var cacheKey = _keyService.ByCustomerIdKey(customerId);
            try
            {
                var cachedAccounts=await _cache.GetStringAsync( cacheKey);

                if (cachedAccounts != null) { 
                    return JsonSerializer.Deserialize<IEnumerable<Account>>(cachedAccounts)
                        ?? Enumerable.Empty<Account>();
                }
                var accounts= await _decoratedRepo.GetByCustomerIdAsync(customerId);
                await _cache.SetStringAsync(
                    cacheKey,
                    JsonSerializer.Serialize(accounts),
                    _cacheOptions
                    );
                return accounts;
            }
            catch (Exception ex) {
                return await _decoratedRepo.GetByCustomerIdAsync(customerId);
            }
        }

        /// <summary>
        /// Updates an existing account in the underlying repository and invalidates related cache entries.
        /// Cache invalidation is performed asynchronously with fire-and-forget semantics.
        /// Failures in cache invalidation do not affect the update operation.
        /// </summary>
        /// <param name="account">Account to update</param>
        public void Update(Account account)
        {
            _decoratedRepo.Update(account);

            _ = Task.Run(async () =>
            {
                try
                {
                    await InvalidateAccountAsync(account);
                }
                catch { }  //Update is already done no need to throw
            });
        }

        /// <summary>
        /// Removes an account from the underlying repository and invalidates related cache entries.
        /// Cache invalidation is performed asynchronously with fire-and-forget semantics.
        /// Failures in cache invalidation do not affect the deletion operation.
        /// </summary>
        /// <param name="account">Account to delete</param>
        public void Delete(Account account)
        {
            _decoratedRepo.Delete(account);

            _ = Task.Run(async () =>
            {
                try
                {
                    await InvalidateAccountAsync(account);
                }
                catch { }  //Deletion is already done no need to throw
            });
        }

        /// <summary>
        /// Invalidates all cache entries related to a specific account.
        /// Called when an account is added, updated, or deleted to maintain cache consistency.
        /// </summary>
        /// <param name="account">Account for which to invalidate cache entries</param>
        /// <returns>Task representing the asynchronous operation</returns>
        private async Task InvalidateAccountAsync(Account account)
        {
            List<string> keysToInvalidate = _keyService.AllKeys(account);


            var tasks = keysToInvalidate.Select(key => _cache.RemoveAsync(key));
            await Task.WhenAll(tasks);
        }
    }
}
