/** Relieve decorativo: no representa la geografía real ni depende de los datos. */

const CURVAS_DE_NIVEL = Array.from({ length: 11 }, (_, i) => {
  const base = 30 + i * 46;
  const fase = i * 0.7;
  const puntos: string[] = [];
  for (let x = 0; x <= 800; x += 20) {
    const y = base + 16 * Math.sin(x * 0.009 + fase) + 7 * Math.sin(x * 0.023 + fase * 2.1);
    puntos.push(`${x} ${y.toFixed(1)}`);
  }
  return `M ${puntos.join(" L ")}`;
});

const PARCELAS: { puntos: string; surco: "a" | "b" }[] = [
  { puntos: "40,60 210,40 240,150 70,175", surco: "a" },
  { puntos: "250,70 400,55 420,140 275,160", surco: "b" },
  { puntos: "560,40 740,55 725,150 575,135", surco: "a" },
  { puntos: "30,250 180,225 205,335 55,360", surco: "b" },
  { puntos: "310,215 480,200 500,300 330,320", surco: "a" },
  { puntos: "590,240 770,225 760,340 605,350", surco: "b" },
  { puntos: "95,395 280,380 300,470 115,485", surco: "a" },
  { puntos: "420,375 600,395 585,480 410,470", surco: "b" },
];

export function FondoMapa() {
  return (
    <g aria-hidden="true">
      <defs>
        <radialGradient id="mapa-luz" cx="62%" cy="42%" r="75%">
          <stop offset="0%" className="mapa-luz-centro" />
          <stop offset="100%" className="mapa-luz-borde" />
        </radialGradient>
        <pattern id="mapa-surco-a" width="7" height="7" patternUnits="userSpaceOnUse" patternTransform="rotate(32)">
          <line x1="0" y1="0" x2="0" y2="7" className="mapa-surco" />
        </pattern>
        <pattern id="mapa-surco-b" width="7" height="7" patternUnits="userSpaceOnUse" patternTransform="rotate(-24)">
          <line x1="0" y1="0" x2="0" y2="7" className="mapa-surco" />
        </pattern>
      </defs>
      <rect width="800" height="500" fill="url(#mapa-luz)" />
      {PARCELAS.map(({ puntos, surco }) => (
        <polygon key={puntos} points={puntos} fill={`url(#mapa-surco-${surco})`} className="mapa-parcela" />
      ))}
      {CURVAS_DE_NIVEL.map((d, i) => (
        <path key={d} d={d} className={`mapa-curva-nivel ${i % 4 === 0 ? "maestra" : ""}`} />
      ))}
    </g>
  );
}
