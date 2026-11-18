/**
 * Un listado paginado que no se pudo leer entero: conserva lo obtenido para
 * mostrarlo con un aviso, en lugar de descartarlo o de cortarlo en silencio.
 */
export class ListadoIncompleto<T> extends Error {
  readonly parciales: T[];

  constructor(parciales: T[], mensaje: string) {
    super(mensaje);
    this.name = "ListadoIncompleto";
    this.parciales = parciales;
  }
}
