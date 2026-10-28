using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

using System.Text.RegularExpressions;

namespace tickets_core.Infrastructure.Persistence.Migrations
{
    public partial class AllowTicketStateRollback : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            InstallProcedure(migrationBuilder, "TicketStateRollbackObjects.sql");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            InstallProcedure(migrationBuilder, "TicketAuditObjects.sql");
        }

        private static void InstallProcedure(MigrationBuilder migrationBuilder, string resource)
        {
            using var stream = typeof(AllowTicketStateRollback).Assembly.GetManifestResourceStream(
                "tickets_core.Infrastructure.Persistence.Migrations." + resource)!;
            using var reader = new StreamReader(stream);
            foreach (var batch in Regex.Split(reader.ReadToEnd(), @"^GO\s*$", RegexOptions.Multiline))
                if (batch.Contains("CREATE OR ALTER PROCEDURE dbo.SP_ChangeTicketState"))
                    migrationBuilder.Sql("EXEC(N'" + batch.Replace("'", "''") + "');");
        }
    }
}
