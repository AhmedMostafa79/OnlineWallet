using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlineWallet.Domain.Enums
{
    /// <summary>
    /// Defines user roles in the Online Wallet system
    /// </summary>
    public enum UserRole
    {
        /// <summary>
        /// Standard wallet user with transaction capabilities
        /// </summary>
        Customer = 0,

        /// <summary>
        /// Administrative user with system management capabilities
        /// </summary>
        Admin = 1,

        /// <summary>
        /// Manager user with full system management capabilities
        /// </summary>
        Manager = 2,
    }
}
