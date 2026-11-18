# Inventario de migraciones después de B01

Este inventario documenta el corte del backend de comercio electrónico. No es una instrucción para borrar bases de datos existentes ni una promesa de migración de datos del proyecto de origen.

| Contexto | Migraciones en el código | Decisión de B01 | Próximo responsable |
|---|---|---|---|
| `AgroFlowDbContext` (turnos, SQLite) | `20260928005218_InitialAppointments`, `20260928031414_AddActiveAssociationUniqueIndex`, `20260928031441_AddVentanaStartAtUtc`, `20260928031515_ReconcileWindowCapacityToTwo` y snapshot | Conservar IDs, modelo y seed de la demo. La continuidad del historial exige seleccionar el mismo archivo: Git no traslada bases ignoradas al renombrar la API. Aplicar la [transición con respaldo y ruta absoluta](../development/local-demo-runbook.md#actualización-desde-la-demo-anterior-sin-perder-turnos) antes de arrancar sobre datos previos. | B02 amplía el modelo mediante migraciones nuevas, sin reescribir las ya aplicadas. |
| `AuthenticateContext` (Identity, histórico) | `20251201002436_InicialAuth` y snapshot en el historial de Git | B01 conservó el andamiaje separado. El corte de lecturas del MVP retiró ese contexto del código activo y agregó las tablas Identity a `AgroFlowDbContext`. | Las cuentas internas se provisionan con rol e ingenio en SQLite. Una base SQL Server preexistente requiere una migración de cuentas explícita. |
| Contexto de `Customer`, `Product` y `Order` | `20251201002217_Migrations` y snapshot | Retirar del código junto con sus entidades, repositorio, controladores y seed `customers.json`. | No aplica a B02: si hubiera datos reales del e-commerce, requieren un proyecto de migración distinto y aprobación explícita. |

`AddMvpIdentityAndAppointmentReadIndex` amplía el historial SQLite sin cambiar los IDs de turnos. Los perfiles comparten esquema y usan archivos separados por defecto. `LocalDemo` carga los datos de `AppointmentsSeeder`. El perfil MVP autentica las cuentas de Identity y ofrece un seed académico opcional. [Arranque del backend](../../backend/README.md#probar-las-lecturas-internas-del-mvp)

La elección SQLite para el MVP y WAL condicionado a pruebas, con PostgreSQL previsto antes del primer cliente real, se registra en [ADR-006](../architecture/decisions/ADR-006-sqlite-para-persistencia-del-mvp.md).
