const crypto = require('crypto');
const { config } = require('./config');
const { withConn, ensureSchema } = require('./db');

function cleanUsername(v) {
  const u = String(v || '').trim();
  if (!/^[A-Za-z0-9._-]{3,40}$/.test(u)) throw new Error('Usuário inválido.');
  return u;
}
function hashPassword(password) {
  password = String(password || '');
  if (password.length < 8) throw new Error('A senha precisa ter pelo menos 8 caracteres.');
  const salt = crypto.randomBytes(16);
  const hash = crypto.scryptSync(password, salt, 64);
  return 'scrypt$' + salt.toString('hex') + '$' + hash.toString('hex');
}
function verifyPassword(password, stored) {
  try {
    const [kind,saltHex,hashHex] = String(stored || '').split('$');
    if (kind !== 'scrypt' || !saltHex || !hashHex) return false;
    const expected = Buffer.from(hashHex,'hex');
    const actual = crypto.scryptSync(String(password || ''),Buffer.from(saltHex,'hex'),expected.length);
    return expected.length === actual.length && crypto.timingSafeEqual(expected,actual);
  } catch { return false; }
}
function tokenHash(token) { return crypto.createHash('sha256').update(token).digest('hex'); }
function addHoursSql(hours) { return Math.max(1,Math.min(168,Number(hours||12))); }

async function createSession(userId) {
  const token = crypto.randomBytes(32).toString('hex');
  const hash = tokenHash(token);
  await withConn(async conn => {
    const p = await ensureSchema(conn);
    await conn.query('DELETE FROM '+p+'web_sessions WHERE expires_at < NOW()');
    await conn.execute(
      'INSERT INTO '+p+'web_sessions(token_hash,user_id,expires_at,last_seen_at) VALUES(?,?,DATE_ADD(NOW(), INTERVAL '+addHoursSql(config.sessionHours)+' HOUR),NOW())',
      [hash,userId]
    );
  });
  return token;
}
async function destroySession(token) {
  if (!token) return;
  await withConn(async conn => {
    const p = await ensureSchema(conn);
    await conn.execute('DELETE FROM '+p+'web_sessions WHERE token_hash=?',[tokenHash(token)]);
  });
}
async function getSession(token) {
  if (!token) return null;
  return withConn(async conn => {
    const p = await ensureSchema(conn);
    const [rows] = await conn.execute(
      "SELECT s.id session_id,u.id,u.username,u.display_name,u.role,u.person_id,u.active "+
      "FROM "+p+"web_sessions s JOIN "+p+"app_users u ON u.id=s.user_id "+
      "WHERE s.token_hash=? AND s.expires_at>NOW() LIMIT 1",
      [tokenHash(token)]
    );
    const u = rows[0];
    if (!u || !Number(u.active)) return null;
    await conn.execute(
      'UPDATE '+p+'web_sessions SET last_seen_at=NOW(),expires_at=DATE_ADD(NOW(), INTERVAL '+addHoursSql(config.sessionHours)+' HOUR) WHERE id=?',
      [u.session_id]
    );
    return {id:Number(u.id),username:u.username,display_name:u.display_name,role:u.role,person_id:u.person_id?Number(u.person_id):null};
  });
}

async function authenticate(username,password) {
  return withConn(async conn => {
    const p = await ensureSchema(conn);
    const [rows] = await conn.execute(
      'SELECT id,username,display_name,password_hash,role,person_id,active FROM '+p+'app_users WHERE LOWER(username)=LOWER(?) LIMIT 1',
      [String(username||'').trim()]
    );
    const u = rows[0];
    if (!u || !Number(u.active) || !verifyPassword(password,u.password_hash)) return null;
    await conn.execute('UPDATE '+p+'app_users SET last_login_at=NOW() WHERE id=?',[u.id]);
    return {id:Number(u.id),username:u.username,display_name:u.display_name,role:u.role,person_id:u.person_id?Number(u.person_id):null};
  });
}

async function listUsers() {
  return withConn(async conn => {
    const p = await ensureSchema(conn);
    const [rows] = await conn.query(
      "SELECT u.id,u.username,u.display_name,u.role,u.person_id,u.active,DATE_FORMAT(u.created_at,'%Y-%m-%d %H:%i') created_at,"+
      "CASE WHEN u.last_login_at IS NULL THEN NULL ELSE DATE_FORMAT(u.last_login_at,'%Y-%m-%d %H:%i') END last_login_at,COALESCE(pe.name,'') person_name "+
      "FROM "+p+"app_users u LEFT JOIN "+p+"people pe ON pe.id=u.person_id ORDER BY u.active DESC,u.display_name,u.username"
    );
    return rows;
  });
}
async function addUser(data) {
  return withConn(async conn => {
    const p = await ensureSchema(conn);
    const username=cleanUsername(data.username), display=String(data.display_name||username).trim()||username;
    const [count] = await conn.query('SELECT COUNT(*) c FROM '+p+'app_users');
    const role = Number(count[0].c||0)===0 ? 'admin' : (['admin','readonly'].includes(data.role)?data.role:'user');
    const personId=Number(data.person_id||0)||null, hash=hashPassword(data.password);
    try {
      await conn.execute('INSERT INTO '+p+'app_users(username,display_name,password_hash,role,person_id,active) VALUES(?,?,?,?,?,1)',[username,display,hash,role,personId]);
    } catch(e) {
      if (Number(e.errno)===1062) throw new Error('Esse usuário já existe.');
      throw e;
    }
    return {ok:true};
  });
}
async function setUserPassword(id,password) {
  return withConn(async conn => {
    const p = await ensureSchema(conn);
    await conn.execute('UPDATE '+p+'app_users SET password_hash=? WHERE id=?',[hashPassword(password),Number(id)]);
    return {ok:true};
  });
}
async function toggleUser(id,active) {
  return withConn(async conn => {
    const p = await ensureSchema(conn); id=Number(id); active=active?1:0;
    if (!active) {
      const [r]=await conn.execute('SELECT role FROM '+p+'app_users WHERE id=? LIMIT 1',[id]);
      if (r[0] && r[0].role==='admin') {
        const [a]=await conn.execute("SELECT COUNT(*) c FROM "+p+"app_users WHERE role='admin' AND active=1 AND id<>?",[id]);
        if (Number(a[0].c||0)<1) throw new Error('Mantenha pelo menos um administrador ativo.');
      }
    }
    await conn.execute('UPDATE '+p+'app_users SET active=? WHERE id=?',[active,id]);
    return {ok:true};
  });
}

module.exports={hashPassword,verifyPassword,createSession,destroySession,getSession,authenticate,listUsers,addUser,setUserPassword,toggleUser};
