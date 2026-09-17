# Visión y alcance del MVP

## Problema

Durante la zafra, la llegada descoordinada de camiones al ingenio genera colas, tiempos improductivos, pérdida de calidad de la caña y períodos de sobrecarga o falta de abastecimiento en recepción.

## Visión

AgroFlow será un sistema B2B para coordinar turnos y operaciones de ingreso de camiones. Proporcionará:

- una aplicación web para operadores, supervisores y responsables de gestión;
- un canal de WhatsApp para transportistas, integrado mediante n8n;
- una API que concentre reglas, permisos y estado operativo;
- trazabilidad suficiente para medir tiempos de espera y calidad del servicio.

El backend es la fuente operativa de verdad. El frontend y n8n presentan o transmiten decisiones, pero no implementan reglas de asignación, prioridad ni transición de estados por cuenta propia.

## Objetivos declarados

- Disponer de un MVP funcional en aproximadamente dos meses.
- Reducir el tiempo promedio de espera en un 30 por ciento.
- Alcanzar una disponibilidad objetivo de 99,9 por ciento durante la zafra.
- Permitir que la gestión de turnos pueda realizarse mediante el asistente de WhatsApp.
- Preparar el producto para operar en más de un ingenio sin mezclar información.

## Alcance funcional del MVP

- autenticación y autorización de usuarios internos;
- aislamiento de información por ingenio;
- gestión de transportistas, camiones y fincas;
- asociación explícita entre transportistas y camiones autorizados;
- solicitud, asignación, consulta y cancelación de turnos;
- máquina de estados del ciclo operativo;
- prioridad y reserva de capacidad;
- registro de interrupciones y reprogramación;
- dashboard operativo diario;
- integración con WhatsApp a través de n8n;
- historial y trazabilidad de operaciones críticas.

## Fuera del MVP

- geolocalización en tiempo real;
- ruteo cartográfico avanzado;
- polígonos de fincas y funciones PostGIS;
- integración con ERP;
- analítica predictiva avanzada;
- obligación de actualización mediante WebSockets;
- optimización de rutas fuera del proceso de turnos.

## Criterio de alcance

Una funcionalidad pertenece al MVP cuando está respaldada por una especificación vigente en `openspec/specs/`, tiene un contrato verificable y aparece en la hoja de ruta aprobada. Una idea presente solo en un PDF, prototipo o conversación sigue siendo referencia hasta ser incorporada mediante el flujo OpenSpec.

## Cuándo el MVP es suficiente

El alcance indica **qué** debe formar parte del MVP; la especificación de [aceptación del MVP](../../openspec/specs/aceptacion-del-mvp/spec.md) determina **cuándo** el conjunto puede considerarse funcional. La [guía de aceptación](mvp-acceptance.md) traduce ese umbral en puertas, evidencias y un guion reproducible.

La reducción del 30 por ciento en el tiempo de espera y la disponibilidad del 99,9 por ciento son objetivos de resultado que requieren línea base y observación operativa. La entrega académica debe dejar preparada su medición, pero no necesita afirmar resultados que todavía no pueden comprobarse en producción.
