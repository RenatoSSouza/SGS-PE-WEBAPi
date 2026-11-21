using Microsoft.EntityFrameworkCore;
using sgspe.Models.Companies;
using sgspe.Models.Energy;
using sgspe.Models.Equipments;
using sgspe.Models.Goals;
using sgspe.Models.Materials;
using sgspe.Models.Notifications;
using sgspe.Models.Waste;
using sgspe.Models.Water;
using sgspe.Models.Users;

namespace sgspe.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Equipment> Equipments => Set<Equipment>();
    public DbSet<EnergyRecord> EnergyRecords => Set<EnergyRecord>();
    public DbSet<WaterRecord> WaterRecords => Set<WaterRecord>();
    public DbSet<WasteEntry> WasteEntries => Set<WasteEntry>();
    public DbSet<MaterialEntry> MaterialEntries => Set<MaterialEntry>();
    public DbSet<Goal> Goals => Set<Goal>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Company>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.LegalName).IsRequired().HasMaxLength(200);
            entity.Property(c => c.TradingName).HasMaxLength(200);
            entity.HasIndex(c => c.LegalName).IsUnique();
        });

        modelBuilder.Entity<Equipment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(150);
            entity.HasOne(e => e.Company).WithMany().HasForeignKey(e => e.CompanyId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EnergyRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Ignore(e => e.NetConsumptionKwh);
            entity.Ignore(e => e.Co2Kg);
            entity.HasIndex(e => new { e.CompanyId, e.Year, e.Month }).IsUnique();
            entity.HasOne(e => e.Company).WithMany().HasForeignKey(e => e.CompanyId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WaterRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Ignore(e => e.NetConsumptionM3);
            entity.HasIndex(e => new { e.CompanyId, e.Year, e.Month }).IsUnique();
            entity.HasOne(e => e.Company).WithMany().HasForeignKey(e => e.CompanyId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WasteEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.CompanyId, e.Year, e.Month });
            entity.HasOne(e => e.Company).WithMany().HasForeignKey(e => e.CompanyId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MaterialEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(150);
            entity.Property(e => e.Unit).IsRequired().HasMaxLength(20);
            entity.HasIndex(e => new { e.CompanyId, e.Year, e.Month });
            entity.HasOne(e => e.Company).WithMany().HasForeignKey(e => e.CompanyId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Goal>(entity =>
        {
            entity.HasKey(g => g.Id);
            entity.Ignore(g => g.HigherIsBetter);
            entity.Property(g => g.Title).IsRequired().HasMaxLength(200);
            entity.HasIndex(g => new { g.CompanyId, g.Deadline });
            entity.HasOne(g => g.Company).WithMany().HasForeignKey(g => g.CompanyId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(g => g.ResponsibleUser).WithMany().HasForeignKey(g => g.ResponsibleUserId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(n => n.Id);
            entity.Property(n => n.Title).IsRequired().HasMaxLength(250);
            entity.Property(n => n.Message).IsRequired().HasMaxLength(1000);
            entity.Property(n => n.Key).IsRequired().HasMaxLength(200);
            entity.HasIndex(n => new { n.CompanyId, n.Key }).IsUnique();
            entity.HasOne(n => n.Company).WithMany().HasForeignKey(n => n.CompanyId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Name).IsRequired().HasMaxLength(100);
            entity.Property(u => u.Email).IsRequired().HasMaxLength(255);
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.PasswordHash).IsRequired();

            entity.HasOne(u => u.Company)
                  .WithMany()
                  .HasForeignKey(u => u.CompanyId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}