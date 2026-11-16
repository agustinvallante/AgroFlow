# Compatibilidad del cursor

`PageCursorCodec` codifica ASCII `<ticks UTC>.<UUID N>` en base64url sin
padding. Las fechas actuales ocupan 18 + 1 + 32 = 51 bytes, es decir 68
caracteres. DateTime.MaxValue tiene 19 dígitos de ticks: 52 bytes se codifican
en ceil(52 × 8 / 6) = 70 caracteres sin padding.

El límite se deriva en `MaxEncodedLength` a partir del máximo de bytes. La
prueba del timestamp máximo verifica que coincide con la salida del encoder.
Los bytes y el algoritmo de Encode no cambian, por lo que un cursor válido
emitido previamente no necesita conversión.

El rechazo de valores demasiado largos sigue ocurriendo antes de decodificar.
Se conservan la comprobación del rango de ticks, UUID N y recodificación
canónica; no se admiten variantes con padding, espacios, ceros iniciales ni
bits sobrantes alternativos.

La sintaxis del cursor no concede acceso. El store sigue filtrando por ingenio
y comprueba que el ancla de mensajes pertenezca a la conversación consultada.
No se modifica el store ni se agregan claves o parámetros de ingenio.

Las pruebas HTTP reproducen el uso real de `nextCursor`, a diferencia de las
pruebas del store que reciben PageCursor ya decodificado. Se recorren las
páginas hasta null y se verifican IDs y orden, incluso con mensajes nuevos
entre lecturas. El caso de 51 mensajes usa limit=50, igual que el frontend.
