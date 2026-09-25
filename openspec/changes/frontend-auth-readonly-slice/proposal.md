# Propuesta: preparación de autenticación y consultas de turnos

## Objetivo

Preparar, sin implementar, la primera porción vertical de solo lectura del frontend para:

- CU-001, autenticación interna;
- resolución del usuario actual y su contexto de ingenio confirmado por el backend;
- CU-004, listado de turnos del ingenio con los filtros ya previstos por la especificación vigente;
- CU-005, consulta del detalle vigente de un turno.

La porción seguirá el modelo de sesión aceptado: el navegador conservará solo un identificador opaco en una cookie `HttpOnly`, mientras el BFF de Next.js almacenará o referenciará del lado servidor la autenticación emitida por el backend. Esta propuesta no afirma que existan el BFF, la cookie, el almacenamiento, los endpoints, el contrato HTTP, las pantallas ni el backend necesario.

## Estado y naturaleza del cambio

Este es un cambio exclusivamente documental y preparatorio. Usa `skip_specs: true` porque todavía no hay contratos suficientes para redactar deltas normativas finales sin inventar decisiones.

Las deltas normativas futuras pertenecen a las capacidades de negocio existentes:

- [`acceso-y-autorizacion`](../../specs/acceso-y-autorizacion/spec.md) para autenticación, sesión y contexto autenticado del usuario y su ingenio;
- [`turnos-consulta`](../../specs/turnos-consulta/spec.md) para listado, filtros y detalle de turnos.

No se creará una especificación normativa propiedad de la tecnología frontend. Las garantías transversales vigentes de [`plataforma-y-segregacion`](../../specs/plataforma-y-segregacion/spec.md) siguen aplicando, sin recibir una delta en este cambio.

## Motivación

La arquitectura frontend ya registra el modelo de sesión aceptado. OpenAPI publica rutas de datos maestros y recibos de mutación en contratos separados, pero no publica rutas de autenticación/sesión/contexto ni de listado/detalle de turnos para esta porción. Las decisiones operativas y contractuales de sesión continúan abiertas. Separar la aceptación del modelo del congelamiento contractual evita que el frontend o el BFF inventen nombres de operaciones, estructuras, permisos o errores.

Esta preparación hace visibles las dependencias que deben resolverse en conjunto antes de modificar contratos o código.

## Alcance

- registrar como modelo aceptado el identificador opaco en cookie `HttpOnly` y la sesión del lado servidor en el BFF de Next.js;
- delimitar la porción de CU-001, CU-004 y CU-005;
- describir el flujo conceptual de autenticación, contexto actual, listado, filtros y detalle sin fijar formas HTTP;
- asignar la futura propiedad normativa a `acceso-y-autorizacion` y `turnos-consulta`;
- definir una lista de congelamiento contractual previa a cualquier implementación;
- identificar impactos futuros en API, backend, frontend, persistencia y pruebas.

## Fuera de alcance

- crear código de frontend, backend o BFF;
- crear pantallas, Route Handlers, adaptadores, cookies o almacenamiento de sesión;
- modificar especificaciones vigentes o `docs/contracts/openapi.yaml`; las rutas de datos maestros existentes son independientes y no cubren esta porción;
- definir nombres o métodos de endpoints, DTO, parámetros concretos o esquemas de seguridad;
- definir nuevos códigos o correspondencias de estado y error;
- cerrar nombres de roles o su matriz de permisos;
- fijar intervalos de polling;
- decidir TTL, custodia del JWT, atributos completos de cookie y dominios, renovación, revocación, cierre, protección CSRF, proveedor o diseño de almacenamiento, topología multi-instancia, despliegue o comportamiento ante fallas;
- importar o modificar el prototipo.

## Porción vertical preparada

La futura porción deberá permitir que un usuario interno se autentique (CU-001), obtenga del backend su identidad y contexto efectivo de ingenio, consulte el listado aislado de ese ingenio con los filtros normativos (CU-004) y abra el detalle vigente de un turno autorizado (CU-005). Todas las consultas deben preservar la inmutabilidad definida por `turnos-consulta`. El BFF mediará únicamente la sesión; el backend conservará la autoridad sobre identidad, roles, pertenencia y acceso al ingenio, autorización, reglas de turnos y estado operativo.

Esta descripción expresa el límite funcional existente, no una forma de API. La implementación queda bloqueada hasta completar la lista de congelamiento de [`design.md`](design.md).

## Impacto futuro esperado

| Área | Preparación requerida antes de implementar | Impacto de este cambio |
|---|---|---|
| API y contrato | Acordar operaciones, formas de solicitud y respuesta, seguridad y errores; luego publicar un cambio OpenAPI separado y revisable. | Ninguno; OpenAPI no se modifica. |
| Backend | Exponer mediante contratos aprobados la autenticación, el contexto efectivo y las consultas aisladas, preservando las reglas vigentes. | Ninguno; no se modifica código. |
| Frontend/BFF | Implementar el identificador opaco y la sesión del lado servidor, los adaptadores contract-first y los estados de interfaz después del congelamiento, sin trasladar autorización ni reglas de turnos al BFF. | Solo documentación arquitectónica. |
| Persistencia | Definir el almacenamiento servidor de sesión y su ciclo de vida sin seleccionar todavía un proveedor; confirmar que las consultas de turnos no escriban datos. | Ninguno; no se modifica el modelo. |
| Pruebas | Acordar evidencia de contrato, seguridad, aislamiento, inmutabilidad y experiencia antes de implementar. | Se documenta la evidencia requerida; no se agrega suite. |
| Despliegue | Definir runtime servidor, dominios, TLS, comunicación con backend y comportamiento multi-instancia. | Ninguno; no se modifica infraestructura. |

## Dependencias explícitas

La porción no puede pasar a contrato o implementación hasta acordar:

- forma de las operaciones de autenticación, sesión, contexto, listado y detalle;
- TTL de la sesión y su relación con la expiración de las credenciales del backend;
- custodia del JWT exclusivamente del lado servidor, renovación, revocación y cierre de sesión;
- protección CSRF y atributos y dominios de cookie por ambiente;
- almacenamiento de sesión del lado servidor, sin seleccionar proveedor, y comportamiento con múltiples instancias;
- despliegue del BFF, su comunicación con el backend y el comportamiento ante fallas;
- matriz de roles y permisos pendiente, cuya aplicación seguirá a cargo del backend;
- definición de fecha operativa, zona horaria y límites temporales aplicables;
- semántica completa de errores sin filtración de datos;
- evidencia de pruebas exigida para aceptar el contrato y la implementación.

## Criterios de aceptación de esta preparación

- ADR-FE-006 y los documentos canónicos marcan el identificador opaco en cookie `HttpOnly` y la sesión del lado servidor en el BFF como modelo aceptado, no como código existente;
- CU-001, CU-004 y CU-005 quedan trazados a sus capacidades de negocio existentes;
- no se introduce una especificación tecnológica del frontend;
- ninguna forma de endpoint, DTO, mapeo final de errores, rol, intervalo u operación OpenAPI se inventa;
- la lista de congelamiento distingue decisiones pendientes de comportamiento vigente;
- el cambio permanece exclusivamente documental.
