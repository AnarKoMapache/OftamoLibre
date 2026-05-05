#!/usr/bin/env bash
# Genera un certificado autofirmado para firmar OftalmoLibre.exe
# Ejecutar una sola vez. El .pfx resultante se usa en validate.sh / build.sh
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
KEY="$SCRIPT_DIR/codesign.key"
CRT="$SCRIPT_DIR/codesign.crt"
PFX="$SCRIPT_DIR/codesign.pfx"

if [[ -f "$PFX" ]]; then
    echo "Ya existe $PFX — borra el archivo si quieres regenerarlo."
    exit 0
fi

echo "Generando clave privada RSA 4096..."
openssl genrsa -out "$KEY" 4096

echo "Generando certificado autofirmado (válido 10 años)..."
openssl req -new -x509 -key "$KEY" \
    -out "$CRT" \
    -days 3650 \
    -subj "/C=CL/ST=Aysen/L=Chile Chico/O=Optica Imagen Chile Chico/CN=OftalmoLibre Code Signing"

echo "Exportando a PFX (sin contraseña)..."
openssl pkcs12 -export \
    -out "$PFX" \
    -inkey "$KEY" \
    -in "$CRT" \
    -passout pass:

echo ""
echo "Certificado generado:"
openssl x509 -in "$CRT" -noout -subject -dates
echo ""
echo "Archivo listo: $PFX"
echo ""
echo "Para que Windows confíe en él sin advertencia SmartScreen, instala"
echo "el certificado en 'Entidades de certificación raíz de confianza':"
echo "  1. Copia codesign.crt al PC Windows"
echo "  2. Doble clic → Instalar certificado → Equipo local"
echo "     → Seleccionar almacén manualmente → Entidades de certificación raíz de confianza"
