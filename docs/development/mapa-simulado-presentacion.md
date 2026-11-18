# Probar el mapa simulado de AgroFlow

El mapa permite demostrar viajes ficticios de finca a ingenio. Su alcance se define en el [cambio OpenSpec mapa-simulado-presentacion](../../openspec/changes/mapa-simulado-presentacion/proposal.md). La preparación del guion completo se sigue en [#111](https://github.com/agustinvallante/AgroFlow/issues/111).

## Iniciar

Desde la raíz del repositorio, con Node y npm disponibles:

```powershell
npm --prefix frontend ci
npm --prefix frontend run dev -- --host 127.0.0.1 --port 5173 --strictPort
```

Abrir `http://127.0.0.1:5173` y elegir Mapa. Esta pestaña funciona sin API. Para ensayar turnos, transportistas y conversaciones, iniciar también el backend y configurar las claves según la [guía de demo local](local-demo.md).

## Escenario ficticio reproducible

| Formulario | Datos |
|---|---|
| Ingenio | Nombre Ingenio simulado, latitud -26.82, longitud -65.22 |
| Finca | Nombre Finca simulada, latitud -26.84, longitud -65.24 |
| Viaje | Patente SIM123, finca Finca simulada, velocidad 40 km/h, salida en 0 minutos, retraso 0 minutos |

1. Guardar el ingenio y agregar la finca.
2. Agregar el viaje. Debe aparecer En ruta en la cola y en el SVG.
3. Seleccionar x60. La distancia estimada ronda 4 km y el recorrido tarda alrededor de seis segundos reales con esa aceleración.
4. Esperar Llegó. El viaje permanece visible al llegar y su recorrido termina.
5. Recargar el mismo origen. Deben conservarse los lugares y el viaje. La velocidad vuelve a x1 y el adelanto acumulado se conserva.
6. Quitar el viaje, cargar otro con retraso de 10 minutos y usar x60. Al superar la llegada estimada debe pasar a Demora y, tras completar el retraso, a Llegó.
7. Para observar Programado, cargar otro viaje con una salida de 5 minutos. También puede verificarse que se rechaza un segundo viaje sin terminar de la misma patente.

## Datos y reloj

Los datos viven en `agroflow.mapa-simulado.v2` de localStorage. Cada navegador, perfil y origen tiene su escenario propio. Abrir otro puerto u otra dirección, aunque apunten al mismo equipo, usa otro almacenamiento. Para mostrarlo al grupo conviene compartir una sola pantalla.

El reloj a x1 sigue el tiempo real. Las velocidades x10 y x60 acumulan un adelanto únicamente mientras el mapa está abierto. Al regresar tras recargar o cambiar de sección, se conserva ese adelanto y se agrega el tiempo real transcurrido. El reloj visible usa la zona de la demo, America/Argentina/Tucuman.

El formulario admite velocidades de 1 a 120 km/h y salidas y retrasos de 0 a 1440 minutos. Si lo guardado contiene valores fuera de esos límites, por ejemplo por una versión anterior o una edición manual, el mapa los descarta al abrirse en lugar de dejar de funcionar. Con x10 y x60 el adelanto se guarda cada pocos segundos y al cambiar de velocidad, de sección o cerrar la página.

Se pueden quitar viajes desde la cola y fincas desde la lista. Quitar una finca elimina sus viajes. Para reiniciar únicamente esta simulación, abrir las herramientas del navegador y eliminar la clave `agroflow.mapa-simulado.v2` del origen actual, y recargar.

## Interpretación de la demostración

Las posiciones proceden de la distancia haversine multiplicada por 1,3 y de una velocidad constante. El SVG y su fondo son ilustrativos. El retraso es un dato ficticio que difiere la llegada local.

El recorrido mostrado es finca → ingenio. La llegada simulada no registra EN_ESPERA ni otro estado operativo. El bot conserva su contrato de turnos y su significado actual de EN_CAMINO. Relacionar ambas cosas requiere un acuerdo posterior del dominio y su cambio compartido de OpenSpec.

Las coordenadas, viajes y llegadas no se envían a la API. Los datos operativos de la cola y las conversaciones siguen viniendo del backend. Las detenciones reportadas por WhatsApp, las rutas reales y la integración con GPS o API requieren una especificación posterior.

## Cobertura y revisión

| Escenario | Evidencia |
|---|---|
| Vacío, formularios y duplicados | `frontend/src/presentation/views/MapaView.test.tsx` |
| Movimiento, llegada, retraso y x1/x60 | El mismo archivo y `frontend/src/domain/services/simulacionViajes.test.ts` |
| Regreso a la pestaña, adelanto tras recarga y eliminación de finca | `MapaView.test.tsx` |
| Límites de entrada, datos guardados inválidos y almacenamiento bloqueado | `MapaView.test.tsx` y `simulacionViajes.test.ts` |
| Trayectorias y etiquetas | `geometriaMapa.test.ts` y `curvasDeNivel.test.ts` en componentes/mapa |
| API desconectada y recarga real | Guion de navegador descrito arriba y evidencia publicada en la PR |

Las pruebas de distancia son deterministas. El tiempo aproximado del guion depende del instante de carga y del refresco del navegador. La CI y el comentario de revisión de [#105](https://github.com/agustinvallante/AgroFlow/pull/105) registran la evidencia de cada commit.
