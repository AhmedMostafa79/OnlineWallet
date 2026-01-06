using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OnlineWallet.Infrastructure.Repositories;
using OnlineWallet.Domain.Models;
using OnlineWallet.Domain.Enums;

namespace OnlineWallet.Application.Interfaces
{
    /// <summary>
    /// Service interface for audit log operations.
    /// Handles logging and retrieval of system audit trails.
    /// </summary>
    public interface IAuditLogService
    {
        /// <summary>
        /// Logs a deposit transaction attempt.
        /// </summary>
        /// <param name="performedBy">Admin who performed the deposit</param>
        /// <param name="toAccount">Destination account identifier</param>
        /// <param name="amount">Deposit amount</param>
        /// <param name="success">Whether the deposit was successful</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public Task LogDepositAsync(Guid performedBy, Guid toAccount, decimal amount, bool success);

        /// <summary>
        /// Logs a transfer transaction attempt.
        /// </summary>
        /// <param name="fromAccount">Source account identifier</param>
        /// <param name="toAccount">Destination account identifier</param>
        /// <param name="amount">Transfer amount</param>
        /// <param name="success">Whether the transfer was successful</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public Task LogTransferAsync(Guid performedBy, Guid fromAccount, Guid toAccount, decimal amount, bool success);

        /// <summary>
        /// Logs a generic system action.
        /// </summary>
        /// <param name="auditLog">Audit log entry to record</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public Task LogActionAsync(AuditLog auditLog, bool saveChanges);

        /// <summary>
        /// Retrieves audit logs for a specific user.
        /// </summary>
        /// <param name="userId">User identifier</param>
        /// <returns>Collection of user's audit logs</returns>
        public Task<IEnumerable<AuditLog>> GetUserAuditLogsAsync(Guid userId);

        /// <summary>
        /// Retrieves all audit log entries.
        /// </summary>
        /// <returns>Collection of all audit logs</returns>
        public Task<IEnumerable<AuditLog>> GetAllAuditLogsAsync();

        /// <summary>
        /// Retrieves audit logs of a specific action type.
        /// </summary>
        /// <param name="actionType">Action type to filter by</param>
        /// <returns>Collection of audit logs matching action type</returns>
        public Task<IEnumerable<AuditLog>> GetByActionTypeAsync(AuditLogActionType actionType);

        /// <summary>
        /// Retrieves audit logs within a time range.
        /// </summary>
        /// <param name="beginDate">Start of time range</param>
        /// <param name="endDate">End of time range</param>
        /// <returns>Collection of audit logs within time range</returns>
        public Task<IEnumerable<AuditLog>> GetByTimeStampAsync(DateTime beginDate,DateTime endDate);

        /// <summary>
        /// Retrieves transaction history for a user.
        /// </summary>
        /// <param name="userId">User identifier</param>
        /// <returns>Collection of user's transaction audit logs</returns>
        public Task<IEnumerable<AuditLog>> GetTransactionHistoryAsync(Guid customerId);
    }
}
