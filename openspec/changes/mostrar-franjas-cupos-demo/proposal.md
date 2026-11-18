# Mostrar franjas y cupos en el panel LocalDemo

## Motivo

La PR #119 integró la lectura de ventanas de B39 (#44), pero el panel seguía
mostrando una grilla vacía. Este corte de F07 (#73) conecta esa lectura para
demostrar ocupación y liberación de capacidad sin calcularlas en el navegador.

## Alcance

- Puerto de lectura, adaptador HTTP y caso de uso de ventanas independientes de
  las métricas aún no publicadas.
- Panel general: fecha opcional, todas las franjas, capacidad, ocupados y
  disponibles entregados por `GET /api/v1/windows`.
- Hoy lo decide la API; fechas/horas conservan el desplazamiento del ingenio.
- Refresco cada cuatro segundos, al entrar al panel, manualmente y después
  de una transición desde el panel. Altas/cancelaciones de otra vista o canal
  se reflejan en el siguiente polling.
- Vacío, error inicial, reintento y última lectura desactualizada explícitos.
- Proxy local: sólo el GET exacto de ventanas recibe la clave del dashboard
  del servidor; conserva controles de origen/loopback y elimina claves entrantes.

## Límites

Entrega parcial de LocalDemo; **no cierra #73 ni #44**. No agrega indicadores,
interrupciones, reprogramación, autenticación MVP ni edición del calendario.
No modifica la API, el contrato OpenAPI 0.7.0, la base, el seed ni dependencias.
No resuelve OD-004/OD-012 del MVP. Camiones en curso conserva su lectura de
activos de cualquier fecha, independiente del filtro de franjas.

## Capacidad

Delta de [monitoreo-operativo](specs/monitoreo-operativo/spec.md).
Guía y prueba integrada en
[franjas del dashboard](../../../docs/development/dashboard-windows-demo.md).
