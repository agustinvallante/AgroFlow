# Diseño del corte LocalDemo

El flujo es `PanelGeneralView → useVentanas → ObtenerVentanas →
VentanaRepository → HttpVentanaRepository → proxy Vite → GET /api/v1/windows`.
La lectura `VentanaRecepcion` conserva los campos del esquema HTTP `Window`;
no necesita traducir ni derivar sus valores.

La fecha vacía omite `date` para que el servidor determine hoy en la zona del
ingenio. Una fecha explícita se envía como YYYY-MM-DD. Los rótulos usan el
RFC3339 completo, incluso al cruzar medianoche. No se usa el reloj ni la zona
del navegador para elegir o agrupar ventanas.

El hook conserva la última lectura válida sólo para la misma fecha. Al cambiar
de fecha se ocultan los datos anteriores y se invalidan sus respuestas tardías.
Una falla inicial no equivale a cero ventanas; una falla posterior conserva la
última lectura con aviso. No se superponen peticiones del mismo ciclo de polling
si la API tarda. El cleanup invalida respuestas y detiene el temporizador.

El panel lista las ventanas en el orden de la API, sin asumir media hora, cupo
dos, cantidad fija de franjas o composición de flota. La disponibilidad mostrada
es `available`, no una resta ni un conteo de turnos en el cliente. Las transiciones
del panel disparan una relectura; las altas/cancelaciones en otra pantalla/canal
se observan al volver al panel o en hasta un intervalo después de la respuesta
de la API. No se agrega un bus global de eventos ni WebSockets.

La clave sigue exclusivamente en el proceso de Vite. La nueva ruta protegida
acepta GET exacto (con query), sin subrutas ni mutaciones, únicamente desde el
mismo origen local. Las rutas del canal continúan bloqueadas.
