using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zibzie.HealthCore.Domain.Entities;

namespace Zibzie.HealthCore.Infrastructure.Persistence.Configurations;

public class PatientProfileConfiguration : IEntityTypeConfiguration<PatientProfile>
{
    public void Configure(EntityTypeBuilder<PatientProfile> entity)
    {
        entity.HasKey(x => x.Id);

        entity.Property(x => x.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        entity.Property(x => x.LastName)
            .IsRequired()
            .HasMaxLength(100);

        entity.Property(x => x.NationalCode)
            .HasMaxLength(20);

        entity.Property(x => x.Gender)
            .HasMaxLength(30);

        entity.Property(x => x.BloodType)
            .HasMaxLength(10);

        entity.Property(x => x.MaritalStatus)
            .HasMaxLength(50);

        entity.Property(x => x.EducationLevel)
            .HasMaxLength(100);

        entity.Property(x => x.Occupation)
            .HasMaxLength(100);

        entity.Property(x => x.ProfileImageUrl)
            .HasMaxLength(500);

        entity.HasOne(x => x.ContactInfo)
            .WithOne(x => x.PatientProfile)
            .HasForeignKey<ContactInfo>(x => x.PatientProfileId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
