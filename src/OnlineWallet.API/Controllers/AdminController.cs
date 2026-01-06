using Castle.Components.DictionaryAdapter.Xml;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineWallet.Application.DTOs;
using OnlineWallet.Application.Helpers;
using OnlineWallet.Application.Interfaces;
using OnlineWallet.Application.Models;
using OnlineWallet.Domain.Enums;
using OnlineWallet.Domain.Models;
using System.Security.Claims;

namespace OnlineWallet.API.Controllers
{
    /// <summary>
    /// Controller for administrative operations
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,Manager")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;
        private readonly ITokenService _tokenService;
        private readonly ILogger<AdminController> _logger;
       
        /// <summary>
        /// Initializes a new instance of the AdminController
        /// </summary>
        /// <param name="adminService">The admin service</param>
        /// <param name="logger">The logger instance</param>
        public AdminController(IAdminService adminService , ITokenService tokenService, ILogger<AdminController> logger)
        {
            _adminService = adminService;
            _tokenService = tokenService;
            _logger = logger;
        }
        private string GetFirstModelError()
        {
            return ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .FirstOrDefault() ?? "Invalid request data";
        }
        private Guid GetAdminId()
        {
            var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                  if (string.IsNullOrEmpty(adminId))
                throw new UnauthorizedAccessException("User not authenticated");
            return Guid.Parse(adminId);
        }
        /// <summary>
        /// Creates a new administrator user account.
        /// </summary>
        /// <remarks>
        /// Creates a new administrator with the provided registration details.
        /// Only users with Manager role are authorized to perform this action.
        /// 
        /// Sample request:
        /// POST /api/admin/newAdmin/create
        /// Authorization: Bearer {manager-token}
        /// Content-Type: application/json
        /// 
        /// {
        ///   "email": "admin@example.com",
        ///   "password": "SecurePass123!",
        ///   "firstName": "John",
        ///   "lastName": "Doe",
        ///   "phoneNumber": "12345678901",
        ///   "dateOfBirth": "1990-01-01"
        /// }
        /// </remarks>
        /// <param name="model">Administrator registration details</param>
        /// <returns>Newly created administrator details</returns>
        /// <response code="201">Returns the newly created admin user</response>
        /// <response code="400">If the request data is invalid</response>
        /// <response code="409">If the email is already registered</response>
        /// <response code="401">If the request is not authenticated</response>
        /// <response code="403">If the authenticated user is not a Manager</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpPost("admins")]
        [Authorize(Roles = "Manager")]
        [ProducesResponseType(typeof(GetUserDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(string), StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<GetUserDto>> CreateNewAdminAsync([FromBody] RegisterModel model)
        {
            if (await _adminService.IsEmailRegistered(model.Email))
               return Conflict("Email is already registered");

            if (!ModelState.IsValid)
                return BadRequest(GetFirstModelError());
            try
            {
                return Ok(await _adminService.CreateAdminAsync(GetAdminId(),model));
            }
            catch (Exception ex) when (ExceptionHelper.IsBusinessException(ex))
            {
                // Check if it's email duplication error
                if (ex.Message.Contains("already registered", StringComparison.OrdinalIgnoreCase))
                {
                    return Conflict(new { Message = ex.Message });
                }
                return BadRequest(ex.Message);
            }
            catch (Exception ex) {
                _logger.LogError(ex, $"Failed to create new admin user for manager");
                return StatusCode(500, "Failed to create new admin user");

            }

        }
        /// <summary>
        /// Retrieves a specific admin by ID
        /// </summary>
        /// <param name="adminId">The unique identifier of the admin</param>
        /// <returns>Admin details</returns>
        /// <remarks>
        /// Sample request:
        ///     GET /api/admin/admin/{e19add5a-472c-46eb-be47-b3c40c691b62} 
        /// </remarks> 
        /// <response code="200">Returns the admin details</response>
        /// <response code="400">Invalid admin ID</response>
        /// <response code="401">User is not authenticated or authorized</response>
        /// <response code="404">admin not found</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("admin/{adminId}")]
        [Authorize(Roles="Manager")]
        [ProducesResponseType(typeof(GetUserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(string), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<GetUserDto>> GetAdminByIdAsync([FromRoute] Guid adminId)
        {
            try
            {
                var admin = await _adminService.GetCustomerByIdAsync(adminId);
                return Ok(admin);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to get admin for manager");
                return StatusCode(500, $"Failed retrieve admin with ID {adminId} profile");
            }
        }

        /// <summary>
        /// Deletes an administrator user from the system
        /// </summary>
        /// <param name="adminId">The unique identifier of the admin to delete</param>
        /// <returns>Success message</returns>
        /// <remarks>
        /// Sample request:
        /// 
        ///     DELETE /api/admin/12345678-1234-1234-1234-123456789abc
        ///     
        /// Only users with Manager role can perform this operation.
        /// </remarks>
        /// <response code="200">Admin deleted successfully</response>
        /// <response code="400">Invalid operation (e.g., cannot delete self)</response>
        /// <response code="401">User is not authenticated or lacks Manager role</response>
        /// <response code="404">Admin with specified ID is not found</response>
        /// <response code="500">Internal server error</response>
        [HttpDelete("admin/{adminId}")]
        [Authorize(Roles = "Manager")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(string), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteAdminAsync([FromRoute] Guid adminId)
        {
            try
            {
                await _adminService.DeleteAdminAsync(GetAdminId(), adminId);
                return Ok(new
                {
                    message = $"Admin with ID {adminId} is deleted successfully"
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to delete admin for Manager");
                return StatusCode(500, $"Failed to delete admin with ID {adminId}");
            }
        }

        /// <summary>
        /// Updates the admin's profile information
        /// </summary>
        /// <param name="updateRequest">The update request containing new profile data</param>
        /// <returns>Success message</returns>
        /// <remarks>
        /// Sample request:
        /// 
        ///     PUT /api/admin/profile
        ///     {
        ///        "firstName":"John",
        ///        "lastName":"Doe"
        ///     }
        /// </remarks> 
        /// <response code="200">Profile updated successfully</response>
        /// <response code="400">Invalid request data</response>
        /// <response code="401">User is not authenticated or authorized</response>
        /// <response code="404">Admin is not found</response>
        /// <response code="500">Internal server error</response>
        [HttpPut("profile")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(string), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> UpdateAdminProfileAsync([FromBody] UpdateUserDto updateRequest)
        {
            if(!ModelState.IsValid)
                return BadRequest(GetFirstModelError());
            try
            {
                await _adminService.UpdateAdminAsync(GetAdminId(),updateRequest);
                return Ok(new
                {
                    message = "Successfully updated profile"
                }
                    );
            }
            catch(UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch(KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex) 
            {
                _logger.LogError(ex, $"Failed to update profile for admin");
                return StatusCode(500, "Failed to update profile");
            }
        }
        /// <summary>
        /// Retrieves all customers in the system
        /// </summary>
        /// <returns>List of all customers</returns>
        /// <response code="200">Returns list of customers</response>
        /// <response code="401">User is not authenticated or authorized</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("customers")]
        [ProducesResponseType(typeof(IEnumerable<GetUserDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<GetUserDto>>> GetAllCustomersAsync()
        {
            try
            {
                var customers= await _adminService.GetAllCustomersAsync();
                return Ok(customers);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to get all customers for admin");
                return StatusCode(500, "Failed retrieve customers");
            }
        }
        /// <summary>
        /// Retrieves a specific customer by ID
        /// </summary>
        /// <param name="customerId">The unique identifier of the customer</param>
        /// <returns>Customer details</returns>
        /// <remarks>
        /// Sample request:
        ///     GET /api/admin/customer/{e19add5a-472c-46eb-be47-b3c40c691b62} 
        /// </remarks> 
        /// <response code="200">Returns the customer details</response>
        /// <response code="400">Invalid customer ID</response>
        /// <response code="401">User is not authenticated or authorized</response>
        /// <response code="404">Customer not found</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("customer/{customerId}")]
        [ProducesResponseType(typeof(GetUserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(string), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<GetUserDto>> GetCustomerByIdAsync([FromRoute] Guid customerId)
        {
            try
            {
                var customer = await _adminService.GetCustomerByIdAsync(customerId);
                return Ok(customer);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to get customer for admin");
                return StatusCode(500, $"Failed retrieve customer with ID {customerId} profile");
            }
        }
        /// <summary>
        /// Deletes specific customer from the system
        /// </summary>
        /// <param name="customerId"></param>
        /// <returns>Success message with deleted customer ID</returns>
        ///  <remarks>
        /// Sample request:
        ///     DELETE /api/admin/customer/{e19add5a-472c-46eb-be47-b3c40c691b62} 
        /// </remarks> 
        /// <response code="200">Customer deleted successfully</response>
        /// <response code="400">Invalid operation (e.g., customer has active accounts)</response>
        /// <response code="401">User is not authenticated or authorized</response>
        /// <response code="404">Customer not found</response>
        /// <response code="500">Internal server error</response>
        [HttpDelete("customer/{customerId}")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(string), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteCustomerAsync([FromRoute]Guid customerId)
        {
            try
            {
                 await _adminService.DeleteCustomerAsync(GetAdminId(),customerId);
                return Ok(new
                {
                    message= $"Customer with ID {customerId} is deleted successfully"
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch(InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to delete customer for admin");
                return StatusCode(500, $"Failed delete customer with ID {customerId}");
            }
        }
        /// <summary>
        /// Retrieves an account by number
        /// </summary>
        /// <param name="accountNumber">The unique account number</param>
        /// <returns>Account details</returns>
        /// <remarks>
        /// Sample request:
        ///     GET /api/admin/account/{e19add5a-472c-46eb-be47-b3c40c691b62} 
        /// </remarks> 
        /// <response code="200">Returns the account details</response>
        /// <response code="401">User is not authenticated or authorized</response>
        /// <response code="404">Account not found</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("account/{accountNumber}")]
        [ProducesResponseType(typeof(GetAccountDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(string), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<GetAccountDto>> GetAccountByNumberAsync([FromRoute] Guid accountNumber)
        {
            try
            {
                var account=await _adminService.GetAccountByIdAsync(accountNumber);
                return Ok(account);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to get account for admin");
                return StatusCode(500, $"Failed retrieve account with number {accountNumber}");
            }
        }
        /// <summary>
        /// Retrieves all accounts in the system
        /// </summary>
        /// <returns>List of all accounts</returns>
        /// <response code="200">Returns list of accounts</response>
        /// <response code="401">User is not authenticated or authorized</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("accounts")]
        [ProducesResponseType(typeof(IEnumerable<GetAccountDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<GetAccountDto>>> GetAllAccountsAsync()
        {
            try
            {
                var accounts=await _adminService.GetAllAccountsAsync();
                return Ok(accounts);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to get all accounts for admin");
                return StatusCode(500, "Failed to retrieve accounts");
            }
        }
        /// <summary>
        /// Activates an inactive account
        /// </summary>
        /// <param name="accountNumber">The account number to activate</param>
        /// <returns>Success message with account number</returns>
        /// <remarks>
        /// Sample request:
        ///     PUT /api/admin/account/{e19add5a-472c-46eb-be47-b3c40c691b62}/activate 
        /// </remarks> 
        /// <response code="200">Account activated successfully</response>
        /// <response code="400">Invalid operation (e.g., account is already active)</response>
        /// <response code="401">User is not authenticated or authorized</response>
        /// <response code="404">Account not found</response>
        /// <response code="500">Internal server error</response>
        [HttpPut("account/{accountNumber}/activate")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(string), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ActivateAccountAsync([FromRoute] Guid accountNumber)
        {
            try
            {
                await _adminService.ActivateAccountAsync(GetAdminId(),accountNumber);
                return Ok(new
                { 
                    message=$"Successfully activated account with ID {accountNumber}",
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch(InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to activate account for admin");
                return StatusCode(500, $"Failed to activate account with number {accountNumber}");
            }
        }
        /// <summary>
        /// Deactivates an active account
        /// </summary>
        /// <param name="accountNumber">Account number to activate</param>
        /// <returns>Success Message with account number</returns>
        /// <remarks>
        /// Sample request:
        ///     PUT /api/admin/account/{e19add5a-472c-46eb-be47-b3c40c691b62}/deactivate 
        /// </remarks> 
        /// <response code="200">Account deactivated successfully</response>
        /// <response code="400">Invalid operation (e.g., account is already inactive)</response>
        /// <response code="401">User is not authenticated or authorized</response>
        /// <response code="404">Account not found</response>
        /// <response code="500">Internal server error</response>
        [HttpPut("account/{accountNumber}/deactivate")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(string), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeactivateAccountAsync([FromRoute] Guid accountNumber)
        {
            try
            {
                await _adminService.DeactivateAccountAsync(GetAdminId(), accountNumber);
                return Ok(new
                {
                    message=$"Successfully Deactivated account with ID {accountNumber}"
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to deactivate account for admin");
                return StatusCode(500, $"Failed to deactivate account with number {accountNumber}");
            }
        }
        /// <summary>
        /// Deposits amount of money to a specific account
        /// </summary>
        /// <param name="request">Deposit request model</param>
        /// <returns>Success message with account number </returns>
        /// <remarks>
        /// Sample request:
        ///     POST /api/admin/account/deposit
        ///     {
        ///         "toAccount":"e19add5a-472c-46eb-be47-b3c40c691b62",
        ///         "amount":0.00
        ///     }
        /// </remarks> 
        /// /// <response code="200">Amount is deposited successfully to the account</response>
        /// <response code="400">Invalid operation (e.g., account is inactive)</response>
        /// <response code="401">User is not authenticated or authorized</response>
        /// <response code="404">Account not found</response>
        /// <response code="500">Internal server error</response>
        [HttpPost("account/deposit")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(string), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DepositToAccountAsync([FromBody] TransferRequestModel request)
        {
            if(!ModelState.IsValid)
                return BadRequest(GetFirstModelError());

            try
            {
                await _adminService.DepositToAccountAsync(GetAdminId(), request);
                return Ok(new 
                    {
                    message = $"Successfully deposited amount {request.Amount} to account {request.ToAccount}"
                        });
            }
            catch(KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch(UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to deposit to account for admin");
                return StatusCode(500, $"Failed to deposit {request.Amount} to account with number {request.ToAccount}");
            }
        }
        /// <summary>
        /// Permanently deletes an account from the system
        /// </summary>
        /// <param name="accountNumber">The account number to delete</param>
        /// <returns>Success message with deleted account number</returns>
        /// <remarks>
        /// Sample request:
        ///     DELETE /api/admin/account/{e19add5a-472c-46eb-be47-b3c40c691b62}
        /// </remarks> 
        /// <response code="200">Account deleted successfully</response>
        /// <response code="400">Invalid operation</response>
        /// <response code="401">User is not authenticated or authorized</response>
        /// <response code="404">Account not found</response>
        /// <response code="500">Internal server error</response>
        [HttpDelete("account/{accountNumber}")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(string), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteAccountAsync([FromRoute] Guid accountNumber)
        {
            try
            {
                await _adminService.DeleteAccountAsync(GetAdminId(), accountNumber);
                return Ok(new
                {
                    message = "Account is successfully deleted",
                    accountNumber
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to delete account for admin");
                return StatusCode(500, $"Failed to delete account with number {accountNumber}");
            }
        }
        /// <summary>
        /// Retrieves all audit logs in the system
        /// </summary>
        /// <returns>List of all audit logs</returns>
        /// <response code="200">Returns list of audit logs</response>
        /// <response code="401">User is not authenticated or authorized</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("audit-logs")]
        [Authorize(Roles = "Manager")]
        [ProducesResponseType(typeof(IEnumerable<AuditLog>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<AuditLog>>> GetAllAuditLogsAsync()
        {
            try
            {
                var auditLogs=await _adminService.GetAllAuditLogsAsync();
                return Ok(auditLogs);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to get all audit Logs for admin");
                return StatusCode(500, "Failed to retrieve audit Logs");
            }
        }
        /// <summary>
        /// Retrieves audit logs for a specific user
        /// </summary>
        /// <param name="userId">The unique identifier of the user</param>
        /// <returns>List of audit logs for the specified user</returns>
        /// <remarks>
        /// Sample request:
        ///     GET /api/admin/audit-logs/users/{e19add5a-472c-46eb-be47-b3c40c691b62} 
        /// </remarks> 
        /// <response code="200">Returns list of user audit logs</response>
        /// <response code="401">User is not authenticated or authorized</response>
        /// <response code="404">User not found</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("audit-logs/users/{userId}")]
        [ProducesResponseType(typeof(IEnumerable<AuditLog>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<AuditLog>>> GetUserAuditLogs([FromRoute]Guid userId)
        {
            try
            {
                var auditLogs = await _adminService.GetUserAuditLogsAsync(userId);
                return Ok(auditLogs);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch(KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to get user audit Logs for admin");
                return StatusCode(500, "Failed to retrieve user audit Logs");
            }
        }
        /// <summary>
        /// Retrieves audit logs filtered by action type
        /// </summary>
        /// <param name="actionType">The type of audit log action to filter by</param>
        /// <returns>List of audit logs for the specified action type</returns>
        /// <remarks>
        /// Sample request:
        ///     GET /api/admin/audit-logs/actions/{actionType}
        ///     {
        ///        actionType:AuditLogActionType
        ///     }
        /// </remarks> 
        /// <response code="200">Returns list of filtered audit logs</response>
        /// <response code="400">Invalid action type</response>
        /// <response code="401">User is not authenticated or authorized</response>
        /// <response code="404">No audit logs found for the specified action type</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("audit-logs/actions/{actionType}")]
        [Authorize(Roles = "Manager")]
        [ProducesResponseType(typeof(IEnumerable<AuditLog>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(string), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<AuditLog>>> GetAuditLogsByActionType([FromRoute] string actionType)
        {
            try
            {
                AuditLogActionType parsedActionType;
                // Try to parse as enum name (e.g., "Transfer", "Deposit")
                if(Enum.TryParse<AuditLogActionType>(actionType,true,out parsedActionType))
                {
                    var auditLogs = await _adminService.GetAuditLogsByTypeAsync(parsedActionType);
                    return Ok(auditLogs);
                }

                // Try to parse as integer ID(e.g., "2", "10")
                if (int.TryParse(actionType, out int actionTypeId) &&
              Enum.IsDefined(typeof(AuditLogActionType), actionTypeId))
                {
                    parsedActionType = (AuditLogActionType)actionTypeId;
                    var auditLogs = await _adminService.GetAuditLogsByTypeAsync(parsedActionType);
                    return Ok(auditLogs);
                }

                return BadRequest($"Invalid action type: '{actionType}'. " +
                       "Use numeric ID (0, 1, 2...) or name (Deposit, Transfer...). " +
                       "Call GET /api/admin/audit-logs/action-types for available values.");
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to get audit Logs with action type for admin");
                return StatusCode(500, "Failed to retrieve audit Logs with action type");
            }
        }
        /// <summary>
        /// Retrieves all available audit log action types
        /// </summary>
        /// <returns>Dictionary of action type IDs and names</returns>
        /// <remarks>
        /// Sample request:
        ///     GET /api/admin/audit-logs/action-types
        /// </remarks>
        [HttpGet("audit-logs/action-types")]
        [ProducesResponseType(typeof(Dictionary<int, string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public IActionResult GetAuditLogActionTypes()
        {
            try
            {
                var actionTypes = Enum.GetValues(typeof(AuditLogActionType))
                    .Cast<AuditLogActionType>()
                    .ToDictionary(
                        k => (int)k,
                        v => v.ToString()
                    );

                return Ok(actionTypes);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get audit log action types");
                return StatusCode(500, "Failed to retrieve action types");
            }
        }
    }
}