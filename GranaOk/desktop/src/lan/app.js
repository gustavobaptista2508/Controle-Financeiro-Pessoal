(()=>{
const $=s=>document.querySelector(s),$$=s=>[...document.querySelectorAll(s)];
const esc=v=>String(v??'').replace(/[&<>"']/g,m=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[m]));
const money=v=>Number(v||0).toLocaleString('pt-BR',{style:'currency',currency:'BRL'});
const today=()=>new Date().toISOString().slice(0,10), mlabel=m=>new Date(m+'-01T12:00:00').toLocaleDateString('pt-BR',{month:'short',year:'numeric'});
const shift=(m,n)=>{const d=new Date(m+'-01T12:00:00');d.setMonth(d.getMonth()+n);return d.toISOString().slice(0,7)};
const br=d=>d?new Date(d+'T12:00:00').toLocaleDateString('pt-BR'):'—';
let month=today().slice(0,7),user=null,ctx={accounts:[],cards:[],people:[]},view='dashboard';

async function api(action,payload={}){
 const r=await fetch('/api/action',{method:'POST',credentials:'same-origin',headers:{'Content-Type':'application/json','X-GranaOk-Client':'lan'},body:JSON.stringify({action,payload})});
 const d=await r.json().catch(()=>({ok:false,error:'Resposta inválida'}));
 if(r.status===401){login();throw new Error('Sessão expirada.')}
 if(!r.ok||!d.ok)throw new Error(d.error||'Falha na operação.');
 return d;
}
function login(){$('#app').classList.add('hidden');$('#login').classList.remove('hidden')}
function enter(){$('#login').classList.add('hidden');$('#app').classList.remove('hidden');$('#who').textContent=user?.display_name||user?.username||''}
function active(v){view=v;$$('.bottom button').forEach(b=>b.classList.toggle('active',b.dataset.view===v))}
function note(t,c=''){return '<div class="note '+c+'">'+esc(t)+'</div>'}
function badge(s){return '<span class="badge '+esc(s)+'">'+esc({paid:'Pago',pending:'Pendente',overdue:'Atrasado',open:'Aberta'}[s]||s)+'</span>'}
async function context(){ctx=await api('context',{});return ctx}

async function dashboard(){
 active('dashboard');const c=$('#content');c.innerHTML='<div class="empty">Carregando...</div>';
 try{const d=await api('dashboard',{month});c.innerHTML=
 '<div class="heading"><div><h1>Visão geral</h1><div class="muted">'+esc(mlabel(month))+'</div></div><div class="month-nav"><button class="secondary" id="prev">‹</button><button class="secondary" id="next">›</button></div></div>'+
 '<div class="grid kpis"><div class="kpi good"><small>Saldo</small><b>'+money(d.accounts_balance)+'</b></div><div class="kpi good"><small>Entradas</small><b>'+money(d.income)+'</b></div><div class="kpi bad"><small>Despesas</small><b>'+money(d.expenses)+'</b></div><div class="kpi bad"><small>Faturas</small><b>'+money(d.card_invoices)+'</b></div></div>'+
 '<div class="card" style="margin-top:12px"><small class="muted">Total de despesas do mês</small><div class="invoice-total">'+money(d.total_monthly_expenses??(Number(d.expenses||0)+Number(d.card_invoices||0)))+'</div><div class="muted">Despesas + faturas</div></div>'+
 '<div class="card" style="margin-top:12px"><h3>Projeção</h3><div class="invoice-total">'+money(d.projected)+'</div></div>';
 $('#prev').onclick=()=>{month=shift(month,-1);dashboard()};$('#next').onclick=()=>{month=shift(month,1);dashboard()};
 }catch(e){c.innerHTML=note(e.message,'err')}
}

async function transactions(){
 active('transactions');await context();const c=$('#content');
 c.innerHTML='<div class="heading"><div><h1>Lançamentos</h1><div class="muted">'+esc(mlabel(month))+'</div></div></div>'+
 '<div class="card filters"><div><label>Mês</label><input id="tm" type="month" value="'+month+'"></div><div><label>Conta</label><select id="ta"><option value="">Todas</option>'+ctx.accounts.map(a=>'<option value="'+a.id+'">'+esc(a.name)+'</option>').join('')+'</select></div><div><label>Tipo</label><select id="tt"><option value="">Todos</option><option value="expense">Despesas</option><option value="income">Entradas</option></select></div><div><label>Status</label><select id="ts"><option value="">Todos</option><option value="pending">Pendentes</option><option value="paid">Pagos</option><option value="overdue">Atrasados</option></select></div><div class="full"><label>Busca</label><input id="tq" placeholder="Descrição"></div><div class="full"><button class="secondary" id="tf">Filtrar</button></div></div><div id="tl" style="margin-top:12px"></div>';
 $('#tf').onclick=()=>{month=$('#tm').value||month;loadTx()};loadTx();
}
async function loadTx(){
 const el=$('#tl');el.innerHTML='<div class="empty">Carregando...</div>';
 try{const d=await api('transactions',{month:$('#tm').value||month,account_id:Number($('#ta').value||0),type:$('#tt').value,status:$('#ts').value,search:$('#tq').value});
 el.innerHTML=d.rows.length?'<div class="list">'+d.rows.map(r=>'<div class="row"><div class="main"><b>'+esc(r.description)+'</b><small>'+br(r.due_date)+' · '+esc(r.account_name||'Sem conta')+' · '+esc(r.category)+'</small><div>'+badge(r.effective_status)+'</div></div><div class="amount '+r.type+'">'+(r.type==='expense'?'-':'')+money(r.amount)+'</div></div>').join('')+'</div>':'<div class="empty">Nenhum lançamento.</div>';
 }catch(e){el.innerHTML=note(e.message,'err')}
}

async function cards(){
 active('cards');await context();const c=$('#content');
 c.innerHTML='<div class="heading"><div><h1>Cartões</h1><div class="muted">'+esc(mlabel(month))+'</div></div></div><div class="grid">'+(ctx.cards.length?ctx.cards.map(x=>'<div class="card credit"><small>CARTÃO</small><h2>'+esc(x.name)+'</h2><button class="secondary inv" data-id="'+x.id+'">Ver fatura</button></div>').join(''):'<div class="empty">Nenhum cartão.</div>')+'</div>';
 $$('.inv').forEach(b=>b.onclick=()=>invoice(Number(b.dataset.id),month));
}
async function invoice(cardId,m){
 month=m;active('cards');const c=$('#content');c.innerHTML='<div class="empty">Carregando...</div>';
 try{const d=await api('invoice',{card_id:cardId,month:m});
 c.innerHTML='<div class="heading"><div><h1>'+esc(d.card.name)+'</h1><div class="muted">'+esc(mlabel(m))+'</div></div><div class="month-nav"><button class="secondary" id="ip">‹</button><button class="secondary" id="in">›</button></div></div>'+
 '<div class="card"><small class="muted">Total</small><div class="invoice-total">'+money(d.total)+'</div><p>'+badge(d.status)+'</p>'+(d.status==='paid'?'<p>Pago em <b>'+br(d.paid_date)+'</b></p><button class="secondary" id="ir">Reabrir</button>':'<label>Data do pagamento</label><input id="idate" type="date" value="'+today()+'"><button class="primary" id="ipay" style="margin-top:10px">✓ Marcar fatura como paga</button>')+'</div>'+
 '<div class="list" style="margin-top:12px">'+(d.rows.length?d.rows.map(r=>'<div class="row"><div class="main"><b>'+esc(r.description)+'</b><small>'+br(r.purchase_date)+' · '+r.installment_number+'/'+r.installment_total+'</small></div><div class="amount expense">-'+money(r.amount)+'</div></div>').join(''):'<div class="empty">Sem compras.</div>')+'</div>';
 $('#ip').onclick=()=>invoice(cardId,shift(m,-1));$('#in').onclick=()=>invoice(cardId,shift(m,1));
 if($('#ipay'))$('#ipay').onclick=async()=>{try{await api('invoice_pay',{card_id:cardId,month:m,paid_date:$('#idate').value});invoice(cardId,m)}catch(e){alert(e.message)}};
 if($('#ir'))$('#ir').onclick=async()=>{try{await api('invoice_reopen',{card_id:cardId,month:m});invoice(cardId,m)}catch(e){alert(e.message)}};
 }catch(e){c.innerHTML=note(e.message,'err')}
}

async function financings(){
 active('financings');const c=$('#content');c.innerHTML='<div class="empty">Carregando...</div>';
 try{const d=await api('financings',{});c.innerHTML='<div class="heading"><div><h1>Financiamentos</h1></div></div><div class="grid">'+(d.rows.length?d.rows.map(f=>'<div class="card"><h3>'+esc(f.name)+'</h3><p><b>'+f.paid_installments+'/'+f.total_installments+'</b> parcelas pagas</p><div class="progress"><span style="width:'+Math.min(100,Number(f.paid_installments||0)/Math.max(1,Number(f.total_installments||1))*100)+'%"></span></div><p class="muted">Parcela: '+money(f.installment_amount)+'<br>Próximo: '+br(f.next_due_date)+'</p>'+(Number(f.active)?'<button class="primary fp" data-id="'+f.id+'">✓ Pagar parcela atual</button>':badge('paid'))+'</div>').join(''):'<div class="empty">Nenhum financiamento.</div>')+'</div>';
 $$('.fp').forEach(b=>b.onclick=async()=>{const dt=prompt('Data do pagamento (AAAA-MM-DD):',today());if(!dt)return;try{await api('financing_pay',{id:Number(b.dataset.id),paid_date:dt});financings()}catch(e){alert(e.message)}})
 }catch(e){c.innerHTML=note(e.message,'err')}
}

async function more(){
 active('more');await context();const c=$('#content');
 c.innerHTML='<div class="heading"><div><h1>Mais</h1><div class="muted">'+esc(user?.display_name||user?.username||'')+'</div></div></div><div class="card"><h3>Contas</h3>'+ctx.accounts.map(a=>'<div class="row"><div class="main"><b>'+esc(a.name)+'</b><small>'+esc(a.bank_code||'')+'</small></div><div class="amount">'+money(a.current_balance)+'</div></div>').join('')+'</div><div class="card" style="margin-top:12px"><p class="muted">Acesso local pelo GranaOk Desktop. Não encaminhe esta porta no roteador para a internet.</p></div>';
}
function route(v){if(v==='dashboard')dashboard();else if(v==='transactions')transactions();else if(v==='cards')cards();else if(v==='financings')financings();else more()}
$$('.bottom button').forEach(b=>b.onclick=()=>route(b.dataset.view));
$('#modal-close').onclick=()=>$('#modal').classList.add('hidden');
$('#logout').onclick=async()=>{await fetch('/api/logout',{method:'POST',credentials:'same-origin'}).catch(()=>{});user=null;login()};
$('#login-form').onsubmit=async e=>{e.preventDefault();const out=$('#login-out');out.innerHTML=note('Entrando...');try{const r=await fetch('/api/login',{method:'POST',credentials:'same-origin',headers:{'Content-Type':'application/json'},body:JSON.stringify({username:$('#login-user').value,password:$('#login-pass').value})});const d=await r.json();if(!r.ok||!d.ok)throw new Error(d.error||'Falha no acesso.');user=d.user;enter();dashboard()}catch(err){out.innerHTML=note(err.message,'err')}};
(async()=>{try{const r=await fetch('/api/session',{credentials:'same-origin'});const d=await r.json();if(d.authenticated){user=d.user;enter();dashboard()}else login()}catch{login()}})();
})();