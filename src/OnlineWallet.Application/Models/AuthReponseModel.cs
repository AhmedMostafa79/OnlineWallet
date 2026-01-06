using OnlineWallet.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlineWallet.Application.Models
{
    /// <summary>
    /// Authentication response model.
    /// Contains authentication results and user session information.
    /// </summary>
    public class AuthResponseModel
    {
        /// <summary>
        /// Authentication status message.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Indicates if authentication was successful.
        /// </summary>
        public bool IsAuthenticated { get; set; }

        /// <summary>
        /// Authenticated user's username.
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// Authenticated user's role.
        /// </summary>
        public UserRole Role { get; set; }

        /// <summary>
        /// JWT token for authenticated session.
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// Authenticated user's email.
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Token expiration timestamp.
        /// </summary>
        public DateTime ExpiresIn { get; set; }
    }
}