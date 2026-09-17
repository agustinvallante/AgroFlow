# Hoja de ruta de implementación

La secuencia prioriza dependencias y demostraciones verticales. Las fechas y responsables se asignarán en GitHub Projects; este documento define el orden lógico, no un calendario cerrado.

## Etapa 0 — Alineación

- aprobar OD-001 a OD-008 o limitar explícitamente el alcance de la primera demo;
- acordar matriz de roles, vocabulario y definición de turno activo;
- retirar del backend heredado el dominio de e-commerce y renombrar la solución;
- importar el frontend prototipo conservando su diseño, sin datos simulados como fuente de verdad;
- aprobar el primer contrato OpenAPI y la estrategia de ambientes.

**Salida verificable:** proyecto compila, las specs validan y frontend y backend comparten un contrato base.

## Etapa 1 — Plataforma y acceso

- autenticación y autorización;
- aislamiento por ingenio;
- manejo uniforme de errores;
- persistencia, migraciones, configuración segura y pruebas base;
- trazabilidad mínima de mutaciones.

**Demo:** usuario interno inicia sesión y solo puede consultar el contexto de su ingenio.

## Etapa 2 — Datos maestros

- transportistas y números de WhatsApp;
- camiones, patentes y tipos de flota;
- fincas;
- asociación autorizada transportista-camión;
- altas, modificaciones e inhabilitación con historial.

**Demo:** un usuario autorizado prepara los datos válidos para solicitar un turno.

## Etapa 3 — Núcleo de turnos

- ventanas, capacidad y prioridad aprobadas;
- solicitud y asignación transaccional;
- exclusión de turnos activos por camión;
- consulta, detalle y filtros;
- cancelación y liberación de capacidad.

**Demo:** desde el frontend se solicita, confirma, consulta y cancela un turno con persistencia real.

## Etapa 4 — Operación y dashboard

- máquina de estados y permisos por transición;
- listados operativos;
- indicadores diarios y señalización de interrupciones;
- actualización periódica de la interfaz;
- pruebas de concurrencia de los flujos críticos.

**Demo:** un operador gestiona un turno desde `ASIGNADO` hasta `FINALIZADO` y observa el impacto en el dashboard.

## Etapa 5 — Interrupciones e integración

- registro y actualización de interrupciones;
- reprogramación y selección de destinatarios;
- contratos seguros e idempotentes para n8n;
- solicitudes, consultas y aviso `EN_CAMINO` por WhatsApp;
- notificaciones y manejo de reintentos.

**Demo:** un transportista opera por WhatsApp y una interrupción produce las reprogramaciones y avisos especificados.

## Etapa 6 — Preparación de entrega

- pruebas de extremo a extremo y accesibilidad;
- observabilidad, copias de seguridad y recuperación;
- revisión de seguridad y datos personales;
- datos de demostración reproducibles;
- manual de operación y guion de presentación.

**Salida verificable:** una instalación limpia puede desplegarse, demostrarse y recuperarse siguiendo la documentación.

## Regla para cada etapa

Cada funcionalidad se implementa como una porción vertical: cambio OpenSpec, contrato, backend, frontend o integración necesaria, pruebas y documentación. No se considera avance funcional una pantalla aislada o un endpoint que aún no satisfaga un escenario aprobado.
