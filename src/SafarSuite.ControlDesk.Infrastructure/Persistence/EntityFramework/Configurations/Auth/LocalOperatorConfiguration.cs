using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SafarSuite.ControlDesk.Domain.Modules.Auth;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework.Configurations.Auth;

internal sealed class LocalOperatorConfiguration : IEntityTypeConfiguration<LocalOperator>
{
    public void Configure(EntityTypeBuilder<LocalOperator> builder)
    {
        builder.ToTable("local_operators", "auth", table =>
        {
            table.HasCheckConstraint(
                "ck_local_operators_status",
                "status IN ('Active', 'Disabled')");
            table.HasCheckConstraint(
                "ck_local_operators_security_version",
                "security_version > 0");
            table.HasCheckConstraint(
                "ck_local_operators_normalized_email",
                "normalized_email = upper(btrim(email))");
        });

        builder.HasKey(localOperator => localOperator.Id);

        builder.Property(localOperator => localOperator.Id)
            .HasColumnName("operator_id")
            .HasConversion(
                id => id.Value,
                value => LocalOperatorId.Create(value))
            .ValueGeneratedNever();

        builder.Property(localOperator => localOperator.Email)
            .HasColumnName("email")
            .HasMaxLength(320)
            .IsRequired();

        builder.Property(localOperator => localOperator.NormalizedEmail)
            .HasColumnName("normalized_email")
            .HasMaxLength(320)
            .IsRequired();

        builder.HasIndex(localOperator => localOperator.NormalizedEmail)
            .IsUnique()
            .HasDatabaseName("ux_local_operators_normalized_email");

        builder.Property(localOperator => localOperator.FullName)
            .HasColumnName("full_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(localOperator => localOperator.PasswordHash)
            .HasColumnName("password_hash")
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(localOperator => localOperator.Status)
            .HasColumnName("status")
            .HasMaxLength(32)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(localOperator => localOperator.SecurityVersion)
            .HasColumnName("security_version")
            .IsRequired();

        builder.Property(localOperator => localOperator.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(localOperator => localOperator.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .IsRequired();

        builder.HasIndex(localOperator => localOperator.Status)
            .HasDatabaseName("ix_local_operators_status");

        builder.Ignore(localOperator => localOperator.Roles);
        builder.Ignore(localOperator => localOperator.Scopes);

        builder.OwnsMany(localOperator => localOperator.RoleGrants, role =>
        {
            role.ToTable("local_operator_roles", "auth", table =>
                table.HasCheckConstraint(
                    "ck_local_operator_roles_role",
                    "role IN ('Administrator', 'CommercialOperator', 'FinanceOperator', 'SupportOperator', 'Auditor')"));

            role.WithOwner()
                .HasForeignKey("operator_id");

            role.Property(grant => grant.Value)
                .HasColumnName("role")
                .HasMaxLength(64)
                .IsRequired();

            role.HasKey("operator_id", nameof(LocalOperatorRoleGrant.Value));
        });

        builder.Navigation(localOperator => localOperator.RoleGrants)
            .HasField("_roleGrants")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.OwnsMany(localOperator => localOperator.ScopeGrants, scope =>
        {
            scope.ToTable("local_operator_scopes", "auth", table =>
                table.HasCheckConstraint(
                    "ck_local_operator_scopes_scope",
                    "scope IN ('control-desk:admin', 'command-center:read', 'clients:manage', 'contracts:manage', 'accounting:manage', 'billing:manage', 'payments:manage', 'entitlements:manage', 'control-cloud:manage', 'diagnostics:read', 'reports:read')"));

            scope.WithOwner()
                .HasForeignKey("operator_id");

            scope.Property(grant => grant.Value)
                .HasColumnName("scope")
                .HasMaxLength(128)
                .IsRequired();

            scope.HasKey("operator_id", nameof(LocalOperatorScopeGrant.Value));
        });

        builder.Navigation(localOperator => localOperator.ScopeGrants)
            .HasField("_scopeGrants")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
