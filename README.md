# AgroFlow

Proyecto académico para la materia **Administración de Sistemas**. El objetivo es convertir el prototipo visual de AgroFlow en una aplicación funcional con una API en .NET y un frontend web.

## Estado actual

Este repositorio contiene el punto de partida técnico:

- `backend/`: copia limpia e independiente del código de [ICS2026-backend](https://github.com/agustinvallante/ICS2026-backend), basada en el commit `dc40f7cfa515a1425f8709c73ae917e9c004266a`.
- `frontend/`: espacio reservado para integrar o evolucionar [AgroFlow-Dashboard](https://github.com/GabrielBurieque/AgroFlow-Dashboard).
- `docs/`: especificaciones, decisiones, planificación y contratos del sistema.

La copia se inició sin reutilizar el historial Git del backend original, para mantener un historial propio de AgroFlow y evitar trasladar configuraciones sensibles antiguas.

## Verificación del backend base

Requisitos: SDK de .NET 8 y SQL Server LocalDB.

```powershell
dotnet restore backend/Dsw2025Tpi.sln
dotnet build backend/Dsw2025Tpi.sln
```

La solución todavía conserva nombres y módulos del proyecto de comercio electrónico original. Su adaptación al dominio de AgroFlow se realizará de forma trazable a partir de las especificaciones de `docs/`.

## Configuración sensible

No se deben versionar claves reales ni credenciales. La clave JWT no está incluida en el repositorio: se debe definir `Jwt__Key` mediante variables de entorno o secretos de usuario de .NET.
