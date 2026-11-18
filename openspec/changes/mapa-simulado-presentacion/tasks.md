# Verificación del mapa simulado

## Alcance y documentación

- [x] Registrar el acuerdo de presentación y la capacidad mapa-simulado.
- [x] Documentar carga manual, estados visuales, reloj, persistencia y aislamiento de la API.
- [x] Vincular el guion con datos ficticios y la cobertura existente.
- [x] Precisar el tramo finca → ingenio y el acuerdo pendiente para relacionarlo con EN_CAMINO.

## Comportamiento y cobertura existentes en #105

- [x] Mapa vacío y validación de lugares y viajes en MapaView.test.tsx.
- [x] Recorrido único, reloj real, avance x60, retraso y regreso a la pestaña.
- [x] Persistencia local y eliminación de viajes junto con la finca.
- [x] Cálculo de distancia, duración, posición y estados en simulacionViajes.test.ts.
- [x] Verificar la rama sincronizada con suites backend/frontend, OpenSpec, contrato, enlaces y aceptación local del canal.
- [x] Ejecutar el guion de navegador sin API y comprobar viaje x60 y persistencia tras recarga.
- [ ] Obtener la revisión de Facundo y Lucas.

## Correcciones de la revisión de Lucas

- [x] Limitar velocidad (1 a 120 km/h), salida y retraso (0 a 1440 min) a valores finitos; la hora no representable se muestra como guion.
- [x] Descartar al leer los lugares, viajes y adelanto guardados que no sean válidos.
- [x] Guardar el adelanto como mucho cada 5 s, al cambiar la velocidad, al salir de la pestaña y al cerrar la página.
- [x] Probar adelanto tras recarga, almacenamiento bloqueado, coordenadas fuera de rango, límites del viaje, Programado y datos guardados inválidos.
- [ ] Enlazar la minuta o el comentario del acuerdo del 2026-11-14.

## Fincas de la API, red vial y detalle del camión

- [x] Leer el catálogo de fincas de la API y ubicarlas de forma determinista alrededor de un ingenio de referencia, como medida temporal.
- [x] Reemplazar el relieve por un fondo de rutas, accesos y ríos simulados, y hacer que cada recorrido siga esos caminos.
- [x] Dibujar los camiones como cuadrados seleccionables y mostrar su detalle en un recuadro.
- [x] Adaptar las pruebas de la vista y de la red, y agregar las del detalle y la falla de fincas.

## Cierre

- [ ] Consolidar la capacidad después de aceptar la revisión.

La comprobación del navegador con API desconectada y las regresiones de las vistas HTTP se registran junto con las pruebas de esta revisión. La presentación completa conserva su seguimiento en #111.
