using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class AddClientDirectoryAndWorkQueueReadModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,");

            migrationBuilder.AddColumn<string>(
                name: "search_text",
                schema: "control",
                table: "clients",
                type: "text",
                nullable: true,
                computedColumnSql: "lower(code || ' ' || display_name || ' ' || legal_name || ' ' || status)",
                stored: true);

            migrationBuilder.CreateIndex(
                name: "ix_cloud_outbox_messages_client_status",
                schema: "control",
                table: "cloud_outbox_messages",
                columns: new[] { "client_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_clients_display_name_code_id",
                schema: "control",
                table: "clients",
                columns: new[] { "display_name", "code", "client_id" });

            migrationBuilder.CreateIndex(
                name: "ix_clients_legal_name_code_id",
                schema: "control",
                table: "clients",
                columns: new[] { "legal_name", "code", "client_id" });

            migrationBuilder.CreateIndex(
                name: "ix_clients_search_text",
                schema: "control",
                table: "clients",
                column: "search_text")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_clients_status_code_id",
                schema: "control",
                table: "clients",
                columns: new[] { "status", "code", "client_id" });

            migrationBuilder.CreateIndex(
                name: "ix_client_contacts_client_id",
                schema: "control",
                table: "client_contacts",
                column: "client_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_cloud_outbox_messages_client_status",
                schema: "control",
                table: "cloud_outbox_messages");

            migrationBuilder.DropIndex(
                name: "ix_clients_display_name_code_id",
                schema: "control",
                table: "clients");

            migrationBuilder.DropIndex(
                name: "ix_clients_legal_name_code_id",
                schema: "control",
                table: "clients");

            migrationBuilder.DropIndex(
                name: "ix_clients_search_text",
                schema: "control",
                table: "clients");

            migrationBuilder.DropIndex(
                name: "ix_clients_status_code_id",
                schema: "control",
                table: "clients");

            migrationBuilder.DropIndex(
                name: "ix_client_contacts_client_id",
                schema: "control",
                table: "client_contacts");

            migrationBuilder.DropColumn(
                name: "search_text",
                schema: "control",
                table: "clients");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:pg_trgm", ",,");
        }
    }
}
