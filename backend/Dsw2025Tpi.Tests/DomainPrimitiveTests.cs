using System.Reflection;
using Dsw2025Tpi.Domain.Entities;
using Dsw2025Tpi.Domain.Enums;
using Dsw2025Tpi.Domain.Interfaces;
using Xunit;

namespace Dsw2025Tpi.Tests;

public sealed class DomainPrimitiveTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Ingenio_rejects_blank_name(string? name)
    {
        var exception = Assert.Throws<ArgumentException>(() => new Ingenio(name!));

        Assert.Equal("name", exception.ParamName);
    }

    [Fact]
    public void Ingenio_trims_name_and_is_active_by_default()
    {
        var ingenio = new Ingenio("  Ingenio Norte  ");

        Assert.Equal("Ingenio Norte", ingenio.Name);
        Assert.True(ingenio.IsActive);
    }

    [Fact]
    public void AuditEvent_rejects_empty_ingenio_id()
    {
        var exception = Assert.Throws<ArgumentException>(() => CreateAuditEvent(ingenioId: Guid.Empty));

        Assert.Equal("ingenioId", exception.ParamName);
    }

    [Theory]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void AuditEvent_rejects_non_utc_timestamp(DateTimeKind kind)
    {
        var timestamp = DateTime.SpecifyKind(new DateTime(2025, 1, 2, 3, 4, 5), kind);
        var exception = Assert.Throws<ArgumentException>(() => CreateAuditEvent(occurredAtUtc: timestamp));

        Assert.Equal("occurredAtUtc", exception.ParamName);
    }

    [Fact]
    public void AuditEvent_retains_audit_metadata()
    {
        var ingenioId = Guid.NewGuid();
        var occurredAt = new DateTime(2025, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        var auditEvent = CreateAuditEvent(
            ingenioId: ingenioId,
            occurredAtUtc: occurredAt,
            reason: "  approved  ",
            detailsReference: "  audit/123  ");

        Assert.Equal(ingenioId, auditEvent.IngenioId);
        Assert.Equal("actor-1", auditEvent.ActorIdentity);
        Assert.Equal(occurredAt, auditEvent.OccurredAtUtc);
        Assert.Equal("TurnApproved", auditEvent.Action);
        Assert.Equal("Turn", auditEvent.EntityType);
        Assert.Equal("turn-123", auditEvent.EntityId);
        Assert.Equal("Succeeded", auditEvent.Result);
        Assert.Equal("correlation-123", auditEvent.CorrelationId);
        Assert.Equal("approved", auditEvent.Reason);
        Assert.Equal("audit/123", auditEvent.DetailsReference);
    }

    [Fact]
    public void IdempotencyRecord_rejects_completion_before_creation()
    {
        var createdAt = new DateTime(2025, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        var completedAt = createdAt.AddTicks(-1);
        var exception = Assert.Throws<ArgumentException>(() => CreateIdempotencyRecord(
            createdAtUtc: createdAt,
            completedAtUtc: completedAt));

        Assert.Equal("completedAtUtc", exception.ParamName);
    }

    [Fact]
    public void IdempotencyRecord_retains_key_and_operation_metadata()
    {
        var createdAt = new DateTime(2025, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        var completedAt = createdAt.AddMinutes(1);
        var record = CreateIdempotencyRecord(
            key: "  request-456  ",
            createdAtUtc: createdAt,
            completedAtUtc: completedAt);

        Assert.Equal("tenant:ingenio-1", record.Scope);
        Assert.Equal("actor-1", record.ActorIdentity);
        Assert.Equal("CreateTurn", record.Operation);
        Assert.Equal("request-456", record.Key);
        Assert.Equal("hash-789", record.InputHash);
        Assert.Equal("Created", record.ResultCode);
        Assert.Equal("turn-123", record.ResultReference);
        Assert.Equal(createdAt, record.CreatedAtUtc);
        Assert.Equal(completedAt, record.CompletedAtUtc);
    }

    [Fact]
    public void InternalRole_has_exact_approved_values()
    {
        Assert.Equal(
            new[]
            {
                ("OPERADOR", 1),
                ("SUPERVISOR", 2),
                ("GERENTE", 3),
                ("ADMINISTRADOR", 4)
            },
            Enum.GetNames<InternalRole>()
                .Select(name => (name, (int)Enum.Parse<InternalRole>(name)))
                .ToArray());
    }

    [Fact]
    public void ITenantContext_exposes_only_getter_properties()
    {
        var properties = typeof(ITenantContext).GetProperties();

        Assert.Equal(2, properties.Length);
        Assert.All(properties, property =>
        {
            Assert.NotNull(property.GetMethod);
            Assert.True(property.GetMethod!.IsPublic);
            Assert.Null(property.SetMethod);
        });
        Assert.Contains(properties, property => property.Name == nameof(ITenantContext.IngenioId)
            && property.PropertyType == typeof(Guid));
        Assert.Contains(properties, property => property.Name == nameof(ITenantContext.ActorIdentity)
            && property.PropertyType == typeof(string));
    }

    private static AuditEvent CreateAuditEvent(
        Guid? ingenioId = null,
        DateTime? occurredAtUtc = null,
        string? reason = null,
        string? detailsReference = null) => new(
            ingenioId ?? Guid.NewGuid(),
            "actor-1",
            occurredAtUtc ?? new DateTime(2025, 1, 2, 3, 4, 5, DateTimeKind.Utc),
            "TurnApproved",
            "Turn",
            "turn-123",
            "Succeeded",
            "correlation-123",
            reason,
            detailsReference);

    private static IdempotencyRecord CreateIdempotencyRecord(
        string key = "request-456",
        DateTime? createdAtUtc = null,
        DateTime? completedAtUtc = null)
    {
        var created = createdAtUtc ?? new DateTime(2025, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        return new IdempotencyRecord(
            "tenant:ingenio-1",
            "actor-1",
            "CreateTurn",
            key,
            "hash-789",
            "Created",
            "turn-123",
            created,
            completedAtUtc ?? created.AddMinutes(1));
    }
}
