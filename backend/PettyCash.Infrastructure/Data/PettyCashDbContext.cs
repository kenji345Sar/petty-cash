using Microsoft.EntityFrameworkCore;
using PettyCash.Domain.Vendor.Ledger;
using PettyCash.Domain.Vendor.BagManagement;
using PettyCash.Domain.Vendor.DenomCheck;
using PettyCash.Domain.PettyCash.Ledger;
using PettyCash.Domain.PettyCash.DenomCheck;
using PettyCash.Domain.SafeAggregate;
using PettyCash.Domain.Shared.Services;

namespace PettyCash.Infrastructure.Data;

public class PettyCashDbContext(DbContextOptions<PettyCashDbContext> options) : DbContext(options), IUnitOfWork
{
    async Task IUnitOfWork.SaveChangesAsync()
    {
        await base.SaveChangesAsync();
    }

    public DbSet<Safe> Safes => Set<Safe>();
    public DbSet<ChangeBag> ChangeBags => Set<ChangeBag>();
    public DbSet<CashBag> CashBags => Set<CashBag>();
    public DbSet<VendorTransaction> VendorTransactions => Set<VendorTransaction>();
    public DbSet<PettyCashTransaction> PettyCashTransactions => Set<PettyCashTransaction>();
    public DbSet<VendorDenominationCheck> VendorDenominationChecks => Set<VendorDenominationCheck>();
    public DbSet<PettyCashDenominationCheck> PettyCashDenominationChecks => Set<PettyCashDenominationCheck>();
    public DbSet<PrepBag> PrepBags => Set<PrepBag>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Safe>(entity =>
        {
            entity.ToTable("safes");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(100);
            entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(200);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Ignore(e => e.CurrentBalance);
            entity.Ignore(e => e.VendorBalance);
            entity.Ignore(e => e.PettyCashBalance);
        });

        modelBuilder.Entity<ChangeBag>(entity =>
        {
            entity.ToTable("change_bags");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.SafeId).HasColumnName("safe_id");
            entity.Property(e => e.TotalAmount).HasColumnName("total_amount");
            entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(200);
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.MovedAt).HasColumnName("moved_at");

            entity.HasOne(e => e.Safe).WithMany().HasForeignKey(e => e.SafeId);

            entity.HasMany<VendorTransaction>("_transactions")
                .WithOne(t => t.ChangeBag)
                .HasForeignKey(t => t.ChangeBagId);

            entity.HasMany(e => e.DenominationChecks)
                .WithOne(c => c.ChangeBag)
                .HasForeignKey(c => c.ChangeBagId);
        });

        modelBuilder.Entity<CashBag>(entity =>
        {
            entity.ToTable("cash_bags");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.SafeId).HasColumnName("safe_id");
            entity.Property(e => e.TotalAmount).HasColumnName("total_amount");
            entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(200);
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.MovedAt).HasColumnName("moved_at");
            entity.Property(e => e.PrepBagId).HasColumnName("prep_bag_id");

            entity.HasOne(e => e.Safe).WithMany().HasForeignKey(e => e.SafeId);

            entity.HasOne(e => e.PrepBag)
                .WithMany(p => p.CashBags)
                .HasForeignKey(e => e.PrepBagId);

            entity.HasOne(e => e.Transaction)
                .WithOne(t => t.CashBag)
                .HasForeignKey<VendorTransaction>(t => t.CashBagId);

            entity.HasMany(e => e.DenominationChecks)
                .WithOne(c => c.CashBag)
                .HasForeignKey(c => c.CashBagId);
        });

        modelBuilder.Entity<VendorTransaction>(entity =>
        {
            entity.ToTable("vendor_transactions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.SequenceNumber).HasColumnName("sequence_number");
            entity.Property(e => e.SafeId).HasColumnName("safe_id");
            entity.Property(e => e.ChangeBagId).HasColumnName("change_bag_id");
            entity.Property(e => e.CashBagId).HasColumnName("cash_bag_id");
            entity.Property(e => e.PrepBagId).HasColumnName("prep_bag_id");
            entity.Property(e => e.Type).HasColumnName("type");
            entity.Property(e => e.Amount).HasColumnName("amount");
            entity.Property(e => e.Balance).HasColumnName("balance");
            entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(200);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");

            entity.OwnsOne(e => e.Denomination, d =>
            {
                d.Property(p => p.Count10000).HasColumnName("denom_10000");
                d.Property(p => p.Count5000).HasColumnName("denom_5000");
                d.Property(p => p.Count1000).HasColumnName("denom_1000");
                d.Property(p => p.Count500).HasColumnName("denom_500");
                d.Property(p => p.Count100).HasColumnName("denom_100");
                d.Property(p => p.Count50).HasColumnName("denom_50");
                d.Property(p => p.Count10).HasColumnName("denom_10");
                d.Property(p => p.Count5).HasColumnName("denom_5");
                d.Property(p => p.Count1).HasColumnName("denom_1");
            });
        });

        modelBuilder.Entity<PettyCashTransaction>(entity =>
        {
            entity.ToTable("petty_cash_transactions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.SequenceNumber).HasColumnName("sequence_number");
            entity.Property(e => e.SafeId).HasColumnName("safe_id");
            entity.Property(e => e.Type).HasColumnName("type");
            entity.Property(e => e.Amount).HasColumnName("amount");
            entity.Property(e => e.Balance).HasColumnName("balance");
            entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(200);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");

            entity.OwnsOne(e => e.Denomination, d =>
            {
                d.Property(p => p.Count10000).HasColumnName("denom_10000");
                d.Property(p => p.Count5000).HasColumnName("denom_5000");
                d.Property(p => p.Count1000).HasColumnName("denom_1000");
                d.Property(p => p.Count500).HasColumnName("denom_500");
                d.Property(p => p.Count100).HasColumnName("denom_100");
                d.Property(p => p.Count50).HasColumnName("denom_50");
                d.Property(p => p.Count10).HasColumnName("denom_10");
                d.Property(p => p.Count5).HasColumnName("denom_5");
                d.Property(p => p.Count1).HasColumnName("denom_1");
            });
        });

        modelBuilder.Entity<PrepBag>(entity =>
        {
            entity.ToTable("prep_bags");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.SafeId).HasColumnName("safe_id");
            entity.Property(e => e.TotalAmount).HasColumnName("total_amount");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.HandedOverAt).HasColumnName("handed_over_at");

            entity.HasOne(e => e.Safe).WithMany().HasForeignKey(e => e.SafeId);

            entity.HasOne(e => e.Transaction)
                .WithOne(t => t.PrepBag)
                .HasForeignKey<VendorTransaction>(t => t.PrepBagId);
        });

        modelBuilder.Entity<VendorDenominationCheck>(entity =>
        {
            entity.ToTable("vendor_denomination_checks");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.SequenceNumber).HasColumnName("sequence_number");
            entity.Property(e => e.SafeId).HasColumnName("safe_id");
            entity.Property(e => e.ChangeBagId).HasColumnName("change_bag_id");
            entity.Property(e => e.CashBagId).HasColumnName("cash_bag_id");
            entity.Property(e => e.PrepBagId).HasColumnName("prep_bag_id");
            entity.Property(e => e.CheckedAmount).HasColumnName("checked_amount");
            entity.Property(e => e.ExpectedAmount).HasColumnName("expected_amount");
            entity.Property(e => e.Difference).HasColumnName("difference");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");

            entity.OwnsOne(e => e.Denomination, d =>
            {
                d.Property(p => p.Count10000).HasColumnName("count_10000");
                d.Property(p => p.Count5000).HasColumnName("count_5000");
                d.Property(p => p.Count1000).HasColumnName("count_1000");
                d.Property(p => p.Count500).HasColumnName("count_500");
                d.Property(p => p.Count100).HasColumnName("count_100");
                d.Property(p => p.Count50).HasColumnName("count_50");
                d.Property(p => p.Count10).HasColumnName("count_10");
                d.Property(p => p.Count5).HasColumnName("count_5");
                d.Property(p => p.Count1).HasColumnName("count_1");
            });

            entity.HasOne(e => e.ChangeBag)
                .WithMany(b => b.DenominationChecks)
                .HasForeignKey(e => e.ChangeBagId);

            entity.HasOne(e => e.CashBag)
                .WithMany(b => b.DenominationChecks)
                .HasForeignKey(e => e.CashBagId);

            entity.HasOne(e => e.PrepBag)
                .WithMany(b => b.DenominationChecks)
                .HasForeignKey(e => e.PrepBagId);
        });

        modelBuilder.Entity<PettyCashDenominationCheck>(entity =>
        {
            entity.ToTable("safe_denomination_checks");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.SequenceNumber).HasColumnName("sequence_number");
            entity.Property(e => e.SafeId).HasColumnName("safe_id");
            entity.Property(e => e.CheckedAmount).HasColumnName("checked_amount");
            entity.Property(e => e.ExpectedAmount).HasColumnName("expected_amount");
            entity.Property(e => e.Difference).HasColumnName("difference");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");

            entity.OwnsOne(e => e.Denomination, d =>
            {
                d.Property(p => p.Count10000).HasColumnName("count_10000");
                d.Property(p => p.Count5000).HasColumnName("count_5000");
                d.Property(p => p.Count1000).HasColumnName("count_1000");
                d.Property(p => p.Count500).HasColumnName("count_500");
                d.Property(p => p.Count100).HasColumnName("count_100");
                d.Property(p => p.Count50).HasColumnName("count_50");
                d.Property(p => p.Count10).HasColumnName("count_10");
                d.Property(p => p.Count5).HasColumnName("count_5");
                d.Property(p => p.Count1).HasColumnName("count_1");
            });
        });
    }
}
