(()=>{
  const finance=()=>window.GranaFinance||null;
  const fEsc=v=>String(v??'').replace(/[&<>"']/g,m=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[m]));
  const fMoney=v=>Number(v||0).toLocaleString('pt-BR',{style:'currency',currency:'BRL'});
  const fNote=(t,c='')=>`<div class="note ${c}">${t}</div>`;
  const fJson=s=>{try{return JSON.parse(s)}catch{return {ok:false,error:'Resposta local inválida.'}}};

  const originalShell=window.shell;
  if(typeof originalShell==='function'){
    window.shell=function(html,nav=false){
      originalShell(html,nav);
      if(nav){
        const bar=document.querySelector('.nav');
        if(bar && !document.getElementById('finance-nav')){
          const b=document.createElement('button');
          b.id='finance-nav';
          b.innerHTML='<span class="navicon">▦</span>Cadastros';
          b.addEventListener('click',()=>window.cadastros());
          bar.appendChild(b);
          bar.classList.add('nav-five');
        }
      }
    };
  }

  const originalDashboard=window.dashboard;
  if(typeof originalDashboard==='function'){
    window.dashboard=function(){
      originalDashboard();
      setTimeout(()=>{
        const ai=document.querySelector('.ai-card');
        if(ai && !document.getElementById('finance-overview-card')){
          ai.insertAdjacentHTML('beforebegin',`<div class="card finance-overview" id="finance-overview-card"><div class="finance-head"><div><h3>💼 Estrutura financeira</h3><p class="muted compact">Contas, cartões e financiamentos</p></div><button class="mini secondary" onclick="cadastros()">Gerenciar</button></div><div id="finance-overview">${fNote('Carregando cadastros...')}</div></div>`);
          try{finance()?.loadOverview?.()}catch(e){const el=document.getElementById('finance-overview');if(el)el.innerHTML=fNote('Não foi possível carregar os cadastros.','err')}
        }
      },0);
    };
  }

  window.GranaOkFinanceOverview=s=>{
    const d=fJson(s),el=document.getElementById('finance-overview');
    if(!el)return;
    if(!d.ok){el.innerHTML=fNote(fEsc(d.error||'Erro ao carregar cadastros.'),'err');return}
    el.innerHTML=`<div class="finance-mini-grid"><button onclick="accountsView()"><span>🏦 Contas</span><b>${fEsc(d.accounts_count||0)}</b><small>${fMoney(d.accounts_balance)}</small></button><button onclick="cardsView()"><span>💳 Cartões</span><b>${fEsc(d.cards_count||0)}</b><small>Limites ${fMoney(d.cards_limit)}</small></button><button onclick="financingsView()"><span>📄 Financiamentos</span><b>${fEsc(d.financings_count||0)}</b><small>${fMoney(d.financing_monthly)}/mês</small></button></div>`;
  };

  window.cadastros=function(){
    window.shell(`${window.brand()}<div class="heading"><div><h2>Cadastros</h2><div class="muted">Organize sua estrutura financeira</div></div></div><div class="finance-menu"><button class="finance-menu-card" onclick="accountsView()"><span class="finance-menu-icon">🏦</span><div><b>Contas</b><small>Conta corrente, poupança, carteira e contas digitais</small></div><span>›</span></button><button class="finance-menu-card" onclick="cardsView()"><span class="finance-menu-icon">💳</span><div><b>Cartões de crédito</b><small>Limite, fechamento, vencimento e fatura atual</small></div><span>›</span></button><button class="finance-menu-card" onclick="financingsView()"><span class="finance-menu-icon">📄</span><div><b>Financiamentos</b><small>Parcelas, valor mensal e progresso de pagamento</small></div><span>›</span></button></div><div class="card"><h3>Resumo</h3><div id="finance-overview">${fNote('Consultando MySQL...')}</div></div>`,true);
    try{finance()?.loadOverview?.()}catch(e){document.getElementById('finance-overview').innerHTML=fNote('Bridge financeiro indisponível.','err')}
  };

  window.accountsView=function(){
    window.shell(`${window.brand()}<div class="heading"><div><h2>Contas</h2><div class="muted">Saldos por conta</div></div><button class="mini primary" onclick="newAccountView()">＋ Nova</button></div><div id="accounts-list">${fNote('Carregando contas...')}</div>`,true);
    try{finance()?.loadAccounts?.()}catch(e){document.getElementById('accounts-list').innerHTML=fNote('Não foi possível abrir as contas.','err')}
  };

  window.GranaOkAccounts=s=>{
    const d=fJson(s),el=document.getElementById('accounts-list');
    if(!el)return;
    if(!d.ok){el.innerHTML=fNote(fEsc(d.error||'Erro ao carregar contas.'),'err');return}
    const labels={bank:'Banco',checking:'Conta corrente',savings:'Poupança',digital:'Conta digital',cash:'Carteira'};
    el.innerHTML=(d.rows||[]).map(x=>`<div class="card finance-list-card"><div><div class="finance-title">${fEsc(x.name)}</div><small>${fEsc(labels[x.type]||x.type||'Conta')}</small></div><div class="finance-value"><b>${fMoney(x.current_balance)}</b><small>Inicial ${fMoney(x.initial_balance)}</small></div></div>`).join('')||`<div class="empty-finance"><span>🏦</span><b>Nenhuma conta cadastrada</b><p>Cadastre suas contas para acompanhar o saldo disponível.</p><button class="primary" onclick="newAccountView()">Cadastrar primeira conta</button></div>`;
  };

  window.newAccountView=function(){
    window.shell(`${window.brand()}<h2>Nova conta</h2><div class="card"><label>Nome da conta</label><input id="acc-name" placeholder="Ex.: Santander, Inter, Carteira"><label>Tipo</label><select id="acc-type"><option value="checking">Conta corrente</option><option value="digital">Conta digital</option><option value="savings">Poupança</option><option value="cash">Carteira / dinheiro</option><option value="bank">Outra conta bancária</option></select><div class="row"><div><label>Saldo inicial</label><input id="acc-initial" inputmode="decimal" placeholder="0,00"></div><div><label>Saldo atual</label><input id="acc-current" inputmode="decimal" placeholder="Igual ao inicial"></div></div><button class="primary" id="acc-save">Salvar conta</button><button class="link" onclick="accountsView()">Cancelar</button><div id="out"></div></div>`,true);
    document.getElementById('acc-save').onclick=()=>{
      const name=document.getElementById('acc-name').value.trim();
      if(!name){document.getElementById('out').innerHTML=fNote('Informe o nome da conta.','err');return}
      const initial=document.getElementById('acc-initial').value.trim()||'0';
      const current=document.getElementById('acc-current').value.trim()||initial;
      document.getElementById('out').innerHTML=fNote('Salvando conta...');
      finance()?.addAccount?.(JSON.stringify({name,type:document.getElementById('acc-type').value,initialBalance:initial,currentBalance:current}));
    };
  };

  window.GranaOkAccountSaved=s=>{
    const d=fJson(s),el=document.getElementById('out');if(!el)return;
    if(!d.ok){el.innerHTML=fNote(fEsc(d.error||'Erro ao salvar conta.'),'err');return}
    el.innerHTML=fNote('Conta cadastrada com sucesso.','ok');setTimeout(accountsView,450);
  };

  window.cardsView=function(){
    window.shell(`${window.brand()}<div class="heading"><div><h2>Cartões</h2><div class="muted">Crédito e faturas</div></div><button class="mini primary" onclick="newCardView()">＋ Novo</button></div><div id="cards-list">${fNote('Carregando cartões...')}</div>`,true);
    try{finance()?.loadCards?.()}catch(e){document.getElementById('cards-list').innerHTML=fNote('Não foi possível abrir os cartões.','err')}
  };

  window.GranaOkCards=s=>{
    const d=fJson(s),el=document.getElementById('cards-list');if(!el)return;
    if(!d.ok){el.innerHTML=fNote(fEsc(d.error||'Erro ao carregar cartões.'),'err');return}
    el.innerHTML=(d.rows||[]).map(x=>`<div class="card credit-card-ui"><div class="credit-top"><div><small>CARTÃO DE CRÉDITO</small><b>${fEsc(x.name)}</b></div><span>💳</span></div><div class="credit-stats"><div><small>Limite</small><b>${fMoney(x.limit_amount)}</b></div><div><small>Fatura atual</small><b>${fMoney(x.invoice_amount)}</b></div></div><div class="credit-footer"><span>Fecha dia ${fEsc(x.closing_day)}</span><span>Vence dia ${fEsc(x.due_day)}</span></div></div>`).join('')||`<div class="empty-finance"><span>💳</span><b>Nenhum cartão cadastrado</b><p>Cadastre um cartão para organizar limites e faturas.</p><button class="primary" onclick="newCardView()">Cadastrar primeiro cartão</button></div>`;
  };

  window.newCardView=function(){
    window.shell(`${window.brand()}<h2>Novo cartão</h2><div class="card"><label>Nome do cartão</label><input id="card-name" placeholder="Ex.: Inter Black, Santander Visa"><label>Limite</label><input id="card-limit" inputmode="decimal" placeholder="0,00"><div class="row"><div><label>Dia de fechamento</label><input id="card-close" inputmode="numeric" value="1"></div><div><label>Dia de vencimento</label><input id="card-due" inputmode="numeric" value="10"></div></div><button class="primary" id="card-save">Salvar cartão</button><button class="link" onclick="cardsView()">Cancelar</button><div id="out"></div></div>`,true);
    document.getElementById('card-save').onclick=()=>{
      const name=document.getElementById('card-name').value.trim();
      const closing=Number(document.getElementById('card-close').value||0),due=Number(document.getElementById('card-due').value||0);
      if(!name||closing<1||closing>31||due<1||due>31){document.getElementById('out').innerHTML=fNote('Confira nome, fechamento e vencimento do cartão.','err');return}
      document.getElementById('out').innerHTML=fNote('Salvando cartão...');
      finance()?.addCard?.(JSON.stringify({name,limitAmount:document.getElementById('card-limit').value.trim()||'0',closingDay:closing,dueDay:due}));
    };
  };

  window.GranaOkCardSaved=s=>{
    const d=fJson(s),el=document.getElementById('out');if(!el)return;
    if(!d.ok){el.innerHTML=fNote(fEsc(d.error||'Erro ao salvar cartão.'),'err');return}
    el.innerHTML=fNote('Cartão cadastrado com sucesso.','ok');setTimeout(cardsView,450);
  };

  window.financingsView=function(){
    window.shell(`${window.brand()}<div class="heading"><div><h2>Financiamentos</h2><div class="muted">Parcelas e saldo contratado</div></div><button class="mini primary" onclick="newFinancingView()">＋ Novo</button></div><div id="financing-list">${fNote('Carregando financiamentos...')}</div>`,true);
    try{finance()?.loadFinancings?.()}catch(e){document.getElementById('financing-list').innerHTML=fNote('Não foi possível abrir os financiamentos.','err')}
  };

  window.GranaOkFinancings=s=>{
    const d=fJson(s),el=document.getElementById('financing-list');if(!el)return;
    if(!d.ok){el.innerHTML=fNote(fEsc(d.error||'Erro ao carregar financiamentos.'),'err');return}
    el.innerHTML=(d.rows||[]).map(x=>{const total=Number(x.total_installments||0),paid=Number(x.paid_installments||0),pct=total?Math.min(100,(paid/total)*100):0;return `<div class="card financing-card"><div class="finance-head"><div><div class="finance-title">${fEsc(x.name)}</div><small>${paid} de ${total} parcelas pagas</small></div><b>${fMoney(x.installment_amount)}/mês</b></div><div class="progress-track"><span style="width:${pct}%"></span></div><div class="financing-foot"><span>Contratado: <b>${fMoney(x.total_amount)}</b></span><span>Restam: <b>${fEsc(x.remaining_installments)}</b></span></div></div>`}).join('')||`<div class="empty-finance"><span>📄</span><b>Nenhum financiamento cadastrado</b><p>Cadastre veículo, empréstimo ou outro parcelamento.</p><button class="primary" onclick="newFinancingView()">Cadastrar financiamento</button></div>`;
  };

  window.newFinancingView=function(){
    window.shell(`${window.brand()}<h2>Novo financiamento</h2><div class="card"><label>Descrição</label><input id="fin-name" placeholder="Ex.: Financiamento do veículo"><label>Valor total contratado</label><input id="fin-total" inputmode="decimal" placeholder="0,00"><label>Valor da parcela</label><input id="fin-installment" inputmode="decimal" placeholder="0,00"><div class="row"><div><label>Total de parcelas</label><input id="fin-count" inputmode="numeric" placeholder="48"></div><div><label>Parcelas pagas</label><input id="fin-paid" inputmode="numeric" value="0"></div></div><button class="primary" id="fin-save">Salvar financiamento</button><button class="link" onclick="financingsView()">Cancelar</button><div id="out"></div></div>`,true);
    document.getElementById('fin-save').onclick=()=>{
      const name=document.getElementById('fin-name').value.trim(),count=Number(document.getElementById('fin-count').value||0),paid=Number(document.getElementById('fin-paid').value||0);
      if(!name||count<1||paid<0||paid>count){document.getElementById('out').innerHTML=fNote('Confira a descrição e a quantidade de parcelas.','err');return}
      document.getElementById('out').innerHTML=fNote('Salvando financiamento...');
      finance()?.addFinancing?.(JSON.stringify({name,totalAmount:document.getElementById('fin-total').value.trim()||'0',installmentAmount:document.getElementById('fin-installment').value.trim()||'0',totalInstallments:count,paidInstallments:paid}));
    };
  };

  window.GranaOkFinancingSaved=s=>{
    const d=fJson(s),el=document.getElementById('out');if(!el)return;
    if(!d.ok){el.innerHTML=fNote(fEsc(d.error||'Erro ao salvar financiamento.'),'err');return}
    el.innerHTML=fNote('Financiamento cadastrado com sucesso.','ok');setTimeout(financingsView,450);
  };
})();
