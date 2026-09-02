function num(v){const n=Number(String(v??'0').replace(',','.'));return Number.isFinite(n)?n:0}
function one(v){return Number(v||0).toLocaleString('pt-BR',{minimumFractionDigits:2,maximumFractionDigits:2})}
async function getJson(url,timeoutMs=8000){
  const ctrl=new AbortController();
  const timer=setTimeout(()=>ctrl.abort(),timeoutMs);
  try{
    const r=await fetch(url,{headers:{Accept:'application/json','User-Agent':'GranaOk-Web/0.2'},signal:ctrl.signal});
    if(!r.ok)throw new Error('HTTP '+r.status);
    return await r.json();
  } finally {clearTimeout(timer)}
}
async function latestBcb(series){
  const data=await getJson('https://api.bcb.gov.br/dados/serie/bcdata.sgs.'+series+'/dados/ultimos/1?formato=json');
  if(!Array.isArray(data)||!data.length)throw new Error('Série '+series+' sem dados.');
  const x=data[data.length-1];
  return {series,date:x.data,value:num(x.valor)};
}
async function treasury(){
  const root=await getJson('https://www.tesourodireto.com.br/json/br/com/b3/tesourodireto/service/api/treasurybondsinfo.json',10000);
  const list=root&&root.response&&Array.isArray(root.response.TrsrBdTradgList)?root.response.TrsrBdTradgList:[];
  return list.slice(0,40).map(w=>w&&w.TrsrBd).filter(Boolean).map(b=>({
    name:String(b.nm||''),
    maturity:String(b.mtrtyDt||'').slice(0,10),
    annual_invest_rate:num(b.anulInvstmtRate),
    unit_invest_value:num(b.untrInvstmtVal),
    annual_redemption_rate:num(b.anulRedRate),
    unit_redemption_value:num(b.untrRedVal)
  })).filter(x=>x.name);
}
async function buildInvestmentRadar(){
  const status={},benchmarks={};
  try{benchmarks.selic_effective=await latestBcb(1178);status.bcb_selic='ok'}catch(e){status.bcb_selic='indisponível'}
  try{benchmarks.selic_target=await latestBcb(432);status.bcb_meta='ok'}catch(e){status.bcb_meta='indisponível'}
  try{benchmarks.ipca_monthly=await latestBcb(433);status.bcb_ipca='ok'}catch(e){status.bcb_ipca='indisponível'}
  let bonds=[];try{bonds=await treasury();status.tesouro=bonds.length?'ok':'sem dados'}catch(e){status.tesouro='indisponível'}
  const selic=(benchmarks.selic_target||benchmarks.selic_effective||{}).value||0;
  const ipca=(benchmarks.ipca_monthly||{}).value||0;
  return {
    updated_at:new Date().toISOString(),
    benchmarks,treasury:bonds.slice(0,24),source_status:status,
    radar:[
      {objective:'Reserva / curto prazo',option:'Tesouro Selic ou CDB com liquidez diária',note:selic?'Compare rendimento líquido, liquidez e risco com a Selic de referência em '+one(selic)+'% a.a.':'Priorize liquidez, risco e custo total antes da taxa anunciada.'},
      {objective:'Prazo definido',option:'CDB, LCI/LCA e Tesouro Prefixado',note:'Compare vencimento, possibilidade de resgate, risco do emissor e retorno líquido.'},
      {objective:'Proteção contra inflação',option:'Tesouro IPCA+',note:ipca?'O IPCA mensal mais recente retornado pelo BCB é '+one(ipca)+'%. Compare taxa real e vencimento.':'Compare taxa real acima do IPCA, vencimento e marcação a mercado.'},
      {objective:'Comparação',option:'Use CDI/Taxa DI como benchmark',note:'Compare a taxa líquida, prazo, liquidez e risco. Taxa maior isoladamente não define a melhor opção.'}
    ],
    references:[
      {name:'Banco Central · Selic',url:'https://dadosabertos.bcb.gov.br/'},
      {name:'Tesouro Direto · preços e taxas',url:'https://www.tesourodireto.com.br/'}
    ],
    disclaimer:'Radar informativo. Não é recomendação personalizada e não executa ordens.'
  };
}

module.exports={buildInvestmentRadar};
