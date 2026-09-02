const path = require('path');
const express = require('express');
const { config } = require('./config');
const { initDb, withConn, ensureSchema } = require('./db');
const { runAction, writeActions } = require('./actions');
const { createSession, destroySession, getSession, authenticate, listUsers, addUser, setUserPassword, toggleUser } = require('./auth');

const app = express();
app.set('trust proxy', config.trustProxy);
app.disable('x-powered-by');
app.use(express.json({limit:'1mb'}));

const attempts = new Map();

function securityHeaders(req,res,next){
  res.setHeader('X-Content-Type-Options','nosniff');
  res.setHeader('X-Frame-Options','DENY');
  res.setHeader('Referrer-Policy','no-referrer');
  res.setHeader('Permissions-Policy','camera=(), microphone=(), geolocation=()');
  res.setHeader('Content-Security-Policy',"default-src 'self'; style-src 'self' 'unsafe-inline'; script-src 'self'; img-src 'self' data:; connect-src 'self'; manifest-src 'self'; frame-ancestors 'none'");
  next();
}
app.use(securityHeaders);

function parseCookies(req){
  const out={};
  String(req.headers.cookie||'').split(';').forEach(p=>{const i=p.indexOf('=');if(i>0)out[p.slice(0,i).trim()]=decodeURIComponent(p.slice(i+1).trim())});
  return out;
}
function cookieString(token,maxAge){
  const parts=['gk_session='+encodeURIComponent(token),'HttpOnly','SameSite=Strict','Path=/','Max-Age='+String(maxAge)];
  if(config.cookieSecure)parts.push('Secure');
  return parts.join('; ');
}
function ipOf(req){return String(req.ip||req.socket.remoteAddress||'unknown')}
function loginAllowed(ip){
  const now=Date.now(),a=attempts.get(ip);
  if(!a||now-a.first>10*60*1000){attempts.delete(ip);return true}
  return a.count<5;
}
function failLogin(ip){
  const now=Date.now(),a=attempts.get(ip);
  if(!a||now-a.first>10*60*1000)attempts.set(ip,{count:1,first:now});
  else {a.count++;attempts.set(ip,a)}
}
async function authRequired(req,res,next){
  try{
    const token=parseCookies(req).gk_session;
    const user=await getSession(token);
    if(!user)return res.status(401).json({ok:false,error:'Sessão expirada.'});
    req.user=user;req.sessionToken=token;next();
  }catch(e){res.status(500).json({ok:false,error:'Falha de autenticação.'})}
}
function adminRequired(req,res,next){
  if(!req.user||req.user.role!=='admin')return res.status(403).json({ok:false,error:'Acesso restrito ao administrador.'});
  next();
}
function webClientRequired(req,res,next){
  if(String(req.headers['x-granaok-client']||'')!=='web')return res.status(403).json({ok:false,error:'Cliente inválido.'});
  next();
}

app.get('/healthz',async(req,res)=>{
  try{const info=await initDb();res.json({ok:true,db:info.db,version:info.version})}
  catch(e){res.status(503).json({ok:false,error:'database_unavailable'})}
});

app.get('/api/session',async(req,res)=>{
  try{
    const user=await getSession(parseCookies(req).gk_session);
    res.json({ok:true,authenticated:!!user,user:user||null});
  }catch(e){res.status(500).json({ok:false,error:'Falha ao validar sessão.'})}
});

app.post('/api/login',async(req,res)=>{
  const ip=ipOf(req);
  if(!loginAllowed(ip))return res.status(429).json({ok:false,error:'Muitas tentativas. Aguarde alguns minutos.'});
  try{
    const user=await authenticate(req.body&&req.body.username,req.body&&req.body.password);
    if(!user){failLogin(ip);return res.status(401).json({ok:false,error:'Usuário ou senha inválidos.'})}
    attempts.delete(ip);
    const token=await createSession(user.id);
    res.setHeader('Set-Cookie',cookieString(token,config.sessionHours*3600));
    res.json({ok:true,user});
  }catch(e){res.status(500).json({ok:false,error:'Não foi possível entrar.'})}
});

app.post('/api/logout',authRequired,async(req,res)=>{
  try{await destroySession(req.sessionToken)}catch(_){}
  res.setHeader('Set-Cookie',cookieString('',0));
  res.json({ok:true});
});

app.post('/api/action',authRequired,webClientRequired,async(req,res)=>{
  try{
    const action=String((req.body&&req.body.action)||'');
    const allowed=new Set(['dashboard','context','transactions','transaction_save','transaction_status','account_save','person_add','category_add','card_save','card_purchase_add','invoice','invoice_pay','invoice_reopen','financings','financing_pay','assistant_summary','assistant_ask','investment_radar','knowledge_rebuild','knowledge_summary','knowledge_feedback']);
    if(!allowed.has(action))return res.status(403).json({ok:false,error:'Operação não permitida.'});
    const result=await runAction(action,(req.body&&req.body.payload)||{},req.user);
    res.json(Object.assign({ok:true},result||{}));
  }catch(e){res.status(400).json({ok:false,error:String(e&&e.message?e.message:e)})}
});

app.get('/api/users',authRequired,adminRequired,async(req,res)=>{
  try{res.json({ok:true,rows:await listUsers()})}
  catch(e){res.status(500).json({ok:false,error:'Falha ao listar usuários.'})}
});
app.post('/api/users',authRequired,adminRequired,webClientRequired,async(req,res)=>{
  try{await addUser(req.body||{});res.json({ok:true,message:'Usuário criado.'})}
  catch(e){res.status(400).json({ok:false,error:String(e.message||e)})}
});
app.post('/api/users/:id/password',authRequired,adminRequired,webClientRequired,async(req,res)=>{
  try{await setUserPassword(Number(req.params.id),(req.body||{}).password);res.json({ok:true,message:'Senha alterada.'})}
  catch(e){res.status(400).json({ok:false,error:String(e.message||e)})}
});
app.post('/api/users/:id/toggle',authRequired,adminRequired,webClientRequired,async(req,res)=>{
  try{await toggleUser(Number(req.params.id),!!(req.body||{}).active);res.json({ok:true})}
  catch(e){res.status(400).json({ok:false,error:String(e.message||e)})}
});

const publicDir=path.join(__dirname,'public');
app.use(express.static(publicDir,{index:false,maxAge:'1h'}));
app.use((req,res,next)=>{if(req.method!=='GET')return next();res.sendFile(path.join(publicDir,'index.html'))});

async function start(){
  const info=await initDb();
  console.log('[GranaOk] MySQL conectado:',info.db,info.version);
  app.listen(config.port,config.host,()=>console.log('[GranaOk] ouvindo em http://'+config.host+':'+config.port));
}
start().catch(e=>{console.error('[GranaOk] falha ao iniciar:',e);process.exit(1)});

process.on('SIGTERM',()=>process.exit(0));
process.on('SIGINT',()=>process.exit(0));
