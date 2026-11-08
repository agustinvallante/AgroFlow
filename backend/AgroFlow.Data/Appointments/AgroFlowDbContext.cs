using AgroFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AgroFlow.Data.Appointments;

/// <summary>
/// Contexto EF Core exclusivo de la demo local de turnos, sobre SQLite.
/// Separado de <see cref="AgroFlow.Data.AuthenticateContext"/> a propósito:
/// no depende de SQL Server/LocalDB ni de Identity, que la demo no necesita.
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
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();
    public DbSet<ChannelInstance> ChannelInstances => Set<ChannelInstance>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ConversationMessage> ConversationMessages => Set<ConversationMessage>();
    public DbSet<MessageProcessing> MessageProcessings => Set<MessageProcessing>();
    public DbSet<SeedRecord> SeedRecords => Set<SeedRecord>();
    public DbSet<LocalDemoTeamAssignment> LocalDemoTeamAssignments => Set<LocalDemoTeamAssignment>();

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
            // These preparatory audit timestamps are not persisted until B02 establishes their schema.
            eb.Ignore(t => t.CreatedAtUtc);
            eb.Ignore(t => t.InactivatedAtUtc);
            eb.Property(t => t.Name).HasMaxLength(150).IsRequired();
            eb.Property(t => t.DNI).HasMaxLength(20).IsRequired();
            eb.Property(t => t.NormalizedDni).HasMaxLength(20).IsRequired();
            eb.Property(t => t.WhatsApp).HasMaxLength(20).IsRequired();
            eb.Property(t => t.NormalizedWhatsApp).HasMaxLength(20).IsRequired();
            // DNI y WhatsApp son únicos dentro del sistema (datos-maestros,
            // RN-043/RN-044): un número de WhatsApp identifica a un único
            // transportista, que es lo que permite reconocer al remitente.
            eb.HasIndex(t => t.NormalizedWhatsApp).IsUnique();
            eb.HasIndex(t => t.NormalizedDni).IsUnique();
            eb.HasIndex(t => t.IngenioId);
        });

        modelBuilder.Entity<Camion>(eb =>
        {
            eb.ToTable("Camiones");
            // These preparatory audit timestamps are not persisted until B02 establishes their schema.
            eb.Ignore(c => c.CreatedAtUtc);
            eb.Ignore(c => c.InactivatedAtUtc);
            eb.Property(c => c.Plate).HasMaxLength(10).IsRequired();
            eb.Property(c => c.NormalizedPlate).HasMaxLength(10).IsRequired();
            eb.Property(c => c.FleetType).HasMaxLength(20).IsRequired();
            eb.HasIndex(c => new { c.IngenioId, c.NormalizedPlate }).IsUnique();
        });

        modelBuilder.Entity<Finca>(eb =>
        {
            eb.ToTable("Fincas");
            // These preparatory audit timestamps are not persisted until B02 establishes their schema.
            eb.Ignore(f => f.CreatedAtUtc);
            eb.Ignore(f => f.InactivatedAtUtc);
            eb.Property(f => f.Code).HasMaxLength(50).IsRequired();
            eb.Property(f => f.NormalizedCode).HasMaxLength(50).IsRequired();
            eb.Property(f => f.Name).HasMaxLength(150).IsRequired();
            eb.Property(f => f.LocationReference).HasMaxLength(200).IsRequired();
            eb.HasIndex(f => new { f.IngenioId, f.NormalizedCode }).IsUnique();
        });

        modelBuilder.Entity<TransportistaCamion>(eb =>
        {
            eb.ToTable("TransportistaCamiones");
            // These preparatory audit timestamps are not persisted until B02 establishes their schema.
            eb.Ignore(a => a.CreatedAtUtc);
            eb.Ignore(a => a.InactivatedAtUtc);
            // Evita asociaciones duplicadas para el mismo par, incluso si una
            // quedó inactiva: reactivar la existente en vez de insertar otra.
            eb.HasIndex(a => new { a.TransportistaId, a.CamionId }).IsUnique();
            // Un camión no puede tener más de un transportista activo a la
            // vez (openspec/specs/datos-maestros/spec.md). Constraint real en
            // BD; el seed y cualquier caso de uso futuro que cree o reactive
            // asociaciones deben manejar el conflicto si lo violan.
            eb.HasIndex(a => a.CamionId).IsUnique().HasFilter("IsActive = 1");
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

        // Metadatos de PR #47 que MasterDataService escribe en cada mutación.
        // Migrarlos no aprueba una política de auditoría (OD-009) ni de
        // idempotencia (OD-006): sólo hace persistible la fundación existente.
        modelBuilder.Entity<AuditEvent>(eb =>
        {
            eb.ToTable("AuditEvents");
            eb.Property(a => a.ActorIdentity).HasMaxLength(200).IsRequired();
            eb.Property(a => a.OccurredAtUtc).HasConversion(UtcConverter);
            eb.Property(a => a.Action).HasMaxLength(200).IsRequired();
            eb.Property(a => a.EntityType).HasMaxLength(100).IsRequired();
            eb.Property(a => a.EntityId).HasMaxLength(100).IsRequired();
            eb.Property(a => a.Result).HasMaxLength(50).IsRequired();
            eb.Property(a => a.CorrelationId).HasMaxLength(100).IsRequired();
            eb.Property(a => a.Reason).HasMaxLength(500);
            eb.Property(a => a.DetailsReference).HasMaxLength(200);
            eb.HasIndex(a => new { a.IngenioId, a.OccurredAtUtc });
        });

        modelBuilder.Entity<IdempotencyRecord>(eb =>
        {
            eb.ToTable("IdempotencyRecords");
            eb.Property(r => r.Scope).HasMaxLength(100).IsRequired();
            eb.Property(r => r.ActorIdentity).HasMaxLength(200).IsRequired();
            eb.Property(r => r.Operation).HasMaxLength(200).IsRequired();
            eb.Property(r => r.Key).HasMaxLength(200).IsRequired();
            eb.Property(r => r.InputHash).HasMaxLength(64).IsRequired();
            eb.Property(r => r.ResultCode).HasMaxLength(50).IsRequired();
            eb.Property(r => r.ResultReference).HasMaxLength(100);
            eb.Property(r => r.CreatedAtUtc).HasConversion(UtcConverter);
            eb.Property(r => r.CompletedAtUtc).HasConversion(UtcConverter);
            eb.HasIndex(r => new { r.Scope, r.ActorIdentity, r.Operation, r.Key }).IsUnique();
        });

        modelBuilder.Entity<ChannelInstance>(eb =>
        {
            eb.ToTable("ChannelInstances");
            eb.Property(c => c.Provider).HasMaxLength(30).IsRequired();
            eb.Property(c => c.InstanceName).HasMaxLength(ChannelInstance.MaxInstanceNameLength).IsRequired();
            eb.HasIndex(c => new { c.Provider, c.InstanceName }).IsUnique();
            eb.HasOne<Ingenio>().WithMany().HasForeignKey(c => c.IngenioId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Conversation>(eb =>
        {
            eb.ToTable("Conversations");
            eb.Property(c => c.RemoteAddress).HasMaxLength(WhatsAppAddress.MaxLength).IsRequired();
            eb.Property(c => c.AddressKind).HasConversion<string>().HasMaxLength(10).IsRequired();
            eb.Property(c => c.NormalizedPhone).HasMaxLength(20);
            eb.Property(c => c.CreatedAtUtc).HasConversion(UtcConverter);
            eb.Property(c => c.LastMessageAtUtc).HasConversion(UtcConverter);
            // Ámbito + proveedor + instancia + dirección: una conversación por
            // dirección remota dentro de cada instancia habilitada.
            eb.HasIndex(c => new { c.ChannelInstanceId, c.RemoteAddress }).IsUnique();
            eb.HasIndex(c => new { c.IngenioId, c.LastMessageAtUtc });
            eb.HasIndex(c => c.TransportistaId);
            eb.HasOne<Ingenio>().WithMany().HasForeignKey(c => c.IngenioId).OnDelete(DeleteBehavior.Restrict);
            eb.HasOne<ChannelInstance>().WithMany().HasForeignKey(c => c.ChannelInstanceId).OnDelete(DeleteBehavior.Restrict);
            eb.HasOne<Transportista>().WithMany().HasForeignKey(c => c.TransportistaId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ConversationMessage>(eb =>
        {
            eb.ToTable("ConversationMessages", t =>
            {
                t.HasCheckConstraint("CK_ConversationMessages_Direction_Shape",
                    "(Direction = 'Inbound' AND ProviderMessageId IS NOT NULL AND SendStatus IS NULL AND InReplyToMessageId IS NULL) " +
                    "OR (Direction = 'Outbound' AND SendStatus IS NOT NULL AND InReplyToMessageId IS NOT NULL)");
                t.HasCheckConstraint("CK_ConversationMessages_Text_Length",
                    $"length(Text) BETWEEN 1 AND {ConversationMessage.MaxTextLength}");
            });
            eb.Property(m => m.Direction).HasConversion<string>().HasMaxLength(10).IsRequired();
            eb.Property(m => m.ProviderMessageId).HasMaxLength(ConversationMessage.MaxProviderMessageIdLength);
            eb.Property(m => m.ContentType).HasMaxLength(20).IsRequired();
            eb.Property(m => m.Text).HasMaxLength(ConversationMessage.MaxTextLength).IsRequired();
            eb.Property(m => m.ProviderTimestampUtc).HasConversion(NullableUtcConverter);
            eb.Property(m => m.PersistedAtUtc).HasConversion(UtcConverter);
            eb.Property(m => m.SendStatus).HasConversion<string>().HasMaxLength(20);
            eb.Property(m => m.SendStatusUpdatedAtUtc).HasConversion(NullableUtcConverter);
            // Clave idempotente del proveedor dentro de la conversación: dos
            // webhooks con el mismo mensaje sólo pueden insertar una fila.
            eb.HasIndex(m => new { m.ConversationId, m.Direction, m.ProviderMessageId })
                .IsUnique().HasFilter("ProviderMessageId IS NOT NULL");
            // Una sola intención de respuesta por mensaje entrante.
            eb.HasIndex(m => m.InReplyToMessageId).IsUnique().HasFilter("InReplyToMessageId IS NOT NULL");
            // Conversation.NextMessageTimestamp hace estrictamente creciente el
            // instante de persistencia: el índice único lo garantiza y permite
            // paginar sólo por ese valor, sin empates.
            eb.HasIndex(m => new { m.ConversationId, m.PersistedAtUtc }).IsUnique();
            eb.HasOne<Conversation>().WithMany().HasForeignKey(m => m.ConversationId).OnDelete(DeleteBehavior.Restrict);
            eb.HasOne<ConversationMessage>().WithMany().HasForeignKey(m => m.InReplyToMessageId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MessageProcessing>(eb =>
        {
            eb.ToTable("MessageProcessings");
            eb.Property(p => p.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            eb.Property(p => p.ClaimTokenHash).HasMaxLength(64).IsRequired();
            eb.Property(p => p.ClaimedAtUtc).HasConversion(UtcConverter);
            eb.Property(p => p.ClaimExpiresAtUtc).HasConversion(UtcConverter);
            eb.Property(p => p.CompletedAtUtc).HasConversion(NullableUtcConverter);
            eb.Property(p => p.LastErrorCode).HasMaxLength(MessageProcessing.MaxErrorCodeLength);
            eb.HasIndex(p => p.InboundMessageId).IsUnique();
            eb.HasIndex(p => new { p.IngenioId, p.Status });
            eb.HasOne<ConversationMessage>().WithOne().HasForeignKey<MessageProcessing>(p => p.InboundMessageId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SeedRecord>(eb =>
        {
            eb.ToTable("SeedRecords");
            eb.HasKey(r => r.Key);
            eb.Property(r => r.Key).HasMaxLength(SeedRecord.MaxKeyLength);
            eb.Property(r => r.CreatedAtUtc).HasConversion(UtcConverter);
        });

        modelBuilder.Entity<LocalDemoTeamAssignment>(eb =>
        {
            eb.ToTable("LocalDemoTeamAssignments");
            eb.HasKey(a => a.SeedId);
            eb.Property(a => a.SeedId).HasMaxLength(SeedRecord.MaxKeyLength);
            eb.Property(a => a.Fingerprint).HasMaxLength(LocalDemoTeamAssignment.FingerprintLength).IsRequired();
            eb.Property(a => a.AppliedAtUtc).HasConversion(UtcConverter);
            eb.HasIndex(a => a.TransportistaId).IsUnique();
        });
    }

    // SQLite devuelve DateTime sin Kind; estas columnas siempre guardan UTC.
    private static readonly ValueConverter<DateTime, DateTime> UtcConverter = new(
        value => value, value => DateTime.SpecifyKind(value, DateTimeKind.Utc));

    private static readonly ValueConverter<DateTime?, DateTime?> NullableUtcConverter = new(
        value => value, value => value.HasValue ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) : value);
}
