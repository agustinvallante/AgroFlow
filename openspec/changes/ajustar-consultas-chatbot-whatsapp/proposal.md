# Ajustar las consultas del chatbot de WhatsApp

> **Estado: implementado en `feat/whatsapp-persistence` (PR #97), pendiente de revisión del equipo.** Corrige dos hallazgos de la prueba real WhatsApp → Evolution API → n8n → backend → SQLite. No resuelve decisiones abiertas del [registro](../../../docs/planning/open-decisions.md).

## Objetivo

1. Que el transportista encuentre por WhatsApp sus turnos activos aunque estén asignados para mañana o un día posterior.
2. Que el transportista pueda nombrar la finca como lo hace habitualmente ("Finca Norte", "norte", "finca sur") y que AgroFlow la resuelva a su código, sin una tabla de códigos en n8n.

## Motivación

- Cuando las ventanas del día ya pasaron, el backend asigna la próxima ventana disponible, que puede ser de mañana (`OD-003`). La herramienta de consulta del agente lista por `phone` sin `date` y el listado usa la fecha local actual por defecto: el turno existe en SQLite, pero el bot responde que no hay ninguno.
- El alta exige `farmCode` y el workflow sólo conocía `FINCA-NORTE` y `FINCA-SUR` como ejemplos fijos. Expresiones naturales no se resolvían y una finca nueva del catálogo nunca llegaría al bot.

## Alcance

- Filtro opcional `active=true` en `GET /api/v1/appointments`: sólo turnos no terminales y, sin `date`, de cualquier día. El valor por defecto del listado para operadores no cambia.
- Publicar en LocalDemo `GET /api/v1/farms` de sólo lectura sobre `MasterDataService`, con búsqueda `search` resuelta en el backend y las claves existentes (`ServiceKey` o `DashboardKey`).
- Herramienta `Lookup Farms` del agente, consulta de turnos con `active=true` e instrucciones para pedir aclaración ante nombres ambiguos.
- Política del validador de CI para admitir la clave del canal únicamente en esa lectura exacta.

## Fuera de alcance

- Alta, edición e inhabilitación de fincas por la API (campos editables pendientes en `OD-013`).
- Camiones y asociaciones (`OD-002`, `OD-013`).
- Búsqueda aproximada o por similitud de nombres, sinónimos o alias de fincas.
- La política definitiva de autenticación entre servicios (`OD-008`): se reutiliza la clave estática del perfil LocalDemo.

## Capacidades afectadas

| Capacidad | Tipo | Delta |
|---|---|---|
| `turnos-consulta` | Existente | [specs/turnos-consulta/spec.md](specs/turnos-consulta/spec.md) |
| `datos-maestros` | Existente | [specs/datos-maestros/spec.md](specs/datos-maestros/spec.md) |
| `integracion-whatsapp-n8n` | Existente | [specs/integracion-whatsapp-n8n/spec.md](specs/integracion-whatsapp-n8n/spec.md) |

Este cambio acota dos reglas del cambio [`demo-local-turnos-basicos`](../demo-local-turnos-basicos/proposal.md): la fecha por defecto del listado no aplica con `active=true`, y n8n MAY consultar el catálogo de fincas en sólo lectura. Ambos deltas de ese cambio remiten aquí.

## Casos de uso relacionados

- **CU-002** Solicitar turno por el canal: resolución de la finca.
- **CU-003** Consultar turno por el canal: turnos activos de cualquier fecha.
- **CU-012** Gestionar fincas: sólo la consulta.

## Impacto

| Área | Impacto |
|---|---|
| Backend | Filtro `ActiveOnly` en la consulta de turnos; `FarmsController` y `FarmSearch` sobre `MasterDataService`; política `FarmCatalogRead`. Sin migraciones. |
| Contrato API | OpenAPI 0.5.0: parámetro `active` y ruta `/api/v1/farms` con el esquema `Farm`. |
| n8n | Herramienta `Lookup Farms`, `active=true` en la consulta de turnos, sin códigos de finca en el workflow. Reasignar la credencial de servicio al nodo nuevo al importar. |
| CI | `validate-integrations.mjs` admite la clave del canal sólo en `GET` exacto a `/api/v1/farms`. |
| Frontend | Sin cambios. |
| Pruebas | Persistencia SQLite, HTTP, búsqueda, simulación del workflow con llamadas a herramientas y aceptación local contra la API real. |
