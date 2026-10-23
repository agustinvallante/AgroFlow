# Automatización de GitHub

## Acceso del equipo

AgroFlow utiliza un repositorio y dos GitHub Projects personales:

- [AgroFlow — Frontend](https://github.com/users/agustinvallante/projects/1);
- [AgroFlow — Backend](https://github.com/users/agustinvallante/projects/2).

Una persona que desarrolla y administra tareas necesita permiso **Write** tanto en el repositorio como en cada Project. Son permisos independientes. El rol Write permite trabajar con código, issues, pull requests e items; el rol Admin se reserva para quienes deban modificar accesos, campos o automatizaciones.

## Etiquetas de área

| Etiqueta | Uso |
|---|---|
| `area:backend` | API, persistencia y documentación exclusiva del backend. |
| `area:frontend` | Aplicación web y documentación exclusiva del frontend. |
| `area:shared` | OpenSpec, contratos, arquitectura e integraciones transversales. |
| `area:docs-devops` | Documentación de equipo, CI/CD y operación. |
| `status:blocked` | Trabajo detenido por una decisión o dependencia concreta. |

El workflow `Issue triage` traduce el campo Área de las plantillas a una etiqueta. El workflow `Pull request labels` aplica etiquetas según las rutas modificadas.

## Automatización de los Projects

Los dos tableros usan `Backlog`, `To do`, `In progress`, `In review` y `Done`.

- todo item nuevo comienza en `Backlog`;
- un issue reabierto vuelve a `To do`;
- un issue o pull request cerrado pasa a `Done`;
- un pull request fusionado pasa a `Done`;
- un pull request vinculado coloca el item en `In review`;
- cambios solicitados devuelven el item a `In progress`;
- una aprobación lo devuelve a `In review` hasta su integración;
- las subtareas se agregan al mismo Project que su tarea principal.

Los issues con `area:backend` se agregan al Project Backend y los que tienen `area:frontend` al Project Frontend. Los issues `area:shared` o `area:docs-devops` pueden aparecer en ambos. Los pull requests no se autoagregan como items independientes. Usar `Closes #<número>` solo cuando se cumplen todos los criterios de la issue; los incrementos parciales usan `Refs #<número>`. Cerrar una PR rechazada no demuestra que una issue ni una capacidad estén terminadas. Los workflows nativos de Projects se administran en cada tablero, no en CI; este cambio no altera sus permisos ni estados.

`To do`, `In progress` y `status:blocked` expresan decisiones del equipo, no inferencias automáticas. Una tarea solo pasa a `To do` cuando fue refinada y priorizada; pasa a `In progress` cuando alguien la toma.

## Verificaciones de pull requests

El workflow `CI` ejecuta cinco controles coordinados:

1. valida OpenSpec, OpenAPI y enlaces Markdown locales;
2. restaura, compila y prueba obligatoriamente `backend/AgroFlow.sln` y su proyecto de tests;
3. instala con `npm ci`, audita el lockfile y ejecuta typecheck, lint, tests y build del frontend; los scripts ya no se omiten con `--if-present`;
4. prueba las reglas de validación de integraciones y valida el export, ejemplo de entorno y Compose del bot integrado;
5. publica `Required checks`, que exige el éxito de los cuatro jobs anteriores, también el de integraciones.

El ruleset activo de `main` exige `Required checks`, `Reject vulnerable dependency changes`, una aprobación, conversaciones resueltas y base actualizada. No se usa bypass para integrar cambios. Los checkouts CI no conservan credenciales Git; el etiquetado con `pull_request_target` no ejecuta código de la rama propuesta.

La mera existencia de proyectos, de un job o de una CI verde no demuestra cobertura funcional del MVP: se evalúan los escenarios realmente ejecutados. La integración frontend vigente es React/Vite; los flujos todavía mock no cuentan como recorridos reales del MVP.

`Dependency review` bloquea nuevas dependencias vulnerables altas/críticas. Además, `npm audit --audit-level=high` comprueba todas las dependencias frontend, incluidas las transitivas de desarrollo: una vulnerabilidad heredada también impide CI verde. La auditoría programada revisa el lockfile cada lunes a las 09:00 de Argentina, sin instalar, cambiar archivos, abrir issues ni hacer merges. Los schedules de GitHub pueden ejecutarse con demora; el control por PR también sigue activo. Dependabot alerts, security updates y secret scanning/push protection permanecen habilitados.

## Integraciones y privacidad

El bot de la PR #79 ya está integrado en `main`. El job comprueba sus archivos reales: `chatbot/workflows/Chatbot.json`, `chatbot/README.md`, `infra/compose.yaml` e `infra/env.example`. Una incorporación parcial falla; si la base ya tenía el componente, tampoco se permite que desaparezca silenciosamente. Los tests conservan cobertura para estados previos sin bot, pero ese caso ya no describe el candidato actual.

La evidencia del candidato posterior a #79 debe mostrar tanto la validación del export y entorno existentes como la ejecución de Compose con el ejemplo de entorno. Que pasen solamente los tests del validador no acredita esos dos pasos; tampoco corresponde presentar el mensaje de componente todavía ausente como una validación del bot integrado.

- Export inactivo, sin `pinData`, metadatos de instancia ni IDs/valores de credenciales; referencias de credenciales genéricas por nombre.
- Nombres/IDs de nodo únicos, conexiones y referencias de expresión válidas; destinos HTTP explícitamente permitidos y sin proxies declarados; peticiones de negocio contra la API configurada, sin UUID de turno fijo.
- Ejemplo de entorno con placeholders y versiones fijadas; Compose se verifica con `config --quiet`, nunca con el `.env` privado.
- Los archivos privados `.env`/`.env.*` no pueden estar versionados; sólo se admiten los ejemplos. La protección se ejecuta también antes de incorporar el bot.
- El validador detecta algunos patrones de claves/cabeceras y no imprime sus valores. Es un control heurístico, no una certificación de ausencia de secretos/datos personales; sigue siendo necesaria revisión humana.

### Límites verificables del validador

Se revisan `n8n-nodes-base.httpRequest`, `n8n-nodes-base.httpRequestTool` y el tipo anterior `@n8n/n8n-nodes-langchain.toolHttpRequest`, incluso si están desconectados o deshabilitados. Una variante de esos nombres en otro paquete falla hasta incorporar una política y pruebas explícitas; no se admite como excepción por su nombre visible en el editor.

Los destinos declarados permitidos son:

- **API de AgroFlow:** los tres tipos admiten `={{ $env.AGROFLOW_API_URL }}/api/v1/appointments`, con el ID dinámico opcional y el sufijo `/transitions` cuando corresponda.
- **Respuesta de WhatsApp:** sólo el nodo ordinario `httpRequest`, con método `POST`, admite `={{ $env.EVOLUTION_INTERNAL_URL }}/message/sendText/{{ $env.EVOLUTION_INSTANCE }}`. Compose configura esa base como `http://evolution:8080`; no se autorizan otras operaciones de Evolution ni herramientas del agente contra ese servicio.

La base debe ocupar el inicio de la plantilla: no basta mencionar esa variable o la ruta en otra URL. No se evalúa JavaScript; concatenaciones, transformaciones de la base y otros formatos requieren adaptar expresamente el control y sus regresiones. Los filtros se pasan por `queryParameters` (o `parametersQuery` en el tipo anterior), no como una URL arbitraria.

Las opciones HTTP deben ser un objeto estático. `options.proxy` y `parameters.proxy` sólo pueden estar ausentes o ser la cadena vacía literal: valores externos, expresiones y objetos fallan sin imprimir el valor. No se admite `options.pagination`, que podría introducir un segundo destino; agregar paginación requiere definir y probar su política. Se conservan las opciones de respuesta del export actual y el timeout. Estas comprobaciones no certifican destinos efectivos en runtime: no inspeccionan valores privados de entorno, proxies globales, DNS, redirecciones de servidores ni código arbitrario de otros tipos de nodo. La configuración desplegada y sus credenciales siguen requiriendo revisión.

La búsqueda de UUID de turno fijos cubre URL y campos de body/query de los tres tipos HTTP, incluidos `parametersBody` y `parametersQuery` del tipo anterior: parámetros por nombre, objetos, JSON y plantillas JSON de n8n. Se reconocen `appointmentId`, `appointment_id`, `turnoId`, `id_turno` y variantes de guion/capitalización; `id` se considera turno en la raíz de la petición, como parámetro por nombre o dentro de `appointment`/`turno`. Los UUID de otras entidades (`carrierId`, `truckId`, `farmId`, o `truck.id`) no se prohíben globalmente. Es análisis estático de las formas soportadas, no ejecución de expresiones ni una garantía de identidad o seguridad runtime.

La detección de eliminación completa compara contra el SHA base del pull request o `before` del push. `workflow_dispatch` no proporciona ninguno: en una ejecución manual sin base se emite un aviso `Historical integration removal check NOT RUN`; no debe presentarse como comparación histórica superada. El resto de controles sí se ejecuta. Una base inválida o no disponible falla en lugar de omitirse silenciosamente. También puede comprobarse localmente una base explícita, usando un **SHA completo de commit disponible en el clon**:

```powershell
$env:INTEGRATION_BASE_REF = '<SHA completo del commit base>'
node .github/scripts/validate-integrations.mjs
Remove-Item Env:INTEGRATION_BASE_REF
```

Las regresiones usan repositorios Git temporales y datos inventados: comprueban la eliminación respecto de una base real, la validación de referencias y el aviso de comparación omitida, además de las rutas y los IDs. No acceden a servicios ni datos del equipo.

CI no inicia n8n/Evolution, no vincula WhatsApp, no utiliza credenciales reales ni ejecuta llamadas pagas. Estos controles no prueban compatibilidad runtime del agente/modelo, autenticación del remitente, E2E de turnos ni aceptación G01–G12. Esos recorridos siguen en B07 y sus dependencias. `chatbot/**` recibe `area:shared`; `infra/**`, `area:docs-devops`. Las plantillas admiten Integración y los títulos manuales `[Backend]`, `[Frontend]` o `[Integración]` también pueden etiquetarse.

## Política de mantenimiento de dependencias

- **Mensual, no actualización continua:** GitHub Actions, NuGet en los proyectos actuales `AgroFlow.*` y npm en `/frontend` generan lotes de minor/patch, con un máximo de dos PRs rutinarias por ecosistema. El lote .NET abarca proyectos juntos para evitar cambios aislados de EF/proveedores/Logging.
- **Versiones principales estables:** Dependabot ignora propuestas rutinarias major. No migra .NET, EF, Identity, React, Vite o TypeScript de generación por sorpresa. Un salto major requiere decisión explícita, notas de incompatibilidad y PR separado; también para Actions se revisa el runtime del runner.
- **Sin auto-merge:** cada actualización necesita revisión y CI del candidato sobre main actual. Minor/patch tampoco garantiza compatibilidad: revisar diferencias, restauración, build, tests, migraciones y contratos afectados.
- **Seguridad prioritaria:** las alertas y reparaciones urgentes no esperan la ventana mensual. Si un filtro de versión impide una reparación necesaria, crear una excepción manual revisada; no conservar una versión vulnerable o sin soporte por aplicar esta política mecánicamente.
- **Imágenes de n8n/Evolution/PostgreSQL/Redis:** conservar las versiones de `infra/env.example`, la clave de cifrado y los volúmenes. No se habilitan bumps automáticos de imágenes por variables ni actualizaciones a `latest`; antes de cambiar, comprobar importación, credenciales, almacenamiento y rollback. No se afirma que Dependabot resuelva esas variables: su mantenimiento sigue siendo explícito y coordinado con B07.

Una PR sobre rutas `Dsw2025Tpi.*` retiradas se rehace sobre los proyectos actuales; CI verde de una base anterior no habilita su merge. Cerrar una propuesta obsoleta no significa que su parche sea incompatible ni que Identity deje de usarse. Los parches EF se revisan agrupados y no se silencian errores NU1605/CS1705 para aceptar combinaciones incoherentes.

La corrección de seguridad de la review de #80 (2026-10-22) eleva los mínimos a Vite 8.2.4 y Vitest 5.0.3, actualizando también el lockfile y las dependencias transitivas afectadas. Son parches dentro de las generaciones existentes, no migraciones de plataforma. La evidencia anterior de una auditoría sin alertas no es permanente: se vuelve a ejecutar la auditoría y el conjunto de verificaciones sobre cada candidato. Referencias: [Vite/launch-editor](https://github.com/vitejs/launch-editor/security/advisories/GHSA-587h-7wmj-h5pm), [Vitest/mocker](https://github.com/vitest-dev/vitest/security/advisories/GHSA-qxg6-89mg-rqv8).

Comprobaciones locales de automatización, sin secretos ni servicios:

```text
node --test .github/scripts/tests/integrations.test.mjs
node .github/scripts/validate-integrations.mjs
node .github/scripts/check-local-markdown-links.mjs
```

Para el bot integrado, ejecutar también `docker compose --env-file infra/env.example -f infra/compose.yaml config --quiet`. No se exige una nueva capacidad OpenSpec por este cambio de tooling: no modifica reglas ni contrato de producto.

## Entrega y despliegue

Cada push válido a `main` publica durante 14 días artefactos compilados del backend y `frontend/dist`. Esto constituye **entrega continua de artefactos**, no despliegue. La continuidad de React/Vite está aprobada por ADR-005; no se afirma una arquitectura BFF de Next.js ni se deriva una topología de despliegue de las propuestas históricas de PR #47.

El despliegue automático se agregará después de aprobar proveedor, ambiente, dominio, base de datos, secretos y estrategia de migraciones. No se debe crear un workflow de despliegue vacío ni almacenar credenciales en el repositorio.
