namespace AgroFlow.Domain.Enums;

/// <summary>Internal role vocabulary; only operator/supervisor catalog access is currently approved.</summary>
public enum InternalRole
{
    OPERADOR = 1,
    SUPERVISOR = 2,
    GERENTE = 3,
    ADMINISTRADOR = 4
}
