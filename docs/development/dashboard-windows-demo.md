# Franjas y cupos del panel — LocalDemo (#73 parcial)

## Qué incorpora

El **Panel general → Cola del día** consume la lectura integrada por la #119:
`GET /api/v1/windows`, documentada en
[OpenAPI 0.7.0](../contracts/openapi.yaml). Muestra todas las ventanas,
vacías/parciales/completas, con inicio, fin, capacidad, ocupados y disponibles.
Esos números vienen de la API; el dashboard no resta capacidad ni cuenta turnos
para decidir cupos. No presupone intervalos de treinta minutos ni capacidad dos.

La fecha opcional usa el día calendario del ingenio. Vacía significa **hoy
según la API**, no hoy según el navegador. **Hoy del ingenio** limpia el filtro.
Cada franja conserva las fechas y el desplazamiento horario de la respuesta,
incluidas las que cruzan medianoche. Una fecha sin ventanas muestra un mensaje
explícito, no una grilla inventada.

La vista relee cada cuatro segundos, al entrar, con **Actualizar franjas** y
después de las transiciones realizadas en el panel. Altas/cancelaciones desde
Cola de turnos o WhatsApp se ven al volver al panel o en el siguiente polling.
**Camiones en curso** sigue leyendo activos de cualquier fecha: el filtro de
franjas no oculta un camión activo de ayer o mañana.

## Arranque y clave

Seguir el [runbook local](local-demo-runbook.md). La API debe usar LocalDemo,
tener el seed de ventanas y `Dashboard__ApiKey`. Vite necesita
`AGROFLOW_DASHBOARD_KEY` con el mismo valor, sin prefijo `VITE_`, y
`VITE_API_URL` apuntando a esa API.

El navegador consulta su mismo origen; el proxy agrega la clave sólo en Node.
Para ventanas se admite únicamente GET exacto, con query opcional; se bloquean
mutaciones, subrutas, clientes remotos y orígenes ajenos. Servir sólo `dist/`
sin un proxy equivalente no habilita esta lectura. Fuera de LocalDemo el
endpoint responde 404; no es el contrato autenticado del MVP.

## Recorrido de presentación y QA

Usar una base de prueba y referencias ficticias del seed. No borrar una base
compartida para repetir el ensayo.

1. Abrir Panel general. Ver las franjas sembradas y su capacidad, incluso las
   vacías. Verificar una franja con `GET /api/v1/windows` usando la clave sólo
   en un cliente de QA del servidor.
2. Crear un turno válido desde Cola de turnos o el canal conectado a la API.
   Volver al panel: la ventana asignada muestra los nuevos ocupados/disponibles.
   Si el panel permanece abierto en otra pestaña, esperar el siguiente polling.
3. Cancelar el turno desde su detalle en Cola de turnos y comprobar el cupo
   liberado en el panel. Para cancelar desde el panel, el turno debe estar en
   curso (por ejemplo EN_CAMINO); se releen franjas al terminar la acción.
4. Elegir mañana y una fecha sin ventanas. Comprobar los valores del servidor
   y el mensaje vacío. **Hoy del ingenio** vuelve a omitir `date`.
5. Para comprobar una franja completa, consumir su capacidad con camiones
   ficticios distintos autorizados por el seed. No duplicar un turno activo
   del mismo camión. La franja sigue visible con cero disponibles.
6. Detener temporalmente la API después de una lectura: las franjas quedan
   visibles con **Franjas desactualizadas**, sin pasar a cero. Reiniciar la
   misma API/base y esperar recuperación o pulsar **Actualizar franjas**.
   Si falla la primera lectura, aparece error con **Reintentar franjas**.
7. Abrir desde un navegador en otra zona horaria: las fechas/horas de ventanas
   deben coincidir con los RFC3339 emitidos por el ingenio.

## Pruebas y límites

Pruebas automatizadas: `PanelGeneralView.test.tsx`,
`HttpVentanaRepository.test.ts` y `server/dashboardProxy.test.ts`.
Cubren capacidad arbitraria, cruce de medianoche, fecha vacía/elegida,
errores/recuperación, respuestas tardías, ausencia de polling superpuesto,
cancelación y controles del proxy.

### Evidencia local — 2026-11-16

- Frontend: **138 pruebas** aprobadas, typecheck y build correctos; lint sin
  errores y con las cinco advertencias preexistentes. `npm audit`: cero alertas.
- Backend: **447 pruebas** aprobadas. En la primera corrida,
  `TrucksHttpTests.Requires_the_dashboard_key` falló con un objeto SQLite
  descartado; pasó aislada y en la repetición de la suite completa sin cambiar
  código del backend. Se registra para seguimiento de la estabilidad del fixture.
- Integraciones/workflow: **106 pruebas** aprobadas. Aceptación existente contra
  API/SQLite/proxy reales aprobada, con Evolution, WhatsApp y agente simulados.
- Chrome en **Asia/Tokyo**, API local y SQLite efímera: veinte franjas vacías,
  dos altas desde otra pestaña hasta completar una franja, cancelación con cupo
  liberado, transición/cancelación desde el panel, fecha sin ventanas y
  recuperación ante 503 inicial/posterior. Sin errores de JavaScript.
- Para comprobar capacidad y duración variables se agregó **sólo en esa base
  efímera** una ventana de capacidad siete, 23:15–00:00 del día siguiente.
  El panel conservó ambas fechas y UTC-03:00. No se alteró el seed del proyecto.
- El navegador no envió claves ZafraFlow; el proxy real bloqueó POST, subrutas
  y origen ajeno con 404.

Entrega parcial: **#73 y #44 siguen abiertas** por el MVP pendiente. No se
agregan indicadores generales, interrupciones, reprogramación, edición del
calendario, JWT/roles ni selector de ingenio. Los indicadores siguen sin datos.
El endpoint y el contrato existentes no cambian. El ensayo de este corte no
acredita WhatsApp real, que conserva su prueba de cierre independiente.
