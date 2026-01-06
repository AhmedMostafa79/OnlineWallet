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
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace OnlineWallet.Infrastructure.Caching.Decorators
{
    /// <summary>
    /// Decorator implementation of IAuditLogsRepository that adds distributed caching functionality.
    /// Implements the Decorator pattern to wrap an existing audit logs repository with caching capabilities.
    /// Optimized for audit log read scenarios with intelligent cache invalidation.
    /// </summary>
    public class CachedAuditLogsRepository : IAuditLogsRepository
    {
        private readonly IAuditLogsRepository _decoratedRepo;
        private readonly IDistributedCache _cache;
        private readonly AuditLogsCacheKeyService _keyService;

        /// <summary>
        /// Default cache entry options for audit log data.
        /// Uses sliding expiration of 10 minutes and absolute expiration of 1 hour.
        /// Longer sliding expiration compared to accounts due to audit log read patterns.
        /// </summary>
        private readonly DistributedCacheEntryOptions _cacheOptions = new()
        {
            SlidingExpiration = TimeSpan.FromMinutes(10),
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
        };

        /// <summary>
        /// Initializes a new instance of CachedAuditLogsRepository.
        /// </summary>
        /// <param name="decoratedRepo">The underlying audit logs repository to decorate with caching</param>
        /// <param name="cache">Distributed cache implementation for storing cached audit log data</param>
        /// <param name="keyService">Service for generating audit log-specific cache keys</param>
        public CachedAuditLogsRepository(IAuditLogsRepository decoratedRepo, IDistributedCache cache, AuditLogsCacheKeyService keyService)
        {
            _decoratedRepo = decoratedRepo;
            _cache = cache;
            _keyService = keyService;
        }

        /// <summary>
        /// Adds a new audit log entry to the underlying repository and invalidates related cache entries.
        /// Cache invalidation is performed with fire-and-forget semantics; failures are silently ignored.
        /// </summary>
        /// <param name="auditLog">Audit log entry to add</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task AddAsync(AuditLog auditLog)
        {
           await _decoratedRepo.AddAsync(auditLog);
            try
            {
                await InvalidateAuditLogsAsync(auditLog);
            }
            catch { }
        }

        /// <summary>
        /// Retrieves all audit log entries with caching support.
        /// First attempts to retrieve from cache; falls back to the underlying repository on cache miss or failure.
        /// Optimized to avoid duplicate database calls on cache failure scenarios.
        /// Results are ordered by creation time descending (newest first).
        /// </summary>
        /// <returns>All audit log entries</returns>
        public async Task<IEnumerable<AuditLog>> GetAllAsync()
        {
            var cacheKey = _keyService.AllAuditLogsKey();
            IEnumerable<AuditLog> auditLogs=null;
            try
            {
                var cachedAuditLogs = await _cache.GetStringAsync(cacheKey);
                if (cachedAuditLogs != null) {
                    return JsonSerializer.Deserialize<IEnumerable<AuditLog>>(cachedAuditLogs)
                        ??Enumerable.Empty<AuditLog>();
                }
                 auditLogs= await _decoratedRepo.GetAllAsync();
                await _cache.SetStringAsync(
                    cacheKey,
                    JsonSerializer.Serialize(auditLogs),
                    _cacheOptions
                    );
                return auditLogs;
            }
            catch(Exception ex){
                // Performance optimization: Avoid duplicate database call if data was already retrieved
                if (auditLogs==null)
                    await _decoratedRepo.GetAllAsync();
                return auditLogs;
            }
        }

        /// <summary>
        /// Retrieves audit log entries filtered by action type with caching support.
        /// First attempts to retrieve from cache; falls back to the underlying repository on cache miss or failure.
        /// Optimized to avoid duplicate database calls on cache failure scenarios.
        /// Results are ordered by creation time descending (newest first).
        /// </summary>
        /// <param name="actionType">Type of audit log action to filter by</param>
        /// <returns>Audit log entries matching the specified action type</returns>
        public async Task<IEnumerable<AuditLog>> GetByActionTypeAsync(AuditLogActionType actionType)
        {
            var cacheKey = _keyService.ByActionTypeKey(actionType);
            IEnumerable<AuditLog> auditLogs = null;
            try
            {
                var cachedAuditLogs = await _cache.GetStringAsync(cacheKey);
                if (cachedAuditLogs != null)
                {
                    return JsonSerializer.Deserialize<IEnumerable<AuditLog>>(cachedAuditLogs)
                        ?? Enumerable.Empty<AuditLog>();
                }
                auditLogs = await _decoratedRepo.GetByActionTypeAsync(actionType);
                await _cache.SetStringAsync(
                    cacheKey,
                    JsonSerializer.Serialize(auditLogs),
                    _cacheOptions
                    );
                return auditLogs;
            }
            catch (Exception ex)
            {
                if (auditLogs == null)
                    await _decoratedRepo.GetByActionTypeAsync(actionType);
                return auditLogs;
            }
        }

        /// <summary>
        /// Retrieves audit log entries filtered by timestamp range with caching support.
        /// First attempts to retrieve from cache; falls back to the underlying repository on cache miss or failure.
        /// Optimized to avoid duplicate database calls on cache failure scenarios.
        /// Results are ordered by creation time descending (newest first).
        /// </summary>
        /// <param name="begin">Start of the timestamp range (inclusive)</param>
        /// <param name="end">End of the timestamp range (inclusive)</param>
        /// <returns>Audit log entries within the specified timestamp range</returns>
        public async Task<IEnumerable<AuditLog>> GetByTimeStampAsync(DateTime begin, DateTime end)
        {
            var cacheKey = _keyService.ByTimeStampKey(begin,end);
            IEnumerable<AuditLog> auditLogs = null;
            try
            {
                var cachedAuditLogs = await _cache.GetStringAsync(cacheKey);
                if (cachedAuditLogs != null)
                {
                    return JsonSerializer.Deserialize<IEnumerable<AuditLog>>(cachedAuditLogs)
                        ?? Enumerable.Empty<AuditLog>();
                }
                auditLogs = await _decoratedRepo.GetByTimeStampAsync(begin,end);
                await _cache.SetStringAsync(
                    cacheKey,
                    JsonSerializer.Serialize(auditLogs),
                    _cacheOptions
                    );
                return auditLogs;
            }
            catch (Exception ex)
            {
                if (auditLogs == null)
                    await   _decoratedRepo.GetByTimeStampAsync(begin, end);
                return auditLogs;
            }
        }

        /// <summary>
        /// Retrieves audit log entries filtered by user ID with caching support.
        /// First attempts to retrieve from cache; falls back to the underlying repository on cache miss or failure.
        /// Optimized to avoid duplicate database calls on cache failure scenarios.
        /// Results are ordered by creation time descending (newest first).
        /// </summary>
        /// <param name="id">User identifier (GUID) to filter audit logs by</param>
        /// <returns>Audit log entries performed by the specified user</returns>
        public async Task<IEnumerable<AuditLog>> GetByUserAsync(Guid id)
        {
            var cacheKey = _keyService.ByUserIdKey(id);
            IEnumerable<AuditLog> auditLogs = null;
            try
            {
                var cachedAuditLogs = await _cache.GetStringAsync(cacheKey);
                if (cachedAuditLogs != null)
                {
                    return JsonSerializer.Deserialize<IEnumerable<AuditLog>>(cachedAuditLogs)
                        ?? Enumerable.Empty<AuditLog>();
                }
                auditLogs = await _decoratedRepo.GetByUserAsync(id);
                await _cache.SetStringAsync(
                    cacheKey,
                    JsonSerializer.Serialize(auditLogs),
                    _cacheOptions
                    );
                return auditLogs;
            }
            catch (Exception ex)
            {
                //for better performance
                if (auditLogs == null)
                    await _decoratedRepo.GetByUserAsync(id);
                return auditLogs;
            }
        }

        /// <summary>
        /// Retrieves transaction history for a user filtered by specific transaction types with caching support.
        /// First attempts to retrieve from cache; falls back to the underlying repository on cache miss or failure.
        /// Optimized to avoid duplicate database calls on cache failure scenarios.
        /// Results are ordered by creation time descending (newest first).
        /// </summary>
        /// <param name="userId">User identifier (GUID) to retrieve transaction history for</param>
        /// <param name="transactionTypes">Collection of transaction action types to filter by</param>
        /// <returns>Transaction audit log entries for the specified user and transaction types</returns>
        public async Task<IEnumerable<AuditLog>> GetTransactionHistoryAsync(Guid userId, IEnumerable<AuditLogActionType> transactionTypes)
        {
            var cacheKey = _keyService.TransactionHistoryByUserIdKey(userId);
            IEnumerable<AuditLog> auditLogs = null;
            try
            {
                var cachedAuditLogs = await _cache.GetStringAsync(cacheKey);
                if (cachedAuditLogs != null)
                {
                    return JsonSerializer.Deserialize<IEnumerable<AuditLog>>(cachedAuditLogs)
                        ?? Enumerable.Empty<AuditLog>();
                }
                auditLogs = await _decoratedRepo.GetTransactionHistoryAsync(userId,transactionTypes);
                await _cache.SetStringAsync(
                    cacheKey,
                    JsonSerializer.Serialize(auditLogs),
                    _cacheOptions
                    );
                return auditLogs;
            }
            catch (Exception ex)
            {
                //for better performance
                if (auditLogs == null)
                    await _decoratedRepo.GetTransactionHistoryAsync(userId, transactionTypes);
                return auditLogs;
            }
        }

        /// <summary>
        /// Invalidates all cache entries related to a specific audit log entry.
        /// Called when an audit log is added to maintain cache consistency across multiple query dimensions.
        /// Invalidates: all logs, action type filters, user-specific logs, and user transaction history.
        /// </summary>
        /// <param name="auditLog">Audit log entry for which to invalidate cache entries</param>
        /// <returns>Task representing the asynchronous operation</returns>
        private async Task InvalidateAuditLogsAsync(AuditLog auditLog)
        {
            List<string> keysToInvalidate = new List<string> {
        _keyService.AllAuditLogsKey(),
        _keyService.ByActionTypeKey(auditLog.ActionType)
            };

            if (auditLog.PerformedBy.HasValue)
            {
                keysToInvalidate.Add(_keyService.TransactionHistoryByUserIdKey(auditLog.PerformedBy.Value));
                keysToInvalidate.Add(_keyService.ByUserIdKey(auditLog.PerformedBy.Value));
            }

            var tasks = keysToInvalidate.Select(key => _cache.RemoveAsync(key));
            await Task.WhenAll(tasks);
        }
    }
}
