using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class AddContractRevisionToBundleIssues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "contract_revision_number",
                schema: "cloud",
                table: "entitlement_bundle_issues",
                type: "bigint",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE cloud.entitlement_bundle_issues
                SET contract_revision_number = CASE
                    WHEN payload_json ->> 'contractRevisionNumber' ~ '^[0-9]+$'
                        THEN (payload_json ->> 'contractRevisionNumber')::bigint
                    ELSE 0
                END;
                """);

            migrationBuilder.AlterColumn<long>(
                name: "contract_revision_number",
                schema: "cloud",
                table: "entitlement_bundle_issues",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "contract_revision_number",
                schema: "cloud",
                table: "entitlement_bundle_issues");
        }
    }
}
