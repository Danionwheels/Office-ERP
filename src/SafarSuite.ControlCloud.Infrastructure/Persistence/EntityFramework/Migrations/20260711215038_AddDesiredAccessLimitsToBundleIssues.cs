using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class AddDesiredAccessLimitsToBundleIssues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "allowed_concurrent_users",
                schema: "cloud",
                table: "entitlement_bundle_issues",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "allowed_named_users",
                schema: "cloud",
                table: "entitlement_bundle_issues",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "feature_limit_count",
                schema: "cloud",
                table: "entitlement_bundle_issues",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddCheckConstraint(
                name: "ck_entitlement_bundle_issues_concurrent_users",
                schema: "cloud",
                table: "entitlement_bundle_issues",
                sql: "allowed_concurrent_users IS NULL OR allowed_concurrent_users >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_entitlement_bundle_issues_feature_limit_count",
                schema: "cloud",
                table: "entitlement_bundle_issues",
                sql: "feature_limit_count >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_entitlement_bundle_issues_named_users",
                schema: "cloud",
                table: "entitlement_bundle_issues",
                sql: "allowed_named_users IS NULL OR allowed_named_users >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_entitlement_bundle_issues_user_limit_order",
                schema: "cloud",
                table: "entitlement_bundle_issues",
                sql: "allowed_named_users IS NULL OR allowed_concurrent_users IS NULL OR allowed_concurrent_users <= allowed_named_users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_entitlement_bundle_issues_concurrent_users",
                schema: "cloud",
                table: "entitlement_bundle_issues");

            migrationBuilder.DropCheckConstraint(
                name: "ck_entitlement_bundle_issues_feature_limit_count",
                schema: "cloud",
                table: "entitlement_bundle_issues");

            migrationBuilder.DropCheckConstraint(
                name: "ck_entitlement_bundle_issues_named_users",
                schema: "cloud",
                table: "entitlement_bundle_issues");

            migrationBuilder.DropCheckConstraint(
                name: "ck_entitlement_bundle_issues_user_limit_order",
                schema: "cloud",
                table: "entitlement_bundle_issues");

            migrationBuilder.DropColumn(
                name: "allowed_concurrent_users",
                schema: "cloud",
                table: "entitlement_bundle_issues");

            migrationBuilder.DropColumn(
                name: "allowed_named_users",
                schema: "cloud",
                table: "entitlement_bundle_issues");

            migrationBuilder.DropColumn(
                name: "feature_limit_count",
                schema: "cloud",
                table: "entitlement_bundle_issues");
        }
    }
}
