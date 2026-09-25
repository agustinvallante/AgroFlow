# ADR-FE-006: Estrategia de sesión web

- Estado: confirmada con dependencia
- Decisión: modelo de sesión aceptado
- Alcance: modelo de sesión web y ciclo de vida de la sesión del usuario interno
- Implementación: pendiente; este ADR no afirma que exista un BFF, una sesión web ni contratos HTTP implementados

## Contexto

La especificación vigente exige que el backend emita JWT para usuarios internos y rechace tokens expirados. Todavía no están definidos la forma de los endpoints, los DTO, el ciclo de vida completo de la sesión ni el transporte contractual entre la aplicación web y el backend. Exponer la credencial al JavaScript del navegador o completar estos vacíos desde el prototipo sería inseguro y contrario al flujo contract-first.

## Decisión

Se acepta como modelo concreto de sesión web **una cookie `HttpOnly` con un identificador opaco y una sesión mantenida del lado servidor por el BFF de Next.js**. El navegador almacenará únicamente ese identificador opaco: no recibirá, leerá ni persistirá el JWT emitido por el backend. El BFF almacenará o referenciará del lado servidor el material de autenticación necesario para comunicarse con la API.

El BFF mediará exclusivamente la sesión web. El backend seguirá decidiendo identidad efectiva, roles, pertenencia y acceso al ingenio, autorización y reglas de turnos; el BFF no replicará ni reemplazará esas decisiones.

Esta aceptación fija el modelo, no su diseño operativo final. No habilita implementación hasta que las capacidades de negocio y el contrato HTTP definan los comportamientos pendientes. Tampoco afirma que hoy existan Route Handlers, cookies, almacenamiento de sesión, endpoints o despliegue con runtime servidor, ni selecciona un proveedor de almacenamiento.

## Alternativas consideradas

- **Token administrado por el navegador:** no seleccionado porque amplía la exposición de la credencial al código cliente y exige almacenamiento y renovación seguros en ese entorno.
- **Cookie de sesión emitida directamente por el backend:** no seleccionada como dirección web porque trasladaría al backend el contrato de sesión del navegador.
- **Next.js BFF con cookie HttpOnly e identificador opaco:** seleccionada porque mantiene el material de autenticación fuera del navegador y establece un límite web explícito, con el costo de operar sesión del lado servidor y controles adicionales.

## Consecuencias

- La implementación de Next.js necesitará un runtime servidor compatible con el BFF; una exportación exclusivamente estática no alcanza para esta dirección.
- La UI no leerá ni almacenará el JWT; el navegador conservará solo el identificador opaco de sesión.
- El BFF almacenará o referenciará el material de autenticación del backend únicamente del lado servidor.
- Las mutaciones futuras deberán aplicar la protección CSRF que resulte de la política contractual de cookies y dominios.
- El backend seguirá siendo la autoridad sobre identidad, roles, pertenencia y acceso al ingenio, autorización y reglas de turnos; el BFF no duplicará reglas de negocio.
- Los secretos y credenciales del canal n8n permanecerán separados de la sesión humana.
- La operación deberá resolver cómo se comparte o valida la sesión cuando existan múltiples instancias.

## Dependencias de contrato e implementación

Antes de implementar el modelo aceptado deben congelarse explícitamente:

- forma de los endpoints y esquemas de autenticación, sesión y contexto;
- TTL de la sesión web y su relación con la expiración de las credenciales del backend;
- custodia del JWT del backend dentro del límite servidor del BFF;
- renovación, revocación y cierre de sesión;
- protección CSRF y relación con CORS cuando corresponda;
- atributos de cookie y política de dominio, subdominio, ruta y ambientes;
- estrategia y ciclo de vida del almacenamiento de sesión del lado servidor, sin seleccionar todavía un proveedor;
- comportamiento con múltiples instancias, incluida cualquier necesidad de estado compartido;
- topología de despliegue, runtime servidor, terminación TLS y comunicación entre BFF y backend;
- comportamiento ante fallas del almacenamiento de sesión, del BFF o de la comunicación con el backend;
- errores observables y evidencia de pruebas de seguridad, contrato e aislamiento.

Estas dependencias son parte del trabajo contractual futuro. Este ADR no fija nombres de endpoints, DTO, códigos adicionales, nombres de roles ni detalles de infraestructura.

## Referencias

- [Especificación de acceso y autorización](../../../openspec/specs/acceso-y-autorizacion/spec.md)
- [Cambio documental de autenticación y consultas](../../../openspec/changes/frontend-auth-readonly-slice/proposal.md)
- [ADR-FE-007](ADR-FE-007-roles-autorizacion.md)
