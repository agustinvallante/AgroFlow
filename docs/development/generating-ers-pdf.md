# Generar el PDF del borrador de ERS

Esta guía describe el generador aislado de `docs/tools/ers-pdf/`. Produce un PDF imprimible desde el Markdown de la ERS, sin cambiar la aplicación AgroFlow.

> **Advertencia:** el PDF es un borrador incompleto del primer avance de la issue #102. No es una ERS aceptada ni prueba que el MVP esté terminado o aceptado.

## Ruta verificada y entorno local

El primer candidato del avance se verificó en GitHub Actions, workflow **ERS PDF**, [ejecución 42439865843](https://github.com/agustinvallante/AgroFlow/actions/runs/42439865843), para el commit `4a4a758fbd0f2bce3a64d5a54735759235388725`. En Ubuntu 24.04.5, Node.js 22.23.4 y npm 10.9.9, CI aprobó 8 pruebas (0 fallidas, 0 omitidas) y la auditoría informó 0 vulnerabilidades. Se generó un PDF A4 de 3 páginas (35.751 bytes), con aviso de borrador y pies numerados; las tres páginas se revisaron visualmente.

Versiones fijadas: `markdown-it` 14.3.3 y Playwright 1.58.2; Chromium 145.0.7632.6. Esta evidencia corresponde a ese candidato, no garantiza contenido futuro. Tras cambios al Markdown, ejecutá nuevamente el workflow para verificar el nuevo commit.

En el entorno WSL usado para este avance, Chromium no pudo iniciarse por bibliotecas Linux faltantes. El script `setup:browser` solo descarga el navegador: no instala esas dependencias del sistema. No se ejecutaron instalaciones con `apt` o `sudo` en ese entorno; las dependencias se preparan automáticamente solo en el runner temporal de CI. Pandoc y TeX no son necesarios.

## Preparación local

Desde la raíz del repositorio, instalá las dependencias aisladas y descargá el navegador:

```bash
npm --prefix docs/tools/ers-pdf ci
npm --prefix docs/tools/ers-pdf run setup:browser
```

`ci` usa el lockfile para una instalación reproducible. La descarga usa el caché estándar de Playwright del usuario, pero no reemplaza las dependencias Linux del runtime indicadas arriba.

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

El artefacto de CI se conserva durante 14 días. El primer candidato verificado generó el PDF real; para cualquier commit posterior, observá una nueva ejecución exitosa y su artefacto antes de afirmar que ese contenido está verificado.

## Contenido admitido y límites

- Markdown se transforma a HTML con HTML embebido desactivado; el contenido HTML se muestra como texto y no se ejecuta.
- La hoja de impresión configura papel A4, tablas y listas. En el candidato verificado, el aviso de borrador apareció en la primera página y los pies numeraron las tres páginas. La maquetación de tablas no se ejercitó en ese esqueleto.
- Se rechaza la sintaxis de imágenes Markdown, incluidas las imágenes locales y remotas. El generador bloquea las solicitudes de red durante la impresión y no descarga fuentes ni imágenes.
- Los bloques Mermaid se imprimen como código, no como diagramas renderizados; no hay conversión automática. Estas son limitaciones del generador, no requisitos de contenido de la ERS.

## Pruebas

```bash
npm --prefix docs/tools/ers-pdf test
```

Las pruebas usan el runner integrado `node --test` e incluyen una generación PDF real, que requiere Chromium y sus bibliotecas Linux de runtime instaladas.
