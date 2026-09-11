using AccountingHelper.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AccountingHelper.Infrastructure.Data.Configurations;

public class PayslipConfiguration : IEntityTypeConfiguration<PayslipEntity>
{
    public void Configure(EntityTypeBuilder<PayslipEntity> builder)
    {
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        
        builder.Property(e => e.Gross).HasPrecision(18, 2);                                                                                                                                                          
        builder.Property(e => e.Tax).HasPrecision(18, 2);                                                                                                                                                            
        builder.Property(e => e.Deductions).HasPrecision(18, 2);                                                                                                                                                     
        builder.Property(e => e.Net).HasPrecision(18, 2);
        
        // One payslip per employee per run. This is what makes redelivery harmless: at-least-once
        // // means the same message can arrive twice, and the second insert must fail, not double the money.
        builder.HasIndex(e => new { e.PayrollRunId, e.EmployeeId }).IsUnique();
        
        builder.HasOne(e => e.PayrollRun)
            .WithMany(pr => pr.Payslips)
            .HasForeignKey(e => e.PayrollRunId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(e => e.Employee)
            .WithMany()
            .HasForeignKey(e => e.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}