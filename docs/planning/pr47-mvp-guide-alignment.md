# Alineación documental de PR #47 con la guía MVP

> **Alcance:** reconciliación documental de la fundación compatible, todavía no habilitada. Esta guía no certifica CRUD, la API del MVP ni la aceptación del MVP completo.

## Guía 1–9: reconciliación y estado observado

| Guía | Alineación | Estado y límite |
|---|---|---|
| 1. Estructura | Priorizar `AgroFlow.sln`, proyectos `AgroFlow.*`, API/persistencia y el retiro ecommerce de main. Conservar menciones históricas intencionales en detector/runbook/tests; no restaurarlas como dominio activo. | No se modifica la estructura en esta corrección documental. |
| 2. Acuerdos | Una asociación activa por camión; ventana automática compatible, sin preferencia; corte y carga obligatorios; capacidad/duración por ingenio desde datos de arranque; catálogos para operador/supervisor conforme a las specs. | Demo de 30 min/cupo 2 son datos de seed, no constantes. Prioridad aprobada no redondea por minuto. Campos, gestión externa y permisos adicionales siguen pendientes. |
| 3. Persistencia | SQLite académico conforme a ADR-006; PostgreSQL como destino futuro; WAL condicionado a prueba. La demo conserva su esquema SQLite actual. | Se ignoran explícitamente ocho propiedades de timestamp no migradas: no persisten y no acreditan historial durable. B02 debe definir/migrar el esquema antes de habilitar la fundación. |
| 4. Asociación | Se conserva la cardinalidad de una asociación activa por camión y su historial conceptual. | Alta/reactivación, pertenencia, contrato externo y concurrencia dependen de OD-007/OD-013/B00; no se publican aquí. |
| 5. Seguridad | Mantener Identity/JWT de main y al backend como autoridad. `LocalDemo` sin autenticación es solo demo de turnos. | No se agrega bypass de maestros ni cliente público. No se decide sesión web, permisos adicionales ni matriz completa. |
| 6. SQLite | Diferenciar restricciones, FK, concurrencia y bloqueo; WAL no resuelve transacciones ni idempotencia. | OD-006 sigue abierto; `IdempotencyRecord` no aprueba `Idempotency-Key`. `AuditEvent` no completa OD-009. No se afirma haber probado recorridos de carrera o restauración. |
| 7. Contrato | OpenAPI conserva `0.3.0`, salud y operaciones demo de appointments, esquemas/status/filtros/errores, `AppointmentProblemDetails` y `security: []` por operación demo. | Rutas/DTO/headers propuestos para maestros quedan fuera del OpenAPI activo. B38, CRUD HTTP y MVP no se declaran completos. |
| 8. Frontend | React/Vite se mantiene por ADR-005. La cola de turnos tiene integración HTTP de demo; otras vistas permanecen mock. | No se exige completar pantallas ni adoptar Next/BFF/TanStack/Zustand. Los mocks no acreditan recorridos obligatorios del MVP. |
| 9. Evidencia | Separar el CI antiguo, la verificación histórica y el candidato integrado con main. | El CI `38101125508` sólo describe `efe8c7c`. Las correcciones se publicaron en `34f1c3a`/`25784be` y la documentación en `f2b6a93`. La sincronización autorizada `32d63ac` incorpora main `a6b38e2`; sus suites locales pasaron 209/209 backend y 37/37 frontend, con auditoría npm sin vulnerabilidades. La CI y decisión de revisión del head final se enlazan en la PR; no se declaran B03/B38/G01–G12 ni MVP completo. |

### Identidad de las evidencias históricas previas a la sincronización

- **Base publicada:** `efe8c7c8c00d8faad5c4c912c6adf5e57b3d1ea1`. El CI [38101125508](https://github.com/agustinvallante/AgroFlow/actions/runs/38101125508) corresponde a esa revisión anterior, no al diff local.
- **Candidato probado:** el HEAD `efe8c7c8c00d8faad5c4c912c6adf5e57b3d1ea1` más el diff local con orden de asociaciones por `Id` persistido y filtro de proyección por `truck.IngenioId`. Las mismas fuentes se consolidaron después en los commits `34f1c3ad624acdd244e96ca8e838aa3c920e98ff` y `25784be056b0c94d932325e146ff2092eae419de`; la suite no se repitió en esos SHAs.
- **Ventana observada:** 2026-10-19 18:37:12–18:40:16 UTC. Runtime: Node 24.19.0, npm 11.17.0 y .NET 8.0.425.
- **Resultados:** `dotnet restore backend/AgroFlow.sln` pasó; build Release sin warnings/errores; tests backend 209/209, sin fallas ni omitidos. Frontend `npm ci`, typecheck, lint, test (37/37 en cinco archivos) y build pasaron. OpenSpec estricto: 17/17; no hay cambios archivados. Redocly fue válido con un warning sobre health sin 4XX; enlaces locales: 107 archivos.
- **Avisos no corregidos:** cinco warnings de lint `setState`, un warning Redocly sobre 4XX de health y un advisory alto reportado por `npm ci`. No se afirma remediación de seguridad.

El verificador ejecutó cada comando por separado; para frontend usó `cd frontend && <comando>`. El bloque siguiente es una **reproducción equivalente y segura para copiar**: cada comando frontend cambia de directorio en un subshell, sin afectar los siguientes comandos.

```sh
timeout 300s dotnet restore backend/AgroFlow.sln
timeout 180s dotnet build backend/AgroFlow.sln --configuration Release --no-restore
timeout 180s dotnet test backend/AgroFlow.sln --configuration Release --no-build --no-restore
(cd frontend && timeout 300s npm ci)
(cd frontend && timeout 180s npm run typecheck)
(cd frontend && timeout 180s npm run lint)
(cd frontend && timeout 180s env CI=true npm test)
(cd frontend && timeout 180s npm run build)
```

Los launchers cacheados de OpenSpec 1.13.1 y Redocly 2.53.3 se invocaron directamente; no se descargaron herramientas. Para reproducir en un entorno con acceso a paquetes se pueden usar `npx --yes @fission-ai/openspec@1.13.1` y `npx --yes @redocly/cli@2.53.3`; son equivalentes sugeridos, no la forma observada en esa ejecución.

## Dependencias y límites pendientes

- B00/B02/B03/B05: alcance/API, esquema y migraciones de maestros, Identity y contexto segregado.
- B38: proyección aprobada; no se declara completada.
- OD-006: concurrencia, idempotencia y reintentos.
- OD-007/OD-013: gestión externa de asociación, campos/validaciones y permisos restantes. La normalización de WhatsApp sigue como propuesta pendiente.
- OD-009: eventos, actor, conservación y acceso de auditoría.
- OD-011/OD-012: operación/recuperación e indicadores/dashboard.
- Sesión web: no se selecciona BFF, cookie opaca, token en navegador ni sesión directa.
- Rutas, DTO y paginación de maestros: no están en OpenAPI activo hasta aprobación.

El DNI de solo dígitos sí es un acuerdo vigente de la spec canónica de datos maestros. Esta reconciliación no modifica specs canónicas ni archiva OD-013.

## Sincronización posterior y evidencia actual

El mantenedor autorizó ajustar y verificar esta misma PR antes de aprobar/fusionar. El commit `32d63ac5562ff86f8b098ae7270c1d4f1253a180` incorpora main `a6b38e2400fab10be9fd7fefb1e1d07496ee3ca6`. El conflicto de `AgroFlow.Tests.csproj` se resolvió conservando TestHost/MVC Testing 8.0.31 y SQLite 9.0.7. Se mantienen los workflows y el parche de seguridad `source-map-js` 1.2.2 de main.

Sobre esa integración se repitieron restore/build, backend 209/209 sin omitidos y frontend 37/37, typecheck/lint/build; `npm audit --audit-level=high` pasó con cero vulnerabilidades. Es evidencia nueva y distinta del snapshot anterior que todavía reportaba el advisory. Los cinco avisos de lint siguen visibles. La validación contractual/documental y la CI del head final se registran con su SHA en [PR #47](https://github.com/agustinvallante/AgroFlow/pull/47); una ejecución local no se presenta como CI remota.

## Revisión y tamaño

Revisar primero el contrato activo y su propuesta no normativa, después los documentos de estado, ADR-005/006 y OpenSpec. Las cifras anteriores de 81 archivos y +3753/-338 describían un snapshot previo; el diff vigente se consulta en GitHub. El mantenedor decidió continuar la revisión e integración en esta misma PR, sin exigir reconstruir una cadena Next/SQL obsoleta. La aceptación sigue limitada a una fundación no habilitada y condicionada a las verificaciones del candidato final.

La validación documental histórica posterior a los commits de código pasó OpenSpec 17/17, archived sin cambios, Redocly válido con un warning de health sin 4XX, enlaces en 108 archivos y whitespace limpio. No sustituye la CI de la sincronización posterior. Los documentos ya se publicaron en `f2b6a93` y la PR salió de Draft; este registro no los presenta como pendientes de publicación. El estado de revisión, los checks y cualquier merge posterior constan en la PR. Ninguna de esas acciones acredita por sí sola CRUD operativo o MVP aceptado.
