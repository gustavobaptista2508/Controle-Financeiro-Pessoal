const mysql = require('mysql2/promise');
const { config } = require('./config');

const pool = mysql.createPool({
  host: config.db.host,
  port: config.db.port,
  database: config.db.database,
  user: config.db.user,
  password: config.db.password,
  waitForConnections: true,
  connectionLimit: 5,
  queueLimit: 0,
  connectTimeout: 10000,
  enableKeepAlive: true,
  keepAliveInitialDelay: 0,
  charset: 'utf8mb4',
  ...(config.db.ssl ? { ssl: { rejectUnauthorized: false } } : {})
});

async function withConn(fn) {
  const conn = await pool.getConnection();
  try { return await fn(conn); }
  finally { conn.release(); }
}

async function hasColumn(conn, table, column) {
  const [rows] = await conn.query('SHOW COLUMNS FROM \`' + table + '\` LIKE ?', [column]);
  return rows.length > 0;
}
async function ensureColumn(conn, table, column, definition) {
  if (!(await hasColumn(conn, table, column))) {
    await conn.query('ALTER TABLE \`' + table + '\` ADD COLUMN \`' + column + '\` ' + definition);
  }
}
async function ensureSchema(conn) {
  const p = config.db.prefix;
  await conn.query(
    'CREATE TABLE IF NOT EXISTS '+p+'app_users ('+
    'id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,'+
    'username VARCHAR(80) NOT NULL,'+
    'display_name VARCHAR(120) NOT NULL,'+
    'password_hash VARCHAR(255) NOT NULL,'+
    "role VARCHAR(20) NOT NULL DEFAULT 'user',"+
    'person_id BIGINT UNSIGNED NULL,'+
    'active TINYINT(1) NOT NULL DEFAULT 1,'+
    'created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,'+
    'last_login_at DATETIME NULL,'+
    'PRIMARY KEY(id), UNIQUE KEY uq_app_user_username(username), KEY idx_app_user_person(person_id)'+
    ') ENGINE=InnoDB DEFAULT CHARSET=utf8mb4'
  );
  await conn.query(
    'CREATE TABLE IF NOT EXISTS '+p+'web_sessions ('+
    'id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,'+
    'token_hash CHAR(64) NOT NULL,'+
    'user_id BIGINT UNSIGNED NOT NULL,'+
    'expires_at DATETIME NOT NULL,'+
    'last_seen_at DATETIME NOT NULL,'+
    'created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,'+
    'PRIMARY KEY(id), UNIQUE KEY uq_web_session_token(token_hash), KEY idx_web_session_user(user_id), KEY idx_web_session_exp(expires_at)'+
    ') ENGINE=InnoDB DEFAULT CHARSET=utf8mb4'
  );

  const needed = [
    [p+'people','entity_kind',"VARCHAR(20) NOT NULL DEFAULT 'person'"],
    [p+'people','partner_name','VARCHAR(120) NULL'],
    [p+'accounts','bank_code','VARCHAR(40) NULL'],
    [p+'transactions','observations','LONGTEXT NULL'],
    [p+'transactions','installment_group','VARCHAR(64) NULL'],
    [p+'transactions','installment_number','INT NOT NULL DEFAULT 1'],
    [p+'transactions','installment_total','INT NOT NULL DEFAULT 1'],
    [p+'card_invoices','paid_date','DATE NULL'],
    [p+'financings','next_due_date','DATE NULL'],
    [p+'financings','last_paid_date','DATE NULL']
  ];
  for (const [table,col,def] of needed) await ensureColumn(conn,table,col,def);
  return p;
}

async function initDb() {
  return withConn(async conn => {
    await conn.query('SELECT 1');
    await ensureSchema(conn);
    const [v] = await conn.query('SELECT VERSION() version, DATABASE() db');
    return v[0];
  });
}

module.exports = { pool, withConn, ensureSchema, initDb };
