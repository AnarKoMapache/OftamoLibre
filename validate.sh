#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
OUTPUT="$SCRIPT_DIR/dist/win-x64"
ZIP_FILE="$SCRIPT_DIR/dist/OpticaImagenChileChico-Portable-win-x64.zip"

echo "=== VitalGest / OftalmoLibre — Validación para Windows ==="
echo ""

# 1. Smoke tests
echo "[1/2] Ejecutando smoke tests..."
dotnet run --project "$SCRIPT_DIR/OftalmoLibre.SmokeTests/OftalmoLibre.SmokeTests.csproj" \
    -c Release --nologo -v quiet
echo ""

# 2. Build win-x64
echo "[2/2] Construyendo distribución win-x64..."
rm -rf "$OUTPUT"
rm -f "$ZIP_FILE"

dotnet publish "$SCRIPT_DIR/OftalmoLibre/OftalmoLibre.csproj" \
    -r win-x64 \
    --self-contained true \
    -c Release \
    -o "$OUTPUT" \
    /p:DebugType=None \
    --nologo \
    -v quiet

MARKER_FILE="$OUTPUT/portable.mode"
printf "portable\n" > "$MARKER_FILE"

cat > "$OUTPUT/LEEME-PORTABLE.txt" <<'EOF'
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

PRIMERA VEZ en cada PC (elimina advertencia de SmartScreen):
- Clic derecho en "Instalar-Certificado.bat"
- Seleccione "Ejecutar como administrador"
- Acepte la instalacion del certificado
- Listo: OftalmoLibre.exe abrira sin advertencias desde ese momento
EOF

EXE="$OUTPUT/OftalmoLibre.exe"
if [[ ! -f "$EXE" ]]; then
    echo "ERROR: No se encontró el ejecutable en $EXE" >&2
    exit 1
fi

# 3. Firma de código (opcional — requiere signing/codesign.pfx y osslsigncode)
PFX="$SCRIPT_DIR/signing/codesign.pfx"
CRT="$SCRIPT_DIR/signing/codesign.crt"
if [[ -f "$PFX" ]] && command -v osslsigncode &>/dev/null; then
    echo "[3/3] Firmando ejecutable..."
    bash "$SCRIPT_DIR/signing/sign-exe.sh" "$EXE"
    SIGNED="firmado"

    # Incluir certificado + instalador en el zip para que el usuario lo instale una vez en cada PC
    if [[ -f "$CRT" ]]; then
        cp "$CRT" "$OUTPUT/OpticaImagen-Certificado.crt"
        cat > "$OUTPUT/Instalar-Certificado.bat" <<'WINEOF'
@echo off
:: Instala el certificado de Optica Imagen Chile Chico como raiz de confianza.
:: Ejecutar UNA SOLA VEZ en cada PC, como Administrador.
:: Despues de esto, OftalmoLibre.exe abre sin advertencia de SmartScreen.

net session >nul 2>&1
if %errorLevel% neq 0 (
    echo Este script necesita ejecutarse como Administrador.
    echo Haga clic derecho en este archivo y seleccione "Ejecutar como administrador".
    pause
    exit /b 1
)

certutil -addstore -f "Root" "%~dp0OpticaImagen-Certificado.crt"
if %errorLevel% equ 0 (
    echo.
    echo Certificado instalado correctamente.
    echo OftalmoLibre.exe ya no mostrara advertencias de SmartScreen en este equipo.
) else (
    echo.
    echo Error al instalar el certificado. Contacte al administrador del sistema.
)
pause
WINEOF
    fi
else
    SIGNED="sin firmar (ejecuta signing/generate-cert.sh para habilitar)"
fi

(cd "$SCRIPT_DIR/dist" && zip -qr "$(basename "$ZIP_FILE")" "$(basename "$OUTPUT")")

echo ""
echo "=== VALIDACIÓN COMPLETA ==="
echo "  Ejecutable : $EXE ($SIGNED)"
echo "  Portable   : $ZIP_FILE"
