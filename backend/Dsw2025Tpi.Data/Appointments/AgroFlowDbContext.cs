using Dsw2025Tpi.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dsw2025Tpi.Data.Appointments;

/// <summary>
/// Contexto EF Core exclusivo de la demo local de turnos, sobre SQLite.
/// Separado de <see cref="Dsw2025TpiContext"/> a propósito: no depende de
/// SQL Server/LocalDB ni de Identity, que la demo no necesita.
/// </summary>
public sealed class AgroFlowDbContext : DbContext
{
    public AgroFlowDbContext(DbContextOptions<AgroFlowDbContext> options)
        : base(options)
    {
    }

    public DbSet<Ingenio> Ingenios => Set<Ingenio>();
    public DbSet<Transportista> Transportistas => Set<Transportista>();
    public DbSet<Camion> Camiones => Set<Camion>();
    public DbSet<Finca> Fincas => Set<Finca>();
    public DbSet<TransportistaCamion> Asociaciones => Set<TransportistaCamion>();
    public DbSet<Ventana> Ventanas => Set<Ventana>();
    public DbSet<Turno> Turnos => Set<Turno>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Ingenio>(eb =>
        {
            eb.ToTable("Ingenios");
            eb.Property(i => i.Name).HasMaxLength(100).IsRequired();
            eb.Property(i => i.TimeZoneId).HasMaxLength(100).IsRequired();
        });

        modelBuilder.Entity<Transportista>(eb =>
        {
            eb.ToTable("Transportistas");
            eb.Property(t => t.Name).HasMaxLength(150).IsRequired();
            eb.Property(t => t.DNI).HasMaxLength(20).IsRequired();
            eb.Property(t => t.NormalizedDni).HasMaxLength(20).IsRequired();
            eb.Property(t => t.WhatsApp).HasMaxLength(20).IsRequired();
            eb.Property(t => t.NormalizedWhatsApp).HasMaxLength(20).IsRequired();
            eb.HasIndex(t => new { t.IngenioId, t.NormalizedWhatsApp }).IsUnique();
        });

        modelBuilder.Entity<Camion>(eb =>
        {
            eb.ToTable("Camiones");
            eb.Property(c => c.Plate).HasMaxLength(10).IsRequired();
            eb.Property(c => c.NormalizedPlate).HasMaxLength(10).IsRequired();
            eb.Property(c => c.FleetType).HasMaxLength(20).IsRequired();
            eb.HasIndex(c => new { c.IngenioId, c.NormalizedPlate }).IsUnique();
        });

        modelBuilder.Entity<Finca>(eb =>
        {
            eb.ToTable("Fincas");
            eb.Property(f => f.Code).HasMaxLength(50).IsRequired();
            eb.Property(f => f.NormalizedCode).HasMaxLength(50).IsRequired();
            eb.Property(f => f.Name).HasMaxLength(150).IsRequired();
            eb.Property(f => f.LocationReference).HasMaxLength(200).IsRequired();
            eb.HasIndex(f => new { f.IngenioId, f.NormalizedCode }).IsUnique();
        });

        modelBuilder.Entity<TransportistaCamion>(eb =>
        {
            eb.ToTable("TransportistaCamiones");
            // Evita asociaciones duplicadas para el mismo par, incluso si una
            // quedó inactiva: reactivar la existente en vez de insertar otra.
            eb.HasIndex(a => new { a.TransportistaId, a.CamionId }).IsUnique();
        });

        modelBuilder.Entity<Ventana>(eb =>
        {
            eb.ToTable("Ventanas", t => t.HasCheckConstraint(
                "CK_Ventanas_Occupied_Range",
                "Occupied >= 0 AND Occupied <= Capacity"));
            eb.HasIndex(v => new { v.IngenioId, v.StartAt });
        });

        modelBuilder.Entity<Turno>(eb =>
        {
            eb.ToTable("Turnos");
            eb.Property(t => t.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            // Único turno no terminal por camión: constraint real, no sólo en
            // memoria. IsActive se mantiene en falso apenas el estado es
            // terminal (ver Turno.SetStatus / EfAppointmentStore).
            eb.HasIndex(t => t.CamionId).IsUnique().HasFilter("IsActive = 1");
            eb.HasIndex(t => new { t.IngenioId, t.VentanaId });
        });
    }
}
