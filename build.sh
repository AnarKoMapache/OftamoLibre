#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
OUTPUT="$SCRIPT_DIR/dist/win-x64"
ZIP_FILE="$SCRIPT_DIR/dist/OpticaImagenChileChico-Portable-win-x64.zip"
README_FILE="$OUTPUT/LEEME-PORTABLE.txt"
MARKER_FILE="$OUTPUT/portable.mode"

rm -rf "$OUTPUT"
rm -f "$ZIP_FILE"

echo "Publicando OftalmoLibre portable (win-x64, self-contained)..."
dotnet publish "$SCRIPT_DIR/OftalmoLibre/OftalmoLibre.csproj" \
    -r win-x64 \
    --self-contained true \
    -c Release \
    -o "$OUTPUT" \
    /p:DebugType=None \
    --nologo \
    -v quiet

cat > "$MARKER_FILE" <<'EOF'
portable
EOF

cat > "$README_FILE" <<'EOF'
OPTICA IMAGEN CHILE CHICO - VERSION PORTABLE

1. Copie esta carpeta completa a cualquier PC Windows.
2. Abra OftalmoLibre.exe.
3. La base de datos y los backups se guardan en esta misma carpeta.

Archivos importantes:
- OftalmoLibre.exe
- oftalmolibre.db
- Backups\

Recomendacion:
- usar una carpeta normal como Escritorio o Documentos
- no ejecutar desde dentro del .zip
- no guardar en Program Files si quiere seguir usando el modo portable
EOF

(cd "$SCRIPT_DIR/dist" && zip -qr "$(basename "$ZIP_FILE")" "$(basename "$OUTPUT")")

echo ""
echo "Listo: $OUTPUT/OftalmoLibre.exe"
echo "Portable zip: $ZIP_FILE"
