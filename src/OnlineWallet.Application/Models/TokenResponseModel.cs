using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlineWallet.Application.Models
{
    /// <summary>
    /// Token response model.
    /// Contains authentication token information.
    /// </summary>
    public class TokenResponseModel
    {
        /// <summary>
        /// JWT authentication token.
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// Token expiration timestamp.
        /// </summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// Type of token (e.g., "Bearer").
        /// </summary>
        public string TokenType { get; set; }
    }
}
