using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlineWallet.Application.Helpers
{
    /// <summary>
    /// JWT configuration settings.
    /// Contains settings for JSON Web Token generation and validation.
    /// </summary>
    public class JWT
    {
        /// <summary>
        /// Secret key used to sign JWT tokens.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Token issuer identifier.
        /// </summary>
        public string Issuer {  get; set; }

        /// <summary>
        /// Token audience identifier.
        /// </summary>
        public string Audience { get; set; }

          /// <summary>
        /// Token validity duration in minutes.
        /// </summary>
        public double DurationInMinutes { get; set; }
    }
}
