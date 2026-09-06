(()=>{
  const esc=v=>String(v??'').replace(/[&<>"']/g,m=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[m]));
  const parse=s=>{try{return JSON.parse(s)}catch{return {ok:false,error:'Resposta inválida.'}}};
  let floatHistory=[];
  let floatPending=false;
  try{floatHistory=JSON.parse(sessionStorage.getItem('granaok_float_ai_061')||'[]')}catch{floatHistory=[]}
  if(!Array.isArray(floatHistory))floatHistory=[];

  function ensureFloating(){
    if(document.getElementById('gk-ai-fab-061'))return;
    const fab=document.createElement('button');
    fab.id='gk-ai-fab-061';fab.className='gk-ai-fab-061';fab.textContent='✦';fab.setAttribute('aria-label','Abrir Grana IA');
    const panel=document.createElement('section');
    panel.id='gk-ai-panel-061';panel.className='gk-ai-panel-061 hidden';
    panel.innerHTML='<div class="gk-ai-head-061"><div><b>✦ Grana IA</b><small>Motor de Conhecimento do GranaOk</small></div><button class="gk-ai-close-061" id="gk-ai-close-061">×</button></div><div class="gk-ai-msgs-061" id="gk-ai-msgs-061"></div><div class="gk-ai-compose-061"><input id="gk-ai-input-061" placeholder="Pergunte sobre qualquer mês"><button class="primary" id="gk-ai-send-061">Enviar</button></div>';
    document.body.appendChild(fab);document.body.appendChild(panel);
    fab.onclick=()=>{panel.classList.remove('hidden');renderFloat();setTimeout(()=>document.getElementById('gk-ai-input-061')?.focus(),30)};
    document.getElementById('gk-ai-close-061').onclick=()=>panel.classList.add('hidden');
    document.getElementById('gk-ai-send-061').onclick=sendFloat;
    document.getElementById('gk-ai-input-061').onkeydown=e=>{if(e.key==='Enter'){e.preventDefault();sendFloat()}};
    renderFloat();
  }
  function saveFloat(){floatHistory=floatHistory.slice(-14);try{sessionStorage.setItem('granaok_float_ai_061',JSON.stringify(floatHistory))}catch(e){}}
  function renderFloat(){
    const el=document.getElementById('gk-ai-msgs-061');if(!el)return;
    el.innerHTML=floatHistory.length?floatHistory.map(m=>'<div class="gk-ai-msg-061 '+m.role+'">'+esc(m.text)+'</div>').join(''):'<div class="gk-ai-msg-061 bot">Pode perguntar do seu jeito. Ex.: “qual minha previsão para o próximo mês?”</div>';
    el.scrollTop=el.scrollHeight;
  }
  function add(role,text){floatHistory.push({role,text:String(text||'')});saveFloat();renderFloat()}
  function connect(){
    const u=prompt('Usuário do GranaOk Web:','gustavo');if(!u)return false;
    const p=prompt('Senha do GranaOk Web:');if(!p)return false;
    try{window.GranaServerAI.login(u,p);return true}catch(e){add('bot','Não consegui iniciar a conexão com o servidor.');return false}
  }
  function sendFloat(){
    const input=document.getElementById('gk-ai-input-061');if(!input)return;
    const q=input.value.trim();if(!q)return;input.value='';add('user',q);
    try{
      if(!window.GranaServerAI?.hasSession?.()){add('bot','Para usar o Motor de Conhecimento no APK, conecte sua conta do GranaOk Web.');connect();return}
      floatPending=true;
      window.GranaServerAI.action('assistant_ask',JSON.stringify({question:q,month:new Date().toISOString().slice(0,7),history:floatHistory.slice(-12).map(x=>({role:x.role,text:x.text}))}));
    }catch(e){floatPending=false;add('bot','Não consegui consultar a Grana IA agora.')}
  }

  const previousResult=window.GranaOkServerAiResult;
  window.GranaOkServerAiResult=function(raw){
    if(floatPending){
      floatPending=false;const d=parse(raw);
      add('bot',d.ok?(d.answer||'O motor respondeu sem texto.'):(d.error||'Falha ao consultar o Motor de Conhecimento.'));
      return;
    }
    if(typeof previousResult==='function')previousResult(raw);
  };
  const previousLogin=window.GranaOkServerAiLogin;
  window.GranaOkServerAiLogin=function(raw){
    const d=parse(raw);
    if(typeof previousLogin==='function')previousLogin(raw);
    const panel=document.getElementById('gk-ai-panel-061');
    if(d.ok&&panel&&!panel.classList.contains('hidden'))add('bot','Conexão realizada. Pode repetir sua pergunta.');
  };

  function addUpdateCard(){
    if(document.getElementById('gk-update-card-061')||!window.GranaUpdater?.check)return;
    const app=document.getElementById('app');if(!app)return;
    const card=document.createElement('div');card.id='gk-update-card-061';card.className='card gk-update-card-061';
    card.innerHTML='<h3>⬆ Atualizações do APK</h3><p class="muted">Versão instalada: '+esc(window.GranaUpdater.versionName?.()||'')+'</p><button class="secondary" id="gk-check-update-061">Verificar atualização</button><div class="gk-update-out-061" id="gk-update-out-061"></div>';
    app.appendChild(card);
    document.getElementById('gk-check-update-061').onclick=()=>{document.getElementById('gk-update-out-061').textContent='Consultando o servidor...';window.GranaUpdater.check()};
  }
  const baseSettings=window.settingsView;
  if(typeof baseSettings==='function')window.settingsView=function(){baseSettings();setTimeout(addUpdateCard,40)};

  window.GranaOkUpdateInfo=function(raw){
    const d=parse(raw),out=document.getElementById('gk-update-out-061');if(!out)return;
    if(!d.ok){out.innerHTML='<span class="negative">'+esc(d.error||'Falha ao verificar atualização.')+'</span>';return}
    if(!d.update_available){out.innerHTML='<span class="positive">Você já está na versão mais recente.</span>';return}
    out.innerHTML='<b>Nova versão: '+esc(d.version_name)+'</b><br><button class="primary" id="gk-install-update-061" style="margin-top:8px">Baixar e instalar</button>';
    document.getElementById('gk-install-update-061').onclick=()=>{out.innerHTML='Baixando e verificando o APK...';window.GranaUpdater.install(d.download_url,d.sha256||'')};
  };
  window.GranaOkUpdateInstall=function(raw){
    const d=parse(raw),out=document.getElementById('gk-update-out-061');if(!out)return;
    out.innerHTML=d.ok?'<span class="positive">'+esc(d.message||'Instalador aberto.')+'</span>':'<span class="negative">'+esc(d.error||'Falha ao instalar.')+'</span>';
  };

  ensureFloating();
})();
