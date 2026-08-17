(()=>{
  const manage=()=>window.GranaManage||null;
  const mEsc=v=>String(v??'').replace(/[&<>"']/g,m=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[m]));
  const mMoney=v=>Number(v||0).toLocaleString('pt-BR',{style:'currency',currency:'BRL'});
  const mJson=s=>{try{return JSON.parse(s)}catch{return {ok:false,error:'Resposta local inválida.'}}};
  const mNote=(t,c='')=>`<div class="note ${c}">${t}</div>`;
  const currentMonth=()=>{const d=new Date();return `${d.getFullYear()}-${String(d.getMonth()+1).padStart(2,'0')}`};
  const shiftMonth=(month,delta)=>{const [y,m]=month.split('-').map(Number),d=new Date(y,m-1+delta,1,12);return `${d.getFullYear()}-${String(d.getMonth()+1).padStart(2,'0')}`};
  const monthLabel=month=>{const [y,m]=month.split('-').map(Number),d=new Date(y,m-1,1,12);const x=new Intl.DateTimeFormat('pt-BR',{month:'long',year:'numeric'}).format(d);return x.charAt(0).toUpperCase()+x.slice(1)};
  const statusLabel=s=>({paid:'Pago',pending:'Pendente',overdue:'Atrasado'}[s]||'Pendente');
  const typeLabel=s=>s==='income'?'Entrada':'Despesa';

  let txMonth=currentMonth();
  let txFilters={type:'',status:'',category:'',search:''};
  let txRows=new Map();
  let txCategories=[];
  let accountRows=new Map();

  const BANKS=[
    ['santander','Santander','S'],['inter','Banco Inter','inter'],['nubank','Nubank','nu'],
    ['itau','Itaú','itaú'],['bradesco','Bradesco','B'],['bb','Banco do Brasil','BB'],
    ['caixa','Caixa','CAIXA'],['sicredi','Sicredi','Sicredi'],['sicoob','Sicoob','Sicoob'],
    ['mercadopago','Mercado Pago','MP'],['neon','Neon','Neon'],['picpay','PicPay','P'],
    ['c6','C6 Bank','C6'],['btg','BTG Pactual','BTG'],['other','Outro / Carteira','🏦']
  ];
  const bank=code=>(BANKS.find(x=>x[0]===code)||BANKS[BANKS.length-1]);
  const bankBadge=code=>{const b=bank(code);return `<span class="bank-icon bank-${mEsc(b[0])}">${mEsc(b[2])}</span>`};

  function filtersPayload(){return JSON.stringify({month:txMonth,...txFilters})}
  function loadManagedTransactions(){
    const el=document.getElementById('txlist');
    if(el)el.innerHTML=mNote(`Carregando ${mEsc(monthLabel(txMonth))}...`);
    try{manage()?.loadTransactions?.(filtersPayload())}catch(e){if(el)el.innerHTML=mNote('Gestão de lançamentos indisponível.','err')}
  }

  window.transactions=function(){
    window.shell(`${window.brand()}<div class="heading tx-heading"><div><h2>Lançamentos</h2><div class="muted">Edite, filtre e acompanhe por mês</div></div><button class="mini primary" onclick="newTransaction()">＋ Novo</button></div>
      <div class="card tx-toolbar">
        <div class="tx-month-nav"><button onclick="changeTxMonth(-1)">‹</button><strong id="tx-month-label">${mEsc(monthLabel(txMonth))}</strong><button onclick="changeTxMonth(1)">›</button></div>
        <div class="tx-filters">
          <select id="tx-filter-type"><option value="">Todos os tipos</option><option value="expense">Despesas</option><option value="income">Entradas</option></select>
          <select id="tx-filter-status"><option value="">Todos os status</option><option value="pending">Pendentes</option><option value="paid">Pagos</option><option value="overdue">Atrasados</option></select>
          <select id="tx-filter-category"><option value="">Todas as categorias</option></select>
        </div>
        <div class="tx-search-row"><input id="tx-filter-search" placeholder="Buscar descrição ou observação"><button class="secondary" id="tx-filter-go">Filtrar</button><button class="link compact-link" id="tx-filter-clear">Limpar</button></div>
      </div>
      <div id="tx-summary"></div><div id="txlist">${mNote('Consultando lançamentos...')}</div>`,true);
    document.getElementById('tx-filter-type').value=txFilters.type;
    document.getElementById('tx-filter-status').value=txFilters.status;
    document.getElementById('tx-filter-search').value=txFilters.search;
    document.getElementById('tx-filter-type').onchange=e=>{txFilters.type=e.target.value;loadManagedTransactions()};
    document.getElementById('tx-filter-status').onchange=e=>{txFilters.status=e.target.value;loadManagedTransactions()};
    document.getElementById('tx-filter-category').onchange=e=>{txFilters.category=e.target.value;loadManagedTransactions()};
    document.getElementById('tx-filter-go').onclick=()=>{txFilters.search=document.getElementById('tx-filter-search').value.trim();loadManagedTransactions()};
    document.getElementById('tx-filter-search').onkeydown=e=>{if(e.key==='Enter'){txFilters.search=e.target.value.trim();loadManagedTransactions()}};
    document.getElementById('tx-filter-clear').onclick=()=>{txFilters={type:'',status:'',category:'',search:''};transactions()};
    loadManagedTransactions();
  };

  window.changeTxMonth=function(delta){txMonth=shiftMonth(txMonth,Number(delta)||0);txFilters.category='';const label=document.getElementById('tx-month-label');if(label)label.textContent=monthLabel(txMonth);loadManagedTransactions()};

  window.GranaOkManagedTransactions=function(s){
    const d=mJson(s),el=document.getElementById('txlist');if(!el)return;
    if(!d.ok){el.innerHTML=mNote(mEsc(d.error||'Erro ao carregar lançamentos.'),'err');return}
    txRows=new Map((d.rows||[]).map(x=>[Number(x.id),x])); txCategories=d.categories||[];
    const cat=document.getElementById('tx-filter-category');
    if(cat){cat.innerHTML='<option value="">Todas as categorias</option>'+txCategories.map(x=>`<option value="${mEsc(x)}">${mEsc(x)}</option>`).join('');cat.value=txFilters.category}
    const summary=document.getElementById('tx-summary');
    if(summary)summary.innerHTML=`<div class="tx-summary-grid"><div><span>Encontrados</span><b>${Number(d.count||0)}</b></div><div><span>Entradas</span><b class="positive">${mMoney(d.income)}</b></div><div><span>Despesas</span><b class="negative">${mMoney(d.expenses)}</b></div></div>`;
    el.innerHTML=(d.rows||[]).map(x=>{
      const effective=x.effective_status||x.status||'pending';
      const autoOverdue=effective==='overdue'&&x.status==='pending';
      return `<div class="card managed-tx ${mEsc(effective)}">
        <div class="managed-tx-top"><div class="managed-tx-title"><span class="tx-type-dot ${x.type==='income'?'income':'expense'}"></span><div><b>${mEsc(x.description)}</b><small>${mEsc(x.category||'Outros')} · ${mEsc(x.due_date)}</small></div></div><strong class="${x.type==='income'?'positive':'negative'}">${x.type==='income'?'+':'-'} ${mMoney(x.amount)}</strong></div>
        <div class="managed-tx-meta"><span class="status-badge ${mEsc(effective)}">${statusLabel(effective)}${autoOverdue?' · automático':''}</span>${Number(x.installment_total||1)>1?`<span class="installment-badge">Parcela ${x.installment_number}/${x.installment_total}</span>`:''}${x.paid_date?`<span>Pago em ${mEsc(x.paid_date)}</span>`:''}</div>
        ${x.observations?`<div class="tx-observation">📝 ${mEsc(x.observations)}</div>`:''}
        <div class="managed-tx-actions"><select aria-label="Alterar status" onchange="quickTxStatus(${Number(x.id)},this.value)"><option value="pending" ${x.status==='pending'?'selected':''}>Pendente</option><option value="paid" ${x.status==='paid'?'selected':''}>Pago</option><option value="overdue" ${x.status==='overdue'?'selected':''}>Atrasado</option></select><button class="secondary mini-edit" onclick="editTransactionView(${Number(x.id)})">✎ Editar</button></div>
      </div>`
    }).join('')||`<div class="empty-finance"><span>↕</span><b>Nenhum lançamento encontrado</b><p>Tente outro mês ou altere os filtros.</p></div>`;
  };

  window.quickTxStatus=function(id,status){
    const row=txRows.get(Number(id)); if(row)row.status=status;
    try{manage()?.setTransactionStatus?.(Number(id),status)}catch(e){}
  };
  window.GranaOkManagedStatusUpdated=function(s){const d=mJson(s);if(!d.ok){alert(d.error||'Não foi possível alterar o status.');return}loadManagedTransactions()};

  window.editTransactionView=function(id){
    const x=txRows.get(Number(id));if(!x)return;
    const cats=[...new Set([x.category||'Outros',...txCategories])];
    const catOpts=cats.map(c=>`<option value="${mEsc(c)}" ${c===(x.category||'Outros')?'selected':''}>${mEsc(c)}</option>`).join('');
    window.shell(`${window.brand()}<div class="heading"><div><h2>Editar lançamento</h2><div class="muted">${Number(x.installment_total||1)>1?`Parcela ${x.installment_number}/${x.installment_total} · altera somente esta parcela`:'Atualize os dados do lançamento'}</div></div></div><div class="card edit-form">
      <label>Tipo</label><select id="edit-tx-type"><option value="expense" ${x.type==='expense'?'selected':''}>Despesa</option><option value="income" ${x.type==='income'?'selected':''}>Entrada</option></select>
      <label>Descrição</label><input id="edit-tx-desc" value="${mEsc(x.description)}">
      <label>Categoria</label><select id="edit-tx-cat">${catOpts}</select>
      <div class="row"><div><label>Valor</label><input id="edit-tx-amount" inputmode="decimal" value="${Number(x.amount||0).toFixed(2).replace('.',',')}"></div><div><label>Vencimento</label><input id="edit-tx-date" type="date" value="${mEsc(x.due_date)}"></div></div>
      <label>Status</label><select id="edit-tx-status"><option value="pending" ${x.status==='pending'?'selected':''}>Pendente</option><option value="paid" ${x.status==='paid'?'selected':''}>Pago</option><option value="overdue" ${x.status==='overdue'?'selected':''}>Atrasado</option></select>
      <label>Observações</label><textarea id="edit-tx-obs" rows="4" placeholder="Informações adicionais, comprovante, motivo, detalhes...">${mEsc(x.observations||'')}</textarea>
      <button class="primary" id="edit-tx-save">Salvar alterações</button><button class="link" onclick="transactions()">Cancelar</button><div id="out"></div></div>`,true);
    document.getElementById('edit-tx-save').onclick=()=>{
      const raw=document.getElementById('edit-tx-amount').value.trim().replace(/\./g,'').replace(',','.');
      const desc=document.getElementById('edit-tx-desc').value.trim();
      if(!desc||!raw||Number(raw)<=0){document.getElementById('out').innerHTML=mNote('Confira descrição e valor.','err');return}
      document.getElementById('out').innerHTML=mNote('Salvando alterações...');
      manage()?.updateTransaction?.(JSON.stringify({id:Number(id),type:document.getElementById('edit-tx-type').value,description:desc,category:document.getElementById('edit-tx-cat').value,amount:raw,dueDate:document.getElementById('edit-tx-date').value,status:document.getElementById('edit-tx-status').value,observations:document.getElementById('edit-tx-obs').value.trim()}));
    };
  };
  window.GranaOkManagedTransactionUpdated=function(s){const d=mJson(s),out=document.getElementById('out');if(!d.ok){if(out)out.innerHTML=mNote(mEsc(d.error||'Erro ao atualizar lançamento.'),'err');return}if(out)out.innerHTML=mNote('Lançamento atualizado.','ok');setTimeout(transactions,450)};

  const baseNewTransaction=window.newTransaction;
  window.newTransaction=function(){
    if(typeof baseNewTransaction==='function')baseNewTransaction();
    const save=document.getElementById('save'); if(!save)return;
    if(!document.getElementById('txobs'))save.insertAdjacentHTML('beforebegin','<label>Observações</label><textarea id="txobs" rows="3" placeholder="Opcional: detalhes do lançamento"></textarea>');
    save.onclick=saveManagedTransaction;
  };
  function saveManagedTransaction(){
    const type=document.getElementById('txtype').value,desc=document.getElementById('txdesc').value.trim(),raw=document.getElementById('txamount').value.trim();
    const normalized=raw.replace(/\./g,'').replace(',','.'); const out=document.getElementById('out');
    if(!desc||!normalized||Number(normalized)<=0){out.innerHTML=mNote('Informe descrição e valor.','err');return}
    const category=document.getElementById('txcat')?.value||'Outros',dueDate=document.getElementById('txdate').value||window.today();
    const installments=type==='expense'&&document.getElementById('txinstall-mode')?.value==='2'?Math.max(2,Math.min(120,Number(document.getElementById('txinstallments').value||2))):1;
    out.innerHTML=mNote('Salvando lançamento...');
    manage()?.addTransaction?.(JSON.stringify({type,description:desc,category,totalAmount:normalized,dueDate,installments,observations:document.getElementById('txobs')?.value.trim()||''}));
  }
  window.GranaOkManagedTransactionSaved=function(s){const d=mJson(s),out=document.getElementById('out');if(!d.ok){if(out)out.innerHTML=mNote(mEsc(d.error||'Erro ao salvar lançamento.'),'err');return}if(out)out.innerHTML=mNote(mEsc(d.message||'Lançamento salvo.'),'ok');setTimeout(transactions,500)};

  window.GranaOkAccountsPlus=function(s){
    const d=mJson(s),el=document.getElementById('accounts-list');if(!el)return;
    if(!d.ok){el.innerHTML=mNote(mEsc(d.error||'Erro ao carregar contas.'),'err');return}
    accountRows=new Map((d.rows||[]).map(x=>[Number(x.id),x]));
    const labels={bank:'Banco',checking:'Conta corrente',savings:'Poupança',digital:'Conta digital',cash:'Carteira'};
    el.innerHTML=(d.rows||[]).map(x=>`<div class="card finance-list-card account-with-bank ${x.active?'':'inactive-account'}"><div class="bank-account-left">${bankBadge(x.bank_code||'other')}<div><div class="finance-title">${mEsc(x.name)}</div><small>${mEsc(bank(x.bank_code||'other')[1])} · ${mEsc(labels[x.type]||x.type||'Conta')}${x.active?'':' · Inativa'}</small></div></div><div class="finance-value"><b>${mMoney(x.current_balance)}</b><button class="secondary account-edit-btn" onclick="editAccountView(${Number(x.id)})">✎ Editar</button></div></div>`).join('')||`<div class="empty-finance"><span>🏦</span><b>Nenhuma conta cadastrada</b><p>Cadastre suas contas para acompanhar o saldo disponível.</p><button class="primary" onclick="newAccountView()">Cadastrar primeira conta</button></div>`;
  };

  window.editAccountView=function(id){
    const x=accountRows.get(Number(id));if(!x)return;
    const bankOpts=BANKS.map(b=>`<option value="${b[0]}" ${b[0]===(x.bank_code||'other')?'selected':''}>${mEsc(b[1])}</option>`).join('');
    window.shell(`${window.brand()}<h2>Editar conta</h2><div class="card edit-form"><label>Banco / instituição</label><select id="edit-acc-bank">${bankOpts}</select><label>Nome da conta</label><input id="edit-acc-name" value="${mEsc(x.name)}"><label>Tipo</label><select id="edit-acc-type"><option value="checking" ${x.type==='checking'?'selected':''}>Conta corrente</option><option value="digital" ${x.type==='digital'?'selected':''}>Conta digital</option><option value="savings" ${x.type==='savings'?'selected':''}>Poupança</option><option value="cash" ${x.type==='cash'?'selected':''}>Carteira / dinheiro</option><option value="bank" ${x.type==='bank'?'selected':''}>Outra conta bancária</option></select><div class="row"><div><label>Saldo inicial</label><input id="edit-acc-initial" inputmode="decimal" value="${Number(x.initial_balance||0).toFixed(2).replace('.',',')}"></div><div><label>Saldo atual</label><input id="edit-acc-current" inputmode="decimal" value="${Number(x.current_balance||0).toFixed(2).replace('.',',')}"></div></div><label class="check-row"><input type="checkbox" id="edit-acc-active" ${x.active?'checked':''}> Conta ativa</label><button class="primary" id="edit-acc-save">Salvar alterações</button><button class="link" onclick="accountsView()">Cancelar</button><div id="out"></div></div>`,true);
    document.getElementById('edit-acc-save').onclick=()=>{
      const name=document.getElementById('edit-acc-name').value.trim();if(!name){document.getElementById('out').innerHTML=mNote('Informe o nome da conta.','err');return}
      document.getElementById('out').innerHTML=mNote('Salvando conta...');
      manage()?.updateAccount?.(JSON.stringify({id:Number(id),name,bankCode:document.getElementById('edit-acc-bank').value,type:document.getElementById('edit-acc-type').value,initialBalance:document.getElementById('edit-acc-initial').value,currentBalance:document.getElementById('edit-acc-current').value,active:document.getElementById('edit-acc-active').checked}));
    };
  };
  window.GranaOkManagedAccountUpdated=function(s){const d=mJson(s),out=document.getElementById('out');if(!d.ok){if(out)out.innerHTML=mNote(mEsc(d.error||'Erro ao atualizar conta.'),'err');return}if(out)out.innerHTML=mNote('Conta atualizada.','ok');setTimeout(window.accountsView,450)};
})();
