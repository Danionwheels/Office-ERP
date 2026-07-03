using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ControlDeskDbContext))]
    [Migration("20260703161000_AddAccountingPeriodCloseArtifacts")]
    public partial class AddAccountingPeriodCloseArtifacts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "accounting_period_close_artifacts",
                schema: "control",
                columns: table => new
                {
                    accounting_period_close_artifact_row_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    generated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    generated_by = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    check_count = table.Column<int>(type: "integer", nullable: false),
                    blocked_check_count = table.Column<int>(type: "integer", nullable: false),
                    currency_count = table.Column<int>(type: "integer", nullable: false),
                    posted_journal_count = table.Column<int>(type: "integer", nullable: false),
                    draft_journal_count = table.Column<int>(type: "integer", nullable: false),
                    snapshot_json = table.Column<string>(type: "text", nullable: false),
                    accounting_period_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_accounting_period_close_artifacts", x => x.accounting_period_close_artifact_row_id);
                    table.ForeignKey(
                        name: "FK_accounting_period_close_artifacts_accounting_periods_accou~",
                        column: x => x.accounting_period_id,
                        principalSchema: "control",
                        principalTable: "accounting_periods",
                        principalColumn: "accounting_period_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_accounting_period_close_artifacts_period_generated",
                schema: "control",
                table: "accounting_period_close_artifacts",
                columns: new[] { "accounting_period_id", "generated_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "accounting_period_close_artifacts",
                schema: "control");
        }
    }
}
