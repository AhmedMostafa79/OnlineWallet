using OnlineWallet.Application.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace OnlineWallet.Application.Interfaces
{
    /// <summary>
    /// Service interface for authentication operations.
    /// Handles user authentication functionality (e.g. registration and login operations) .
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// Registers a new user in the system.
        /// </summary>
        /// <param name="model">User registration details</param>
        /// <returns>Authentication response with token and user details</returns>
        public Task<AuthResponseModel> RegisterAsync(RegisterModel model);

        /// <summary>
        /// Authenticates an existing user.
        /// </summary>
        /// <param name="model">User login credentials</param>
        /// <returns>Authentication response with token and user details</returns>
        public Task<AuthResponseModel> LoginAsync(TokenRequestModel model);

    }
}
