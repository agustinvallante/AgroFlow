# Generar el PDF del borrador de ERS

Esta guía describe el generador aislado de `docs/tools/ers-pdf/`. Produce un PDF imprimible desde el Markdown de la ERS, sin cambiar la aplicación AgroFlow.

> **Advertencia:** el PDF es un borrador incompleto del primer avance de la issue #102. No es una ERS aceptada ni prueba que el MVP esté terminado o aceptado.

## Ruta verificada y entorno local

La ruta de verificación prevista para este avance es el workflow de GitHub Actions **ERS PDF**, sobre Ubuntu 24.04 y Node.js 22. El workflow todavía debe ejecutarse sobre el candidato; hasta observar una ejecución exitosa y descargar su artefacto, no se afirma que CI haya generado un PDF.

Versiones fijadas por el paquete: `markdown-it` 14.3.3 y Playwright 1.58.2. El setup de Playwright descargó Chromium 145.0.7632.6 (Chromium revision v1208). En el entorno local se observó Node.js 24.19.0 y npm 11.17.0; el workflow usa Node.js 22.

Descargar Chromium con Playwright **no instala por sí solo las bibliotecas Linux que el navegador necesita para ejecutarse**. En Ubuntu 24.04 se identificaron como faltantes `libnspr4.so`, `libnss3.so`, `libnssutil3.so`, `libsmime3.so` y `libasound.so.2` (asociada al paquete `libasound2t64`). Por eso, en este entorno WSL local no se puede iniciar Chromium actualmente. Esta guía no pide ni ejecuta cambios del sistema: para usar Chromium localmente, una persona administradora debe preparar previamente esas dependencias para su distribución. Pandoc y TeX no son necesarios.

## Preparación local

Desde la raíz del repositorio, instalá las dependencias aisladas y descargá el navegador:

```bash
npm --prefix docs/tools/ers-pdf install
npm --prefix docs/tools/ers-pdf run setup:browser
```

La descarga usa el caché estándar de Playwright del usuario. Estos comandos preparan el paquete y los binarios del navegador, pero no reemplazan las dependencias Linux del runtime indicadas arriba.

## Generación

Desde la raíz del repositorio:

```bash
npm --prefix docs/tools/ers-pdf run build:pdf
```

La entrada predeterminada es `docs/product/ers/ieee830-ers.md` y la salida es `artifacts/ers-tfi-parte2.pdf`. `artifacts/` ya está excluido por Git. El comando no reemplaza una salida existente.

Para indicar rutas alternativas, ejecutá el CLI desde la raíz del repositorio; las rutas relativas se resuelven desde esa raíz (también podés usar rutas absolutas):

```bash
node docs/tools/ers-pdf/src/cli.mjs --input ruta/al-documento.md --output ruta/de-salida.pdf
```

La entrada debe existir y la salida debe terminar en `.pdf`; si ya existe, el generador falla en vez de sobrescribirla. Elegí una ruta de salida que no contenga un archivo que quieras conservar.

## Descargar el PDF generado por CI

1. Abrí la ejecución del workflow **ERS PDF** asociada al push o pull request.
2. Confirmá que el job haya terminado correctamente; el artefacto no se publica si falla una verificación o la generación.
3. En la sección **Artifacts** de esa ejecución, descargá `ers-tfi-parte2-<sha>`.
4. Verificá el PDF descargado: sigue identificado como borrador incompleto y no debe presentarse como ERS aceptada.

El artefacto de CI se conserva durante 14 días. La ejecución del workflow y la generación real del artefacto deben observarse antes de afirmar que este camino está verificado.

## Contenido admitido y límites

- Markdown se transforma a HTML con HTML embebido desactivado; el contenido HTML se muestra como texto y no se ejecuta.
- La hoja de impresión configura papel A4, tablas y listas; el PDF incluye aviso de borrador y numeración de páginas.
- Se rechaza la sintaxis de imágenes Markdown, incluidas las imágenes locales y remotas. El generador bloquea todas las solicitudes de red durante la impresión y no descarga fuentes ni imágenes.
- Los bloques Mermaid, si se agregan, se imprimen como código y **no** como diagramas renderizados. No hay conversión automática de diagramas.

## Pruebas

```bash
npm --prefix docs/tools/ers-pdf test
```

Las pruebas usan el runner integrado `node --test` e incluyen una generación PDF real, que requiere Chromium y sus bibliotecas Linux de runtime instaladas.
