using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AgroFlow.Data;

/// <summary>
/// Distingue violaciones de unicidad por su código extendido de SQLite. Las
/// fallas de clave foránea, check, trigger o BUSY/LOCKED conservan su
/// excepción original del proveedor.
/// </summary>
public static class SqliteConstraintErrors
{
    private const int SqliteConstraintPrimaryKey = 1555;
    private const int SqliteConstraintUnique = 2067;
    private const int SqliteConstraintRowId = 2579;

    public static bool IsUniqueOrPrimaryKeyViolation(DbUpdateException exception)
    {
        for (var current = exception.InnerException; current is not null; current = current.InnerException)
        {
            if (current is SqliteException sqlite && sqlite.SqliteExtendedErrorCode is
                SqliteConstraintPrimaryKey or SqliteConstraintUnique or SqliteConstraintRowId)
                return true;
        }
        return false;
    }
}
