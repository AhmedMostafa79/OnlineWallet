using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore;
using OnlineWallet.Infrastructure.Interfaces;
using System.Data;
namespace OnlineWallet.Infrastructure.Repositories
{
    /// <summary>
    /// Coordinates work across multiple repositories.
    /// Manages database transactions and ensures data consistency.
    /// </summary>
    public class UnitOfWork:IUnitOfWork
    {
        private readonly WalletDbContext _dbContext;
        private IDbContextTransaction _transaction;

        private IAccountsRepository _accountsRepository;
        private IUsersRepository _usersRepository;
        private IAuditLogsRepository _auditLogsRepository;

        /// <summary>
        /// Initializes a new instance of UnitOfWork.
        /// </summary>
        /// <param name="dbContext">Database context for data operations</param>
        public UnitOfWork(WalletDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// Repository for account data operations.
        /// Uses lazy initialization for repository instances.
        /// </summary>
        public IAccountsRepository AccountsRepository =>
            _accountsRepository ??= new AccountsRepository(_dbContext);

        /// <summary>
        /// Repository for user data operations.
        /// Uses lazy initialization for repository instances.
        /// </summary>
        public IUsersRepository UsersRepository =>
            _usersRepository ??= new UsersRepository(_dbContext);

        /// <summary>
        /// Repository for audit log data operations.
        /// Uses lazy initialization for repository instances.
        /// </summary>
        public IAuditLogsRepository AuditLogsRepository =>
            _auditLogsRepository ??= new AuditLogsRepository(_dbContext);

        /// <summary>
        /// Begins a new database transaction with specified isolation level.
        /// </summary>
        /// <param name="isolationLevel">Transaction isolation level</param>
        /// <exception cref="InvalidOperationException">Thrown when a transaction is already in progress</exception>
        public async Task BeginTransactionAsync(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
            if (_transaction != null)
            {
                throw new InvalidOperationException("A transaction is already in progress.");
            }
            _transaction = await _dbContext.Database.BeginTransactionAsync(isolationLevel);
        }

        /// <summary>
        /// Commits the current transaction.
        /// Saves all changes before committing.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when no transaction exists</exception>
        public async Task CommitTransactionAsync()
        {
            if (_transaction == null)
            {
                throw new InvalidOperationException("No transaction to commit.");
            }
            await _dbContext.SaveChangesAsync();
            await _transaction.CommitAsync();
            await DisposeTransactionAsync();
        }

        /// <summary>
        /// Rolls back the current transaction.
        /// </summary>
        public async Task RollbackTransactionAsync()
        {
            if (_transaction == null)
            {
                return;
            }
            await _transaction.RollbackAsync();
            await DisposeTransactionAsync();
        }

        /// <summary>
        /// Saves all changes made in the unit of work.
        /// </summary>
        public async Task SaveChangesAsync()
        {
            await _dbContext.SaveChangesAsync();
        }

        /// <summary>
        /// Detaches all tracked entities from the change tracker.
        /// Useful when you need to discard failed changes before saving audit logs.
        /// </summary>
        public void DetachAllEntities()
        {
            var entries = _dbContext.ChangeTracker.Entries()
                .Where(e => e.State != EntityState.Detached)
                .ToList();

            foreach (var entry in entries)
            {
                entry.State = EntityState.Detached;
            }
        }

        /// <summary>
        /// Disposes the current transaction if one exists.
        /// </summary>
        private async Task DisposeTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        /// <summary>
        /// Releases all resources used by the unit of work.
        /// Disposes database context and clears repository references.
        /// </summary>
        public void Dispose()
        {
            DisposeTransactionAsync().GetAwaiter().GetResult();
            _dbContext?.Dispose();

            _accountsRepository = null;
            _usersRepository = null;
            _auditLogsRepository = null;
        }

    }
}
