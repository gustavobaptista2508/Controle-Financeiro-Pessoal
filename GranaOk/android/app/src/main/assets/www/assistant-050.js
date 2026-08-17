(()=>{
  const aEsc=v=>String(v??'').replace(/[&<>"']/g,m=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[m]));
  const aMoney=v=>Number(v||0).toLocaleString('pt-BR',{style:'currency',currency:'BRL'});
  const aJson=s=>{try{return JSON.parse(s)}catch{return {ok:false,error:'Resposta local inválida.'}}};
  const norm=s=>String(s||'').toLowerCase().normalize('NFD').replace(/[\u0300-\u036f]/g,'');
  const brNum=s=>{let x=String(s??'').trim().replace(/R\$/gi,'').replace(/\s/g,'');if(!x)return NaN;if(x.includes(','))x=x.replace(/\./g,'').replace(',','.');return Number(x)};

  let aiDash=null;
  let aiRadar=null;
  let lastIntent='';
  let lastSimulation=null;
  let radarSimulation=null;
  let history=[];
  try{history=JSON.parse(sessionStorage.getItem('granaok_ai_history_050')||'[]')}catch{history=[]}
  if(!Array.isArray(history))history=[];

  const baseDashCallback=window.GranaOkDashboard;
  if(typeof baseDashCallback==='function'){
    window.GranaOkDashboard=function(s){
      const d=aJson(s);if(d.ok)aiDash=d;
      baseDashCallback(s);
    };
  }

  const baseRadarCallback=window.GranaOkInvestmentRadar;
  if(typeof baseRadarCallback==='function'){
    window.GranaOkInvestmentRadar=function(s){
      const d=aJson(s);if(d.ok)aiRadar=d;
      baseRadarCallback(s);
      if(d.ok)setTimeout(()=>renderRadarSimulator(d),30);
    };
  }

  const baseDashboard=window.dashboard;
  if(typeof baseDashboard==='function'){
    window.dashboard=function(){
      baseDashboard();
      setTimeout(enhanceAssistantCard,70);
      if(!aiRadar){try{window.GranaInvest?.refresh?.()}catch(e){}}
    };
  }

  function saveHistory(){
    history=history.slice(-18);
    try{sessionStorage.setItem('granaok_ai_history_050',JSON.stringify(history))}catch(e){}
  }

  function enhanceAssistantCard(){
    const card=document.querySelector('.ai-card');if(!card||card.dataset.v050==='1')return;card.dataset.v050='1';
    card.innerHTML=`<div class="titleicon"><span>✨</span><div><h3>Grana IA</h3><p class="muted compact">Assistente financeiro local · mantém o contexto da conversa</p></div></div><div class="ai-chat-050" id="ai-chat-050"></div><div class="chips ai-chips-050"><button onclick="quickAI('Como estão minhas finanças este mês?')">Resumo do mês</button><button onclick="quickAI('Quanto posso separar para investir?')">Quanto investir?</button><button onclick="quickAI('Simule R$ 10.000 por 2 anos')">Simular investimento</button><button onclick="quickAI('Onde estou gastando mais?')">Maiores gastos</button></div><div class="ai-composer-050"><input id="aiq" placeholder="Converse sobre gastos, saldo, faturas ou investimentos"><button class="primary" id="ask">Enviar</button></div><small class="ai-local-note">As respostas continuam sendo processadas no aparelho. O assistente usa regras financeiras, contexto da conversa e os dados do GranaOk — não é um LLM em nuvem.</small>`;
    if(!history.length)history.push({role:'assistant',text:'Olá! Posso analisar seu mês, explicar seus gastos, conversar sobre faturas e fazer simulações de investimento. Pode perguntar do seu jeito.'});
    renderChat();
    document.getElementById('ask').onclick=window.askLocalAI;
    document.getElementById('aiq').onkeydown=e=>{if(e.key==='Enter')window.askLocalAI()};
    const pending=sessionStorage.getItem('granaok_ai_pending_050');
    if(pending){sessionStorage.removeItem('granaok_ai_pending_050');setTimeout(()=>{const q=document.getElementById('aiq');if(q){q.value=pending;window.askLocalAI()}},100)}
  }

  function renderChat(){
    const el=document.getElementById('ai-chat-050');if(!el)return;
    el.innerHTML=history.slice(-12).map(m=>`<div class="ai-bubble-050 ${m.role==='user'?'user':'assistant'}"><b>${m.role==='user'?'Você':'Grana IA'}</b><p>${aEsc(m.text).replace(/\n/g,'<br>')}</p></div>`).join('');
    el.scrollTop=el.scrollHeight;
  }

  function addMessage(role,text){history.push({role,text:String(text||'')});saveHistory();renderChat()}

  window.quickAI=function(q){const el=document.getElementById('aiq');if(el)el.value=q;window.askLocalAI()};

  window.askLocalAI=function(){
    const input=document.getElementById('aiq');if(!input)return;
    const q=input.value.trim();if(!q)return;input.value='';addMessage('user',q);
    const answer=answerQuestion(q);setTimeout(()=>addMessage('assistant',answer),120);
  };

  function answerQuestion(q){
    const t=norm(q),d=aiDash||{};
    if(/^(oi|ola|bom dia|boa tarde|boa noite|e ai|eae)\b/.test(t)){lastIntent='chat';return 'Olá! Estou acompanhando seu orçamento por aqui. Quer olhar o mês atual, alguma fatura ou fazer uma simulação de investimento?'}

    const investmentIntent=/(invest|aplic|rend|simul|selic|tesouro|cdb|lci|lca|guardar dinheiro)/.test(t)||(lastIntent==='investment'&&/(ano|mes|%|e se|quanto)/.test(t));
    if(investmentIntent){lastIntent='investment';return answerInvestment(q,t)}

    if(/quanto.*(invest|guardar|separar)|sobra|disponivel.*invest/.test(t)){
      lastIntent='capacity';
      if(!d.ok&&d.income===undefined)return 'Ainda estou aguardando os dados do mês. Abra o painel e tente de novo em alguns segundos.';
      const surplus=Number(d.income||0)-Number(d.expenses||0)-Number(d.card_invoices||0);
      if(surplus<=0)return `Pelos lançamentos atuais, não aparece sobra mensal: entram ${aMoney(d.income)}, enquanto despesas diretas e faturas somam ${aMoney(Number(d.expenses||0)+Number(d.card_invoices||0))}. Antes de definir um aporte, eu priorizaria equilibrar esse fluxo.`;
      const cautious=surplus*.5;
      return `Pelos dados deste mês, a sobra projetada antes de outros compromissos é de ${aMoney(surplus)}. Para não apertar seu caixa, uma referência conservadora seria começar com algo perto de ${aMoney(cautious)} e só aumentar depois de confirmar que os gastos pendentes do mês estão cobertos.`;
    }

    if(/posso (comprar|gastar|pagar)|da para (comprar|gastar)|cabe no orcamento/.test(t)){
      lastIntent='affordability';const amount=firstMoney(q);
      if(!Number.isFinite(amount)||amount<=0)return 'Consigo comparar com seu fluxo. Me diga o valor da compra, por exemplo: “Posso gastar R$ 1.500 este mês?”';
      const projected=Number(d.projected_balance||0),after=projected-amount;
      if(after>=0)return `Pelo saldo projetado atual, ${aMoney(amount)} cabe matematicamente e deixaria aproximadamente ${aMoney(after)} de margem. Eu ainda conferiria os lançamentos pendentes e a data da compra antes de considerar esse valor realmente livre.`;
      return `Com os dados atuais, essa compra de ${aMoney(amount)} ultrapassaria seu saldo projetado em cerca de ${aMoney(Math.abs(after))}. Se for necessária, vale simular parcelamento ou adiar para um mês com mais folga.`;
    }

    if(/saldo|como estao|minhas financas|resumo|situacao/.test(t)){
      lastIntent='summary';
      const net=Number(d.income||0)-Number(d.expenses||0)-Number(d.card_invoices||0);
      const tone=net>=0?'positivo':'apertado';
      return `Neste mês, você tem ${aMoney(d.income)} de entradas, ${aMoney(d.expenses)} em despesas diretas e ${aMoney(d.card_invoices)} em faturas. O resultado projetado está ${tone}, em ${aMoney(d.projected_balance)}.${net>0?' Isso dá alguma margem, mas eu trataria como disponível apenas o que não estiver comprometido com vencimentos futuros.':''}`;
    }

    if(/onde.*gast|maior.*gast|categoria|despesa/.test(t)){
      lastIntent='spending';const cats=d.categories||[];
      if(!cats.length)return 'Ainda não tenho despesas categorizadas suficientes neste mês para apontar onde está concentrado o gasto.';
      const top=cats.slice(0,3);return `O maior peso está em ${top[0].name}, com ${aMoney(top[0].total)}.${top[1]?` Depois vêm ${top[1].name} (${aMoney(top[1].total)})`:''}${top[2]?` e ${top[2].name} (${aMoney(top[2].total)})`:''}. Se quiser, posso usar isso para sugerir onde existe mais espaço para cortar sem mexer nas contas essenciais.`;
    }

    if(/compar|mes passado|aument|dimin/.test(t)){
      lastIntent='comparison';const now=Number(d.expenses||0),prev=Number(d.prev_expenses||0),diff=now-prev,pct=prev>0?Math.abs(diff)/prev*100:null;
      return `As despesas diretas estão em ${aMoney(now)} agora, contra ${aMoney(prev)} no mês anterior. ${diff>=0?'Isso representa aumento':'Isso representa redução'} de ${aMoney(Math.abs(diff))}${pct!==null?`, cerca de ${pct.toFixed(1).replace('.',',')}%`:''}.`;
    }

    if(/fatura|cartao/.test(t)){
      lastIntent='card';return `As faturas com vencimento neste mês somam ${aMoney(d.card_invoices||0)}. Na área Cartões você consegue abrir cada cartão, navegar mês a mês e ver as compras que formam a fatura.`;
    }

    if(/venc|proxim|atras/.test(t)){
      lastIntent='due';const u=d.upcoming||[];
      if(!u.length)return 'Não aparecem vencimentos registrados para os próximos 14 dias no resumo atual.';
      const first=u[0];return `Há ${u.length} lançamento(s) nos próximos 14 dias. O mais próximo é “${first.description}”, em ${first.due_date}, no valor de ${aMoney(first.amount)}. Se quiser, posso ajudar a comparar esses vencimentos com o saldo projetado.`;
    }

    lastIntent='chat';
    return 'Entendi. Eu consigo conversar melhor quando a pergunta envolve seu fluxo do mês, despesas, faturas, capacidade de compra ou investimento. Por exemplo: “quanto sobra para investir?”, “posso gastar R$ 800?” ou “simule R$ 5.000 por 18 meses”.';
  }

  function answerInvestment(q,t){
    let amount=firstMoney(q),monthly=monthlyMoney(q),months=durationMonths(t),rate=percentRate(q);
    if((!Number.isFinite(amount)||amount<=0)&&lastSimulation)amount=lastSimulation.initial;
    if((!Number.isFinite(monthly)||monthly<0)&&lastSimulation)monthly=lastSimulation.monthly;
    if(!months&&lastSimulation)months=lastSimulation.months;
    if(!Number.isFinite(rate)||rate<=0)rate=radarRate();
    if(!Number.isFinite(monthly)||monthly<0)monthly=0;
    if(!Number.isFinite(amount)||amount<=0)return 'Claro. Qual valor você quer investir? Você pode dizer algo como “simule R$ 10.000 por 2 anos” ou “R$ 5.000 mais R$ 300 por mês por 36 meses”.';
    if(!months)months=12;
    if(!Number.isFinite(rate)||rate<=0)return `Tenho o valor (${aMoney(amount)}) e o prazo (${months} meses), mas a taxa de referência ainda não foi carregada. Abra o Radar de Investimentos ou informe uma taxa anual, por exemplo “a 12% ao ano”.`;
    const sim=simulate(amount,monthly,months,rate,0);lastSimulation={initial:amount,monthly,months,rate};
    const source=radarRateLabel();
    return `Usando ${rate.toFixed(2).replace('.',',')}% ao ano${source?` como ${source}`:''}, ${aMoney(amount)}${monthly>0?` mais ${aMoney(monthly)} por mês`:''} durante ${months} meses chegaria a aproximadamente ${aMoney(sim.gross)} em uma simulação bruta. Você teria colocado ${aMoney(sim.contributed)} do próprio bolso e o ganho estimado seria ${aMoney(sim.gain)}.\n\nIsso é uma projeção matemática, não uma garantia: imposto, taxas, carência e a oscilação da taxa podem mudar o resultado.`;
  }

  function firstMoney(q){
    const explicit=String(q).match(/R\$\s*([\d.]+(?:,\d{1,2})?)/i);if(explicit)return brNum(explicit[1]);
    const nums=[...String(q).matchAll(/\b(\d{1,3}(?:\.\d{3})+(?:,\d{1,2})?|\d{3,}(?:[,.]\d{1,2})?)\b/g)].map(m=>brNum(m[1])).filter(Number.isFinite);return nums.length?nums[0]:NaN;
  }

  function monthlyMoney(q){
    const s=String(q);const m=s.match(/(?:mais|e|aporte(?:s)? de)?\s*R?\$?\s*([\d.]+(?:,\d{1,2})?)\s*(?:por|ao)?\s*m[eê]s/i);return m?brNum(m[1]):NaN;
  }

  function durationMonths(t){
    let m=t.match(/(\d+)\s*(ano|anos)\b/);if(m)return Number(m[1])*12;
    m=t.match(/(\d+)\s*(mes|meses)\b/);return m?Number(m[1]):0;
  }

  function percentRate(q){const m=String(q).match(/([\d]+(?:[,.]\d+)?)\s*%/);return m?brNum(m[1]):NaN}

  function radarRate(){
    const b=aiRadar?.benchmarks||{};const effective=Number(b.selic_effective?.value||0),target=Number(b.selic_target?.value||0);return effective>0?effective:target;
  }
  function radarRateLabel(){return aiRadar?'referência atual do Radar':''}

  function simulate(initial,monthly,months,annual,taxPct){
    initial=Number(initial||0);monthly=Number(monthly||0);months=Math.max(1,Number(months||1));annual=Number(annual||0);taxPct=Math.max(0,Math.min(100,Number(taxPct||0)));
    const r=Math.pow(1+annual/100,1/12)-1;let balance=initial;
    for(let i=0;i<months;i++)balance=balance*(1+r)+monthly;
    const contributed=initial+monthly*months,gain=Math.max(0,balance-contributed),tax=gain*taxPct/100;
    return {gross:balance,contributed,gain,tax,net:balance-tax,monthlyRate:r};
  }
  window.GranaInvestmentSimulator050=simulate;

  function renderRadarSimulator(d){
    const root=document.getElementById('radar-content');if(!root||document.getElementById('invest-simulator-050'))return;
    const b=d.benchmarks||{},selic=Number(b.selic_effective?.value||b.selic_target?.value||0);
    const rates=[];if(selic>0)rates.push({name:'Selic de referência',rate:selic});
    (d.treasury||[]).forEach(t=>{const r=Number(t.annual_invest_rate||0);if(r>0&&!rates.some(x=>x.name===t.name))rates.push({name:t.name,rate:r})});
    const opts=rates.slice(0,15).map((x,i)=>`<option value="${x.rate}" ${i===0?'selected':''}>${aEsc(x.name)} · ${x.rate.toFixed(2).replace('.',',')}% a.a.</option>`).join('');
    root.insertAdjacentHTML('beforeend',`<div class="card invest-simulator-050" id="invest-simulator-050"><div class="sim-head"><div><h3>🧮 Simulador de investimento</h3><p class="muted compact">Use uma taxa do Radar ou informe sua própria taxa anual.</p></div></div>${opts?`<label>Referência</label><select id="sim-source-050">${opts}<option value="manual">Taxa manual</option></select>`:''}<div class="row"><div><label>Valor inicial</label><input id="sim-initial-050" inputmode="decimal" value="10000,00"></div><div><label>Aporte mensal</label><input id="sim-monthly-050" inputmode="decimal" value="0,00"></div></div><div class="row"><div><label>Prazo em meses</label><input id="sim-months-050" inputmode="numeric" value="24"></div><div><label>Taxa anual (%)</label><input id="sim-rate-050" inputmode="decimal" value="${selic>0?selic.toFixed(2).replace('.',','):'10,00'}"></div></div><label>IR estimado sobre o rendimento (%) <small>opcional</small></label><input id="sim-tax-050" inputmode="decimal" value="0"><button class="primary" id="sim-run-050">Simular</button><div id="sim-result-050"></div><p class="hint">A simulação usa juros compostos com equivalência mensal. Rentabilidade real, tributação, taxas e liquidez dependem do produto escolhido.</p></div>`);
    const source=document.getElementById('sim-source-050');if(source)source.onchange=()=>{if(source.value!=='manual')document.getElementById('sim-rate-050').value=Number(source.value).toFixed(2).replace('.',',')};
    document.getElementById('sim-run-050').onclick=runRadarSimulation;runRadarSimulation();
  }

  function runRadarSimulation(){
    const initial=brNum(document.getElementById('sim-initial-050')?.value),monthly=brNum(document.getElementById('sim-monthly-050')?.value),months=Number(document.getElementById('sim-months-050')?.value||0),rate=brNum(document.getElementById('sim-rate-050')?.value),tax=brNum(document.getElementById('sim-tax-050')?.value||0),out=document.getElementById('sim-result-050');
    if(!out)return;if(!Number.isFinite(initial)||initial<0||!Number.isFinite(monthly)||monthly<0||months<1||!Number.isFinite(rate)||rate<0){out.innerHTML='<div class="note err">Confira os valores da simulação.</div>';return}
    const sim=simulate(initial,monthly,months,rate,Number.isFinite(tax)?tax:0);radarSimulation={initial,monthly,months,rate,tax,sim};lastSimulation={initial,monthly,months,rate};
    out.innerHTML=`<div class="sim-result-grid"><div><span>Total investido</span><b>${aMoney(sim.contributed)}</b></div><div><span>Valor bruto estimado</span><b>${aMoney(sim.gross)}</b></div><div><span>Rendimento estimado</span><b>${aMoney(sim.gain)}</b></div><div><span>Valor líquido estimado</span><b>${aMoney(sim.net)}</b></div></div><button class="secondary sim-ai-btn" onclick="openSimulationInAI050()">✨ Conversar sobre esta simulação</button>`;
  }

  window.openSimulationInAI050=function(){
    if(!radarSimulation)return;const s=radarSimulation;
    const q=`Analise esta simulação: ${aMoney(s.initial)} de valor inicial${s.monthly>0?`, ${aMoney(s.monthly)} por mês`:''}, por ${s.months} meses, usando ${s.rate.toFixed(2).replace('.',',')}% ao ano.`;
    sessionStorage.setItem('granaok_ai_pending_050',q);window.dashboard();
  };
})();
