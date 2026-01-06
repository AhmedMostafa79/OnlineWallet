using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlineWallet.Domain.Enums
{
    /// <summary>
    /// Status of an audited action.
    /// Indicates the outcome of logged operations.
    /// </summary>
    public enum AuditLogStatus
    {
        /// <summary>
        /// Action did not complete successfully.
        /// </summary>
        Failed = 0,

        /// <summary>
        /// Action completed successfully.
        /// </summary>
        Success = 1
    }
}
