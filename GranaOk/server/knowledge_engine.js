const { withConn, ensureSchema } = require('./db');

function clamp(n,min,max){return Math.max(min,Math.min(max,Number(n||0)))}
function round2(n){return Math.round(Number(n||0)*100)/100}
function monthKey(d){return new Date(d).toISOString().slice(0,7)}
function normalizeKey(v){
  return String(v||'')
    .normalize('NFD').replace(/[\u0300-\u036f]/g,'')
    .toLowerCase()
    .replace(/\b(pix|transf|transferencia|deb|cred|compra|pagamento|pgto|emit|receb|outra if|msm)\b/g,' ')
    .replace(/\d{5,}/g,' ')
    .replace(/[^a-z0-9]+/g,' ')
    .trim()
    .replace(/\s+/g,' ')
    .slice(0,160);
}
function avg(values){return values.length?values.reduce((a,b)=>a+Number(b||0),0)/values.length:0}
function std(values){
  if(values.length<2)return 0;
  const m=avg(values);
  return Math.sqrt(values.reduce((s,v)=>s+Math.pow(Number(v||0)-m,2),0)/values.length);
}
function cv(values){
  const m=avg(values);
  return m>0?std(values)/m:0;
}
function personWhere(alias,user,field='person_id'){
  if(user&&user.person_id)return {sql:' AND ('+alias+'.'+field+'=? OR '+alias+'.'+field+' IS NULL)',params:[Number(user.person_id)]};
  return {sql:'',params:[]};
}
function previousMonths(count){
  const out=[],now=new Date();
  now.setDate(1);
  for(let i=count-1;i>=0;i--){
    const d=new Date(now.getFullYear(),now.getMonth()-i,1,12,0,0);
    out.push(d.toISOString().slice(0,7));
  }
  return out;
}
function confidencePattern(g){
  const amounts=g.amounts||[],months=g.months?g.months.size:0,occ=amounts.length;
  const stability=Math.max(0,1-Math.min(1,cv(amounts)));
  return round2(clamp(30+months*9+Math.min(20,occ*2)+stability*20,0,99));
}
async function loadBehavior(conn,p,user,months){
  const txFilter=personWhere('t',user);
  const [tx]=await conn.execute(
    "SELECT t.id,t.type,t.description,t.amount,t.category_id,COALESCE(c.name,'Outros') category,"+
    "DATE_FORMAT(t.due_date,'%Y-%m-%d') event_date,DATE_FORMAT(t.due_date,'%Y-%m') month_key,DAY(t.due_date) day_no,"+
    "t.status,CASE WHEN t.paid_date IS NULL THEN NULL ELSE DATE_FORMAT(t.paid_date,'%Y-%m-%d') END paid_date "+
    "FROM "+p+"transactions t LEFT JOIN "+p+"categories c ON c.id=t.category_id "+
    "WHERE t.due_date>=DATE_SUB(DATE_FORMAT(CURDATE(),'%Y-%m-01'),INTERVAL "+Number(months-1)+" MONTH)"+
    txFilter.sql+" ORDER BY t.due_date,t.id",
    txFilter.params
  );
  const cpFilter=personWhere('cp',user);
  const [cp]=await conn.execute(
    "SELECT cp.id,'expense' type,cp.description,cp.amount,cp.category_id,COALESCE(c.name,'Outros') category,"+
    "DATE_FORMAT(cp.purchase_date,'%Y-%m-%d') purchase_date,DATE_FORMAT(cp.due_date,'%Y-%m-%d') event_date,"+
    "DATE_FORMAT(cp.due_date,'%Y-%m') month_key,DAY(cp.purchase_date) day_no,cp.installment_number,cp.installment_total "+
    "FROM "+p+"card_purchases cp LEFT JOIN "+p+"categories c ON c.id=cp.category_id "+
    "WHERE cp.due_date>=DATE_SUB(DATE_FORMAT(CURDATE(),'%Y-%m-01'),INTERVAL "+Number(months-1)+" MONTH)"+
    cpFilter.sql+" ORDER BY cp.due_date,cp.id",
    cpFilter.params
  );
  const cardFilter=user&&user.person_id?' AND (c.person_id=? OR c.person_id IS NULL)':'';
  const cardParams=user&&user.person_id?[Number(user.person_id)]:[];
  const [iv]=await conn.execute(
    "SELECT DATE_FORMAT(i.reference_month,'%Y-%m') month_key,COALESCE(SUM(i.amount),0) total,"+
    "COALESCE(SUM(CASE WHEN i.status='paid' THEN i.amount ELSE 0 END),0) paid,"+
    "COALESCE(SUM(CASE WHEN i.status<>'paid' THEN i.amount ELSE 0 END),0) pending "+
    "FROM "+p+"card_invoices i JOIN "+p+"cards c ON c.id=i.card_id "+
    "WHERE i.reference_month>=DATE_SUB(DATE_FORMAT(CURDATE(),'%Y-%m-01'),INTERVAL "+Number(months-1)+" MONTH)"+
    cardFilter+" GROUP BY DATE_FORMAT(i.reference_month,'%Y-%m')",
    cardParams
  );
  return {tx,cp,iv};
}
function monthlyFeatures(rows,months){
  const keys=previousMonths(months),map=new Map(keys.map(k=>[k,{
    month:k,income:0,expense:0,paidExpense:0,pendingExpense:0,overdue:0,card:0,cardPaid:0,cardPending:0,installments:0
  }]));
  const now=new Date().toISOString().slice(0,10);
  for(const r of rows.tx){
    const m=map.get(r.month_key);if(!m)continue;
    const amount=Number(r.amount||0);
    if(r.type==='income')m.income+=amount;
    else{
      m.expense+=amount;
      if(r.status==='paid')m.paidExpense+=amount;else m.pendingExpense+=amount;
      if(r.status!=='paid'&&r.event_date<now)m.overdue+=amount;
    }
  }
  for(const r of rows.cp){
    const m=map.get(r.month_key);if(!m)continue;
    if(Number(r.installment_total||1)>1)m.installments+=Number(r.amount||0);
  }
  for(const r of rows.iv){
    const m=map.get(r.month_key);if(!m)continue;
    m.card=Number(r.total||0);m.cardPaid=Number(r.paid||0);m.cardPending=Number(r.pending||0);
  }
  return [...map.values()].map(m=>{
    const totalOut=m.expense+m.card;
    const net=m.income-totalOut;
    return Object.assign(m,{
      expense:round2(m.expense),card:round2(m.card),income:round2(m.income),
      paidExpense:round2(m.paidExpense),pendingExpense:round2(m.pendingExpense),overdue:round2(m.overdue),
      installments:round2(m.installments),net:round2(net),savingsRate:m.income>0?round2(net/m.income):0
    });
  });
}
function recurringPatterns(rows){
  const groups=new Map();
  for(const r of rows.tx){
    const key=normalizeKey(r.description);if(!key)continue;
    const gk=r.type+'|'+key;
    if(!groups.has(gk))groups.set(gk,{type:r.type,key,label:r.description,category_id:r.category_id,category:r.category,amounts:[],days:[],months:new Set(),last:null});
    const g=groups.get(gk);
    g.amounts.push(Number(r.amount||0));g.days.push(Number(r.day_no||0));g.months.add(r.month_key);
    if(!g.last||r.event_date>g.last)g.last=r.event_date;
    if(!g.category_id&&r.category_id)g.category_id=r.category_id;
  }
  return [...groups.values()]
    .filter(g=>g.amounts.length>=2&&g.months.size>=2)
    .map(g=>({
      pattern_type:g.type==='income'?'recurring_income':'recurring_expense',
      pattern_key:g.key,label:g.label,category_id:g.category_id||null,category:g.category,
      avg_amount:round2(avg(g.amounts)),occurrences:g.amounts.length,months_seen:g.months.size,
      day_min:Math.min(...g.days.filter(Boolean)),day_max:Math.max(...g.days.filter(Boolean)),
      confidence:confidencePattern(g),last_seen:g.last
    }))
    .sort((a,b)=>b.confidence-a.confidence||b.avg_amount-a.avg_amount);
}
function merchantMemory(rows){
  const groups=new Map();
  const all=rows.tx.concat(rows.cp);
  for(const r of all){
    const key=normalizeKey(r.description);if(!key)continue;
    const gk=r.type+'|'+key;
    if(!groups.has(gk))groups.set(gk,{type:r.type,key,label:r.description,category_id:r.category_id,category:r.category,amounts:[],months:new Set(),last:null});
    const g=groups.get(gk);g.amounts.push(Number(r.amount||0));g.months.add(r.month_key);
    const date=r.purchase_date||r.event_date;if(!g.last||date>g.last)g.last=date;
    if(!g.category_id&&r.category_id)g.category_id=r.category_id;
  }
  return [...groups.values()]
    .filter(g=>g.amounts.length>=2)
    .map(g=>({
      merchant_key:g.key,merchant_label:g.label,category_id:g.category_id||null,category:g.category,txn_type:g.type,
      occurrences:g.amounts.length,avg_amount:round2(avg(g.amounts)),
      confidence:round2(clamp(35+Math.min(30,g.amounts.length*5)+Math.min(20,g.months.size*4)+(1-Math.min(1,cv(g.amounts)))*14,0,99)),
      last_seen:g.last
    }))
    .sort((a,b)=>b.occurrences-a.occurrences||b.confidence-a.confidence);
}
function profileFrom(features,patterns,memory){
  const active=features.filter(x=>x.income>0||x.expense>0||x.card>0);
  const income=active.map(x=>x.income),out=active.map(x=>x.expense+x.card),cards=active.map(x=>x.card);
  const recurringIncome=patterns.filter(x=>x.pattern_type==='recurring_income');
  const recurringExpense=patterns.filter(x=>x.pattern_type==='recurring_expense');
  const avgIncome=avg(income),avgOut=avg(out),avgCard=avg(cards);
  const score=clamp(25+Math.min(35,active.length*4)+Math.min(20,patterns.length*2)+Math.min(15,memory.length),0,99);
  return {
    months_with_data:active.length,
    average_income:round2(avgIncome),
    average_outflow:round2(avgOut),
    average_card:round2(avgCard),
    average_net:round2(avgIncome-avgOut),
    income_volatility:round2(avgIncome>0?std(income)/avgIncome:0),
    outflow_volatility:round2(avgOut>0?std(out)/avgOut:0),
    card_share_of_income:round2(avgIncome>0?avgCard/avgIncome:0),
    recurring_income_count:recurringIncome.length,
    recurring_expense_count:recurringExpense.length,
    merchant_memory_count:memory.length,
    confidence_score:round2(score)
  };
}
function makeRecommendations(features,patterns,profile,suppressed){
  const rec=[],latest=features[features.length-1]||{},prev=features.slice(Math.max(0,features.length-4),-1);
  const avgPrev=avg(prev.map(x=>x.expense+x.card));
  const totalOut=Number(latest.expense||0)+Number(latest.card||0);
  const income=Number(latest.income||0);
  const add=(rule,title,message,severity,confidence,evidence,source)=>{
    if(suppressed.has(rule))return;
    rec.push({rule_key:rule,title,message,severity,confidence:round2(confidence),evidence_json:JSON.stringify(evidence||{}),source_key:source||null});
  };

  if(Number(latest.overdue||0)>0)add(
    'overdue_current','Há despesas vencidas',
    'Existem despesas pendentes já vencidas. Vale priorizar a revisão dessas obrigações antes de assumir novos compromissos.',
    'warning',96,{overdue_total:latest.overdue,month:latest.month},'bcb_orcamento'
  );
  if(income>0&&totalOut>income)add(
    'month_negative_flow','Saídas acima das entradas',
    'Neste mês, despesas e faturas estão acima das entradas registradas. Revise os maiores grupos de gasto e compromissos futuros para entender o que pode ser ajustado.',
    'warning',92,{income,total_out:round2(totalOut),difference:round2(totalOut-income),month:latest.month},'bcb_orcamento'
  );
  if(income>0&&Number(latest.card||0)/income>=0.30)add(
    'card_pressure','Uso de cartão ganhou peso no fluxo',
    'As faturas representam uma parcela relevante das entradas deste mês. O percentual é um sinal interno do GranaOk, não uma regra universal; use-o para revisar parcelamentos e compras futuras.',
    'attention',86,{card_total:latest.card,income,ratio:round2(Number(latest.card||0)/income),heuristic:'internal_30_percent_signal'},'bcb_orcamento'
  );
  if(avgPrev>0&&totalOut>avgPrev*1.15)add(
    'outflow_growth','Saídas acima do padrão recente',
    'As saídas deste mês estão acima da média dos três meses anteriores. O GranaOk sugere conferir quais categorias ou compras explicam essa mudança.',
    'attention',88,{current:round2(totalOut),previous_average:round2(avgPrev),change:round2(totalOut/avgPrev-1)},'cvm_planejamento'
  );
  if(profile.average_net>0&&profile.months_with_data>=3)add(
    'reserve_opportunity','Há margem histórica positiva',
    'A média recente indica alguma margem entre entradas e saídas. Considere transformar parte dessa margem em uma meta explícita de reserva ou objetivo financeiro, de acordo com suas prioridades.',
    'info',78,{average_net:profile.average_net,months:profile.months_with_data},'cvm_planejamento'
  );
  const incomes=patterns.filter(x=>x.pattern_type==='recurring_income'&&x.confidence>=70).slice(0,3);
  if(incomes.length)add(
    'income_pattern','Padrão de receita identificado',
    'O motor encontrou receita recorrente com boa confiança. Isso melhora a previsão de fluxo dos próximos meses, mas o padrão continua sendo revisado conforme chegam novos lançamentos.',
    'info',Math.max(...incomes.map(x=>x.confidence)),{patterns:incomes.map(x=>({label:x.label,avg_amount:x.avg_amount,confidence:x.confidence}))},'bcb_educacao'
  );
  const fixed=patterns.filter(x=>x.pattern_type==='recurring_expense'&&x.confidence>=75);
  if(fixed.length>=3)add(
    'recurring_commitments','Compromissos recorrentes aprendidos',
    'O GranaOk já reconhece '+fixed.length+' despesas recorrentes com boa confiança. Elas passam a compor a leitura de compromissos mensais e previsões futuras.',
    'info',round2(avg(fixed.map(x=>x.confidence))),{count:fixed.length,total_average:round2(fixed.reduce((s,x)=>s+x.avg_amount,0))},'cvm_planejamento'
  );
  return rec;
}
async function suppressedRules(conn,p,userId){
  const [rows]=await conn.execute(
    "SELECT DISTINCT r.rule_key FROM "+p+"ai_feedback f JOIN "+p+"recommendations r ON r.id=f.recommendation_id "+
    "WHERE f.user_id=? AND f.feedback='not_relevant' AND f.created_at>=DATE_SUB(NOW(),INTERVAL 180 DAY)",
    [userId]
  );
  return new Set(rows.map(r=>r.rule_key));
}
async function rebuildKnowledge(user,months=12){
  months=clamp(Math.round(months||12),3,24);
  return withConn(async conn=>{
    const p=await ensureSchema(conn),userId=Number(user.id),personId=user.person_id?Number(user.person_id):null;
    const rows=await loadBehavior(conn,p,user,months);
    const features=monthlyFeatures(rows,months);
    const patterns=recurringPatterns(rows);
    const memory=merchantMemory(rows);
    const profile=profileFrom(features,patterns,memory);
    const suppressed=await suppressedRules(conn,p,userId);
    const recs=makeRecommendations(features,patterns,profile,suppressed);

    await conn.beginTransaction();
    try{
      await conn.execute('DELETE FROM '+p+'monthly_features WHERE user_id=?',[userId]);
      await conn.execute('DELETE FROM '+p+'recurring_patterns WHERE user_id=?',[userId]);
      await conn.execute('DELETE FROM '+p+'merchant_memory WHERE user_id=?',[userId]);
      await conn.execute("UPDATE "+p+"recommendations SET status='archived',updated_at=NOW() WHERE user_id=? AND status='active'",[userId]);

      for(const x of features){
        await conn.execute(
          'INSERT INTO '+p+'monthly_features(user_id,person_id,reference_month,income_total,expense_total,card_total,paid_expense_total,pending_expense_total,overdue_total,installment_commitment,net_cashflow,savings_rate,updated_at) VALUES(?,?,?,?,?,?,?,?,?,?,?,?,NOW())',
          [userId,personId,x.month+'-01',x.income,x.expense,x.card,x.paidExpense,x.pendingExpense,x.overdue,x.installments,x.net,x.savingsRate]
        );
      }
      for(const x of patterns){
        await conn.execute(
          'INSERT INTO '+p+'recurring_patterns(user_id,person_id,pattern_type,pattern_key,label,category_id,frequency,avg_amount,occurrences,months_seen,day_min,day_max,confidence,last_seen,active,updated_at) VALUES(?,?,?,?,?,?,"monthly",?,?,?,?,?,?,?,1,NOW())',
          [userId,personId,x.pattern_type,x.pattern_key,x.label,x.category_id,x.avg_amount,x.occurrences,x.months_seen,x.day_min||null,x.day_max||null,x.confidence,x.last_seen]
        );
      }
      for(const x of memory){
        await conn.execute(
          'INSERT INTO '+p+'merchant_memory(user_id,person_id,merchant_key,merchant_label,category_id,txn_type,occurrences,avg_amount,confidence,last_seen,updated_at) VALUES(?,?,?,?,?,?,?,?,?,?,NOW())',
          [userId,personId,x.merchant_key,x.merchant_label,x.category_id,x.txn_type,x.occurrences,x.avg_amount,x.confidence,x.last_seen]
        );
      }
      for(const x of recs){
        await conn.execute(
          'INSERT INTO '+p+'recommendations(user_id,person_id,rule_key,title,message,severity,confidence,evidence_json,source_key,status,updated_at) VALUES(?,?,?,?,?,?,?,?,?,"active",NOW())',
          [userId,personId,x.rule_key,x.title,x.message,x.severity,x.confidence,x.evidence_json,x.source_key]
        );
      }
      await conn.execute(
        'INSERT INTO '+p+'learning_profiles(user_id,person_id,months_analyzed,confidence_score,profile_json,last_rebuilt_at,active,updated_at) VALUES(?,?,?,?,?,NOW(),1,NOW()) '+
        'ON DUPLICATE KEY UPDATE person_id=VALUES(person_id),months_analyzed=VALUES(months_analyzed),confidence_score=VALUES(confidence_score),profile_json=VALUES(profile_json),last_rebuilt_at=NOW(),active=1,updated_at=NOW()',
        [userId,personId,months,profile.confidence_score,JSON.stringify(profile)]
      );
      await conn.commit();
    }catch(e){await conn.rollback();throw e}
    return {profile,features,patterns:patterns.slice(0,20),merchants:memory.slice(0,20),recommendations:recs,rebuilt_at:new Date().toISOString()};
  });
}
async function getKnowledgeSummary(user){
  return withConn(async conn=>{
    const p=await ensureSchema(conn),userId=Number(user.id);
    const [pr]=await conn.execute("SELECT months_analyzed,confidence_score,profile_json,CASE WHEN last_rebuilt_at IS NULL THEN NULL ELSE DATE_FORMAT(last_rebuilt_at,'%Y-%m-%d %H:%i:%s') END last_rebuilt_at FROM "+p+"learning_profiles WHERE user_id=? AND active=1 LIMIT 1",[userId]);
    if(!pr[0])return {ready:false};
    let profile={};try{profile=JSON.parse(pr[0].profile_json||'{}')}catch(_){}
    const [patterns]=await conn.execute(
      "SELECT id,pattern_type,label,avg_amount,occurrences,months_seen,day_min,day_max,confidence,DATE_FORMAT(last_seen,'%Y-%m-%d') last_seen FROM "+p+"recurring_patterns WHERE user_id=? AND active=1 ORDER BY confidence DESC,avg_amount DESC LIMIT 20",
      [userId]
    );
    const [merchants]=await conn.execute(
      "SELECT id,merchant_label,txn_type,occurrences,avg_amount,confidence,DATE_FORMAT(last_seen,'%Y-%m-%d') last_seen FROM "+p+"merchant_memory WHERE user_id=? ORDER BY occurrences DESC,confidence DESC LIMIT 15",
      [userId]
    );
    const [recs]=await conn.execute(
      "SELECT r.id,r.rule_key,r.title,r.message,r.severity,r.confidence,r.evidence_json,r.source_key,ks.name source_name,ks.url source_url "+
      "FROM "+p+"recommendations r LEFT JOIN "+p+"knowledge_sources ks ON ks.source_key=r.source_key "+
      "WHERE r.user_id=? AND r.status='active' ORDER BY FIELD(r.severity,'warning','attention','info'),r.confidence DESC,r.id DESC",
      [userId]
    );
    const [sources]=await conn.query("SELECT source_key,name,url,authority FROM "+p+"knowledge_sources WHERE active=1 ORDER BY authority,name");
    return {
      ready:true,months_analyzed:Number(pr[0].months_analyzed||0),confidence_score:Number(pr[0].confidence_score||0),
      last_rebuilt_at:pr[0].last_rebuilt_at,profile,patterns,merchants,recommendations:recs,sources
    };
  });
}
async function saveFeedback(user,recommendationId,feedback,comment){
  const allowed=new Set(['useful','not_relevant','done','later']);
  if(!allowed.has(String(feedback||'')))throw new Error('Feedback inválido.');
  return withConn(async conn=>{
    const p=await ensureSchema(conn),userId=Number(user.id),rid=Number(recommendationId||0);
    if(!rid)throw new Error('Recomendação inválida.');
    const [r]=await conn.execute('SELECT id FROM '+p+'recommendations WHERE id=? AND user_id=? LIMIT 1',[rid,userId]);
    if(!r[0])throw new Error('Recomendação não encontrada.');
    await conn.execute('INSERT INTO '+p+'ai_feedback(user_id,recommendation_id,feedback,comment) VALUES(?,?,?,?)',[userId,rid,String(feedback),String(comment||'').slice(0,500)||null]);
    if(feedback==='done'||feedback==='not_relevant')await conn.execute("UPDATE "+p+"recommendations SET status='dismissed',updated_at=NOW() WHERE id=?",[rid]);
    return {ok:true};
  });
}

module.exports={rebuildKnowledge,getKnowledgeSummary,saveFeedback};
