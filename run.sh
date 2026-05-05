#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
EXE="$SCRIPT_DIR/dist/win-x64/OftalmoLibre.exe"

# Compilar si el ejecutable no existe todavía
if [ ! -f "$EXE" ]; then
    echo "Ejecutable no encontrado — compilando primero..."
    bash "$SCRIPT_DIR/build.sh"
fi

# Esta build portable guarda la base y los backups junto al .exe.

echo "Iniciando OftalmoLibre en Wine..."
exec wine "$EXE"
