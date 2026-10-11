# Registro de diseño histórico: consulta web autenticada

> **Estado: propuesta de PR #47, no activa.** Su modelo BFF/Next.js no está
> seleccionado; la custodia de sesión queda abierta.

La preparación histórica describía autenticación, contexto efectivo, lista y
detalle de turnos dentro de una aplicación Next.js con cookie opaca y sesión
servidor. Se conserva como inventario de preguntas, no como diseño decidido ni
como checklist contractual vigente.

Antes de reconsiderar esta porción, el equipo debe resolver explícitamente la
custodia de sesión, operación y amenazas; determinar contratos propietarios y
permisos aplicables; y publicar OpenAPI aprobado. No inferir alternativas de
sesión, endpoints, DTO, paginación ni cadencia desde este archivo. El backend
mantiene autorización efectiva y aislamiento.

La demo frontend existente es React/Vite y solo integra turnos por HTTP; esta
página no reclama otro código o evidencia. Toda futura aceptación debe registrar
comandos y resultados realmente observados.
