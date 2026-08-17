(()=>{
  const imp=()=>window.GranaImport||null;
  const invest=()=>window.GranaInvest||null;
  const tEsc=v=>String(v??'').replace(/[&<>"']/g,m=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[m]));
  const tMoney=v=>Number(v||0).toLocaleString('pt-BR',{style:'currency',currency:'BRL'});
  const tJson=s=>{try{return JSON.parse(s)}catch{return {ok:false,error:'Resposta local inválida.'}}};
  const tNote=(t,c='')=>`<div class="note ${c}">${t}</div>`;
  const ptNum=s=>{const x=String(s??'').trim().replace(/R\$/gi,'').replace(/\s/g,'');if(x.includes(','))return Number(x.replace(/\./g,'').replace(',','.'));return Number(x)};
  let receiptData=null;
  let statementRows=[];
  let statementFile='extrato';
  let statementTotal=0;
  let importAccounts=[];

  // Mantém a tela de gestão 0.3.9 e apenas acrescenta atalhos de importação.
  const baseTransactions=window.transactions;
  if(typeof baseTransactions==='function'){
    window.transactions=function(){
      baseTransactions();
      setTimeout(()=>{
        const heading=document.querySelector('.tx-heading')||document.querySelector('.heading');
        if(heading && !document.getElementById('import-actions')){
          const actions=document.createElement('div');actions.id='import-actions';actions.className='import-actions';
          actions.innerHTML='<button class="secondary" onclick="receiptScanView()">📷 Ler comprovante</button><button class="secondary" onclick="statementImportView()">🏦 Importar extrato</button>';
          heading.insertAdjacentElement('afterend',actions);
        }
      },20);
    };
  }

  const baseDashboard=window.dashboard;
  if(typeof baseDashboard==='function'){
    window.dashboard=function(){
      baseDashboard();
      setTimeout(()=>{
        const ai=document.querySelector('.ai-card');
        if(ai&&!document.getElementById('radar-teaser')){
          ai.insertAdjacentHTML('beforebegin',`<div class="card radar-teaser" id="radar-teaser"><div><h3>📈 Radar de Investimentos</h3><p class="muted compact">Selic e IPCA do Banco Central + títulos do Tesouro quando a fonte estiver disponível.</p></div><button class="secondary" onclick="investmentRadarView()">Abrir radar</button></div>`);
        }
      },150);
    };
  }

  window.receiptScanView=function(){
    receiptData=null;
    window.shell(`${window.brand()}<div class="heading"><div><h2>Ler comprovante</h2><div class="muted">Cupom fiscal ou via da máquina de cartão</div></div></div><div class="card"><div class="scan-choice"><button class="primary" onclick="GranaImport.takeReceiptPhoto()"><span class="scan-icon">📷</span><b>Fotografar</b><small>Use a câmera do celular</small></button><button class="secondary" onclick="GranaImport.chooseReceiptImage()"><span class="scan-icon">🖼️</span><b>Escolher imagem</b><small>Foto já salva</small></button></div><p class="hint">A leitura é feita no aparelho. Depois você confere os dados antes de criar o lançamento.</p><div id="import-progress"></div></div><button class="link" onclick="transactions()">Voltar aos lançamentos</button>`,true);
  };

  window.GranaOkImportProgress=function(s){
    const d=tJson(s),el=document.getElementById('import-progress')||document.getElementById('statement-progress');
    if(el)el.innerHTML=tNote(tEsc(d.message||'Processando...'));
  };
  window.GranaOkImportCancelled=function(){const el=document.getElementById('import-progress');if(el)el.innerHTML=tNote('Operação cancelada.')};

  window.GranaOkReceiptScan=function(s){
    const d=tJson(s);receiptData=d;
    if(!d.ok){const el=document.getElementById('import-progress');if(el)el.innerHTML=tNote(tEsc(d.error||'Não foi possível ler o comprovante.'),'err');return}
    const amount=Number(d.total||0),obs=[d.payment&&d.payment!=='Não identificado'?`Pagamento: ${d.payment}`:'',d.card_last4?`Cartão final ${d.card_last4}`:'',d.nsu?`NSU ${d.nsu}`:'',d.authorization?`Autorização ${d.authorization}`:''].filter(Boolean).join(' · ');
    window.shell(`${window.brand()}<div class="heading"><div><h2>Conferir comprovante</h2><div class="muted">Revise antes de salvar</div></div></div><div class="card"><div class="scan-result-grid"><div><small>Estabelecimento</small><b>${tEsc(d.merchant||'Não identificado')}</b></div><div><small>Forma detectada</small><b>${tEsc(d.payment||'Não identificada')}</b></div></div><label>Descrição</label><input id="receipt-desc" value="${tEsc(d.merchant||'Compra / comprovante')}"><label>Categoria</label><select id="receipt-cat"><option>Alimentação</option><option>Supermercado</option><option>Transporte</option><option>Combustível</option><option>Saúde</option><option>Farmácia</option><option>Lazer</option><option>Vestuário</option><option selected>Outros</option></select><div class="row"><div><label>Valor</label><input id="receipt-value" inputmode="decimal" value="${amount>0?amount.toFixed(2).replace('.',','):''}" placeholder="0,00"></div><div><label>Data</label><input id="receipt-date" type="date" value="${tEsc(d.date||window.today())}"></div></div><label>Observações</label><textarea id="receipt-obs" rows="3">${tEsc(obs)}</textarea><details><summary>Texto reconhecido</summary><div class="scan-raw">${tEsc(d.raw_text||'')}</div></details><button class="primary" id="receipt-save">Criar lançamento</button><button class="secondary" onclick="receiptScanView()">Ler novamente</button><button class="link" onclick="transactions()">Cancelar</button><div id="out"></div><p class="hint">Se o comprovante for de cartão de crédito, confira para não cadastrar a mesma despesa novamente quando lançar a fatura.</p></div>`,true);
    document.getElementById('receipt-save').onclick=saveReceiptTransaction;
  };

  function saveReceiptTransaction(){
    const out=document.getElementById('out'),desc=document.getElementById('receipt-desc').value.trim(),raw=document.getElementById('receipt-value').value.trim(),amount=ptNum(raw);
    if(!desc||!Number.isFinite(amount)||amount<=0){out.innerHTML=tNote('Confira descrição e valor.','err');return}
    out.innerHTML=tNote('Salvando lançamento...');
    const notes=document.getElementById('receipt-obs').value.trim();
    window.GranaManage?.addTransaction?.(JSON.stringify({type:'expense',description:desc,category:document.getElementById('receipt-cat').value,totalAmount:String(amount),dueDate:document.getElementById('receipt-date').value||window.today(),installments:1,observations:notes?`Comprovante lido pelo GranaOk · ${notes}`:'Comprovante lido pelo GranaOk'}));
  }

  window.statementImportView=function(){
    statementRows=[];statementFile='extrato';statementTotal=0;importAccounts=[];
    window.shell(`${window.brand()}<div class="heading"><div><h2>Importar extrato</h2><div class="muted">OFX, QFX, CSV, TXT, PDF ou imagem</div></div></div><div class="card"><p>Escolha o arquivo do banco. O GranaOk tenta reconhecer data, descrição, débito/crédito e valor e mostra tudo para conferência.</p><button class="primary" onclick="GranaImport.chooseBankStatement()">📄 Escolher extrato</button><p class="hint">OFX/QFX oferece a leitura mais confiável. PDF e imagens usam reconhecimento de texto e podem exigir correções.</p><div id="statement-progress"></div></div><button class="link" onclick="transactions()">Voltar aos lançamentos</button>`,true);
    try{imp()?.loadAccounts?.()}catch(e){}
  };

  window.GranaOkImportAccounts=function(s){const d=tJson(s);if(d.ok)importAccounts=d.rows||[];fillStatementAccounts()};
  function fillStatementAccounts(){const sel=document.getElementById('statement-account');if(!sel)return;sel.innerHTML='<option value="0">Sem vincular a uma conta</option>'+importAccounts.filter(x=>x.active!==false).map(x=>`<option value="${Number(x.id)}">${tEsc(x.name)}</option>`).join('')}

  window.GranaOkStatementParsed=function(s){
    const d=tJson(s),progress=document.getElementById('statement-progress');
    if(!d.ok){if(progress)progress.innerHTML=tNote(tEsc(d.error||'Não foi possível ler o extrato.'),'err');return}
    statementFile=d.file_name||'extrato';statementTotal=Number(d.count||0);statementRows=(d.rows||[]).slice(0,300);
    if(!statementRows.length){if(progress)progress.innerHTML=tNote('Nenhum lançamento foi identificado. Se o arquivo for PDF, tente um OFX/CSV do banco ou um PDF com texto mais nítido.','err');return}
    renderStatementReview(d.format||'Extrato');
  };

  function renderStatementReview(format){
    const limited=statementTotal>statementRows.length?`<div class="radar-warning">O arquivo tem ${statementTotal} itens detectados. Para manter a tela leve, esta revisão mostra os primeiros ${statementRows.length} por vez.</div>`:'';
    window.shell(`${window.brand()}<div class="heading"><div><h2>Conferir extrato</h2><div class="muted">${tEsc(statementFile)} · ${tEsc(format)}</div></div></div><div class="card"><label>Conta relacionada</label><select id="statement-account" class="statement-account"></select><div class="statement-head"><div><b>${statementRows.length} lançamento(s) para revisar</b><small class="muted">Itens importados do extrato entram como pagos.</small></div><div class="statement-actions"><button class="secondary" onclick="toggleStatementRows(true)">Todos</button><button class="secondary" onclick="toggleStatementRows(false)">Nenhum</button></div></div>${limited}<div class="statement-list">${statementRows.map((r,i)=>statementRowHtml(r,i)).join('')}</div><button class="primary" id="statement-import">Importar selecionados</button><button class="secondary" onclick="statementImportView()">Escolher outro arquivo</button><button class="link" onclick="transactions()">Cancelar</button><div id="out"></div><p class="hint">O GranaOk usa identificação/hash para ignorar lançamentos iguais em uma nova importação. A importação não altera automaticamente o saldo atual da conta cadastrada.</p></div>`,true);
    fillStatementAccounts();document.getElementById('statement-import').onclick=importSelectedStatement;
  }

  function statementRowHtml(r,i){
    const amount=Number(r.amount||0);
    return `<div class="statement-row"><input type="checkbox" id="stmt-check-${i}" checked><input class="statement-date" type="date" id="stmt-date-${i}" value="${tEsc(r.date||'')}"><input class="statement-desc" id="stmt-desc-${i}" value="${tEsc(r.description||'')}"><select class="statement-type" id="stmt-type-${i}"><option value="expense" ${r.type==='expense'?'selected':''}>Saída</option><option value="income" ${r.type==='income'?'selected':''}>Entrada</option></select><input class="statement-amount ${r.type==='income'?'amount-income':'amount-expense'}" inputmode="decimal" id="stmt-amount-${i}" value="${amount.toFixed(2).replace('.',',')}"></div>`;
  }

  window.toggleStatementRows=function(checked){statementRows.forEach((_,i)=>{const x=document.getElementById(`stmt-check-${i}`);if(x)x.checked=checked})};

  function importSelectedStatement(){
    const selected=[];
    statementRows.forEach((r,i)=>{
      const check=document.getElementById(`stmt-check-${i}`);if(!check?.checked)return;
      const amount=ptNum(document.getElementById(`stmt-amount-${i}`).value);
      const date=document.getElementById(`stmt-date-${i}`).value,description=document.getElementById(`stmt-desc-${i}`).value.trim(),type=document.getElementById(`stmt-type-${i}`).value;
      if(!date||!description||!Number.isFinite(amount)||amount<=0)return;
      selected.push({date,description,type,amount:String(amount),external_id:r.external_id||'',observations:r.observations||''});
    });
    const out=document.getElementById('out');if(!selected.length){out.innerHTML=tNote('Selecione pelo menos um lançamento válido.','err');return}
    out.innerHTML=tNote(`Importando ${selected.length} lançamento(s)...`);
    imp()?.importStatement?.(JSON.stringify({accountId:Number(document.getElementById('statement-account').value||0),fileName:statementFile,source:'bank_statement',rows:selected}));
  }

  window.GranaOkStatementImported=function(s){
    const d=tJson(s),out=document.getElementById('out');if(!out)return;
    if(!d.ok){out.innerHTML=tNote(tEsc(d.error||'Erro durante a importação.'),'err');return}
    out.innerHTML=`<div class="note ok"><b>${tEsc(d.message||'Importação concluída.')}</b><div class="import-summary"><div><b>${Number(d.imported||0)}</b><small>importados</small></div><div><b>${Number(d.skipped||0)}</b><small>duplicados</small></div><div><b>${Number(d.invalid||0)}</b><small>ignorados</small></div></div><button class="secondary" onclick="transactions()">Ver lançamentos</button></div>`;
  };

  window.investmentRadarView=function(){
    window.shell(`${window.brand()}<div class="radar-header"><div><h2>Radar de Investimentos</h2><div class="muted">Fontes públicas online e comparação por objetivo</div></div><button class="secondary" id="radar-refresh">↻ Atualizar</button></div><div id="radar-content">${tNote('Consultando Banco Central e Tesouro Direto...')}</div><button class="link" onclick="dashboard()">Voltar ao painel</button>`,true);
    document.getElementById('radar-refresh').onclick=loadRadar;loadRadar();
  };

  function loadRadar(){const el=document.getElementById('radar-content');if(el)el.innerHTML=tNote('Atualizando fontes oficiais...');try{invest()?.refresh?.()}catch(e){if(el)el.innerHTML=tNote('Radar online indisponível.','err')}}

  window.GranaOkInvestmentRadar=function(s){
    const d=tJson(s),el=document.getElementById('radar-content');if(!el)return;
    if(!d.ok){el.innerHTML=tNote(tEsc(d.error||'Não foi possível atualizar o radar.'),'err');return}
    const b=d.benchmarks||{},selic=b.selic_effective||{},meta=b.selic_target||{},ipca=b.ipca_monthly||{},treasury=d.treasury||[];
    const benchmark=`<div class="benchmark-grid"><div class="benchmark-card"><span>Meta Selic</span><b>${Number(meta.value||0)?Number(meta.value).toFixed(2).replace('.',',')+'%':'—'}</b><small>${tEsc(meta.date||'BCB')}</small></div><div class="benchmark-card"><span>Selic efetiva anualizada</span><b>${Number(selic.value||0)?Number(selic.value).toFixed(2).replace('.',',')+'%':'—'}</b><small>${tEsc(selic.date||'BCB')}</small></div><div class="benchmark-card"><span>IPCA mensal</span><b>${Number.isFinite(Number(ipca.value))&&ipca.date?Number(ipca.value).toFixed(2).replace('.',',')+'%':'—'}</b><small>${tEsc(ipca.date||'BCB')}</small></div></div>`;
    const objectives=`<div class="card"><h3>Destaques por objetivo</h3><div class="radar-objectives">${(d.radar||[]).map(r=>`<div class="radar-item"><span class="objective">${tEsc(r.objective)}</span><h4>${tEsc(r.option)}</h4><p>${tEsc(r.note)}</p></div>`).join('')}</div></div>`;
    const tesouroStatus=String(d.source_status?.tesouro||'');
    const treasuryHtml=treasury.length?`<div class="card"><h3>🇧🇷 Tesouro Direto · títulos retornados agora</h3><p class="muted compact">Taxa exibida para investimento na fonte consultada. Compare também prazo e risco de venda antecipada.</p><div class="treasury-list">${treasury.slice(0,16).map(x=>`<div class="treasury-row"><div><b>${tEsc(x.name)}</b><small>Vencimento ${tEsc(x.maturity||'-')} · Unidade ${Number(x.unit_invest_value||0)>0?tMoney(x.unit_invest_value):'—'}</small></div><div class="treasury-rate"><strong>${Number(x.annual_invest_rate||0)>0?Number(x.annual_invest_rate).toFixed(2).replace('.',',')+'% a.a.':'—'}</strong></div></div>`).join('')}</div></div>`:`<div class="card"><h3>🇧🇷 Tesouro Direto</h3><div class="radar-warning">A cotação automática do Tesouro não respondeu nesta atualização (${tEsc(tesouroStatus||'fonte indisponível')}). Os benchmarks do Banco Central continuam válidos e você pode abrir a fonte oficial abaixo.</div></div>`;
    const refs=`<div class="card"><h3>Fontes</h3><div class="source-list">${(d.references||[]).map((r,i)=>`<button class="source-btn" data-source="${i}"><span>${tEsc(r.name)}</span><b>↗</b></button>`).join('')}</div><div class="status-source">Atualizado: ${tEsc(d.updated_at||'-')}\n${Object.entries(d.source_status||{}).map(([k,v])=>`${k}: ${v}`).join('\n')}</div></div>`;
    el.innerHTML=`${benchmark}${objectives}${treasuryHtml}${refs}<div class="radar-disclaimer">${tEsc(d.disclaimer||'Painel informativo, não é recomendação de investimento.')}</div>`;
    const references=d.references||[];el.querySelectorAll('[data-source]').forEach(btn=>btn.onclick=()=>{const r=references[Number(btn.dataset.source)];if(r?.url)invest()?.openOfficialSource?.(r.url)});
  };
})();
