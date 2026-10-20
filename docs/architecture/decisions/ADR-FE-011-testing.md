# ADR-FE-011: Ideas de evidencia frontend

> **Estado: orientación evaluable; no fija herramientas ni nuevos criterios de
> aceptación.** La aceptación normativa está en OpenSpec y sus guías vigentes.

La propuesta de PR #47 enumeraba pruebas unitarias, de componentes, contrato,
integración y end-to-end. Esas capas pueden considerarse según riesgo y
capacidad, pero no constituyen una arquitectura de pruebas aprobada ni
seleccionan librerías, runner o CI.

Usar los comandos y herramientas existentes del frontend. Los dobles pueden
servir a pruebas acotadas, pero no sustituyen evidencia integrada requerida
para un recorrido real del MVP. Vincular cada afirmación de cobertura con un
resultado observado; no declarar una capacidad operativa por la presencia de
una suite.
