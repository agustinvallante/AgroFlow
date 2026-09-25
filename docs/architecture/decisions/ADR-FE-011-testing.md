# ADR-FE-011: Estrategia de pruebas del frontend

- Estado: aceptada para la arquitectura objetivo
- Alcance: niveles de prueba y evidencia
- Implementación: pendiente; no existe una suite afirmada por este ADR

## Contexto

La interfaz deberá verificar no solo componentes, sino también su alineación con OpenAPI, el manejo de rechazos y los recorridos que atraviesan el backend real. Basarse únicamente en snapshots o mocks permitiría declarar funcional una experiencia desconectada de la fuente operativa de verdad.

## Decisión

La estrategia SHALL combinar:

1. pruebas unitarias de mapeos, validación estructural y estado de UI;
2. pruebas de componentes para carga, vacío, error, interacción y accesibilidad;
3. pruebas contractuales de adaptadores contra OpenAPI;
4. pruebas de integración por capacidad;
5. recorridos end-to-end con backend real para flujos aprobados y aislamiento por ingenio.

Los dobles estarán delimitados y no sustituirán la evidencia integrada exigida por la aceptación del MVP.

## Alternativas consideradas

- **Solo pruebas end-to-end:** alta confianza por recorrido, pero diagnóstico lento y cobertura costosa.
- **Solo pruebas unitarias y de componentes:** rápidas, pero no prueban contrato ni persistencia real.
- **Pirámide por riesgos:** combina feedback rápido con evidencia contractual e integrada.

## Consecuencias

- Cada capacidad deberá vincular escenarios OpenSpec con evidencia adecuada.
- Los adaptadores cubrirán respuestas válidas, vacías y erróneas.
- Las pruebas de permisos incluirán rechazo del backend, no solo controles ocultos.
- La selección de herramientas y comandos se hará al crear la base ejecutable, sin agregarlas en este cambio.

## Dependencias no resueltas

- runner y bibliotecas concretas;
- ambiente reproducible para integración y E2E;
- primeros endpoints y datos de prueba contractuales;
- integración futura con CI.

## Referencias

- [Aceptación del MVP](../../../openspec/specs/aceptacion-del-mvp/spec.md)
- [ADR-FE-008](ADR-FE-008-openapi-contract-first.md)
