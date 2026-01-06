using OnlineWallet.Application.DTOs;
using OnlineWallet.Application.Models;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlineWallet.Application.Interfaces.TokenStrategies
{
    /// <summary>
    /// Interface for JWT token generation strategies.
    /// Defines methods for creating JWT tokens and token responses.
    /// </summary>
    public interface IJwtTokenGenerator
    {
        /// <summary>
        /// Generates a JWT security token for a user.
        /// </summary>
        /// <param name="user">User details for token claims</param>
        /// <returns>JWT security token</returns>
        public JwtSecurityToken GenerateToken(GetUserDto user);

        /// <summary>
        /// Creates a complete token response for a user.
        /// </summary>
        /// <param name="user">User details for token generation</param>
        /// <returns>Token response containing token and expiration information</returns>
        public TokenResponseModel GetToken(GetUserDto user);

    }
}
