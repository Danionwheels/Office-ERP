using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ControlDeskDbContext))]
    [Migration("20260703140000_AddCompanyAccountCodeRanges")]
    public partial class AddCompanyAccountCodeRanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "account_code_ranges",
                schema: "control",
                columns: table => new
                {
                    account_code_range_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    role = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    display_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    search_prefix = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    range_start = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    range_end = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    code_length = table.Column<int>(type: "integer", nullable: false),
                    account_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    normal_balance = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    is_posting_account = table.Column<bool>(type: "boolean", nullable: false),
                    parent_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_account_code_ranges", x => x.account_code_range_id);
                });

            migrationBuilder.CreateIndex(
                name: "ux_account_code_ranges_company_role",
                schema: "control",
                table: "account_code_ranges",
                columns: new[] { "company_code", "role" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "account_code_ranges",
                schema: "control");
        }
    }
}
