# ADR-FE-007: Límite de autorización en la interfaz

> **Estado: principio evaluable; no amplía permisos.** OpenSpec y ADR-004 son
> la autoridad normativa para permisos aprobados y pendientes.

PR #47 proponía adaptar la navegación a permisos y delegar autorización efectiva
al backend. Se conserva como principio de diseño general: ocultar un control no
es una barrera de seguridad. La UI no decide autorización ni pertenencia al
ingenio.

Los permisos aprobados de operador/supervisor para catálogos y turnos internos
son únicamente los descritos en `openspec/specs/acceso-y-autorizacion/spec.md`
y `datos-maestros/spec.md`. Permisos para asociaciones, otros roles y el resto
de OD-007 permanecen pendientes. Este ADR no elige modelo de sesión ni campos
contractuales para comunicar permisos.
