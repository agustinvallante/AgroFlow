# ADR-FE-007: Roles visibles y autorización efectiva

- Estado: aceptada en su límite; permisos parcialmente aprobados
- Alcance: experiencia por rol y frontera de autorización
- Implementación: la experiencia debe limitarse a permisos normativos vigentes; permisos restantes de OD-007 siguen pendientes

## Contexto

La interfaz puede mostrar navegación y acciones según el usuario, pero ocultar un control no protege una operación. OpenSpec exige autorización por rol en el backend y aislamiento por ingenio. Ya es normativo que operador y supervisor puedan listar, crear, editar e inhabilitar transportistas, camiones y fincas y crear/cancelar turnos dentro de su ingenio. El resto de la matriz —incluidos gerente/administración y gestión de asociaciones— sigue pendiente.

## Decisión

El backend SHALL ser la autoridad de permisos y pertenencia al ingenio. El frontend podrá adaptar navegación, mensajes y disponibilidad visual usando información confirmada, pero nunca considerará esa adaptación una barrera de seguridad.

Solo podrán representarse en la experiencia los permisos ya aprobados en OpenSpec. No se inferirán ni codificarán permisos adicionales, matrices para gerente/administración ni gestión de asociaciones mientras OD-007 siga abierto en esos aspectos. Un recurso de otro ingenio no se solicitará ni mostrará a partir de un selector libre del cliente.

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

- OD-007: permisos adicionales a la porción ya normativa, incluidos gerente/administración y gestión de asociaciones;
- forma contractual de comunicar identidad, ingenio y permisos;
- estrategia de sesión de [ADR-FE-006](ADR-FE-006-auth-session.md).

## Referencias

- [ADR-003: backend como fuente de verdad](ADR-003-backend-fuente-de-verdad.md)
- [Plataforma y segregación](../../../openspec/specs/plataforma-y-segregacion/spec.md)
