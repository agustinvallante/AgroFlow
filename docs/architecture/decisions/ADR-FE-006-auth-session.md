# ADR-FE-006: Propuestas de custodia de sesión web

> **Estado: decisión abierta; las alternativas de este documento no están
> seleccionadas.** El modelo anterior de cookie opaca y sesión BFF de Next.js
> no es una decisión aprobada y no debe implementarse por inercia.

## Contexto

OpenSpec define autenticación interna y JWT backend; no fija cómo una futura
interfaz web conserva o media credenciales. ADR-005 mantiene React/Vite. En
PR #47 se propuso un identificador opaco en cookie `HttpOnly` y estado de sesión
del lado servidor en un BFF de Next.js, pero esa propuesta dependía de la
plataforma no seleccionada.

## Opciones que requieren decisión

Deben evaluarse explícitamente alternativas de custodia y comunicación entre
frontend y backend, incluidas sus amenazas, compatibilidad con React/Vite,
contratos, CSRF, expiración, renovación, revocación, despliegue, almacenamiento,
múltiples instancias y fallas. Esta lista no preselecciona cookie, BFF, token en
el navegador ni sesión gestionada directamente por el backend.

## Límites

No hay BFF, cookie, sesión web, endpoints o almacenamiento aprobados por este
ADR. El backend conserva la autoridad sobre identidad, roles, autorización y
aislamiento. La decisión requiere acuerdo explícito y, si modifica
comportamiento, cambio OpenSpec de la capacidad propietaria y contrato revisado.
