using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlineWallet.Domain.Enums
{
    // <summary>
    /// Types of notifications sent to users.
    /// Categorizes notification content and purpose.
    /// </summary>
    public enum NotificationType
    {
        /// <summary>
        /// Notification about a deposit transaction.
        /// </summary>
        Deposit = 0,

        /// <summary>
        /// Notification about a transfer transaction.
        /// </summary>
        Transfer = 1,

        /// <summary>
        /// Alert notification requiring user attention.
        /// </summary>
        Alert = 10,

        /// <summary>
        /// Informational notification.
        /// </summary>
        Info = 11,
    }
}
