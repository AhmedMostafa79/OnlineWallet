using OnlineWallet.Application.DTOs;
using OnlineWallet.Domain.Models;
using OnlineWallet.Application.Interfaces;
using OnlineWallet.Infrastructure.Interfaces;
using OnlineWallet.Infrastructure.Repositories;
using OnlineWallet.Infrastructure;
using OnlineWallet.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.Security.Principal;
using OnlineWallet.Application.Helpers;
using System.Data;

namespace OnlineWallet.Application.Models
{
    /// <summary>
    /// Service implementation for account operations.
    /// Handles business logic for account management and transactions.
    /// </summary>
    public class AccountService:IAccountService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditLogService _auditLogService;

        /// <summary>
        /// Initializes a new instance of AccountService.
        /// </summary>
        /// <param name="unitOfWork">Unit of work for data operations</param>
        /// <param name="auditLogService">Service for audit logging</param>
        public AccountService(IUnitOfWork unitOfWork, IAuditLogService auditLogService)
        {
            _unitOfWork = unitOfWork;
            _auditLogService = auditLogService;
        }

        /// <summary>
        /// Creates a new financial account for a customer.
        /// </summary>
        /// <param name="newAccount">Account creation details including owner ID</param>
        /// <param name="saveChanges">Whether to immediately persist changes to allow transaction operations</param>
        /// <returns>DTO containing created account details</returns>
        /// <exception cref="Exception">Wrapped exception when database operation fails</exception>
        /// <remarks>
        /// Business exceptions bubble up through ExceptionHelper.IsBusinessException filter.
        /// System exceptions are wrapped with context information.
        /// Always creates audit log for success or failure.
        /// </remarks>
        public async Task<GetAccountDto> CreateAccountAsync(CreateAccountDto newAccount, bool saveChanges)
        {
            Account account = null;
            var successOperation = false;
            try
            {
              account = new Account(
                       accountNumber: Guid.NewGuid(),
                       isActive: true,
                       ownerId: newAccount.OwnerId,
                       createdAt: newAccount.CreatedAt
                    );
                await _unitOfWork.AccountsRepository.AddAsync(account);

                if (saveChanges)
                    await _unitOfWork.SaveChangesAsync();

                successOperation = true;
                return new GetAccountDto(
                            accountNumber: account.AccountNumber,
                            isActive: account.IsActive,
                            ownerId: account.OwnerId,
                            balance: account.Balance,
                            dateOpened: account.CreatedAt
                        );
            }
            catch(Exception ex) when (ExceptionHelper.IsBusinessException(ex))
            {
                throw;
            }
            catch (Exception ex) {
                throw new Exception($"Failed to create account for customer with ID {newAccount.OwnerId}",ex);
            }
            finally
            {
                if (!successOperation)
                {
                    _unitOfWork.DetachAllEntities();
                }
                await _auditLogService.LogActionAsync(
                    new AuditLog(
                     id: Guid.NewGuid(),
                     performedBy: newAccount.OwnerId,
                     actionType: AuditLogActionType.CustomerAccountCreation,
                    createdAt: DateTime.UtcNow,
                   details: successOperation ?
                   $"Customer with ID {newAccount.OwnerId} successfully created account with number {account.AccountNumber}" :
                   $"Customer with ID {newAccount.OwnerId} failed to create account",
                     status: successOperation ? AuditLogStatus.Success : AuditLogStatus.Failed
                     )
                , saveChanges: true);
            }
        }

        /// <summary>
        /// Retrieves account details by account identifier.
        /// </summary>
        /// <param name="accountNumber">Unique account identifier</param>
        /// <returns>DTO containing account details</returns>
        /// <exception cref="KeyNotFoundException">Thrown when account does not exist</exception>
        /// <remarks>
        /// Business exception bubbles up to caller for handling.
        /// </remarks>
        public async Task<GetAccountDto> GetAccountByIdAsync(Guid accountNumber)
        {
                var account= await _unitOfWork.AccountsRepository.GetByIdAsync(accountNumber);
            if (account == null)
            {
                throw new KeyNotFoundException($"Account with number {accountNumber} not found");
            }

                return new GetAccountDto(
                       accountNumber: account.AccountNumber,
                       isActive: account.IsActive,
                       ownerId: account.OwnerId,
                       balance: account.Balance,
                       dateOpened: account.CreatedAt
                   );
        }

        /// <summary>
        /// Retrieves the current balance of an account.
        /// </summary>
        /// <param name="accountNumber">Unique account identifier</param>
        /// <returns>Current account balance</returns>
        /// <exception cref="KeyNotFoundException">Thrown when account does not exist</exception>
        /// <remarks>
        /// Business exception bubbles up to caller for handling.
        /// </remarks>
        public async Task<decimal> GetAccountBalanceAsync(Guid accountNumber)
        {

            var account = await _unitOfWork.AccountsRepository.GetByIdAsync(accountNumber);
            if (account == null)
            {
                throw new KeyNotFoundException($"Account with number {accountNumber} not found");
            }

                return account.Balance;
        }

        /// <summary>
        /// Retrieves all accounts in the system.
        /// </summary>
        /// <returns>Collection of account DTOs</returns>
        public async Task<IEnumerable<GetAccountDto>> GetAllAccountsAsync()
        {
                var allAccounts =await _unitOfWork.AccountsRepository.GetAllAsync();

                return allAccounts.Select(account => new GetAccountDto(
                     accountNumber: account.AccountNumber,
                       isActive: account.IsActive,
                       ownerId: account.OwnerId,
                       balance: account.Balance,
                       dateOpened: account.CreatedAt
                    ));
        }
        public async Task<bool> IsAccountOwner(Guid accountId, Guid customerId)
        {
            try
            {
                var account = await GetAccountByIdAsync(accountId);

                return account.OwnerId == customerId;
            }
            catch (Exception ex) when (ExceptionHelper.IsBusinessException(ex))
            {
                throw;
            }
            catch(Exception ex)
            {
                throw new Exception("An error occurred while checking account owner", ex);
            }
        }


        /// <summary>
        /// Retrieves all accounts belonging to a customer.
        /// </summary>
        /// <param name="userId">Customer identifier</param>
        /// <returns>Collection of customer's account DTOs</returns>
        public async Task<IEnumerable<GetAccountDto>> GetAccountByCustomerIdAsync(Guid userId)
        {
            var userAccounts = await _unitOfWork.AccountsRepository.GetByCustomerIdAsync(userId);
            return userAccounts.Select(account=>new GetAccountDto(
                     accountNumber: account.AccountNumber,
                       isActive: account.IsActive,
                       ownerId: account.OwnerId,
                       balance: account.Balance,
                       dateOpened: account.CreatedAt
                    ));
        }

        /// <summary>
        /// Activates an inactive account.
        /// </summary>
        /// <param name="accountNumber">Account identifier to activate</param>
        /// <exception cref="KeyNotFoundException">Thrown when account does not exist</exception>
        /// <exception cref="InvalidOperationException">Thrown when account is already active</exception>
        /// <remarks>
        /// Business exceptions bubble up to caller for handling.
        /// </remarks>
        public async Task ActivateAccountAsync(Guid accountNumber)
        {
            var existingAccount = await _unitOfWork.AccountsRepository.GetByIdAsync(accountNumber);
            if (existingAccount == null)
            {
                throw new KeyNotFoundException($"Account with number{accountNumber} not found");
            }

            if (existingAccount.IsActive)
            {
                    throw new InvalidOperationException($"Account with number {accountNumber} is already active");
            }

            existingAccount.IsActive = true;
                _unitOfWork.AccountsRepository.Update(existingAccount);
            await _unitOfWork.SaveChangesAsync();

        }

        /// <summary>
        /// Deactivates an active account.
        /// </summary>
        /// <param name="accountNumber">Account identifier to deactivate</param>
        /// <exception cref="KeyNotFoundException">Thrown when account does not exist</exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when account is already inactive or has non-zero balance
        /// </exception>
        /// <remarks>
        /// Business exceptions bubble up to caller for handling.
        /// </remarks>
        public async Task DeactivateAccountAsync(Guid accountNumber)
        {
            var existingAccount = await _unitOfWork.AccountsRepository.GetByIdAsync(accountNumber);
            if (existingAccount == null)
            {
                throw new KeyNotFoundException($"Account with number{accountNumber} not found");
            }

            if (!existingAccount.IsActive)
            {
                    throw new InvalidOperationException($"Account with number {accountNumber} is already inactive");
            }

            if(existingAccount.Balance>0)
                throw new InvalidOperationException("Cannot deactivate account with balance");

            existingAccount.IsActive = false;
            _unitOfWork.AccountsRepository.Update(existingAccount);
            await _unitOfWork.SaveChangesAsync();
        }

        /// <summary>
        /// Transfers funds between two accounts with transaction safety.
        /// </summary>
        /// <param name="fromAccount">Source account identifier</param>
        /// <param name="toAccount">Destination account identifier</param>
        /// <param name="amount">Transfer amount (must be positive)</param>
        /// <exception cref="ArgumentException">Thrown when amount is zero or negative</exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when transferring to same account or accounts are inactive
        /// </exception>
        /// <exception cref="KeyNotFoundException">Thrown when accounts are not found</exception>
        /// <remarks>
        /// Uses Serializable isolation level to prevent race conditions.
        /// Business exceptions bubble up, transaction rolls back on failure.
        /// Always creates audit log for success or failure.
        /// </remarks>
        public async Task TransferAsync(Guid fromAccount, Guid toAccount, decimal amount)
        {
            if (amount <= 0)
                throw new ArgumentException($"Transfer amount {amount} must be greater than zero.");

            if (fromAccount == toAccount)
                throw new InvalidOperationException("Can't transfer to the same account ");

            bool transferSuccess = false;
            Account source = null;
            Account destination=null;
            //to prevent race conditions 
            await _unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                 source = await _unitOfWork.AccountsRepository.GetByIdAsync(fromAccount);
                 destination = await _unitOfWork.AccountsRepository.GetByIdAsync(toAccount);

                if (source == null)
                    throw new KeyNotFoundException($"Source account {fromAccount} not found.");

                if (destination == null)
                    throw new KeyNotFoundException($"Destination account {toAccount} not found.");

                if (!source.IsActive)
                    throw new InvalidOperationException("Source account is not active");

                if (!destination.IsActive)
                    throw new InvalidOperationException("Destination account is not active");


                source.Withdraw(amount);
                destination.Deposit(amount);

                 _unitOfWork.AccountsRepository.Update(source);
                 _unitOfWork.AccountsRepository.Update(destination);

                await _unitOfWork.CommitTransactionAsync();
                transferSuccess = true;
            }
            catch(Exception ex) when (ExceptionHelper.IsBusinessException(ex))
            {
                throw;
            }
            catch(Exception)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
            finally
            {
                if (!transferSuccess)
                {
                    _unitOfWork.DetachAllEntities();
                }
                //auditing transaction operation
                if (source != null)
                {
                    await _auditLogService.LogTransferAsync(
                        performedBy:source.OwnerId,
                        fromAccount: source.AccountNumber,
                        toAccount: toAccount,
                        amount: amount,
                        success: transferSuccess);
                }
            }
        }

        /// <summary>
        /// Deposits funds into an account with transaction safety.
        /// </summary>
        /// <param name="initiatedBy">User identifier performing the deposit</param>
        /// <param name="toAccount">Destination account identifier</param>
        /// <param name="amount">Deposit amount (must be positive)</param>
        /// <exception cref="ArgumentException">Thrown when amount is zero or negative</exception>
        /// <exception cref="InvalidOperationException">Thrown when account is inactive</exception>
        /// <exception cref="KeyNotFoundException">Thrown when account does not exist</exception>
        /// <remarks>
        /// Uses Serializable isolation level to prevent race conditions.
        /// Business exceptions bubble up, transaction rolls back on failure.
        /// Always creates audit log for success or failure.
        /// </remarks>
        public async Task DepositAsync(Guid performedBy, Guid toAccount, decimal amount)
        {
            //input validation so no need to log it if thrown
            if (amount <= 0)
                throw new ArgumentException($"Deposit amount {amount} must be greater than zero.");

            await _unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable);
            bool transferSuccess = false;
            try
            {
                var account = await _unitOfWork.AccountsRepository.GetByIdAsync(toAccount);
                if (account == null)
                    throw new KeyNotFoundException($"Account with number {toAccount} not found");
                    
                if(!account.IsActive)
                    throw new InvalidOperationException("Can't deposit to Inactive account");

                account.Deposit(amount);
                 _unitOfWork.AccountsRepository.Update( account);
                await _unitOfWork.CommitTransactionAsync();
                transferSuccess = true;
            }
            catch (Exception ex) when (ExceptionHelper.IsBusinessException(ex))
            {
                throw;
            }
            catch (Exception) { 
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
            finally
            {
                if (!transferSuccess)
                {
                    _unitOfWork.DetachAllEntities();
                }
                await _auditLogService.LogDepositAsync(
                    performedBy: performedBy,
                    toAccount: toAccount,
                    amount: amount,
                    success: transferSuccess);
            }
        }

        /// <summary>
        /// Permanently deletes an account.
        /// </summary>
        /// <param name="accountId">Account identifier to delete</param>
        /// <param name="saveChanges">Whether to immediately persist changes to allow transaction operations</param>
        /// <exception cref="KeyNotFoundException">Thrown when account does not exist</exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when account has non-zero balance
        /// </exception>
        /// <remarks>
        /// Business exceptions bubble up to caller for handling.
        /// Only accounts with zero balance can be deleted.
        /// </remarks>
        public async Task DeleteAccountAsync(Guid accountId, bool saveChanges)
        {
            var existingAccount = await _unitOfWork.AccountsRepository.GetByIdAsync(accountId);

            if (existingAccount == null)
                throw new KeyNotFoundException($"Account with number {accountId} not found");

            if (existingAccount.Balance != 0)
                throw new InvalidOperationException("Cannot delete account with balance");


            _unitOfWork.AccountsRepository.Delete(existingAccount);
            if(saveChanges)
                await _unitOfWork.SaveChangesAsync();
        }

        }
    }
