using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class AddEffectiveAccessScheduling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "effective_from_utc",
                schema: "control",
                table: "entitlement_snapshots",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "effective_from_utc",
                schema: "control",
                table: "client_access_revisions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE control.entitlement_snapshots
                SET effective_from_utc = issued_at_utc
                WHERE effective_from_utc IS NULL;

                UPDATE control.client_access_revisions
                SET effective_from_utc = approved_at_utc
                WHERE effective_from_utc IS NULL;
                """);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "effective_from_utc",
                schema: "control",
                table: "entitlement_snapshots",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "effective_from_utc",
                schema: "control",
                table: "client_access_revisions",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_entitlement_snapshots_client_effective_from",
                schema: "control",
                table: "entitlement_snapshots",
                columns: new[] { "client_id", "effective_from_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_client_access_revisions_client_effective_from",
                schema: "control",
                table: "client_access_revisions",
                columns: new[] { "client_id", "effective_from_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_entitlement_snapshots_client_effective_from",
                schema: "control",
                table: "entitlement_snapshots");

            migrationBuilder.DropIndex(
                name: "ix_client_access_revisions_client_effective_from",
                schema: "control",
                table: "client_access_revisions");

            migrationBuilder.DropColumn(
                name: "effective_from_utc",
                schema: "control",
                table: "entitlement_snapshots");

            migrationBuilder.DropColumn(
                name: "effective_from_utc",
                schema: "control",
                table: "client_access_revisions");
        }
    }
}
