using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OnlineWallet.Infrastructure.Interfaces;
using OnlineWallet.Domain.Models;
using OnlineWallet.Domain.Enums;
namespace OnlineWallet.Infrastructure.Repositories
{
    /// <summary>
    /// Entity Framework implementation of IAuditLogsRepository.
    /// Handles database operations for AuditLog entities.
    /// </summary>
    public class AuditLogsRepository:IAuditLogsRepository
    {
        private readonly WalletDbContext _dbContext;

        /// <summary>
        /// Initializes a new instance of AuditLogsRepository.
        /// </summary>
        /// <param name="dbContext">Database context for data access</param>
        public AuditLogsRepository(WalletDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// Adds a new audit log entry to the database.
        /// </summary>
        /// <param name="auditLog">Audit log to add</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task AddAsync(AuditLog auditLog)
        {
            await _dbContext.AuditLogs.AddAsync(auditLog);
        }

        /// <summary>
        /// Retrieves all audit log entries from the database.
        /// Uses AsNoTracking for read-only performance optimization.
        /// Results are ordered by creation time descending (newest first).
        /// </summary>
        /// <returns>All audit logs</returns>
        public async Task<IEnumerable<AuditLog>> GetAllAsync()
        {
            return await _dbContext.AuditLogs
                .OrderByDescending(a => a.CreatedAt)
                .AsNoTracking()
                .ToListAsync();
        }

        /// <summary>
        /// Retrieves audit logs for a specific user.
        /// Uses AsNoTracking for read-only performance optimization.
        /// Results are ordered by creation time descending (newest first).
        /// </summary>
        /// <param name="userId">User identifier</param>
        /// <returns>User's audit logs</returns>
        public async Task<IEnumerable<AuditLog>> GetByUserAsync(Guid userId)
        {
            return await _dbContext.AuditLogs.
                Where(a => a.PerformedBy == userId)
                .OrderByDescending(a => a.CreatedAt)
                .AsNoTracking()
                .ToListAsync() ;
        }

        /// <summary>
        /// Retrieves audit logs of a specific action type.
        /// Uses AsNoTracking for read-only performance optimization.
        /// Results are ordered by creation time descending (newest first).
        /// </summary>
        /// <param name="actionType">Type of action to filter by</param>
        /// <returns>Audit logs matching the action type</returns>
        public async Task<IEnumerable<AuditLog>> GetByActionTypeAsync(AuditLogActionType actionType)
        {
            return await _dbContext.AuditLogs.
                Where(a=>a.ActionType == actionType)
                .OrderByDescending(a => a.CreatedAt)
                .AsNoTracking()
                .ToListAsync() ;
        }

        /// <summary>
        /// Retrieves audit logs within a time range.
        /// Results are ordered by creation time descending.
        /// Uses AsNoTracking for read-only performance optimization.
        /// </summary>
        /// <param name="begin">Start of time range</param>
        /// <param name="end">End of time range</param>
        /// <returns>Audit logs within the specified time period</returns>
        public async Task<IEnumerable<AuditLog>> GetByTimeStampAsync(DateTime begin, DateTime end)
        {
            return await _dbContext.AuditLogs.
                Where(a => begin <= a.CreatedAt && a.CreatedAt <= end)
                .OrderByDescending(a=>a.CreatedAt)
                .AsNoTracking()
                .ToListAsync();
        }

        /// <summary>
        /// Retrieves transaction history for a user.
        /// Filters by specified transaction action types.
        /// Uses AsNoTracking for read-only performance optimization.
        /// Results are ordered by creation time descending (newest first).
        /// </summary>
        /// <param name="userId">User identifier</param>
        /// <param name="transactionTypes">Transaction types to include</param>
        /// <returns>User's transaction audit logs</returns>
        public async Task<IEnumerable<AuditLog>> GetTransactionHistoryAsync(Guid userId, IEnumerable<AuditLogActionType> transactionTypes)
        {
          

            return await _dbContext.AuditLogs
                .Where(a => transactionTypes.Contains(a.ActionType) && a.PerformedBy == userId)
                .OrderByDescending(a => a.CreatedAt)
                .AsNoTracking()
                .ToListAsync() ;
        }
    }
}
