# Tareas: listar ventanas y cupos en LocalDemo

## 1. Backend

- [x] 1.1 `IWindowReader` y su implementación en `EfAppointmentStore.ListWindowsAsync`: una consulta de lectura por ingenio y día local en SQL. Verificación: `WindowReaderStoreTests`.
- [x] 1.2 `WindowsController` (`GET /api/v1/windows`, `DashboardAccess`) con el parámetro `date` y el rechazo de parámetros no documentados, repetidos o vacíos, y registro en `LocalDemoAppointmentsConvention` como exclusivo de LocalDemo. Verificación: `WindowsHttpTests`.
- [x] 1.3 Probar sobre SQLite real: franjas vacías y ocupadas, cupos restantes, liberación por cancelación, fecha sin ventanas, validación, `401` con clave ausente, incorrecta o del canal, ruta ausente fuera de LocalDemo, sin mutaciones publicadas, lectura sin efectos y día local del ingenio. Verificación: `WindowsHttpTests`.
- [x] 1.4 Probar el aislamiento entre ingenios, el desplazamiento horario y la fecha por defecto del ingenio. Verificación: `WindowReaderStoreTests`.

## 2. Contrato

- [x] 2.1 Documentar la operación `listarVentanas` y el esquema `Window` en `docs/contracts/openapi.yaml` `0.7.0`, con la limitación de interrupciones. Verificación: Redocly válido.

## 3. Pendiente de B39 (#44)

- [ ] 3.1 Versión del MVP con JWT y roles por ingenio, y prueba HTTP con dos ingenios autenticados.
- [ ] 3.2 Calendario configurable y horizonte de búsqueda (OD-004, OD-012).
- [ ] 3.3 Indisponibilidad por interrupciones, cuando exista B33 (#37).
- [ ] 3.4 Integrar la grilla y el proxy del dashboard con F07 #73.
- [ ] 3.5 Revisar la PR y consolidar el delta en `openspec/specs/`.
