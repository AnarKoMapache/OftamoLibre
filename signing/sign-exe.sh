#!/usr/bin/env bash
# Firma un ejecutable Windows con el certificado autofirmado.
# Uso: bash sign-exe.sh <ruta-al-exe>
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PFX="$SCRIPT_DIR/codesign.pfx"
EXE="${1:-}"

if [[ -z "$EXE" ]]; then
    echo "Uso: $0 <ruta-al-exe>" >&2
    exit 1
fi

if [[ ! -f "$EXE" ]]; then
    echo "ERROR: no se encontró el ejecutable: $EXE" >&2
    exit 1
fi

if [[ ! -f "$PFX" ]]; then
    echo "ERROR: no se encontró $PFX — ejecuta primero signing/generate-cert.sh" >&2
    exit 1
fi

osslsigncode sign \
    -pkcs12 "$PFX" \
    -pass "" \
    -n "OftalmoLibre - Optica Imagen Chile Chico" \
    -i "https://opticaimagen.cl" \
    -t "http://timestamp.digicert.com" \
    -in "$EXE" \
    -out "${EXE}.signed"

mv "${EXE}.signed" "$EXE"
echo "Firmado: $EXE"
