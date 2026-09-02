(()=>{
const $=s=>document.querySelector(s), $$=s=>[...document.querySelectorAll(s)];
const money=v=>Number(v||0).toLocaleString('pt-BR',{style:'currency',currency:'BRL'});
const esc=v=>String(v??'').replace(/[&<>"']/g,m=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[m]));
const today=()=>new Date().toISOString().slice(0,10);
const monthNow=()=>today().slice(0,7);
const monthLabel=m=>new Date(m+'-01T12:00:00').toLocaleDateString('pt-BR',{month:'long',year:'numeric'});
const shiftMonth=(m,n)=>{const d=new Date(m+'-01T12:00:00');d.setMonth(d.getMonth()+n);return d.toISOString().slice(0,7)};
const brDate=s=>s?new Date(s+'T12:00:00').toLocaleDateString('pt-BR'):'—';
const content=()=>$('#content');
let currentView='dashboard', currentMonth=monthNow(), ctx={people:[],accounts:[],categories:[],cards:[]};

const ACTIONS={transactions:'transactions:list',transaction_save:'transaction:save',transaction_status:'transaction:status',account_save:'account:save',person_add:'person:add',category_add:'category:add',card_save:'card:save',card_purchase_add:'card:purchase',invoice:'invoice:get',invoice_pay:'invoice:pay',invoice_reopen:'invoice:reopen',financings:'financings:list',financing_pay:'financing:pay'};
async function api(action,data={}){
  const d=await window.granaok.invoke(ACTIONS[action]||action,data);
  if(!d||!d.ok)throw new Error((d&&d.error)||'Falha na operação.');
  return d;
}
function note(t,c=''){return '<div class="note '+c+'">'+esc(t)+'</div>'}
function showLogin(){ $('#shell').classList.add('hidden');$('#setup').classList.remove('hidden'); }
function showApp(){ $('#setup').classList.add('hidden');$('#shell').classList.remove('hidden'); }
function setTitle(t,s='GranaOk Desktop'){ $('#title').textContent=t;$('#subtitle').textContent=s; }
function modal(html){$('#modal-content').innerHTML=html;$('#modal').classList.remove('hidden')}
function closeModal(){$('#modal').classList.add('hidden');$('#modal-content').innerHTML=''}
async function loadContext(){ctx=await api('context');return ctx}
function options(rows,selected=0,label='name'){return '<option value="0">—</option>'+rows.map(x=>'<option value="'+Number(x.id)+'" '+(Number(x.id)===Number(selected)?'selected':'')+'>'+esc(x[label])+'</option>').join('')}
function categoryOptions(kind,selected=''){const rows=ctx.categories.filter(x=>x.kind===kind&&Number(x.active)!==0);return rows.map(x=>'<option '+(x.name===selected?'selected':'')+'>'+esc(x.name)+'</option>').join('')}
function statusBadge(s){const map={paid:'Pago',pending:'Pendente',overdue:'Atrasado',open:'Aberta'};return '<span class="badge '+esc(s)+'">'+esc(map[s]||s)+'</span>'}
function activateNav(v){$$('#nav button').forEach(b=>b.classList.toggle('active',b.dataset.view===v));currentView=v}

async function dashboard(){
  activateNav('dashboard');setTitle('Visão geral',monthLabel(currentMonth));
  content().innerHTML='<div class="empty">Carregando painel...</div>';
  try{
    const d=await api('dashboard',{month:currentMonth});
    content().innerHTML=`
      <div class="heading"><div><h1>Visão geral</h1><p>${esc(monthLabel(currentMonth))}</p></div><div class="month-nav"><button class="secondary" id="prev-month">‹</button><b>${esc(monthLabel(currentMonth))}</b><button class="secondary" id="next-month">›</button></div></div>
      <div class="grid kpis">
        <div class="kpi good"><small>Saldo das contas</small><b>${money(d.accounts_balance)}</b></div>
        <div class="kpi good"><small>Entradas do mês</small><b>${money(d.income)}</b></div>
        <div class="kpi bad"><small>Despesas do mês</small><b>${money(d.expenses)}</b></div>
        <div class="kpi bad"><small>Faturas dos cartões</small><b>${money(d.card_invoices)}</b></div>
      </div>
      <div class="card total-month-card">
        <div>
          <small>Total de despesas do mês</small>
          <b>${money(d.total_monthly_expenses ?? (Number(d.expenses||0)+Number(d.card_invoices||0)))}</b>
        </div>
        <div class="total-month-formula">Despesas ${money(d.expenses)} + faturas ${money(d.card_invoices)}</div>
      </div>
      <div class="grid two" style="margin-top:16px">
        <div class="card"><h3>Fluxo do mês</h3>
          <div class="grid kpis" style="grid-template-columns:repeat(2,1fr)">
            <div class="kpi"><small>Despesas pagas</small><b>${money(d.paid_expenses)}</b></div>
            <div class="kpi"><small>Despesas pendentes</small><b>${money(d.pending_expenses)}</b></div>
            <div class="kpi"><small>Faturas pagas</small><b>${money(d.card_paid)}</b></div>
            <div class="kpi"><small>Faturas pendentes</small><b>${money(d.card_pending)}</b></div>
          </div>
        </div>
        <div class="card"><h3>Projeção</h3><p class="muted">Saldo atual + entradas − despesas − faturas do mês selecionado.</p><div class="invoice-total">${money(d.projected)}</div><p>${d.overdue_count?'<span class="badge overdue">'+d.overdue_count+' lançamento(s) atrasado(s)</span>':'<span class="badge paid">Sem atrasos</span>'}</p><p class="muted">Financiamentos ativos: ${money(d.financing_monthly)}/mês</p></div>
      </div>`;
    $('#prev-month').onclick=()=>{currentMonth=shiftMonth(currentMonth,-1);dashboard()};
    $('#next-month').onclick=()=>{currentMonth=shiftMonth(currentMonth,1);dashboard()};
  }catch(e){content().innerHTML=note(e.message,'err')}
}

async function transactions(){
  activateNav('transactions');setTitle('Lançamentos',monthLabel(currentMonth));
  await loadContext();
  content().innerHTML=`
    <div class="heading"><div><h1>Lançamentos</h1><p>Entradas, despesas e status por mês</p></div><button class="primary" id="new-tx">＋ Novo lançamento</button></div>
    <div class="card tx-filter-card">
      <div class="toolbar tx-main-filters">
        <div><label>Mês</label><input id="tx-month" type="month" value="${currentMonth}"></div>
        <div><label>Conta</label><select id="tx-account"><option value="">Todas as contas</option>${ctx.accounts.filter(a=>Number(a.active)!==0).map(a=>'<option value="'+a.id+'">'+esc(a.name)+'</option>').join('')}</select></div>
        <div><label>Tipo</label><select id="tx-type"><option value="">Todos</option><option value="expense">Despesas</option><option value="income">Entradas</option></select></div>
        <div><label>Status</label><select id="tx-status"><option value="">Todos</option><option value="pending">Pendentes</option><option value="paid">Pagos</option><option value="overdue">Atrasados</option></select></div>
        <div class="tx-search-main"><label>Descrição</label><input id="tx-search" placeholder="Buscar na descrição"></div>
        <button class="secondary" id="tx-filter">Filtrar</button>
      </div>

      <div class="tx-filter-actions">
        <button class="ghost" id="tx-more">⚙ Mais filtros <span id="tx-filter-count"></span></button>
        <button class="ghost" id="tx-clear">Limpar filtros</button>
      </div>

      <div id="tx-advanced" class="tx-advanced hidden">
        <div><label>Pessoa/Casal</label><select id="tx-person"><option value="">Todos</option>${ctx.people.filter(p=>Number(p.active)!==0).map(p=>'<option value="'+p.id+'">'+esc(p.name)+'</option>').join('')}</select></div>
        <div><label>Categoria</label><select id="tx-category"><option value="">Todas</option>${ctx.categories.filter(c=>Number(c.active)!==0).map(c=>'<option value="'+c.id+'">'+esc(c.name)+' · '+(c.kind==='income'?'Entrada':'Despesa')+'</option>').join('')}</select></div>
        <div><label>Observação contém</label><input id="tx-observation" placeholder="Texto da observação"></div>
        <div><label>Origem</label><input id="tx-source" placeholder="manual_import, desktop..."></div>
        <div><label>Valor mínimo</label><input id="tx-min" inputmode="decimal" placeholder="0,00"></div>
        <div><label>Valor máximo</label><input id="tx-max" inputmode="decimal" placeholder="0,00"></div>
        <div><label>Vencimento de</label><input id="tx-date-from" type="date"></div>
        <div><label>Vencimento até</label><input id="tx-date-to" type="date"></div>
        <div><label>Parcelamento</label><select id="tx-installments"><option value="">Todos</option><option value="cash">Somente à vista</option><option value="installment">Somente parcelados</option></select></div>
        <div><label>Ordenar por</label><select id="tx-order"><option value="date_asc">Data · mais antiga</option><option value="date_desc">Data · mais recente</option><option value="amount_desc">Maior valor</option><option value="amount_asc">Menor valor</option><option value="description_asc">Descrição A–Z</option></select></div>
      </div>
    </div>

    <div id="tx-summary" class="tx-summary"></div>
    <div class="card"><div id="tx-list" class="table-wrap"><div class="empty">Carregando...</div></div></div>`;

  $('#new-tx').onclick=()=>transactionForm();
  $('#tx-filter').onclick=()=>{currentMonth=$('#tx-month').value||currentMonth;loadTransactions()};
  $('#tx-more').onclick=()=>{$('#tx-advanced').classList.toggle('hidden');$('#tx-more').classList.toggle('active-filter-panel',!$('#tx-advanced').classList.contains('hidden'))};
  $('#tx-clear').onclick=()=>{
    ['tx-account','tx-type','tx-status','tx-person','tx-category','tx-installments'].forEach(id=>{const e=$('#'+id);if(e)e.value=''});
    ['tx-search','tx-observation','tx-source','tx-min','tx-max','tx-date-from','tx-date-to'].forEach(id=>{const e=$('#'+id);if(e)e.value=''});
    $('#tx-order').value='date_asc';loadTransactions();
  };
  ['tx-month','tx-account','tx-type','tx-status'].forEach(id=>$('#'+id).addEventListener('change',()=>{if(id==='tx-month')currentMonth=$('#tx-month').value||currentMonth;loadTransactions()}));
  $('#tx-search').addEventListener('keydown',e=>{if(e.key==='Enter')loadTransactions()});
  loadTransactions();
}

function txFilterPayload(){
  return {
    month:$('#tx-month')?.value||currentMonth,
    account_id:Number($('#tx-account')?.value||0),
    type:$('#tx-type')?.value||'',
    status:$('#tx-status')?.value||'',
    search:$('#tx-search')?.value||'',
    person_id:Number($('#tx-person')?.value||0),
    category_id:Number($('#tx-category')?.value||0),
    observation:$('#tx-observation')?.value||'',
    source:$('#tx-source')?.value||'',
    min_amount:$('#tx-min')?.value||'',
    max_amount:$('#tx-max')?.value||'',
    date_from:$('#tx-date-from')?.value||'',
    date_to:$('#tx-date-to')?.value||'',
    installments:$('#tx-installments')?.value||'',
    order:$('#tx-order')?.value||'date_asc'
  };
}
function updateTxFilterCount(){
  const p=txFilterPayload();
  const advanced=[p.person_id,p.category_id,p.observation,p.source,p.min_amount,p.max_amount,p.date_from,p.date_to,p.installments,p.order!=='date_asc'?p.order:''].filter(Boolean).length;
  const e=$('#tx-filter-count');if(e)e.textContent=advanced?'('+advanced+')':'';
}
async function loadTransactions(){
  const el=$('#tx-list');if(!el)return;el.innerHTML='<div class="empty">Carregando...</div>';updateTxFilterCount();
  try{
    const d=await api('transactions',txFilterPayload());
    const expenses=d.rows.filter(r=>r.type==='expense').reduce((s,r)=>s+Number(r.amount||0),0);
    const income=d.rows.filter(r=>r.type==='income').reduce((s,r)=>s+Number(r.amount||0),0);
    const summary=$('#tx-summary');
    if(summary)summary.innerHTML=`
      <div><b>${d.rows.length}</b><span>resultado(s)</span></div>
      <div class="tx-summary-expense"><b>${money(expenses)}</b><span>despesas filtradas</span></div>
      <div class="tx-summary-income"><b>${money(income)}</b><span>entradas filtradas</span></div>`;

    el.innerHTML=d.rows.length?`<table class="table"><thead><tr><th>Data</th><th>Descrição</th><th>Conta</th><th>Pessoa</th><th>Categoria</th><th>Status</th><th>Valor</th><th></th></tr></thead><tbody>${d.rows.map(r=>`<tr><td>${brDate(r.due_date)}</td><td class="desc"><b>${esc(r.description)}</b>${Number(r.installment_total)>1?'<small> · '+r.installment_number+'/'+r.installment_total+'</small>':''}${r.observations?'<div class="tx-row-note">'+esc(r.observations)+'</div>':''}</td><td>${esc(r.account_name||'—')}</td><td>${esc(r.person_name||'—')}</td><td>${esc(r.category)}</td><td>${statusBadge(r.effective_status)}</td><td class="amount ${r.type}">${r.type==='expense'?'-':''}${money(r.amount)}</td><td><button class="ghost tx-edit" data-id="${r.id}">Editar</button>${r.status!=='paid'?'<button class="ghost tx-pay" data-id="'+r.id+'">Pagar</button>':''}</td></tr>`).join('')}</tbody></table>`:'<div class="empty">Nenhum lançamento corresponde aos filtros.</div>';
    $$('.tx-edit').forEach(b=>b.onclick=()=>transactionForm(d.rows.find(x=>Number(x.id)===Number(b.dataset.id))));
    $$('.tx-pay').forEach(b=>b.onclick=async()=>{try{await api('transaction_status',{id:Number(b.dataset.id),status:'paid'});loadTransactions()}catch(e){alert(e.message)}});
  }catch(e){el.innerHTML=note(e.message,'err')}
}
function transactionForm(r=null){
  const kind=r?.type||'expense';
  modal(`<h2>${r?'Editar':'Novo'} lançamento</h2><div class="form-grid">
    <div><label>Tipo</label><select id="f-type"><option value="expense" ${kind==='expense'?'selected':''}>Despesa</option><option value="income" ${kind==='income'?'selected':''}>Entrada</option></select></div>
    <div><label>Status</label><select id="f-status"><option value="pending">Pendente</option><option value="paid" ${r?.status==='paid'?'selected':''}>Pago</option><option value="overdue" ${r?.status==='overdue'?'selected':''}>Atrasado</option></select></div>
    <div class="full"><label>Descrição</label><input id="f-desc" value="${esc(r?.description||'')}"></div>
    <div><label>Valor</label><input id="f-amount" inputmode="decimal" value="${r?.amount||''}"></div>
    <div><label>Vencimento</label><input id="f-due" type="date" value="${r?.due_date||today()}"></div>
    <div><label>Pessoa/casal</label><select id="f-person">${options(ctx.people,r?.person_id||1)}</select></div>
    <div><label>Conta</label><select id="f-account">${options(ctx.accounts,r?.account_id||0)}</select></div>
    <div class="full"><label>Categoria</label><input id="f-cat" list="cats-${kind}" value="${esc(r?.category||'Outros')}"><datalist id="cats-${kind}">${categoryOptions(kind)}</datalist></div>
    <div class="full"><label>Observações</label><textarea id="f-obs" rows="3">${esc(r?.observations||'')}</textarea></div>
    <div class="full"><button class="primary" id="f-save">Salvar</button><div id="f-out"></div></div>
  </div>`);
  $('#f-save').onclick=async()=>{try{
    await api('transaction_save',{id:r?.id||0,type:$('#f-type').value,status:$('#f-status').value,description:$('#f-desc').value,amount:$('#f-amount').value,due_date:$('#f-due').value,person_id:Number($('#f-person').value||0),account_id:Number($('#f-account').value||0),category:$('#f-cat').value,observations:$('#f-obs').value});
    closeModal();transactions();
  }catch(e){$('#f-out').innerHTML=note(e.message,'err')}};
}

async function cards(){
  activateNav('cards');setTitle('Cartões','Faturas por cartão');
  await loadContext();
  content().innerHTML=`<div class="heading"><div><h1>Cartões</h1><p>Compras, faturas e pagamentos</p></div><button class="primary" id="new-card">＋ Novo cartão</button></div><div class="grid card-grid" id="cards-grid">${ctx.cards.map(c=>`<div class="card credit"><small>CARTÃO</small><h2>${esc(c.name)}</h2><div class="rowline"><div><small>Limite</small><b>${money(c.limit_amount)}</b></div><div><small>Vencimento</small><b>dia ${c.due_day}</b></div></div><div class="actions"><button class="secondary invoice-open" data-id="${c.id}">Ver fatura</button><button class="primary purchase-add" data-id="${c.id}">＋ Compra</button></div></div>`).join('')||'<div class="empty">Nenhum cartão cadastrado.</div>'}</div>`;
  $('#new-card').onclick=()=>cardForm();
  $$('.invoice-open').forEach(b=>b.onclick=()=>invoiceView(Number(b.dataset.id),currentMonth));
  $$('.purchase-add').forEach(b=>b.onclick=()=>purchaseForm(Number(b.dataset.id)));
}
function cardForm(){
  modal(`<h2>Novo cartão</h2><div class="form-grid"><div class="full"><label>Nome</label><input id="c-name"></div><div><label>Titular</label><select id="c-person">${options(ctx.people,1)}</select></div><div><label>Limite</label><input id="c-limit" inputmode="decimal"></div><div><label>Fechamento</label><input id="c-close" type="number" min="1" max="31" value="27"></div><div><label>Vencimento</label><input id="c-due" type="number" min="1" max="31" value="10"></div><div class="full"><button class="primary" id="c-save">Salvar</button><div id="c-out"></div></div></div>`);
  $('#c-save').onclick=async()=>{try{await api('card_save',{name:$('#c-name').value,person_id:Number($('#c-person').value||0),limit_amount:$('#c-limit').value,closing_day:Number($('#c-close').value),due_day:Number($('#c-due').value)});closeModal();cards()}catch(e){$('#c-out').innerHTML=note(e.message,'err')}};
}
function purchaseForm(cardId){
  modal(`<h2>Nova compra no cartão</h2><div class="form-grid"><div class="full"><label>Descrição</label><input id="p-desc"></div><div><label>Valor total</label><input id="p-amount" inputmode="decimal"></div><div><label>Data da compra</label><input id="p-date" type="date" value="${today()}"></div><div><label>Parcelas</label><input id="p-inst" type="number" min="1" max="60" value="1"></div><div><label>Pessoa/casal</label><select id="p-person">${options(ctx.people,1)}</select></div><div class="full"><label>Categoria</label><input id="p-cat" value="Outros"></div><div class="full"><label>Observações</label><textarea id="p-obs"></textarea></div><div class="full"><button class="primary" id="p-save">Incluir na fatura</button><div id="p-out"></div></div></div>`);
  $('#p-save').onclick=async()=>{try{const d=await api('card_purchase_add',{card_id:cardId,person_id:Number($('#p-person').value||0),description:$('#p-desc').value,amount:$('#p-amount').value,purchase_date:$('#p-date').value,installments:Number($('#p-inst').value||1),category:$('#p-cat').value,observations:$('#p-obs').value});closeModal();invoiceView(cardId,d.month||currentMonth)}catch(e){$('#p-out').innerHTML=note(e.message,'err')}};
}
async function invoiceView(cardId,month){
  currentMonth=month;activateNav('cards');setTitle('Fatura do cartão',monthLabel(month));
  content().innerHTML='<div class="empty">Carregando fatura...</div>';
  try{
    const d=await api('invoice',{card_id:cardId,month});
    content().innerHTML=`<div class="heading"><div><h1>${esc(d.card.name)}</h1><p>Vence em ${brDate(d.due_date)}</p></div><div class="month-nav"><button class="secondary" id="inv-prev">‹</button><b>${esc(monthLabel(month))}</b><button class="secondary" id="inv-next">›</button></div></div>
      <div class="grid two"><div class="card"><small class="muted">Total da fatura</small><div class="invoice-total">${money(d.total)}</div><p>${statusBadge(d.status)}</p></div>
      <div class="card"><h3>Pagamento</h3>${d.status==='paid'?'<p>Fatura paga'+(d.paid_date?' em <b>'+brDate(d.paid_date)+'</b>':'')+'.</p><button class="secondary" id="inv-reopen">Reabrir fatura</button>':'<label>Data do pagamento</label><input id="inv-paid-date" type="date" value="'+today()+'"><button class="primary" id="inv-pay" style="margin-top:12px">✓ Marcar fatura como paga</button>'}<div id="inv-out"></div></div></div>
      <div class="card" style="margin-top:16px"><div class="heading"><div><h3>Compras</h3></div><button class="primary" id="inv-add">＋ Compra</button></div><div class="table-wrap">${d.rows.length?'<table class="table"><thead><tr><th>Compra</th><th>Descrição</th><th>Categoria</th><th>Parcela</th><th>Valor</th></tr></thead><tbody>'+d.rows.map(r=>'<tr><td>'+brDate(r.purchase_date)+'</td><td class="desc"><b>'+esc(r.description)+'</b></td><td>'+esc(r.category)+'</td><td>'+r.installment_number+'/'+r.installment_total+'</td><td class="amount expense">-'+money(r.amount)+'</td></tr>').join('')+'</tbody></table>':'<div class="empty">Sem compras nesta fatura.</div>'}</div></div>
      <button class="ghost" id="back-cards" style="margin-top:14px">← Voltar aos cartões</button>`;
    $('#inv-prev').onclick=()=>invoiceView(cardId,shiftMonth(month,-1));$('#inv-next').onclick=()=>invoiceView(cardId,shiftMonth(month,1));$('#inv-add').onclick=()=>purchaseForm(cardId);$('#back-cards').onclick=cards;
    if($('#inv-pay'))$('#inv-pay').onclick=async()=>{try{await api('invoice_pay',{card_id:cardId,month,paid_date:$('#inv-paid-date').value});invoiceView(cardId,month)}catch(e){$('#inv-out').innerHTML=note(e.message,'err')}};
    if($('#inv-reopen'))$('#inv-reopen').onclick=async()=>{try{await api('invoice_reopen',{card_id:cardId,month});invoiceView(cardId,month)}catch(e){$('#inv-out').innerHTML=note(e.message,'err')}};
  }catch(e){content().innerHTML=note(e.message,'err')}
}

async function accounts(){
  activateNav('accounts');setTitle('Contas','Saldos e bancos');await loadContext();
  content().innerHTML=`<div class="heading"><div><h1>Contas</h1><p>Contas bancárias e saldos cadastrados</p></div><button class="primary" id="new-account">＋ Nova conta</button></div><div class="grid card-grid">${ctx.accounts.map(a=>`<div class="card"><small class="muted">${esc(a.bank_code||'Banco')}</small><h3>${esc(a.name)}</h3><div class="invoice-total">${money(a.current_balance)}</div><p class="muted">${esc(a.person_name||'Sem titular')}</p><button class="secondary account-edit" data-id="${a.id}">Editar</button></div>`).join('')}</div>`;
  $('#new-account').onclick=()=>accountForm();
  $$('.account-edit').forEach(b=>accountForm.bind(null,ctx.accounts.find(x=>Number(x.id)===Number(b.dataset.id))) && (b.onclick=()=>accountForm(ctx.accounts.find(x=>Number(x.id)===Number(b.dataset.id)))));
}
function accountForm(a=null){
  modal(`<h2>${a?'Editar':'Nova'} conta</h2><div class="form-grid"><div class="full"><label>Nome</label><input id="a-name" value="${esc(a?.name||'')}"></div><div><label>Banco</label><input id="a-bank" value="${esc(a?.bank_code||'other')}"></div><div><label>Tipo</label><select id="a-type"><option value="checking">Conta corrente</option><option value="digital">Digital</option><option value="savings">Poupança</option><option value="cash">Dinheiro</option></select></div><div><label>Saldo inicial</label><input id="a-initial" value="${a?.initial_balance||0}"></div><div><label>Saldo atual</label><input id="a-current" value="${a?.current_balance||0}"></div><div class="full"><label>Titular</label><select id="a-person">${options(ctx.people,a?.person_id||1)}</select></div><div class="full"><button class="primary" id="a-save">Salvar</button><div id="a-out"></div></div></div>`);
  if(a)$('#a-type').value=a.type||'checking';
  $('#a-save').onclick=async()=>{try{await api('account_save',{id:a?.id||0,name:$('#a-name').value,bank_code:$('#a-bank').value,type:$('#a-type').value,initial_balance:$('#a-initial').value,current_balance:$('#a-current').value,person_id:Number($('#a-person').value||0)});closeModal();accounts()}catch(e){$('#a-out').innerHTML=note(e.message,'err')}};
}

async function financings(){
  activateNav('financings');setTitle('Financiamentos','Parcelas e vencimentos');content().innerHTML='<div class="empty">Carregando...</div>';
  try{
    const d=await api('financings');
    content().innerHTML=`<div class="heading"><div><h1>Financiamentos</h1><p>Acompanhe o progresso e dê baixa na parcela atual.</p></div></div><div class="grid card-grid">${d.rows.map(f=>{const pct=Math.min(100,Number(f.paid_installments||0)/Math.max(1,Number(f.total_installments||1))*100);return `<div class="card"><h3>${esc(f.name)}</h3><p><b>${f.paid_installments}/${f.total_installments}</b> parcelas pagas</p><div class="progress"><span style="width:${pct}%"></span></div><p class="muted">Parcela: ${money(f.installment_amount)} · Próximo vencimento: ${brDate(f.next_due_date)}</p>${Number(f.active)?'<button class="primary fin-pay" data-id="'+f.id+'">✓ Marcar parcela atual como paga</button>':'<span class="badge paid">Concluído</span>'}</div>`}).join('')||'<div class="empty">Nenhum financiamento cadastrado.</div>'}</div>`;
    $$('.fin-pay').forEach(b=>b.onclick=()=>{modal('<h2>Pagamento da parcela</h2><label>Data do pagamento</label><input id="fin-paid" type="date" value="'+today()+'"><button class="primary" id="fin-confirm" style="margin-top:12px">Confirmar pagamento</button><div id="fin-out"></div>');$('#fin-confirm').onclick=async()=>{try{await api('financing_pay',{id:Number(b.dataset.id),paid_date:$('#fin-paid').value});closeModal();financings()}catch(e){$('#fin-out').innerHTML=note(e.message,'err')}}});
  }catch(e){content().innerHTML=note(e.message,'err')}
}

async function registry(){
  activateNav('registry');setTitle('Cadastros','Pessoas, casal e categorias');await loadContext();
  content().innerHTML=`<div class="heading"><div><h1>Cadastros</h1><p>Pessoas/casal e categorias</p></div></div><div class="grid two"><div class="card"><h3>Pessoas e casal</h3><div>${ctx.people.map(p=>'<p><b>'+esc(p.name)+'</b> <small class="muted">'+esc(p.kind)+'</small></p>').join('')}</div><button class="primary" id="person-add">＋ Cadastrar</button></div><div class="card"><h3>Categorias</h3><div style="max-height:320px;overflow:auto">${ctx.categories.map(c=>'<p>'+esc(c.name)+' <small class="muted">'+(c.kind==='income'?'Entrada':'Despesa')+'</small></p>').join('')}</div><button class="primary" id="cat-add">＋ Categoria</button></div></div>`;
  $('#person-add').onclick=()=>{modal('<h2>Nova pessoa/casal</h2><label>Nome exibido</label><input id="per-name"><label>Tipo</label><select id="per-kind"><option value="person">Pessoa</option><option value="couple">Casal</option></select><label>Nome do parceiro(a), se casal</label><input id="per-partner"><button class="primary" id="per-save" style="margin-top:12px">Salvar</button><div id="per-out"></div>');$('#per-save').onclick=async()=>{try{await api('person_add',{name:$('#per-name').value,kind:$('#per-kind').value,partner_name:$('#per-partner').value});closeModal();registry()}catch(e){$('#per-out').innerHTML=note(e.message,'err')}}};
  $('#cat-add').onclick=()=>{modal('<h2>Nova categoria</h2><label>Nome</label><input id="cat-name"><label>Tipo</label><select id="cat-kind"><option value="expense">Despesa</option><option value="income">Entrada</option></select><button class="primary" id="cat-save" style="margin-top:12px">Salvar</button><div id="cat-out"></div>');$('#cat-save').onclick=async()=>{try{await api('category_add',{name:$('#cat-name').value,kind:$('#cat-kind').value});closeModal();registry()}catch(e){$('#cat-out').innerHTML=note(e.message,'err')}}};
}


async function accessView(){
  activateNav('access');setTitle('iPhone & Usuários','Acesso local pela sua rede Wi-Fi');
  content().innerHTML='<div class="empty">Carregando acesso local...</div>';
  try{
    await loadContext();
    const [lan,users]=await Promise.all([api('lan:status',{}),api('users:list',{})]);
    const urls=(lan.urls||[]);
    let qr='';
    if(lan.running&&urls[0]){try{const q=await api('lan:qr',{url:urls[0]});qr=q.qr||''}catch(_){}}
    content().innerHTML=`
      <div class="heading"><div><h1>iPhone & Usuários</h1><p>O iPhone acessa este computador pela rede local; o MySQL continua protegido no Desktop.</p></div></div>
      <div class="grid two">
        <div class="card">
          <div class="access-head"><div><h3>Acesso pelo celular</h3><p class="muted">Use somente na sua rede Wi-Fi. Não encaminhe esta porta no roteador para a internet.</p></div>${lan.running?'<span class="badge paid">Ativo</span>':'<span class="badge pending">Desligado</span>'}</div>
          <div class="form-grid">
            <div><label>Porta local</label><input id="lan-port" type="number" min="1024" max="65535" value="${lan.port||8787}"></div>
            <div class="access-toggle-wrap"><label>Status</label><button class="${lan.running?'danger':'primary'}" id="lan-toggle">${lan.running?'Desligar acesso':'Ativar acesso'}</button></div>
          </div>
          <div id="lan-out"></div>
          ${lan.running?'<div class="lan-live"><div><b>Abra no iPhone:</b>'+urls.map(u=>'<a class="lan-url" href="'+esc(u)+'">'+esc(u)+'</a>').join('')+'<p class="muted">No Safari, use Compartilhar → Adicionar à Tela de Início.</p></div>'+(qr?'<img class="lan-qr" src="'+qr+'" alt="QR Code do GranaOk">':'')+'</div>':''}
        </div>
        <div class="card">
          <h3>Como funciona</h3>
          <p>1. Crie pelo menos um usuário abaixo.</p>
          <p>2. Ative o acesso pelo celular.</p>
          <p>3. Escaneie o QR Code com o iPhone.</p>
          <p>4. Entre com usuário e senha.</p>
          <div class="note">Usuário de acesso é diferente de Pessoa/Casal. O usuário serve para login; Pessoa/Casal continua identificando de quem é cada lançamento.</div>
        </div>
      </div>

      <div class="card" style="margin-top:16px">
        <div class="heading"><div><h3>Usuários de acesso</h3><p>Você pode criar mais de um login para o GranaOk.</p></div><button class="primary" id="user-new">＋ Novo usuário</button></div>
        <div class="table-wrap">
          ${users.rows.length?'<table class="table"><thead><tr><th>Nome</th><th>Usuário</th><th>Perfil</th><th>Vinculado a</th><th>Último acesso</th><th>Status</th><th></th></tr></thead><tbody>'+users.rows.map(u=>'<tr><td><b>'+esc(u.display_name)+'</b></td><td>'+esc(u.username)+'</td><td>'+esc(u.role==='admin'?'Administrador':'Usuário')+'</td><td>'+esc(u.person_name||'—')+'</td><td>'+esc(u.last_login_at||'Nunca')+'</td><td>'+(Number(u.active)?'<span class="badge paid">Ativo</span>':'<span class="badge pending">Inativo</span>')+'</td><td><button class="ghost user-pass" data-id="'+u.id+'">Senha</button><button class="ghost user-toggle" data-id="'+u.id+'" data-active="'+(Number(u.active)?'1':'0')+'">'+(Number(u.active)?'Desativar':'Ativar')+'</button></td></tr>').join('')+'</tbody></table>':'<div class="empty">Nenhum usuário de acesso criado ainda.</div>'}
        </div>
      </div>`;

    $('#lan-toggle').onclick=async()=>{
      const o=$('#lan-out');o.innerHTML=note(lan.running?'Desligando...':'Ativando...');
      try{
        if(lan.running) await api('lan:stop',{});
        else await api('lan:start',{port:Number($('#lan-port').value||8787)});
        accessView();
      }catch(e){o.innerHTML=note(e.message,'err')}
    };
    $('#user-new').onclick=()=>userForm();
    $$('.user-pass').forEach(b=>b.onclick=()=>passwordForm(Number(b.dataset.id)));
    $$('.user-toggle').forEach(b=>b.onclick=async()=>{try{await api('user:toggle',{id:Number(b.dataset.id),active:b.dataset.active!=='1'});accessView()}catch(e){alert(e.message)}});
  }catch(e){content().innerHTML=note(e.message,'err')}
}
function userForm(){
  modal(`<h2>Novo usuário de acesso</h2><div class="form-grid">
    <div><label>Nome exibido</label><input id="u-display" placeholder="Ex.: Wanessa"></div>
    <div><label>Usuário</label><input id="u-name" placeholder="Ex.: wanessa"></div>
    <div><label>Senha</label><input id="u-pass" type="password" placeholder="Mínimo 8 caracteres"></div>
    <div><label>Perfil</label><select id="u-role"><option value="user">Usuário</option><option value="admin">Administrador</option></select></div>
    <div class="full"><label>Vincular à Pessoa/Casal (opcional)</label><select id="u-person">${options(ctx.people,0)}</select></div>
    <div class="full"><button class="primary" id="u-save">Criar usuário</button><div id="u-out"></div></div>
  </div>`);
  $('#u-save').onclick=async()=>{try{await api('user:add',{display_name:$('#u-display').value,username:$('#u-name').value,password:$('#u-pass').value,role:$('#u-role').value,person_id:Number($('#u-person').value||0)});closeModal();accessView()}catch(e){$('#u-out').innerHTML=note(e.message,'err')}};
}
function passwordForm(id){
  modal(`<h2>Alterar senha</h2><label>Nova senha</label><input id="up-pass" type="password" placeholder="Mínimo 8 caracteres"><button class="primary" id="up-save" style="margin-top:12px">Salvar nova senha</button><div id="up-out"></div>`);
  $('#up-save').onclick=async()=>{try{await api('user:password',{id,password:$('#up-pass').value});closeModal();accessView()}catch(e){$('#up-out').innerHTML=note(e.message,'err')}};
}

async function radar(){
  activateNav('radar');setTitle('Radar de investimentos','Referências online');content().innerHTML='<div class="empty">Consultando Banco Central...</div>';
  try{
    const d=await api('radar');const selic=Number(String(d.selic?.valor||'0').replace(',','.')),ipca=Number(String(d.ipca?.valor||'0').replace(',','.'));
    content().innerHTML=`<div class="heading"><div><h1>Radar de investimentos</h1><p>Referências oficiais do Banco Central</p></div></div><div class="grid kpis"><div class="kpi"><small>Selic referência</small><b>${selic?selic.toLocaleString('pt-BR')+'%':'Indisponível'}</b><small>${esc(d.selic?.data||'')}</small></div><div class="kpi"><small>IPCA</small><b>${ipca?ipca.toLocaleString('pt-BR')+'%':'Indisponível'}</b><small>${esc(d.ipca?.data||'')}</small></div></div><div class="card" style="margin-top:16px"><h3>Simulador</h3><div class="form-grid"><div><label>Valor inicial</label><input id="sim-init" value="1000"></div><div><label>Aporte mensal</label><input id="sim-month" value="300"></div><div><label>Prazo (meses)</label><input id="sim-term" type="number" value="24"></div><div><label>Taxa anual (%)</label><input id="sim-rate" value="${selic||10}"></div><div class="full"><button class="primary" id="sim-run">Simular</button><div id="sim-out"></div></div></div></div>`;
    $('#sim-run').onclick=()=>{const init=Number($('#sim-init').value||0),monthly=Number($('#sim-month').value||0),n=Math.max(1,Number($('#sim-term').value||1)),annual=Number(String($('#sim-rate').value||0).replace(',','.'))/100,r=Math.pow(1+annual,1/12)-1;let v=init;for(let i=0;i<n;i++)v=v*(1+r)+monthly;const invested=init+monthly*n;$('#sim-out').innerHTML='<div class="note ok"><b>Valor projetado: '+money(v)+'</b><br>Capital aportado: '+money(invested)+' · rendimento bruto estimado: '+money(v-invested)+'</div>'};
  }catch(e){content().innerHTML=note(e.message,'err')}
}

async function route(v){try{if(v==='dashboard')return dashboard();if(v==='transactions')return transactions();if(v==='cards')return cards();if(v==='accounts')return accounts();if(v==='financings')return financings();if(v==='registry')return registry();if(v==='access')return accessView();if(v==='radar')return radar()}catch(e){content().innerHTML=note(e.message,'err')}}
$$('#nav button').forEach(b=>b.onclick=()=>{route(b.dataset.view);$('.sidebar').classList.remove('open')});
$('#refresh').onclick=()=>route(currentView);
$('#modal-close').onclick=closeModal;$('#modal').onclick=e=>{if(e.target===$('#modal'))closeModal()};

async function fillConfig(){
  const c=await window.granaok.invoke('config:get',{});
  $('#cfg-host').value=c.host||'';$('#cfg-port').value=c.port||3306;$('#cfg-db').value=c.database||'';$('#cfg-user').value=c.user||'';$('#cfg-prefix').value=c.prefix||'granaok_';$('#cfg-ssl').value=c.ssl?'1':'0';
  $('#cfg-pass').value='';$('#cfg-pass').placeholder=c.hasPassword?'Senha já salva — deixe em branco para manter':'Senha do MySQL';
  return c;
}
function configPayload(){
  return {host:$('#cfg-host').value.trim(),port:Number($('#cfg-port').value||3306),database:$('#cfg-db').value.trim(),user:$('#cfg-user').value.trim(),password:$('#cfg-pass').value,ssl:$('#cfg-ssl').value==='1',prefix:$('#cfg-prefix').value.trim()||'granaok_'};
}
$('#cfg-test').onclick=async()=>{const o=$('#cfg-out');o.innerHTML=note('Testando conexão...');const d=await window.granaok.invoke('config:test',configPayload());o.innerHTML=d.ok?note('Conexão OK · MySQL '+(d.version||''),'ok'):note(d.error||'Não foi possível conectar.','err')};
$('#cfg-save').onclick=async()=>{const o=$('#cfg-out');o.innerHTML=note('Salvando e conectando...');const d=await window.granaok.invoke('config:save',configPayload());if(!d.ok){o.innerHTML=note(d.error||'Falha ao salvar.','err');return}o.innerHTML=note(d.message||'Conexão salva.','ok');setTimeout(()=>{showApp();dashboard()},250)};
$('#settings').onclick=async()=>{await fillConfig();showLogin()};
$('#backup').onclick=async()=>{const d=await window.granaok.invoke('backup:save',{});if(d.ok)alert('Backup salvo em:\n'+d.file);else if(!d.canceled)alert(d.error||'Não foi possível salvar o backup.')};
(async()=>{const cfg=await fillConfig();if(!cfg.configured){showLogin();return}const t=await window.granaok.invoke('config:test',{});if(t.ok){showApp();dashboard()}else{showLogin();$('#cfg-out').innerHTML=note('A conexão salva não respondeu: '+(t.error||'erro desconhecido'),'err')}})();
})();