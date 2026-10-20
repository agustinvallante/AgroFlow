# ADR-FE-005: Propuesta histórica de Zustand

> **Estado: no adoptada; librería pendiente de evaluación.** No agregar Zustand
> ni reemplazar el estado existente basándose en esta recomendación.

PR #47 propuso Zustand para estado visual efímero y excluir de stores locales
las entidades operativas, permisos y credenciales. El límite de no duplicar la
fuente operativa del backend sigue siendo un principio útil; la elección de
librería no está aprobada.

Evaluar cualquier necesidad de estado compartido frente a React y los patrones
presentes en la aplicación. La custodia de identidad/sesión y datos operativos
no se decide en este documento.
