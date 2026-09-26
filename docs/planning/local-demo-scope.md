# Alcance de la demo local de turnos

## Objetivo

Llegar al lunes con una demostración local integrada y pequeña de AgroFlow: crear un turno, consultarlo y recorrer su ciclo de vida con backend y persistencia reales. Un segundo turno demuestra cancelación y liberación de capacidad.

Este alcance implementa la issue #49. No reemplaza la [aceptación del MVP](../../openspec/specs/aceptacion-del-mvp/spec.md) ni reduce sus requisitos.

## Incluido

| Caso de uso | Evidencia mínima |
|---|---|
| CU-002 | Crear un turno con teléfono del transportista, patente y código de finca, sin elegir franja, y recibir una ventana persistida. |
| CU-003 y CU-004 | Listar los turnos del día y filtrar por fecha, estado, patente y teléfono del transportista. |
| CU-005 | Consultar el detalle vigente por identificador. |
| CU-006 | Avanzar en orden desde `ASIGNADO` hasta `FINALIZADO`. |
| CU-011, sólo cancelación | Cancelar otro turno desde un estado permitido, conservarlo y reutilizar el cupo. |

El contrato exacto es [OpenAPI](../contracts/openapi.yaml). Este documento no redefine rutas, DTOs, códigos ni reglas.

## No incluido

- login, registro, roles ni demostración de segregación;
- despliegue o infraestructura productiva;
- CRUD de transportistas, camiones, fincas o asociaciones;
- interrupciones y reprogramación;
- dashboard de indicadores/cupos, mapa o reportes;
- WhatsApp real;
- cierre de decisiones generales de idempotencia, concurrencia, auditoría u operación productiva.

Los datos maestros se preparan mediante seed. Las exclusiones son sólo de esta demo; permanecen vigentes en el MVP completo.

## Supuestos controlados

- Un único ingenio local, sin selector y sin autenticación.
- Zona horaria `America/Argentina/Tucuman`.
- Ventanas de 30 minutos, cupo 2 y calendario preparado para la fecha local de ejecución.
- Al menos dos transportistas con teléfonos ficticios estables, dos camiones con asociaciones activas y patentes estables, y dos fincas con códigos estables.
- n8n usa el teléfono del mensaje entrante y los identificadores naturales del seed; no consulta datos maestros ni contiene UUIDs hardcodeados.
- Backend en `http://localhost:5000`, frontend en `http://localhost:5173` y n8n en `http://localhost:5678`.
- Salud técnica en `http://localhost:5000/health`, fuera de `/api/v1`.

## Propiedad y división entre seis personas

| Persona | Propiedad | No debe decidir de forma aislada |
|---|---|---|
| 1. Contrato/coordinación | OpenSpec, OpenAPI y compatibilidad | Reglas nuevas para destrabar implementación. |
| 2. Datos/seed | Persistencia, inicialización y fixtures | Formas HTTP o reglas en consumidores. |
| 3. API | Operaciones, validaciones y pruebas backend | Cambios contractuales sin actualizar OpenAPI. |
| 4. Frontend | Formularios, vistas y estados de interacción | Ventanas, capacidad o transiciones válidas. |
| 5. n8n/chatbot | Conversación y adaptación HTTP | Estado paralelo, asignación o confirmación anticipada. |
| 6. QA/integración | Smoke tests, recorrido y evidencia | Relajar criterios para obtener una demo verde. |

Una persona puede ayudar a otra, pero la propiedad del contrato permanece centralizada y las reglas siguen en el backend.

## Secuencia de integración

1. Revisar y congelar OpenSpec/OpenAPI y los identificadores del seed.
2. Preparar persistencia/seed mientras frontend y n8n construyen contra el contrato.
3. Entregar alta, listado y detalle backend; conectar consumidores.
4. Entregar transiciones y cancelación; conectar acciones.
5. Ejecutar smoke tests por operación.
6. Ejecutar dos veces el recorrido completo documentado.

## Criterios de aceptación

- [ ] `/health` responde correctamente y las cuatro operaciones canónicas coinciden con OpenAPI.
- [ ] El seed se prepara sin datos personales y permite repetir los dos recorridos.
- [ ] Un turno válido se crea `ASIGNADO` con `carrierPhone`, `truckPlate` y `farmCode`, sin enviar UUIDs ni una ventana preferida.
- [ ] Listado, filtros —incluido `phone` para CU-003— y detalle muestran estado persistido y no mutan datos.
- [ ] El recorrido normal alcanza `FINALIZADO` sólo mediante transiciones consecutivas.
- [ ] Saltos, retrocesos y transiciones desde estados terminales responden conflicto.
- [ ] Otro turno puede cancelarse desde `ASIGNADO`, `EN_CAMINO` o `EN_ESPERA`, queda consultable y libera capacidad.
- [ ] Un camión no obtiene dos turnos activos y una solicitud sin cupo no crea un turno incompleto.
- [ ] Frontend y n8n representan respuestas de la API sin duplicar reglas; n8n opera desde el teléfono entrante sin consultas de datos maestros ni UUIDs hardcodeados.
- [ ] n8n usa una URL base configurable y funciona desde Docker con la salvedad documentada.
- [ ] La demo y su evidencia nombran las exclusiones y no afirman aceptación del MVP completo.
