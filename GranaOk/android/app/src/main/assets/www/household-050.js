(()=>{
  const hh=()=>window.GranaHousehold||null;
  const hEsc=v=>String(v??'').replace(/[&<>"']/g,m=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[m]));
  const hMoney=v=>Number(v||0).toLocaleString('pt-BR',{style:'currency',currency:'BRL'});
  const hJson=s=>{try{return JSON.parse(s)}catch{return {ok:false,error:'Resposta local inválida.'}}};
  const hNote=(t,c='')=>`<div class="note ${c}">${t}</div>`;
  const hToday=()=>typeof window.today==='function'?window.today():new Date().toISOString().slice(0,10);
  const hMonth=()=>{const d=new Date();return `${d.getFullYear()}-${String(d.getMonth()+1).padStart(2,'0')}`};
  const shiftMonth=(month,delta)=>{const [y,m]=String(month).split('-').map(Number),d=new Date(y,m-1+delta,1,12);return `${d.getFullYear()}-${String(d.getMonth()+1).padStart(2,'0')}`};
  const monthLabel=month=>{const [y,m]=String(month).split('-').map(Number),d=new Date(y,m-1,1,12);const s=new Intl.DateTimeFormat('pt-BR',{month:'long',year:'numeric'}).format(d);return s.charAt(0).toUpperCase()+s.slice(1)};
  const num=v=>{const s=String(v??'').trim().replace(/R\$/gi,'').replace(/\s/g,'');return Number(s.includes(',')?s.replace(/\./g,'').replace(',','.'):s)};

  const BANKS=[
    ['santander','Santander'],['inter','Banco Inter'],['nubank','Nubank'],['itau','Itaú'],['bradesco','Bradesco'],['bb','Banco do Brasil'],
    ['caixa','Caixa'],['sicredi','Sicredi'],['sicoob','Sicoob'],['mercadopago','Mercado Pago'],['neon','Neon'],['picpay','PicPay'],['c6','C6 Bank'],['btg','BTG Pactual'],['other','Outro / Carteira']
  ];
  const bankName=c=>(BANKS.find(x=>x[0]===c)||BANKS[BANKS.length-1])[1];

  let context={people:[],accounts:[],cards:[]};
  let contextBusy=false;
  let contextWaiters=[];
  let invoiceCardId=0;
  let invoiceMonth=hMonth();

  function requestContext(fn){
    if(typeof fn==='function')contextWaiters.push(fn);
    if(contextBusy)return;
    contextBusy=true;
    try{hh()?.loadContext?.()}catch(e){contextBusy=false;const list=contextWaiters.splice(0);list.forEach(f=>f({ok:false,error:String(e)}))}
  }

  window.GranaOkHouseholdContext=function(s){
    const d=hJson(s);contextBusy=false;
    if(d.ok)context={people:d.people||[],accounts:d.accounts||[],cards:d.cards||[]};
    const list=contextWaiters.splice(0);list.forEach(f=>{try{f(d)}catch(e){}});
  };

  const peopleOptions=(selected=0,allowNone=true)=>`${allowNone?'<option value="0">Sem vincular</option>':''}${context.people.filter(x=>x.active!==false).map(x=>`<option value="${Number(x.id)}" ${Number(x.id)===Number(selected)?'selected':''}>${hEsc(x.kind==='couple'?'👥 ':'👤 ')}${hEsc(x.name)}</option>`).join('')}`;
  const accountOptions=(selected=0)=>`<option value="0">Sem conta específica</option>${context.accounts.filter(x=>x.active!==false).map(x=>`<option value="${Number(x.id)}" ${Number(x.id)===Number(selected)?'selected':''}>${hEsc(x.name)} · ${hEsc(bankName(x.bank_code||'other'))}</option>`).join('')}`;
  const cardOptions=(selected=0)=>context.cards.filter(x=>x.active!==false).map(x=>`<option value="${Number(x.id)}" ${Number(x.id)===Number(selected)?'selected':''}>${hEsc(x.name)}</option>`).join('');

  window.cadastros=function(){
    window.shell(`${window.brand()}<div class="heading"><div><h2>Cadastros</h2><div class="muted">Família, contas, cartões e compromissos</div></div></div>
      <div class="finance-menu">
        <button class="finance-menu-card" onclick="peopleView()"><span class="finance-menu-icon">👥</span><div><b>Pessoas e casal</b><small>Separe ou compartilhe a organização financeira</small></div><span>›</span></button>
        <button class="finance-menu-card" onclick="accountsView()"><span class="finance-menu-icon">🏦</span><div><b>Contas</b><small>Associe cada conta a uma pessoa ou ao casal</small></div><span>›</span></button>
        <button class="finance-menu-card" onclick="cardsView()"><span class="finance-menu-icon">💳</span><div><b>Cartões de crédito</b><small>Compras, parcelamentos e faturas por cartão</small></div><span>›</span></button>
        <button class="finance-menu-card" onclick="financingsView()"><span class="finance-menu-icon">📄</span><div><b>Financiamentos</b><small>Parcelas, valor mensal e progresso</small></div><span>›</span></button>
      </div><div class="card"><h3>Resumo</h3><div id="finance-overview">${hNote('Consultando estrutura financeira...')}</div></div>`,true);
    try{window.GranaFinance?.loadOverview?.()}catch(e){const el=document.getElementById('finance-overview');if(el)el.innerHTML=hNote('Resumo indisponível.','err')}
  };

  window.peopleView=function(){
    window.shell(`${window.brand()}<div class="heading"><div><h2>Pessoas e casal</h2><div class="muted">Quem participa do orçamento</div></div><div class="heading-actions"><button class="mini secondary" onclick="newPersonView('couple')">＋ Casal</button><button class="mini primary" onclick="newPersonView('person')">＋ Pessoa</button></div></div><div id="people-list">${hNote('Carregando...')}</div>`,true);
    requestContext(d=>{
      const el=document.getElementById('people-list');if(!el)return;
      if(!d.ok){el.innerHTML=hNote(hEsc(d.error||'Erro ao carregar pessoas.'),'err');return}
      el.innerHTML=context.people.map(x=>`<div class="card person-card"><div class="person-avatar">${x.kind==='couple'?'👥':'👤'}</div><div class="person-main"><b>${hEsc(x.name)}</b><small>${x.kind==='couple'?'Casal / orçamento compartilhado':'Pessoa'}${x.partner_name?` · ${hEsc(x.partner_name)}`:''}</small></div><span class="status-badge ${x.active?'paid':'pending'}">${x.active?'Ativo':'Inativo'}</span></div>`).join('')||`<div class="empty-finance"><span>👥</span><b>Nenhuma pessoa cadastrada</b><p>Cadastre as pessoas que fazem parte do orçamento.</p></div>`;
    });
  };

  window.newPersonView=function(kind='person'){
    const couple=kind==='couple';
    window.shell(`${window.brand()}<h2>${couple?'Novo casal':'Nova pessoa'}</h2><div class="card edit-form"><label>${couple?'Primeira pessoa':'Nome'}</label><input id="person-name" placeholder="${couple?'Ex.: Gustavo':'Ex.: Gustavo'}"><div id="partner-wrap" ${couple?'':'hidden'}><label>Segunda pessoa</label><input id="partner-name" placeholder="Ex.: Wanessa"></div>${couple?'<label>Nome exibido do casal <small>(opcional)</small></label><input id="couple-display" placeholder="Ex.: Gustavo e Wanessa">':''}<button class="primary" id="person-save">Salvar</button><button class="link" onclick="peopleView()">Cancelar</button><div id="out"></div></div>`,true);
    document.getElementById('person-save').onclick=()=>{
      const name=document.getElementById('person-name').value.trim(),partner=document.getElementById('partner-name')?.value.trim()||'';
      if(!name||(couple&&!partner)){document.getElementById('out').innerHTML=hNote('Preencha os nomes.','err');return}
      document.getElementById('out').innerHTML=hNote('Salvando...');
      hh()?.addEntity?.(JSON.stringify({kind:couple?'couple':'person',name,partnerName:partner,displayName:document.getElementById('couple-display')?.value.trim()||''}));
    };
  };

  window.accountsView=function(){
    window.shell(`${window.brand()}<div class="heading"><div><h2>Contas</h2><div class="muted">Saldos e titulares</div></div><button class="mini primary" onclick="newAccountView()">＋ Nova</button></div><div id="accounts-list">${hNote('Carregando contas...')}</div>`,true);
    requestContext(d=>{
      const el=document.getElementById('accounts-list');if(!el)return;
      if(!d.ok){el.innerHTML=hNote(hEsc(d.error||'Erro ao carregar contas.'),'err');return}
      const labels={bank:'Banco',checking:'Conta corrente',savings:'Poupança',digital:'Conta digital',cash:'Carteira'};
      el.innerHTML=context.accounts.map(x=>`<div class="card finance-list-card account-050"><div><div class="finance-title">${hEsc(x.name)}</div><small>${hEsc(bankName(x.bank_code||'other'))} · ${hEsc(labels[x.type]||x.type||'Conta')}</small><div class="owner-line">${x.person_name?`👤 ${hEsc(x.person_name)}`:'👤 Sem titular definido'}</div></div><div class="finance-value"><b>${hMoney(x.current_balance)}</b><small>Inicial ${hMoney(x.initial_balance)}</small>${typeof window.editAccountView==='function'?`<button class="secondary account-edit-btn" onclick="editAccountView(${Number(x.id)})">✎ Editar</button>`:''}</div></div>`).join('')||`<div class="empty-finance"><span>🏦</span><b>Nenhuma conta cadastrada</b><p>Cadastre uma conta e escolha quem é o titular.</p></div>`;
    });
  };

  window.newAccountView=function(){
    requestContext(d=>{
      if(!d.ok){window.shell(`${window.brand()}${hNote(hEsc(d.error||'Não foi possível carregar os cadastros.'),'err')}`,true);return}
      const bankOpts=BANKS.map(b=>`<option value="${b[0]}">${hEsc(b[1])}</option>`).join('');
      window.shell(`${window.brand()}<h2>Nova conta</h2><div class="card edit-form"><label>Titular / orçamento</label><select id="acc-person">${peopleOptions()}</select><label>Banco / instituição</label><select id="acc-bank">${bankOpts}</select><label>Nome da conta</label><input id="acc-name" placeholder="Ex.: Conta principal"><label>Tipo</label><select id="acc-type"><option value="checking">Conta corrente</option><option value="digital">Conta digital</option><option value="savings">Poupança</option><option value="cash">Carteira / dinheiro</option><option value="bank">Outra conta bancária</option></select><div class="row"><div><label>Saldo inicial</label><input id="acc-initial" inputmode="decimal" placeholder="0,00"></div><div><label>Saldo atual</label><input id="acc-current" inputmode="decimal" placeholder="Igual ao inicial"></div></div><button class="primary" id="acc-save">Salvar conta</button><button class="link" onclick="accountsView()">Cancelar</button><div id="out"></div></div>`,true);
      document.getElementById('acc-save').onclick=()=>{
        const name=document.getElementById('acc-name').value.trim();if(!name){document.getElementById('out').innerHTML=hNote('Informe o nome da conta.','err');return}
        const initial=document.getElementById('acc-initial').value.trim()||'0',current=document.getElementById('acc-current').value.trim()||initial;
        document.getElementById('out').innerHTML=hNote('Salvando conta...');
        hh()?.addAccount?.(JSON.stringify({personId:Number(document.getElementById('acc-person').value||0),bankCode:document.getElementById('acc-bank').value,name,type:document.getElementById('acc-type').value,initialBalance:initial,currentBalance:current}));
      };
    });
  };

  window.cardsView=function(){
    window.shell(`${window.brand()}<div class="heading"><div><h2>Cartões</h2><div class="muted">Compras e faturas separadas por cartão</div></div><button class="mini primary" onclick="newCardView()">＋ Novo</button></div><div id="cards-list">${hNote('Carregando cartões...')}</div>`,true);
    requestContext(d=>{
      const el=document.getElementById('cards-list');if(!el)return;
      if(!d.ok){el.innerHTML=hNote(hEsc(d.error||'Erro ao carregar cartões.'),'err');return}
      el.innerHTML=context.cards.map(x=>`<div class="card credit-card-ui card-050"><div class="credit-top"><div><small>CARTÃO DE CRÉDITO</small><b>${hEsc(x.name)}</b><span class="card-owner">${x.person_name?`👤 ${hEsc(x.person_name)}`:'Sem titular definido'}</span></div><span>💳</span></div><div class="credit-stats"><div><small>Limite cadastrado</small><b>${hMoney(x.limit_amount)}</b></div><div><small>Fatura deste mês</small><b>${hMoney(x.invoice_amount)}</b></div></div><div class="credit-footer"><span>Fecha dia ${Number(x.closing_day)}</span><span>Vence dia ${Number(x.due_day)}</span></div><div class="card-actions-050"><button class="secondary" onclick="cardInvoiceView(${Number(x.id)},'${hMonth()}')">Ver fatura</button><button class="primary" onclick="newCardPurchaseView(${Number(x.id)})">＋ Compra</button></div></div>`).join('')||`<div class="empty-finance"><span>💳</span><b>Nenhum cartão cadastrado</b><p>Cadastre um cartão para lançar compras e acompanhar cada fatura.</p></div>`;
    });
  };

  window.newCardView=function(){
    requestContext(d=>{
      if(!d.ok){window.shell(`${window.brand()}${hNote(hEsc(d.error||'Não foi possível carregar os cadastros.'),'err')}`,true);return}
      window.shell(`${window.brand()}<h2>Novo cartão</h2><div class="card edit-form"><label>Titular / orçamento</label><select id="card-person">${peopleOptions()}</select><label>Nome do cartão</label><input id="card-name" placeholder="Ex.: Inter Black, Santander Visa"><label>Limite</label><input id="card-limit" inputmode="decimal" placeholder="0,00"><div class="row"><div><label>Dia de fechamento</label><input id="card-close" inputmode="numeric" value="1"></div><div><label>Dia de vencimento</label><input id="card-due" inputmode="numeric" value="10"></div></div><button class="primary" id="card-save">Salvar cartão</button><button class="link" onclick="cardsView()">Cancelar</button><div id="out"></div></div>`,true);
      document.getElementById('card-save').onclick=()=>{
        const name=document.getElementById('card-name').value.trim(),closing=Number(document.getElementById('card-close').value||0),due=Number(document.getElementById('card-due').value||0);
        if(!name||closing<1||closing>31||due<1||due>31){document.getElementById('out').innerHTML=hNote('Confira nome, fechamento e vencimento.','err');return}
        document.getElementById('out').innerHTML=hNote('Salvando cartão...');
        hh()?.addCard?.(JSON.stringify({personId:Number(document.getElementById('card-person').value||0),name,limitAmount:document.getElementById('card-limit').value.trim()||'0',closingDay:closing,dueDay:due}));
      };
    });
  };

  window.newTransaction=function(){
    requestContext(d=>{
      if(!d.ok){window.shell(`${window.brand()}${hNote(hEsc(d.error||'Não foi possível carregar contas e cartões.'),'err')}`,true);return}
      window.shell(`${window.brand()}<h2>Novo lançamento</h2><div class="card edit-form"><label>Tipo</label><select id="tx50-type"><option value="expense">Despesa</option><option value="income">Entrada</option></select><label>Pessoa / casal</label><select id="tx50-person">${peopleOptions()}</select><label>Descrição</label><input id="tx50-desc" placeholder="Ex.: Supermercado"><label>Categoria</label><input id="tx50-cat" placeholder="Ex.: Alimentação"><div class="row"><div><label>Valor total</label><input id="tx50-value" inputmode="decimal" placeholder="0,00"></div><div><label id="tx50-date-label">Data / vencimento</label><input id="tx50-date" type="date" value="${hToday()}"></div></div><label>Onde lançar</label><select id="tx50-dest"><option value="account">Conta / dinheiro</option>${context.cards.some(x=>x.active!==false)?'<option value="card">Cartão de crédito</option>':''}</select><div id="tx50-account-wrap"><label>Conta</label><select id="tx50-account">${accountOptions()}</select></div><div id="tx50-card-wrap" hidden><label>Cartão</label><select id="tx50-card">${cardOptions()}</select></div><div id="tx50-install-wrap"><label>Parcelas</label><input id="tx50-installments" inputmode="numeric" value="1" min="1" max="120"></div><label>Observações</label><textarea id="tx50-obs" rows="3" placeholder="Opcional"></textarea><button class="primary" id="tx50-save">Salvar lançamento</button><button class="link" onclick="transactions()">Cancelar</button><div id="out"></div></div>`,true);
      const sync=()=>{
        const type=document.getElementById('tx50-type').value,dest=document.getElementById('tx50-dest');
        if(type==='income'&&dest.value==='card')dest.value='account';
        [...dest.options].forEach(o=>{if(o.value==='card')o.disabled=type==='income'});
        const card=dest.value==='card';document.getElementById('tx50-account-wrap').hidden=card;document.getElementById('tx50-card-wrap').hidden=!card;
        document.getElementById('tx50-date-label').textContent=card?'Data da compra':'Data / vencimento';
      };
      document.getElementById('tx50-type').onchange=sync;document.getElementById('tx50-dest').onchange=sync;sync();
      document.getElementById('tx50-save').onclick=saveTransaction050;
    });
  };

  function saveTransaction050(){
    const out=document.getElementById('out'),type=document.getElementById('tx50-type').value,desc=document.getElementById('tx50-desc').value.trim(),amount=num(document.getElementById('tx50-value').value),date=document.getElementById('tx50-date').value||hToday(),dest=document.getElementById('tx50-dest').value,installments=Math.max(1,Math.min(dest==='card'?60:120,Number(document.getElementById('tx50-installments').value||1)));
    if(!desc||!Number.isFinite(amount)||amount<=0){out.innerHTML=hNote('Informe descrição e valor.','err');return}
    out.innerHTML=hNote(dest==='card'?'Incluindo compra na fatura...':'Salvando lançamento...');
    const common={personId:Number(document.getElementById('tx50-person').value||0),description:desc,category:document.getElementById('tx50-cat').value.trim()||'Outros',totalAmount:String(amount),installments,observations:document.getElementById('tx50-obs').value.trim()};
    if(dest==='card'){
      const cardId=Number(document.getElementById('tx50-card').value||0);if(!cardId){out.innerHTML=hNote('Escolha um cartão.','err');return}
      hh()?.addCardPurchase?.(JSON.stringify({...common,cardId,purchaseDate:date}));
    }else{
      hh()?.addTransaction?.(JSON.stringify({...common,type,accountId:Number(document.getElementById('tx50-account').value||0),dueDate:date}));
    }
  }

  window.newCardPurchaseView=function(cardId){
    requestContext(d=>{
      const card=context.cards.find(x=>Number(x.id)===Number(cardId));if(!d.ok||!card){window.shell(`${window.brand()}${hNote('Cartão não encontrado.','err')}`,true);return}
      window.shell(`${window.brand()}<div class="heading"><div><h2>Nova compra</h2><div class="muted">${hEsc(card.name)}</div></div></div><div class="card edit-form"><label>Pessoa / casal</label><select id="cp-person">${peopleOptions(card.person_id||0)}</select><label>Descrição</label><input id="cp-desc" placeholder="Ex.: Supermercado"><label>Categoria</label><input id="cp-cat" placeholder="Ex.: Alimentação"><div class="row"><div><label>Valor total</label><input id="cp-value" inputmode="decimal" placeholder="0,00"></div><div><label>Data da compra</label><input id="cp-date" type="date" value="${hToday()}"></div></div><label>Parcelas</label><input id="cp-installments" inputmode="numeric" value="1" min="1" max="60"><label>Observações</label><textarea id="cp-obs" rows="3" placeholder="Opcional"></textarea><button class="primary" id="cp-save">Incluir na fatura</button><button class="link" onclick="cardsView()">Cancelar</button><div id="out"></div></div>`,true);
      document.getElementById('cp-save').onclick=()=>{
        const out=document.getElementById('out'),desc=document.getElementById('cp-desc').value.trim(),amount=num(document.getElementById('cp-value').value),installments=Math.max(1,Math.min(60,Number(document.getElementById('cp-installments').value||1)));
        if(!desc||!Number.isFinite(amount)||amount<=0){out.innerHTML=hNote('Informe descrição e valor.','err');return}
        out.innerHTML=hNote('Incluindo compra na fatura...');
        hh()?.addCardPurchase?.(JSON.stringify({cardId:Number(cardId),personId:Number(document.getElementById('cp-person').value||0),description:desc,category:document.getElementById('cp-cat').value.trim()||'Outros',totalAmount:String(amount),purchaseDate:document.getElementById('cp-date').value||hToday(),installments,observations:document.getElementById('cp-obs').value.trim()}));
      };
    });
  };

  window.cardInvoiceView=function(cardId,month){
    invoiceCardId=Number(cardId);invoiceMonth=month&&/^\d{4}-\d{2}$/.test(month)?month:hMonth();
    window.shell(`${window.brand()}<div class="invoice-header"><div><h2>Fatura do cartão</h2><div class="muted" id="invoice-card-name">Carregando...</div></div><button class="mini primary" onclick="newCardPurchaseView(${invoiceCardId})">＋ Compra</button></div><div class="card invoice-month-nav"><button onclick="changeInvoiceMonth050(-1)">‹</button><b id="invoice-month-label">${hEsc(monthLabel(invoiceMonth))}</b><button onclick="changeInvoiceMonth050(1)">›</button></div><div id="invoice-content">${hNote('Carregando fatura...')}</div><button class="link" onclick="cardsView()">Voltar aos cartões</button>`,true);
    hh()?.loadCardInvoice?.(invoiceCardId,invoiceMonth);
  };

  window.changeInvoiceMonth050=function(delta){invoiceMonth=shiftMonth(invoiceMonth,Number(delta)||0);const l=document.getElementById('invoice-month-label');if(l)l.textContent=monthLabel(invoiceMonth);const el=document.getElementById('invoice-content');if(el)el.innerHTML=hNote('Carregando fatura...');hh()?.loadCardInvoice?.(invoiceCardId,invoiceMonth)};

  window.GranaOkCardInvoice=function(s){
    const d=hJson(s),el=document.getElementById('invoice-content');if(!el)return;
    if(!d.ok){el.innerHTML=hNote(hEsc(d.error||'Erro ao carregar fatura.'),'err');return}
    invoiceMonth=d.month||invoiceMonth;const card=d.card||{},name=document.getElementById('invoice-card-name');if(name)name.textContent=`${card.name||'Cartão'} · vence ${d.due_date||'-'}`;
    const label=document.getElementById('invoice-month-label');if(label)label.textContent=monthLabel(invoiceMonth);
    el.innerHTML=`<div class="invoice-kpis"><div><span>Total da fatura</span><b>${hMoney(d.total)}</b></div><div><span>Limite cadastrado</span><b>${hMoney(card.limit_amount)}</b></div><div><span>Status</span><b>${d.status==='paid'?'Paga':'Aberta'}</b></div></div><div class="invoice-purchases">${(d.rows||[]).map(x=>`<div class="card invoice-purchase"><div><b>${hEsc(x.description)}</b><small>${hEsc(x.category||'Outros')} · compra ${hEsc(x.purchase_date)}${Number(x.installment_total||1)>1?` · ${Number(x.installment_number)}/${Number(x.installment_total)}`:''}</small>${x.person_name?`<span>👤 ${hEsc(x.person_name)}</span>`:''}${x.observations?`<span>📝 ${hEsc(x.observations)}</span>`:''}</div><strong>${hMoney(x.amount)}</strong></div>`).join('')||`<div class="empty-finance"><span>💳</span><b>Sem compras nesta fatura</b><p>Use “Nova compra” para incluir uma despesa neste cartão.</p></div>`}</div>`;
  };

  window.GranaOkHouseholdSaved=function(s){
    const d=hJson(s),out=document.getElementById('out');
    if(!d.ok){if(out)out.innerHTML=hNote(hEsc(d.error||'Não foi possível salvar.'),'err');else alert(d.error||'Não foi possível salvar.');return}
    if(out)out.innerHTML=hNote(hEsc(d.message||'Salvo com sucesso.'),'ok');
    context={people:[],accounts:[],cards:[]};
    setTimeout(()=>{
      if(d.kind==='entity')peopleView();
      else if(d.kind==='account')accountsView();
      else if(d.kind==='card')cardsView();
      else if(d.kind==='card_purchase')cardInvoiceView(Number(d.card_id),d.month||hMonth());
      else transactions();
    },420);
  };

  // Mostra a conta vinculada nos cartões de lançamento sem alterar a gestão 0.3.9.
  const baseManaged=window.GranaOkManagedTransactions;
  if(typeof baseManaged==='function'){
    window.GranaOkManagedTransactions=function(s){
      const d=hJson(s);baseManaged(s);
      if(!d.ok||!(d.rows||[]).length)return;
      requestContext(()=>setTimeout(()=>{
        const nodes=[...document.querySelectorAll('.managed-tx')];
        (d.rows||[]).forEach((r,i)=>{
          const node=nodes[i];if(!node||node.querySelector('.tx-account-050'))return;
          const account=context.accounts.find(a=>Number(a.id)===Number(r.account_id||0));if(!account)return;
          const meta=document.createElement('div');meta.className='tx-account-050';meta.textContent=`🏦 ${account.name}${account.person_name?' · 👤 '+account.person_name:''}`;
          const actions=node.querySelector('.managed-tx-actions');if(actions)node.insertBefore(meta,actions);else node.appendChild(meta);
        });
      },20));
    };
  }
})();
