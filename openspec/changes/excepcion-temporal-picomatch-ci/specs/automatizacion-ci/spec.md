## ADDED Requirements

### Requirement: Excepción temporal al aviso de Picomatch en CI

El control del frontend SHALL seguir ejecutando `npm audit --audit-level=high` y publicando los hallazgos. Hasta el 2 de diciembre de 2026 a las 03:00 UTC, SHALL permitir solamente el aviso `GHSA-fc5q-39xc-vjpf` de severidad alta para la instalación transitiva de desarrollo `picomatch@4.0.7` presente en el lockfile, incluidos los hallazgos derivados exclusivamente de ese aviso. La auditoría programada y la revisión de cambios de dependencias SHALL conservar sus criterios de bloqueo.

#### Scenario: El único aviso bloqueante es el de Picomatch

- **WHEN** npm informa el aviso permitido y sus efectos transitivos antes del vencimiento
- **THEN** CI muestra la advertencia y ejecuta el chequeo de tipos, el lint, las pruebas y la compilación

#### Scenario: Hay un hallazgo bloqueante adicional o venció la excepción

- **WHEN** npm informa otro aviso alto o crítico, o sigue informando el aviso permitido una vez vencida la excepción
- **THEN** el control del frontend falla
