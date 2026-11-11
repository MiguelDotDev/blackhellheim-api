#!/usr/bin/env bash
# Prueba de la construcción de la imagen (historia 005): construye los destinos "migraciones" y
# "final", comprueba que el bundle de migraciones existe y que ninguna imagen conserva secretos
# ni la variable de conexión usada solo durante la construcción.
set -euo pipefail

raiz="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$raiz"

imagen_migraciones="blackhellheim-migraciones:prueba"
imagen_final="blackhellheim-api:prueba"

fallar() { echo "ERROR: $1" >&2; exit 1; }

[[ -f Dockerfile ]] || fallar "No existe el Dockerfile."
docker build --target migraciones -t "$imagen_migraciones" . >/dev/null || fallar "Falló la construcción del destino migraciones."
docker build --target final -t "$imagen_final" . >/dev/null || fallar "Falló la construcción del destino final."

docker run --rm --entrypoint sh "$imagen_migraciones" -c 'test -x ./efbundle' \
  || fallar "La imagen de migraciones no contiene el ejecutable efbundle."

for imagen in "$imagen_migraciones" "$imagen_final"; do
  historial="$(docker history --no-trunc "$imagen")"
  if grep -qiE 'Passwor[d]=|BlackHellHeimBundle' <<<"$historial"; then
    fallar "El historial de $imagen contiene credenciales o la cadena ficticia de la construcción."
  fi
  if docker run --rm --entrypoint printenv "$imagen" ConnectionStrings__BlackHellHeim >/dev/null 2>&1; then
    fallar "La imagen $imagen conserva la variable ConnectionStrings__BlackHellHeim."
  fi
done

echo "Construcción de la imagen verificada."
