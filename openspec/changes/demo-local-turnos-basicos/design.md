# Diseño del demo local de turnos básicos

## Contexto

La demo debe unir frontend, backend y n8n en una máquina de desarrollo con tiempo limitado. El alcance completo sigue definido en `openspec/specs/`; este cambio introduce un perfil local deliberadamente menor y no una nueva definición del MVP.

OpenAPI es el único contrato HTTP canónico. Las guías del dashboard y del chatbot deben enlazarlo en lugar de copiar DTOs, códigos o reglas. El backend conserva la autoridad sobre asignación, estado y capacidad.

## Decisiones

### Perfil local sin identidad

La demo utiliza un único ingenio proveniente del seed y no ofrece login ni registro. Las operaciones no reciben credenciales ni permiten seleccionar un ingenio. Esta simplificación sólo es válida para ejecución local: no demuestra autenticación, autorización, segregación ni seguridad entre servicios.

### Contrato mínimo

Se publican cuatro operaciones versionadas:

1. `POST /api/v1/appointments` para CU-002.
2. `GET /api/v1/appointments` para CU-003 y CU-004, con filtros opcionales `date`, `status`, `truckPlate` y `phone`.
3. `GET /api/v1/appointments/{id}` para CU-005.
4. `POST /api/v1/appointments/{id}/transitions` para CU-006 y la cancelación de CU-011.

`GET /health` permanece fuera de `/api/v1` porque expresa salud técnica y no una capacidad de negocio. Los nombres, formatos y respuestas exactos viven únicamente en `docs/contracts/openapi.yaml`.

### Modelo observable

El alta identifica al transportista por `carrierPhone`, al camión por `truckPlate` y a la finca por `farmCode`, todos valores naturales y estables publicados por el seed, e informa corte y carga estimada en toneladas. No acepta una ventana preferida. El backend resuelve y valida las entidades y su asociación; n8n no consulta endpoints de datos maestros ni fija UUIDs en el workflow. La respuesta incluye referencias legibles, que pueden conservar UUIDs internos, ventana asignada y estado vigente.

El enum visible es `ASIGNADO`, `EN_CAMINO`, `EN_ESPERA`, `INGRESADO`, `EN_DESCARGA`, `FINALIZADO` y `CANCELADO`. La transición recibe sólo el estado destino; el backend valida la secuencia y persiste antes de responder.

Los errores siguen una forma compatible con Problem Details y agregan `code`, `errors` y `traceId`. El contrato diferencia entrada inválida (`400`), recurso inexistente (`404`), conflicto de negocio (`409`) y error inesperado (`500`).

### Seed y asignación acotados

El seed crea datos ficticios y estables para un único ingenio local:

- zona horaria `America/Argentina/Tucuman`;
- ventanas de 30 minutos y cupo de dos camiones;
- calendario suficiente para la fecha local de la demo;
- al menos dos transportistas activos, dos camiones activos con asociación válida y dos fincas activas;
- teléfonos ficticios, patentes y códigos de finca documentados por el proceso de seed, sin datos personales reales.

La demo asigna la primera ventana futura con cupo y evita dos turnos activos para un mismo camión. Estos supuestos no cierran las decisiones generales pendientes sobre calendario, horizonte, concurrencia o idempotencia del MVP.

### Responsabilidades

- **OpenAPI/OpenSpec:** define comportamiento y forma compartida; no implementa reglas.
- **Backend:** valida referencias del seed, asigna, persiste, filtra, aplica transiciones y libera capacidad al cancelar.
- **Frontend:** reúne entradas y representa respuestas; no calcula ventanas, capacidad ni transiciones válidas.
- **n8n/chatbot:** toma `carrierPhone` del mensaje entrante, reúne `truckPlate` y `farmCode`, y adapta la conversación a OpenAPI sin consultas de datos maestros ni UUIDs hardcodeados; no guarda un estado autoritativo ni confirma antes de la API.
- **Persistencia/seed:** provee datos ficticios reproducibles; no requiere CRUD para prepararlos.
- **QA/integración:** verifica el recorrido contra el contrato y registra defectos sin cambiar reglas en los consumidores.

### Topología local

El navegador usa `http://localhost:5173` para el frontend y `http://localhost:5000` para la API. n8n usa `http://localhost:5678`. Si n8n corre en Docker y la API corre en el host, el workflow debe llamar `http://host.docker.internal:5000`; `localhost` dentro del contenedor apunta al propio contenedor. En Linux puede requerirse el mapeo `host-gateway` documentado en el runbook. Esta dirección es configuración local y no debe fijarse como URL productiva.

## División entre seis personas

1. **Contrato y coordinación:** OpenSpec/OpenAPI, compatibilidad entre áreas y control de cambios.
2. **Modelo, persistencia y seed:** entidades mínimas, configuración del ingenio y datos reproducibles.
3. **API de turnos:** cuatro operaciones, validaciones, errores y pruebas de integración.
4. **Frontend:** alta, listado, filtros, detalle y acciones de transición usando OpenAPI.
5. **n8n/chatbot:** solicitud, consulta y aviso `EN_CAMINO` contra la misma API.
6. **QA e integración local:** smoke tests, recorrido completo, evidencias y runbook.

Cada persona es propietaria de su área de implementación, no de redefinir el contrato. Todo cambio observable vuelve primero a la persona 1 y se refleja en OpenAPI antes de adaptar consumidores.

## Secuencia

1. Congelar este cambio y OpenAPI; acordar fixtures del seed.
2. En paralelo, preparar persistencia/seed y esqueletos de consumidores generados o tipados desde el contrato.
3. Implementar y probar alta, listado y detalle en backend.
4. Conectar frontend y n8n a esas lecturas y al alta.
5. Implementar transiciones y cancelación; luego habilitar sus acciones en ambos consumidores.
6. Ejecutar el recorrido completo dos veces desde un seed limpio y corregir sólo dentro del alcance.

## Alternativas descartadas

- Duplicar DTOs o reglas en documentación del chatbot/dashboard.
- Incorporar login “mínimo” o un selector de ingenio sin poder demostrar su seguridad.
- Exponer endpoints CRUD sólo para preparar la demo.
- Separar cancelación en otra ruta cuando es una transición terminal del mismo agregado.
- Agregar interrupciones, mapa, reportes o despliegue para completar pantallas del prototipo.

## Riesgos

- El perfil sin identidad podría confundirse con una decisión de seguridad del producto; las guías deben remarcar que es local y no productivo.
- `host.docker.internal` no se resuelve igual en todos los hosts Docker; el runbook incluye la salvedad de Linux.
- Las decisiones generales de concurrencia e idempotencia siguen abiertas; la demo no debe presentarse como evidencia de esos requisitos.
- Una fecha sin ventanas sembradas impide crear turnos; el procedimiento de seed debe preparar la fecha efectiva de ejecución.

## Estrategia de verificación

- Validación estricta del cambio OpenSpec.
- Validación sintáctica y semántica de OpenAPI.
- Pruebas backend para alta, filtros, detalle, secuencia, terminalidad, cancelación y conflictos.
- Pruebas de consumidores que demuestren que representan respuestas y errores sin recalcular reglas.
- Smoke test local del recorrido principal y otro turno cancelado, con salud disponible en `/health`.
