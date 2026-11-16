# Generar el PDF del borrador de ERS

Esta guía describe el generador aislado de `docs/tools/ers-pdf/`. Produce un PDF imprimible desde el Markdown de la ERS, sin cambiar la aplicación AgroFlow.

> **Estado:** el PDF contiene el borrador completo de la ERS para revisión académica. No está aprobado, no es una especificación normativa y no prueba que el MVP esté terminado o aceptado. Cada artefacto debe verificarse en la ejecución de CI correspondiente a su commit.

## Ruta verificada y entorno local

Como antecedente, el primer candidato del publicador se verificó en GitHub Actions, workflow **ERS PDF**, [ejecución 42439865843](https://github.com/agustinvallante/AgroFlow/actions/runs/42439865843), para el commit `4a4a758fbd0f2bce3a64d5a54735759235388725`. Esa ejecución generó un PDF de tres páginas para el esqueleto de entonces; no verifica la ERS completa actual ni sustituye la revisión visual de su nuevo artefacto.

Versiones fijadas: `markdown-it` 14.3.3 y Playwright 1.58.2. CI usa Ubuntu 24.04 y Node.js 22, instala Chromium y genera el artefacto de cada commit elegible. Tras cambios al Markdown, verificá la ejecución y el PDF de ese commit antes de afirmar que fue generado o revisado.

En el entorno WSL usado para este avance, Chromium no pudo iniciarse por bibliotecas Linux faltantes. El script `setup:browser` solo descarga el navegador: no instala esas dependencias del sistema. No se ejecutaron instalaciones con `apt` o `sudo` en ese entorno; las dependencias se preparan automáticamente solo en el runner temporal de CI. Pandoc y TeX no son necesarios.

## Evidencia del borrador completo para revisión

El candidato `e7311a9892299d9c63d9dd6d831c64f62c480175` se verificó en [ERS PDF — ejecución 43788016729](https://github.com/agustinvallante/AgroFlow/actions/runs/43788016729): ocho pruebas aprobadas, ninguna fallida u omitida, auditoría sin vulnerabilidades y generación/publicación correctas. [Descargar el PDF de ese candidato (ZIP)](https://github.com/agustinvallante/AgroFlow/actions/runs/43788016729/artifacts/14191515611); su disponibilidad está sujeta a la retención de 14 días.

| Control | Resultado observado |
|---|---|
| Archivo | `ers-tfi-parte2.pdf`, 175.294 bytes, 26 páginas A4 |
| SHA-256 | `0405a196f125dbc3b10e4b7425230becaab1d73dd50ef193412e4238c5423527` |
| Contenido exportado | 17 subsecciones, RF-001–RF-016 y 367/367 unidades de texto normalizado del Markdown conservadas |
| Revisión visual independiente | Las 26 páginas renderizadas a 120 DPI e inspeccionadas individualmente con PyMuPDF 1.26.7; sin recortes, solapamientos, páginas vacías ni filas ilegibles o perdidas |
| Tablas y numeración | Seis restricciones completas en página 22; 13 filas de trazabilidad en páginas 25–26, encabezado repetido y cierre conservado; pies `n / 26` correctos |

La comprobación adicional del coordinador confirmó la tabla de restricciones y el final de la matriz en las páginas 22 y 26. Se observaron detalles cosméticos no bloqueantes: espacios amplios, algunos títulos separados del cuerpo por un salto y referencias/identificadores largos partidos en varias líneas. No se modificó la maquetación para esta entrega.

Esta evidencia corresponde únicamente al PDF del SHA citado: no constituye aprobación académica, aceptación del MVP ni validación automática de futuros artefactos. La inspección utilizó herramientas aisladas en un directorio temporal, sin instalar paquetes del sistema ni reparar Chromium en WSL.

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
4. Verificá el aviso «BORRADOR COMPLETO PARA REVISIÓN · NO APROBADO» y revisá el documento y sus tablas. No lo presentes como ERS aprobada ni como aceptación del MVP.

El artefacto de CI se conserva durante 14 días. El primer candidato verificado generó el PDF real; para cualquier commit posterior, observá una nueva ejecución exitosa y su artefacto antes de afirmar que ese contenido está verificado.

## Contenido admitido y límites

- Markdown se transforma a HTML con HTML embebido desactivado; el contenido HTML se muestra como texto y no se ejecuta.
- La hoja de impresión configura papel A4, tablas y listas, con aviso de revisión y pies numerados. El candidato inicial de tres páginas no ejercitó la maquetación de las tablas de la ERS completa; revisá visualmente todas las páginas del artefacto actualizado antes de distribuirlo.
- Se rechaza la sintaxis de imágenes Markdown, incluidas las imágenes locales y remotas. El generador bloquea las solicitudes de red durante la impresión y no descarga fuentes ni imágenes.
- Los bloques Mermaid se imprimen como código, no como diagramas renderizados; no hay conversión automática. Estas son limitaciones del generador, no requisitos de contenido de la ERS.

## Pruebas

```bash
npm --prefix docs/tools/ers-pdf test
```

Las pruebas usan el runner integrado `node --test` e incluyen generación PDF real, que requiere Chromium y sus bibliotecas Linux de runtime. En el entorno WSL usado para esta tarea faltan bibliotecas compartidas; el runtime de generación previsto y comprobable es GitHub Actions Ubuntu 24.04. No se afirma que el PDF actual se haya generado localmente.
