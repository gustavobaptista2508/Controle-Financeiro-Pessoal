(()=>{
  const planning=()=>window.GranaPlanning||null;
  const pEsc=v=>String(v??'').replace(/[&<>"']/g,m=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[m]));
  const pMoney=v=>Number(v||0).toLocaleString('pt-BR',{style:'currency',currency:'BRL'});
  const pJson=s=>{try{return JSON.parse(s)}catch{return {ok:false,error:'Resposta local inválida.'}}};
  const pNorm=s=>String(s||'').toLowerCase().normalize('NFD').replace(/[\u0300-\u036f]/g,'');
  const pNote=(t,c='')=>`<div class="note ${c}">${t}</div>`;

  const currentMonthKey=()=>{
    const d=new Date();
    return `${d.getFullYear()}-${String(d.getMonth()+1).padStart(2,'0')}`;
  };
  const shiftMonth=(month,delta)=>{
    const [y,m]=String(month).split('-').map(Number);
    const d=new Date(y,m-1+delta,1,12,0,0,0);
    return `${d.getFullYear()}-${String(d.getMonth()+1).padStart(2,'0')}`;
  };
  const monthLabel=month=>{
    const [y,m]=String(month).split('-').map(Number);
    const d=new Date(y,m-1,1,12,0,0,0);
    const text=new Intl.DateTimeFormat('pt-BR',{month:'long',year:'numeric'}).format(d);
    return text.charAt(0).toUpperCase()+text.slice(1);
  };

  let selectedMonth=currentMonthKey();
  const monthCache={};
  let pendingAi=null;

  const baseDashboard=window.dashboard;
  const baseDashboardCallback=window.GranaOkDashboard;
  const baseAskLocalAI=window.askLocalAI;

  window.GranaOkDashboard=function(s){
    if(selectedMonth!==currentMonthKey()) return;
    if(typeof baseDashboardCallback==='function') baseDashboardCallback(s);
  };

  window.dashboard=function(){
    selectedMonth=currentMonthKey();
    baseDashboard();
    setTimeout(()=>{
      installMonthNavigation();
      installAiActions();
      loadSelectedMonth();
    },60);
  };

  function installMonthNavigation(){
    const heading=document.querySelector('.heading');
    if(!heading)return;
    const titleBlock=heading.firstElementChild;
    const muted=titleBlock?.querySelector?.('.muted');
    if(muted){muted.id='planning-month-label';muted.textContent=monthLabel(selectedMonth)}
    if(!document.getElementById('planning-month-nav')){
      const nav=document.createElement('div');
      nav.id='planning-month-nav';
      nav.className='month-nav';
      nav.innerHTML=`<button type="button" aria-label="Mês anterior" onclick="changeDashboardMonth(-1)">‹</button><button type="button" class="month-current" onclick="goCurrentMonth()">Atual</button><button type="button" aria-label="Mês seguinte" onclick="changeDashboardMonth(1)">›</button>`;
      const badge=heading.querySelector('.localbadge');
      if(badge) heading.insertBefore(nav,badge); else heading.appendChild(nav);
    }
  }

  window.changeDashboardMonth=function(delta){
    selectedMonth=shiftMonth(selectedMonth,Number(delta)||0);
    const label=document.getElementById('planning-month-label');
    if(label)label.textContent=monthLabel(selectedMonth);
    loadSelectedMonth();
  };

  window.goCurrentMonth=function(){
    selectedMonth=currentMonthKey();
    const label=document.getElementById('planning-month-label');
    if(label)label.textContent=monthLabel(selectedMonth);
    loadSelectedMonth();
  };

  function loadSelectedMonth(){
    const dash=document.getElementById('dash');
    if(dash)dash.innerHTML=pNote(`Consultando ${pEsc(monthLabel(selectedMonth))}...`);
    if(!planning()?.loadMonth){
      if(dash)dash.innerHTML=pNote('Planejamento mensal indisponível nesta instalação.','err');
      return;
    }
    planning().loadMonth(selectedMonth);
  }

  function renderMonthSummary(d){
    const dash=document.getElementById('dash');
    if(!dash)return;
    const rows=d.scheduled||d.upcoming||[];
    const financing=Number(d.financing_monthly||0);
    dash.innerHTML=`<div class="grid"><div class="kpi"><span>↗ Entradas</span><b>${pMoney(d.income)}</b></div><div class="kpi"><span>↘ Despesas</span><b>${pMoney(d.expenses)}</b></div><div class="kpi"><span>▣ Faturas</span><b>${pMoney(d.card_invoices)}</b></div><div class="kpi"><span>✓ Saldo projetado</span><b>${pMoney(d.projected_balance)}</b></div></div><div class="card"><div class="planning-card-head"><h3>📅 ${pEsc(monthLabel(d.month||selectedMonth))}</h3><small>${rows.length} lançamento(s) encontrado(s)</small></div>${rows.map(x=>`<div class="item"><span>${pEsc(x.description)}<small>${pEsc(x.due_date)}${x.status?` · ${pEsc(x.status)}`:''}</small></span><b class="${x.type==='income'?'positive':'negative'}">${x.type==='income'?'+':'-'} ${pMoney(x.amount)}</b></div>`).join('')||'<div class="muted">Nenhum lançamento cadastrado para este mês.</div>'}${financing>0?`<div class="planning-footnote">Financiamentos cadastrados: <b>${pMoney(financing)}/mês</b>. Eles entram no saldo somente quando também existem como lançamento ou fatura, evitando duplicidade.</div>`:''}</div>`;
  }

  window.GranaOkMonthSummary=function(s){
    const d=pJson(s);
    if(!d.ok){
      const dash=document.getElementById('dash');
      if(dash)dash.innerHTML=pNote(pEsc(d.error||'Erro ao consultar o mês.'),'err');
      return;
    }
    monthCache[d.month]=d;
    if(d.month!==selectedMonth)return;
    try{state.dashboard=d}catch(e){}
    const label=document.getElementById('planning-month-label');
    if(label)label.textContent=monthLabel(d.month);
    renderMonthSummary(d);
  };

  function installAiActions(){
    const chips=document.querySelector('.ai-card .chips');
    if(chips && !document.getElementById('ai-next-month')){
      const projection=document.createElement('button');
      projection.id='ai-next-month';
      projection.textContent='Projetar próximo mês';
      projection.onclick=()=>window.projectNextMonthAI();
      chips.appendChild(projection);

      const simulation=document.createElement('button');
      simulation.id='ai-simulate-purchase';
      simulation.textContent='Simular compra';
      simulation.onclick=()=>window.preparePurchaseSimulation();
      chips.appendChild(simulation);
    }
    const q=document.getElementById('aiq');
    if(q)q.placeholder='Ex.: simule uma compra de R$ 2.000 em 10x';
  }

  window.projectNextMonthAI=function(){
    const out=document.getElementById('aiout');
    if(out)out.innerHTML=pNote('Calculando o próximo mês com os lançamentos já cadastrados...');
    pendingAi={type:'projection'};
    if(planning()?.projectNextMonth) planning().projectNextMonth(selectedMonth);
    else if(out)out.innerHTML=pNote('Planejamento local indisponível.','err');
  };

  window.preparePurchaseSimulation=function(){
    const q=document.getElementById('aiq');
    if(!q)return;
    q.value='Simule uma compra de R$  em 10x';
    q.focus();
    try{
      const pos=q.value.indexOf(' em 10x');
      q.setSelectionRange('Simule uma compra de R$ '.length,pos);
    }catch(e){}
  };

  function parsePtNumber(text){
    const raw=String(text||'').trim();
    if(!raw)return NaN;
    if(raw.includes(','))return Number(raw.replace(/\./g,'').replace(',','.'));
    const dots=(raw.match(/\./g)||[]).length;
    if(dots>1 || (dots===1 && /\.\d{3}$/.test(raw)))return Number(raw.replace(/\./g,''));
    return Number(raw);
  }

  function parsePurchase(q){
    let amountMatch=String(q).match(/r\$\s*([0-9][0-9.]*?(?:,[0-9]{1,2})?)(?=\s|$|em|no|na)/i);
    if(!amountMatch)amountMatch=String(q).match(/compra(?:\s+de)?\s+([0-9][0-9.]*?(?:,[0-9]{1,2})?)(?=\s|$|em|no|na)/i);
    if(!amountMatch)amountMatch=String(q).match(/([0-9][0-9.]*,[0-9]{1,2})/);
    const total=amountMatch?parsePtNumber(amountMatch[1]):NaN;
    const inst=String(q).match(/(\d{1,3})\s*x\b/i)||String(q).match(/(\d{1,3})\s*parcel/i);
    const installments=inst?Math.max(1,Math.min(120,Number(inst[1])||1)):1;
    const next=pNorm(q).includes('proximo mes')||pNorm(q).includes('mes seguinte');
    return {total,installments,next};
  }

  function renderProjection(d){
    const out=document.getElementById('aiout');
    if(!out)return;
    if(!d.ok){out.innerHTML=pNote(pEsc(d.error||'Não foi possível projetar o próximo mês.'),'err');return}
    const target=monthLabel(d.month);
    const inferred=d.income_inferred;
    const income=Number(d.scenario_income||d.income||0);
    const outflows=Number(d.registered_outflows||0);
    const balance=Number(d.scenario_balance||d.projected_balance||0);
    const noOutflows=outflows<=0;
    out.innerHTML=`<div class="ai-answer planning-ai"><b>Grana IA Local · projeção de ${pEsc(target)}</b><p>${inferred?`Ainda não há entradas registradas para ${pEsc(target)}. Para montar o cenário, usei como referência as entradas do mês analisado (${pMoney(d.base_income)}).`:`Entradas já registradas/consideradas: ${pMoney(income)}.`}</p><p>Compromissos já cadastrados para o mês: ${pMoney(outflows)}. Saldo projetado no cenário: <strong class="${balance<0?'negative':'positive'}">${pMoney(balance)}</strong>.</p>${noOutflows?'<p class="projection-warning">A projeção ainda é parcial porque não há despesas ou faturas cadastradas para o mês seguinte.</p>':''}${Number(d.financing_monthly||0)>0?`<p>Financiamentos cadastrados somam ${pMoney(d.financing_monthly)}/mês e só entram neste cálculo quando possuem lançamento correspondente, evitando dupla contagem.</p>`:''}<small>Projeção local baseada nos dados já existentes no GranaOk; não é uma garantia de saldo futuro.</small></div>`;
  }

  function renderSimulation(d,purchase,isFuture){
    const out=document.getElementById('aiout');
    if(!out)return;
    const base=Number(isFuture?(d.scenario_balance??d.projected_balance):(d.projected_balance??0));
    const monthly=purchase.installments>1?purchase.total/purchase.installments:purchase.total;
    const after=base-monthly;
    const target=monthLabel(d.month||selectedMonth);
    const parcelText=purchase.installments>1?`${purchase.installments}x de aproximadamente ${pMoney(monthly)}`:'à vista';
    out.innerHTML=`<div class="ai-answer planning-ai"><b>Grana IA Local · simulação de compra</b><p>Compra de <strong>${pMoney(purchase.total)}</strong> ${parcelText==='à vista'?'à vista':`em ${parcelText}`}.</p><p>Assumindo que ${purchase.installments>1?'a primeira parcela':'a compra'} impacte ${pEsc(target)}, o saldo projetado do mês passaria de ${pMoney(base)} para <strong class="${after<0?'negative':'positive'}">${pMoney(after)}</strong>.</p>${after<0?'<p class="projection-warning">Neste cenário o mês ficaria negativo. Vale revisar o parcelamento ou adiar a compra.</p>':''}<small>Simulação sem gravar lançamento. Não considera juros, IOF nem mudança de fechamento do cartão; se houver juros, informe o valor total já financiado.</small></div>`;
  }

  window.GranaOkNextProjection=function(s){
    const d=pJson(s);
    const request=pendingAi;
    pendingAi=null;
    if(!d.ok){
      const out=document.getElementById('aiout');
      if(out)out.innerHTML=pNote(pEsc(d.error||'Erro ao calcular projeção.'),'err');
      return;
    }
    if(request?.type==='simulation')renderSimulation(d,request.purchase,true);
    else renderProjection(d);
  };

  window.askLocalAI=function(){
    const q=document.getElementById('aiq')?.value.trim();
    if(!q)return;
    const t=pNorm(q);
    if((t.includes('projec')||t.includes('prever')||t.includes('previs')) && (t.includes('proximo')||t.includes('seguinte'))){
      window.projectNextMonthAI();
      return;
    }
    if(t.includes('simul')||t.includes('compra')){
      const purchase=parsePurchase(q);
      const out=document.getElementById('aiout');
      if(!Number.isFinite(purchase.total)||purchase.total<=0){
        if(out)out.innerHTML=pNote('Informe o valor da compra. Exemplo: “Simule uma compra de R$ 2.000 em 10x”.','err');
        return;
      }
      if(purchase.next){
        if(out)out.innerHTML=pNote('Simulando a compra no próximo mês...');
        pendingAi={type:'simulation',purchase};
        planning()?.projectNextMonth?.(selectedMonth);
        return;
      }
      let d=null;
      try{d=state.dashboard}catch(e){}
      if(!d){
        if(out)out.innerHTML=pNote('Aguarde os dados do mês carregarem.');
        return;
      }
      renderSimulation(d,purchase,false);
      return;
    }
    if(typeof baseAskLocalAI==='function')baseAskLocalAI();
  };

  // Banco Sicoob e identificação visual das contas.
  const BANKS038=[
    ['santander','Santander','S'],['inter','Banco Inter','inter'],['nubank','Nubank','nu'],
    ['itau','Itaú','itaú'],['bradesco','Bradesco','B'],['bb','Banco do Brasil','BB'],
    ['caixa','Caixa','CAIXA'],['sicredi','Sicredi','Sicredi'],['sicoob','Sicoob','Sicoob'],
    ['mercadopago','Mercado Pago','MP'],['neon','Neon','Neon'],['picpay','PicPay','P'],
    ['c6','C6 Bank','C6'],['btg','BTG Pactual','BTG'],['other','Outro / Carteira','🏦']
  ];
  const bank038=code=>(BANKS038.find(x=>x[0]===code)||BANKS038[BANKS038.length-1]);
  const bankBadge038=code=>{const b=bank038(code);return `<span class="bank-icon bank-${pEsc(b[0])}">${pEsc(b[2])}</span>`};

  window.GranaOkAccountsPlus=function(s){
    const d=pJson(s),el=document.getElementById('accounts-list');if(!el)return;
    if(!d.ok){el.innerHTML=pNote(pEsc(d.error||'Erro ao carregar contas.'),'err');return}
    const labels={bank:'Banco',checking:'Conta corrente',savings:'Poupança',digital:'Conta digital',cash:'Carteira'};
    el.innerHTML=(d.rows||[]).map(x=>`<div class="card finance-list-card account-with-bank"><div class="bank-account-left">${bankBadge038(x.bank_code||'other')}<div><div class="finance-title">${pEsc(x.name)}</div><small>${pEsc(bank038(x.bank_code||'other')[1])} · ${pEsc(labels[x.type]||x.type||'Conta')}</small></div></div><div class="finance-value"><b>${pMoney(x.current_balance)}</b><small>Inicial ${pMoney(x.initial_balance)}</small></div></div>`).join('')||`<div class="empty-finance"><span>🏦</span><b>Nenhuma conta cadastrada</b><p>Cadastre suas contas para acompanhar o saldo disponível.</p><button class="primary" onclick="newAccountView()">Cadastrar primeira conta</button></div>`;
  };

  window.newAccountView=function(){
    const opts=BANKS038.map(b=>`<option value="${b[0]}">${pEsc(b[1])}</option>`).join('');
    window.shell(`${window.brand()}<h2>Nova conta</h2><div class="card"><label>Banco / instituição</label><select id="acc-bank">${opts}</select><label>Nome da conta</label><input id="acc-name" placeholder="Ex.: Conta principal"><label>Tipo</label><select id="acc-type"><option value="checking">Conta corrente</option><option value="digital">Conta digital</option><option value="savings">Poupança</option><option value="cash">Carteira / dinheiro</option><option value="bank">Outra conta bancária</option></select><div class="row"><div><label>Saldo inicial</label><input id="acc-initial" inputmode="decimal" placeholder="0,00"></div><div><label>Saldo atual</label><input id="acc-current" inputmode="decimal" placeholder="Igual ao inicial"></div></div><button class="primary" id="acc-save">Salvar conta</button><button class="link" onclick="accountsView()">Cancelar</button><div id="out"></div></div>`,true);
    const bank=document.getElementById('acc-bank'),name=document.getElementById('acc-name');
    bank.onchange=()=>{if(!name.value.trim()){const b=bank038(bank.value);if(bank.value!=='other')name.value=b[1]}};
    document.getElementById('acc-save').onclick=()=>{
      const n=name.value.trim();
      if(!n){document.getElementById('out').innerHTML=pNote('Informe o nome da conta.','err');return}
      const initial=document.getElementById('acc-initial').value.trim()||'0';
      const current=document.getElementById('acc-current').value.trim()||initial;
      document.getElementById('out').innerHTML=pNote('Salvando conta...');
      window.GranaExtras?.addAccount?.(JSON.stringify({name:n,bankCode:bank.value,type:document.getElementById('acc-type').value,initialBalance:initial,currentBalance:current}));
    };
  };
})();
