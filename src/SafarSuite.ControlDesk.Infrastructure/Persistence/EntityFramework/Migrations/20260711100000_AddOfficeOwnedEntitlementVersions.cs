using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework.Migrations;

[DbContext(typeof(ControlDeskDbContext))]
[Migration("20260711100000_AddOfficeOwnedEntitlementVersions")]
public sealed class AddOfficeOwnedEntitlementVersions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateSequence<long>(
            name: "entitlement_version_sequence",
            schema: "control");

        migrationBuilder.AddColumn<long>(
            name: "entitlement_version",
            schema: "control",
            table: "entitlement_snapshots",
            type: "bigint",
            nullable: false,
            defaultValue: 0L);

        migrationBuilder.Sql(
            """
            WITH ranked AS
            (
                SELECT entitlement_snapshot_id,
                       ROW_NUMBER() OVER (ORDER BY issued_at_utc, entitlement_snapshot_id)::bigint AS entitlement_version
                FROM control.entitlement_snapshots
            )
            UPDATE control.entitlement_snapshots AS snapshots
            SET entitlement_version = ranked.entitlement_version
            FROM ranked
            WHERE snapshots.entitlement_snapshot_id = ranked.entitlement_snapshot_id;

            SELECT setval(
                'control.entitlement_version_sequence',
                GREATEST(
                    COALESCE((SELECT MAX(entitlement_version) FROM control.entitlement_snapshots), 1),
                    1),
                EXISTS(SELECT 1 FROM control.entitlement_snapshots));

            ALTER TABLE control.entitlement_snapshots
            ALTER COLUMN entitlement_version DROP DEFAULT;
            """);

        migrationBuilder.CreateIndex(
            name: "ux_entitlement_snapshots_client_version",
            schema: "control",
            table: "entitlement_snapshots",
            columns: new[] { "client_id", "entitlement_version" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ux_entitlement_snapshots_client_version",
            schema: "control",
            table: "entitlement_snapshots");

        migrationBuilder.DropColumn(
            name: "entitlement_version",
            schema: "control",
            table: "entitlement_snapshots");

        migrationBuilder.DropSequence(
            name: "entitlement_version_sequence",
            schema: "control");
    }
}
