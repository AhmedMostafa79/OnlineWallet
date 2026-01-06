using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OnlineWallet.Domain.Models;
using OnlineWallet.Domain.Enums;
using OnlineWallet.Application.DTOs;
using OnlineWallet.Application.Models;
namespace OnlineWallet.Application.Interfaces
{
    /// <summary>
    /// Service interface for administrator operations.
    /// Defines business logic for system management and oversight.
    /// Used as a facade layer for AdminController also
    /// </summary>
    public interface IAdminService
    {
        /// <summary>
        /// Creates a new administrator user.
        /// </summary>
        /// <param name="performedBy">Admin performing the action</param>
        /// <param name="newAdmin">New admin registration details</param>
        /// <returns>Created admin details</returns>
        public Task<GetUserDto> CreateAdminAsync(Guid performedBy, RegisterModel newAdmin);

        /// <summary>
        /// Retrieves administrator details by identifier.
        /// </summary>
        /// <param name="adminId">Admin identifier</param>
        /// <returns>Admin details</returns>
        public Task<GetUserDto> GetAdminByIdAsync(Guid adminId);

        /// <summary>
        /// Updates administrator information.
        /// </summary>
        /// <param name="adminId">Admin identifier</param>
        /// <param name="updateAdmin">Updated admin details</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public Task UpdateAdminAsync(Guid adminId, UpdateUserDto updateAdmin);

        /// <summary>
        /// Checks if an email address is already registered.
        /// </summary>
        /// <param name="email">Email address to check</param>
        /// <returns>True if email is registered, false otherwise</returns>
        public Task<bool> IsEmailRegistered(string email);
        /// <summary>
        /// Deletes an administrator user.
        /// </summary>
        /// <param name="performedBy">Admin performing the deletion</param>
        /// <param name="adminId">Admin to delete</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public Task DeleteAdminAsync(Guid performedBy, Guid adminId);//not used 

        /// <summary>
        /// Retrieves all customer users.
        /// </summary>
        /// <returns>All customers</returns>
        public Task<IEnumerable<GetUserDto>> GetAllCustomersAsync();

        /// <summary>
        /// Retrieves customer details by identifier.
        /// </summary>
        /// <param name="customerId">Customer identifier</param>
        /// <returns>Customer details</returns>
        public Task<GetUserDto> GetCustomerByIdAsync(Guid customerId);

        /// <summary>
        /// Deletes a customer user.
        /// </summary>
        /// <param name="performedBy">Admin performing the deletion</param>
        /// <param name="customerId">Customer to delete</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public Task DeleteCustomerAsync(Guid performedBy, Guid customerId);

        /// <summary>
        /// Retrieves account details by identifier.
        /// </summary>
        /// <param name="accountId">Account identifier</param>
        /// <returns>Account details</returns>
        public Task<GetAccountDto> GetAccountByIdAsync(Guid accountId);

        /// <summary>
        /// Retrieves all accounts in the system.
        /// </summary>
        /// <returns>All accounts</returns>
        public Task<IEnumerable<GetAccountDto>> GetAllAccountsAsync();

        /// <summary>
        /// Activates an account.
        /// </summary>
        /// <param name="performedBy">Admin performing the activation</param>
        /// <param name="accountId">Account to activate</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public Task ActivateAccountAsync(Guid performedBy, Guid accountId);

        /// <summary>
        /// Deactivates an account.
        /// </summary>
        /// <param name="performedBy">Admin performing the deactivation</param>
        /// <param name="accountId">Account to deactivate</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public Task DeactivateAccountAsync(Guid performedBy, Guid accountId);
        
        /// <summary>
        /// Deposits funds into an account as an administrative action.
        /// </summary>
        /// <param name="performedBy">Admin performing the deposit</param>
        /// <param name="request">Transfer request details</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public Task DepositToAccountAsync(Guid performedBy, TransferRequestModel request);

        /// <summary>
        /// Permanently deletes an account.
        /// </summary>
        /// <param name="performedBy">Admin performing the deletion</param>
        /// <param name="accountId">Account to delete</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public Task DeleteAccountAsync(Guid performedBy, Guid accountId);

        /// <summary>
        /// Retrieves all audit log entries.
        /// </summary>
        /// <returns>All audit logs</returns>
        public Task<IEnumerable<AuditLog>>  GetAllAuditLogsAsync();

        /// <summary>
        /// Retrieves audit logs for a specific user.
        /// </summary>
        /// <param name="userId">User identifier</param>
        /// <returns>User's audit logs</returns>
        public Task<IEnumerable<AuditLog>>  GetUserAuditLogsAsync(Guid userId);


        /// <summary>
        /// Retrieves audit logs of a specific action type.
        /// </summary>
        /// <param name="actionType">Action type to filter by</param>
        /// <returns>Audit logs matching the action type</returns>
        public Task<IEnumerable<AuditLog>> GetAuditLogsByTypeAsync(AuditLogActionType actionType);

        /// <summary>
        /// Retrieves audit logs within a time range.
        /// </summary>
        /// <param name="begin">Start of time range</param>
        /// <param name="end">End of time range</param>
        /// <returns>Audit logs within the specified time period</returns>
        public Task<IEnumerable<AuditLog>> GetAuditLogsByTimeStamp(DateTime begin, DateTime end);

    }
}
