using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OnlineWallet.Domain.Enums;
using OnlineWallet.Domain.Models;
namespace OnlineWallet.Infrastructure
{
    /// <summary>
    /// Entity Framework database context for the Online Wallet application.
    /// Defines database tables and entity configurations.
    /// </summary>
    public class WalletDbContext:DbContext
    {
        /// <summary>
        /// Users table in the database.
        /// </summary>
        public DbSet<User> Users { get; set; }

        /// <summary>
        /// Accounts table in the database.
        /// </summary>
        public DbSet<Account> Accounts { get; set; }

        /// <summary>
        /// Audit logs table in the database.
        /// </summary>
        public DbSet<AuditLog> AuditLogs { get; set; }

        /// <summary>
        /// Initializes a new instance of WalletDbContext.
        /// </summary>
        /// <param name="options">Database context configuration options</param>
        public WalletDbContext(DbContextOptions<WalletDbContext> options) : base(options)
        {
        }

      
    }
}
