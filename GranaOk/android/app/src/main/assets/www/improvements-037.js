(()=>{
  const extras=()=>window.GranaExtras||null;
  const xEsc=v=>String(v??'').replace(/[&<>"']/g,m=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[m]));
  const xMoney=v=>Number(v||0).toLocaleString('pt-BR',{style:'currency',currency:'BRL'});
  const xNote=(t,c='')=>`<div class="note ${c}">${t}</div>`;
  const xJson=s=>{try{return JSON.parse(s)}catch{return {ok:false,error:'Resposta local inválida.'}}};
  let categoryContext='manage';

  const BANKS=[
    ['santander','Santander','S'],['inter','Banco Inter','inter'],['nubank','Nubank','nu'],
    ['itau','Itaú','itaú'],['bradesco','Bradesco','B'],['bb','Banco do Brasil','BB'],
    ['caixa','Caixa','CAIXA'],['sicredi','Sicredi','Sicredi'],['mercadopago','Mercado Pago','MP'],
    ['neon','Neon','Neon'],['picpay','PicPay','P'],['c6','C6 Bank','C6'],['btg','BTG Pactual','BTG'],
    ['other','Outro / Carteira','🏦']
  ];
  const bankLabel=code=>(BANKS.find(x=>x[0]===code)||BANKS[BANKS.length-1]);
  const bankBadge=code=>{const b=bankLabel(code);return `<span class="bank-icon bank-${xEsc(b[0])}">${xEsc(b[2])}</span>`};

  const baseCadastros=window.cadastros;
  window.cadastros=function(){
    window.shell(`${window.brand()}<div class="heading"><div><h2>Cadastros</h2><div class="muted">Organize sua estrutura financeira</div></div></div><div class="finance-menu"><button class="finance-menu-card" onclick="accountsView()"><span class="finance-menu-icon">🏦</span><div><b>Contas</b><small>Bancos, carteiras e saldos</small></div><span>›</span></button><button class="finance-menu-card" onclick="cardsView()"><span class="finance-menu-icon">💳</span><div><b>Cartões de crédito</b><small>Limite, fechamento, vencimento e fatura</small></div><span>›</span></button><button class="finance-menu-card" onclick="financingsView()"><span class="finance-menu-icon">📄</span><div><b>Financiamentos</b><small>Parcelas, valor mensal e progresso</small></div><span>›</span></button><button class="finance-menu-card" onclick="categoriesView()"><span class="finance-menu-icon">🏷️</span><div><b>Categorias</b><small>Despesas e entradas personalizadas</small></div><span>›</span></button></div><div class="card"><h3>Resumo</h3><div id="finance-overview">${xNote('Consultando MySQL...')}</div></div>`,true);
    try{window.GranaFinance?.loadOverview?.()}catch(e){const el=document.getElementById('finance-overview');if(el)el.innerHTML=xNote('Bridge financeiro indisponível.','err')}
  };

  window.accountsView=function(){
    window.shell(`${window.brand()}<div class="heading"><div><h2>Contas</h2><div class="muted">Saldos por banco ou carteira</div></div><button class="mini primary" onclick="newAccountView()">＋ Nova</button></div><div id="accounts-list">${xNote('Carregando contas...')}</div>`,true);
    try{extras()?.loadAccounts?.()}catch(e){document.getElementById('accounts-list').innerHTML=xNote('Não foi possível abrir as contas.','err')}
  };

  window.GranaOkAccountsPlus=s=>{
    const d=xJson(s),el=document.getElementById('accounts-list');if(!el)return;
    if(!d.ok){el.innerHTML=xNote(xEsc(d.error||'Erro ao carregar contas.'),'err');return}
    const labels={bank:'Banco',checking:'Conta corrente',savings:'Poupança',digital:'Conta digital',cash:'Carteira'};
    el.innerHTML=(d.rows||[]).map(x=>`<div class="card finance-list-card account-with-bank"><div class="bank-account-left">${bankBadge(x.bank_code||'other')}<div><div class="finance-title">${xEsc(x.name)}</div><small>${xEsc(bankLabel(x.bank_code||'other')[1])} · ${xEsc(labels[x.type]||x.type||'Conta')}</small></div></div><div class="finance-value"><b>${xMoney(x.current_balance)}</b><small>Inicial ${xMoney(x.initial_balance)}</small></div></div>`).join('')||`<div class="empty-finance"><span>🏦</span><b>Nenhuma conta cadastrada</b><p>Cadastre suas contas para acompanhar o saldo disponível.</p><button class="primary" onclick="newAccountView()">Cadastrar primeira conta</button></div>`;
  };

  window.newAccountView=function(){
    const opts=BANKS.map(b=>`<option value="${b[0]}">${xEsc(b[1])}</option>`).join('');
    window.shell(`${window.brand()}<h2>Nova conta</h2><div class="card"><label>Banco / instituição</label><select id="acc-bank">${opts}</select><label>Nome da conta</label><input id="acc-name" placeholder="Ex.: Conta principal"><label>Tipo</label><select id="acc-type"><option value="checking">Conta corrente</option><option value="digital">Conta digital</option><option value="savings">Poupança</option><option value="cash">Carteira / dinheiro</option><option value="bank">Outra conta bancária</option></select><div class="row"><div><label>Saldo inicial</label><input id="acc-initial" inputmode="decimal" placeholder="0,00"></div><div><label>Saldo atual</label><input id="acc-current" inputmode="decimal" placeholder="Igual ao inicial"></div></div><button class="primary" id="acc-save">Salvar conta</button><button class="link" onclick="accountsView()">Cancelar</button><div id="out"></div></div>`,true);
    const bank=document.getElementById('acc-bank'),name=document.getElementById('acc-name');
    bank.onchange=()=>{if(!name.value.trim()){const b=bankLabel(bank.value);if(bank.value!=='other')name.value=b[1]}};
    document.getElementById('acc-save').onclick=()=>{
      const n=name.value.trim();if(!n){document.getElementById('out').innerHTML=xNote('Informe o nome da conta.','err');return}
      const initial=document.getElementById('acc-initial').value.trim()||'0',current=document.getElementById('acc-current').value.trim()||initial;
      document.getElementById('out').innerHTML=xNote('Salvando conta...');
      extras()?.addAccount?.(JSON.stringify({name:n,bankCode:bank.value,type:document.getElementById('acc-type').value,initialBalance:initial,currentBalance:current}));
    };
  };
  window.GranaOkAccountPlusSaved=s=>{const d=xJson(s),el=document.getElementById('out');if(!el)return;if(!d.ok){el.innerHTML=xNote(xEsc(d.error||'Erro ao salvar conta.'),'err');return}el.innerHTML=xNote('Conta cadastrada com sucesso.','ok');setTimeout(accountsView,400)};

  window.categoriesView=function(kind='expense'){
    categoryContext='manage';
    window.shell(`${window.brand()}<div class="heading"><div><h2>Categorias</h2><div class="muted">Organize entradas e despesas</div></div><button class="mini primary" onclick="newCategoryView()">＋ Nova</button></div><div class="category-tabs"><button class="${kind==='expense'?'active':''}" onclick="categoriesView('expense')">Despesas</button><button class="${kind==='income'?'active':''}" onclick="categoriesView('income')">Entradas</button></div><div id="categories-list">${xNote('Carregando categorias...')}</div>`,true);
    try{extras()?.loadCategories?.(kind)}catch(e){document.getElementById('categories-list').innerHTML=xNote('Não foi possível carregar categorias.','err')}
  };

  window.GranaOkCategories=s=>{
    const d=xJson(s);
    if(categoryContext==='transaction'){
      const sel=document.getElementById('txcat');if(!sel)return;
      if(!d.ok){sel.innerHTML='<option value="Outros">Outros</option>';return}
      sel.innerHTML=(d.rows||[]).map(x=>`<option value="${xEsc(x.name)}">${xEsc(x.name)}</option>`).join('');
      return;
    }
    const el=document.getElementById('categories-list');if(!el)return;
    if(!d.ok){el.innerHTML=xNote(xEsc(d.error||'Erro ao carregar categorias.'),'err');return}
    el.innerHTML=`<div class="category-grid">${(d.rows||[]).map(x=>`<div class="category-pill"><span>${x.kind==='income'?'↗':'↘'}</span><b>${xEsc(x.name)}</b></div>`).join('')}</div>`;
  };

  window.newCategoryView=function(){
    window.shell(`${window.brand()}<h2>Nova categoria</h2><div class="card"><label>Tipo</label><select id="cat-kind"><option value="expense">Despesa</option><option value="income">Entrada</option></select><label>Nome da categoria</label><input id="cat-name" placeholder="Ex.: Academia"><button class="primary" id="cat-save">Salvar categoria</button><button class="link" onclick="categoriesView()">Cancelar</button><div id="out"></div></div>`,true);
    document.getElementById('cat-save').onclick=()=>{const name=document.getElementById('cat-name').value.trim(),kind=document.getElementById('cat-kind').value;if(!name){document.getElementById('out').innerHTML=xNote('Informe o nome da categoria.','err');return}document.getElementById('out').innerHTML=xNote('Salvando categoria...');extras()?.addCategory?.(JSON.stringify({name,kind}))};
  };
  window.GranaOkCategorySaved=s=>{const d=xJson(s),el=document.getElementById('out');if(!el)return;if(!d.ok){el.innerHTML=xNote(xEsc(d.error||'Erro ao salvar categoria.'),'err');return}el.innerHTML=xNote('Categoria cadastrada.','ok');const k=document.getElementById('cat-kind')?.value||'expense';setTimeout(()=>categoriesView(k),400)};

  window.newTransaction=function(){
    window.shell(`${window.brand()}<h2>Novo lançamento</h2><div class="card"><label>Tipo</label><select id="txtype"><option value="expense">Despesa</option><option value="income">Entrada</option></select><label>Descrição</label><input id="txdesc" placeholder="Ex.: Supermercado"><label>Categoria</label><select id="txcat"><option>Carregando...</option></select><div class="row"><div><label id="amount-label">Valor total</label><input id="txamount" inputmode="decimal" placeholder="0,00"></div><div><label>Primeiro vencimento</label><input id="txdate" type="date" value="${window.today()}"></div></div><div id="installment-box"><label>Parcelamento</label><div class="installment-row"><select id="txinstall-mode"><option value="1">À vista</option><option value="2">Parcelada</option></select><input id="txinstallments" inputmode="numeric" value="2" min="2" max="120" disabled></div><p class="hint" id="installment-preview">À vista: um único lançamento.</p></div><button class="primary" id="save">Salvar lançamento</button><button class="link" onclick="transactions()">Cancelar</button><div id="out"></div></div>`);
    const type=document.getElementById('txtype'),mode=document.getElementById('txinstall-mode'),count=document.getElementById('txinstallments'),amount=document.getElementById('txamount');
    const loadCats=()=>{categoryContext='transaction';extras()?.loadCategories?.(type.value)};
    const refresh=()=>{const expense=type.value==='expense';document.getElementById('installment-box').style.display=expense?'block':'none';document.getElementById('amount-label').textContent=expense?'Valor total':'Valor';loadCats();};
    const preview=()=>{const n=mode.value==='2'?Math.max(2,Number(count.value||2)):1;count.disabled=mode.value!=='2';const raw=Number(String(amount.value||'0').replace('.','').replace(',','.'))||0;document.getElementById('installment-preview').textContent=n>1?`${n} parcelas de aproximadamente ${xMoney(raw/n)}. O último vencimento pode ajustar centavos.`:'À vista: um único lançamento.'};
    type.onchange=refresh;mode.onchange=preview;count.oninput=preview;amount.oninput=preview;document.getElementById('save').onclick=window.saveTransaction;refresh();preview();
  };

  window.saveTransaction=function(){
    const type=document.getElementById('txtype').value,desc=document.getElementById('txdesc').value.trim(),raw=document.getElementById('txamount').value.trim();
    const normalized=raw.replace('.','').replace(',','.');
    if(!desc||!normalized||Number(normalized)<=0){document.getElementById('out').innerHTML=xNote('Informe descrição e valor.','err');return}
    const category=document.getElementById('txcat').value||'Outros',dueDate=document.getElementById('txdate').value||window.today();
    document.getElementById('out').innerHTML=xNote('Salvando lançamento...');
    if(type==='expense'){
      const installments=document.getElementById('txinstall-mode').value==='2'?Math.max(2,Math.min(120,Number(document.getElementById('txinstallments').value||2))):1;
      extras()?.addExpense?.(JSON.stringify({description:desc,category,totalAmount:normalized,dueDate,installments}));
    }else{
      window.GranaNative?.addTransaction?.(JSON.stringify({type:'income',description:desc,category,amount:normalized,dueDate}));
    }
  };

  window.GranaOkExpenseSaved=s=>{const d=xJson(s),el=document.getElementById('out');if(!el)return;if(!d.ok){el.innerHTML=xNote(xEsc(d.error||'Erro ao salvar despesa.'),'err');return}el.innerHTML=xNote(xEsc(d.message||'Despesa salva.'),'ok');setTimeout(window.transactions,550)};
})();
