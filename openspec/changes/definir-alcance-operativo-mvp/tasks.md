# Tareas del cambio

## 1. Especificaciones y documentación

- [x] Registrar las decisiones confirmadas mediante esta propuesta, diseño y deltas OpenSpec; sincronizar las specs vigentes.
- [x] Resolver y enlazar el estado correspondiente de OD-001, OD-002, OD-003 y las porciones resueltas de OD-004, OD-005, OD-007, OD-012 y OD-013; verificar que los temas restantes sigan explícitos como abiertos. Área: producto/arquitectura.
- [x] Sincronizar glosario, reglas, casos de uso y guía de aceptación con las specs; verificar que no permanezcan referencias a preferencia de franja ni cardinalidad pendiente como regla vigente. Área: documentación.

## 2. Contrato y modelo

- [ ] Especificar en `docs/contracts/openapi.yaml` los campos de solicitud, configuración consultable, catálogos/asociaciones, permisos y proyección de la cola; validar el contrato sin inventar valores aún pendientes. Área: backend y frontend.
- [ ] Implementar datos y migraciones que representen corte, carga, asociación activa única, configuración por ingenio y ventanas; probar conservación del historial. Área: backend.

## 3. Comportamiento y consumidores

- [ ] Implementar asignación/reprogramación con prioridad acordada y próxima ventana compatible; probar empates y capacidad con dos ingenios de configuración distinta. Área: backend.
- [ ] Implementar permisos de operador/supervisor, gestión de catálogos y alta/cancelación interna; probar rechazo entre ingenios. Área: backend.
- [ ] Integrar interfaz interna y cola con franjas vacías/cupos desde la API; probar que no se recalculan reglas en frontend. Área: frontend.
- [ ] Adaptar n8n para reunir corte/carga sin pedir franja; probar confirmación únicamente tras respuesta persistida de la API. Área: integración.

## 4. Aceptación y cierre

- [ ] Ejecutar pruebas de contrato, backend, frontend e integración y el guion MVP con seed de 30 minutos/dos camiones; guardar evidencia trazable. Área: equipo.
- [ ] Validar OpenSpec y archivar este cambio sólo cuando contrato, implementación, pruebas y documentación estén alineados. Área: equipo.
