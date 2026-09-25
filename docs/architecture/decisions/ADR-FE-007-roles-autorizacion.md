# ADR-FE-007: Roles visibles y autorización efectiva

- Estado: aceptada en su límite; matriz funcional pendiente
- Alcance: experiencia por rol y frontera de autorización
- Implementación: bloqueada por OD-007 para permisos concretos

## Contexto

La interfaz puede mostrar navegación y acciones según el usuario, pero ocultar un control no protege una operación. OpenSpec exige autorización por rol en el backend y aislamiento por ingenio. La matriz concreta para operador, supervisor, gerente y administración sigue pendiente.

## Decisión

El backend SHALL ser la autoridad de permisos y pertenencia al ingenio. El frontend podrá adaptar navegación, mensajes y disponibilidad visual usando información confirmada, pero nunca considerará esa adaptación una barrera de seguridad.

No se codificarán nombres de rol, matrices de acciones ni excepciones hasta resolver OD-007 y publicarlas en especificación y contrato. Un recurso de otro ingenio no se solicitará ni mostrará a partir de un selector libre del cliente.

## Alternativas consideradas

- **Autorizar solo en la UI:** rechazada porque puede eludirse y contradice ADR-003.
- **Mostrar siempre toda acción y depender del rechazo:** mantiene seguridad del backend, pero degrada la experiencia y revela operaciones irrelevantes.
- **Permisos confirmados por backend más verificación en cada operación:** mantiene defensa efectiva y permite una UI contextual.

## Consecuencias

- Cada operación seguirá verificándose en el backend aunque el control esté oculto.
- `401` y `403` tendrán tratamientos distintos según el contrato.
- Las pruebas integradas deberán incluir intentos entre ingenios y fuera de rol.
- La navegación funcional no puede cerrarse antes de la matriz aprobada.

## Dependencias no resueltas

- OD-007: roles válidos y operaciones permitidas;
- forma contractual de comunicar identidad, ingenio y permisos;
- estrategia de sesión de [ADR-FE-006](ADR-FE-006-auth-session.md).

## Referencias

- [ADR-003: backend como fuente de verdad](ADR-003-backend-fuente-de-verdad.md)
- [Plataforma y segregación](../../../openspec/specs/plataforma-y-segregacion/spec.md)
