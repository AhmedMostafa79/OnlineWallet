using OnlineWallet.Application.DTOs;
using OnlineWallet.Application.Interfaces;
using OnlineWallet.Application.Interfaces.TokenStrategies;
using OnlineWallet.Application.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlineWallet.Application.Services
{
    /// <summary>
    /// Service implementation for token operations.
    /// Handles JWT token generation using configured token strategies.
    /// </summary>
    public class TokenService : ITokenService
    {
        private readonly IJwtTokenGenerator _jwtGenerator;

        /// <summary>
        /// Initializes a new instance of TokenService.
        /// </summary>
        /// <param name="jwtGenerator">JWT token generation strategy</param>
        public TokenService(IJwtTokenGenerator jwtGenerator)
        {
            _jwtGenerator = jwtGenerator;
        }

        /// <summary>
        /// Generates a JWT token for a user using the configured token strategy.
        /// </summary>
        /// <param name="user">User details for token claims</param>
        /// <returns>Token response containing JWT token and expiration</returns>
        public TokenResponseModel GetToken(GetUserDto user)
        {
            return _jwtGenerator.GetToken(user);
        }
    }
}