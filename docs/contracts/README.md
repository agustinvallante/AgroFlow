# Contratos compartidos

Este directorio describe cómo se comunican frontend, backend e integraciones. Los contratos expresan técnicamente el comportamiento definido por OpenSpec; no agregan reglas de negocio por cuenta propia.

## Artefactos

- `openapi.yaml`: contrato HTTP de la API. Por ahora contiene un esqueleto válido sin endpoints (`paths: {}`); los paths se incorporarán mediante cambios OpenSpec aprobados.
- futuros esquemas de eventos o webhooks: deben documentar versión, autenticación, correlación, idempotencia y errores.

Las issues `B10`–`B39` del [Project Backend](https://github.com/users/agustinvallante/projects/2) nombran rutas **propuestas** para repartir el trabajo, no endpoints vigentes. La issue `B00` consolida las decisiones necesarias y el contrato inicial. Si el diseño aprobado cambia un path, método, campo, permiso o código de respuesta, el equipo actualiza OpenAPI y las issues/consumidores afectados; no se conserva un nombre de issue por inercia.

## Reglas

1. No implementar un endpoint nuevo antes de acordar su comportamiento observable.
2. Mantener nombres, tipos, códigos HTTP y errores coherentes entre contrato y código.
3. Tratar todo cambio incompatible como una decisión explícita de versión o migración.
4. No incluir ejemplos con secretos ni datos personales reales.
5. Incluir en cada operación documentada el esquema de solicitud/respuesta, validaciones observables, autorización, contexto de ingenio, errores relevantes y conducta de reintentos cuando aplique.
