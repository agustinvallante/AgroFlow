# Propuesta no normativa: fundamentos de datos maestros de PR #47

> **Estado: pendiente de acuerdos B00/B02/B03 y decisiones asociadas.** Este
> documento conserva ideas técnicas útiles de PR #47 para evaluación. No define
> endpoints, DTO, seguridad, permisos, paginación, idempotencia ni comportamiento
> HTTP, y no autoriza su implementación. El OpenAPI activo publica solo salud y
> appointments de la demo local.

## Fundamento reutilizable a evaluar

La PR propone servicios de aplicación, contratos de repositorio, validación y
persistencia basada en EF Core/SQLite para los catálogos. Estos patrones pueden
revisarse como base técnica si se compatibilizan con el modelo AgroFlow, el
`AgroFlowDbContext` operativo existente, Identity/JWT de main y sus migraciones.
No deben crear una segunda base de catálogo ni asumir que el esquema de demo
resuelve la persistencia MVP.

La auditoría y el registro de idempotencia que aparecen en el trabajo de PR son
solo fundamentos/propuestas. La presencia de una entidad o servicio de auditoría
no completa una política de auditoría, y una entidad de idempotencia no aprueba
clave pública obligatoria, semántica de repetición, reintentos ni contrato.
OD-006, OD-009 y OD-013 permanecen abiertos en lo que no resuelve ADR-004.

## Contratos y decisiones aún pendientes

Antes de exponer datos maestros hacen falta B00/B02/B03 y acuerdos explícitos
sobre rutas, nombres de operaciones, DTO/campos, permisos restantes, asociaciones,
reactivación, idempotencia y paginación. Los requisitos ya aprobados de catálogo
siguen definidos en `openspec/specs/datos-maestros/spec.md`; este documento no los
amplía ni los sustituye. Operador y supervisor tienen únicamente los permisos
normativos allí descritos, dentro de su ingenio.

## Inventario técnico conservado de la propuesta PR (no usar como contrato)

La propuesta descartada del OpenAPI contenía estos paths para evaluación futura;
no están activos y no implican aprobación:

| Path propuesto | Operaciones propuestas |
|---|---|
| `/api/v1/drivers` | `GET` lista por actividad; `POST` alta |
| `/api/v1/drivers/{id}` | `GET` detalle; `PUT` actualización |
| `/api/v1/drivers/{id}/inactivation` | `POST` inhabilitación |
| `/api/v1/trucks` | `GET` lista por actividad; `POST` alta |
| `/api/v1/trucks/{id}` | `GET` detalle; `PUT` actualización |
| `/api/v1/trucks/{id}/inactivation` | `POST` inhabilitación |
| `/api/v1/farms` | `GET` lista por actividad; `POST` alta |
| `/api/v1/farms/{id}` | `GET` detalle; `PUT` actualización |
| `/api/v1/farms/{id}/inactivation` | `POST` inhabilitación |

Los DTO propuestos eran `DriverInput(name, dni, whatsApp)`,
`TruckInput(plate, fleetType)`, `FarmInput(code, name, locationReference)` y
respuestas genéricas con `id`, `name`, `code`, `identifier`,
`secondaryIdentifier`, `isActive`, `createdAtUtc` e `inactivatedAtUtc`. La
propuesta incluía `MutationReceipt(resourceId, resourceType, outcome)` y una
respuesta RFC 7807 simplificada. Son bosquejos PR sin aprobación de nombres,
campos, nulabilidad, reglas, estados ni seguridad. Además, `CreatedAtUtc` e
`InactivatedAtUtc` no tienen columnas en las migraciones SQLite main de las
entidades maestras; propiedades CLR de ese tipo no implican persistencia.

La propuesta también exigía `Idempotency-Key` en altas, actualizaciones e
inhabilitaciones. Esa obligación es expresamente **no aprobada**: OD-006 sigue
pendiente. No copiar los paths, DTO, recibos ni encabezado en OpenAPI activo
hasta resolver B00/B02/B03, el cambio OpenSpec propietario y contrato asociado.
Tampoco inferir una política de sesión web ni trasladar el modelo de la PR a un
nuevo esquema.

## Cómo revisar la propuesta

1. Comparar la persistencia con el modelo real de turnos SQLite y la ADR-006.
2. Confirmar continuidad de Identity/JWT de main y el aislamiento por ingenio.
3. Resolver decisiones pendientes con los responsables y documentar el cambio
   en la capacidad OpenSpec propietaria.
4. Publicar contrato, implementación, pruebas y documentación juntos después de
   aprobar esas decisiones.

Hasta entonces, esta propuesta sirve solo como inventario para revisión técnica.
