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
- [ ] Consolidar la capacidad después de aceptar la revisión.

La comprobación del navegador con API desconectada y las regresiones de las vistas HTTP se registran junto con las pruebas de esta revisión. La presentación completa conserva su seguimiento en #111.
