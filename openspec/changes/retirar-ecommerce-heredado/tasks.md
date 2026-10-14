# Tareas de B01

- [x] Renombrar la solución y los cinco proyectos .NET; actualizar referencias, namespaces, CI, guías y cliente HTTP local.
- [x] Retirar controladores, DTOs, servicios, entidades, contexto, migración y seed del e-commerce; conservar la API/migraciones de turnos e Identity como base.
- [x] Inventariar migraciones y documentar los límites de B02/B03 sin prometer conversión de la base de origen.
- [x] Proteger la selección de la SQLite previa al renombre, documentar respaldo/reutilización no destructivos y probar continuidad de turnos, seed, cupos e historial de migraciones después de dos arranques, con DELETE y WAL.
- [x] Verificar build y pruebas (102/102); la prueba de superficie inspecciona Swagger de la aplicación real y confirma ausencia de rutas heredadas. CI verificará restore y el mismo resultado desde un clon limpio.
- [ ] Confirmar revisión del equipo y archivar el cambio cuando el refactor esté integrado.
