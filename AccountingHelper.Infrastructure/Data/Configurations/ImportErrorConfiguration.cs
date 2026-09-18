using AccountingHelper.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AccountingHelper.Infrastructure.Data.Configurations;

public class ImportErrorConfiguration : IEntityTypeConfiguration<ImportErrorEntity>
{
    public void Configure(EntityTypeBuilder<ImportErrorEntity> builder)
    {
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(e => e.Message).IsRequired().HasMaxLength(1000);
        
        // One error per file row. Redelivery cannot write the report twice, and the index is also
        // what makes the paged report an ordered scan instead of a sort.
        builder.HasIndex(e => new { e.ImportJobId, e.RowNumber }).IsUnique();
        
        builder.HasOne<ImportJobEntity>()
            .WithMany()
            .HasForeignKey(e => e.ImportJobId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}