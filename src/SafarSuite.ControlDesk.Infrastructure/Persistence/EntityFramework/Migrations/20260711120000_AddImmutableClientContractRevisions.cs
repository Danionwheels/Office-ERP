using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class AddImmutableClientContractRevisions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "approval_reason",
                schema: "control",
                table: "client_contracts",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "approved_at_utc",
                schema: "control",
                table: "client_contracts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "approved_by",
                schema: "control",
                table: "client_contracts",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "revision_number",
                schema: "control",
                table: "client_contracts",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "supersedes_contract_id",
                schema: "control",
                table: "client_contracts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "contract_revision_number",
                schema: "control",
                table: "client_access_revisions",
                type: "bigint",
                nullable: true);

            migrationBuilder.Sql(
                """
                WITH ordered_contracts AS (
                    SELECT
                        contract_id,
                        ROW_NUMBER() OVER (
                            PARTITION BY client_id
                            ORDER BY COALESCE(activated_at_utc, created_at_utc), created_at_utc, contract_id
                        ) AS revision_number,
                        LAG(contract_id) OVER (
                            PARTITION BY client_id
                            ORDER BY COALESCE(activated_at_utc, created_at_utc), created_at_utc, contract_id
                        ) AS supersedes_contract_id
                    FROM control.client_contracts
                )
                UPDATE control.client_contracts AS contract
                SET
                    revision_number = ordered.revision_number,
                    supersedes_contract_id = ordered.supersedes_contract_id,
                    approved_by = 'migration:legacy-contract',
                    approval_reason = 'Imported existing contract as an immutable commercial revision.',
                    approved_at_utc = COALESCE(contract.activated_at_utc, contract.created_at_utc)
                FROM ordered_contracts AS ordered
                WHERE ordered.contract_id = contract.contract_id;

                UPDATE control.client_access_revisions AS access_revision
                SET contract_revision_number = contract.revision_number
                FROM control.client_contracts AS contract
                WHERE contract.contract_id = access_revision.contract_id;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "approval_reason",
                schema: "control",
                table: "client_contracts",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "approved_at_utc",
                schema: "control",
                table: "client_contracts",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "approved_by",
                schema: "control",
                table: "client_contracts",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "revision_number",
                schema: "control",
                table: "client_contracts",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "contract_revision_number",
                schema: "control",
                table: "client_access_revisions",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.Sql(
                """
                ALTER TABLE control.client_contracts
                    ADD CONSTRAINT ck_client_contracts_revision_positive
                    CHECK (revision_number > 0);

                ALTER TABLE control.client_contracts
                    ADD CONSTRAINT ck_client_contracts_approval_nonempty
                    CHECK (btrim(approved_by) <> '' AND btrim(approval_reason) <> '');

                ALTER TABLE control.client_access_revisions
                    ADD CONSTRAINT ck_client_access_revisions_contract_revision_positive
                    CHECK (contract_revision_number > 0);

                CREATE UNIQUE INDEX ux_client_contracts_client_root
                    ON control.client_contracts (client_id)
                    WHERE supersedes_contract_id IS NULL;
                """);

            migrationBuilder.CreateIndex(
                name: "ux_client_contracts_client_active",
                schema: "control",
                table: "client_contracts",
                column: "client_id",
                unique: true,
                filter: "status = 'Active'");

            migrationBuilder.CreateIndex(
                name: "ux_client_contracts_client_revision",
                schema: "control",
                table: "client_contracts",
                columns: new[] { "client_id", "revision_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_client_contracts_supersedes",
                schema: "control",
                table: "client_contracts",
                column: "supersedes_contract_id",
                unique: true,
                filter: "supersedes_contract_id IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_client_contracts_client_contracts_supersedes_contract_id",
                schema: "control",
                table: "client_contracts",
                column: "supersedes_contract_id",
                principalSchema: "control",
                principalTable: "client_contracts",
                principalColumn: "contract_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP INDEX IF EXISTS control.ux_client_contracts_client_root;
                ALTER TABLE control.client_access_revisions
                    DROP CONSTRAINT IF EXISTS ck_client_access_revisions_contract_revision_positive;
                ALTER TABLE control.client_contracts
                    DROP CONSTRAINT IF EXISTS ck_client_contracts_approval_nonempty;
                ALTER TABLE control.client_contracts
                    DROP CONSTRAINT IF EXISTS ck_client_contracts_revision_positive;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_client_contracts_client_contracts_supersedes_contract_id",
                schema: "control",
                table: "client_contracts");

            migrationBuilder.DropIndex(
                name: "ux_client_contracts_client_active",
                schema: "control",
                table: "client_contracts");

            migrationBuilder.DropIndex(
                name: "ux_client_contracts_client_revision",
                schema: "control",
                table: "client_contracts");

            migrationBuilder.DropIndex(
                name: "ux_client_contracts_supersedes",
                schema: "control",
                table: "client_contracts");

            migrationBuilder.DropColumn(
                name: "approval_reason",
                schema: "control",
                table: "client_contracts");

            migrationBuilder.DropColumn(
                name: "approved_at_utc",
                schema: "control",
                table: "client_contracts");

            migrationBuilder.DropColumn(
                name: "approved_by",
                schema: "control",
                table: "client_contracts");

            migrationBuilder.DropColumn(
                name: "revision_number",
                schema: "control",
                table: "client_contracts");

            migrationBuilder.DropColumn(
                name: "supersedes_contract_id",
                schema: "control",
                table: "client_contracts");

            migrationBuilder.DropColumn(
                name: "contract_revision_number",
                schema: "control",
                table: "client_access_revisions");
        }
    }
}
