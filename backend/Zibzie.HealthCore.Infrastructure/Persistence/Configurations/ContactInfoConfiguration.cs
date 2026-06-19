using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zibzie.HealthCore.Domain.Entities;

namespace Zibzie.HealthCore.Infrastructure.Persistence.Configurations;

public class ContactInfoConfiguration : IEntityTypeConfiguration<ContactInfo>
{
    public void Configure(EntityTypeBuilder<ContactInfo> entity)
    {
        entity.HasKey(x => x.Id);

        entity.Property(x => x.MobileNumber)
            .IsRequired()
            .HasMaxLength(30);

        entity.Property(x => x.Email)
            .HasMaxLength(200);

        entity.Property(x => x.EmergencyContactName)
            .HasMaxLength(150);

        entity.Property(x => x.EmergencyContactPhone)
            .HasMaxLength(30);

        entity.Property(x => x.HomeAddress)
            .HasMaxLength(1000);

        entity.Property(x => x.WorkAddress)
            .HasMaxLength(1000);
    }
}
