(()=>{
const $=s=>document.querySelector(s),$$=s=>[...document.querySelectorAll(s)];
const esc=v=>String(v??'').replace(/[&<>"']/g,m=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot',"'":'&#39;'}[m]));
const money=v=>Number(v||0).toLocaleString('pt-BR',{style:'currency',currency:'BRL'});
const today=()=>new Date().toISOString().slice(0,10);
const mlabel=m=>new Date(m+'-01T12:00:00').toLocaleDateString('pt-BR',{month:'long',year:'numeric'});
const shift=(m,n)=>{const d=new Date(m+'-01T12:00:00');d.setMonth(d.getMonth()+n);return d.toISOString().slice(0,7)};
const br=d=>d?new Date(d+'T12:00:00').toLocaleDateString('pt-BR'):'—';
let month=today().slice(0,7),user=null,ctx={accounts:[],cards:[],people:[],categories:[]},view='dashboard';

async function api(action,payload={}){
  const r=await fetch('/api/action',{method:'POST',credentials:'same-origin',headers:{'Content-Type':'application/json','X-GranaOk-Client':'web'},body:JSON.stringify({action,payload})});
  const d=await r.json().catch(()=>({ok:false,error:'Resposta inválida'}));
  if(r.status===401){showLogin();throw new Error('Sessão expirada.')}
  if(!r.ok||!d.ok)throw new Error(d.error||'Falha na operação.');
  return d;
}
function showLogin(){$('#app').classList.add('hidden');$('#login').classList.remove('hidden')}
function showApp(){
  $('#login').classList.add('hidden');$('#app').classList.remove('hidden');
  const name=user?.display_name||user?.username||'';
  if($('#who-side'))$('#who-side').textContent=name;
  if($('#who-top'))$('#who-top').textContent=name;
  $$('.admin-only').forEach(el=>el.classList.toggle('hidden',user?.role!=='admin'));
}
function setTitle(t,s='GranaOk Web'){if($('#page-title'))$('#page-title').textContent=t;if($('#page-subtitle'))$('#page-subtitle').textContent=s}
function active(v){
  view=v;
  $$('[data-view]').forEach(b=>b.classList.toggle('active',b.dataset.view===v));
}
function note(t,c=''){return '<div class="note '+c+'">'+esc(t)+'</div>'}
function badge(s){return '<span class="badge '+esc(s)+'">'+esc({paid:'Pago',pending:'Pendente',overdue:'Atrasado',open:'Aberta'}[s]||s)+'</span>'}
async function context(){ctx=await api('context',{});return ctx}
function modal(html){$('#modal-content').innerHTML=html;$('#modal').classList.remove('hidden')}
function closeModal(){$('#modal').classList.add('hidden');$('#modal-content').innerHTML=''}

async function dashboard(){
  active('dashboard');setTitle('Visão geral',mlabel(month));
  const c=$('#content');c.innerHTML='<div class="empty">Carregando painel...</div>';
  try{
    const d=await api('dashboard',{month});
    c.innerHTML=
    '<div class="heading"><div><h1>Visão geral</h1><p>'+esc(mlabel(month))+'</p></div><div class="month-nav"><button class="secondary" id="prev">‹</button><b>'+esc(mlabel(month))+'</b><button class="secondary" id="next">›</button></div></div>'+
    '<div class="grid kpis">'+
      '<div class="kpi good"><small>Saldo das contas</small><b>'+money(d.accounts_balance)+'</b></div>'+
      '<div class="kpi good"><small>Entradas do mês</small><b>'+money(d.income)+'</b></div>'+
      '<div class="kpi bad"><small>Despesas do mês</small><b>'+money(d.expenses)+'</b></div>'+
      '<div class="kpi bad"><small>Faturas dos cartões</small><b>'+money(d.card_invoices)+'</b></div>'+
    '</div>'+
    '<div class="card total-month-card"><div><small>Total de despesas do mês</small><b>'+money(d.total_monthly_expenses??(Number(d.expenses||0)+Number(d.card_invoices||0)))+'</b></div><div class="total-month-formula">Despesas '+money(d.expenses)+' + faturas '+money(d.card_invoices)+'</div></div>'+
    '<div class="grid two" style="margin-top:16px">'+
      '<div class="card"><h3>Fluxo do mês</h3><div class="grid kpis" style="grid-template-columns:repeat(2,minmax(0,1fr))">'+
        '<div class="kpi"><small>Despesas pagas</small><b>'+money(d.paid_expenses)+'</b></div>'+
        '<div class="kpi"><small>Despesas pendentes</small><b>'+money(d.pending_expenses)+'</b></div>'+
        '<div class="kpi"><small>Faturas pagas</small><b>'+money(d.card_paid)+'</b></div>'+
        '<div class="kpi"><small>Faturas pendentes</small><b>'+money(d.card_pending)+'</b></div>'+
      '</div></div>'+
      '<div class="card"><h3>Projeção</h3><p class="muted">Saldo atual + entradas − despesas − faturas do mês selecionado.</p><div class="invoice-total">'+money(d.projected)+'</div><p>'+(d.overdue_count?'<span class="badge overdue">'+d.overdue_count+' lançamento(s) atrasado(s)</span>':'<span class="badge paid">Sem atrasos</span>')+'</p><p class="muted">Financiamentos ativos: '+money(d.financing_monthly)+'/mês</p></div>'+
    '</div>';
    $('#prev').onclick=()=>{month=shift(month,-1);dashboard()};
    $('#next').onclick=()=>{month=shift(month,1);dashboard()};
  }catch(e){c.innerHTML=note(e.message,'err')}
}

async function transactions(){
  active('transactions');setTitle('Lançamentos',mlabel(month));await context();
  const c=$('#content');
  c.innerHTML=
  '<div class="heading"><div><h1>Lançamentos</h1><p>Entradas, despesas e status por mês</p></div><button class="primary" id="txnew">＋ Novo lançamento</button></div>'+
  '<div class="card filters">'+
    '<div><label>Mês</label><input id="tm" type="month" value="'+month+'"></div>'+
    '<div><label>Conta</label><select id="ta"><option value="">Todas as contas</option>'+ctx.accounts.filter(a=>Number(a.active)!==0).map(a=>'<option value="'+a.id+'">'+esc(a.name)+'</option>').join('')+'</select></div>'+
    '<div><label>Tipo</label><select id="tt"><option value="">Todos</option><option value="expense">Despesas</option><option value="income">Entradas</option></select></div>'+
    '<div><label>Status</label><select id="ts"><option value="">Todos</option><option value="pending">Pendentes</option><option value="paid">Pagos</option><option value="overdue">Atrasados</option></select></div>'+
    '<div><label>Descrição</label><input id="tq" placeholder="Buscar"></div>'+
    '<div class="filter-action"><button class="secondary" id="tf">Filtrar</button></div>'+
  '</div><div id="tl" style="margin-top:16px"></div>';
  $('#tf').onclick=()=>{month=$('#tm').value||month;loadTx()};
  $('#txnew').onclick=transactionForm;
  loadTx();
}
async function loadTx(){
  const el=$('#tl');el.innerHTML='<div class="empty">Carregando...</div>';
  try{
    const d=await api('transactions',{month:$('#tm')?.value||month,account_id:Number($('#ta')?.value||0),type:$('#tt')?.value||'',status:$('#ts')?.value||'',search:$('#tq')?.value||''});
    el.innerHTML=d.rows.length?'<div class="list">'+d.rows.map(r=>'<div class="row"><div class="main"><b>'+esc(r.description)+'</b><small>'+br(r.due_date)+' · '+esc(r.account_name||'Sem conta')+' · '+esc(r.category)+'</small><div style="margin-top:6px">'+badge(r.effective_status)+'</div></div><div class="amount '+r.type+'">'+(r.type==='expense'?'-':'')+money(r.amount)+'</div></div>').join('')+'</div>':'<div class="empty">Nenhum lançamento.</div>';
  }catch(e){el.innerHTML=note(e.message,'err')}
}
function transactionForm(){
  modal('<h2>Novo lançamento</h2><div class="form-grid">'+
  '<div><label>Tipo</label><select id="ft"><option value="expense">Despesa</option><option value="income">Entrada</option></select></div>'+
  '<div><label>Status</label><select id="fs"><option value="pending">Pendente</option><option value="paid">Pago</option></select></div>'+
  '<div class="full"><label>Descrição</label><input id="fd"></div>'+
  '<div><label>Valor</label><input id="fv" inputmode="decimal"></div>'+
  '<div><label>Vencimento</label><input id="fdu" type="date" value="'+today()+'"></div>'+
  '<div><label>Conta</label><select id="fa"><option value="">—</option>'+ctx.accounts.map(a=>'<option value="'+a.id+'">'+esc(a.name)+'</option>').join('')+'</select></div>'+
  '<div><label>Pessoa/Casal</label><select id="fp"><option value="">—</option>'+ctx.people.map(p=>'<option value="'+p.id+'">'+esc(p.name)+'</option>').join('')+'</select></div>'+
  '<div class="full"><label>Categoria</label><input id="fc" value="Outros"></div>'+
  '<div class="full"><label>Observação</label><textarea id="fo"></textarea></div>'+
  '<div class="full"><button class="primary" id="fsv">Salvar lançamento</button><div id="fout"></div></div></div>');
  $('#fsv').onclick=async()=>{try{await api('transaction_save',{type:$('#ft').value,status:$('#fs').value,description:$('#fd').value,amount:$('#fv').value,due_date:$('#fdu').value,account_id:Number($('#fa').value||0),person_id:Number($('#fp').value||0),category:$('#fc').value,observations:$('#fo').value});closeModal();transactions()}catch(e){$('#fout').innerHTML=note(e.message,'err')}};
}

async function cards(){
  active('cards');setTitle('Cartões',mlabel(month));await context();
  const c=$('#content');
  c.innerHTML='<div class="heading"><div><h1>Cartões</h1><p>Faturas e compras por cartão</p></div></div><div class="grid card-grid">'+(ctx.cards.length?ctx.cards.filter(x=>Number(x.active)!==0).map(x=>'<div class="card credit"><small>CARTÃO</small><h2>'+esc(x.name)+'</h2><div class="muted" style="color:#cbd5e1">Limite '+money(x.limit_amount)+'</div><div class="actions"><button class="secondary inv" data-id="'+x.id+'">Ver fatura</button></div></div>').join(''):'<div class="empty">Nenhum cartão.</div>')+'</div>';
  $$('.inv').forEach(b=>b.onclick=()=>invoice(Number(b.dataset.id),month));
}
async function invoice(cardId,m){
  month=m;active('cards');const c=$('#content');c.innerHTML='<div class="empty">Carregando...</div>';
  try{
    const d=await api('invoice',{card_id:cardId,month:m});
    setTitle(d.card.name,mlabel(m));
    c.innerHTML='<div class="heading"><div><h1>'+esc(d.card.name)+'</h1><p>'+esc(mlabel(m))+'</p></div><div class="month-nav"><button class="secondary" id="ip">‹</button><b>'+esc(mlabel(m))+'</b><button class="secondary" id="in">›</button></div></div>'+
    '<div class="card"><small class="muted">Total da fatura</small><div class="invoice-total">'+money(d.total)+'</div><p>'+badge(d.status)+'</p>'+(d.status==='paid'?'<p>Pago em <b>'+br(d.paid_date)+'</b></p><button class="secondary" id="ir">Reabrir fatura</button>':'<label>Data do pagamento</label><input id="idate" type="date" value="'+today()+'"><button class="primary" id="ipay" style="margin-top:10px">✓ Marcar fatura como paga</button>')+'</div>'+
    '<div class="list" style="margin-top:16px">'+(d.rows.length?d.rows.map(r=>'<div class="row"><div class="main"><b>'+esc(r.description)+'</b><small>'+br(r.purchase_date)+' · '+r.installment_number+'/'+r.installment_total+' · '+esc(r.category||'')+'</small></div><div class="amount expense">-'+money(r.amount)+'</div></div>').join(''):'<div class="empty">Sem compras.</div>')+'</div>';
    $('#ip').onclick=()=>invoice(cardId,shift(m,-1));$('#in').onclick=()=>invoice(cardId,shift(m,1));
    if($('#ipay'))$('#ipay').onclick=async()=>{try{await api('invoice_pay',{card_id:cardId,month:m,paid_date:$('#idate').value});invoice(cardId,m)}catch(e){alert(e.message)}};
    if($('#ir'))$('#ir').onclick=async()=>{try{await api('invoice_reopen',{card_id:cardId,month:m});invoice(cardId,m)}catch(e){alert(e.message)}};
  }catch(e){c.innerHTML=note(e.message,'err')}
}

async function accounts(){
  active('accounts');setTitle('Contas','Saldos e bancos');await context();const c=$('#content');
  c.innerHTML='<div class="heading"><div><h1>Contas</h1><p>Contas bancárias vinculadas ao GranaOk</p></div></div><div class="grid card-grid">'+ctx.accounts.map(a=>'<div class="card"><small class="muted">'+esc(a.bank_code||'Conta')+'</small><h3 style="margin:8px 0">'+esc(a.name)+'</h3><div class="invoice-total">'+money(a.current_balance)+'</div><div class="muted">'+esc(a.person_name||'')+'</div></div>').join('')+'</div>';
}
async function financings(){
  active('financings');setTitle('Financiamentos','Parcelas e progresso');const c=$('#content');c.innerHTML='<div class="empty">Carregando...</div>';
  try{
    const d=await api('financings',{});
    c.innerHTML='<div class="heading"><div><h1>Financiamentos</h1><p>Controle de parcelas pagas e próximas</p></div></div><div class="grid card-grid">'+(d.rows.length?d.rows.map(f=>'<div class="card"><h3>'+esc(f.name)+'</h3><p><b>'+f.paid_installments+'/'+f.total_installments+'</b> parcelas pagas</p><div class="progress"><span style="width:'+Math.min(100,Number(f.paid_installments||0)/Math.max(1,Number(f.total_installments||1))*100)+'%"></span></div><p class="muted">Parcela: '+money(f.installment_amount)+'<br>Próximo vencimento: '+br(f.next_due_date)+'</p>'+(Number(f.active)?'<button class="primary fp" data-id="'+f.id+'">✓ Pagar parcela atual</button>':badge('paid'))+'</div>').join(''):'<div class="empty">Nenhum financiamento.</div>')+'</div>';
    $$('.fp').forEach(b=>b.onclick=async()=>{const dt=prompt('Data do pagamento (AAAA-MM-DD):',today());if(!dt)return;try{await api('financing_pay',{id:Number(b.dataset.id),paid_date:dt});financings()}catch(e){alert(e.message)}})
  }catch(e){c.innerHTML=note(e.message,'err')}
}
async function registry(){
  active('registry');setTitle('Cadastros','Pessoas e categorias');await context();const c=$('#content');
  c.innerHTML='<div class="heading"><div><h1>Cadastros</h1><p>Base do seu financeiro</p></div></div><div class="grid two">'+
  '<div class="card"><h3>Pessoas/Casal</h3>'+ctx.people.map(p=>'<div class="row"><div class="main"><b>'+esc(p.name)+'</b><small>'+esc(p.kind||'person')+(p.partner_name?' · '+esc(p.partner_name):'')+'</small></div></div>').join('')+'</div>'+
  '<div class="card"><h3>Categorias</h3>'+ctx.categories.map(x=>'<div class="row"><div class="main"><b>'+esc(x.name)+'</b><small>'+(x.kind==='income'?'Entrada':'Despesa')+'</small></div></div>').join('')+'</div></div>';
}
async function users(){
  active('users');setTitle('Usuários','Acesso ao GranaOk');await context();const c=$('#content');
  if(user?.role!=='admin'){c.innerHTML=note('Acesso restrito ao administrador.','err');return}
  c.innerHTML='<div class="heading"><div><h1>Usuários</h1><p>Logins e permissões</p></div><button class="primary" id="unew">＋ Novo usuário</button></div><div class="card"><div id="users-list"><div class="empty">Carregando...</div></div></div>';
  $('#unew').onclick=userForm;loadUsers();
}
async function more(){
  active('more');await context();setTitle('Mais',user?.display_name||user?.username||'');
  const c=$('#content');
  c.innerHTML='<div class="heading"><div><h1>Mais</h1><p>Atalhos e configurações</p></div></div>'+
  '<div class="grid card-grid">'+
    '<button class="card secondary shortcut" data-go="accounts"><h3>Contas</h3><div class="muted">Saldos e bancos</div></button>'+
    '<button class="card secondary shortcut" data-go="registry"><h3>Cadastros</h3><div class="muted">Pessoas e categorias</div></button>'+
    (user?.role==='admin'?'<button class="card secondary shortcut" data-go="users"><h3>Usuários</h3><div class="muted">Logins e permissões</div></button>':'')+
  '</div><div class="card" style="margin-top:16px"><p class="muted">No iPhone: Safari → Compartilhar → Adicionar à Tela de Início.</p></div>';
  $$('.shortcut').forEach(b=>b.onclick=()=>route(b.dataset.go));
}

async function usersApi(url,method='GET',body){
  const r=await fetch(url,{method,credentials:'same-origin',headers:{'Content-Type':'application/json','X-GranaOk-Client':'web'},body:body?JSON.stringify(body):undefined});
  const d=await r.json();if(!r.ok||!d.ok)throw new Error(d.error||'Falha.');return d;
}
async function loadUsers(){
  const el=$('#users-list');if(!el)return;
  try{
    const d=await usersApi('/api/users');
    el.innerHTML=d.rows.length?d.rows.map(u=>'<div class="user-item"><div class="user-meta"><div><b>'+esc(u.display_name)+'</b><small>'+esc(u.username)+' · '+esc(u.role)+(u.person_name?' · '+esc(u.person_name):'')+'</small></div>'+badge(Number(u.active)?'paid':'pending')+'</div><div class="user-actions"><button class="secondary upass" data-id="'+u.id+'">Senha</button><button class="secondary utoggle" data-id="'+u.id+'" data-active="'+(Number(u.active)?'1':'0')+'">'+(Number(u.active)?'Desativar':'Ativar')+'</button></div></div>').join(''):'<div class="empty">Nenhum usuário.</div>';
    $$('.upass').forEach(b=>b.onclick=()=>passwordForm(Number(b.dataset.id)));
    $$('.utoggle').forEach(b=>b.onclick=async()=>{try{await usersApi('/api/users/'+b.dataset.id+'/toggle','POST',{active:b.dataset.active!=='1'});loadUsers()}catch(e){alert(e.message)}});
  }catch(e){el.innerHTML=note(e.message,'err')}
}
function userForm(){
  modal('<h2>Novo usuário</h2><div class="form-grid"><div><label>Nome exibido</label><input id="ud"></div><div><label>Usuário</label><input id="uu"></div><div><label>Senha</label><input id="up" type="password"></div><div><label>Perfil</label><select id="ur"><option value="user">Usuário</option><option value="readonly">Somente leitura</option><option value="admin">Administrador</option></select></div><div class="full"><label>Vincular a pessoa (opcional)</label><select id="uper"><option value="">—</option>'+ctx.people.map(p=>'<option value="'+p.id+'">'+esc(p.name)+'</option>').join('')+'</select></div><div class="full"><button class="primary" id="us">Criar usuário</button><div id="uo"></div></div></div>');
  $('#us').onclick=async()=>{try{await usersApi('/api/users','POST',{display_name:$('#ud').value,username:$('#uu').value,password:$('#up').value,role:$('#ur').value,person_id:Number($('#uper').value||0)});closeModal();users()}catch(e){$('#uo').innerHTML=note(e.message,'err')}};
}
function passwordForm(id){
  modal('<h2>Alterar senha</h2><label>Nova senha</label><input id="np" type="password"><button class="primary" id="nps" style="margin-top:12px">Salvar</button><div id="npo"></div>');
  $('#nps').onclick=async()=>{try{await usersApi('/api/users/'+id+'/password','POST',{password:$('#np').value});closeModal()}catch(e){$('#npo').innerHTML=note(e.message,'err')}};
}

function route(v){
  if(v==='dashboard')return dashboard();
  if(v==='transactions')return transactions();
  if(v==='cards')return cards();
  if(v==='accounts')return accounts();
  if(v==='financings')return financings();
  if(v==='registry')return registry();
  if(v==='users')return users();
  return more();
}
$$('[data-view]').forEach(b=>b.onclick=()=>route(b.dataset.view));
$('#modal-close').onclick=closeModal;
$('#modal').onclick=e=>{if(e.target===$('#modal'))closeModal()};
async function doLogout(){await fetch('/api/logout',{method:'POST',credentials:'same-origin'}).catch(()=>{});user=null;showLogin()}
$('#logout').onclick=doLogout;$('#logout-side').onclick=doLogout;
$('#refresh').onclick=()=>route(view);
$('#login-form').onsubmit=async e=>{e.preventDefault();const out=$('#login-out');out.innerHTML=note('Entrando...');try{const r=await fetch('/api/login',{method:'POST',credentials:'same-origin',headers:{'Content-Type':'application/json'},body:JSON.stringify({username:$('#login-user').value,password:$('#login-pass').value})});const d=await r.json();if(!r.ok||!d.ok)throw new Error(d.error||'Falha no acesso.');user=d.user;showApp();dashboard()}catch(err){out.innerHTML=note(err.message,'err')}};
(async()=>{try{const r=await fetch('/api/session',{credentials:'same-origin'});const d=await r.json();if(d.authenticated){user=d.user;showApp();dashboard()}else showLogin()}catch{showLogin()}})();
})();