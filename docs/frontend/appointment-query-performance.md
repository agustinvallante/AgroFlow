# Rendimiento de consultas de turnos — pendiente del MVP

Estado: pendiente de implementación en [F06 #72](https://github.com/agustinvallante/AgroFlow/issues/72)
y de coordinación con el panel [F07 #73](https://github.com/agustinvallante/AgroFlow/issues/73).
Observación registrada en la [review de la PR #115](https://github.com/agustinvallante/AgroFlow/pull/115#discussion_r4579234626).
Esta nota registra deuda técnica; no agrega endpoints ni cambia el contrato de la presentación.

## Comportamiento actual de LocalDemo

`HttpTurnoRepository.listar` obtiene resúmenes y completa los campos que faltan
mediante un detalle por turno sin caché. Los detalles se solicitan en paralelo.
`detalles` y `versiones` viven en la instancia del repositorio, compartida por
las vistas mediante el contenedor.

| Situación | Solicitudes del listado |
|---|---|
| Primera consulta con N turnos desconocidos | 1 listado + N detalles |
| Polling con todos los detalles ya cacheados | 1 listado |
| Llegan M turnos todavía desconocidos | 1 listado + M detalles |

El polling del listado corre cada cuatro segundos. La ventana y el estado
recientes del resumen prevalecen sobre los del detalle cacheado. Un detalle
seleccionado se refresca por separado cada cuatro segundos, y abrirlo también
solicita una lectura vigente. Esos pedidos explícitos son adicionales a la tabla.
Las respuestas de alta y transición actualizan la caché.

Por eso no se repiten N detalles en todos los refrescos, pero sí existe el coste
1+N con caché vacía. `active=true` incluye turnos activos de cualquier fecha,
incluso los ASIGNADO que el panel no representa en Camiones en curso; una primera
carga amplia puede disparar muchos pedidos. La caché no tiene límite ni una
política de desalojo, y una sesión nueva vuelve a cargar sus detalles.

La presentación conserva este adaptador. La prueba
`HttpTurnoRepository.test.ts` verifica que el segundo listado activo reutiliza
el detalle y actualiza el estado con el resumen vigente.

## Trabajo a realizar al integrar el MVP

1. Coordinar F06/F07 con [la PR #106](https://github.com/agustinvallante/AgroFlow/pull/106),
   que propone listado paginado con proyección completa de cada turno.
   Adaptar DTO, mapper, repositorio y caso de uso al contrato finalmente aceptado,
   usando sus campos para la fila sin un GET de detalle por cada ítem.
2. Mantener el detalle individual para la selección explícita y su actualización,
   preservando autorización por ingenio y descarte de lecturas anteriores a una
   transición confirmada.
3. Acordar en [B00 #7](https://github.com/agustinvallante/AgroFlow/issues/7) la consulta
   del panel para activos de cualquier fecha. El contrato MVP de #106 no admite
   `active=true` y su listado sin fecha mantiene el día actual. Tampoco devuelve
   el arreglo de resúmenes de LocalDemo: devuelve una página con `items`.
   No intercambiar perfiles ni trasladar el parámetro sin especificar esa
   compatibilidad.
4. Medir primera carga, refresco y llegada de turnos nuevos con un conjunto
   ficticio mayor al de la exposición. Usar paginación según el contrato y definir
   el ciclo de vida de cualquier caché que se conserve; no ampliar el número de
   pedidos por fila para completar datos disponibles en la proyección.

## Criterios para verificar la mejora

- Cargar una página exige un listado por página, sin N detalles automáticos.
- La fila conserva transportista, camión, finca, ventana, corte, carga y estado.
- Abrir o refrescar el detalle sigue siendo una lectura explícita y vigente.
- Una transición confirmada o un resumen reciente no se revierte por una lectura vieja.
- El panel conserva su alcance temporal acordado y la cola conserva filtros y paginación.
- Se prueban por separado LocalDemo y MVP mientras ambos perfiles sigan disponibles.
