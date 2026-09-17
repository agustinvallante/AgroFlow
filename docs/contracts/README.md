# Contratos compartidos

Este directorio describe cómo se comunican frontend, backend e integraciones. Los contratos expresan técnicamente el comportamiento definido por OpenSpec; no agregan reglas de negocio por cuenta propia.

## Artefactos

- `openapi.yaml`: contrato HTTP de la API. Por ahora contiene un esqueleto válido sin endpoints; los paths se incorporarán mediante cambios OpenSpec aprobados.
- futuros esquemas de eventos o webhooks: deben documentar versión, autenticación, correlación, idempotencia y errores.

## Reglas

1. No implementar un endpoint nuevo antes de acordar su comportamiento observable.
2. Mantener nombres, tipos, códigos HTTP y errores coherentes entre contrato y código.
3. Tratar todo cambio incompatible como una decisión explícita de versión o migración.
4. No incluir ejemplos con secretos ni datos personales reales.
