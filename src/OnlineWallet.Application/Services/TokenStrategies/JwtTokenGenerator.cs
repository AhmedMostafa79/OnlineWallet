using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OnlineWallet.Application.DTOs;
using OnlineWallet.Application.Helpers;
using OnlineWallet.Application.Interfaces.TokenStrategies;
using OnlineWallet.Application.Models;
using OnlineWallet.Domain.Models;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace OnlineWallet.Application.Services.TokenStrategies
{
    /// <summary>
    /// Implementation of JWT token generation strategy.
    /// Creates JWT tokens with user claims and signing credentials.
    /// </summary>
    public class JwtTokenGenerator : IJwtTokenGenerator
    {
        private readonly JWT _jwt;

        /// <summary>
        /// Initializes a new instance of JwtTokenGenerator.
        /// </summary>
        /// <param name="jwt">JWT configuration settings</param>
        public JwtTokenGenerator(IOptions<JWT> jwt)
        {
            _jwt = jwt.Value;
        }

        /// <summary>
        /// Generates a JWT security token for a user.
        /// </summary>
        /// <param name="user">User details for token claims</param>
        /// <returns>JWT security token with user claims</returns>
        public JwtSecurityToken GenerateToken(GetUserDto user)
        {
            var roleClaims = new List<Claim>();
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub,user.Id.ToString()),
                new Claim(ClaimTypes.NameIdentifier,user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti,Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Email,user.Email),
                new Claim(ClaimTypes.Role,user.Role.ToString()),
                new Claim(ClaimTypes.Name,$"{user.FirstName} {user.LastName}"),
            };
            var symmetricSecurityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key));

            var signingCredentials = new SigningCredentials(symmetricSecurityKey, SecurityAlgorithms.HmacSha256);

            var jwtSecurityToken = new JwtSecurityToken(
                issuer: _jwt.Issuer,
                audience: _jwt.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwt.DurationInMinutes),
                signingCredentials: signingCredentials
                );
            return jwtSecurityToken;
        }

        /// <summary>
        /// Creates a complete token response for a user.
        /// </summary>
        /// <param name="user">User details for token generation</param>
        /// <returns>Token response containing serialized token and expiration information</returns>
        public TokenResponseModel GetToken(GetUserDto user)
        {
            var jwtToken = GenerateToken(user);
            return new TokenResponseModel
            {
                Token = new JwtSecurityTokenHandler().WriteToken(jwtToken),
                ExpiresAt = jwtToken.ValidTo,
                TokenType = "Bearer"
            };
        }

    }
}
