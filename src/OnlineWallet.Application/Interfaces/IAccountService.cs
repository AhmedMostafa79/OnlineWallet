using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OnlineWallet.Domain.Models;
using OnlineWallet.Application.DTOs;
using OnlineWallet.Application.Models;
namespace OnlineWallet.Application.Interfaces
{
    /// <summary>
    /// Service interface for account operations.
    /// Defines business logic for managing accounts and transactions.
    /// </summary>
    public interface  IAccountService
    {
        /// <summary>
        /// Creates a new account for a customer.
        /// </summary>
        /// <param name="newAccount">Account creation details</param>
        /// <param name="saveChanges">Whether to immediately save changes to database</param>
        /// <returns>Created account details</returns>
        public Task<GetAccountDto> CreateAccountAsync(CreateAccountDto newAccount,bool saveChanges);

        // <summary>
        /// Retrieves account details by account identifier.
        /// </summary>
        /// <param name="accountId">Account identifier</param>
        /// <returns>Account details</returns>
        public Task<GetAccountDto> GetAccountByIdAsync(Guid accountId);

        /// <summary>
        /// Retrieves the current balance of an account.
        /// </summary>
        /// <param name="accountId">Account identifier</param>
        /// <returns>Account balance</returns>
        public Task<decimal> GetAccountBalanceAsync(Guid accountId);

        /// <summary>
        /// Retrieves all accounts belonging to a customer.
        /// </summary>
        /// <param name="userId">Customer identifier</param>
        /// <returns>Customer's accounts</returns>
        public Task<IEnumerable<GetAccountDto>> GetAccountByCustomerIdAsync(Guid customerId);

        /// <summary>
        /// Retrieves all accounts in the system.
        /// </summary>
        /// <returns>All accounts</returns>
        public Task<IEnumerable<GetAccountDto>> GetAllAccountsAsync();

        /// <summary>
        /// Determines whether a customer owns a specific account.
        /// </summary>
        /// <param name="customerId">The unique identifier of the customer to check</param>
        /// <param name="accountId">The unique identifier of the account to verify ownership</param>
        /// <returns> true if the customer is the owner of the account; otherwise, false. <returns>
        public Task<bool> IsAccountOwner(Guid accountId,Guid customerId);

        /// <summary>
        /// Activates an account.
        /// </summary>
        /// <param name="accountNumber">Account identifier</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public Task ActivateAccountAsync(Guid accountNumber);

        /// <summary>
        /// Deactivates an account.
        /// </summary>
        /// <param name="accountNumber">Account identifier</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public Task DeactivateAccountAsync(Guid accountNumber);

        /// <summary>
        /// Transfers funds between two accounts.
        /// </summary>
        /// <param name="fromAccount">Source account identifier</param>
        /// <param name="toAccount">Destination account identifier</param>
        /// <param name="amount">Transfer amount</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public Task TransferAsync(Guid fromAccount, Guid toAccount, decimal amount);

        /// <summary>
        /// Deposits funds into an account.
        /// </summary>
        /// <param name="performedBy">User performing the deposit</param>
        /// <param name="toAccount">Destination account identifier</param>
        /// <param name="amount">Deposit amount</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public Task DepositAsync(Guid performedBy, Guid toAccount, decimal amount);

        /// <summary>
        /// Permanently deletes an account.
        /// </summary>
        /// <param name="accountId">Account identifier</param>
        /// <param name="saveChanges">Whether to immediately save changes to database</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public Task DeleteAccountAsync(Guid accountId,bool saveChanges);
    }
}
