# Definir decisiones operativas del MVP

## Why

El relevamiento y el prototipo dejaban abiertas reglas que impiden diseñar un contrato único y pruebas reproducibles: prioridad, elección de ventana, cardinalidad camión–transportista, configuración de capacidad, datos de solicitud, permisos internos y cola visual. Las alternativas confirmadas al planificar el backlog del backend ([B00](https://github.com/agustinvallante/AgroFlow/issues/7)) se registraron en [ADR-004](../../../docs/architecture/decisions/ADR-004-politicas-operativas-del-mvp.md).

## What Changes

- Ordenar las solicitudes por mayor tiempo desde el corte, después flota propia y finalmente fecha/hora de solicitud más antigua registrada de forma estable por el backend.
- Asignar siempre la próxima ventana compatible sin preferencia de horario; configurar duración y cupo por ingenio al preparar el ambiente, sin edición desde la pantalla de Configuración del MVP.
- Permitir como máximo un transportista activo por camión, mediante asociación explícita e historial conservado.
- Exigir transportista, camión, finca, momento de corte y carga estimada para solicitar un turno.
- Permitir a operador y supervisor gestionar los tres catálogos y crear/cancelar turnos internos del propio ingenio.
- Exponer en el dashboard franjas vacías y cupos configurados, ocupados y restantes desde el backend.
- Preparar el ingenio de demostración con ventanas de 30 minutos y dos camiones por franja, sin convertir esos valores en constantes del producto.

## Capabilities

Se modifican `turnos-solicitud-y-asignacion`, `datos-maestros`, `acceso-y-autorizacion`, `turnos-consulta`, `turnos-ciclo-de-vida`, `monitoreo-operativo`, `integracion-whatsapp-n8n` y `aceptacion-del-mvp`.

## Impact

- **Backend y persistencia:** datos de corte/carga, asociación activa única, política de asignación, configuración por ingenio, permisos y proyección de cupos.
- **Frontend:** solicitud interna sin selector de franja, catálogos completos y cola que incluye ventanas vacías.
- **n8n:** recolección de corte/carga sin preguntar por franja preferida; la API sigue siendo la autoridad.
- **Contrato y pruebas:** los campos, errores y operaciones concretos deben definirse en OpenAPI y verificarse con pruebas unitarias, de integración y de aceptación antes de cerrar este cambio.

## No alcance

Este cambio no decide la representación exacta del calendario y zona horaria, unidades y formatos de carga, mecanismo de idempotencia/concurrencia, permisos de otros roles, fórmulas de indicadores ni protocolos de n8n. Tampoco implementa una pantalla para editar la capacidad durante la operación.

## Trazabilidad

CU-001, CU-002, CU-003, CU-004, CU-006, CU-008, CU-009, CU-010, CU-011 y CU-012; RN-005, RN-009 a RN-018, RN-038 a RN-054A; OD-001 a OD-005, OD-007, OD-012 y OD-013.
