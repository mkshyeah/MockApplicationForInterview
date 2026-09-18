using AccountingHelper.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AccountingHelper.Infrastructure.Data.Configurations;

public class ImportFileConfiguration : IEntityTypeConfiguration<ImportFileEntity>
{
    public void Configure(EntityTypeBuilder<ImportFileEntity> builder)
    {
        builder.HasKey(x => x.Id);
        
        builder.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(e => e.ContentType).IsRequired().HasMaxLength(100);
        builder.Property(e => e.Content).IsRequired();
        
        // Claim check: the message carries an id, the payload stays here. A broker is not storage,
        // and a status poll must not pay for the file to read three counters. In phase 9 Content 
        // becomes a blob path and only this property changes.
        builder.HasOne<ImportJobEntity>()
            .WithOne()
            .HasForeignKey<ImportFileEntity>(e => e.ImportJobId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}