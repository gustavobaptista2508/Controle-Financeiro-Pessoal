(()=>{
  const fin=()=>window.GranaFinancing||null;
  const e=v=>String(v??'').replace(/[&<>"']/g,m=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[m]));
  const m=v=>Number(v||0).toLocaleString('pt-BR',{style:'currency',currency:'BRL'});
  const j=s=>{try{return JSON.parse(s)}catch{return {ok:false,error:'Resposta local inválida.'}}};
  const n=(t,c='')=>`<div class="note ${c}">${t}</div>`;
  const today=()=>new Date().toISOString().slice(0,10);
  const brDate=s=>{if(!s)return '—';const p=String(s).slice(0,10).split('-');return p.length===3?`${p[2]}/${p[1]}/${p[0]}`:s};
  const statusLabel=s=>({pending:'Pendente',overdue:'Atrasada',needs_due_date:'Definir vencimento',completed:'Concluído'}[s]||s||'Pendente');
  let rows=[];
  let currentId=0;
  let flash='';

  window.financingsView=function(){
    window.shell(`${window.brand()}<div class="heading"><div><h2>Financiamentos</h2><div class="muted">Controle parcela por parcela</div></div><button class="mini primary" onclick="newFinancingView()">＋ Novo</button></div>${flash?`<div id="fin-flash">${n(e(flash),'ok')}</div>`:''}<div id="financing-list">${n('Carregando financiamentos...')}</div>`,true);
    flash='';
    try{fin()?.loadFinancings?.()}catch(err){document.getElementById('financing-list').innerHTML=n('Controle de financiamentos indisponível.','err')}
  };

  window.GranaOkFinancings052=s=>{
    const d=j(s),el=document.getElementById('financing-list');if(!el)return;
    if(!d.ok){el.innerHTML=n(e(d.error||'Erro ao carregar financiamentos.'),'err');return}
    rows=d.rows||[];
    el.innerHTML=rows.map(card).join('')||`<div class="empty-finance"><span>📄</span><b>Nenhum financiamento cadastrado</b><p>Cadastre um financiamento para acompanhar vencimentos e pagamentos.</p><button class="primary" onclick="newFinancingView()">Cadastrar financiamento</button></div>`;
  };

  function card(x){
    const total=Number(x.total_installments||0),paid=Number(x.paid_installments||0),pct=total?Math.min(100,paid/total*100):0;
    const status=x.current_status||'pending';
    const completed=status==='completed';
    const noDue=status==='needs_due_date';
    const overdue=status==='overdue';
    const next=Number(x.next_installment_number||Math.min(total,paid+1));
    const statusClass=completed?'fin-status-completed':overdue?'fin-status-overdue':noDue?'fin-status-config':'fin-status-pending';
    let action='';
    if(completed) action=`<button class="secondary" onclick="financingDetailView(${Number(x.id)})">Ver histórico</button>`;
    else if(noDue) action=`<button class="primary" onclick="financingSetDueView(${Number(x.id)})">📅 Definir próxima parcela</button><button class="secondary" onclick="financingDetailView(${Number(x.id)})">Histórico</button>`;
    else action=`<button class="primary" onclick="financingPayView(${Number(x.id)})">✓ Marcar parcela ${next}/${total} como paga</button><button class="secondary" onclick="financingDetailView(${Number(x.id)})">Histórico</button>`;
    return `<div class="card financing-card fin-052-card"><div class="finance-head"><div><div class="finance-title">${e(x.name)}</div><small>${paid} de ${total} parcelas pagas</small></div><span class="fin-status ${statusClass}">${e(statusLabel(status))}</span></div><div class="progress-track"><span style="width:${pct}%"></span></div>${completed?`<div class="fin-current completed"><small>FINANCIAMENTO CONCLUÍDO</small><b>${x.completed_at?`Concluído em ${brDate(x.completed_at)}`:'Todas as parcelas foram pagas'}</b></div>`:`<div class="fin-current ${overdue?'overdue':''}"><div><small>PARCELA ATUAL</small><b>${next}/${total}</b></div><div><small>VALOR</small><b>${m(x.next_amount||x.installment_amount)}</b></div><div><small>VENCIMENTO</small><b>${x.next_due_date?brDate(x.next_due_date):'Não definido'}</b></div></div>`}<div class="financing-foot"><span>Contratado: <b>${m(x.total_amount)}</b></span><span>Restam: <b>${e(x.remaining_installments)}</b></span></div>${x.last_paid_date?`<div class="fin-last-paid">Último pagamento registrado: <b>${brDate(x.last_paid_date)}</b></div>`:''}<div class="fin-actions">${action}</div></div>`;
  }

  window.newFinancingView=function(){
    window.shell(`${window.brand()}<h2>Novo financiamento</h2><div class="card"><label>Descrição</label><input id="fin52-name" placeholder="Ex.: Financiamento do veículo"><label>Valor total contratado</label><input id="fin52-total" inputmode="decimal" placeholder="0,00"><label>Valor da parcela</label><input id="fin52-installment" inputmode="decimal" placeholder="0,00"><div class="row"><div><label>Total de parcelas</label><input id="fin52-count" inputmode="numeric" placeholder="48"></div><div><label>Já pagas</label><input id="fin52-paid" inputmode="numeric" value="0"></div></div><label>Vencimento da próxima parcela</label><input id="fin52-due" type="date"><p class="hint">Se algumas parcelas já foram pagas, informe o vencimento da próxima que ainda está pendente. A partir dela o GranaOk monta o calendário mensal restante.</p><button class="primary" id="fin52-save">Salvar financiamento</button><button class="link" onclick="financingsView()">Cancelar</button><div id="out"></div></div>`,true);
    document.getElementById('fin52-save').onclick=saveNew;
  };

  function saveNew(){
    const name=document.getElementById('fin52-name').value.trim();
    const count=Number(document.getElementById('fin52-count').value||0),paid=Number(document.getElementById('fin52-paid').value||0);
    const installment=document.getElementById('fin52-installment').value.trim(),due=document.getElementById('fin52-due').value;
    if(!name||count<1||paid<0||paid>count||!installment){document.getElementById('out').innerHTML=n('Confira descrição, valor e quantidade de parcelas.','err');return}
    if(paid<count&&!due){document.getElementById('out').innerHTML=n('Informe o vencimento da próxima parcela.','err');return}
    document.getElementById('out').innerHTML=n('Criando calendário das parcelas...');
    fin()?.addFinancing?.(JSON.stringify({name,totalAmount:document.getElementById('fin52-total').value.trim()||'0',installmentAmount:installment,totalInstallments:count,paidInstallments:paid,nextDueDate:due}));
  }

  window.GranaOkFinancingSaved052=s=>{
    const d=j(s),out=document.getElementById('out');
    if(!d.ok){if(out)out.innerHTML=n(e(d.error||'Erro ao salvar financiamento.'),'err');return}
    flash=d.message||'Financiamento cadastrado.';setTimeout(financingsView,350);
  };

  window.financingSetDueView=function(id){
    const x=rows.find(r=>Number(r.id)===Number(id));if(!x)return financingsView();currentId=Number(id);
    const next=Number(x.paid_installments||0)+1;
    window.shell(`${window.brand()}<h2>Próxima parcela</h2><div class="card"><div class="fin-pay-summary"><span>${e(x.name)}</span><b>Parcela ${next}/${e(x.total_installments)} · ${m(x.installment_amount)}</b></div><label>Vencimento da próxima parcela</label><input id="fin52-nextdue" type="date" value="${e(x.next_due_date||'')}"><p class="hint">As demais parcelas serão projetadas mensalmente a partir desta data, mantendo a quantidade já paga.</p><button class="primary" id="fin52-due-save">Salvar vencimento</button><button class="link" onclick="financingsView()">Cancelar</button><div id="out"></div></div>`,true);
    document.getElementById('fin52-due-save').onclick=()=>{const due=document.getElementById('fin52-nextdue').value;if(!due){document.getElementById('out').innerHTML=n('Informe o vencimento.','err');return}document.getElementById('out').innerHTML=n('Montando próximas parcelas...');fin()?.setNextDue?.(JSON.stringify({financingId:currentId,nextDueDate:due}))};
  };

  window.GranaOkFinancingScheduleSaved052=s=>{
    const d=j(s),out=document.getElementById('out');if(!d.ok){if(out)out.innerHTML=n(e(d.error||'Erro ao configurar vencimento.'),'err');return}
    flash=d.message||'Próxima parcela configurada.';setTimeout(financingsView,350);
  };

  window.financingPayView=function(id){
    const x=rows.find(r=>Number(r.id)===Number(id));if(!x)return financingsView();currentId=Number(id);
    const installment=Number(x.next_installment_number||0);
    window.shell(`${window.brand()}<h2>Confirmar pagamento</h2><div class="card"><div class="fin-pay-summary"><span>${e(x.name)}</span><b>Parcela ${installment}/${e(x.total_installments)}</b><strong>${m(x.next_amount||x.installment_amount)}</strong><small>Vencimento ${brDate(x.next_due_date)}</small></div>${x.current_status==='overdue'?n('Esta parcela está vencida. A data abaixo deve ser a data em que ela realmente foi paga.','err'):''}<label>Data do pagamento</label><input id="fin52-paiddate" type="date" value="${today()}"><p class="hint">Ao confirmar, esta parcela será arquivada como paga e o GranaOk avançará automaticamente para a próxima.</p><button class="primary" id="fin52-pay">✓ Confirmar pagamento</button><button class="link" onclick="financingsView()">Cancelar</button><div id="out"></div></div>`,true);
    document.getElementById('fin52-pay').onclick=()=>{const paidDate=document.getElementById('fin52-paiddate').value;if(!paidDate){document.getElementById('out').innerHTML=n('Informe a data do pagamento.','err');return}document.getElementById('out').innerHTML=n('Registrando pagamento e avançando parcela...');document.getElementById('fin52-pay').disabled=true;fin()?.payCurrent?.(JSON.stringify({financingId:currentId,paidDate}))};
  };

  window.GranaOkFinancingPaid052=s=>{
    const d=j(s),out=document.getElementById('out');if(!d.ok){if(out){out.innerHTML=n(e(d.error||'Erro ao registrar pagamento.'),'err');const b=document.getElementById('fin52-pay');if(b)b.disabled=false}return}
    flash=d.message+(d.completed?'':d.next_due_date?` Próximo vencimento: ${brDate(d.next_due_date)}.`:'');setTimeout(financingsView,450);
  };

  window.financingDetailView=function(id){
    const x=rows.find(r=>Number(r.id)===Number(id));if(!x)return financingsView();currentId=Number(id);
    window.shell(`${window.brand()}<div class="heading"><div><h2>${e(x.name)}</h2><div class="muted">Histórico das parcelas</div></div></div><div class="card"><div class="fin-detail-grid"><div><small>Total</small><b>${e(x.total_installments)}</b></div><div><small>Pagas</small><b>${e(x.paid_installments)}</b></div><div><small>Restantes</small><b>${e(x.remaining_installments)}</b></div><div><small>Parcela</small><b>${m(x.installment_amount)}</b></div></div></div><div id="fin52-history">${n('Carregando histórico...')}</div><button class="link" onclick="financingsView()">Voltar</button>`,true);
    fin()?.loadInstallments?.(Number(id));
  };

  window.GranaOkFinancingInstallments052=s=>{
    const d=j(s),el=document.getElementById('fin52-history');if(!el)return;
    if(!d.ok){el.innerHTML=n(e(d.error||'Erro ao carregar parcelas.'),'err');return}
    const legacy=Number(d.legacy_paid_count||0),list=d.rows||[];
    const legacyNote=legacy>0?n(`${legacy} parcela(s) já estavam marcadas como pagas antes do controle detalhado. A data individual desses pagamentos não estava registrada.`):'';
    el.innerHTML=legacyNote+`<div class="card fin-history">${list.map(r=>{const paid=r.status==='paid',over=!paid&&r.due_date&&r.due_date<today();return `<div class="fin-history-row"><div><b>Parcela ${e(r.installment_number)}</b><small>Vence ${brDate(r.due_date)}</small></div><div class="fin-history-right"><b>${m(r.amount)}</b><span class="${paid?'paid':over?'late':'pending'}">${paid?`Paga em ${brDate(r.paid_date)}`:over?'Atrasada':'Pendente'}</span></div></div>`}).join('')||'<div class="muted">Ainda não há parcelas detalhadas. Defina o próximo vencimento para iniciar o calendário.</div>'}</div>`;
  };
})();
