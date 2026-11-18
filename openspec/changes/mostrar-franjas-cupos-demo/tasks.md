# Tareas — franjas del panel LocalDemo

- [x] 1. Agregar lectura tipada, puerto, caso de uso, adaptador HTTP y composición sin reutilizar métricas no implementadas.
- [x] 2. Habilitar GET exacto de ventanas en el proxy del dashboard sin exponer claves al navegador.
- [x] 3. Mostrar todas las ventanas y sus tres cupos, con fecha opcional y horarios del ingenio.
- [x] 4. Implementar polling, actualización manual, relectura tras transición, error/reintento y estado desactualizado.
- [x] 5. Probar vacío/ocupado/completo, cambio de fecha, respuestas tardías, recuperación, cancelación y aislamiento del polling de camiones activos.
- [x] 6. Probar el contrato HTTP y la protección del proxy (clave servidor, origen, método y rutas).
- [x] 7. Actualizar alcance, README y runbook; conservar las limitaciones del MVP.
- [x] 8. Verificar suites, build, OpenSpec y el recorrido navegador → proxy → API → SQLite con datos ficticios; evidencia en `docs/development/dashboard-windows-demo.md`.
- [ ] 9. Revisar con Lucas y, tras la integración, consolidar el delta. Mantener #73 abierta por el alcance MVP restante.
