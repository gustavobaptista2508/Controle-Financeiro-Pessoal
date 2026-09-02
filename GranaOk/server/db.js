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

  await conn.query(
    'CREATE TABLE IF NOT EXISTS '+p+'learning_profiles ('+
    'id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,'+
    'user_id BIGINT UNSIGNED NOT NULL,'+
    'person_id BIGINT UNSIGNED NULL,'+
    'months_analyzed INT NOT NULL DEFAULT 0,'+
    'confidence_score DECIMAL(5,2) NOT NULL DEFAULT 0,'+
    'profile_json LONGTEXT NULL,'+
    'last_rebuilt_at DATETIME NULL,'+
    'active TINYINT(1) NOT NULL DEFAULT 1,'+
    'created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,'+
    'updated_at DATETIME NULL,'+
    'PRIMARY KEY(id), UNIQUE KEY uq_learning_user(user_id), KEY idx_learning_person(person_id)'+
    ') ENGINE=InnoDB DEFAULT CHARSET=utf8mb4'
  );
  await conn.query(
    'CREATE TABLE IF NOT EXISTS '+p+'monthly_features ('+
    'id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,'+
    'user_id BIGINT UNSIGNED NOT NULL,'+
    'person_id BIGINT UNSIGNED NULL,'+
    'reference_month DATE NOT NULL,'+
    'income_total DECIMAL(14,2) NOT NULL DEFAULT 0,'+
    'expense_total DECIMAL(14,2) NOT NULL DEFAULT 0,'+
    'card_total DECIMAL(14,2) NOT NULL DEFAULT 0,'+
    'paid_expense_total DECIMAL(14,2) NOT NULL DEFAULT 0,'+
    'pending_expense_total DECIMAL(14,2) NOT NULL DEFAULT 0,'+
    'overdue_total DECIMAL(14,2) NOT NULL DEFAULT 0,'+
    'installment_commitment DECIMAL(14,2) NOT NULL DEFAULT 0,'+
    'net_cashflow DECIMAL(14,2) NOT NULL DEFAULT 0,'+
    'savings_rate DECIMAL(8,4) NOT NULL DEFAULT 0,'+
    'created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,'+
    'updated_at DATETIME NULL,'+
    'PRIMARY KEY(id), UNIQUE KEY uq_feature_user_month(user_id,reference_month), KEY idx_feature_person(person_id)'+
    ') ENGINE=InnoDB DEFAULT CHARSET=utf8mb4'
  );
  await conn.query(
    'CREATE TABLE IF NOT EXISTS '+p+'recurring_patterns ('+
    'id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,'+
    'user_id BIGINT UNSIGNED NOT NULL,'+
    'person_id BIGINT UNSIGNED NULL,'+
    'pattern_type VARCHAR(30) NOT NULL,'+
    'pattern_key VARCHAR(160) NOT NULL,'+
    'label VARCHAR(255) NOT NULL,'+
    'category_id BIGINT UNSIGNED NULL,'+
    "frequency VARCHAR(20) NOT NULL DEFAULT 'monthly',"+
    'avg_amount DECIMAL(14,2) NOT NULL DEFAULT 0,'+
    'occurrences INT NOT NULL DEFAULT 0,'+
    'months_seen INT NOT NULL DEFAULT 0,'+
    'day_min INT NULL,'+
    'day_max INT NULL,'+
    'confidence DECIMAL(5,2) NOT NULL DEFAULT 0,'+
    'last_seen DATE NULL,'+
    'active TINYINT(1) NOT NULL DEFAULT 1,'+
    'created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,'+
    'updated_at DATETIME NULL,'+
    'PRIMARY KEY(id), UNIQUE KEY uq_pattern_user_key(user_id,pattern_type,pattern_key), KEY idx_pattern_person(person_id)'+
    ') ENGINE=InnoDB DEFAULT CHARSET=utf8mb4'
  );
  await conn.query(
    'CREATE TABLE IF NOT EXISTS '+p+'merchant_memory ('+
    'id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,'+
    'user_id BIGINT UNSIGNED NOT NULL,'+
    'person_id BIGINT UNSIGNED NULL,'+
    'merchant_key VARCHAR(160) NOT NULL,'+
    'merchant_label VARCHAR(255) NOT NULL,'+
    'category_id BIGINT UNSIGNED NULL,'+
    'txn_type VARCHAR(20) NOT NULL,'+
    'occurrences INT NOT NULL DEFAULT 0,'+
    'avg_amount DECIMAL(14,2) NOT NULL DEFAULT 0,'+
    'confidence DECIMAL(5,2) NOT NULL DEFAULT 0,'+
    'manual_corrections INT NOT NULL DEFAULT 0,'+
    'last_seen DATE NULL,'+
    'created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,'+
    'updated_at DATETIME NULL,'+
    'PRIMARY KEY(id), UNIQUE KEY uq_merchant_user_key(user_id,merchant_key,txn_type), KEY idx_merchant_person(person_id)'+
    ') ENGINE=InnoDB DEFAULT CHARSET=utf8mb4'
  );
  await conn.query(
    'CREATE TABLE IF NOT EXISTS '+p+'recommendations ('+
    'id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,'+
    'user_id BIGINT UNSIGNED NOT NULL,'+
    'person_id BIGINT UNSIGNED NULL,'+
    'rule_key VARCHAR(80) NOT NULL,'+
    'title VARCHAR(180) NOT NULL,'+
    'message LONGTEXT NOT NULL,'+
    'severity VARCHAR(20) NOT NULL DEFAULT \'info\','+
    'confidence DECIMAL(5,2) NOT NULL DEFAULT 0,'+
    'evidence_json LONGTEXT NULL,'+
    'source_key VARCHAR(80) NULL,'+
    'status VARCHAR(20) NOT NULL DEFAULT \'active\','+
    'created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,'+
    'updated_at DATETIME NULL,'+
    'PRIMARY KEY(id), KEY idx_reco_user_status(user_id,status), KEY idx_reco_person(person_id)'+
    ') ENGINE=InnoDB DEFAULT CHARSET=utf8mb4'
  );
  await conn.query(
    'CREATE TABLE IF NOT EXISTS '+p+'ai_feedback ('+
    'id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,'+
    'user_id BIGINT UNSIGNED NOT NULL,'+
    'recommendation_id BIGINT UNSIGNED NULL,'+
    'feedback VARCHAR(20) NOT NULL,'+
    'comment VARCHAR(500) NULL,'+
    'created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,'+
    'PRIMARY KEY(id), KEY idx_feedback_user(user_id), KEY idx_feedback_reco(recommendation_id)'+
    ') ENGINE=InnoDB DEFAULT CHARSET=utf8mb4'
  );
  await conn.query(
    'CREATE TABLE IF NOT EXISTS '+p+'knowledge_sources ('+
    'id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,'+
    'source_key VARCHAR(80) NOT NULL,'+
    'name VARCHAR(180) NOT NULL,'+
    'url VARCHAR(500) NOT NULL,'+
    'authority VARCHAR(80) NOT NULL,'+
    'active TINYINT(1) NOT NULL DEFAULT 1,'+
    'created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,'+
    'updated_at DATETIME NULL,'+
    'PRIMARY KEY(id), UNIQUE KEY uq_knowledge_source(source_key)'+
    ') ENGINE=InnoDB DEFAULT CHARSET=utf8mb4'
  );
  await conn.query(
    'CREATE TABLE IF NOT EXISTS '+p+'knowledge_items ('+
    'id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,'+
    'source_id BIGINT UNSIGNED NOT NULL,'+
    'item_key VARCHAR(100) NOT NULL,'+
    'title VARCHAR(200) NOT NULL,'+
    'summary LONGTEXT NOT NULL,'+
    'topic VARCHAR(80) NOT NULL,'+
    'active TINYINT(1) NOT NULL DEFAULT 1,'+
    'reviewed_at DATETIME NULL,'+
    'created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,'+
    'PRIMARY KEY(id), UNIQUE KEY uq_knowledge_item(source_id,item_key), KEY idx_knowledge_topic(topic)'+
    ') ENGINE=InnoDB DEFAULT CHARSET=utf8mb4'
  );

  const sources = [
    ['bcb_educacao','Banco Central do Brasil · Educação Financeira','https://www.bcb.gov.br/meubc/faqs/p/qual-a-diferenca-entre-educacao-financeira-e-cidadania-financeira','BCB'],
    ['bcb_orcamento','Banco Central do Brasil · Orçamento Pessoal','https://www.bcb.gov.br/meubc/faqs/p/o-que-e-um-orcamento-pessoal','BCB'],
    ['cvm_planejamento','Portal do Investidor · Guia de Planejamento Financeiro','https://www.gov.br/investidor/pt-br/educacional/publicacoes-educacionais/guias/guia-de-planejamento-financeiro','CVM'],
    ['cvm_educacao','CVM · Política de Educação Financeira','https://www.gov.br/investidor/pt-br/educacional/politica-de-educacao-financeira','CVM']
  ];
  for (const src of sources) {
    await conn.execute(
      'INSERT INTO '+p+'knowledge_sources(source_key,name,url,authority,active) VALUES(?,?,?,?,1) '+
      'ON DUPLICATE KEY UPDATE name=VALUES(name),url=VALUES(url),authority=VALUES(authority),active=1,updated_at=NOW()',
      src
    );
  }
  const knowledge = [
    ['bcb_orcamento','budget_income_expense','Orçamento acompanha entradas e gastos','Orçamento pessoal deve mostrar quanto se ganha, quanto se gasta e com o que se gasta.','orcamento'],
    ['bcb_educacao','conscious_decisions','Decisões financeiras conscientes','Educação financeira busca desenvolver conhecimentos, habilidades, atitudes e comportamentos para decisões financeiras conscientes e bem-estar financeiro.','educacao'],
    ['cvm_planejamento','periodic_review','Planejamento deve ser revisto','O planejamento financeiro deve ser acompanhado e revisto periodicamente porque objetivos, prioridades e condições econômicas mudam.','planejamento'],
    ['cvm_educacao','informed_decisions','Informação e proteção do investidor','Educação financeira deve apoiar decisões conscientes e bem informadas, com foco em letramento, bem-estar e proteção do investidor.','investimentos']
  ];
  for (const item of knowledge) {
    const [sr] = await conn.execute('SELECT id FROM '+p+'knowledge_sources WHERE source_key=? LIMIT 1',[item[0]]);
    if (sr[0]) {
      await conn.execute(
        'INSERT INTO '+p+'knowledge_items(source_id,item_key,title,summary,topic,active,reviewed_at) VALUES(?,?,?,?,?,1,NOW()) '+
        'ON DUPLICATE KEY UPDATE title=VALUES(title),summary=VALUES(summary),topic=VALUES(topic),active=1,reviewed_at=NOW()',
        [sr[0].id,item[1],item[2],item[3],item[4]]
      );
    }
  }

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
