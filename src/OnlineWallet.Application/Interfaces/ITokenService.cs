using OnlineWallet.Application.DTOs;
using OnlineWallet.Application.Models;
using OnlineWallet.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlineWallet.Application.Interfaces
{
    /// <summary>
    /// Service interface for token operations.
    /// Handles JWT token generation for authenticated users.
    /// </summary>
    public interface ITokenService
    {
        /// <summary>
        /// Generates a JWT token for a user.
        /// </summary>
        /// <param name="user">User details for token claims</param>
        /// <returns>Token response containing JWT token and expiration</returns>
        TokenResponseModel GetToken(GetUserDto user);
        //Task<bool> IsTokenValidAsync(string token);
    }
}
