using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineWallet.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddForeignKeyConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add foreign key for Account.OwnerId → Users.Id

            migrationBuilder.AddForeignKey( 
                name: "FK_Accounts_Users_OwnerId",
                table: "Accounts",
                column: "OwnerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            // Add foreign key for AuditLogs.PerformedBy → Users.Id
            migrationBuilder.AddForeignKey(
                name: "FK_AuditLogs_Users_PerformedBy",
                table: "AuditLogs",
                column: "PerformedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
              name: "FK_AuditLogs_Users_PerformedBy",
              table: "AuditLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_Accounts_Users_OwnerId",
                table: "Accounts");
        }
    }
}
