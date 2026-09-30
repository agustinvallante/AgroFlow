# Retirar el dominio heredado de la API

## Why

La copia inicial de ICS2026-backend todavía publicaba controladores de productos, pedidos y registro de clientes. No pertenecen a AgroFlow y crean rutas, dependencias y migraciones que interfieren con el trabajo paralelo del MVP. La issue [B01](https://github.com/agustinvallante/AgroFlow/issues/8) solicita retirarlos sin perder el recorrido persistente de turnos de la demo.

## What Changes

- Renombrar solución, cinco proyectos, namespaces, referencias y rutas de CI a `AgroFlow.*`.
- Retirar la superficie HTTP, el modelo y la migración de comercio electrónico; no cargar `customers.json` ni administradores desde un archivo de contraseñas.
- Conservar la API de turnos, sus migraciones/seed y pruebas; conservar Identity/JWT sólo como andamiaje no integrado al MVP.
- Desacoplar el login heredado de `Customer` y retirar el registro público ligado a ese dominio.

## Capabilities

Se modifica `plataforma-y-segregacion` para asegurar que la API sólo publique capacidades de AgroFlow. No se alteran las reglas de turnos ni se decide la matriz de permisos pendiente.

## Impact

Cambian rutas de archivos para todo el equipo y desaparecen rutas HTTP de comercio electrónico. Las bases SQL Server del proyecto de origen no se migran ni se borran. B02 y B03 deberán crear/adaptar sus migraciones y contratos propios.
