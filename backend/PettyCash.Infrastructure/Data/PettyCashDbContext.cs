using Microsoft.EntityFrameworkCore;
using PettyCash.Domain.Entities;

namespace PettyCash.Infrastructure.Data;

public class PettyCashDbContext(DbContextOptions<PettyCashDbContext> options) : DbContext(options)
{
    public DbSet<ChangeBag> ChangeBags => Set<ChangeBag>();
    public DbSet<CashBag> CashBags => Set<CashBag>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<DenominationCheck> DenominationChecks => Set<DenominationCheck>();
    public DbSet<PrepBag> PrepBags => Set<PrepBag>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ChangeBag>(entity =>
        {
            entity.ToTable("change_bags");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.TotalAmount).HasColumnName("total_amount");
            entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(200);
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.MovedAt).HasColumnName("moved_at");

            entity.HasMany<Transaction>("_transactions")
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
            entity.Property(e => e.TotalAmount).HasColumnName("total_amount");
            entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(200);
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.MovedAt).HasColumnName("moved_at");
            entity.Property(e => e.PrepBagId).HasColumnName("prep_bag_id");

            entity.HasOne(e => e.PrepBag)
                .WithMany(p => p.CashBags)
                .HasForeignKey(e => e.PrepBagId);

            entity.HasOne(e => e.Transaction)
                .WithOne(t => t.CashBag)
                .HasForeignKey<Transaction>(t => t.CashBagId);

            entity.HasMany(e => e.DenominationChecks)
                .WithOne(c => c.CashBag)
                .HasForeignKey(c => c.CashBagId);
        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.ToTable("transactions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ChangeBagId).HasColumnName("change_bag_id");
            entity.Property(e => e.CashBagId).HasColumnName("cash_bag_id");
            entity.Property(e => e.Type).HasColumnName("type");
            entity.Property(e => e.Amount).HasColumnName("amount");
            entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(200);
            entity.Property(e => e.PrepBagId).HasColumnName("prep_bag_id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
        });

        modelBuilder.Entity<PrepBag>(entity =>
        {
            entity.ToTable("prep_bags");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.TotalAmount).HasColumnName("total_amount");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.HandedOverAt).HasColumnName("handed_over_at");

            entity.HasOne(e => e.Transaction)
                .WithOne(t => t.PrepBag)
                .HasForeignKey<Transaction>(t => t.PrepBagId);
        });

        modelBuilder.Entity<DenominationCheck>(entity =>
        {
            entity.ToTable("denomination_checks");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
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
    }
}
