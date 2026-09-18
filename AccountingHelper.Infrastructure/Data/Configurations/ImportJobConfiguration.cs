using AccountingHelper.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AccountingHelper.Infrastructure.Data.Configurations;

public class ImportJobConfiguration : IEntityTypeConfiguration<ImportJobEntity>
{
    public void Configure(EntityTypeBuilder<ImportJobEntity> builder)
    {
        builder.HasKey(x => x.Id);
        
        builder.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.Status).HasConversion<string>();
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        
        builder.Property(e => e.FileName).IsRequired().HasMaxLength(255);
    }
}