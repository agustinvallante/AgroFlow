# Inventario de migraciones después de B01

Este inventario documenta el corte del backend de comercio electrónico. No es una instrucción para borrar bases de datos existentes ni una promesa de migración de datos del proyecto de origen.

| Contexto | Migraciones en el código | Decisión de B01 | Próximo responsable |
|---|---|---|---|
| `AgroFlowDbContext` (turnos, SQLite) | `20260928005218_InitialAppointments`, `20260928031414_AddActiveAssociationUniqueIndex`, `20260928031441_AddVentanaStartAtUtc`, `20260928031515_ReconcileWindowCapacityToTwo` y snapshot | Conservar IDs, modelo y seed de la demo. La continuidad del historial exige seleccionar el mismo archivo: Git no traslada bases ignoradas al renombrar la API. Aplicar la [transición con respaldo y ruta absoluta](../development/local-demo-runbook.md#actualización-desde-la-demo-anterior-sin-perder-turnos) antes de arrancar sobre datos previos. | B02 amplía el modelo mediante migraciones nuevas, sin reescribir las ya aplicadas. |
| `AuthenticateContext` (Identity) | `20251201002436_InicialAuth` y snapshot | Conservar como andamiaje técnico separado. No provee todavía usuarios/roles por ingenio ni el login MVP. | B03 decide y documenta su adaptación a la persistencia elegida para el MVP; no asumir que la base SQL Server heredada se reutiliza. |
| Contexto de `Customer`, `Product` y `Order` | `20251201002217_Migrations` y snapshot | Retirar del código junto con sus entidades, repositorio, controladores y seed `customers.json`. | No aplica a B02: si hubiera datos reales del e-commerce, requieren un proyecto de migración distinto y aprobación explícita. |

`admins.json` tampoco se carga al arrancar: nunca fue una fuente aceptada para usuarios del MVP y podría contener credenciales. La demo local sólo necesita SQLite y datos ficticios generados por `AppointmentsSeeder`. Fuera de `LocalDemo`, Identity/JWT permanece como base para B03 y necesita configuración local segura; no se lo debe presentar como un flujo de acceso MVP funcional.

La elección SQLite para el MVP y WAL condicionado a pruebas, con PostgreSQL previsto antes del primer cliente real, se registra en ADR-006 de la PR #76 cuando ese cambio documental se integre a `main`. Hasta entonces, esta nota sólo describe el estado del código y no implementa una migración de proveedor.
