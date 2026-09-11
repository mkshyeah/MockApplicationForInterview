using AccountingHelper.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AccountingHelper.Infrastructure.Data.Configurations;

public class PayrollRunConfiguration : IEntityTypeConfiguration<PayrollRunEntity>
{
    public void Configure(EntityTypeBuilder<PayrollRunEntity> builder)
    {
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.Status).HasConversion<string>();
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        
        builder.HasIndex(e => new{ e.Year, e.Month}).IsUnique();
        
        
    }
}