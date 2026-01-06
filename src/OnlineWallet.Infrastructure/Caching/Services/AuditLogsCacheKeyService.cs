using OnlineWallet.Domain.Enums;
using OnlineWallet.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlineWallet.Infrastructure.Caching.Services
{
    /// <summary>
    /// Service responsible for generating and managing cache keys for AuditLog entities.
    /// Provides consistent cache key naming conventions for various audit log query patterns
    /// to support efficient caching of audit trail data.
    /// </summary>
    public class AuditLogsCacheKeyService
    {
        /// <summary>
        /// Generates a cache key for retrieving all audit log entries in the system.
        /// </summary>
        /// <returns>
        /// Cache key in the format: "auditLogs:all"
        /// Used for caching the complete collection of all audit log entries.
        /// </returns>
        public string AllAuditLogsKey() => $"auditLogs:all";

        /// <summary>
        /// Generates a cache key for retrieving audit log entries filtered by action type.
        /// </summary>
        /// <param name="actionType">The type of audit log action to filter by</param>
        /// <returns>
        /// Cache key in the format: "auditLogs:actionType:{actionType}"
        /// Example: "auditLogs:actionType:AccountCreation"
        /// </returns>
        public string ByActionTypeKey(AuditLogActionType actionType) => $"auditLogs:actionType:{actionType}";

        /// <summary>
        /// Generates a cache key for retrieving transaction history for a specific user.
        /// </summary>
        /// <param name="userId">The unique identifier of the user (GUID)</param>
        /// <returns>
        /// Cache key in the format: "auditLogs:transactionHistory:{userId}"
        /// Example: "auditLogs:transactionHistory:12345678-1234-1234-1234-123456789012"
        /// </returns>
        public string TransactionHistoryByUserIdKey(Guid userId) => $"auditLogs:transactionHistory:{userId}";

        /// <summary>
        /// Generates a cache key for retrieving audit log entries within a specific time range.
        /// </summary>
        /// <param name="begin">The start of the time range (inclusive)</param>
        /// <param name="end">The end of the time range (inclusive)</param>
        /// <returns>
        /// Cache key in the format: "auditLogs:timeStamp:from{begin}to{end}"
        /// Example: "auditLogs:timeStamp:from2024-01-01T00:00:00to2024-01-31T23:59:59"
        /// </returns>
        public string ByTimeStampKey(DateTime begin, DateTime end) => $"auditLogs:timeStamp:from{begin}to{end}";

        /// <summary>
        /// Generates a cache key for retrieving all audit log entries performed by a specific user.
        /// </summary>
        /// <param name="userId">The unique identifier of the user (GUID)</param>
        /// <returns>
        /// Cache key in the format: "auditLogs:user:{userId}"
        /// Example: "auditLogs:user:12345678-1234-1234-1234-123456789012"
        /// </returns>
        public string ByUserIdKey(Guid userId) => $"auditLogs:user:{userId}";
    }
}
