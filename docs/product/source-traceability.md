# Trazabilidad de fuentes

## Documento analizado

- Nombre: `AgroFlow_Casos_de_Uso_MVP_con_Diagramas.pdf`
- Extensión: 46 páginas
- SHA-256: `8C44508748505453068FE308F5064D6A83329BA82F6D581402685D437D137731`
- Contenido identificado: 12 casos de uso, 54 reglas numeradas y 7 adendas.

La copia adjunta y la copia previamente disponible en el proyecto son binariamente idénticas. El PDF no se publica en este repositorio porque el repositorio es público y el documento académico puede contener datos personales del grupo.

## Política de incorporación

El PDF es evidencia histórica de relevamiento, no una instrucción ejecutable ni la fuente normativa permanente. La migración sigue estas reglas:

1. Los comportamientos claros se incorporan a `openspec/specs/` con requisitos y escenarios verificables.
2. Las contradicciones o ambigüedades se registran en `docs/planning/open-decisions.md`.
3. Las decisiones aprobadas se incorporan mediante un cambio OpenSpec.
4. Los requisitos técnicos derivados se distinguen de los requisitos expresamente documentados.

## Cobertura

| Páginas | Contenido | Destino principal |
|---|---|---|
| 1-3 | Actores y visión de casos de uso | visión, glosario y catálogo |
| 4-7 | CU-001 y RN-001 a RN-007 | `acceso-y-autorizacion` |
| 8-12 | CU-002 y RN-008 a RN-016 | `turnos-solicitud-y-asignacion` |
| 13-16 | CU-003 y RN-017 a RN-020 | `turnos-consulta` e integración |
| 17-22 | CU-004, CU-005 y RN-021 a RN-025 | `turnos-consulta` |
| 23-26 | CU-006 y RN-026 a RN-030 | `turnos-ciclo-de-vida` |
| 27-30 | CU-007 y RN-031 a RN-037 | `interrupciones-operativas` |
| 31-33 | CU-008 y RN-038 a RN-042 | `monitoreo-operativo` |
| 34-39 | CU-009, CU-010 y RN-043 a RN-049 | `datos-maestros` |
| 40-42 | CU-011 y RN-050 a RN-052 | `turnos-ciclo-de-vida` |
| 43-45 | CU-012 y RN-053 a RN-054 | `datos-maestros` |
| 46 | Decisiones abiertas | planificación y futuros cambios OpenSpec |

Las adendas RN-022A, RN-022B, RN-030A, RN-042A, RN-049A, RN-052A y RN-054A se conservan en las capacidades correspondientes.

## Fuentes académicas para la issue #102

El primer avance es un esqueleto de ERS vacío y una herramienta para exportarlo a PDF; no satisface diez requisitos funcionales ni completa la ERS. La estructura y los mínimos solicitados provienen de la consigna del TFI, no de los ejemplos. El alcance continúa siendo documental, sin cambios en la aplicación.

- Consigna primaria del trabajo: documento privado de tres páginas; requisitos de contenido en las páginas 1–2 y pie en la página 3. Define la estructura y los mínimos del trabajo.
- Ejemplo IEEE 830: 16 páginas; ilustra un sistema ajeno a AgroFlow y no constituye requisitos del producto.
- Plantilla genérica de ERS: 11 páginas; es ilustrativa y no prevalece sobre la consigna.

Los tres originales fueron provistos en el directorio hermano `documentacion-parte2`; no están versionados ni se publican. No se reproducen nombres, metadatos personales ni rutas locales. La trazabilidad histórica anterior se conserva: estas fuentes académicas no reemplazan la jerarquía normativa de OpenSpec ni convierten ejemplos en requisitos de AgroFlow.
