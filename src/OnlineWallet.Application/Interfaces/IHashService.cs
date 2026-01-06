using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlineWallet.Application.Interfaces
{
    /// <summary>
    /// Service interface for password hashing and verification.
    /// Provides cryptographic password security operations.
    /// </summary>
    public interface IHashService
    {

        /// <summary>
        /// Hashes a plain text password using secure cryptographic algorithm.
        /// </summary>
        /// <param name="password">Plain text password to hash</param>
        /// <returns>Hashed password string</returns>
        string HashPassword(string password);

        /// <summary>
        /// Verifies a plain text password against a hashed password.
        /// </summary>
        /// <param name="password">Plain text password to verify</param>
        /// <param name="hashedPassword">Hashed password to compare against</param>
        /// <returns>True if password matches the hash, false otherwise</returns>
        bool VerifyPassword(string password, string hashedPassword);
    }
}
