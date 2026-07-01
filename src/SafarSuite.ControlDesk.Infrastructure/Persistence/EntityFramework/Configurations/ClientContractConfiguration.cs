using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SafarSuite.ControlDesk.Domain.Modules.Clients;
using SafarSuite.ControlDesk.Domain.Modules.Contracts;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework.Configurations;

internal sealed class ClientContractConfiguration : IEntityTypeConfiguration<ClientContract>
{
    public void Configure(EntityTypeBuilder<ClientContract> builder)
    {
        builder.ToTable("client_contracts");

        builder.HasKey(contract => contract.Id);

        builder.Property(contract => contract.Id)
            .HasColumnName("contract_id")
            .HasConversion(
                id => id.Value,
                value => ContractId.Create(value))
            .ValueGeneratedNever();

        builder.Property(contract => contract.ClientId)
            .HasColumnName("client_id")
            .HasConversion(
                id => id.Value,
                value => ClientId.Create(value))
            .IsRequired();

        builder.HasOne<Client>()
            .WithMany()
            .HasForeignKey(contract => contract.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(contract => contract.Number)
            .HasColumnName("number")
            .HasMaxLength(40)
            .HasConversion(
                number => number.Value,
                value => ContractNumber.Create(value))
            .IsRequired();

        builder.HasIndex(contract => contract.Number)
            .IsUnique()
            .HasDatabaseName("ux_client_contracts_number");

        builder.OwnsOne(contract => contract.Term, term =>
        {
            term.Property(value => value.StartsOn)
                .HasColumnName("starts_on")
                .IsRequired();

            term.Property(value => value.EndsOn)
                .HasColumnName("ends_on")
                .IsRequired();
        });

        builder.Navigation(contract => contract.Term)
            .IsRequired();

        builder.OwnsOne(contract => contract.Pricing, pricing =>
        {
            pricing.OwnsOne(value => value.RecurringAmount, money =>
            {
                MoneyConfiguration.Configure(money, "recurring_amount", "currency_code");
            });

            pricing.Navigation(value => value.RecurringAmount)
                .IsRequired();

            pricing.Property(value => value.BillingCycle)
                .HasColumnName("billing_cycle")
                .HasMaxLength(32)
                .HasConversion<string>()
                .IsRequired();

            pricing.Property(value => value.BillingDayOfMonth)
                .HasColumnName("billing_day_of_month")
                .IsRequired();
        });

        builder.Navigation(contract => contract.Pricing)
            .IsRequired();

        builder.OwnsOne(contract => contract.DeviceAllowance, allowance =>
        {
            allowance.Property(value => value.AllowedDevices)
                .HasColumnName("allowed_devices")
                .IsRequired();
        });

        builder.Navigation(contract => contract.DeviceAllowance)
            .IsRequired();

        builder.OwnsOne(contract => contract.BranchAllowance, allowance =>
        {
            allowance.Property(value => value.AllowedBranches)
                .HasColumnName("allowed_branches")
                .IsRequired();
        });

        builder.Navigation(contract => contract.BranchAllowance)
            .IsRequired();

        builder.Property(contract => contract.Status)
            .HasColumnName("status")
            .HasMaxLength(32)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(contract => contract.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(contract => contract.ActivatedAtUtc)
            .HasColumnName("activated_at_utc");

        builder.HasIndex(contract => new { contract.ClientId, contract.Status })
            .HasDatabaseName("ix_client_contracts_client_status");

        builder.Navigation(contract => contract.ModuleAllowances)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.OwnsMany(contract => contract.ModuleAllowances, module =>
        {
            module.ToTable("client_contract_module_allowances");

            module.WithOwner()
                .HasForeignKey("contract_id");

            module.Property<int>("client_contract_module_allowance_row_id")
                .HasColumnName("client_contract_module_allowance_row_id")
                .ValueGeneratedOnAdd();

            module.HasKey("client_contract_module_allowance_row_id");

            module.Property(moduleAllowance => moduleAllowance.ModuleCode)
                .HasColumnName("module_code")
                .HasMaxLength(64)
                .HasConversion(
                    moduleCode => moduleCode.Value,
                    value => ModuleCode.Create(value))
                .IsRequired();

            module.Property(moduleAllowance => moduleAllowance.IsEnabled)
                .HasColumnName("is_enabled")
                .IsRequired();

            module.HasIndex("contract_id", nameof(ModuleAllowance.ModuleCode))
                .IsUnique()
                .HasDatabaseName("ux_client_contract_modules_contract_code");
        });
    }
}
