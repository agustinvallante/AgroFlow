import { main } from "./generator.mjs";

main().catch((error) => {
  console.error(`No se pudo generar el PDF: ${error.message}`);
  process.exitCode = 1;
});
