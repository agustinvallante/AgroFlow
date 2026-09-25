using Dsw2025Tpi.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dsw2025Tpi.Data;

public class Dsw2025TpiContext : DbContext
{
    public Dsw2025TpiContext(DbContextOptions<Dsw2025TpiContext> options)
        : base(options)
    {

    }

    public DbSet<Ingenio> Ingenios => Set<Ingenio>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();
    public DbSet<Transportista> Transportistas => Set<Transportista>();
    public DbSet<Camion> Camiones => Set<Camion>();
    public DbSet<Finca> Fincas => Set<Finca>();
    public DbSet<TransportistaCamion> TransportistasCamiones => Set<TransportistaCamion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Ingenio>(eb =>
        {
            eb.ToTable("Ingenios");
            eb.HasKey(x => x.Id);
            eb.Property(x => x.Name)
                .HasMaxLength(120)
                .IsRequired();
            eb.Property(x => x.IsActive)
                .IsRequired();
        });

        modelBuilder.Entity<AuditEvent>(eb =>
        {
            eb.ToTable("AuditEvents");
            eb.HasKey(x => x.Id);
            eb.Property(x => x.ActorIdentity)
                .HasMaxLength(160)
                .IsRequired();
            eb.Property(x => x.Action)
                .HasMaxLength(100)
                .IsRequired();
            eb.Property(x => x.EntityType)
                .HasMaxLength(100)
                .IsRequired();
            eb.Property(x => x.EntityId)
                .HasMaxLength(100)
                .IsRequired();
            eb.Property(x => x.Result)
                .HasMaxLength(40)
                .IsRequired();
            eb.Property(x => x.CorrelationId)
                .HasMaxLength(100)
                .IsRequired();
            eb.Property(x => x.Reason)
                .HasMaxLength(500);
            eb.Property(x => x.DetailsReference)
                .HasMaxLength(500);
            eb.Property(x => x.OccurredAtUtc)
                .HasColumnType("datetime2")
                .IsRequired();
            eb.HasIndex(x => new { x.IngenioId, x.OccurredAtUtc });
            eb.HasOne<Ingenio>()
                .WithMany()
                .HasForeignKey(x => x.IngenioId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<IdempotencyRecord>(eb =>
        {
            eb.ToTable("IdempotencyRecords");
            eb.HasKey(x => x.Id);
            eb.Property(x => x.Scope)
                .HasMaxLength(120)
                .IsRequired();
            eb.Property(x => x.ActorIdentity)
                .HasMaxLength(160)
                .IsRequired();
            eb.Property(x => x.Operation)
                .HasMaxLength(120)
                .IsRequired();
            eb.Property(x => x.Key)
                .HasMaxLength(200)
                .IsRequired();
            eb.Property(x => x.InputHash)
                .HasMaxLength(128)
                .IsRequired();
            eb.Property(x => x.ResultCode)
                .HasMaxLength(50)
                .IsRequired();
            eb.Property(x => x.ResultReference)
                .HasMaxLength(200);
            eb.Property(x => x.CreatedAtUtc)
                .HasColumnType("datetime2")
                .IsRequired();
            eb.Property(x => x.CompletedAtUtc)
                .HasColumnType("datetime2")
                .IsRequired();
            eb.HasIndex(x => new { x.Scope, x.ActorIdentity, x.Operation, x.Key })
                .IsUnique();
        });

        modelBuilder.Entity<Transportista>(eb =>
        {
            eb.ToTable("Transportistas");
            eb.HasKey(x => x.Id);
            eb.Property(x => x.Name).HasMaxLength(160).IsRequired();
            eb.Property(x => x.DNI).HasMaxLength(40).IsRequired();
            eb.Property(x => x.NormalizedDni).HasMaxLength(40).IsRequired();
            eb.Property(x => x.WhatsApp).HasMaxLength(40).IsRequired();
            eb.Property(x => x.NormalizedWhatsApp).HasMaxLength(40).IsRequired();
            eb.Property(x => x.IsActive).IsRequired();
            eb.Property(x => x.CreatedAtUtc).HasColumnType("datetime2").IsRequired();
            eb.Property(x => x.InactivatedAtUtc).HasColumnType("datetime2");
            eb.HasAlternateKey(x => new { x.IngenioId, x.Id });
            eb.HasIndex(x => x.NormalizedDni).IsUnique();
            eb.HasIndex(x => x.NormalizedWhatsApp).IsUnique();
            eb.HasOne<Ingenio>()
                .WithMany()
                .HasForeignKey(x => x.IngenioId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Camion>(eb =>
        {
            eb.ToTable("Camiones");
            eb.HasKey(x => x.Id);
            eb.Property(x => x.Plate).HasMaxLength(30).IsRequired();
            eb.Property(x => x.NormalizedPlate).HasMaxLength(30).IsRequired();
            eb.Property(x => x.FleetType).HasMaxLength(20).IsRequired();
            eb.Property(x => x.IsActive).IsRequired();
            eb.Property(x => x.CreatedAtUtc).HasColumnType("datetime2").IsRequired();
            eb.Property(x => x.InactivatedAtUtc).HasColumnType("datetime2");
            eb.HasAlternateKey(x => new { x.IngenioId, x.Id });
            eb.HasIndex(x => x.NormalizedPlate).IsUnique();
            eb.HasOne<Ingenio>()
                .WithMany()
                .HasForeignKey(x => x.IngenioId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Finca>(eb =>
        {
            eb.ToTable("Fincas");
            eb.HasKey(x => x.Id);
            eb.Property(x => x.Code).HasMaxLength(40).IsRequired();
            eb.Property(x => x.Name).HasMaxLength(160).IsRequired();
            eb.Property(x => x.LocationReference).HasMaxLength(300).IsRequired();
            eb.Property(x => x.IsActive).IsRequired();
            eb.Property(x => x.CreatedAtUtc).HasColumnType("datetime2").IsRequired();
            eb.Property(x => x.InactivatedAtUtc).HasColumnType("datetime2");
            eb.HasAlternateKey(x => new { x.IngenioId, x.Id });
            eb.HasIndex(x => new { x.IngenioId, x.Code }).IsUnique();
            eb.HasOne<Ingenio>()
                .WithMany()
                .HasForeignKey(x => x.IngenioId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TransportistaCamion>(eb =>
        {
            eb.ToTable("TransportistasCamiones");
            eb.HasKey(x => x.Id);
            eb.Property(x => x.IsActive).IsRequired();
            eb.Property(x => x.CreatedAtUtc).HasColumnType("datetime2").IsRequired();
            eb.Property(x => x.InactivatedAtUtc).HasColumnType("datetime2");
            eb.HasIndex(x => new { x.IngenioId, x.TransportistaId, x.CamionId })
                .IsUnique()
                .HasFilter("[IsActive] = 1");
            eb.HasOne(x => x.Transportista)
                .WithMany()
                .HasForeignKey(x => new { x.IngenioId, x.TransportistaId })
                .HasPrincipalKey(x => new { x.IngenioId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            eb.HasOne(x => x.Camion)
                .WithMany()
                .HasForeignKey(x => new { x.IngenioId, x.CamionId })
                .HasPrincipalKey(x => new { x.IngenioId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Customer>(eb =>
        {
            eb.Property(p => p.Name)
            .HasMaxLength(50)
            .IsRequired();

            eb.Property(p => p.Email)
            .HasMaxLength(60)
            .IsRequired();

            // AGREGAR ESTO: Para asegurar que no haya dos clientes con el mismo mail
            eb.HasIndex(p => p.Email).IsUnique();

            eb.Property(p => p.PhoneNumber)
            .HasMaxLength(17)
            .IsRequired(false); // <--- CAMBIO IMPORTANTE: Ahora es opcional
        });



        modelBuilder.Entity<Product>(eb =>
        {
            eb.ToTable("Products");
            eb.Property(P => P.Sku)
            .HasMaxLength(50)
            .IsRequired();

            eb.Property(P => P.InternalCode)
            .HasMaxLength(50)
            .IsRequired();

            eb.Property(P => P.Name)
            .HasMaxLength(50)
            .IsRequired();

            eb.Property(p => p.Description)
            .HasMaxLength(200);

            eb.Property(P => P.CurrentUnitPrice)
            .HasPrecision(15, 2)
            .IsRequired();

            eb.Property(p => p.StockQuantity)
            .IsRequired();

        });

        modelBuilder.Entity<Order>(eb =>
        {
            eb.ToTable("Orders");

            eb.Property(P => P.ShippingAddress)
            .HasMaxLength(150)
            .IsRequired();

            eb.Property(P => P.BillingAddress)
            .HasMaxLength(150)
            .IsRequired();

            eb.Property(p => p.TotalAmount)
            .HasPrecision(15, 2)
            .IsRequired();

            eb.Property(p => p.Notes)
            .HasMaxLength(300);
        });

        modelBuilder.Entity<OrderItem>(eb =>
        {
            eb.ToTable("OrderItems");

            eb.Property(p => p.Quantity)
                .IsRequired();

            eb.Property(p => p.UnitPrice)
            .HasPrecision(15, 2)
            .IsRequired();

            eb.Ignore(p => p.Subtotal);

        });
    }
}
