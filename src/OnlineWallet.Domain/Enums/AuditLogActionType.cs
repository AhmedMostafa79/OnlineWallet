using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlineWallet.Domain.Enums
{
    /// <summary>
    /// Types of actions that can be recorded in audit logs.
    /// Categorizes system activities for tracking and reporting.
    /// </summary>
    public enum AuditLogActionType
    {
        /// <summary>
        /// Funds added to an account.
        /// </summary>
        Deposit = 0,

        /// <summary>
        /// Funds transferred between accounts.
        /// </summary>
        Transfer = 1,

        /// <summary>
        /// New customer user created.
        /// </summary>
        CustomerCreation = 10,

        /// <summary>
        /// Customer user deleted.
        /// </summary>
        CustomerDeletion = 11,

        /// <summary>
        /// Customer created new financial account.
        /// </summary>
        CustomerAccountCreation = 20,

        /// <summary>
        /// Account deleted by an customer.
        /// </summary>
        CustomerAccountDeletion = 21,

        /// <summary>
        /// Administrator user deleted.
        /// </summary>
        AdminDeletion = 30,

        /// <summary>
        /// New administrator user created.
        /// </summary>
        AdminCreation = 31,

        /// <summary>
        /// Customer deleted by an administrator.
        /// </summary>
        AdminCustomerDeletion = 32,

        /// <summary>
        /// Account deleted by an administrator.
        /// </summary>
        AdminAccountDeletion = 33,

        /// <summary>
        /// Account activated by an administrator.
        /// </summary>
        AdminAccountActivation = 34,

        /// <summary>
        /// Account deactivated by an administrator.
        /// </summary>
        AdminAccountDeactivation = 35,
    }
}
