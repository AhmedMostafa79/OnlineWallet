using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OnlineWallet.Domain.Enums;
using OnlineWallet.Domain.Models;
namespace OnlineWallet.Infrastructure.Interfaces
{
        /// <summary>
        /// Repository interface for audit log data operations.
        /// Provides specialized queries for audit trail analysis.
        /// </summary>
    public interface IAuditLogsRepository
    {
        /// <summary>
        /// Adds a new audit log entry.
        /// </summary>
        /// <param name="auditLog">Audit log to add</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public Task AddAsync(AuditLog auditLog);

        /// <summary>
        /// Retrieves all audit log entries.
        /// </summary>
        /// <returns>All audit logs</returns>
        public Task<IEnumerable<AuditLog>> GetAllAsync();

        /// <summary>
        /// Retrieves audit logs for a specific user.
        /// </summary>
        /// <param name="id">User identifier</param>
        /// <returns>User's audit logs</returns>
        public Task<IEnumerable<AuditLog>> GetByUserAsync(Guid id);

        /// <summary>
        /// Retrieves audit logs of a specific action type.
        /// </summary>
        /// <param name="actionType">Type of action to filter by</param>
        /// <returns>Audit logs matching the action type</returns>
        public Task<IEnumerable<AuditLog>> GetByActionTypeAsync(AuditLogActionType actionType);

        /// <summary>
        /// Retrieves audit logs within a time range.
        /// </summary>
        /// <param name="begin">Start of time range</param>
        /// <param name="end">End of time range</param>
        /// <returns>Audit logs within the specified time period</returns>
        public Task<IEnumerable<AuditLog>> GetByTimeStampAsync(DateTime begin, DateTime end);
        
        /// <summary>
        /// Retrieves transaction history for a user.
        /// </summary>
        /// <param name="userId">User identifier</param>
        /// <returns>User's transaction audit logs</returns>
        public Task<IEnumerable<AuditLog>> GetTransactionHistoryAsync(Guid userId,IEnumerable<AuditLogActionType> transactionTypes);


    }
}
