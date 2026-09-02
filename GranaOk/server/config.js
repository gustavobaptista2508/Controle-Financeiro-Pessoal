require('dotenv').config();

function cleanPrefix(v) {
  const p = String(v || 'granaok_').trim();
  if (!/^[A-Za-z0-9_]{1,32}$/.test(p)) throw new Error('DB_PREFIX inválido.');
  return p;
}
function bool(v, fallback=false) {
  if (v === undefined || v === null || v === '') return fallback;
  return ['1','true','yes','on'].includes(String(v).toLowerCase());
}

const config = {
  host: process.env.HOST || '127.0.0.1',
  port: Number(process.env.PORT || 3000),
  db: {
    host: String(process.env.DB_HOST || '').trim(),
    port: Number(process.env.DB_PORT || 3306),
    database: String(process.env.DB_NAME || '').trim(),
    user: String(process.env.DB_USER || '').trim(),
    password: process.env.DB_PASSWORD_B64 ? Buffer.from(String(process.env.DB_PASSWORD_B64),'base64').toString('utf8') : String(process.env.DB_PASSWORD || ''),
    ssl: bool(process.env.DB_SSL, false),
    prefix: cleanPrefix(process.env.DB_PREFIX || 'granaok_')
  },
  sessionHours: Math.max(1, Math.min(168, Number(process.env.SESSION_HOURS || 12))),
  cookieSecure: bool(process.env.COOKIE_SECURE, false),
  trustProxy: Math.max(0, Math.min(2, Number(process.env.TRUST_PROXY || 1)))
};

for (const k of ['host','database','user','password']) {
  if (!config.db[k]) throw new Error('Configuração ausente no .env: DB_' + k.toUpperCase());
}

module.exports = { config, cleanPrefix };
