# Definición de terminado

Una tarea funcional se considera terminada solo cuando cumplen los puntos que le corresponden.

## Especificación y alcance

- [ ] El issue enlaza capacidad, caso de uso y reglas relevantes.
- [ ] El comportamiento nuevo o modificado fue aprobado mediante un cambio OpenSpec.
- [ ] No queda una decisión pendiente que impida verificar el resultado.
- [ ] El alcance implementado coincide con la propuesta y no introduce reglas no acordadas.

## Contrato e implementación

- [ ] El contrato compartido fue actualizado antes o junto con sus consumidores.
- [ ] El backend vuelve a validar permisos, ingenio y datos, aunque la interfaz también lo haga.
- [ ] Frontend y n8n presentan el estado confirmado por la API y no duplican reglas de negocio.
- [ ] Migraciones, configuración y compatibilidad fueron consideradas cuando aplican.

## Pruebas y calidad

- [ ] Cada escenario OpenSpec afectado tiene evidencia de prueba automatizada o una justificación explícita.
- [ ] Se probaron éxito, validaciones, permisos, conflictos y errores relevantes.
- [ ] Compilan frontend y backend y pasan sus verificaciones estáticas.
- [ ] Los flujos críticos de capacidad, estado e idempotencia incluyen pruebas de concurrencia cuando corresponda.

## Seguridad y operación

- [ ] No se versionaron secretos, credenciales ni datos personales reales.
- [ ] Los errores y registros no revelan información sensible.
- [ ] Se evaluaron observabilidad, recuperación y efecto operacional del cambio.
- [ ] Las dependencias nuevas tienen propósito, versión y riesgo revisados.

## Documentación y revisión

- [ ] OpenSpec, OpenAPI, ADR y guías permanecen coherentes.
- [ ] El pull request explica cómo verificar el resultado y enlaza issues y cambios.
- [ ] Se resolvieron observaciones de revisión de las áreas afectadas.
- [ ] El comportamiento puede demostrarse con datos controlados y pasos reproducibles.
