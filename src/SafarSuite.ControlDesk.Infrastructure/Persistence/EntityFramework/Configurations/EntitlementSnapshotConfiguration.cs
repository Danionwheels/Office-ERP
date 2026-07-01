using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SafarSuite.ControlDesk.Domain.Modules.Clients;
using SafarSuite.ControlDesk.Domain.Modules.Contracts;
using SafarSuite.ControlDesk.Domain.Modules.Entitlements;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework.Configurations;

internal sealed class EntitlementSnapshotConfiguration : IEntityTypeConfiguration<EntitlementSnapshot>
{
    public void Configure(EntityTypeBuilder<EntitlementSnapshot> builder)
    {
        builder.ToTable("entitlement_snapshots");

        builder.HasKey(snapshot => snapshot.Id);

        builder.Property(snapshot => snapshot.Id)
            .HasColumnName("entitlement_snapshot_id")
            .HasConversion(
                id => id.Value,
                value => EntitlementSnapshotId.Create(value))
            .ValueGeneratedNever();

        builder.Property(snapshot => snapshot.ClientId)
            .HasColumnName("client_id")
            .HasConversion(
                id => id.Value,
                value => ClientId.Create(value))
            .IsRequired();

        builder.HasOne<Client>()
            .WithMany()
            .HasForeignKey(snapshot => snapshot.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(snapshot => snapshot.ContractId)
            .HasColumnName("contract_id")
            .HasConversion(
                id => id.Value,
                value => ContractId.Create(value))
            .IsRequired();

        builder.Property(snapshot => snapshot.Status)
            .HasColumnName("status")
            .HasMaxLength(32)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(snapshot => snapshot.PaidUntil)
            .HasColumnName("paid_until")
            .IsRequired();

        builder.Property(snapshot => snapshot.GraceUntil)
            .HasColumnName("grace_until")
            .IsRequired();

        builder.Property(snapshot => snapshot.OfflineValidUntil)
            .HasColumnName("offline_valid_until")
            .IsRequired();

        builder.Property(snapshot => snapshot.AllowedDevices)
            .HasColumnName("allowed_devices")
            .IsRequired();

        builder.Property(snapshot => snapshot.AllowedBranches)
            .HasColumnName("allowed_branches")
            .IsRequired();

        builder.Property(snapshot => snapshot.IssuedAtUtc)
            .HasColumnName("issued_at_utc")
            .IsRequired();

        builder.HasIndex(snapshot => new { snapshot.ClientId, snapshot.IssuedAtUtc })
            .HasDatabaseName("ix_entitlement_snapshots_client_issued");

        builder.Navigation(snapshot => snapshot.Modules)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.OwnsMany(snapshot => snapshot.Modules, module =>
        {
            module.ToTable("entitlement_snapshot_modules");

            module.WithOwner()
                .HasForeignKey("entitlement_snapshot_id");

            module.Property<int>("entitlement_snapshot_module_row_id")
                .HasColumnName("entitlement_snapshot_module_row_id")
                .ValueGeneratedOnAdd();

            module.HasKey("entitlement_snapshot_module_row_id");

            module.Property(entitlementModule => entitlementModule.ModuleCode)
                .HasColumnName("module_code")
                .HasMaxLength(64)
                .HasConversion(
                    moduleCode => moduleCode.Value,
                    value => ModuleCode.Create(value))
                .IsRequired();

            module.Property(entitlementModule => entitlementModule.IsEnabled)
                .HasColumnName("is_enabled")
                .IsRequired();

            module.HasIndex("entitlement_snapshot_id", nameof(EntitlementModule.ModuleCode))
                .IsUnique()
                .HasDatabaseName("ux_entitlement_snapshot_modules_snapshot_code");
        });
    }
}
