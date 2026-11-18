# Corregir los cursores de conversaciones (#122)

## Problema

La API genera cursores de 68 caracteres para las fechas actuales, pero
`TryDecode` y OpenAPI sólo aceptaban hasta 64. La primera página era válida y
la siguiente fallaba con 400. Afecta tanto mensajes como conversaciones.
El frontend carga mensajes en páginas de 50, por lo que un chat largo dispara
el defecto al abrirse.

## Cambio

- Conservar el formato base64url opaco sin padding de ticks UTC y UUID.
- Admitir hasta 70 caracteres: cubre el rango completo de DateTime.
- Mantener validación canónica, límites, anclaje a la conversación y filtro
  por ingenio; no cambiar escrituras, seguridad, datos ni paginación frontend.
- Alinear query y nextCursor de ambas respuestas en OpenAPI 0.7.1.
- Agregar pruebas de codec, recorridos HTTP de varias páginas, nuevas llegadas
  y aislamiento. Verificar un chat largo con API/SQLite y navegador reales.

## Capacidad y documentación

Delta de [conversaciones-whatsapp](specs/conversaciones-whatsapp/spec.md).
Complementa los requisitos del cambio
[persistir-conversaciones-whatsapp](../persistir-conversaciones-whatsapp/specs/conversaciones-whatsapp/spec.md).
Guía de QA: [paginación de conversaciones](../../../docs/development/conversation-pagination.md).

La corrección no requiere decisiones nuevas del MVP ni migraciones. No
implementa paginación adicional del listado frontend ni amplía su límite
existente de 1000 mensajes por lectura.
