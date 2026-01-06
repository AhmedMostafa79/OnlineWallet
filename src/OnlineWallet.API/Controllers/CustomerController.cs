using Microsoft.AspNetCore.Mvc;
using OnlineWallet.Application.Interfaces;
using OnlineWallet.Domain.Models;
using OnlineWallet.Application.DTOs;
using Microsoft.Identity.Client;
using System.Security.Principal;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using OnlineWallet.Application.Helpers;
using OnlineWallet.Application.Models;

namespace OnlineWallet.API.Controllers
{
    /// <summary>
    /// Controller for customer operations
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles="Customer")]
    public class CustomerController : ControllerBase
    {
        private readonly ICustomerService _customerService;
        private readonly ILogger<CustomerController> _logger;

        /// <summary>
        /// Initializes a new instance of the CustomerController
        /// </summary>
        /// <param name="customerService">The customer service</param>
        /// <param name="logger">The logger instance</param>
        public CustomerController(ICustomerService customerService, ILogger<CustomerController> logger)
        {
            _customerService = customerService;
            _logger = logger;
        }
        private string GetFirstModelError()
        {
            return ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .FirstOrDefault() ?? "Invalid request data";
        }
        private Guid GetCustomerId()
        {
            var customerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(customerId))
                throw new UnauthorizedAccessException("User is not authenticated");

            return Guid.Parse(customerId);
        }
        /// <summary>
        /// Retrieves customer profile
        /// </summary>
        /// <returns>Customer profile details</returns>
        /// <response code="200">Returns the customer details</response>
        /// <response code="401">User is not authenticated or authorized</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("profile")]
        [ProducesResponseType(typeof(GetUserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<GetUserDto>> GetMyProfileAsync()
        {
            try
            {
                var customer = await _customerService.GetCustomerByIdAsync(GetCustomerId());
                return Ok(customer);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex) 
            { 
            
                _logger.LogError(ex, $"Failed to get profile for customer");
                return StatusCode(500, $"Failed to load profile");
            }
        }
        /// <summary>
        /// Creates the customer account
        /// </summary>
        /// <returns>The created account</returns>
        /// <remarks> 
        /// Sample request:
        ///     POST /api/customer/accounts
        ///     
        /// </remarks> 
        /// <response code="200">Account created successfully</response>
        /// <response code="400">Invalid request</response>
        /// <response code="401">User is not authenticated or authorized</response>
        /// <response code="500">Internal server error</response>
        [HttpPost("accounts")]
        [ProducesResponseType(typeof(GetAccountDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(string), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<GetAccountDto>> CreateAccountAsync()
        {
            try
            {

                var createdAccount =await _customerService.CreateAccountAsync(GetCustomerId());
                return Ok(createdAccount);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex) when (ExceptionHelper.IsBusinessException(ex))
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to create account for customer");

                return StatusCode(500, "Failed to create account");
            }
        }

        /// <summary>
        /// Sets a customer's primary account
        /// </summary>
        /// <param name="accountNumber">The account identifier to set as primary</param>
        /// <returns>Success message and updated customer details</returns>
        /// <remarks>
        /// Sample request:
        /// 
        ///     PUT /api/customer/accounts/current
        /// 
        /// </remarks>
        /// <response code="200">Primary account successfully set</response>
        /// <response code="400">Invalid request or business rule violation</response>
        /// <response code="401">User is not authenticated or authorized</response>
        /// <response code="404">Customer or account not found</response>
        /// <response code="500">Internal server error</response>
        [HttpPut("accounts/current")]
        [ProducesResponseType(typeof(GetUserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(string), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<GetUserDto>> SetPrimaryAccount(Guid accountNumber)
        {
            try
            {
                var customer=await _customerService.SetPrimaryAccountAsync(GetCustomerId(), accountNumber);
                return Ok(
                    new
                    {
                        message=$"Account {accountNumber} is successfully put as primary account",
                      customer=  customer
                    });
            }
            catch(KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex) when (ExceptionHelper.IsBusinessException(ex))
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to set primary account for customer");

                return StatusCode(500, "Failed to set primary account");
            }
        }

        /// <summary>
        /// Updates the customer's profile information
        /// </summary>
        /// <param name="updateRequest">The update request containing new profile data</param>
        /// <returns>Success message</returns>
        /// <remarks>
        /// Sample request:
        /// 
        ///     PUT /api/customer/profile
        ///     {
        ///        "firstName":"John",
        ///        "lastName":"Doe"
        ///     }
        /// </remarks> 
        /// <response code="200">Profile updated successfully</response>
        /// <response code="400">Invalid request data</response>
        /// <response code="401">User is not authenticated or authorized</response>
        /// <response code="404">Customer is not found</response>
        /// <response code="500">Internal server error</response>
        [HttpPut("profile")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(string), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateMyProfileAsync([FromBody]UpdateUserDto updateRequest)
        {
            if (!ModelState.IsValid)
                return BadRequest(GetFirstModelError());
            try
            {
                await _customerService.UpdateCustomerAsync(GetCustomerId(), updateRequest);
                return Ok(new { message="Successfully updated profile" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch(Exception ex) when (ExceptionHelper.IsBusinessException(ex))
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to update profile for customer");

                return StatusCode(500, "Failed to update profile");
            }
        }
        /// <summary>
        /// Retrieves current account
        /// </summary>
        /// <returns>Current account details</returns>
        /// <remarks>
        /// Sample request:
        ///     GET /api/customer/accounts/current
        /// </remarks> 
        /// <response code="200">Returns the current account details</response>
        /// <response code="401">User is not authenticated or authorized</response>
        /// <response code="404">User has no current account</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("accounts/current")]
        [ProducesResponseType(typeof(GetAccountDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<GetAccountDto>> GetMyCurrentAccountAsync()
        {
            try
            {
                var customerId = GetCustomerId();
                var account = await  _customerService.GetCurrentAccountAsync(customerId);
                return Ok(account);
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
                _logger.LogError(ex, $"Failed to get account for customer");
                return StatusCode(500, "Failed to retrieve account");
            }
        }

        /// <summary>
        /// Retrieves current account balance
        /// </summary>
        /// <returns>Current account balance</returns>
        /// <remarks>
        /// Sample request:
        ///     GET /api/customer/accounts/current/balance
        /// </remarks> 
        /// <response code="200">Returns the current account balance</response>
        /// <response code="401">User is not authenticated or authorized</response>
        /// <response code="404">User has no current account</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("accounts/current/balance")]
        [ProducesResponseType(typeof(decimal), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<decimal>> GetCurrentAccountBalanceAsync()
        {

            try
            {
                var customerId = GetCustomerId();


                decimal balance = await _customerService.GetCurrentAccountBalanceAsync(customerId);

                return Ok(balance);
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
                _logger.LogError(ex, $"Failed to get account balance for customer");
                return StatusCode(500, "Failed to retrieve your balance");
            }

        }
        /// <summary>
        /// Retrieves all customer accounts
        /// </summary>
        /// <returns>List of all customer's accounts</returns>
        /// <response code="200">Returns list of customer accounts</response>
        /// <response code="401">User is not authenticated or authorized</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("accounts")]
        [ProducesResponseType(typeof(IEnumerable<GetAccountDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<GetAccountDto>>> GetAllMyAccountsAsync()
        {
            try
            {
                var accounts = await _customerService.GetCustomerAccountsAsync(GetCustomerId());
                return Ok(accounts);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to get all accounts for customer");
                return StatusCode(500, "Failed to retrieve your accounts");
            }
        }


        /// <summary>
        /// Permanently deletes one of customer accounts
        /// </summary>
        /// <param name="accountNumber">The account number to delete</param>
        /// <returns>Success message with deleted account number</returns>
        /// <remarks>
        /// Sample request:
        ///     DELETE /api/customer/account/{e19add5a-472c-46eb-be47-b3c40c691b62}
        /// </remarks> 
        /// <response code="200">Account deleted successfully</response>
        /// <response code="400">Invalid operation</response>
        /// <response code="401">User is not authenticated or authorized</response>
        /// <response code="404">Account not found</response>
        /// <response code="500">Internal server error</response>
        [HttpDelete("accounts/{accountNumber}")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(string), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteAccountAsync([FromRoute] Guid accountNumber)
        {
            try
            {
                await _customerService.DeleteAccountAsync(accountNumber);
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
                _logger.LogError(ex, $"Failed to delete account for customer");
                return StatusCode(500, $"Failed to delete account with number {accountNumber}");
            }
        }
        /// <summary>
        /// Retrieves customer's transaction history
        /// </summary>
        /// <returns>List of customer's transactions</returns>
        /// <response code="200">Returns list of transactions</response>
        /// <response code="401">User is not authenticated or authorized</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("transactions")]
        [ProducesResponseType(typeof(IEnumerable<AuditLog>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<AuditLog>>> GetMyTransactionsHistoryAsync()
        {
         
            try
            {
                var customerId = GetCustomerId();
                
                var transactionHistory = await _customerService.GetTransactionHistoryAsync(customerId);
                return Ok(transactionHistory);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                    _logger.LogError(ex, $"Failed to get transaction history for customer");
                return StatusCode(500, "Failed to retrieve your transaction history");
            }
        }
       
        /// <summary>
        /// Deposits amount of money from customer's current account to a specific account
        /// </summary>
        /// <param name="request">Transfer request model</param>
        /// <returns>Success message with account number </returns>
        /// <remarks>
        /// Sample request:
        ///     POST /api/customer/accounts/current/transfers
        ///     {
        ///         "toAccount":"e19add5a-472c-46eb-be47-b3c40c691b62",
        ///         "amount":0.00
        ///     }
        /// </remarks> 
        /// /// <response code="200">Amount is transferred successfully to the account</response>
        /// <response code="400">Invalid operation (e.g.,one of the accounts is inactive)</response>
        /// <response code="401">User is not authenticated or authorized</response>
        /// <response code="404">Any of the accounts is not found</response>
        /// <response code="500">Internal server error</response>
        [HttpPost("accounts/current/transfers")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(string), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult>TransferFromMyAccountAsync([FromBody] TransferRequestModel request)
        {
            if (!ModelState.IsValid)
                return BadRequest(GetFirstModelError());

            try
            {
                var customerId = GetCustomerId();
                
                await _customerService.TransferAsync(customerId,request);
                return Ok(new 
                {
                    message = $"Successfully transferred {request.Amount} to {request.ToAccount}",
                    request.Amount,
                    request.ToAccount,
                });
            }
            catch(UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch(Exception ex) when (ExceptionHelper.IsBusinessException(ex))// a feedback exception, to show to user for better exp.
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to transfer {request.Amount}  to account  {request.ToAccount}");
                return StatusCode(500, $"Failed to process transfer");
            }
        }

    }
}
