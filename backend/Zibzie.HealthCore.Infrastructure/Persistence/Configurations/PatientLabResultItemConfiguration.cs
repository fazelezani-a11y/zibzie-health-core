using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zibzie.HealthCore.Domain.Entities;

namespace Zibzie.HealthCore.Infrastructure.Persistence.Configurations;

public class PatientLabResultItemConfiguration : IEntityTypeConfiguration<PatientLabResultItem>
{
    public void Configure(EntityTypeBuilder<PatientLabResultItem> entity)
    {
        entity.HasKey(x => x.Id);

        entity.Property(x => x.TestName)
            .IsRequired()
            .HasMaxLength(200);

        entity.Property(x => x.Value)
            .HasMaxLength(200);

        entity.Property(x => x.Unit)
            .HasMaxLength(50);

        entity.Property(x => x.ReferenceRange)
            .HasMaxLength(200);

        entity.Property(x => x.Interpretation)
            .HasMaxLength(1000);

        entity.HasOne(x => x.PatientParaclinicalResult)
            .WithMany(x => x.LabItems)
            .HasForeignKey(x => x.PatientParaclinicalResultId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasIndex(x => x.PatientParaclinicalResultId);

        entity.HasIndex(x => x.DisplayOrder);
    }
}
