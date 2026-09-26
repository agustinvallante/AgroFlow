# Tareas del demo local de turnos básicos

La asignación propone seis responsables, uno por frente. Cada casilla requiere la verificación indicada y respeta OpenAPI como contrato único.

## 1. Congelar alcance y contrato — Persona 1, contrato y coordinación

- [x] Registrar propuesta, diseño y deltas OpenSpec de la issue #49. Verificación: `openspec validate demo-local-turnos-basicos --strict --no-interactive`.
- [x] Publicar en OpenAPI las cuatro operaciones canónicas, `/health`, el alta con `carrierPhone`/`truckPlate`/`farmCode`, el filtro opcional `phone`, los demás DTOs, filtros, estados y errores. Verificación: validación OpenAPI disponible en el repositorio o entorno, sin instalar dependencias.
- [x] Documentar que la demo no modifica el MVP completo y que chatbot/dashboard referencian OpenAPI. Verificación: revisión cruzada de `proposal.md`, `docs/contracts/README.md` y `docs/planning/local-demo-scope.md`.

## 2. Preparar modelo, persistencia y seed — Persona 2

- [ ] Implementar el modelo mínimo de turno, ventana y referencias a datos maestros preexistentes, sin endpoints CRUD. Verificación: migración o inicialización desde una base local limpia.
- [ ] Crear seed ficticio estable para un ingenio, dos transportistas con teléfonos naturales estables, dos camiones asociados con patentes, dos fincas con códigos y ventanas de la fecha local. Verificación: ejecutar el seed dos veces sin duplicar datos y crear dos turnos usando `carrierPhone`, `truckPlate` y `farmCode`.
- [ ] Configurar zona `America/Argentina/Tucuman`, duración de 30 minutos y cupo 2 como datos del seed. Verificación: prueba que lea la configuración persistida y no constantes de dominio.

## 3. Implementar API de turnos — Persona 3

- [ ] Implementar alta por `carrierPhone`, `truckPlate` y `farmCode`, listado con filtros —incluido `phone`— y detalle conforme a OpenAPI, resolviendo las referencias en backend. Verificación: pruebas de integración para `201`, `200`, `400`, `404` y conflictos de capacidad/camión activo.
- [ ] Implementar secuencia de estados y cancelación en la operación de transiciones. Verificación: pruebas de secuencia completa, salto rechazado, terminalidad y liberación de cupo.
- [ ] Mantener errores compatibles con el esquema canónico. Verificación: pruebas de contrato sobre `Content-Type`, `status`, `code`, `traceId` y errores de campos.

## 4. Integrar frontend — Persona 4

- [ ] Implementar alta, listado, filtros y detalle usando los DTOs de OpenAPI. Verificación: pruebas de componente o integración con respuestas contractuales.
- [ ] Implementar acciones del siguiente estado y cancelación sin calcular reglas localmente. Verificación: ante `409`, la vista conserva y vuelve a consultar el estado informado por la API.
- [ ] Mostrar explícitamente “Demo local” y no incluir login, CRUD, interrupciones, mapa ni reportes. Verificación: recorrido visual contra los criterios de alcance.

## 5. Integrar n8n/chatbot — Persona 5

- [ ] Implementar solicitud desde el teléfono entrante, consulta por `phone` y aviso `EN_CAMINO` contra las mismas operaciones. Verificación: ejecución reproducible del workflow con API real, sin consultas de datos maestros, UUIDs hardcodeados ni nodos de asignación o transición local.
- [ ] Parametrizar la URL base y documentar `host.docker.internal`. Verificación: ejecutar n8n en Docker contra la API del host; en Linux, comprobar el mapeo `host-gateway` si aplica.
- [ ] Comunicar errores de API sin convertirlos en éxito. Verificación: escenarios de `carrierPhone`, `truckPlate` o `farmCode` inexistente y transición inválida.

## 6. Integrar y verificar — Persona 6, QA

- [ ] Automatizar o ejecutar smoke tests de salud, alta con identificadores naturales, filtros —incluido `phone`—, detalle, secuencia y cancelación. Verificación: evidencia de cada criterio en `docs/development/local-demo-runbook.md`.
- [ ] Ejecutar el recorrido principal hasta `FINALIZADO` y otro recorrido hasta `CANCELADO` dos veces desde datos controlados. Verificación: estados persistidos y cupo reutilizable tras cancelar.
- [ ] Confirmar las exclusiones y registrar como defectos cualquier regla duplicada en frontend o n8n. Verificación: revisión del diff y checklist de alcance.

## Secuencia y puertas

1. La sección 1 bloquea cambios de comportamiento en las secciones 2 a 5.
2. Persona 2 habilita las pruebas de integración de Persona 3.
3. Alta y consultas de Persona 3 habilitan integración parcial de Personas 4 y 5.
4. Transiciones y cancelación de Persona 3 habilitan el cierre de ambos consumidores.
5. Persona 6 comienza smoke tests por operación y ejecuta el recorrido completo sólo cuando 2 a 5 están verdes.
6. La demo no se declara lista con validaciones OpenSpec/OpenAPI fallidas ni con divergencia entre contrato y consumidores.
