using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OnlineWallet.Application.Interfaces;
using OnlineWallet.Domain.Enums;
using OnlineWallet.Domain.Models;
using OnlineWallet.Infrastructure.Interfaces;
using OnlineWallet.Infrastructure.Repositories;

namespace OnlineWallet.Application.Models
{
    /// <summary>
    /// Service implementation for audit log operations.
    /// Handles logging and retrieval of system audit trails.
    /// </summary>
    public class AuditLogService : IAuditLogService
    {
        
        private readonly IUnitOfWork _unitOfWork;

        /// <summary>
        /// Initializes a new instance of AuditLogService.
        /// </summary>
        /// <param name="unitOfWork">Unit of work for data operations</param>
        public AuditLogService(IUnitOfWork unitOfWork)
        {
                _unitOfWork=unitOfWork;
        }

        /// <summary>
        /// Logs a deposit transaction attempt.
        /// </summary>
        /// <param name="initiatedBy">User who initiated the deposit</param>
        /// <param name="toAccount">Destination account identifier</param>
        /// <param name="amount">Deposit amount</param>
        /// <param name="success">Whether the deposit was successful</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task LogDepositAsync(Guid performedBy, Guid toAccount, decimal amount, bool success)
        {
            await _unitOfWork.AuditLogsRepository.AddAsync(new AuditLog(
                id: Guid.NewGuid(),
                performedBy: performedBy,
                actionType: AuditLogActionType.Deposit,
                status: success ? AuditLogStatus.Success : AuditLogStatus.Failed,
                details: success
                    ? $"{performedBy} Successfully deposited {amount:C} to account {toAccount}"
                    : $"{performedBy} failed to deposit {amount:C} to account {toAccount}",
                createdAt: DateTime.UtcNow
            ));
            await _unitOfWork.SaveChangesAsync();
        }

        /// <summary>
        /// Logs a transfer transaction attempt.
        /// </summary>
        /// <param name="fromAccount">Source account identifier</param>
        /// <param name="toAccount">Destination account identifier</param>
        /// <param name="amount">Transfer amount</param>
        /// <param name="success">Whether the transfer was successful</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task LogTransferAsync(Guid performedBy,Guid fromAccount, Guid toAccount, decimal amount, bool success)
        {
            await _unitOfWork.AuditLogsRepository.AddAsync(new AuditLog(
                id: Guid.NewGuid(),
                performedBy: performedBy,
                actionType: AuditLogActionType.Transfer,
                status: success ? AuditLogStatus.Success : AuditLogStatus.Failed,
                details: success
                    ? $"Transferred {amount:C} from account {fromAccount} to {toAccount}"
                    : $"Failed to transfer {amount:C} from account {fromAccount} to {toAccount}",
                createdAt: DateTime.UtcNow
            ));
            await _unitOfWork.SaveChangesAsync();
        }
        /// <summary>
        /// Logs a generic system action.
        /// </summary>
        /// <param name="auditLog">Audit log entry to record</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task LogActionAsync(AuditLog auditLog,bool saveChanges)
        {
            await _unitOfWork.AuditLogsRepository.AddAsync(auditLog);
            if (saveChanges)
                await _unitOfWork.SaveChangesAsync();
        }

        /// <summary>
        /// Retrieves audit logs for a specific user.
        /// </summary>
        /// <param name="userId">User identifier</param>
        /// <returns>Collection of user's audit logs</returns>
        public async Task<IEnumerable<AuditLog>> GetUserAuditLogsAsync(Guid userId)
        {
            return await _unitOfWork.AuditLogsRepository.GetByUserAsync(userId);
        }

        /// <summary>
        /// Retrieves all audit log entries.
        /// </summary>
        /// <returns>Collection of all audit logs</returns>
        public async Task<IEnumerable<AuditLog>> GetAllAuditLogsAsync()
        {
            return await _unitOfWork.AuditLogsRepository.GetAllAsync();
        }

        /// <summary>
        /// Retrieves audit logs of a specific action type.
        /// </summary>
        /// <param name="actionType">Action type to filter by</param>
        /// <returns>Collection of audit logs matching action type</returns>
        public async Task<IEnumerable<AuditLog>> GetByActionTypeAsync(AuditLogActionType actionType)
        {
            return await _unitOfWork.AuditLogsRepository.GetByActionTypeAsync(actionType);
        }

        /// <summary>
        /// Retrieves audit logs within a time range.
        /// </summary>
        /// <param name="beginDate">Start of time range</param>
        /// <param name="endDate">End of time range</param>
        /// <returns>Collection of audit logs within time range</returns>
        public async Task<IEnumerable<AuditLog>> GetByTimeStampAsync(DateTime beginDate, DateTime endDate)
        {
            return await _unitOfWork.AuditLogsRepository.GetByTimeStampAsync(beginDate, endDate);
        }

        /// <summary>
        /// Retrieves transaction history for a user.
        /// Filters by transaction action types (e.g. deposit and transfer).
        /// </summary>
        /// <param name="userId">User identifier</param>
        /// <returns>Collection of user's transaction audit logs</returns>
        public async Task<IEnumerable<AuditLog>> GetTransactionHistoryAsync(Guid customerId)
        {
            IEnumerable<AuditLogActionType> transactionTypes =
                [
                     AuditLogActionType.Deposit,
                 AuditLogActionType.Transfer
                ];
            return await _unitOfWork.AuditLogsRepository.GetTransactionHistoryAsync(customerId, transactionTypes);
        }
    }
}