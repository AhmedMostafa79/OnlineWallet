using Microsoft.AspNetCore.Identity;
using OnlineWallet.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlineWallet.Application.Services
{
    /// <summary>
    /// Service implementation for password hashing and verification.
    /// Uses ASP.NET Core Identity PasswordHasher for cryptographic operations.
    /// </summary>
    /// <remarks>
    /// Uses ASP.NET Core Identity's secure password hashing with salt and iteration count.
    /// Supports password rehashing when algorithms are upgraded.
    /// </remarks>
    public class HashService:IHashService
    {
        private readonly PasswordHasher<object> _passwordHasher;

        /// <summary>
        /// Initializes a new instance of HashService.
        /// Creates PasswordHasher for cryptographic operations.
        /// </summary>
        public HashService(PasswordHasher<object> passwordHasher)
        {
            _passwordHasher = passwordHasher;
        }

        /// <summary>
        /// Hashes a plain text password using ASP.NET Core Identity PasswordHasher cryptographic algorithm.
        /// </summary>
        /// <param name="password">Plain text password to hash</param>
        /// <returns>Hashed password string</returns>
        /// <exception cref="ArgumentException">Thrown when password is null or empty</exception>
        public string HashPassword(string password)
        {
            if(string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password cannot be null or empty", nameof(password));

            return _passwordHasher.HashPassword(null,password);
        }

        /// <summary>
        /// Verifies a plain text password against a hashed password.
        /// </summary>
        /// <param name="password">Plain text password to verify</param>
        /// <param name="hashedPassword">Hashed password to compare against</param>
        /// <returns>True if password matches the hash, false otherwise</returns>
        public bool VerifyPassword(string password,string hashedPassword)
        {
            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hashedPassword))
                return false;

            var result = _passwordHasher.VerifyHashedPassword(null, hashedPassword, password);

            return result == PasswordVerificationResult.Success ||
               result == PasswordVerificationResult.SuccessRehashNeeded;
        }

    }
}
