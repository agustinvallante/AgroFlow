# ADR-FE-008: Integración desde contratos aprobados

> **Estado: principio de trabajo, no especificación de arquitectura frontend.**
> La forma activa del contrato se limita al alcance descrito en OpenAPI.

La recomendación histórica de PR #47 de derivar adaptadores de contratos es
compatible con la jerarquía documental del proyecto: OpenSpec define
comportamiento y OpenAPI describe la forma HTTP aprobada. Se conserva como
orientación para futuras integraciones, sin fijar generador, tipos, herramienta
o convención de dependencias.

OpenAPI activo publica salud y appointments de demo, no endpoints de maestros,
autenticación/sesión, asociaciones o paginación. No inferir esas rutas, DTO,
seguridad o errores a partir de propuestas. Todo cambio observable necesita
acuerdo de la capacidad OpenSpec propietaria y actualización coordinada de
contrato, pruebas e implementación.
