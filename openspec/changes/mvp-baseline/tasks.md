# Tareas: corrección documental del baseline de PR #47

Esta lista registra controles de revisión y decisión para esta corrección exclusivamente documental. No es un plan de implementación para los temas OD que siguen abiertos.

## 1. Reconciliar la documentación

- [x] 1.1 Confirmar `main`, ADR-004 y B00 como autoridades para las decisiones ya aprobadas; conservar abiertas las demás opciones.
- [x] 1.2 Marcar este cambio como exclusivamente documental (`spec-driven` con `skip_specs: true`) y no agregar especificaciones normativas de capacidades.
- [x] 1.3 Corregir la propuesta, el diseño, las tareas y la referencia de defaults para distinguir decisiones aprobadas, aprobaciones parciales y propuestas no normativas.
- [ ] 1.4 Revisar la corrección frente a `main`, ADR-004, B00 y `open-decisions.md`; registrar para seguimiento fuera de este alcance cualquier contradicción restante.

## 2. Decisiones futuras: no autorizan implementación

- [ ] 2.1 Para OD-006 y OD-008 a OD-011, obtener decisiones explícitas mediante el proceso aplicable de B00/ADR y luego crear o actualizar los cambios OpenSpec de las capacidades propietarias antes de considerar su implementación.
- [ ] 2.2 Resolver explícitamente los detalles abiertos de OD-012 y los campos editables, la gestión de asociaciones y la reactivación de OD-013; actualizar la documentación de las capacidades propietarias antes de implementar.
- [ ] 2.3 Mantener abierta la matriz restante de roles y permisos de OD-007, así como las rutas, DTO y convenciones de paginación de B00, hasta que se decidan por separado.
- [ ] 2.4 Conservar las propuestas futuras útiles como contexto no normativo salvo aprobación explícita; no inferir una matriz completa de cuatro roles, permisos para reintentar notificaciones, un intervalo fijo de actualización, una zona horaria ni una duración fija de ventana como regla de negocio.

## 3. Verificación

- [ ] 3.1 Validar los metadatos de OpenSpec y todos los cambios.
- [ ] 3.2 Revisar los enlaces Markdown locales y los espacios en blanco.
- [ ] 3.3 Informar al responsable del trabajo cualquier contradicción fuera de los archivos autorizados.
