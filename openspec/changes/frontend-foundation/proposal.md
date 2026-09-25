# Propuesta: fundación documental del frontend

## Objetivo

Establecer una arquitectura canónica y límites observables para el futuro frontend web de AgroFlow antes de implementar pantallas, integración HTTP o lógica funcional.

## Estado

Cambio exclusivamente documental de OpenSpec (`skip_specs: true`). Registra decisiones confirmadas, recomendaciones pendientes y bloqueos existentes; no demuestra que el frontend esté implementado ni introduce una delta normativa. Las especificaciones vigentes organizadas por capacidad de negocio continúan siendo la única fuente de comportamiento.

## Alcance

- documentar Next.js, TypeScript y App Router como stack objetivo;
- definir organización por capacidad y límites entre Server y Client Components;
- separar estado remoto de estado efímero de interfaz;
- preservar al backend como fuente operativa de verdad y autoridad de permisos;
- exigir integración contract-first mediante OpenAPI;
- definir criterios para errores, pruebas, configuración, observabilidad, mocks y aislamiento por ingenio;
- registrar el modelo aceptado de sesión web con sus decisiones operativas aún pendientes;
- documentar como principios técnicos que los datos simulados no son estado operativo y que una operación rechazada no puede presentarse como exitosa, sin crear requisitos normativos duplicados.

## Fuera de alcance

- crear o modificar código de frontend o backend;
- importar el prototipo disponible en `docs/contexto/`;
- definir endpoints, DTO, códigos de error o esquemas de seguridad HTTP;
- implementar login, dashboard, turnos, datos maestros u otra capacidad funcional;
- implementar el BFF aceptado o elegir su almacenamiento operativo, proveedor de observabilidad, librería visual o herramienta de pruebas;
- resolver la matriz de roles, indicadores, fecha operativa o intervalo del dashboard;
- cambiar CI, despliegue u OpenAPI.

## Capacidades afectadas

- **Sin nueva delta normativa:** este cambio modifica documentación de arquitectura y ADR; no crea una capacidad tecnológica paralela.
- **Capacidades vigentes relacionadas, sin modificación:** acceso y autorización, plataforma y segregación, monitoreo operativo y aceptación del MVP continúan siendo las propietarias únicas de sus requisitos de comportamiento.
- **Cambios funcionales futuros:** deberán modificar la especificación de la capacidad de negocio correspondiente, no una especificación transversal del frontend.

## Impacto

| Área | Impacto de este cambio |
|---|---|
| Frontend | Solo documentación de arquitectura y futuros límites de implementación. |
| Backend | Ninguno; conserva su autoridad operativa. |
| Persistencia | Ninguno. |
| Contrato API | Ninguno; `docs/contracts/openapi.yaml` no se modifica. |
| Pruebas | Se documenta una estrategia futura; no se agrega runner ni suite. |
| Operación | Ninguna modificación de CI o despliegue. |

## Decisiones y bloqueos

Las decisiones confirmadas y propuestas están resumidas en [`frontend-decisions.md`](../../../docs/architecture/frontend/frontend-decisions.md). En particular, el modelo BFF de Next.js con identificador opaco en cookie `HttpOnly` y sesión del lado servidor está aceptado; su implementación y ciclo operativo siguen pendientes.

Permanecen bloqueantes:

- OD-007 para roles y permisos concretos;
- OD-011 para observabilidad y calidad de servicio productivas;
- OD-012 para indicadores, fecha operativa e intervalo de refresco;
- detalles operativos y contrato de sesión web, además de los controles asociados;
- contratos HTTP de las capacidades futuras;
- las decisiones de negocio que correspondan a cada porción vertical.

## Criterios de aceptación

- existe una ubicación canónica para la arquitectura del frontend;
- cada ADR declara su estado y dependencias pendientes;
- el cambio no contiene una delta spec normativa y remite el comportamiento a las capacidades de negocio vigentes;
- el prototipo queda explícitamente como referencia no importada;
- ninguna recomendación pendiente aparece como comportamiento aprobado;
- el modelo BFF de Next.js con cookie HttpOnly permanece aceptado como dirección arquitectónica, mientras su implementación y operación siguen pendientes;
- el cambio se limita a documentación y ADR.
