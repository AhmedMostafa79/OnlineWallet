using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using OnlineWallet.Application.DTOs;
using OnlineWallet.Application.Helpers;
using OnlineWallet.Application.Interfaces;
using OnlineWallet.Application.Services;
using OnlineWallet.Domain.Enums;
using OnlineWallet.Domain.Models;
using OnlineWallet.Infrastructure.Interfaces;

namespace OnlineWallet.Application.Models
{
    /// <summary>
    /// Service implementation for administrator operations.
    /// Provides system management and oversight functionality.
    /// Aslo used as a facade for AdminController.
    /// </summary>
    public class AdminService:IAdminService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICustomerService _customerService;
        private readonly IAccountService _accountService;
        private readonly IAuditLogService _auditLogService;
        private readonly IHashService _hashService;

        /// <summary>
        /// Initializes a new instance of AdminService.
        /// </summary>
        /// <param name="unitOfWork">Unit of work for data operations</param>
        /// <param name="customerService">Service for customer operations</param>
        /// <param name="accountService">Service for account operations</param>
        /// <param name="auditLogService">Service for audit logging</param>
        /// <param name="hashService">Service for password hashing</param>
        public AdminService(IUnitOfWork unitOfWork,ICustomerService customerService, IAccountService accountService, IAuditLogService auditLogService,IHashService hashService)
        {
            _unitOfWork = unitOfWork;
            _customerService = customerService;
           _accountService=accountService;
            _auditLogService = auditLogService;
            _hashService = hashService;
        }

        /// <summary>
        /// Creates a new administrator user.
        /// </summary>
        /// <param name="performedBy">Identifier of admin performing the action</param>
        /// <param name="model">Registration details for new admin</param>
        /// <returns>DTO containing created admin details</returns>
        /// <exception cref="Exception">Wrapped exception when creation fails</exception>
        /// <remarks>
        /// Business exceptions bubble up through ExceptionHelper.IsBusinessException filter.
        /// System exceptions are wrapped with context information.
        /// Always creates audit log for success or failure.
        /// </remarks>
        public async Task<GetUserDto> CreateAdminAsync(Guid performedBy, RegisterModel model)
        {
            if (await _customerService.IsEmailRegistered(model.Email))
                throw new InvalidOperationException("Email is already registered");

            User newAdmin = null;
            var successOperation = false;
            try
            {
                newAdmin = new User(
                id: Guid.NewGuid(),
                email: model.Email.ToLower(),
                phoneNumber: model.PhoneNumber,
                firstName: model.FirstName.Trim(),
                lastName: model.LastName.Trim(),
                dateOfBirth: model.DateOfBirth,
                role: UserRole.Admin,
                passwordHash: _hashService.HashPassword(model.Password)
            );

                await _unitOfWork.UsersRepository.AddAsync(newAdmin);
                await _unitOfWork.SaveChangesAsync();

                successOperation = true;
                return new GetUserDto
                {
                    Id = newAdmin.Id,
                    Email = newAdmin.Email,
                    PhoneNumber = newAdmin.PhoneNumber,
                    FirstName = newAdmin.FirstName,
                    LastName = newAdmin.LastName,
                    DateOfBirth = newAdmin.DateOfBirth,
                    Role = newAdmin.Role,
                    DateCreated = newAdmin.CreatedAt,
                };
            }
            catch (Exception ex) when (ExceptionHelper.IsBusinessException(ex))
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create Admin account for admin with email {model.Email}");
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
                      performedBy: performedBy,
                      actionType: AuditLogActionType.AdminCreation,
                     createdAt: DateTime.UtcNow,
                      details: successOperation ?
                      $"Admin with Id {performedBy} created successfully new admin with Id {newAdmin.Id}":
                      $"Admin with Id {performedBy} failed to create account for user with email {model.Email}",
                      status: successOperation ?
                      AuditLogStatus.Success:
                      AuditLogStatus.Failed
                      ), saveChanges: true);
            }
            
        }

        /// <summary>
        /// Retrieves administrator details by identifier.
        /// </summary>
        /// <param name="adminId">Admin identifier</param>
        /// <returns>DTO containing admin details</returns>
        /// <exception cref="KeyNotFoundException">Thrown when admin does not exist</exception>
        /// <remarks>
        /// Business exception bubbles up to caller for handling.
        /// </remarks>
        public async Task<GetUserDto> GetAdminByIdAsync(Guid adminId)
        {
                var admin=await _unitOfWork.UsersRepository.GetByIdAsync(adminId);
                if (admin == null)
                    throw new KeyNotFoundException($"Admin with ID {adminId} not found.");
                return new GetUserDto
                {
                    Id = admin.Id,
                    FirstName = admin.FirstName,
                    LastName = admin.LastName,
                    Email = admin.Email,
                    Role=admin.Role,
                    PhoneNumber = admin.PhoneNumber,
                    DateOfBirth = admin.DateOfBirth,
                    DateCreated = admin.CreatedAt,
                };
        }

        /// <summary>
        /// Updates administrator information.
        /// </summary>
        /// <param name="adminId">Admin identifier to update</param>
        /// <param name="updateAdmin">Updated admin details</param>
        /// <exception cref="KeyNotFoundException">Thrown when admin does not exist</exception>
        /// <remarks>
        /// Business exception bubbles up to caller for handling.
        /// </remarks>
        public async Task UpdateAdminAsync(Guid adminId, UpdateUserDto updateAdmin)
        {
            var admin=await _unitOfWork.UsersRepository.GetByIdAsync(adminId);
            if(admin==null)
                throw new KeyNotFoundException($"Admin with ID {adminId} not found.");

                admin.FirstName = updateAdmin.FirstName;
                admin.LastName = updateAdmin.LastName;
                _unitOfWork.UsersRepository.Update(admin);
            await _unitOfWork.SaveChangesAsync();
        }

        /// <summary>
        /// Checks if an email address is already registered.
        /// </summary>
        /// <param name="email">Email address to check</param>
        /// <returns>True if email is registered, false otherwise</returns>
        public async Task<bool> IsEmailRegistered(string email)
        {
            var user = await _unitOfWork.UsersRepository.FindByEmailAsync(email);
            return user != null;
        }

        /// <summary>
        /// Deletes an administrator user.
        /// </summary>
        /// <param name="performedBy">Identifier of admin performing the deletion</param>
        /// <param name="adminToDelete">Admin identifier to delete</param>
        /// <exception cref="KeyNotFoundException">Thrown when admin does not exist</exception>
        /// <exception cref="Exception">Wrapped exception when deletion fails</exception>
        /// <remarks>
        /// Business exceptions bubble up through ExceptionHelper.IsBusinessException filter.
        /// System exceptions are wrapped with context information.
        /// Always creates audit log for success or failure.
        /// </remarks>
        public async Task DeleteAdminAsync(Guid performedBy, Guid adminToDelete)
        {
            var successOperation = false;
            try
            {
                var admin=await _unitOfWork.UsersRepository.GetByIdAsync(adminToDelete);
                if (admin == null)
                    throw new KeyNotFoundException($"Admin with ID {adminToDelete} not found.");

                if (admin.Role == UserRole.Manager)
                    throw new InvalidOperationException("Manager can't be deleted");

                _unitOfWork.UsersRepository.Delete(admin);
                await _unitOfWork.SaveChangesAsync();
                successOperation = true;
            }
            catch (Exception ex) when (ExceptionHelper.IsBusinessException(ex))
            {
                throw;
            }
            catch (Exception ex)
            {

                throw new Exception($"Failed to delete admin with ID {adminToDelete}", ex);
            }
            finally
            {
                if (!successOperation)
                {
                    _unitOfWork.DetachAllEntities();
                }
                await _auditLogService.LogActionAsync(new AuditLog(
                          id: Guid.NewGuid(),
                     performedBy: performedBy,
                     actionType: AuditLogActionType.AdminDeletion,
                     status: successOperation?
                     AuditLogStatus.Success:
                     AuditLogStatus.Failed
                     ,
                     details: successOperation?
                     $"Admin with ID {performedBy} deleted admin with Id {adminToDelete}":
                     $"Admin with ID {performedBy} failed to delete admin with ID {adminToDelete}",
                     createdAt: DateTime.UtcNow
                         ), saveChanges: true);
            }
        }

        /// <summary>
        /// Retrieves all customer users.
        /// </summary>
        /// <returns>Collection of customer DTOs</returns>
        /// <exception cref="Exception">Wrapped exception when retrieval fails</exception>
        public async Task<IEnumerable<GetUserDto>> GetAllCustomersAsync()
        {
            try
            {
                return await _customerService.GetAllCustomersAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to get all customers", ex);
            }
        }

        /// <summary>
        /// Retrieves customer details by identifier.
        /// </summary>
        /// <param name="customerId">Customer identifier</param>
        /// <returns>DTO containing customer details</returns>
        /// <exception cref="Exception">Wrapped exception when retrieval fails</exception>
        /// <remarks>
        /// Business exceptions bubble up through ExceptionHelper.IsBusinessException filter.
        /// System exceptions are wrapped with context information.
        /// </remarks>
        public async Task<GetUserDto> GetCustomerByIdAsync(Guid customerId)
        {
            try
            {
                return await _customerService.GetCustomerByIdAsync(customerId);
            }
            catch (Exception ex) when (ExceptionHelper.IsBusinessException(ex))
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get customer with ID {customerId}", ex);
            }
        }

        /// <summary>
        /// Deletes a customer user.
        /// </summary>
        /// <param name="performedBy">Identifier of admin performing the deletion</param>
        /// <param name="customerId">Customer identifier to delete</param>
        /// <exception cref="Exception">Wrapped exception when deletion fails</exception>
        /// <remarks>
        /// Business exceptions bubble up through ExceptionHelper.IsBusinessException filter.
        /// System exceptions are wrapped with context information.
        /// Always creates audit log for success or failure.
        /// </remarks>
        public async Task DeleteCustomerAsync(Guid performedBy,Guid customerId)
        {
            var successOperation = false;
            try
            {
                await _customerService.DeleteCustomerAsync(customerId);
                successOperation = true;
            }
            catch (Exception ex) when (ExceptionHelper.IsBusinessException(ex))
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to delete customer with ID {customerId}", ex);
            }
            finally
            {
                if (!successOperation)
                {
                    _unitOfWork.DetachAllEntities();
                }
                await _auditLogService.LogActionAsync(new AuditLog(
                         id: Guid.NewGuid(),
                    performedBy: performedBy,
                    actionType: AuditLogActionType.AdminCustomerDeletion,
                    status: successOperation?AuditLogStatus.Success:AuditLogStatus.Failed,
                    details: successOperation?
                    $"Admin with ID {performedBy} deleted customer with ID {customerId}":
                    $"Admin with ID {performedBy} failed to delete customer with ID {customerId}"
                    ,
                    createdAt: DateTime.UtcNow
                        ), saveChanges: true);
            }
        }

        /// <summary>
        /// Retrieves account details by identifier.
        /// </summary>
        /// <param name="accountId">Account identifier</param>
        /// <returns>DTO containing account details</returns>
        /// <exception cref="Exception">Wrapped exception when retrieval fails</exception>
        /// <remarks>
        /// Business exceptions bubble up through ExceptionHelper.IsBusinessException filter.
        /// System exceptions are wrapped with context information.
        /// </remarks>
        public async Task<GetAccountDto> GetAccountByIdAsync(Guid accountId)
        {
            try
            {
                return await _accountService.GetAccountByIdAsync(accountId);
            }
            catch (Exception ex) when (ExceptionHelper.IsBusinessException(ex))
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get account with ID {accountId}", ex);
            }
        }

        /// <summary>
        /// Retrieves all accounts in the system.
        /// </summary>
        /// <returns>Collection of account DTOs</returns>
        /// <exception cref="Exception">Wrapped exception when retrieval fails</exception>
        public async Task<IEnumerable<GetAccountDto>> GetAllAccountsAsync()
        {
            try
            {
                return await _accountService.GetAllAccountsAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to get all accounts", ex);
            }
        }

        /// <summary>
        /// Activates an inactive account.
        /// </summary>
        /// <param name="performedBy">Identifier of admin performing the activation</param>
        /// <param name="accountId">Account identifier to activate</param>
        /// <exception cref="Exception">Wrapped exception when activation fails</exception>
        /// <remarks>
        /// Business exceptions bubble up through ExceptionHelper.IsBusinessException filter.
        /// System exceptions are wrapped with context information.
        /// Always creates audit log for success or failure.
        /// </remarks>
        public async Task ActivateAccountAsync(Guid performedBy, Guid accountId)
        {
            var successOperation = false;
            try
            {
                await _accountService.ActivateAccountAsync(accountId);
                successOperation = true;
            }
            catch (Exception ex) when (ExceptionHelper.IsBusinessException(ex))
            {
                throw;
            }
            catch (Exception ex)
            {
               
                throw new Exception($"Failed to activate account with ID {accountId} ", ex);
            }
            finally
            {
                if (!successOperation)
                {
                    _unitOfWork.DetachAllEntities();
                }
                await _auditLogService.LogActionAsync(new AuditLog(
                       id: Guid.NewGuid(),
                  performedBy: performedBy,
                  actionType: AuditLogActionType.AdminAccountActivation,
                  status: successOperation?AuditLogStatus.Success: AuditLogStatus.Failed,

                  details: successOperation?
                  $"Admin with ID {performedBy} activated account with ID {accountId}":
                  $"Admin with ID {performedBy} failed to activate account with ID {accountId}",
                  createdAt: DateTime.UtcNow
                      ), saveChanges: true);
            }
        }

        /// <summary>
        /// Deactivates an active account.
        /// </summary>
        /// <param name="performedBy">Identifier of admin performing the deactivation</param>
        /// <param name="accountId">Account identifier to deactivate</param>
        /// <exception cref="Exception">Wrapped exception when deactivation fails</exception>
        /// <remarks>
        /// Business exceptions bubble up through ExceptionHelper.IsBusinessException filter.
        /// System exceptions are wrapped with context information.
        /// Always creates audit log for success or failure.
        /// </remarks>
        public async Task DeactivateAccountAsync(Guid performedBy, Guid accountId)
        {
            var successOperation = false;
            try
            {
                await _accountService.DeactivateAccountAsync(accountId);
                successOperation = true;
              
            }
            catch (Exception ex) when (ExceptionHelper.IsBusinessException(ex))
            {
                throw;
            }
            catch (Exception ex) 
            {
               
                throw new Exception($"Failed to deactivate account with ID {accountId} ",ex);
            }
            finally
            {
                if (!successOperation)
                {
                    _unitOfWork.DetachAllEntities();
                }
                await _auditLogService.LogActionAsync(new AuditLog(
                     id: Guid.NewGuid(),
                performedBy: performedBy,
                actionType: AuditLogActionType.AdminAccountDeactivation,
                status: successOperation ? AuditLogStatus.Success : AuditLogStatus.Failed,
                details:successOperation?
                $"Admin with ID {performedBy} deactivated account with ID {accountId}":
                $"Admin with ID {performedBy} failed to deactivate account with ID {accountId}",
                createdAt: DateTime.UtcNow
                    ), saveChanges: true);
            }
        }

        /// <summary>
        /// Deposits funds into an account as an administrative action.
        /// </summary>
        /// <param name="performedBy">Identifier of admin performing the deposit</param>
        /// <param name="request">Transfer request containing destination and amount</param>
        /// <exception cref="InvalidOperationException">Wrapped exception when deposit fails</exception>
        /// <remarks>
        /// Business exceptions bubble up through ExceptionHelper.IsBusinessException filter.
        /// System exceptions are wrapped with context information.
        /// Always creates audit log for success or failure.
        /// </remarks>
        public async Task DepositToAccountAsync(Guid performedBy,TransferRequestModel request)
        {
            var successOperation = false;
            try
            {
                await _accountService.DepositAsync(performedBy, request.ToAccount, request.Amount);
                successOperation = true;
            }
            catch(Exception ex) when (ExceptionHelper.IsBusinessException(ex))
            {
                throw;
            }
            catch(Exception ex)
            {
                throw new Exception($"Failed to deposit {request.Amount} to account with ID {request.ToAccount}",ex);
            }
            finally
            {
                if (!successOperation)
                {
                    _unitOfWork.DetachAllEntities();
                }
                //auditing admin action 
                await _auditLogService.LogActionAsync(new AuditLog(
                    id: Guid.NewGuid(),
               performedBy: performedBy,
               actionType: AuditLogActionType.Deposit,
               status: successOperation ? AuditLogStatus.Success : AuditLogStatus.Failed,
               details: successOperation ?
               $"Admin with ID {performedBy} successfully deposited amount {request.Amount} to account with ID {request.ToAccount}" :
               $"Admin with ID {performedBy} failed to deposit amount {request.Amount} to account with ID {request.ToAccount}",
               createdAt: DateTime.UtcNow
                   ), saveChanges: true);
            }
        }

        /// <summary>
        /// Permanently deletes an account.
        /// </summary>
        /// <param name="performedBy">Identifier of admin performing the deletion</param>
        /// <param name="accountId">Account identifier to delete</param>
        /// <exception cref="Exception">Wrapped exception when deletion fails</exception>
        /// <remarks>
        /// Business exceptions bubble up through ExceptionHelper.IsBusinessException filter.
        /// System exceptions are wrapped with context information.
        /// Always creates audit log for success or failure.
        /// </remarks>
        public async Task DeleteAccountAsync(Guid performedBy, Guid accountId)
        {
            var successOperation = false;
            try
            {
                await _accountService.DeleteAccountAsync(accountId,true);
                successOperation = true;
            }
            catch (Exception ex) when (ExceptionHelper.IsBusinessException(ex))
            {
                throw;
            }
            catch (Exception ex)
            {

                throw new Exception($"Failed to delete account with ID {accountId}",ex);
            }
            finally
            {
                if (!successOperation)
                {
                    _unitOfWork.DetachAllEntities();
                }
                await _auditLogService.LogActionAsync(new AuditLog(
                     id: Guid.NewGuid(),
                performedBy: performedBy,
                actionType: AuditLogActionType.AdminAccountDeletion,
                status: successOperation ? AuditLogStatus.Success : AuditLogStatus.Failed,
                details: successOperation ?
                $"Admin with ID {performedBy} deleted account with ID {accountId}" :
                $"Admin with ID {performedBy} failed to deleted account with ID {accountId}",
                createdAt: DateTime.UtcNow
                    ), saveChanges: true);
            }
               
        }

        /// <summary>
        /// Retrieves all audit log entries.
        /// </summary>
        /// <returns>Collection of audit log entries</returns>
        /// <exception cref="Exception">Wrapped exception when retrieval fails</exception>
        public async Task<IEnumerable<AuditLog>> GetAllAuditLogsAsync()
        {
            try
            {
               return await _auditLogService.GetAllAuditLogsAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get all audit logs", ex);
            }
        }

        /// <summary>
        /// Retrieves audit logs for a specific user.
        /// </summary>
        /// <param name="userId">User identifier</param>
        /// <returns>Collection of user's audit log entries</returns>
        /// <exception cref="Exception">Wrapped exception when retrieval fails</exception>
        public async Task<IEnumerable<AuditLog>> GetUserAuditLogsAsync(Guid userId)
        {
            try
            {
                return await _auditLogService.GetUserAuditLogsAsync(userId);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get audit logs for user with ID {userId}", ex);
            }
        }

        /// <summary>
        /// Retrieves audit logs of a specific action type.
        /// </summary>
        /// <param name="actionType">Action type to filter by</param>
        /// <returns>Collection of audit log entries matching action type</returns>
        /// <exception cref="Exception">Wrapped exception when retrieval fails</exception>
        public async Task<IEnumerable<AuditLog>> GetAuditLogsByTypeAsync(AuditLogActionType actionType)
        {
            try
            {
                return await _auditLogService.GetByActionTypeAsync(actionType);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get audit logs with type {actionType}", ex);
            }
        }

        /// <summary>
        /// Retrieves audit logs within a time range.
        /// </summary>
        /// <param name="begin">Start of time range</param>
        /// <param name="end">End of time range</param>
        /// <returns>Collection of audit log entries within time range</returns>
        /// <exception cref="InvalidOperationException">Thrown when begin time is greater than end time</exception>
        /// <exception cref="Exception">Wrapped exception when retrieval fails</exception>
        /// <remarks>
        /// Business exception bubbles up for invalid time range.
        /// System exceptions are wrapped with context information.
        /// </remarks>
        public async Task<IEnumerable<AuditLog>> GetAuditLogsByTimeStamp(DateTime begin, DateTime end)
        {
            if (begin > end )
                throw new InvalidOperationException($"Start time {begin} cannot be greater than end time {end}");
            try
            {
                return await _auditLogService.GetByTimeStampAsync(begin, end);
            }
            catch(Exception ex)
            {
                throw new Exception($"Failed to get audit logs by timestamp begin: {begin}, end: {end}", ex);
            }
        }

    }
}
