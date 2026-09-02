const { app, BrowserWindow, ipcMain, safeStorage, dialog } = require('electron');
const path = require('path');
const fs = require('fs');
const mysql = require('mysql2/promise');
const http = require('http');
const os = require('os');
const crypto = require('crypto');
const QRCode = require('qrcode');

let win = null;
let pool = null;
let lanServer = null;
const lanSessions = new Map();
const loginAttempts = new Map();

function configPath() { return path.join(app.getPath('userData'), 'db-config.json'); }
function cleanPrefix(p) { return /^[A-Za-z0-9_]{1,32}$/.test(String(p || '')) ? String(p) : 'granaok_'; }

function loadStoredConfig() {
  try {
    const raw = JSON.parse(fs.readFileSync(configPath(), 'utf8'));
    let password = '';
    if (raw.password_enc && safeStorage.isEncryptionAvailable()) {
      password = safeStorage.decryptString(Buffer.from(raw.password_enc, 'base64'));
    }
    return Object.assign({}, raw, { password: password });
  } catch (_) {
    return null;
  }
}
function publicConfig() {
  const c = loadStoredConfig();
  if (!c) return { configured:false, host:'', port:3306, database:'', user:'', ssl:false, prefix:'granaok_', hasPassword:false, lan_enabled:false, lan_port:8787 };
  return {
    configured: !!(c.host && c.database && c.user && c.password),
    host: c.host || '', port: Number(c.port || 3306), database: c.database || '',
    user: c.user || '', ssl: !!c.ssl, prefix: cleanPrefix(c.prefix), hasPassword: !!c.password,
    lan_enabled: !!c.lan_enabled, lan_port: Number(c.lan_port || 8787)
  };
}
function saveStoredConfig(input) {
  const prev = loadStoredConfig() || {};
  const password = String(input.password || prev.password || '');
  if (!safeStorage.isEncryptionAvailable()) throw new Error('A criptografia segura do Windows não está disponível.');
  const out = {
    host: String(input.host || '').trim(),
    port: Number(input.port || 3306),
    database: String(input.database || '').trim(),
    user: String(input.user || '').trim(),
    ssl: !!input.ssl,
    prefix: cleanPrefix(input.prefix || 'granaok_'),
    lan_enabled: input.lan_enabled !== undefined ? !!input.lan_enabled : !!prev.lan_enabled,
    lan_port: Number(input.lan_port || prev.lan_port || 8787),
    password_enc: safeStorage.encryptString(password).toString('base64')
  };
  if (!out.host || !out.database || !out.user || !password) throw new Error('Preencha host, banco, usuário e senha.');
  fs.mkdirSync(path.dirname(configPath()), { recursive:true });
  fs.writeFileSync(configPath(), JSON.stringify(out, null, 2), 'utf8');
  if (pool) { pool.end().catch(()=>{}); pool = null; }
  return out;
}
function mysqlOptions(c) {
  const o = {
    host:c.host, port:Number(c.port||3306), database:c.database, user:c.user, password:c.password,
    waitForConnections:true, connectionLimit:4, queueLimit:0, connectTimeout:8000,
    enableKeepAlive:true, keepAliveInitialDelay:0, charset:'utf8mb4'
  };
  if (c.ssl) o.ssl = { rejectUnauthorized:false };
  return o;
}
async function getPool() {
  if (pool) return pool;
  const c = loadStoredConfig();
  if (!c || !c.host || !c.database || !c.user || !c.password) throw new Error('Banco de dados ainda não configurado.');
  pool = mysql.createPool(mysqlOptions(c));
  const conn = await pool.getConnection();
  try { await conn.query('SELECT 1'); } finally { conn.release(); }
  return pool;
}
async function testConfig(c) {
  const tmp = mysql.createPool(mysqlOptions(c));
  try {
    const conn = await tmp.getConnection();
    try {
      const [rows] = await conn.query('SELECT VERSION() version, DATABASE() db');
      return { ok:true, version:rows[0] && rows[0].version, database:rows[0] && rows[0].db };
    } finally { conn.release(); }
  } finally { await tmp.end(); }
}
async function withConn(fn) {
  const p = await getPool();
  const c = await p.getConnection();
  try { return await fn(c); } finally { c.release(); }
}
async function addColumn(conn, table, col, def) {
  try { await conn.query('ALTER TABLE ' + table + ' ADD COLUMN ' + col + ' ' + def); }
  catch (e) { if (Number(e.errno) !== 1060) throw e; }
}
async function ensureSchema(conn) {
  const p = cleanPrefix((loadStoredConfig() || {}).prefix);
  const qs = [
    'CREATE TABLE IF NOT EXISTS '+p+'people (id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,name VARCHAR(120) NOT NULL,active TINYINT(1) NOT NULL DEFAULT 1,created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,PRIMARY KEY(id)) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4',
    'CREATE TABLE IF NOT EXISTS '+p+'accounts (id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,person_id BIGINT UNSIGNED NULL,name VARCHAR(120) NOT NULL,type VARCHAR(40) NOT NULL DEFAULT \'bank\',initial_balance DECIMAL(14,2) NOT NULL DEFAULT 0,current_balance DECIMAL(14,2) NOT NULL DEFAULT 0,active TINYINT(1) NOT NULL DEFAULT 1,created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,PRIMARY KEY(id)) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4',
    'CREATE TABLE IF NOT EXISTS '+p+'categories (id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,name VARCHAR(120) NOT NULL,kind VARCHAR(20) NOT NULL DEFAULT \'expense\',active TINYINT(1) NOT NULL DEFAULT 1,created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,PRIMARY KEY(id),UNIQUE KEY uq_cat(name,kind)) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4',
    'CREATE TABLE IF NOT EXISTS '+p+'transactions (id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,person_id BIGINT UNSIGNED NULL,account_id BIGINT UNSIGNED NULL,category_id BIGINT UNSIGNED NULL,type VARCHAR(20) NOT NULL,description VARCHAR(255) NOT NULL,amount DECIMAL(14,2) NOT NULL,due_date DATE NOT NULL,paid_date DATE NULL,status VARCHAR(20) NOT NULL DEFAULT \'pending\',source VARCHAR(30) NOT NULL DEFAULT \'manual\',created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,PRIMARY KEY(id),KEY idx_due(due_date)) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4',
    'CREATE TABLE IF NOT EXISTS '+p+'cards (id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,person_id BIGINT UNSIGNED NULL,name VARCHAR(120) NOT NULL,closing_day INT NOT NULL DEFAULT 1,due_day INT NOT NULL DEFAULT 10,limit_amount DECIMAL(14,2) NOT NULL DEFAULT 0,active TINYINT(1) NOT NULL DEFAULT 1,created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,PRIMARY KEY(id)) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4',
    'CREATE TABLE IF NOT EXISTS '+p+'card_invoices (id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,card_id BIGINT UNSIGNED NOT NULL,reference_month DATE NOT NULL,due_date DATE NOT NULL,amount DECIMAL(14,2) NOT NULL DEFAULT 0,status VARCHAR(20) NOT NULL DEFAULT \'open\',created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,PRIMARY KEY(id),KEY idx_card_ref(card_id,reference_month)) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4',
    'CREATE TABLE IF NOT EXISTS '+p+'card_purchases (id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,card_id BIGINT UNSIGNED NOT NULL,person_id BIGINT UNSIGNED NULL,category_id BIGINT UNSIGNED NULL,description VARCHAR(255) NOT NULL,purchase_date DATE NOT NULL,amount DECIMAL(14,2) NOT NULL,invoice_month DATE NOT NULL,due_date DATE NOT NULL,installment_group VARCHAR(64) NULL,installment_number INT NOT NULL DEFAULT 1,installment_total INT NOT NULL DEFAULT 1,observations LONGTEXT NULL,created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,PRIMARY KEY(id),KEY idx_cp_card_due(card_id,due_date)) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4',
    'CREATE TABLE IF NOT EXISTS '+p+'financings (id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,name VARCHAR(160) NOT NULL,total_amount DECIMAL(14,2) NOT NULL DEFAULT 0,installment_amount DECIMAL(14,2) NOT NULL DEFAULT 0,total_installments INT NOT NULL DEFAULT 0,paid_installments INT NOT NULL DEFAULT 0,active TINYINT(1) NOT NULL DEFAULT 1,created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,PRIMARY KEY(id)) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4',
    'CREATE TABLE IF NOT EXISTS '+p+'financing_installments (id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,financing_id BIGINT UNSIGNED NOT NULL,installment_number INT NOT NULL,amount DECIMAL(14,2) NOT NULL,due_date DATE NOT NULL,status VARCHAR(20) NOT NULL DEFAULT \'pending\',paid_date DATE NULL,created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,PRIMARY KEY(id),UNIQUE KEY uq_fin_inst(financing_id,installment_number)) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4',
    'CREATE TABLE IF NOT EXISTS '+p+'app_users (id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,username VARCHAR(80) NOT NULL,display_name VARCHAR(120) NOT NULL,password_hash VARCHAR(255) NOT NULL,role VARCHAR(20) NOT NULL DEFAULT \'user\',person_id BIGINT UNSIGNED NULL,active TINYINT(1) NOT NULL DEFAULT 1,created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,last_login_at DATETIME NULL,PRIMARY KEY(id),UNIQUE KEY uq_app_user_username(username),KEY idx_app_user_person(person_id)) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4'
  ];
  for (const q of qs) await conn.query(q);
  await addColumn(conn,p+'people','entity_kind',"VARCHAR(20) NOT NULL DEFAULT 'person'");
  await addColumn(conn,p+'people','partner_name','VARCHAR(120) NULL');
  await addColumn(conn,p+'accounts','bank_code','VARCHAR(40) NULL');
  await addColumn(conn,p+'transactions','observations','LONGTEXT NULL');
  await addColumn(conn,p+'transactions','installment_group','VARCHAR(64) NULL');
  await addColumn(conn,p+'transactions','installment_number','INT NOT NULL DEFAULT 1');
  await addColumn(conn,p+'transactions','installment_total','INT NOT NULL DEFAULT 1');
  await addColumn(conn,p+'card_invoices','paid_date','DATE NULL');
  await addColumn(conn,p+'financings','next_due_date','DATE NULL');
  await addColumn(conn,p+'financings','last_paid_date','DATE NULL');
  return p;
}
function monthOk(v) { return /^\d{4}-(0[1-9]|1[0-2])$/.test(String(v||'')) ? String(v) : new Date().toISOString().slice(0,7); }
function dateOk(v) { if (!/^\d{4}-\d{2}-\d{2}$/.test(String(v||''))) throw new Error('Data inválida.'); return String(v); }
function money(v) { let s=String(v==null?'0':v).trim(); if (s.indexOf(',')>=0) s=s.replace(/\./g,'').replace(',','.'); const n=Math.abs(Number(s)); return Number.isFinite(n)?Math.round(n*100)/100:0; }
function addMonths(dateStr,n) { const d=new Date(dateStr+'T12:00:00'); d.setMonth(d.getMonth()+n); return d.toISOString().slice(0,10); }
function invoiceDue(month,dueDay) { const d=new Date(month+'-01T12:00:00'); d.setDate(Math.min(Math.max(1,Number(dueDay||10)), new Date(d.getFullYear(),d.getMonth()+1,0).getDate())); return d.toISOString().slice(0,10); }
async function categoryId(conn,p,name,kind) {
  name=String(name||'Outros').trim()||'Outros';
  await conn.execute('INSERT IGNORE INTO '+p+'categories(name,kind,active) VALUES(?,?,1)',[name,kind]);
  const [r]=await conn.execute('SELECT id FROM '+p+'categories WHERE name=? AND kind=? LIMIT 1',[name,kind]);
  return r[0] ? Number(r[0].id) : null;
}
async function syncInvoice(conn,p,cardId,month,dueDate) {
  const [sum]=await conn.execute('SELECT COALESCE(SUM(amount),0) total FROM '+p+'card_purchases WHERE card_id=? AND DATE_FORMAT(due_date,\'%Y-%m\')=?',[cardId,month]);
  const total=Number(sum[0].total||0);
  const [rows]=await conn.execute('SELECT id FROM '+p+'card_invoices WHERE card_id=? AND DATE_FORMAT(reference_month,\'%Y-%m\')=? ORDER BY id LIMIT 1',[cardId,month]);
  if (rows[0]) await conn.execute('UPDATE '+p+'card_invoices SET amount=?,due_date=? WHERE id=?',[total,dueDate,rows[0].id]);
  else await conn.execute('INSERT INTO '+p+'card_invoices(card_id,reference_month,due_date,amount,status) VALUES(?,?,?,?,\'open\')',[cardId,month+'-01',dueDate,total]);
}


function updateStoredMeta(patch) {
  const file = configPath();
  let raw = {};
  try { raw = JSON.parse(fs.readFileSync(file, 'utf8')); } catch (_) {}
  Object.assign(raw, patch || {});
  fs.mkdirSync(path.dirname(file), { recursive:true });
  fs.writeFileSync(file, JSON.stringify(raw, null, 2), 'utf8');
}

function hashAppPassword(password) {
  password = String(password || '');
  if (password.length < 8) throw new Error('A senha precisa ter pelo menos 8 caracteres.');
  const salt = crypto.randomBytes(16);
  const hash = crypto.scryptSync(password, salt, 64);
  return 'scrypt$' + salt.toString('hex') + '$' + hash.toString('hex');
}
function verifyAppPassword(password, stored) {
  try {
    const parts = String(stored || '').split('$');
    if (parts.length !== 3 || parts[0] !== 'scrypt') return false;
    const salt = Buffer.from(parts[1], 'hex');
    const expected = Buffer.from(parts[2], 'hex');
    const actual = crypto.scryptSync(String(password || ''), salt, expected.length);
    return expected.length === actual.length && crypto.timingSafeEqual(expected, actual);
  } catch (_) { return false; }
}
function cleanUsername(v) {
  const u = String(v || '').trim();
  if (!/^[A-Za-z0-9._-]{3,40}$/.test(u)) throw new Error('Use um usuário de 3 a 40 caracteres: letras, números, ponto, traço ou _.');
  return u;
}
function localIPv4() {
  const out = [];
  const nets = os.networkInterfaces();
  for (const list of Object.values(nets)) {
    for (const n of (list || [])) {
      const family = typeof n.family === 'string' ? n.family : (n.family === 4 ? 'IPv4' : '');
      if (family === 'IPv4' && !n.internal && n.address) out.push(n.address);
    }
  }
  return [...new Set(out)];
}
function lanStatus() {
  const cfg = publicConfig();
  const port = Number(cfg.lan_port || 8787);
  const running = !!lanServer;
  return { ok:true, running, enabled:!!cfg.lan_enabled, port, urls: running ? localIPv4().map(ip=>'http://'+ip+':'+port) : [] };
}
function parseCookies(req) {
  const out = {};
  String(req.headers.cookie || '').split(';').forEach(part=>{
    const i=part.indexOf('=');
    if(i>0) out[part.slice(0,i).trim()] = decodeURIComponent(part.slice(i+1).trim());
  });
  return out;
}
function sessionUser(req) {
  const token = parseCookies(req).gk_session;
  if (!token) return null;
  const s = lanSessions.get(token);
  if (!s) return null;
  if (s.expiresAt < Date.now()) { lanSessions.delete(token); return null; }
  s.expiresAt = Date.now() + 12*60*60*1000;
  return s;
}
function secureHeaders(res, contentType) {
  res.setHeader('Content-Type', contentType || 'application/json; charset=utf-8');
  res.setHeader('Cache-Control','no-store');
  res.setHeader('X-Content-Type-Options','nosniff');
  res.setHeader('X-Frame-Options','DENY');
  res.setHeader('Referrer-Policy','no-referrer');
  res.setHeader('Content-Security-Policy',"default-src 'self'; style-src 'self' 'unsafe-inline'; script-src 'self'; img-src 'self' data:; connect-src 'self'; frame-ancestors 'none'");
}
function jsonResponse(res, status, payload) {
  secureHeaders(res,'application/json; charset=utf-8');
  res.statusCode=status;
  res.end(JSON.stringify(payload));
}
function readBody(req) {
  return new Promise((resolve,reject)=>{
    let data=''; let size=0;
    req.on('data',chunk=>{ size+=chunk.length; if(size>1024*1024){reject(new Error('Requisição muito grande.')); req.destroy(); return;} data+=chunk.toString('utf8'); });
    req.on('end',()=>{ try{resolve(data?JSON.parse(data):{});}catch(_){reject(new Error('JSON inválido.'));} });
    req.on('error',reject);
  });
}
function staticLanFile(res, name, type) {
  const root = path.join(__dirname,'src','lan');
  const file = path.join(root,name);
  if (!file.startsWith(root) || !fs.existsSync(file)) { res.statusCode=404; return res.end('Not found'); }
  secureHeaders(res,type);
  res.end(fs.readFileSync(file));
}
function clientIp(req) { return String((req.socket && req.socket.remoteAddress) || 'unknown'); }
function loginAllowed(ip) {
  const now=Date.now(), item=loginAttempts.get(ip);
  if(!item || now-item.first>10*60*1000){loginAttempts.delete(ip);return true;}
  return item.count<5;
}
function registerFailedLogin(ip) {
  const now=Date.now(), item=loginAttempts.get(ip);
  if(!item || now-item.first>10*60*1000) loginAttempts.set(ip,{count:1,first:now});
  else { item.count++; loginAttempts.set(ip,item); }
}
async function handleLanRequest(req,res) {
  try {
    const url = new URL(req.url,'http://localhost');
    if(req.method==='GET' && url.pathname==='/') return staticLanFile(res,'index.html','text/html; charset=utf-8');
    if(req.method==='GET' && url.pathname==='/app.js') return staticLanFile(res,'app.js','application/javascript; charset=utf-8');
    if(req.method==='GET' && url.pathname==='/styles.css') return staticLanFile(res,'styles.css','text/css; charset=utf-8');
    if(req.method==='GET' && url.pathname==='/manifest.json') return staticLanFile(res,'manifest.json','application/manifest+json; charset=utf-8');

    if(url.pathname==='/api/session' && req.method==='GET') {
      const s=sessionUser(req);
      return jsonResponse(res,200,{ok:true,authenticated:!!s,user:s?{id:s.userId,username:s.username,display_name:s.displayName,role:s.role,person_id:s.personId}:null});
    }
    if(url.pathname==='/api/login' && req.method==='POST') {
      const ip=clientIp(req);
      if(!loginAllowed(ip)) return jsonResponse(res,429,{ok:false,error:'Muitas tentativas. Aguarde alguns minutos.'});
      const body=await readBody(req), username=String(body.username||'').trim(), password=String(body.password||'');
      const result=await withConn(async conn=>{
        const p=await ensureSchema(conn);
        const [all]=await conn.query('SELECT COUNT(*) c FROM '+p+'app_users WHERE active=1');
        if(!Number(all[0].c||0)) return {none:true};
        const [rows]=await conn.execute('SELECT id,username,display_name,password_hash,role,person_id,active FROM '+p+'app_users WHERE LOWER(username)=LOWER(?) LIMIT 1',[username]);
        return {user:rows[0]||null,p};
      });
      if(result.none) return jsonResponse(res,409,{ok:false,error:'Nenhum usuário de acesso foi criado. Crie o primeiro usuário no GranaOk Desktop.'});
      if(!result.user || !Number(result.user.active) || !verifyAppPassword(password,result.user.password_hash)) {
        registerFailedLogin(ip);
        return jsonResponse(res,401,{ok:false,error:'Usuário ou senha inválidos.'});
      }
      loginAttempts.delete(ip);
      const token=crypto.randomBytes(32).toString('hex');
      lanSessions.set(token,{userId:Number(result.user.id),username:result.user.username,displayName:result.user.display_name,role:result.user.role,personId:result.user.person_id?Number(result.user.person_id):null,expiresAt:Date.now()+12*60*60*1000});
      await withConn(async conn=>{const p=cleanPrefix((loadStoredConfig()||{}).prefix);await conn.execute('UPDATE '+p+'app_users SET last_login_at=NOW() WHERE id=?',[result.user.id]);});
      res.setHeader('Set-Cookie','gk_session='+token+'; HttpOnly; SameSite=Strict; Path=/; Max-Age=43200');
      return jsonResponse(res,200,{ok:true,user:{id:Number(result.user.id),username:result.user.username,display_name:result.user.display_name,role:result.user.role,person_id:result.user.person_id?Number(result.user.person_id):null}});
    }
    if(url.pathname==='/api/logout' && req.method==='POST') {
      const token=parseCookies(req).gk_session;if(token)lanSessions.delete(token);
      res.setHeader('Set-Cookie','gk_session=; HttpOnly; SameSite=Strict; Path=/; Max-Age=0');
      return jsonResponse(res,200,{ok:true});
    }
    if(url.pathname==='/api/action' && req.method==='POST') {
      const s=sessionUser(req);
      if(!s) return jsonResponse(res,401,{ok:false,error:'Sessão expirada.'});
      if(String(req.headers['x-granaok-client']||'')!=='lan') return jsonResponse(res,403,{ok:false,error:'Cliente inválido.'});
      const body=await readBody(req), requested=String(body.action||'');
      const allowed=new Set(['dashboard','context','transactions','transaction_save','transaction_status','account_save','person_add','category_add','card_save','card_purchase_add','invoice','invoice_pay','invoice_reopen','financings','financing_pay']);
      if(!allowed.has(requested)) return jsonResponse(res,403,{ok:false,error:'Operação não permitida no acesso móvel.'});
      const result=await action(requested,body.payload||{});
      return jsonResponse(res,200,Object.assign({ok:true},result||{}));
    }
    res.statusCode=404;secureHeaders(res,'text/plain; charset=utf-8');res.end('Não encontrado');
  } catch(e) {
    jsonResponse(res,500,{ok:false,error:String(e&&e.message?e.message:e)});
  }
}
async function startLanServer(port, persist=true) {
  port=Math.max(1024,Math.min(65535,Number(port||8787)));
  if(lanServer) {
    const current=lanServer.address(); if(current && Number(current.port)===port) return lanStatus();
    await stopLanServer(false);
  }
  await new Promise((resolve,reject)=>{
    const server=http.createServer((req,res)=>{handleLanRequest(req,res);});
    server.on('error',reject);
    server.listen(port,'0.0.0.0',()=>{lanServer=server;resolve();});
  });
  if(persist) updateStoredMeta({lan_enabled:true,lan_port:port});
  return lanStatus();
}
async function stopLanServer(persist=true) {
  if(lanServer){const s=lanServer;lanServer=null;await new Promise(resolve=>s.close(()=>resolve()));}
  lanSessions.clear();
  if(persist) updateStoredMeta({lan_enabled:false});
  return lanStatus();
}

async function action(name,a) {
  const aliases = {transactions:'transactions:list',transaction_save:'transaction:save',transaction_status:'transaction:status',account_save:'account:save',person_add:'person:add',category_add:'category:add',card_save:'card:save',card_purchase_add:'card:purchase',invoice:'invoice:get',invoice_pay:'invoice:pay',invoice_reopen:'invoice:reopen',financings:'financings:list',financing_pay:'financing:pay'};
  name = aliases[name] || name;
  if (name === 'config:get') return publicConfig();
  if (name === 'config:test') {
    const prev=loadStoredConfig()||{};
    const c={host:String(a.host||prev.host||'').trim(),port:Number(a.port||prev.port||3306),database:String(a.database||prev.database||'').trim(),user:String(a.user||prev.user||'').trim(),password:String(a.password||prev.password||''),ssl:!!a.ssl,prefix:cleanPrefix(a.prefix||prev.prefix||'granaok_')};
    return testConfig(c);
  }
  if (name === 'config:save') {
    const prev=loadStoredConfig()||{};
    const c={host:String(a.host||prev.host||'').trim(),port:Number(a.port||prev.port||3306),database:String(a.database||prev.database||'').trim(),user:String(a.user||prev.user||'').trim(),password:String(a.password||prev.password||''),ssl:!!a.ssl,prefix:cleanPrefix(a.prefix||prev.prefix||'granaok_')};
    await testConfig(c); saveStoredConfig(c);
    await withConn(async conn=>{await ensureSchema(conn);});
    return {ok:true,message:'Conexão salva com segurança no Windows.'};
  }
  if (name === 'lan:status') return lanStatus();
  if (name === 'lan:start') return startLanServer(Number(a.port||8787),true);
  if (name === 'lan:stop') return stopLanServer(true);
  if (name === 'lan:qr') {
    const st=lanStatus(); const url=String(a.url||st.urls[0]||'');
    if(!url) throw new Error('Inicie o acesso pelo celular primeiro.');
    return {ok:true,url,qr:await QRCode.toDataURL(url,{margin:1,width:280})};
  }
  if (name === 'users:list') {
    return withConn(async conn=>{
      const p=await ensureSchema(conn);
      const [rows]=await conn.query("SELECT u.id,u.username,u.display_name,u.role,u.person_id,u.active,DATE_FORMAT(u.created_at,'%Y-%m-%d %H:%i') created_at,CASE WHEN u.last_login_at IS NULL THEN NULL ELSE DATE_FORMAT(u.last_login_at,'%Y-%m-%d %H:%i') END last_login_at,COALESCE(pe.name,'') person_name FROM "+p+"app_users u LEFT JOIN "+p+"people pe ON pe.id=u.person_id ORDER BY u.active DESC,u.display_name,u.username");
      return {ok:true,rows};
    });
  }
  if (name === 'user:add') {
    return withConn(async conn=>{
      const p=await ensureSchema(conn), username=cleanUsername(a.username), display=String(a.display_name||username).trim()||username, role=a.role==='admin'?'admin':'user', personId=Number(a.person_id||0)||null, hash=hashAppPassword(a.password);
      try { await conn.execute('INSERT INTO '+p+'app_users(username,display_name,password_hash,role,person_id,active) VALUES(?,?,?,?,?,1)',[username,display,hash,role,personId]); }
      catch(e){ if(Number(e.errno)===1062) throw new Error('Esse nome de usuário já existe.'); throw e; }
      return {ok:true,message:'Usuário criado.'};
    });
  }
  if (name === 'user:password') {
    return withConn(async conn=>{
      const p=await ensureSchema(conn), id=Number(a.id||0); if(!id) throw new Error('Usuário inválido.');
      await conn.execute('UPDATE '+p+'app_users SET password_hash=? WHERE id=?',[hashAppPassword(a.password),id]); return {ok:true,message:'Senha alterada.'};
    });
  }
  if (name === 'user:toggle') {
    return withConn(async conn=>{
      const p=await ensureSchema(conn), id=Number(a.id||0), active=a.active?1:0;if(!id)throw new Error('Usuário inválido.');
      if(!active){
        const [target]=await conn.execute('SELECT role FROM '+p+'app_users WHERE id=? LIMIT 1',[id]);
        if(target[0]&&target[0].role==='admin'){
          const [admins]=await conn.query("SELECT COUNT(*) c FROM "+p+"app_users WHERE role='admin' AND active=1 AND id<>"+Number(id));
          if(Number(admins[0].c||0)<1) throw new Error('Mantenha pelo menos um administrador ativo.');
        }
      }
      await conn.execute('UPDATE '+p+'app_users SET active=? WHERE id=?',[active,id]); return {ok:true,message:active?'Usuário ativado.':'Usuário desativado.'};
    });
  }

  if (name === 'backup:save') {
    const p=await getPool(); const c=loadStoredConfig(); const prefix=cleanPrefix(c.prefix);
    const tables=['people','accounts','categories','transactions','cards','card_invoices','card_purchases','financings','financing_installments','app_users'];
    const out={version:1,created_at:new Date().toISOString(),tables:{}};
    for(const t of tables){try{const [rows]=await p.query('SELECT * FROM '+prefix+t);out.tables[t]=rows;}catch(_){out.tables[t]=[];}}
    const r=await dialog.showSaveDialog(win,{title:'Salvar backup do GranaOk',defaultPath:'GranaOk-Backup-'+new Date().toISOString().slice(0,10)+'.json',filters:[{name:'JSON',extensions:['json']}]});
    if(r.canceled||!r.filePath)return {ok:false,canceled:true};
    fs.writeFileSync(r.filePath,JSON.stringify(out,null,2),'utf8'); return {ok:true,file:r.filePath};
  }

  return withConn(async conn=>{
    const p=await ensureSchema(conn);

    if(name==='dashboard'){
      const m=monthOk(a.month), start=m+'-01', next=addMonths(start,1);
      const [[bal]]=await conn.query('SELECT COALESCE(SUM(current_balance),0) v FROM '+p+'accounts WHERE active=1');
      const [[tx]]=await conn.execute("SELECT COALESCE(SUM(CASE WHEN type='income' THEN amount ELSE 0 END),0) income,COALESCE(SUM(CASE WHEN type='expense' THEN amount ELSE 0 END),0) expenses,COALESCE(SUM(CASE WHEN type='expense' AND status='paid' THEN amount ELSE 0 END),0) paid_expenses,COALESCE(SUM(CASE WHEN type='expense' AND status<>'paid' THEN amount ELSE 0 END),0) pending_expenses FROM "+p+"transactions WHERE due_date>=? AND due_date<?",[start,next]);
      const [[iv]]=await conn.execute("SELECT COALESCE(SUM(amount),0) total,COALESCE(SUM(CASE WHEN status='paid' THEN amount ELSE 0 END),0) paid,COALESCE(SUM(CASE WHEN status<>'paid' THEN amount ELSE 0 END),0) pending FROM "+p+"card_invoices WHERE reference_month>=? AND reference_month<?",[start,next]);
      const [[ov]]=await conn.query("SELECT COUNT(*) c FROM "+p+"transactions WHERE type='expense' AND status<>'paid' AND due_date<CURDATE()");
      const [[fin]]=await conn.query('SELECT COALESCE(SUM(installment_amount),0) v FROM '+p+'financings WHERE active=1');
      const totalMonthlyExpenses=Number(tx.expenses||0)+Number(iv.total||0);
      return {ok:true,month:m,accounts_balance:Number(bal.v||0),income:Number(tx.income||0),expenses:Number(tx.expenses||0),paid_expenses:Number(tx.paid_expenses||0),pending_expenses:Number(tx.pending_expenses||0),card_invoices:Number(iv.total||0),card_paid:Number(iv.paid||0),card_pending:Number(iv.pending||0),total_monthly_expenses:totalMonthlyExpenses,overdue_count:Number(ov.c||0),financing_monthly:Number(fin.v||0),projected:Number(bal.v||0)+Number(tx.income||0)-totalMonthlyExpenses};
    }

    if(name==='context'){
      const [people]=await conn.query("SELECT id,name,COALESCE(entity_kind,'person') kind,COALESCE(partner_name,'') partner_name,active FROM "+p+"people ORDER BY active DESC,name");
      const [accounts]=await conn.query("SELECT a.id,a.person_id,a.name,a.type,a.initial_balance,a.current_balance,a.active,COALESCE(a.bank_code,'other') bank_code,COALESCE(pe.name,'') person_name FROM "+p+"accounts a LEFT JOIN "+p+"people pe ON pe.id=a.person_id ORDER BY a.active DESC,a.name");
      const [categories]=await conn.query('SELECT id,name,kind,active FROM '+p+'categories ORDER BY kind,name');
      const [cards]=await conn.query("SELECT c.id,c.person_id,c.name,c.closing_day,c.due_day,c.limit_amount,c.active,COALESCE(pe.name,'') person_name FROM "+p+"cards c LEFT JOIN "+p+"people pe ON pe.id=c.person_id ORDER BY c.active DESC,c.name");
      return {ok:true,people,accounts,categories,cards};
    }

    if(name==='transactions:list'){
      const m=monthOk(a.month), monthStart=m+'-01', monthNext=addMonths(monthStart,1);
      const type=['income','expense'].includes(a.type)?a.type:'';
      const status=['paid','pending','overdue'].includes(a.status)?a.status:'';
      const description=String(a.search||'').trim();
      const observation=String(a.observation||'').trim();
      const source=String(a.source||'').trim();
      const accountId=Number(a.account_id||0);
      const personId=Number(a.person_id||0);
      const categoryId=Number(a.category_id||0);
      const minAmount=(a.min_amount===null||a.min_amount===undefined||String(a.min_amount).trim()==='')?null:money(a.min_amount);
      const maxAmount=(a.max_amount===null||a.max_amount===undefined||String(a.max_amount).trim()==='')?null:money(a.max_amount);
      const dateFrom=/^\d{4}-\d{2}-\d{2}$/.test(String(a.date_from||''))?String(a.date_from):'';
      const dateTo=/^\d{4}-\d{2}-\d{2}$/.test(String(a.date_to||''))?String(a.date_to):'';
      const installments=['cash','installment'].includes(a.installments)?a.installments:'';
      const orderMap={
        'date_asc':'t.due_date ASC,t.id ASC',
        'date_desc':'t.due_date DESC,t.id DESC',
        'amount_desc':'t.amount DESC,t.due_date ASC',
        'amount_asc':'t.amount ASC,t.due_date ASC',
        'description_asc':'t.description ASC,t.due_date ASC'
      };
      const orderBy=orderMap[a.order]||orderMap.date_asc;
      const effective="CASE WHEN t.status='paid' THEN 'paid' WHEN t.status='overdue' THEN 'overdue' WHEN t.status='pending' AND t.due_date<CURDATE() THEN 'overdue' ELSE t.status END";

      let sql="SELECT t.id,t.person_id,t.account_id,t.category_id,t.type,t.description,t.amount,DATE_FORMAT(t.due_date,'%Y-%m-%d') due_date,CASE WHEN t.paid_date IS NULL THEN NULL ELSE DATE_FORMAT(t.paid_date,'%Y-%m-%d') END paid_date,t.status,"+effective+" effective_status,COALESCE(c.name,'Outros') category,COALESCE(ac.name,'') account_name,COALESCE(pe.name,'') person_name,COALESCE(t.observations,'') observations,COALESCE(t.source,'') source,COALESCE(t.installment_number,1) installment_number,COALESCE(t.installment_total,1) installment_total FROM "+p+"transactions t LEFT JOIN "+p+"categories c ON c.id=t.category_id LEFT JOIN "+p+"accounts ac ON ac.id=t.account_id LEFT JOIN "+p+"people pe ON pe.id=t.person_id WHERE t.due_date>=? AND t.due_date<?";
      const params=[monthStart,monthNext];

      if(type){sql+=" AND t.type=?";params.push(type);}
      if(status){sql+=" AND "+effective+"=?";params.push(status);}
      if(description){sql+=" AND t.description LIKE ?";params.push('%'+description+'%');}
      if(observation){sql+=" AND COALESCE(t.observations,'') LIKE ?";params.push('%'+observation+'%');}
      if(source){sql+=" AND COALESCE(t.source,'') LIKE ?";params.push('%'+source+'%');}
      if(accountId>0){sql+=" AND t.account_id=?";params.push(accountId);}
      if(personId>0){sql+=" AND t.person_id=?";params.push(personId);}
      if(categoryId>0){sql+=" AND t.category_id=?";params.push(categoryId);}
      if(minAmount!==null){sql+=" AND t.amount>=?";params.push(minAmount);}
      if(maxAmount!==null){sql+=" AND t.amount<=?";params.push(maxAmount);}
      if(dateFrom){sql+=" AND t.due_date>=?";params.push(dateFrom);}
      if(dateTo){sql+=" AND t.due_date<=?";params.push(dateTo);}
      if(installments==='cash')sql+=" AND COALESCE(t.installment_total,1)<=1";
      if(installments==='installment')sql+=" AND COALESCE(t.installment_total,1)>1";
      sql+=" ORDER BY "+orderBy;

      const [rows]=await conn.execute(sql,params);
      return {ok:true,rows,month:m};
    }

    if(name==='transaction:save'){
      const id=Number(a.id||0), type=a.type==='income'?'income':'expense', desc=String(a.description||'').trim(), amt=money(a.amount), due=dateOk(a.due_date), status=['paid','pending','overdue'].includes(a.status)?a.status:'pending', obs=String(a.observations||''), person=Number(a.person_id||0)||null, account=Number(a.account_id||0)||null, cat=await categoryId(conn,p,a.category||'Outros',type);
      if(!desc||amt<=0)throw new Error('Informe descrição e valor.');
      if(id>0) await conn.execute('UPDATE '+p+'transactions SET person_id=?,account_id=?,category_id=?,type=?,description=?,amount=?,due_date=?,paid_date=?,status=?,observations=? WHERE id=?',[person,account,cat,type,desc,amt,due,status==='paid'?(a.paid_date||new Date().toISOString().slice(0,10)):null,status,obs,id]);
      else await conn.execute("INSERT INTO "+p+"transactions(person_id,account_id,category_id,type,description,amount,due_date,paid_date,status,source,observations) VALUES(?,?,?,?,?,?,?,?,?,'desktop',?)",[person,account,cat,type,desc,amt,due,status==='paid'?new Date().toISOString().slice(0,10):null,status,obs]);
      return {ok:true,message:id?'Lançamento atualizado.':'Lançamento criado.'};
    }

    if(name==='transaction:status'){
      const id=Number(a.id||0), s=['paid','pending','overdue'].includes(a.status)?a.status:null;if(!id||!s)throw new Error('Dados inválidos.');
      await conn.execute('UPDATE '+p+'transactions SET status=?,paid_date=? WHERE id=?',[s,s==='paid'?new Date().toISOString().slice(0,10):null,id]);return {ok:true};
    }

    if(name==='account:save'){
      const id=Number(a.id||0), namev=String(a.name||'').trim();if(!namev)throw new Error('Informe o nome da conta.');
      const vals=[Number(a.person_id||0)||null,namev,String(a.type||'checking'),money(a.initial_balance),money(a.current_balance),String(a.bank_code||'other')];
      if(id)await conn.execute('UPDATE '+p+'accounts SET person_id=?,name=?,type=?,initial_balance=?,current_balance=?,bank_code=? WHERE id=?',vals.concat([id]));
      else await conn.execute('INSERT INTO '+p+'accounts(person_id,name,type,initial_balance,current_balance,active,bank_code) VALUES(?,?,?,?,?,1,?)',vals);
      return {ok:true,message:'Conta salva.'};
    }

    if(name==='person:add'){
      const n=String(a.name||'').trim();if(!n)throw new Error('Informe o nome.');
      await conn.execute('INSERT INTO '+p+'people(name,active,entity_kind,partner_name) VALUES(?,1,?,?)',[n,a.kind==='couple'?'couple':'person',String(a.partner_name||'').trim()||null]);return {ok:true};
    }
    if(name==='category:add'){
      const n=String(a.name||'').trim();if(!n)throw new Error('Informe a categoria.');await conn.execute('INSERT IGNORE INTO '+p+'categories(name,kind,active) VALUES(?,?,1)',[n,a.kind==='income'?'income':'expense']);return {ok:true};
    }

    if(name==='card:save'){
      const n=String(a.name||'').trim();if(!n)throw new Error('Informe o cartão.');
      await conn.execute('INSERT INTO '+p+'cards(person_id,name,closing_day,due_day,limit_amount,active) VALUES(?,?,?,?,?,1)',[Number(a.person_id||0)||null,n,Math.max(1,Math.min(31,Number(a.closing_day||1))),Math.max(1,Math.min(31,Number(a.due_day||10))),money(a.limit_amount)]);return {ok:true};
    }

    if(name==='card:purchase'){
      const cardId=Number(a.card_id||0), desc=String(a.description||'').trim(), total=money(a.amount), purchase=dateOk(a.purchase_date), inst=Math.max(1,Math.min(60,Number(a.installments||1)));if(!cardId||!desc||total<=0)throw new Error('Confira cartão, descrição e valor.');
      const [[card]]=await conn.execute('SELECT closing_day,due_day,person_id FROM '+p+'cards WHERE id=? AND active=1',[cardId]);if(!card)throw new Error('Cartão não encontrado.');
      let first=new Date(purchase+'T12:00:00');const closing=Number(card.closing_day||1),due=Number(card.due_day||10);if(first.getDate()>closing)first.setMonth(first.getMonth()+1);if(due<=closing)first.setMonth(first.getMonth()+1);const firstMonth=first.toISOString().slice(0,7);
      const person=Number(a.person_id||0)||Number(card.person_id||0)||null, cat=await categoryId(conn,p,a.category||'Outros','expense'), group=inst>1?require('crypto').randomUUID():null, base=Math.floor((total/inst)*100)/100;let allocated=0;
      await conn.beginTransaction();try{
        for(let i=1;i<=inst;i++){const amount=i===inst?Math.round((total-allocated)*100)/100:base;allocated=Math.round((allocated+amount)*100)/100;const month=monthOk(firstMonth);const mdate=addMonths(month+'-01',i-1).slice(0,7), dueDate=invoiceDue(mdate,due);
          await conn.execute('INSERT INTO '+p+'card_purchases(card_id,person_id,category_id,description,purchase_date,amount,invoice_month,due_date,installment_group,installment_number,installment_total,observations) VALUES(?,?,?,?,?,?,?,?,?,?,?,?)',[cardId,person,cat,desc,purchase,amount,mdate+'-01',dueDate,group,i,inst,String(a.observations||'')]);
          await syncInvoice(conn,p,cardId,mdate,dueDate);
        } await conn.commit();
      }catch(e){await conn.rollback();throw e;}
      return {ok:true,month:firstMonth,message:'Compra incluída na fatura.'};
    }

    if(name==='invoice:get'){
      const cardId=Number(a.card_id||0),m=monthOk(a.month);const [[card]]=await conn.execute('SELECT id,name,closing_day,due_day,limit_amount FROM '+p+'cards WHERE id=?',[cardId]);if(!card)throw new Error('Cartão não encontrado.');
      const [rows]=await conn.execute("SELECT cp.id,cp.description,DATE_FORMAT(cp.purchase_date,'%Y-%m-%d') purchase_date,cp.amount,cp.installment_number,cp.installment_total,COALESCE(c.name,'Outros') category,COALESCE(cp.observations,'') observations FROM "+p+"card_purchases cp LEFT JOIN "+p+"categories c ON c.id=cp.category_id WHERE cp.card_id=? AND DATE_FORMAT(cp.due_date,'%Y-%m')=? ORDER BY cp.purchase_date,cp.id",[cardId,m]);
      const total=rows.reduce((s,r)=>s+Number(r.amount||0),0);const [ir]=await conn.execute("SELECT id,status,DATE_FORMAT(due_date,'%Y-%m-%d') due_date,CASE WHEN paid_date IS NULL THEN NULL ELSE DATE_FORMAT(paid_date,'%Y-%m-%d') END paid_date FROM "+p+"card_invoices WHERE card_id=? AND DATE_FORMAT(reference_month,'%Y-%m')=? ORDER BY id LIMIT 1",[cardId,m]);const inv=ir[0]||{};
      return {ok:true,card,rows,total,month:m,status:inv.status||'open',paid_date:inv.paid_date||null,due_date:inv.due_date||invoiceDue(m,card.due_day)};
    }

    if(name==='invoice:pay'||name==='invoice:reopen'){
      const cardId=Number(a.card_id||0),m=monthOk(a.month);if(!cardId)throw new Error('Cartão inválido.');
      const [ir]=await conn.execute("SELECT id FROM "+p+"card_invoices WHERE card_id=? AND DATE_FORMAT(reference_month,'%Y-%m')=? LIMIT 1",[cardId,m]);
      if(name==='invoice:reopen'){if(!ir[0])throw new Error('Fatura não encontrada.');await conn.execute("UPDATE "+p+"card_invoices SET status='open',paid_date=NULL WHERE id=?",[ir[0].id]);return {ok:true};}
      const paid=dateOk(a.paid_date||new Date().toISOString().slice(0,10));const [[card]]=await conn.execute('SELECT due_day FROM '+p+'cards WHERE id=?',[cardId]);const [[sum]]=await conn.execute("SELECT COALESCE(SUM(amount),0) total FROM "+p+"card_purchases WHERE card_id=? AND DATE_FORMAT(due_date,'%Y-%m')=?",[cardId,m]);const dueDate=invoiceDue(m,card?card.due_day:10), total=Number(sum.total||0);
      if(ir[0])await conn.execute("UPDATE "+p+"card_invoices SET amount=?,due_date=?,status='paid',paid_date=? WHERE id=?",[total,dueDate,paid,ir[0].id]);
      else await conn.execute("INSERT INTO "+p+"card_invoices(card_id,reference_month,due_date,amount,status,paid_date) VALUES(?,?,?,?, 'paid',?)",[cardId,m+'-01',dueDate,total,paid]);
      return {ok:true};
    }

    if(name==='financings:list'){
      const [rows]=await conn.query("SELECT id,name,total_amount,installment_amount,total_installments,paid_installments,active,CASE WHEN next_due_date IS NULL THEN NULL ELSE DATE_FORMAT(next_due_date,'%Y-%m-%d') END next_due_date,CASE WHEN last_paid_date IS NULL THEN NULL ELSE DATE_FORMAT(last_paid_date,'%Y-%m-%d') END last_paid_date FROM "+p+"financings ORDER BY active DESC,id DESC");return {ok:true,rows};
    }
    if(name==='financing:pay'){
      const id=Number(a.id||0),paid=dateOk(a.paid_date||new Date().toISOString().slice(0,10));if(!id)throw new Error('Financiamento inválido.');
      await conn.beginTransaction();try{
        const [rr]=await conn.execute("SELECT id,installment_number FROM "+p+"financing_installments WHERE financing_id=? AND status<>'paid' ORDER BY installment_number LIMIT 1 FOR UPDATE",[id]);if(!rr[0])throw new Error('Não há parcela pendente.');
        await conn.execute("UPDATE "+p+"financing_installments SET status='paid',paid_date=? WHERE id=?",[paid,rr[0].id]);const [[cnt]]=await conn.execute("SELECT COUNT(*) c FROM "+p+"financing_installments WHERE financing_id=? AND status='paid'",[id]);const [[f]]=await conn.execute('SELECT total_installments FROM '+p+'financings WHERE id=?',[id]);const [nx]=await conn.execute("SELECT due_date FROM "+p+"financing_installments WHERE financing_id=? AND status<>'paid' ORDER BY installment_number LIMIT 1",[id]);const paidN=Number(cnt.c||0),totalN=Number(f.total_installments||0);
        await conn.execute('UPDATE '+p+'financings SET paid_installments=?,last_paid_date=?,next_due_date=?,active=? WHERE id=?',[paidN,paid,nx[0]?nx[0].due_date:null,paidN<totalN?1:0,id]);await conn.commit();
      }catch(e){await conn.rollback();throw e;}return {ok:true};
    }
    throw new Error('Operação desconhecida.');
  });
}

function createWindow() {
  win = new BrowserWindow({
    width: 1280, height: 820, minWidth: 980, minHeight: 650,
    backgroundColor:'#f3f5f7',
    webPreferences:{ preload:path.join(__dirname,'preload.js'), nodeIntegration:false, contextIsolation:true, sandbox:false }
  });
  win.setMenuBarVisibility(false);
  win.loadFile(path.join(__dirname,'src','index.html'));
}
app.whenReady().then(()=>{
  ipcMain.handle('granaok:invoke', async (_event,name,payload)=>{
    try { const r=await action(String(name||''),payload||{}); return Object.assign({ok:true},r||{}); }
    catch(e){ return {ok:false,error:String(e && e.message ? e.message : e)}; }
  });
  createWindow();
  const cfg=publicConfig();
  if(cfg.configured && cfg.lan_enabled) startLanServer(cfg.lan_port||8787,false).catch(()=>{});
  app.on('activate',()=>{if(BrowserWindow.getAllWindows().length===0)createWindow();});
});
app.on('window-all-closed',()=>{if(process.platform!=='darwin')app.quit();});
app.on('before-quit',()=>{if(lanServer){try{lanServer.close();}catch(_){}} if(pool)pool.end().catch(()=>{});});
