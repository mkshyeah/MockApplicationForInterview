using AccountingHelper.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AccountingHelper.Infrastructure.Data.Configurations;

public class DepartmentConfiguration : IEntityTypeConfiguration<DepartmentEntity>
{
    public void Configure(EntityTypeBuilder<DepartmentEntity> builder)
    {
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Id)
            .HasDefaultValueSql("NEWID()");
        
        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(e => e.Description)
            .HasMaxLength(500);
        
        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");
        
        builder.HasData(
            new DepartmentEntity
            {
                Id = new Guid("22222222-0000-0000-0000-000000000001"),
                Name = "Engineering",
                CreatedAt = new DateTime(2019, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new DepartmentEntity
            {
                Id = new Guid("22222222-0000-0000-0000-000000000002"),
                Name = "Design",
                CreatedAt = new DateTime(2019, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new DepartmentEntity
            {
                Id = new Guid("22222222-0000-0000-0000-000000000003"),
                Name = "HR",
                CreatedAt = new DateTime(2019, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );
    }
}