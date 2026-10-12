# ADR-FE-009: Propuesta de presentación de errores

> **Estado: orientación no normativa.** Las semánticas observables pertenecen a
> OpenSpec y al contrato aprobado de cada operación.

La idea de presentar errores de manera accionable y sin información sensible
puede evaluarse en cada integración React/Vite. La clasificación, los mensajes,
los estados HTTP y las estrategias de recuperación no se fijan aquí ni deben
inferirse de la antigua propuesta de Next.js.

El backend es autoridad sobre el resultado. Una solicitud rechazada o fallida
no se presenta como exitosa; la UI no debe inventar correspondencias entre
códigos y comportamiento de producto. Los errores contractuales de la demo
siguen definidos en `docs/contracts/openapi.yaml`.
