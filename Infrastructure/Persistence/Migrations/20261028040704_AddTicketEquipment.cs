using System.IO;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace tickets_core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketEquipment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Equipment",
                schema: "dbo",
                table: "TICKET",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.Sql("UPDATE dbo.TICKET SET Equipment = N'Sin especificar' WHERE Equipment IS NULL;");
            migrationBuilder.AlterColumn<string>(
                name: "Equipment",
                schema: "dbo",
                table: "TICKET",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
            InstallProcedures(migrationBuilder, "TicketEquipmentObjects.sql");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            InstallProcedures(migrationBuilder, "TicketAuditObjects.sql");
            migrationBuilder.DropColumn(
                name: "Equipment",
                schema: "dbo",
                table: "TICKET");
        }
        private static void InstallProcedures(MigrationBuilder migrationBuilder, string resource)
        {
            using var stream = typeof(AddTicketEquipment).Assembly.GetManifestResourceStream(
                "tickets_core.Infrastructure.Persistence.Migrations." + resource)!;
            using var reader = new StreamReader(stream);
            foreach (var batch in Regex.Split(reader.ReadToEnd(), @"^GO\s*$", RegexOptions.Multiline))
                if (batch.Contains("CREATE OR ALTER PROCEDURE dbo.SP_EditTicket") ||
                    batch.Contains("CREATE OR ALTER PROCEDURE dbo.SP_GetTicketsGroupedByState"))
                    migrationBuilder.Sql("EXEC(N'" + batch.Replace("'", "''") + "');");
        }
    }
}
