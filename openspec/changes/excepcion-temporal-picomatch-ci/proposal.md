# Excepción temporal de Picomatch en CI

## Why

El aviso sobre la versión transitiva de Picomatch heredada de `main` bloquea el control global de CI mientras se publica el parche oficial.

## What Changes

- Admitir temporalmente un aviso exacto en el control de auditoría del frontend y publicar el resultado íntegro de `npm audit`.
- Mantener las pruebas requeridas y los controles de dependencia por cambio y por calendario.
- Documentar el vencimiento y el retiro de la excepción en la [guía de automatización](../../../docs/development/github-automation.md#excepción-temporal-para-picomatch).

## Impact

El cambio afecta el criterio de éxito de CI. La API, el frontend y el lockfile conservan sus implementaciones y versiones.
