#!/usr/bin/env bash
set -euo pipefail

ROOT="${1:-/opt/granaok-src}"
SIGNING_ENV="${GRANAOK_SIGNING_ENV:-/opt/granaok-signing/private/signing.env}"

[[ -f "$SIGNING_ENV" ]] || { echo "Configuração da assinatura não encontrada: $SIGNING_ENV" >&2; exit 1; }
# shellcheck disable=SC1090
source "$SIGNING_ENV"

cd "$ROOT/GranaOk/android"

if [[ -x ./gradlew ]]; then
  ./gradlew clean assembleRelease
elif command -v gradle >/dev/null 2>&1; then
  gradle clean assembleRelease
else
  echo "Gradle não encontrado. Instale Gradle/Android SDK ou adicione o wrapper." >&2
  exit 2
fi

APK="$(find app/build/outputs/apk/release -maxdepth 1 -name '*.apk' -type f | head -n1)"
[[ -n "$APK" ]] || { echo "APK release não encontrado." >&2; exit 3; }

echo "Release gerado em: $ROOT/GranaOk/android/$APK"
