# ADR-003: Backend como fuente operativa de verdad

- Estado: aceptada
- Fecha: 2026-09-17
- Alcance: reglas de negocio, estado e integraciones

## Contexto

Una misma operación puede iniciarse desde el frontend o desde WhatsApp mediante n8n. Si cada canal calcula capacidad, prioridad, estados o permisos por su cuenta, el comportamiento puede divergir y confirmar resultados incompatibles.

Además, el prototipo actual usa datos simulados y la automatización conversacional todavía no constituye un registro operativo confiable.

## Decisión

El backend de AgroFlow es la única fuente operativa de verdad para:

- identidad efectiva, permisos y pertenencia al ingenio;
- entidades y relaciones del dominio;
- disponibilidad, asignación, prioridad y capacidad;
- transiciones y estado vigente de un turno;
- interrupciones y sus efectos aprobados;
- persistencia y trazabilidad de operaciones críticas.

El frontend y n8n pueden validar formatos, reunir datos y presentar resultados, pero toda decisión de negocio debe ser solicitada y confirmada por la API. Una operación solo se comunica como exitosa después de que el backend la haya validado y persistido.

## Responsabilidad del frontend

- representar el estado recibido de la API;
- realizar validaciones de experiencia que no sustituyan las del backend;
- evitar asumir una operación exitosa cuando la API la rechazó o no la confirmó;
- actualizarse frente a conflictos o cambios de estado informados por el backend;
- no inferir autorización a partir de elementos visibles u ocultos en pantalla.

## Responsabilidad de n8n

- adaptar el protocolo del canal a los contratos de la API;
- mantener la correlación necesaria entre mensaje y solicitud;
- presentar errores y respuestas aprobadas;
- aplicar la política de reintentos y deduplicación que se defina en el contrato;
- no conservar una tabla paralela como estado autoritativo;
- no implementar reglas que deberían producir el mismo resultado desde la web.

## Responsabilidad del backend

- validar nuevamente toda entrada, aunque el consumidor ya la haya validado;
- decidir con el estado persistido vigente;
- ofrecer errores estables y accionables para los consumidores;
- impedir operaciones entre ingenios no autorizados;
- soportar concurrencia e idempotencia de acuerdo con las decisiones que se adopten;
- producir trazabilidad suficiente para explicar una decisión operativa.

## Consecuencias

### Beneficios

- comportamiento consistente entre web y mensajería;
- menor duplicación de reglas;
- una ubicación clara para pruebas de dominio y concurrencia;
- auditoría basada en un único estado confirmado.

### Costos

- la API pasa a ser una dependencia crítica de ambos canales;
- las caídas y latencias deben tratarse explícitamente;
- los consumidores necesitan manejar respuestas pendientes, rechazadas o reintentables;
- el backend requiere contratos estables y observabilidad suficiente.

## Aclaración

Esta decisión define dónde vive el estado operativo. No reemplaza la jerarquía documental: el comportamiento esperado se especifica en OpenSpec y el backend debe implementarlo. Tampoco resuelve por sí sola algoritmos o políticas todavía abiertos.
