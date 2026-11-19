# Probar el mapa simulado de AgroFlow

El mapa permite demostrar viajes ficticios de finca a ingenio. Su alcance se define en el [cambio OpenSpec mapa-simulado-presentacion](../../openspec/changes/mapa-simulado-presentacion/proposal.md). La preparación del guion completo se sigue en [#111](https://github.com/agustinvallante/AgroFlow/issues/111).

## Iniciar

Desde la raíz del repositorio, con Node y npm disponibles:

```powershell
npm --prefix frontend ci
npm --prefix frontend run dev -- --host 127.0.0.1 --port 5173 --strictPort
```

Iniciar también el backend y configurar las claves según la [guía de demo local](local-demo.md): el mapa toma el catálogo de fincas de `GET /api/v1/farms`. Abrir `http://127.0.0.1:5173` y elegir Mapa. Sin API, la vista muestra el ingenio, avisa que no pudo cargar las fincas y ofrece Reintentar.

## Escenario ficticio reproducible

| Dato | Valor |
|---|---|
| Ingenio | Ubicación de referencia (-26.83, -65.2); se puede cambiar desde el formulario Ingenio |
| Fincas | Finca Norte y Finca Sur del seed de LocalDemo, al norte y al sur del ingenio |
| Viaje | Patente SIM123, Finca Sur, velocidad 40 km/h, salida en 0 minutos, retraso 0 minutos |

1. Comprobar que Finca Norte y Finca Sur aparecen en el mapa, cada una unida a una ruta del fondo.
2. En **Cola de turnos**, crear un turno desde Finca Sur. Al volver a Mapa, en menos de 10 segundos aparece solo, sin botón para quitarlo. Llega al inicio de su ventana; con x60 recorre la ruta en poco más de un minuto. Si el turno pasa a En espera, el mapa lo muestra como llegado. Si el transportista informa por WhatsApp un problema en ruta (por ejemplo, un pinchazo), en la siguiente lectura el camión pasa a Demora y queda donde estaba; su detalle muestra una fila Problema, por ejemplo «Pinchazo · puede seguir · demora est. 30 min». Al informar que lo resolvió, vuelve a avanzar.
3. Los campos bajo **Sólo para la demo** cargan viajes de prueba que no se envían a la API; se usan para los pasos siguientes.
4. Agregar el viaje. Debe aparecer En ruta en la cola y como un círculo sobre la ruta de su finca. Las fincas y el ingenio se dibujan como cuadrados.
5. Tocar el camión: el recuadro muestra patente, estado, finca, salida, llegada, distancia y distancia restante. Cerrarlo con ×, Escape o tocando el mapa.
6. Seleccionar x60. Con las distancias de la ubicación temporal, el recorrido tarda menos de un minuto real.
7. Esperar Llegó. El viaje permanece visible al llegar y su recorrido termina.
8. Recargar el mismo origen. Deben conservarse el ingenio y el viaje. La velocidad vuelve a x1 y el adelanto acumulado se conserva.
9. Quitar el viaje, cargar otro con retraso de 10 minutos y usar x60. Al superar la llegada estimada debe pasar a Demora y, tras completar el retraso, a Llegó.
10. Para observar Programado, cargar otro viaje con una salida de 5 minutos. También puede verificarse que se rechaza un segundo viaje sin terminar de la misma patente.

## Datos y reloj

Ingenio, viajes de prueba y adelanto viven en `agroflow.mapa-simulado.v3` de localStorage; las fincas y los turnos se vuelven a leer de la API. Con x10 o x60 también se adelantan los viajes de la cola: para la demo importa mostrar el recorrido, no la precisión. Cada navegador, perfil y origen tiene su escenario propio. Abrir otro puerto u otra dirección, aunque apunten al mismo equipo, usa otro almacenamiento. Para mostrarlo al grupo conviene compartir una sola pantalla.

El reloj a x1 sigue el tiempo real. Las velocidades x10 y x60 acumulan un adelanto únicamente mientras el mapa está abierto. Al regresar tras recargar o cambiar de sección, se conserva ese adelanto y se agrega el tiempo real transcurrido. El reloj visible usa la zona de la demo, America/Argentina/Tucuman.

El formulario admite velocidades de 1 a 120 km/h y salidas y retrasos de 0 a 1440 minutos. Si lo guardado contiene valores fuera de esos límites, por ejemplo por una versión anterior o una edición manual, el mapa los descarta al abrirse en lugar de dejar de funcionar. Con x10 y x60 el adelanto se guarda cada pocos segundos y al cambiar de velocidad, de sección o cerrar la página.

Se pueden quitar viajes desde la cola. Un viaje de una finca que la API deja de publicar no se muestra. Para reiniciar únicamente esta simulación, abrir las herramientas del navegador y eliminar la clave `agroflow.mapa-simulado.v3` del origen actual, y recargar.

## Interpretación de la demostración

La API publica el catálogo de fincas, pero no coordenadas: como medida temporal, el frontend ubica cada finca de forma determinista entre 14 y 30 km del ingenio, hacia el rumbo que indique su nombre. Las posiciones proceden de la distancia haversine multiplicada por 1,3 y de una velocidad constante. Las rutas, accesos y ríos del fondo son simulados; cada recorrido los sigue, pero no son caminos reales. El retraso es un dato ficticio que difiere la llegada local.

El recorrido mostrado es finca → ingenio. La llegada simulada no registra EN_ESPERA ni otro estado operativo. El bot conserva su contrato de turnos y su significado actual de EN_CAMINO. Relacionar ambas cosas requiere un acuerdo posterior del dominio y su cambio compartido de OpenSpec.

Las coordenadas, viajes y llegadas no se envían a la API; sólo se lee el catálogo de fincas. Los datos operativos de la cola y las conversaciones siguen viniendo del backend. Los problemas en ruta que informa la API se muestran como demora y no cambian el estado del turno. Las rutas reales y la integración con GPS o API requieren una especificación posterior.

## Cobertura y revisión

| Escenario | Evidencia |
|---|---|
| Fincas de la API, falla de carga, formularios y duplicados | `frontend/src/presentation/views/MapaView.test.tsx` |
| Movimiento, llegada, retraso y x1/x60 | El mismo archivo y `frontend/src/domain/services/simulacionViajes.test.ts` |
| Regreso a la pestaña, adelanto tras recarga y detalle del camión | `MapaView.test.tsx` |
| Problema en ruta: demora, posición detenida, detalle y resolución | `MapaView.test.tsx` y `frontend/src/domain/services/viajesDesdeTurnos.test.ts` |
| Límites de entrada, datos guardados inválidos y almacenamiento bloqueado | `MapaView.test.tsx` y `simulacionViajes.test.ts` |
| Red vial, recorridos y etiquetas | `geometriaMapa.test.ts` en componentes/mapa |
| API desconectada y recarga real | Guion de navegador descrito arriba y evidencia publicada en la PR |

Las pruebas de distancia son deterministas. El tiempo aproximado del guion depende del instante de carga y del refresco del navegador. La CI y el comentario de revisión de [#105](https://github.com/agustinvallante/AgroFlow/pull/105) registran la evidencia de cada commit.
