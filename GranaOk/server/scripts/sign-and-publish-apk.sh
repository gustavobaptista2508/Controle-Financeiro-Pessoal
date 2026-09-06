#!/usr/bin/env bash
set -euo pipefail

if [[ $# -lt 3 ]]; then
  echo "Uso: $0 /caminho/GranaOk-UNSIGNED.apk VERSION_CODE VERSION_NAME" >&2
  exit 1
fi

UNSIGNED="$1"
VERSION_CODE="$2"
VERSION_NAME="$3"
SIGNING_ENV="${GRANAOK_SIGNING_ENV:-/opt/granaok-signing/private/signing.env}"
PUBLISH_SCRIPT="${GRANAOK_PUBLISH_SCRIPT:-/opt/granaok/scripts/publish-apk.sh}"
WORK="${GRANAOK_SIGN_WORK:-/opt/granaok-signing/work}"

[[ -f "$UNSIGNED" ]] || { echo "APK não encontrado: $UNSIGNED" >&2; exit 2; }
[[ -f "$SIGNING_ENV" ]] || { echo "Assinatura permanente não configurada: $SIGNING_ENV" >&2; exit 3; }
[[ -x "$PUBLISH_SCRIPT" || -f "$PUBLISH_SCRIPT" ]] || { echo "Publicador não encontrado: $PUBLISH_SCRIPT" >&2; exit 4; }
command -v jarsigner >/dev/null 2>&1 || { echo "jarsigner não encontrado. Instale um JDK." >&2; exit 5; }

# shellcheck disable=SC1090
source "$SIGNING_ENV"
mkdir -p "$WORK"
SIGNED="$WORK/GranaOk-$VERSION_NAME.apk"
cp "$UNSIGNED" "$SIGNED"

jarsigner   -keystore "$GRANAOK_KEYSTORE_PATH"   -storepass "$GRANAOK_KEYSTORE_PASSWORD"   -keypass "$GRANAOK_KEY_PASSWORD"   -sigalg SHA256withRSA   -digestalg SHA-256   "$SIGNED" "$GRANAOK_KEY_ALIAS"

jarsigner -verify -strict -certs "$SIGNED" >/dev/null

bash "$PUBLISH_SCRIPT" "$SIGNED" "$VERSION_CODE" "$VERSION_NAME"

echo
echo "Release assinado com a chave permanente e publicado no VPS."
echo "Observação: jarsigner aplica assinatura APK/JAR v1, suficiente para distribuição direta."
echo "Quando Android build-tools estiver disponível no VPS, o fluxo pode usar apksigner v2/v3."
