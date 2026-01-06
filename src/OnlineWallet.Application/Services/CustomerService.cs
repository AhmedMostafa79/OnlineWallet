using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Castle.Core.Resource;
using OnlineWallet.Application.DTOs;
using OnlineWallet.Application.Helpers;
using OnlineWallet.Application.Interfaces;
using OnlineWallet.Domain.Enums;
using OnlineWallet.Domain.Models;
using OnlineWallet.Infrastructure.Interfaces;

namespace OnlineWallet.Application.Models
{
    /// <summary>
    /// Service implementation for customer operations.
    /// Handles business logic for customer management and account activities.
    /// Also used as facade layer for CustomerController
    /// </summary>
    public class CustomerService:ICustomerService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHashService _hashService;
        private readonly IAccountService _accountService;
        private readonly IAuditLogService _auditLogService;

        /// <summary>
        /// Initializes a new instance of CustomerService.
        /// </summary>
        /// <param name="unitOfWork">Unit of work for data operations</param>
        /// <param name="accountService">Service for account operations</param>
        /// <param name="auditLogService">Service for audit logging</param>
        /// <param name="hashService">Service for password hashing</param>
        public CustomerService(IUnitOfWork unitOfWork, IAccountService accountService, IAuditLogService   auditLogService, IHashService hashService)
        {
            _unitOfWork = unitOfWork;
            _accountService = accountService;
            _auditLogService = auditLogService;
            _hashService = hashService;
        }

        /// <summary>
        /// Creates a new customer user with initial account.
        /// </summary>
        /// <param name="model">Customer registration details</param>
        /// <returns>Created customer details</returns>
        /// <exception cref="Exception">Wrapped exception when creation fails</exception>
        /// <remarks>
        /// Uses transaction to ensure atomic creation of customer and account.
        /// Business exceptions bubble up through ExceptionHelper.IsBusinessException filter.
        /// System exceptions are wrapped with context information.
        /// Always creates audit log for success or failure.
        /// </remarks>
        public async Task<GetUserDto> CreateCustomerAsync(RegisterModel model)
        {

            await _unitOfWork.BeginTransactionAsync();
            User newCustomer=null;
            var successOperation = false;
            try
            {
                newCustomer = new User (
                  id: Guid.NewGuid(),
                  email: model.Email.ToLower(),
                  phoneNumber: model.PhoneNumber,
                  firstName: model.FirstName.Trim(),
                  lastName: model.LastName.Trim(),
                  dateOfBirth: model.DateOfBirth,
                  role: UserRole.Customer,
                  passwordHash: _hashService.HashPassword(model.Password)
              );

                await _unitOfWork.UsersRepository.AddAsync(newCustomer);
                await _unitOfWork.SaveChangesAsync();
                var userAccount = await _accountService.CreateAccountAsync(
                newAccount:new CreateAccountDto
                {
                    OwnerId = newCustomer.Id,
                    CreatedAt = newCustomer.CreatedAt
                },
                saveChanges:false);

                newCustomer.CurrentAccountNumber = userAccount.AccountNumber;

                await _unitOfWork.CommitTransactionAsync();
                successOperation = true;
             

                return new GetUserDto {
                    Id= newCustomer.Id,
                    Email= newCustomer.Email,
                    PhoneNumber= newCustomer.PhoneNumber,
                    FirstName= newCustomer.FirstName,
                    LastName= newCustomer.LastName,
                    DateOfBirth= newCustomer.DateOfBirth,
                    Role= newCustomer.Role,
                    DateCreated= newCustomer.CreatedAt,
                    };
            }
            //rethrow business exceptions to be handled by controller for better user experience
            catch(Exception ex) when (ExceptionHelper.IsBusinessException(ex))
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
            catch (Exception ex) {
                await _unitOfWork.RollbackTransactionAsync();
                throw new Exception($"Failed to create customer account for customer {model.Email}");
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
                    performedBy:null,
                    actionType: AuditLogActionType.CustomerCreation,
                    createdAt: DateTime.UtcNow,
                    details: successOperation?
                    $"Customer {model.Email} created successfully with Id {newCustomer.Id}":
                    $"Failed to create account for customer with email {model.Email}.",
                    status: successOperation?AuditLogStatus.Success:AuditLogStatus.Failed
                    ), saveChanges: true);
            }
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
        /// Retrieves customer details by identifier.
        /// </summary>
        /// <param name="customerId">Customer identifier</param>
        /// <returns>Customer details</returns>
        /// <exception cref="KeyNotFoundException">Thrown when customer does not exist</exception>
        public async Task<GetUserDto> GetCustomerByIdAsync(Guid customerId)
        {
            var customer = await _unitOfWork.UsersRepository.GetByIdAsync(customerId);

            if (customer == null)
                throw new KeyNotFoundException($"Customer with ID {customerId} is not found");

            return new GetUserDto
                {
                    Id = customer.Id,
                    FirstName = customer.FirstName,
                    LastName = customer.LastName,
                    Email = customer.Email,
                    Role=customer.Role,
                    CurrentAccountNumber=customer.CurrentAccountNumber,
                    PhoneNumber = customer.PhoneNumber,
                    DateOfBirth = customer.DateOfBirth,
                    DateCreated = customer.CreatedAt
                };
        }

        /// <summary>
        /// Finds a customer by email address.
        /// </summary>
        /// <param name="email">Email address to search</param>
        /// <returns>Customer details if found</returns>
        /// <exception cref="KeyNotFoundException">Thrown when customer does not exist</exception>
        public async Task<GetUserDto> FindByEmailAsync(string email)
        {
            var customer= await _unitOfWork.UsersRepository.FindByEmailAsync(email);

            if (customer == null)
                throw new KeyNotFoundException($"Customer with email {email} not found.");
           
            return new GetUserDto
            {
                Id = customer.Id,
                FirstName = customer.FirstName,
                LastName = customer.LastName,
                Email = customer.Email,
                CurrentAccountNumber=customer.CurrentAccountNumber,
                Role=customer.Role,
                PhoneNumber = customer.PhoneNumber,
                DateOfBirth = customer.DateOfBirth,
                DateCreated = customer.CreatedAt
            };
        }

        /// <summary>
        /// Verifies a user's password.
        /// </summary>
        /// <param name="customerId">Customer identifier</param>
        /// <param name="password">Password to verify</param>
        /// <returns>True if password matches, false otherwise</returns>
        /// <exception cref="KeyNotFoundException">Thrown when customer does not exist</exception>
        /// <exception cref="Exception">Wrapped exception when verification fails</exception>
        /// <remarks>
        /// Business exceptions bubble up through ExceptionHelper.IsBusinessException filter.
        /// System exceptions are wrapped with context information.
        /// </remarks>
        public async Task<bool> CheckUserPasswordAsync(Guid customerId, string password)
        {
            var customer = await _unitOfWork.UsersRepository.GetByIdAsync(customerId);
            if (customer == null)
                throw new KeyNotFoundException($"Customer with ID {customerId} not found.");
            try
            {
                return _hashService.VerifyPassword(password, customer.PasswordHash);
            }
            catch(Exception ex) when (ExceptionHelper.IsBusinessException(ex))
            {
                throw;
            }
            catch(Exception ex)
            {
                throw new Exception($"Failed to verify password for customer {customerId}", ex);
            }
        }

        /// <summary>
        /// Retrieves all customer users.
        /// </summary>
        /// <returns>All customers</returns>
        public async Task<IEnumerable<GetUserDto>> GetAllCustomersAsync()
        {
                var allCustomers=await _unitOfWork.UsersRepository.GetAllCustomersAsync();

                return allCustomers.Select(customer => new GetUserDto
                {
                    Id = customer.Id,
                    FirstName = customer.FirstName,
                    LastName = customer.LastName,
                    Email = customer.Email,
                    Role=customer.Role,
                    CurrentAccountNumber=customer.CurrentAccountNumber,
                    PhoneNumber = customer.PhoneNumber,
                    DateOfBirth = customer.DateOfBirth,
                    DateCreated = customer.CreatedAt
                }).ToList();
        }

        /// <summary>
        /// Creates a new financial account for a customer.
        /// </summary>
        /// <param name="customerId">Customer identifier</param>
        /// <returns>Created account details</returns>
        /// <exception cref="Exception">Wrapped exception when creation fails</exception>
        /// <remarks>
        /// Business exceptions bubble up through ExceptionHelper.IsBusinessException filter.
        /// System exceptions are wrapped with context information.
        /// Creates audit log for success operation to track customer actions.
        /// </remarks>
        public async Task<GetAccountDto> CreateAccountAsync(Guid customerId)
        {
            GetAccountDto createdAccount = null;
            try
            {
                createdAccount = await _accountService.CreateAccountAsync(
                   newAccount: new CreateAccountDto
                   {
                       OwnerId = customerId,
                       CreatedAt = DateTime.UtcNow
                   },
                    saveChanges: true
                );
             
                return createdAccount;
            }
            catch(Exception ex) when (ExceptionHelper.IsBusinessException(ex))
            {
                throw;
            }
            catch (Exception ex){
                throw new Exception($"Failed to create account for customer with Id {customerId}", ex);
            }
        }

        /// <summary>
        /// Sets a customer-owned account as their primary account.
        /// </summary>
        /// <param name="customerId">Customer identifier</param>
        /// <param name="accountNumber">Account identifier to set as primary</param>
        /// <returns>Updated customer details with new primary account</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown when customer does not own the specified account</exception>
        /// <exception cref="Exception">Wrapped exception when operation fails</exception>
        /// <remarks>
        /// Business exceptions bubble up through ExceptionHelper.IsBusinessException filter.
        /// System exceptions are wrapped with context information.
        /// Validates account ownership before updating primary account reference.
        /// </remarks>
        public async Task<GetUserDto> SetPrimaryAccountAsync(Guid customerId, Guid accountNumber)
        {
            if (! await _accountService.IsAccountOwner(accountNumber, customerId))
            {
                throw new UnauthorizedAccessException($"customer {customerId} doesn't own account {accountNumber}");
            }
            try
            {
                User customer=await _unitOfWork.UsersRepository.GetByIdAsync(customerId);
                if (customer == null)
                    throw new KeyNotFoundException($"Customer with ID {customerId} not found.");
                    
                customer.CurrentAccountNumber = accountNumber;
                await _unitOfWork.SaveChangesAsync();
                return new GetUserDto
                {
                    Id = customer.Id,
                    FirstName = customer.FirstName,
                    LastName = customer.LastName,
                    Email = customer.Email,
                    Role = customer.Role,
                    CurrentAccountNumber = customer.CurrentAccountNumber,
                    PhoneNumber = customer.PhoneNumber,
                    DateOfBirth = customer.DateOfBirth,
                    DateCreated = customer.CreatedAt
                };
            }
            catch (Exception ex) when (ExceptionHelper.IsBusinessException(ex))
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to put {accountNumber} as primary account for customer {customerId}",ex);
            }
        }

        /// <summary>
        /// Retrieves the current primary account for a customer.
        /// </summary>
        /// <param name="customerId">Customer identifier</param>
        /// <returns>Current account details</returns>
        /// <exception cref="Exception">Wrapped exception when retrieval fails</exception>
        /// <remarks>
        /// Business exceptions bubble up through ExceptionHelper.IsBusinessException filter.
        /// System exceptions are wrapped with context information.
        /// </remarks>
        public async Task<GetAccountDto> GetCurrentAccountAsync(Guid customerId)
        {
            try
            {
                var customer = await GetCustomerByIdAsync(customerId);
                if (customer.CurrentAccountNumber == null)
                {
                    throw new KeyNotFoundException("Customer doesn't have primary account");
                }
                return await _accountService.GetAccountByIdAsync(customer.CurrentAccountNumber.Value);
            }
            catch (Exception ex) when (ExceptionHelper.IsBusinessException(ex))
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get current account for customer {customerId}", ex);
            }
        }

        /// <summary>
        /// Retrieves all accounts belonging to a customer.
        /// </summary>
        /// <param name="customerId">Customer identifier</param>
        /// <returns>Customer's accounts</returns>
        /// <exception cref="Exception">Wrapped exception when retrieval fails</exception>
        /// <remarks>
        /// Business exceptions bubble up through ExceptionHelper.IsBusinessException filter.
        /// System exceptions are wrapped with context information.
        /// </remarks>
        public async Task<IEnumerable<GetAccountDto>> GetCustomerAccountsAsync(Guid customerId)
        {
            try
            {
                return await _accountService.GetAccountByCustomerIdAsync(customerId);
            }
            catch (Exception ex) when (ExceptionHelper.IsBusinessException(ex))
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to get customer Accounts ",ex);
            }
        }

        /// <summary>
        /// Retrieves the current balance of a customer's primary account.
        /// </summary>
        /// <param name="customerId">Customer identifier</param>
        /// <returns>Account balance</returns>
        /// <exception cref="Exception">Wrapped exception when retrieval fails</exception>
        /// <remarks>
        /// Business exceptions bubble up through ExceptionHelper.IsBusinessException filter.
        /// System exceptions are wrapped with context information.
        /// </remarks>
        public async Task<decimal> GetCurrentAccountBalanceAsync(Guid customerId)
        {
            try
            {
                var customer = await GetCustomerByIdAsync(customerId); // ensures the customer has current account

                var accountNumber = customer.CurrentAccountNumber;
                if (accountNumber == null)
                {
                    throw new KeyNotFoundException("Customer doesn't have primary account");
                }
                return await _accountService.GetAccountBalanceAsync(accountNumber.Value); //current account can never be null 
            }
            catch (Exception ex) when (ExceptionHelper.IsBusinessException(ex))
            {
                throw;
            }
            catch (Exception ex){
                throw new Exception($"Failed to get account balance for customer {customerId}",ex);
            }   
        }

        /// <summary>
        /// Permanently deletes an account and updates customer references.
        /// </summary>
        /// <param name="accountId">The unique identifier of the account to delete</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown when account deletion fails due to system errors.
        /// </exception>
        /// <remarks>
        /// Business exceptions bubble up through ExceptionHelper.IsBusinessException filter.
        /// System exceptions are wrapped with context information.
        /// Creates audit log for success operations to track customer actions.

        /// </remarks>
        public async Task DeleteAccountAsync(Guid accountId)
        {
            var successOperation = false;
            var account=await _accountService.GetAccountByIdAsync(accountId);
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                User customer = await _unitOfWork.UsersRepository.GetByIdAsync(account.OwnerId);
                if (customer == null)
                    throw new KeyNotFoundException($"Customer with ID {account.OwnerId} not found.");
                    
                await _accountService.DeleteAccountAsync(accountId,false);

                if (accountId == customer.CurrentAccountNumber)
                {
                    customer.CurrentAccountNumber =null;
                }
                await _auditLogService.LogActionAsync(new AuditLog(
                   id: Guid.NewGuid(),
                  performedBy: customer.Id,
                  actionType: AuditLogActionType.CustomerAccountDeletion,
                  status:  AuditLogStatus.Success ,
                  details:  $"Customer with ID {account.OwnerId} successfully deleted account with ID {accountId}",
                  createdAt: DateTime.UtcNow
                  ), saveChanges: false);
                await _unitOfWork.CommitTransactionAsync();
            }
            catch (Exception ex) when (ExceptionHelper.IsBusinessException(ex))
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw new InvalidOperationException($"Failed to delete account with ID {accountId}",ex);
            }
            
        }

        /// <summary>
        /// Retrieves transaction history for a customer.
        /// </summary>
        /// <param name="customerId">Customer identifier</param>
        /// <returns>Customer's transaction audit logs</returns>
        /// <exception cref="Exception">Wrapped exception when retrieval fails</exception>
        /// <remarks>
        /// Business exceptions bubble up through ExceptionHelper.IsBusinessException filter.
        /// System exceptions are wrapped with context information.
        /// </remarks>
        public async Task<IEnumerable<AuditLog>> GetTransactionHistoryAsync(Guid customerId)
        {
            try
            {
                return await _auditLogService.GetTransactionHistoryAsync(customerId);
            }
            catch (Exception ex) when (ExceptionHelper.IsBusinessException(ex))
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get transaction history for customer with ID {customerId}",ex);
            }
        }

        /// <summary>
        /// Updates customer information.
        /// </summary>
        /// <param name="customerId">Customer identifier</param>
        /// <param name="updateCustomer">Updated customer details</param>
        /// <exception cref="KeyNotFoundException">Thrown when customer does not exist</exception>
        public async Task UpdateCustomerAsync(Guid customerId, UpdateUserDto updateCustomer)
        {
           
            var customer = await _unitOfWork.UsersRepository.GetByIdAsync(customerId);
            if(customer==null)
                throw new KeyNotFoundException($"Customer with ID {customerId} not found.");

            if(!string.IsNullOrEmpty(updateCustomer.FirstName))
                customer.FirstName = updateCustomer.FirstName;
            if(!string.IsNullOrEmpty(updateCustomer.LastName))
                customer.LastName = updateCustomer.LastName;

                _unitOfWork.UsersRepository.Update(customer);
                await _unitOfWork.SaveChangesAsync();
        }

        /// <summary>
        /// Transfers funds from customer's primary account.
        /// </summary>
        /// <param name="customerId">Customer identifier</param>
        /// <param name="request">Transfer request details</param>
        /// <exception cref="Exception">Wrapped exception when transfer fails</exception>
        /// <remarks>
        /// Business exceptions bubble up through ExceptionHelper.IsBusinessException filter.
        /// System exceptions are wrapped with context information.
        /// </remarks>
        public async Task TransferAsync(Guid customerId, TransferRequestModel request)
        {


            try
            {
                var customer  = await GetCustomerByIdAsync(customerId);

                if (customer.CurrentAccountNumber == null)
                {
                    throw new KeyNotFoundException("Customer doesn't have primary account");
                }
                await _accountService.TransferAsync(customer.CurrentAccountNumber.Value,request.ToAccount,request.Amount);
            }
            catch (Exception ex) when (ExceptionHelper.IsBusinessException(ex))
            {
                throw;
            }
            catch (Exception ex)
            {
                    throw new Exception($"Failed to transfer amount {request.Amount} by customer {customerId} to account  {request.ToAccount}",ex);
            }
        }

        /// <summary>
        /// Permanently deletes a customer and all his accounts.
        /// </summary>
        /// <param name="customerId">Customer identifier</param>
        /// <exception cref="KeyNotFoundException">Thrown when customer does not exist</exception>
        /// <exception cref="InvalidOperationException">Thrown when customer has accounts with balance</exception>
        /// <exception cref="Exception">Wrapped exception when deletion fails</exception>
        /// <remarks>
        /// Uses transaction to ensure atomic deletion of customer and accounts.
        /// Business exceptions bubble up through ExceptionHelper.IsBusinessException filter.
        /// System exceptions are wrapped with context information.
        /// </remarks>
        public async Task DeleteCustomerAsync(Guid customerId)
        {

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var customer= await _unitOfWork.UsersRepository.GetByIdAsync(customerId);
                if (customer == null)
                    throw new KeyNotFoundException($"Customer with ID {customerId} not found.");

                var customerAccounts = await _accountService.GetAccountByCustomerIdAsync(customerId);
                var accountWithBalance = customerAccounts.Where(a => a.Balance > 0);

                if (accountWithBalance.Any())
                    throw new InvalidOperationException("Can't delete customer with accounts containing balance");

                _unitOfWork.UsersRepository.Delete(customer);

                foreach(var account in customerAccounts)
                {
                    await _accountService.DeleteAccountAsync(account.AccountNumber,false);
                }
                await _unitOfWork.CommitTransactionAsync();

            }
            catch (Exception ex) when (ExceptionHelper.IsBusinessException(ex))
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw new Exception($"Failed to delete customer with ID {customerId}",ex);
            }
           
        }
    }
}
