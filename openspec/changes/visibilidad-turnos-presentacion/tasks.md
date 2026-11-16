## 1. Fecha y confirmación del alta — #108

- [x] 1.1 Conservar la ventana RFC 3339 completa en el modelo y mapper.
- [x] 1.2 Propagar el turno confirmado, seleccionar su fecha y abrir el detalle.
- [x] 1.3 Limpiar filtros excluyentes con aviso y mostrar fecha, hora y zona.
- [x] 1.4 Probar hoy/mañana, errores y respuestas de lecturas atrasadas.

## 2. Camiones en curso — #109

- [x] 2.1 Agregar repositorio y caso de uso de activos con `active=true`.
- [x] 2.2 Conectar el panel sin cambiar la consulta diaria de la cola.
- [x] 2.3 Probar ventanas anteriores/futuras, terminales, polling y fallas.

## 3. Verificación conjunta

- [x] 3.1 Documentar comportamiento y recorrido de verificación.
- [x] 3.2 Ejecutar tipos, lint, pruebas, build y validación documental.
- [x] 3.3 Verificar LocalDemo con API/SQLite reales y navegador en otra zona.
- [x] 3.4 Abrir una PR conjunta a main y registrar la evidencia sin mergear.

PR de implementación y evidencia:
[#115](https://github.com/agustinvallante/AgroFlow/pull/115).

## 4. Correcciones de la review de Braian

- [x] 4.1 Aclarar que ASIGNADO se excluye de Camiones en curso y se consulta en la cola.
- [x] 4.2 Permitir cerrar la confirmación y retirarla cuando cambia cualquier filtro.
- [x] 4.3 Disparar explícitamente la recarga tras el alta y separar el polling de lista y detalle.
- [x] 4.4 Documentar el coste 1+N de primera carga, la caché y la coordinación de rendimiento con F06/F07/#106.
- [x] 4.5 Agregar regresiones de aviso, recarga con filtros coincidentes y reutilización de detalles.
- [x] 4.6 Validar tipos, lint, pruebas, build y documentación, y repetir el recorrido con API/SQLite reales.
