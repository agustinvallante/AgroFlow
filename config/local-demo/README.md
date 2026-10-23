# Precarga privada del equipo (LocalDemo)

La demo siembra cinco **posiciones** ficticias, cada una con un transportista, un camión y una asociación activa, así que los cinco pueden solicitar turnos. Este archivo privado asigna a cada posición el nombre y el WhatsApp autorizado de un integrante del equipo, sin publicar esos datos en Git.

| `seedId` | DNI ficticio | WhatsApp ficticio por defecto | Camión |
|---|---|---|---|
| `demo-uno` | 20111111111 | +5493815550101 | AF123BC (propia) |
| `demo-dos` | 20222222222 | +5493815550102 | AF456DE (propia) |
| `demo-tres` | 20313131313 | +5493815550121 | AH301AB (propia) |
| `demo-cuatro` | 20414141414 | +5493815550122 | AH302AB (terceros) |
| `demo-cinco` | 20515151515 | +5493815550123 | AH303AB (terceros) |

El DNI, el camión y la asociación de cada posición no cambian: el archivo sólo cambia el nombre y el WhatsApp.

## Uso

1. Copiar [`transportistas.example.json`](transportistas.example.json) como `transportistas.private.json` en esta carpeta, o recibir el archivo completo por un canal seguro (nunca por Git, issues ni chats públicos).
2. Completar `nombre` y `whatsApp` de cada integrante que haya **autorizado** el uso de su número. El WhatsApp va en formato E.164, sin espacios: `+549` seguido del código de área y el número. Se pueden incluir de una a cinco entradas.
3. Levantar la API en LocalDemo. Al arrancar, después de migrar y sembrar, aplica el archivo y registra cuántas posiciones aplicó, con los números enmascarados.

`config/local-demo/*.private.json` está en `.gitignore`. Comprobarlo antes de commitear con `git status`: el archivo privado no debe aparecer.

## Reglas

- **Todo o nada**: se valida el archivo completo antes de escribir y se aplica en una sola transacción. El arranque se detiene sin cambiar la base si el archivo no es JSON válido, tiene propiedades desconocidas, repite un `seedId` o un WhatsApp, usa un `seedId` inexistente, tiene un número fuera de E.164 o un número que ya pertenece a otro transportista (por ejemplo, un alta hecha desde la API). Los mensajes de error nunca muestran teléfonos.
- **Sin archivo**, la demo arranca normalmente con los datos ficticios.
- **Sólo LocalDemo y nunca en `Production`**: en ese entorno el archivo se ignora con un aviso.
- **No toca otros registros**: sólo modifica las cinco posiciones. Los transportistas registrados desde el frontend o la API quedan fuera de la precarga.

## Control de cambios

Por cada posición, la base guarda una huella SHA-256 de la entrada aplicada (tabla `LocalDemoTeamAssignments`), pero no el contenido del archivo.

- Si la entrada **no cambió**, no se vuelve a aplicar: las ediciones hechas después desde el frontend o la API **prevalecen** en los reinicios.
- Si el equipo **cambia una entrada**, sólo esa posición se actualiza en el próximo arranque. Si dos posiciones intercambian números, se aplica sin violar la unicidad.
- Para que una edición hecha desde la API se conserve, no volver a cambiar esa entrada en el archivo.

## Ruta y Docker

Por defecto se busca `config/local-demo/transportistas.private.json` en la raíz del repositorio, relativa a `backend/AgroFlow.Api`. Para otra ubicación, configurar `LocalDemo__TeamFile` con una ruta absoluta.

Si la API se ejecuta en un contenedor, montar el archivo en tiempo de ejecución, de sólo lectura, sin copiarlo a la imagen ni al contexto de build:

```bash
docker run --rm -p 127.0.0.1:5000:8080 \
  -v "$PWD/config/local-demo/transportistas.private.json:/run/secrets/agroflow-team.json:ro" \
  -e LocalDemo__TeamFile=/run/secrets/agroflow-team.json \
  <imagen-de-la-api>
```

El repositorio no incluye hoy una imagen de la API: la demo la ejecuta en el host. Si se agrega un `Dockerfile`, excluir `config/local-demo/*.private.json` en `.dockerignore`.

## Turnos

Cada posición tiene un camión con asociación activa, así que su transportista puede solicitar turnos con su WhatsApp, la patente de la tabla y una finca sembrada (`FINCA-NORTE` o `FINCA-SUR`). Un sexto transportista registrado desde la API es reconocido por el canal, pero no puede solicitar turnos hasta tener una asociación: la gestión de asociaciones sigue pendiente (OD-002, OD-013) y no tiene ruta publicada.
