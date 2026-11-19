using AgroFlow.Data.Appointments;
using AgroFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AgroFlow.Tests;

/// <summary>
/// Writes a <see cref="Turno"/> into a database migrated to a schema older than
/// AddAppointmentDeparture. The current entity maps DepartedAtUtc, so adding it
/// through EF would target a column that the legacy schema does not have yet.
/// </summary>
internal static class LegacyTurnoRows
{
    public static Task<int> InsertAsync(AgroFlowDbContext db, Turno turno) =>
        db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO Turnos
                (Id, IngenioId, TransportistaId, CamionId, FincaId, VentanaId, CutAt, EstimatedLoadTons, Status, IsActive, CreatedAt)
            VALUES
                ({turno.Id}, {turno.IngenioId}, {turno.TransportistaId}, {turno.CamionId}, {turno.FincaId}, {turno.VentanaId},
                 {turno.CutAt}, {turno.EstimatedLoadTons}, {turno.Status.ToString()}, {turno.IsActive}, {turno.CreatedAt})
            """);
}
