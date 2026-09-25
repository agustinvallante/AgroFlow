# ADR-FE-009: Manejo consistente de errores

- Estado: aceptada en sus principios; contrato concreto pendiente
- Alcance: clasificación, presentación y recuperación de errores
- Implementación: pendiente

## Contexto

Una interfaz operativa necesita presentar fallas de forma consistente sin atribuirles una semántica HTTP que el contrato todavía no define. Sin un contrato común, cada vista podría interpretar respuestas distintas, ocultar fallas o presentar como exitosa una operación no confirmada.

## Decisión

El frontend SHALL transformar los errores publicados por OpenAPI a una clasificación estable de experiencia, preservando la causa accionable permitida y sin exponer información sensible.

La única correspondencia HTTP vigente es la definida por la especificación de acceso y autorización: un token expirado en un recurso protegido recibe `401 Unauthorized`. Esta regla no asigna `401` a otros casos de autenticación. Toda otra semántica de estados, esquemas y recuperación queda pendiente del contrato OpenAPI.

Una falla o un rechazo nunca producirán confirmación local ni se reinterpretarán como éxito. La recuperación y la presentación contextual seguirán exclusivamente el contrato aprobado de cada capacidad.

## Alternativas consideradas

- **Mostrar mensajes crudos de la API:** conserva detalle, pero puede filtrar información y producir una experiencia inconsistente.
- **Mensaje genérico para todo:** evita filtraciones, pero impide corrección y soporte efectivos.
- **Clasificación contractual y presentación contextual:** separa transporte de experiencia sin inventar resultados.

## Consecuencias

- Habrá un adaptador central de errores y mensajes contextuales por capacidad.
- La interfaz deberá contemplar carga, vacío, error recuperable y error terminal.
- La telemetría excluirá cuerpos sensibles, tokens y cookies.
- Las pruebas cubrirán los casos y las semánticas de error cuando el contrato de cada capacidad los defina.

## Dependencias no resueltas

- esquema normativo de errores en OpenAPI;
- política de identificadores de correlación y soporte;
- estrategia de sesión para recuperación ante autenticación vencida;
- textos de producto y requisitos de accesibilidad específicos.

## Referencias

- [Acceso y autorización](../../../openspec/specs/acceso-y-autorizacion/spec.md)
- [Plataforma y segregación](../../../openspec/specs/plataforma-y-segregacion/spec.md)
- [ADR-FE-006](ADR-FE-006-auth-session.md)
