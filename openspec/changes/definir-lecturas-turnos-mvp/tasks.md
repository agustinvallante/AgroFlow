# Tareas del corte de lectura interna

## Contrato

- [x] Fijar el alcance de OD-004 y OD-007 para las dos consultas.
- [x] Definir el contrato HTTP del MVP y mantener las formas de respuesta LocalDemo.
- [ ] Revisar el contrato con backend y el consumidor F06.

## Base

- [x] B02: disponer de SQLite migrada con identidad, dos ingenios y ventanas con inicio UTC, preservando datos existentes.
- [x] B03: exponer a las lecturas un contexto de ingenio obtenido de la identidad autenticada y una política para operador y supervisor.
- [x] B04: integrar errores del contrato y preparar pruebas de integración HTTP con dos ingenios.

## Lecturas

- [x] B25: en la ruta existente, calcular los dos límites UTC a partir de las medianoches locales de fechas consecutivas. Filtrar por ingenio, rango, estado y patente en SQL. Contar el conjunto filtrado y paginar con el orden definido antes de materializar la proyección.
- [x] B26: en la ruta existente, restringir por ingenio e ID antes de proyectar el detalle. Aplicar el mismo error para ID inexistente y ajeno.
- [x] Probar ambos roles, token inválido o vencido, rol insuficiente, aislamiento del listado y su total, detalle ajeno, filtros combinados, límite de medianoche, desempate y página vacía.
- [x] Probar lectura tras reinicio y conservar las pruebas de las rutas de escritura de LocalDemo.
- [ ] Consolidar el delta luego de revisar el corte.
