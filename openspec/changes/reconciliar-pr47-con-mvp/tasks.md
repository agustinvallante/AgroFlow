# Tareas documentales de reconciliación PR #47

## Alineación

- [x] Registrar fundamento compatible pero todavía no habilitado; conservar las dependencias B00/B02/B03/B05.
- [x] Mantener rutas y DTO de maestros fuera del contrato OpenAPI activo.
- [x] Distinguir metadatos CLR no migrados de datos persistentes y aclarar que no prueban auditoría durable.
- [x] Conservar DNI de solo dígitos y SQLite OD-014 como acuerdos de fuentes canónicas.
- [x] Marcar normalización de WhatsApp como propuesta pendiente, no como decisión aprobada.
- [x] Alinear React/Vite y separar el flujo HTTP de turnos de demo de las vistas mock.

## Decisiones y validación pendientes

- [ ] Aprobar o rechazar la propuesta de normalización WhatsApp en OD-013 antes de consolidarla en la spec canónica.
- [ ] Resolver dependencias B00/B02/B03/B05 antes de considerar habilitada cualquier API maestra.
- [ ] Completar la suite backend y comprobar específicamente las fallas reportadas por el writer backend; este cambio no las declara resueltas.
- [ ] Validar todos los cambios OpenSpec y OpenAPI con las herramientas disponibles y registrar resultados observados.
- [ ] Reconciliar artefactos OpenSpec y estado Git desde la unidad de integración; no cerrar el merge por esta lista.
