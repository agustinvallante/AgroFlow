# ADR-FE-004: TanStack Query para estado remoto

- Estado: aceptada para la arquitectura objetivo
- Alcance: consultas, mutaciones, caché e invalidación en el cliente
- Implementación: pendiente

## Contexto

El frontend necesita representar datos cuyo propietario es el backend, manejar carga y error, e invalidar vistas relacionadas después de una mutación confirmada. Guardar estas respuestas como estado global manual duplicaría responsabilidades y favorecería datos obsoletos.

## Decisión

TanStack Query SHALL administrar el estado remoto utilizado por componentes cliente. Las claves, políticas de vigencia, reintentos e invalidaciones se definirán por capacidad y contrato.

Una mutación operativa no se mostrará como confirmada antes de la respuesta exitosa del backend. Las actualizaciones optimistas sobre estado operativo crítico quedan fuera de la base salvo decisión posterior con reconciliación explícita.

## Alternativas consideradas

- **Fetch y estado React manual:** suficiente para casos aislados, pero obliga a recrear caché, deduplicación e invalidación.
- **Zustand para respuestas de API:** mezcla estado remoto y local, y crea una segunda fuente de verdad.
- **Caché exclusiva de Next.js:** no cubre por sí sola toda la interacción cliente y mutaciones del panel operativo.

## Consecuencias

- Las capacidades deberán definir claves y alcance de invalidación de forma consistente.
- Los estados de carga, vacío, error y reintento serán explícitos.
- Los valores del backend no se copiarán a stores locales sin una necesidad de edición controlada.
- El refresco del dashboard podrá usar consultas periódicas una vez definido su intervalo.

## Dependencias no resueltas

- contratos HTTP y semántica de errores;
- política de reintentos para operaciones idempotentes;
- OD-012 para el intervalo y la fecha operativa del dashboard.

## Referencias

- [ADR-003: backend como fuente de verdad](ADR-003-backend-fuente-de-verdad.md)
- [ADR-FE-010](ADR-FE-010-dashboard-refresh.md)
