# Tareas del corte de lectura interna

## Contrato

- [x] Fijar el alcance de OD-004 y OD-007 para las dos consultas.
- [x] Definir el contrato HTTP del MVP y mantener las formas de respuesta LocalDemo.
- [ ] Completar la revisión del contrato con el consumidor F06 (#72); Lucas ya revisó el backend en la PR #106.
- [x] Declarar el login como andamiaje fuera del contrato y registrar los pendientes de sesión y protección de intentos en el alcance de B03/B10.

## Base

- [x] B02: disponer de SQLite migrada con identidad, dos ingenios y ventanas con inicio UTC, preservando datos existentes.
- [x] B03: exponer a las lecturas un contexto de ingenio obtenido de la identidad autenticada y una política para operador y supervisor.
- [x] B04: integrar errores del contrato y preparar pruebas de integración HTTP con dos ingenios.

## Lecturas

- [x] B25: en la ruta existente, calcular los dos límites UTC a partir de las medianoches locales de fechas consecutivas. Filtrar por ingenio, rango, estado y patente en SQL. Contar el conjunto filtrado y paginar con el orden definido antes de materializar la proyección.
- [x] B26: en la ruta existente, restringir por ingenio e ID antes de proyectar el detalle. Aplicar el mismo error para ID inexistente y ajeno.
- [x] Probar ambos roles, token inválido o vencido, rol insuficiente, aislamiento del listado y su total, detalle ajeno, filtros combinados, límite de medianoche, desempate y página vacía.
- [x] Probar lectura tras reinicio y conservar las pruebas de las rutas de escritura de LocalDemo.
- [x] Probar que ventanas migradas con `datetime(StartAt)` aparecen por el filtro SQL de fecha, incluidos ambos límites del día local.
- [ ] Consolidar el delta luego de revisar el corte.

Las tareas abiertas conservan la trazabilidad en #29 y #30; la PR usa referencias
sin cierre automático. Consolidar el delta requiere la aceptación del corte,
no sólo una compilación o una revisión técnica favorable.
