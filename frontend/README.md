# Frontend de AgroFlow

Espacio reservado para integrar y evolucionar el prototipo [AgroFlow-Dashboard](https://github.com/GabrielBurieque/AgroFlow-Dashboard).

El prototipo se mantiene por ahora como referencia externa. Su incorporación se hará cuando estén definidos los contratos de la API y la estrategia de integración, evitando duplicar datos mock que luego deban descartarse.

Antes de implementar una pantalla o flujo, consultar:

- [especificaciones vigentes](../openspec/specs/);
- [documentación específica del frontend](../docs/frontend/README.md);
- [contratos compartidos](../docs/contracts/README.md);
- [catálogo de casos de uso](../docs/product/use-case-catalog.md).

El frontend no debe recrear reglas de asignación, prioridad o transiciones de estado. Esas decisiones pertenecen al backend y deben consumirse mediante contratos explícitos.
