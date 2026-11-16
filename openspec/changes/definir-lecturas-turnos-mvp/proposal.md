# Definir las lecturas internas de turnos del MVP

**Estado:** primer corte de [B00 #7](https://github.com/agustinvallante/AgroFlow/issues/7) con implementación de listado y detalle. Base del código: `main` en `8873a26`, incorporada a la rama mediante merge.

## Resultado

Reutilizar `GET /api/v1/appointments` y `GET /api/v1/appointments/{id}` para [B25 #29](https://github.com/agustinvallante/AgroFlow/issues/29) y [B26 #30](https://github.com/agustinvallante/AgroFlow/issues/30). La [capacidad de consulta](../../specs/turnos-consulta/spec.md) y su [delta](specs/turnos-consulta/spec.md) fijan el comportamiento. [OpenAPI del MVP](../../../docs/contracts/openapi.mvp-appointment-reads.yaml) fija filtros, paginación, esquemas y errores para las operaciones `listarTurnosMvp` y `obtenerTurnoMvp`.

El [contrato publicado de LocalDemo](../../../docs/contracts/openapi.yaml) conserva sus rutas y formas de respuesta para el dashboard y n8n. Los contratos se distinguen por perfil de ejecución y comparten los esquemas comunes mediante referencias. Las claves de LocalDemo no conceden acceso a las lecturas internas del MVP.

## Decisiones de este corte

- [OD-007](../../../docs/planning/open-decisions.md): operador y supervisor tienen acceso al listado y al detalle de su ingenio. Los demás permisos siguen sujetos a la matriz del MVP.
- [OD-004](../../../docs/planning/open-decisions.md): el día operativo de las lecturas es el día calendario de la zona configurada para el ingenio. La selección de ventanas y el calendario de atención se resuelven en su propio corte.
- La proyección de cada fila usa el esquema de detalle `Appointment` para que la cola muestre transportista, camión, finca, franja, corte, carga y estado sin consultas adicionales. El contrato omite datos de contacto, cálculo de prioridad e historial de transiciones.
- `page` y `pageSize` son opcionales y el total pertenece al ingenio y filtros de la petición. Se ordena antes de paginar y las peticiones sucesivas leen el estado vigente.

## Alcance de las dependencias

| Issue | Entrega mínima para estas lecturas |
|---|---|
| [B02 #9](https://github.com/agustinvallante/AgroFlow/issues/9) | Perfil SQLite con identidad y turnos en el mismo entorno, migración de los datos existentes y datos ficticios de dos ingenios. Confirmar que las ventanas antiguas tengan un inicio UTC utilizable por la consulta. |
| [B03 #10](https://github.com/agustinvallante/AgroFlow/issues/10) | Vincular usuario, rol e ingenio y derivar el ámbito del JWT validado. Usar `sub` estable y el claim `ingenioId` existente. |
| [B04 #11](https://github.com/agustinvallante/AgroFlow/issues/11) | Normalizar respuestas de validación y autorización en `application/problem+json` con los códigos de este contrato. Ejecutar pruebas HTTP con SQLite real. |
| [F06 #72](https://github.com/agustinvallante/AgroFlow/issues/72) | Coordinar el consumo de la página y sus campos. La entrega de la pantalla no bloquea la API. |

La consulta de transportista por WhatsApp mantiene su recorrido LocalDemo y se evoluciona al MVP en [B31 #35](https://github.com/agustinvallante/AgroFlow/issues/35). Las escrituras de turnos, la asignación de ventanas y la gestión de datos maestros quedan fuera de este cambio.

## Implementación

`AgroFlowDbContext` migra los datos de dominio e Identity en un solo archivo SQLite y conserva el historial de migraciones de turnos. El seed académico se habilita con una contraseña externa y crea una vez datos ficticios de dos ingenios. El JWT usa el ID estable de Identity, el rol y el ingenio. En cada lectura se valida que el usuario siga activo y que su ingenio y rol coincidan con los del token. La autorización precede a la validación de parámetros.

La consulta EF acota turno y referencias al ingenio, aplica en SQL los límites UTC del día y devuelve una página con proyección de la cola. El detalle aplica el mismo ámbito y devuelve la representación persistida. El [procedimiento de arranque](../../../backend/README.md#probar-las-lecturas-internas-del-mvp) describe cómo verificar ambas operaciones.

## Login provisional y trabajo restante de B03

`POST /api/auth/login` es andamiaje de prueba fuera del contrato de este corte:
permite obtener un JWT para ensayar las dos lecturas con las cuentas ficticias.
La adaptación del login heredado usa el ID de Identity como `sub`, un rol válido
y `ingenioId`, y rechaza usuarios inactivos o sin ingenio. Esto no constituye la
entrega del acceso interno completo ni publica un contrato de sesión para F01.

El contrato de login, su validación y sus respuestas se deben especificar en
[B10 #14](https://github.com/agustinvallante/AgroFlow/issues/14), coordinado con
[B03 #10](https://github.com/agustinvallante/AgroFlow/issues/10), antes de integrarlo
como flujo del MVP. Hoy su 401 es texto plano y no el `problem+json` de las
lecturas. El README describe ese comportamiento únicamente para reproducir las
pruebas, sin convertirlo en un contrato estable.

En B03 quedan pendientes el bloqueo o limitación de intentos y el tratamiento de
tiempos de respuesta frente a usuarios inexistentes. Actualmente
`CheckPasswordSignInAsync` usa `lockoutOnFailure: false` y el usuario inexistente
se rechaza antes de verificar un hash. El mensaje genérico cumple RN-001/RN-007,
pero no acredita protección frente a fuerza bruta ni frente a enumeración por
tiempo. Este corte no modifica esa política ni cierra B03/B10.

## Aceptación y cierre de issues

Las dos lecturas están implementadas y probadas, pero la revisión del contrato
con F06 y la consolidación del delta continúan pendientes en
[tasks.md](tasks.md). La PR referencia #29 y #30 sin cerrarlas automáticamente.
Se podrán cerrar al confirmar su aceptación y separar cualquier pendiente ajeno
a las lecturas. En particular, la visibilidad de turnos futuros en la demo se
atiende en [#108](https://github.com/agustinvallante/AgroFlow/issues/108) y
[#109](https://github.com/agustinvallante/AgroFlow/issues/109), mediante la
[PR #115](https://github.com/agustinvallante/AgroFlow/pull/115), y la verificación
de WhatsApp real está en [#110](https://github.com/agustinvallante/AgroFlow/issues/110).
