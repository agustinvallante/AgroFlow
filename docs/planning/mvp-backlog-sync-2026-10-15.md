# Actualización del backlog MVP — 2026-10-15

Este corte actualiza las **37 issues backend abiertas, F01–F09 y la épica #46**
sin cambiar alcance de producto, responsables, etiquetas ni estados de Projects.
No crea issues nuevas ni considera las PRs de Dependabot como avance funcional.
Es un inventario de implementación: no sustituye OpenSpec ni aprueba propuestas.

## Base integrada y aportes pendientes

Base comprobada: [`main` en `92a80c1`](https://github.com/agustinvallante/AgroFlow/tree/92a80c1b81d3f456d1ca46224f7e73602bfc99db).
Los estados siguientes son una fotografía, no un monitor automático.

| Aporte | Estado en este corte | Consecuencia para las issues |
|---|---|---|
| #76/#77; B01 #8 | Integrados; B01 cerrada | Usar `AgroFlow.sln` y `AgroFlow.*`. No repetir renombre/retiro de e-commerce ni recuperar Customer/registro público. Preservar detector y continuidad de SQLite anterior. |
| API y frontend de demo | Integrados | Reutilizar appointments/transitions, migraciones/seed y cola HTTP React/Vite. Faltan perfil seguro/segregado, contratos y capacidades del MVP. |
| [PR #47](https://github.com/agustinvallante/AgroFlow/pull/47), `efe8c7c` | Draft, no integrada | Coordinar adopción de entidades, servicio/repositorio de maestros, contexto de identidad y pruebas. `MasterDataController` tiene `[NonController]`: no publica CRUD. Su descripción conserva referencias antiguas; las guías actuales ya marcan Next.js como propuesta supersedida. |
| [PR #79](https://github.com/agustinvallante/AgroFlow/pull/79), `15944f4` | Abierta, no integrada | Ruta objetivo del bot: `chatbot/workflows/Chatbot.json`; Compose/ejemplo en `infra/`. No hacer otra importación ni mantener dos copias operativas. Empaquetado/saludo/conectividad no acreditan E2E de turnos. |
| [PR #80](https://github.com/agustinvallante/AgroFlow/pull/80), `4a89295` | Abierta, nueva review solicitada | Controles de CI y 39 pruebas estáticas de integración pendientes de incorporación. No duplicarlos en B04/B06/B07 ni afirmar que ya rigen main. No sustituyen seguridad runtime ni G01–G12. |
| [PR #88](https://github.com/agustinvallante/AgroFlow/pull/88) | Documento integrado | [Historial WhatsApp](propuesta-persistencia-conversaciones-whatsapp.md) sigue siendo propuesta no aprobada/no implementada. No agrega tablas/endpoints ni se vuelve criterio obligatorio del MVP por este refresco. |

## Decisiones que se conservan

- React/TypeScript/Vite en `frontend/` ([ADR-005](../architecture/decisions/ADR-005-continuidad-react-vite-para-el-mvp.md)); no migración implícita a Next.js, BFF o bibliotecas de estado propuestas.
- SQLite académico con datos ficticios; WAL condicionado a pruebas; PostgreSQL antes del primer cliente real ([ADR-006](../architecture/decisions/ADR-006-sqlite-para-persistencia-del-mvp.md)). PostgreSQL/Redis de Evolution no son la persistencia de AgroFlow ni su respaldo.
- Se conserva Identity. Hoy el arranque separa LocalDemo/SQLite de Identity/SQL Server: B02/B03 deben permitir el perfil MVP con identidad y appointments juntos. El login técnico `/api/auth/login` ya existe, con error genérico y expiración UTC, pero no acredita usuario activo/ingenio/roles del MVP.
- Prioridad, asociación única activa, asignación automática, configuración al arranque, corte/carga y gestión acordada siguen [ADR-004](../architecture/decisions/ADR-004-politicas-operativas-del-mvp.md). No se vuelven a decidir por una implementación propuesta.
- OpenAPI `0.3.0` sigue siendo contrato de demo. Rutas de maestros de títulos antiguos y propuestas de #47 no son API aprobada. La custodia de sesión web, permisos restantes y detalles [OD](open-decisions.md) se resuelven por slice, no por suposición.

## Delta y coordinación por grupo

| Issues | Qué aprovechar | Qué falta para cerrar |
|---|---|---|
| B00 [#7](https://github.com/agustinvallante/AgroFlow/issues/7) | Contrato de demo y ADR ya integrados; propuestas de #47/#88 | Contrato/ejemplos y decisiones del primer corte seguro; luego los siguientes slices. No exigir resolver todo el MVP antes de cualquier avance. |
| B02/B03 [#9](https://github.com/agustinvallante/AgroFlow/issues/9)/[#10](https://github.com/agustinvallante/AgroFlow/issues/10) | Modelo/contexto/seed existentes, Identity y fundación condicional #47 | Migraciones incrementales, dos ingenios y perfil seguro coherente. Probar base limpia y actualización respaldada sin descartar IDs/turnos/historial. |
| B04/B05/B10 #11/#12/#14 | Suite/hosting/errores de appointments, transacciones y login existentes | Errores uniformes, aislamiento HTTP real, política de idempotencia/auditoría y contrato de acceso MVP. No crear otra solución/test project ni rehacer arreglos de #77. |
| B11–B24/B38 #15–#28/#43 | Entidades/seed y servicios/repositorio de #47 si se corrigen/adoptan | Contrato público, migraciones, endpoints, permisos y pruebas de maestros/asociaciones. No cerrar por presencia de `[NonController]` ni reimplementar sin coordinar la reutilización. |
| B25–B29 #29–#33 | Appointments, detalle, estados/cancelación y cupos demo | Seguridad, prioridad/calendario, idempotencia, proyección paginada y pruebas MVP sobre el servicio existente, sin API paralela. |
| B30–B32/B07 #34–#36/#42 | Tres herramientas y empaquetado #79 | Autoridad/remitente/instancia, correlación/replay, errores y pruebas de canal. Memoria, prompt, teléfono del body y acuse del webhook no acreditan identidad/negocio. |
| B33–B37/B39 #37–#41/#44 | Modelo de turnos/ventanas, no las capacidades completas | Interrupciones, indicadores/franjas, outbox y resultados aprobados. Acordar aceptación del proveedor frente a entrega e incertidumbre; no reenvíos ciegos ni promesas de entrega exactamente una vez. |
| F01 #67 | App/cliente/composición React/Vite | Acceso/sesión aprobados, limpieza de caché/peticiones al cambiar usuario y permisos de UX respaldados por API. |
| F02–F05 #68–#71 | Vistas/puertos existentes y fundación backend condicional | Separar entidades mezcladas de demo; CRUD/asociaciones HTTP reales, persistencia, conflictos y aislamiento. |
| F06 #72 | Cola HTTP y protección contra respuestas fuera de orden | Seguridad/DTO/páginas y eliminar detalle por fila del primer listado; el polling ya aprovecha caché. No calcular reglas de negocio en UI. |
| F07/F08 #73/#74 | Panel/componentes visuales | Reemplazar mocks por indicadores/franjas/interrupciones API, incluidos vacío/error/desactualización y efectos persistidos. |
| B06/F09 #13/#75 | Runbook, fixtures y controles #80 cuando se integren | Límites honestos de mocks; evidencia G01–G12, dos recorridos fechados, reinicio y respaldo/restauración. #79 no conecta automáticamente ChatbotView a un historial real. |

### Regresiones al reutilizar #47

Antes de apoyar nuevas operaciones en `EfMasterDataUnitOfWork`, verificar la
corrección de `ListAssociations`: ordena por `CreatedAtUtc`, ignorado por el
modelo EF. Verificar también que `ListActiveTrucksForTransportista` restrinja
el ingenio del camión además del de la asociación. Una asociación inconsistente
no debe revelar una patente ajena. B02/B04/B38 incluyen estas regresiones con
SQLite/repositorio real; no basta una prueba con un doble.

### Dependencias sin ciclos

- B28 provee la máquina de estados a B32; no depende a su vez de que B30–B32 estén terminadas.
- B29 prueba liberación por persistencia/nueva reserva. B39 y F07 consumen después esa capacidad; no bloquean B29 mientras B39 espera sus reservas/liberaciones.
- F06 consume B25–B29; la API coordina DTO/proyección con la UI, pero no necesita que F06 esté terminada para implementarse.
- Un seed válido de B02 permite el primer recorrido sin esperar todo el CRUD de maestros. Eso no reduce la aceptación posterior de G03.
- B07 coordina implementación del workflow/E2E; no debe considerarse ya terminada para que B30–B32 puedan implementar su parte API.

## Por dónde comenzar y cuándo está lista una issue

Comenzar por **B00 #7**, con responsables de B02/B03/F01/F06: cerrar el contrato
del primer recorrido autenticado y sus decisiones bloqueantes. Preparar en
paralelo persistencia/identidad, adopción del delta útil de #47 y pruebas/UI
contra el contrato acordado. #79/#80 pueden seguir su revisión sin que el
equipo reimporte el bot o reescriba sus automatizaciones.

El texto actualizado de cada issue incluye base exacta, aporte condicionado,
delta/archivos y criterios de verificación. **No significa que todas estén
Ready.** Aplicar la [puerta de refinamiento](../development/issue-readiness.md):
operaciones/schemas/errores y decisiones del slice aprobados antes de delegar
su implementación. Los placeholders y nombres previstos no son contrato.

La [épica #46](https://github.com/agustinvallante/AgroFlow/issues/46) permanece
abierta. CI verde, una PR parcial o un documento no prueban la
[aceptación del MVP](../product/mvp-acceptance.md). Historial conversacional,
reportes/mapa, configuración editable, infraestructura productiva o cambio de
framework requieren su propio cambio aprobado si se quieren incorporar.
