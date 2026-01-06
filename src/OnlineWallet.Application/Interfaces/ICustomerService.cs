using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OnlineWallet.Domain.Models;
using OnlineWallet.Application.Models;
using OnlineWallet.Application.DTOs;
namespace OnlineWallet.Application.Interfaces
{
    /// <summary>
    /// Service interface for customer operations.
    /// Defines business logic for customer management and account activities.
    /// Also used as facade layer for CustomerController
    /// </summary>
    public interface ICustomerService
    {
        /// <summary>
        /// Creates a new customer user.
        /// </summary>
        /// <param name="model">Customer registration details</param>
        /// <returns>Created customer details</returns>
        public Task<GetUserDto> CreateCustomerAsync(RegisterModel model);

        /// <summary>
        /// Retrieves customer details by identifier.
        /// </summary>
        /// <param name="customerId">Customer identifier</param>
        /// <returns>Customer details</returns>
        public Task<GetUserDto> GetCustomerByIdAsync(Guid customerId);

        /// <summary>
        /// Retrieves the current primary account for a customer.
        /// </summary>
        /// <param name="customerId">Customer identifier</param>
        /// <returns>Current account details</returns>
        public Task<GetAccountDto>GetCurrentAccountAsync(Guid customerId);

        /// <summary>
        /// Checks if an email address is already registered.
        /// </summary>
        /// <param name="email">Email address to check</param>
        /// <returns>True if email is registered, false otherwise</returns>
        public Task<bool> IsEmailRegistered(string email);

        /// <summary>
        /// Finds a customer by email address.
        /// </summary>
        /// <param name="email">Email address to search</param>
        /// <returns>Customer details if found</returns>
        public Task<GetUserDto> FindByEmailAsync(string email);

        /// <summary>
        /// Verifies a user's password.
        /// </summary>
        /// <param name="userId">User identifier</param>
        /// <param name="password">Password to verify</param>
        /// <returns>True if password matches, false otherwise</returns>
        public Task<bool> CheckUserPasswordAsync (Guid userId, string password);

        /// <summary>
        /// Retrieves all customer users.
        /// </summary>
        /// <returns>All customers</returns>
        public Task<IEnumerable<GetUserDto>> GetAllCustomersAsync();

        /// <summary>
        /// Creates a new financial account for a customer.
        /// </summary>
        /// <param name="customerId">Customer identifier</param>
        /// <returns>Created account details</returns>
        public Task<GetAccountDto> CreateAccountAsync(Guid customerId);

        /// <summary>
        /// Sets a customer-owned account as their primary account.
        /// </summary>
        /// <param name="customerId">Customer identifier</param>
        /// <param name="accountNumber">Account identifier to set as primary</param>
        /// <returns>
        /// Updated customer details with the new primary account reference.
        /// </returns>
        public Task<GetUserDto> SetPrimaryAccountAsync(Guid customerId, Guid accountNumber);

        /// <summary>
        /// Retrieves the current balance of an account.
        /// </summary>
        /// <param name="accountId">Account identifier</param>
        /// <returns>Account balance</returns>
        public Task<decimal> GetCurrentAccountBalanceAsync(Guid accountId);

        /// <summary>
        /// Permanently deletes an account.
        /// </summary>
        /// <param name="accountId">Account identifier</param>
        public Task DeleteAccountAsync( Guid accountId);

        /// <summary>
        /// Retrieves all accounts belonging to a customer.
        /// </summary>
        /// <param name="customerId">Customer identifier</param>
        /// <returns>Customer's accounts</returns>
        public Task<IEnumerable<GetAccountDto>> GetCustomerAccountsAsync(Guid customerId);

        /// <summary>
        /// Retrieves transaction history for a user.
        /// </summary>
        /// <param name="userId">User identifier</param>
        /// <returns>User's transaction audit logs</returns>
        public Task<IEnumerable<AuditLog>> GetTransactionHistoryAsync(Guid customerId);

        /// <summary>
        /// Updates customer information.
        /// </summary>
        /// <param name="customerId">Customer identifier</param>
        /// <param name="updateCustomer">Updated customer details</param>
        Task UpdateCustomerAsync(Guid customerId, UpdateUserDto updateCustomer);

        /// <summary>
        /// Transfers funds from customer's account.
        /// </summary>
        /// <param name="initiateAccount">Source account identifier</param>
        /// <param name="request">Transfer request details</param>
        public Task TransferAsync(Guid initiateAccount, TransferRequestModel request);

        /// <summary>
        /// Permanently deletes a customer.
        /// </summary>
        /// <param name="customerId">Customer identifier</param>
        public Task DeleteCustomerAsync(Guid customerId);
    }
}
