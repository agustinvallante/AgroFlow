# ADR-FE-003: Propuesta de organización por capacidad

> **Estado: propuesta evaluable, no convención aceptada.** La estructura actual
> de React/Vite en main se conserva; este documento no requiere reorganizarla.

PR #47 recomendó agrupar código bajo `features/<capacidad>/` para mantener junta
la interfaz de una capacidad. Esa motivación de trazabilidad puede evaluarse
conforme evolucione el frontend, junto con la organización actual en
`domain/`, `application/`, `infrastructure/`, `composition/`, `presentation/`
y `shared/`.

No se exige una migración, nuevas capas, alias ni reglas de importación. Cualquier
cambio estructural debe responder a necesidades observadas, mantenerse acotado
y no duplicar reglas de negocio del backend.
