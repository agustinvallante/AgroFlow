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

Los issues con `area:backend` se agregan al Project Backend y los que tienen `area:frontend` al Project Frontend. Los issues `area:shared` o `area:docs-devops` pueden aparecer en ambos. Los pull requests no se autoagregan como items independientes: deben enlazar el issue correspondiente mediante `Closes #<número>` para evitar duplicar el trabajo.

`To do`, `In progress` y `status:blocked` expresan decisiones del equipo, no inferencias automáticas. Una tarea solo pasa a `To do` cuando fue refinada y priorizada; pasa a `In progress` cuando alguien la toma.

## Verificaciones de pull requests

El workflow `CI` ejecuta cuatro controles coordinados:

1. valida OpenSpec, OpenAPI y enlaces Markdown locales;
2. restaura, compila y, cuando existan, prueba los proyectos .NET;
3. instala, analiza, prueba y compila el frontend cuando esté incorporado;
4. publica un resultado agregado estable llamado `Required checks`.

La protección de `main` debe exigir `Required checks`, una aprobación y la resolución de conversaciones. El job informa expresamente si aún no existen pruebas backend o si el frontend todavía no fue importado; un aviso no debe confundirse con cobertura.

`Dependency review` rechaza dependencias nuevas con vulnerabilidades altas o críticas. Dependabot revisa semanalmente GitHub Actions y paquetes NuGet. La fuente npm se agregará cuando `frontend/` posea `package.json` y `package-lock.json`.

## Entrega y despliegue

Cada push válido a `main` publica durante 14 días un artefacto compilado del backend y, cuando exista, `frontend/dist`. Esto constituye **entrega continua de artefactos**, no despliegue.

El despliegue automático se agregará después de aprobar proveedor, ambiente, dominio, base de datos, secretos y estrategia de migraciones. No se debe crear un workflow de despliegue vacío ni almacenar credenciales en el repositorio.
