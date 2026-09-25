# Tareas: baseline funcional del MVP

## 1. Aprobación y consolidación documental

- [ ] 1.1 Revisar las deltas OD-001 a OD-013 con responsables de producto, operación, backend, frontend e integración; verificar que cada regla tenga una única capacidad propietaria.
- [ ] 1.2 Tras la aprobación, archivar el cambio para consolidar las specs vigentes y actualizar `docs/planning/open-decisions.md` con enlaces a las decisiones resultantes.
- [ ] 1.3 Registrar en ADR cualquier decisión técnica que deba conservarse, sin trasladar detalles de proveedor a requisitos funcionales.

## 2. Contrato API [Backend/Contrato]

- [ ] 2.1 Publicar en `docs/contracts/openapi.yaml` las operaciones de sesión, datos maestros, turnos, interrupciones, monitoreo, auditoría e integración necesarias para el baseline; validar el contrato.
- [ ] 2.2 Definir entradas mínimas, permisos efectivos, preferencia/asignación, versiones, idempotencia, correlación, conflictos y errores sin información sensible; verificar trazabilidad contra cada delta.
- [ ] 2.3 Definir operaciones acotadas para la identidad de servicio n8n y estados de entrega; verificar que no exista una credencial compartida con el navegador.

## 3. Plataforma y acceso [Backend/Persistencia]

- [ ] 3.1 Implementar los cuatro roles, ingenio efectivo y autorización backend; probar la matriz completa con casos permitidos, denegados y acceso cruzado.
- [ ] 3.2 Implementar idempotencia durable, transacciones, restricciones y control optimista de versión; probar replay idéntico, clave reutilizada y carreras concurrentes.
- [ ] 3.3 Implementar auditoría append-only y consulta aislada de solo lectura; probar actor, servicio, resultado, correlación, permisos y ausencia de secretos.
- [ ] 3.4 Documentar y automatizar el perfil académico de instalación, salud, logs, reinicio, backup y restore; ejecutar una recuperación sobre datos controlados.

## 4. Datos maestros y capacidad [Backend/Persistencia]

- [ ] 4.1 Implementar transportistas, camiones, fincas y asociaciones muchos a muchos con los campos, unicidades, permisos y estados aprobados; probar inhabilitación, reactivación e historia.
- [ ] 4.2 Implementar calendarios explícitos, ventanas de 30 minutos, capacidad positiva y fecha operativa en `America/Argentina/Tucuman`; probar límites `[inicio, fin)`, agotamiento y liberación.
- [ ] 4.3 Verificar que ninguna operación de datos maestros incorpore borrado físico con historia ni geodatos fuera de alcance.

## 5. Turnos e interrupciones [Backend]

- [ ] 5.1 Implementar solicitud mínima, asociación activa, prioridad lexicográfica y preferencia opcional con fallback; probar cada criterio y desempate.
- [ ] 5.2 Implementar asignación atómica, exclusión de turno activo y respuesta ante falta de capacidad o conflicto; probar solicitudes simultáneas.
- [ ] 5.3 Implementar permisos de ciclo de vida, cancelación y liberación de capacidad; probar matriz de roles, estados inválidos e idempotencia.
- [ ] 5.4 Implementar interrupciones y reprogramación con la misma política de prioridad; probar tratamiento por estado, aislamiento y fallas de persistencia.

## 6. WhatsApp, n8n y notificaciones [Integración/Backend]

- [ ] 6.1 Implementar autenticidad del canal y credencial de servicio acotada; probar firma o evidencia equivalente inválida, credencial ausente, scope insuficiente y remitente desconocido.
- [ ] 6.2 Implementar deduplicación persistida de mensajes y conservación de correlación/idempotencia; probar replay, carga distinta y recuperación tras reinicio.
- [ ] 6.3 Implementar outbox, estados de entrega, tres intentos y reintento manual autorizado con un adaptador reproducible; probar éxito, callback repetido y falla sin rollback operativo.

## 7. Monitoreo y consumidores [Backend/Frontend]

- [ ] 7.1 Implementar el agregado diario autoritativo con todos los conteos, zona, instante e interrupción activa; verificarlo contra un dataset conocido y otro ingenio.
- [ ] 7.2 Conectar la interfaz a permisos, estados y conteos del backend, con refresco cada 30 segundos mientras esté visible y refresco manual; comprobar que no calcule reglas localmente.
- [ ] 7.3 Conectar los flujos n8n al contrato real y comprobar que nunca confirmen una operación rechazada o no persistida.

## 8. Aceptación [Todas las áreas]

- [ ] 8.1 Mantener una matriz de trazabilidad entre requisitos, escenarios, contrato y evidencia automatizada o manual reproducible.
- [ ] 8.2 Ejecutar los recorridos obligatorios de acceso, datos maestros, turno principal, cancelación, interrupción, monitoreo y canal conversacional con aislamiento entre ingenios.
- [ ] 8.3 Ejecutar dos veces el guion completo desde datos controlados, incluyendo reinicio y restore, sin editar manualmente la base; registrar resultados y defectos.
