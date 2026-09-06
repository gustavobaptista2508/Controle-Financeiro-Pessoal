#!/usr/bin/env bash
set -euo pipefail

if [[ $# -lt 3 ]]; then
  echo "Uso: $0 /caminho/GranaOk.apk VERSION_CODE VERSION_NAME" >&2
  exit 1
fi

APK="$1"
VERSION_CODE="$2"
VERSION_NAME="$3"
RELEASE_DIR="${GRANAOK_APK_RELEASE_DIR:-/opt/granaok-releases}"

[[ -f "$APK" ]] || { echo "APK não encontrado: $APK" >&2; exit 2; }
[[ "$VERSION_CODE" =~ ^[0-9]+$ ]] || { echo "VERSION_CODE precisa ser inteiro." >&2; exit 3; }

mkdir -p "$RELEASE_DIR"
NAME="GranaOk-$VERSION_NAME.apk"
DEST="$RELEASE_DIR/$NAME"

cp "$APK" "$DEST"
chmod 644 "$DEST"
SHA="$(sha256sum "$DEST" | awk '{print $1}')"
SIZE="$(stat -c %s "$DEST")"
NOW="$(date -u +%Y-%m-%dT%H:%M:%SZ)"

cat > "$RELEASE_DIR/latest.json" <<EOF
{
  "available": true,
  "version_code": $VERSION_CODE,
  "version_name": "$VERSION_NAME",
  "download_url": "https://granaok.com.br/apk/$NAME",
  "sha256": "$SHA",
  "size_bytes": $SIZE,
  "published_at": "$NOW",
  "required_signing": "GranaOk permanent signing key"
}
EOF

chmod 644 "$RELEASE_DIR/latest.json"

echo "APK publicado:"
echo "  https://granaok.com.br/apk/$NAME"
echo "Manifesto:"
echo "  https://granaok.com.br/apk/latest.json"
echo "SHA-256: $SHA"
