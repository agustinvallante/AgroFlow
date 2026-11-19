# Reportes diarios y consulta de configuración en LocalDemo

## Why

La presentación necesita mostrar el impacto de un turno en un resumen por día y
los horarios y cupos efectivos del ingenio. Las pantallas del prototipo
conservaban esos lugares sin lecturas de la API.

## What Changes

- Reportes usa una fecha de franja y presenta totales, finalizados, cancelados,
  distribución por estado actual y cupos de ese día. Reutiliza los resúmenes de
  turnos y las franjas de la API.
- Configuración muestra el nombre y la zona horaria del ingenio, y permite
  consultar las franjas persistidas por fecha con horario, duración y capacidad.
- `GET /api/v1/mill` expone el nombre, la zona horaria y la fecha local actual
  del ingenio efectivo. La operación es de LocalDemo y usa la clave del dashboard.

## Impact

Frontend, [contrato HTTP](../../../docs/contracts/openapi.yaml) y una lectura
de la API sin migraciones. Las lecturas no alteran el calendario ni turnos.
La edición de parámetros operativos sigue fuera del alcance acordado en
[ADR-004](../../../docs/architecture/decisions/ADR-004-politicas-operativas-del-mvp.md).
Los rangos, las métricas históricas, la atribución al bot y la exportación
quedan fuera de este corte.
