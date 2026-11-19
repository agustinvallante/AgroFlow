# Indicadores del panel para la presentación

## Why

El Panel general muestra cuatro indicadores sin datos. Para la presentación se
necesita completar los que puedan obtenerse sin cambiar el backend ni el
contrato, y dejar explícito cuáles son medidas temporales del frontend.
La API no guarda el origen de un turno, el estado de la molienda ni el momento
en que un turno pasó a `EN_ESPERA`.

## What Changes

- **Camiones en espera ahora:** cuenta los turnos `EN_ESPERA` de la misma
  lectura `GET /api/v1/appointments?active=true` que usa Camiones en curso,
  incluidos los de ventanas de otro día. Sin una respuesta válida muestra "—".
- **Turnos gestionados por el bot (medida temporal):** el navegador recuerda los
  turnos que crea con "Nuevo turno"; los demás turnos activos se atribuyen al
  bot. Se muestra el total junto a la cantidad de turnos activos.
- **Estado de la molienda (simulado):** un interruptor alterna entre Operando y
  Detenida, se recuerda en el navegador y no avisa por WhatsApp ni modifica
  turnos ni interrupciones.
- **Espera promedio en canchón** se mantiene sin dato.

## Impact

Sólo frontend: vista y pruebas del Panel general, y su fila en
`frontend/README.md`. No cambia el contrato HTTP, la base de datos ni las
reglas de interrupciones. Las medidas temporales no reemplazan datos del
backend ([ADR-003](../../../docs/architecture/decisions/ADR-003-backend-fuente-de-verdad.md))
ni cierran la fórmula de los indicadores del MVP pendiente en `OD-012`.
La solución definitiva requiere que el backend persista el origen del turno y
publique el estado de la molienda como interrupción activa (RN-041).
