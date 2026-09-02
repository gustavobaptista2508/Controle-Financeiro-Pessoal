const crypto = require('crypto');
const { initDb, withConn, ensureSchema } = require('../db');
const { hashPassword } = require('../auth');

function randomPassword(){
  return crypto.randomBytes(18).toString('base64url') + 'A9!';
}
async function main(){
  const username=String(process.argv[2]||'admin').trim();
  const display=String(process.argv[3]||'Administrador').trim();
  const password=randomPassword();
  await initDb();
  await withConn(async conn=>{
    const p=await ensureSchema(conn);
    const [exists]=await conn.execute('SELECT id FROM '+p+'app_users WHERE LOWER(username)=LOWER(?) LIMIT 1',[username]);
    if(exists[0])throw new Error('O usuário já existe.');
    await conn.execute('INSERT INTO '+p+'app_users(username,display_name,password_hash,role,active) VALUES(?,?,?,\'admin\',1)',[username,display,hashPassword(password)]);
  });
  console.log('');
  console.log('Usuário administrador criado.');
  console.log('Usuário:',username);
  console.log('Senha inicial:',password);
  console.log('Troque a senha depois do primeiro acesso.');
  console.log('');
}
main().then(()=>process.exit(0)).catch(e=>{console.error('Erro:',e.message||e);process.exit(1)});
