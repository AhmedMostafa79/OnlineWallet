using OnlineWallet.Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlineWallet.Infrastructure.Interfaces
{
    /// <summary>
    /// Coordinates work across multiple repositories.
    /// Ensures data consistency through transaction management.
    /// </summary>
    public interface IUnitOfWork
    {
        /// <summary>
        /// Repository for account data operations.
        /// </summary>
        IAccountsRepository AccountsRepository { get; }

        /// <summary>
        /// Repository for user data operations.
        /// </summary>
        IUsersRepository UsersRepository { get; }

        /// <summary>
        /// Repository for audit log data operations.
        /// </summary>
        IAuditLogsRepository AuditLogsRepository { get; }

        /// <summary>
        /// Begins a new database transaction.
        /// </summary>
        /// <param name="isolationLevel">Transaction isolation level (default: ReadCommitted)</param>
        /// <returns>Task representing the asynchronous operation</returns>
        Task BeginTransactionAsync(IsolationLevel isolationLevel=IsolationLevel.ReadCommitted);

        /// <summary>
        /// Commits the current transaction.
        /// </summary>
        /// <returns>Task representing the asynchronous operation</returns>
        Task CommitTransactionAsync();

        /// <summary>
        /// Rolls back the current transaction.
        /// </summary>
        /// <returns>Task representing the asynchronous operation</returns>
        Task RollbackTransactionAsync();

        /// <summary>
        /// Saves all changes made in the unit of work.
        /// </summary>
        /// <returns>Task representing the asynchronous operation</returns>
        Task SaveChangesAsync();

        /// <summary>
        /// Detaches all tracked entities from the change tracker.
        /// Useful when you need to discard failed changes before saving audit logs.
        /// </summary>
        void DetachAllEntities();

        /// <summary>
        /// Releases resources used by the unit of work.
        /// </summary>
        public void Dispose();

    }

}
