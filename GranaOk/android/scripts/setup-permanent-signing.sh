#!/usr/bin/env bash
set -euo pipefail

BASE="${GRANAOK_SIGNING_DIR:-/opt/granaok-signing}"
PRIVATE="$BASE/private"
BACKUP="$BASE/backup"
KEYSTORE="$PRIVATE/granaok-release.jks"
ENVFILE="$PRIVATE/signing.env"
ALIAS="granaok"

umask 077
mkdir -p "$PRIVATE" "$BACKUP"

if ! command -v keytool >/dev/null 2>&1; then
  echo "Erro: keytool não encontrado. Instale um JDK antes de continuar." >&2
  exit 1
fi

if [[ -e "$KEYSTORE" || -e "$ENVFILE" ]]; then
  echo "A assinatura permanente já parece existir em $PRIVATE."
  echo "Nada foi sobrescrito."
  exit 2
fi

STORE_PASS="$(openssl rand -hex 24)"
KEY_PASS="$STORE_PASS"
BACKUP_PASS="$(openssl rand -hex 32)"

keytool -genkeypair   -keystore "$KEYSTORE"   -storetype JKS   -storepass "$STORE_PASS"   -keypass "$KEY_PASS"   -alias "$ALIAS"   -keyalg RSA   -keysize 4096   -validity 10000   -dname "CN=GranaOk, OU=Android, O=GranaOk, L=Marataizes, ST=ES, C=BR"

cat > "$ENVFILE" <<EOF
export GRANAOK_KEYSTORE_PATH="$KEYSTORE"
export GRANAOK_KEYSTORE_PASSWORD="$STORE_PASS"
export GRANAOK_KEY_ALIAS="$ALIAS"
export GRANAOK_KEY_PASSWORD="$KEY_PASS"
EOF

chmod 600 "$KEYSTORE" "$ENVFILE"

STAMP="$(date +%Y%m%d-%H%M%S)"
ARCHIVE="$BACKUP/granaok-assinatura-permanente-$STAMP.tar.enc"

tar -C "$PRIVATE" -cf - "$(basename "$KEYSTORE")" "$(basename "$ENVFILE")"   | openssl enc -aes-256-cbc -salt -pbkdf2 -iter 200000 -pass pass:"$BACKUP_PASS" -out "$ARCHIVE"

chmod 600 "$ARCHIVE"

CERT="$BACKUP/granaok-certificado-$STAMP.pem"
keytool -exportcert -rfc -keystore "$KEYSTORE" -storepass "$STORE_PASS" -alias "$ALIAS" -file "$CERT" >/dev/null
chmod 644 "$CERT"

echo
echo "=============================================================="
echo " ASSINATURA PERMANENTE DO GRANAOK CRIADA"
echo "=============================================================="
echo "Keystore privado: $KEYSTORE"
echo "Configuração privada: $ENVFILE"
echo "Backup criptografado: $ARCHIVE"
echo "Certificado público: $CERT"
echo
echo "SENHA DO BACKUP (ANOTE EM LOCAL SEGURO; ELA NÃO SERÁ SALVA):"
echo "$BACKUP_PASS"
echo
echo "O arquivo .tar.enc deve ser copiado para o Google Drive."
echo "NUNCA envie granaok-release.jks ou signing.env para o GitHub."
echo "=============================================================="
