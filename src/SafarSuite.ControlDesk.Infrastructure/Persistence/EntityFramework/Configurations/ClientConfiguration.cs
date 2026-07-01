using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SafarSuite.ControlDesk.Domain.Modules.Clients;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework.Configurations;

internal sealed class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        builder.ToTable("clients");

        builder.HasKey(client => client.Id);

        builder.Property(client => client.Id)
            .HasColumnName("client_id")
            .HasConversion(
                id => id.Value,
                value => ClientId.Create(value))
            .ValueGeneratedNever();

        builder.Property(client => client.Code)
            .HasColumnName("code")
            .HasMaxLength(32)
            .HasConversion(
                code => code.Value,
                value => ClientCode.Create(value))
            .IsRequired();

        builder.HasIndex(client => client.Code)
            .IsUnique()
            .HasDatabaseName("ux_clients_code");

        builder.Property(client => client.LegalName)
            .HasColumnName("legal_name")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(client => client.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(client => client.Status)
            .HasColumnName("status")
            .HasMaxLength(32)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(client => client.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(client => client.ActivatedAtUtc)
            .HasColumnName("activated_at_utc");

        builder.Property(client => client.SuspendedAtUtc)
            .HasColumnName("suspended_at_utc");

        builder.Navigation(client => client.Contacts)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.OwnsMany(client => client.Contacts, contact =>
        {
            contact.ToTable("client_contacts");

            contact.WithOwner()
                .HasForeignKey("client_id");

            contact.HasKey(clientContact => clientContact.Id);

            contact.Property(clientContact => clientContact.Id)
                .HasColumnName("client_contact_id")
                .HasConversion(
                    id => id.Value,
                    value => ClientContactId.Create(value))
                .ValueGeneratedNever();

            contact.Property(clientContact => clientContact.Role)
                .HasColumnName("role")
                .HasMaxLength(32)
                .HasConversion<string>()
                .IsRequired();

            contact.Property(clientContact => clientContact.FullName)
                .HasColumnName("full_name")
                .HasMaxLength(256)
                .IsRequired();

            contact.Property(clientContact => clientContact.JobTitle)
                .HasColumnName("job_title")
                .HasMaxLength(128);

            contact.Property(clientContact => clientContact.Email)
                .HasColumnName("email")
                .HasMaxLength(256);

            contact.Property(clientContact => clientContact.Phone)
                .HasColumnName("phone")
                .HasMaxLength(64);

            contact.Property(clientContact => clientContact.IsPrimary)
                .HasColumnName("is_primary")
                .IsRequired();

            contact.Property(clientContact => clientContact.CreatedAtUtc)
                .HasColumnName("created_at_utc")
                .IsRequired();

            contact.HasIndex("client_id", nameof(ClientContact.Role))
                .IsUnique()
                .HasDatabaseName("ux_client_contacts_primary_role")
                .HasFilter("is_primary");
        });

        builder.Navigation(client => client.SupportNotes)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.OwnsMany(client => client.SupportNotes, note =>
        {
            note.ToTable("client_support_notes");

            note.WithOwner()
                .HasForeignKey("client_id");

            note.Property<int>("support_note_row_id")
                .HasColumnName("support_note_row_id")
                .ValueGeneratedOnAdd();

            note.HasKey("support_note_row_id");

            note.Property(supportNote => supportNote.Text)
                .HasColumnName("text")
                .HasMaxLength(4000)
                .IsRequired();

            note.Property(supportNote => supportNote.CreatedBy)
                .HasColumnName("created_by")
                .HasMaxLength(128)
                .IsRequired();

            note.Property(supportNote => supportNote.CreatedAtUtc)
                .HasColumnName("created_at_utc")
                .IsRequired();
        });
    }
}
