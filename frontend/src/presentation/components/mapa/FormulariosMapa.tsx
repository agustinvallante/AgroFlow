import { useState } from "react";
import type { FormEvent } from "react";
import type { FincaMapa } from "../../../domain/services/simulacionViajes";
import type { DatosFinca, DatosViaje, IngenioMapa } from "../../hooks/useMapaSimulado";

const texto = (datos: FormData, campo: string) => String(datos.get(campo) ?? "").trim();

/** Acepta coma decimal; `NaN` si está vacío o no es un número. */
function numero(datos: FormData, campo: string): number {
  const valor = texto(datos, campo).replace(",", ".");
  return valor === "" ? Number.NaN : Number(valor);
}

function errorDeUbicacion(lat: number, lon: number): string | null {
  if (!(Math.abs(lat) <= 90)) return "La latitud debe estar entre -90 y 90.";
  if (!(Math.abs(lon) <= 180)) return "La longitud debe estar entre -180 y 180.";
  return null;
}

interface CamposLugarProps {
  prefijo: string;
  sujeto: string;
}

function CamposLugar({ prefijo, sujeto }: CamposLugarProps) {
  return (
    <>
      <div className="form-field">
        <label htmlFor={`${prefijo}-nombre`}>Nombre {sujeto}</label>
        <input id={`${prefijo}-nombre`} name="nombre" />
      </div>
      <div className="form-field">
        <label htmlFor={`${prefijo}-lat`}>Latitud {sujeto}</label>
        <input id={`${prefijo}-lat`} name="lat" inputMode="decimal" />
      </div>
      <div className="form-field">
        <label htmlFor={`${prefijo}-lon`}>Longitud {sujeto}</label>
        <input id={`${prefijo}-lon`} name="lon" inputMode="decimal" />
      </div>
    </>
  );
}

interface FormularioIngenioProps {
  onGuardar: (ingenio: IngenioMapa) => void;
}

export function FormularioIngenio({ onGuardar }: FormularioIngenioProps) {
  const [error, setError] = useState<string | null>(null);

  function enviar(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const formulario = e.currentTarget;
    const datos = new FormData(formulario);
    const nombre = texto(datos, "nombre");
    const lat = numero(datos, "lat");
    const lon = numero(datos, "lon");

    if (!nombre) return setError("Ingresá el nombre del ingenio.");
    const problema = errorDeUbicacion(lat, lon);
    if (problema) return setError(problema);

    onGuardar({ nombre, lat, lon });
    setError(null);
    formulario.reset();
  }

  return (
    <form onSubmit={enviar} noValidate>
      <div className="form-grid">
        <CamposLugar prefijo="ingenio" sujeto="del ingenio" />
      </div>
      {error && <div className="error-state">{error}</div>}
      <button className="btn-primary mapa-enviar" type="submit">
        Guardar ingenio
      </button>
    </form>
  );
}

interface FormularioFincaProps {
  fincas: FincaMapa[];
  onAgregar: (datos: DatosFinca) => void;
}

export function FormularioFinca({ fincas, onAgregar }: FormularioFincaProps) {
  const [error, setError] = useState<string | null>(null);

  function enviar(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const formulario = e.currentTarget;
    const datos = new FormData(formulario);
    const nombre = texto(datos, "nombre");
    const lat = numero(datos, "lat");
    const lon = numero(datos, "lon");

    if (!nombre) return setError("Ingresá el nombre de la finca.");
    if (fincas.some((f) => f.nombre.toLowerCase() === nombre.toLowerCase()))
      return setError("Ya existe una finca con ese nombre.");
    const problema = errorDeUbicacion(lat, lon);
    if (problema) return setError(problema);

    onAgregar({ nombre, lat, lon });
    setError(null);
    formulario.reset();
  }

  return (
    <form onSubmit={enviar} noValidate>
      <div className="form-grid">
        <CamposLugar prefijo="finca" sujeto="de la finca" />
      </div>
      {error && <div className="error-state">{error}</div>}
      <button className="btn-primary mapa-enviar" type="submit">
        Agregar finca
      </button>
    </form>
  );
}

interface FormularioViajeProps {
  hayIngenio: boolean;
  fincas: FincaMapa[];
  /** Un camión que todavía no llegó no puede salir en otro viaje. */
  patentesEnViaje: string[];
  onAgregar: (datos: DatosViaje) => void;
}

export function FormularioViaje({ hayIngenio, fincas, patentesEnViaje, onAgregar }: FormularioViajeProps) {
  const [error, setError] = useState<string | null>(null);

  function enviar(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const formulario = e.currentTarget;
    const datos = new FormData(formulario);
    const patente = texto(datos, "patente").replace(/\s+/g, "").toUpperCase();
    const fincaId = texto(datos, "fincaId");
    const velocidadKmh = numero(datos, "velocidad");
    const saleEnMin = numero(datos, "saleEn");
    const retrasoMin = numero(datos, "retraso");

    if (!hayIngenio) return setError("Primero guardá el ingenio.");
    if (!fincaId) return setError("Primero agregá una finca.");
    if (!patente) return setError("Ingresá la patente del camión.");
    if (patentesEnViaje.includes(patente)) return setError("Ese camión ya tiene un viaje sin terminar.");
    if (!(velocidadKmh > 0)) return setError("La velocidad debe ser mayor a 0.");
    if (!(saleEnMin >= 0)) return setError("La salida debe ser 0 o más minutos.");
    if (!(retrasoMin >= 0)) return setError("El retraso debe ser 0 o más minutos.");

    onAgregar({ patente, fincaId, velocidadKmh, saleEnMin, retrasoMin });
    setError(null);
    formulario.reset();
  }

  return (
    <form onSubmit={enviar} noValidate>
      <div className="form-grid">
        <div className="form-field">
          <label htmlFor="viaje-patente">Patente</label>
          <input id="viaje-patente" name="patente" />
        </div>
        <div className="form-field">
          <label htmlFor="viaje-finca">Finca de origen</label>
          <select id="viaje-finca" name="fincaId">
            {fincas.map((f) => (
              <option key={f.id} value={f.id}>
                {f.nombre}
              </option>
            ))}
          </select>
        </div>
        <div className="form-field">
          <label htmlFor="viaje-velocidad">Velocidad (km/h)</label>
          <input id="viaje-velocidad" name="velocidad" inputMode="decimal" defaultValue="40" />
        </div>
        <div className="form-field">
          <label htmlFor="viaje-sale">Sale en (min)</label>
          <input id="viaje-sale" name="saleEn" inputMode="decimal" defaultValue="0" />
        </div>
        <div className="form-field">
          <label htmlFor="viaje-retraso">Retraso simulado (min)</label>
          <input id="viaje-retraso" name="retraso" inputMode="decimal" defaultValue="0" />
        </div>
      </div>
      {error && <div className="error-state">{error}</div>}
      <button className="btn-primary mapa-enviar" type="submit">
        Agregar viaje
      </button>
    </form>
  );
}
