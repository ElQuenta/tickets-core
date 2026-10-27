using System;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace tickets_core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TicketAuditAndSeeds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                schema: "dbo",
                table: "TICKET",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "dbo",
                table: "TICKET",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                schema: "dbo",
                table: "TICKET",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                schema: "dbo",
                table: "STATE",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                schema: "dbo",
                table: "STATE",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                schema: "dbo",
                table: "HISTORY",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                schema: "dbo",
                table: "HISTORY",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                schema: "dbo",
                table: "AspNetUserTokens",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                schema: "dbo",
                table: "AspNetUserTokens",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                schema: "dbo",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                schema: "dbo",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                schema: "dbo",
                table: "AspNetUserRoles",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                schema: "dbo",
                table: "AspNetUserRoles",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                schema: "dbo",
                table: "AspNetUserLogins",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                schema: "dbo",
                table: "AspNetUserLogins",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                schema: "dbo",
                table: "AspNetUserClaims",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                schema: "dbo",
                table: "AspNetUserClaims",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                schema: "dbo",
                table: "AspNetRoles",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                schema: "dbo",
                table: "AspNetRoles",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                schema: "dbo",
                table: "AspNetRoleClaims",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                schema: "dbo",
                table: "AspNetRoleClaims",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");
            using var stream = typeof(TicketAuditAndSeeds).Assembly.GetManifestResourceStream(
                "tickets_core.Infrastructure.Persistence.Migrations.TicketAuditObjects.sql")!;
            using var reader = new StreamReader(stream);
            foreach (var batch in Regex.Split(reader.ReadToEnd(), @"^GO\s*$", RegexOptions.Multiline))
                if (!string.IsNullOrWhiteSpace(batch))
                    migrationBuilder.Sql("EXEC(N'" + batch.Replace("'", "''") + "');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.SP_ChangeTicketState;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.SP_EditTicket;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.SP_DeleteTicket;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.SP_GetTicketsGroupedByState;");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS dbo.TG_TICKET_Timestamps;");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS dbo.TG_HISTORY_Timestamps;");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS dbo.TG_STATE_Timestamps;");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS dbo.TG_AspNetUsers_Timestamps;");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS dbo.TG_AspNetRoles_Timestamps;");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS dbo.TG_AspNetUserRoles_Timestamps;");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS dbo.TG_AspNetUserClaims_Timestamps;");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS dbo.TG_AspNetUserLogins_Timestamps;");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS dbo.TG_AspNetUserTokens_Timestamps;");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS dbo.TG_AspNetRoleClaims_Timestamps;");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "dbo",
                table: "TICKET");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "dbo",
                table: "TICKET");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "dbo",
                table: "TICKET");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "dbo",
                table: "STATE");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "dbo",
                table: "STATE");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "dbo",
                table: "HISTORY");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "dbo",
                table: "HISTORY");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "dbo",
                table: "AspNetUserTokens");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "dbo",
                table: "AspNetUserTokens");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "dbo",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "dbo",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "dbo",
                table: "AspNetUserRoles");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "dbo",
                table: "AspNetUserRoles");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "dbo",
                table: "AspNetUserLogins");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "dbo",
                table: "AspNetUserLogins");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "dbo",
                table: "AspNetUserClaims");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "dbo",
                table: "AspNetUserClaims");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "dbo",
                table: "AspNetRoles");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "dbo",
                table: "AspNetRoles");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "dbo",
                table: "AspNetRoleClaims");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "dbo",
                table: "AspNetRoleClaims");
        }
    }
}
