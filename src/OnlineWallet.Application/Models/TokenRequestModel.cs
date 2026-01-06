using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlineWallet.Application.Models
{
    /// <summary>
    /// Token request model.
    /// Contains credentials for authentication token generation.
    /// </summary>
    public class TokenRequestModel
    {
        /// <summary>
        /// User's email address.
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// User's password.
        /// </summary>
        public string Password { get; set; }
    }
}
