const { withConn, ensureSchema } = require('./db');

function monthOk(v){
  return /^\d{4}-(0[1-9]|1[0-2])$/.test(String(v||'')) ? String(v) : new Date().toISOString().slice(0,7);
}
function addMonth(m){
  const d=new Date(m+'-01T12:00:00'); d.setMonth(d.getMonth()+1); return d.toISOString().slice(0,7);
}
function brMoney(v){
  return Number(v||0).toLocaleString('pt-BR',{style:'currency',currency:'BRL'});
}
function brDate(v){
  if(!v)return '—';
  const d=new Date(String(v).slice(0,10)+'T12:00:00');
  return Number.isNaN(d.getTime())?String(v):d.toLocaleDateString('pt-BR');
}
function normalize(s){
  return String(s||'').normalize('NFD').replace(/[\u0300-\u036f]/g,'').toLowerCase().replace(/[^a-z0-9%]+/g,' ').replace(/\s+/g,' ').trim();
}
function words(s){
  const stop=new Set(['qual','quais','que','quem','como','onde','quando','quanto','quantos','uma','umas','uns','para','por','com','sem','dos','das','do','da','de','em','no','na','nos','nas','me','eu','voce','esta','estao','esse','essa','isso','aqui','agora','mes','este','essa','tem','tenho','tinha','meu','minha','pra']);
  return normalize(s).split(' ').filter(x=>x.length>=3&&!stop.has(x));
}
function joinTop(rows){
  if(!rows.length)return 'Ainda não há despesas categorizadas nesse mês.';
  return rows.slice(0,5).map((r,i)=>(i+1)+'. '+r.category+': '+brMoney(r.total)).join('\n');
}
function safeHistory(history){
  return Array.isArray(history)?history.slice(-12).map(x=>({role:String(x&&x.role||''),text:String(x&&x.text||'').slice(0,1200)})):[];
}
function explicitTopic(n){
  if(/cartao|cartoes|fatura|credito/.test(n))return 'cards';
  if(/financi|parcela/.test(n))return 'financing';
  if(/receita|entrada|salario|recebi|ganhei|renda/.test(n))return 'income';
  if(/categoria|vestuario|roupa|alimentacao|transporte|saude|farmacia|lazer|moradia|supermercado/.test(n))return 'category';
  if(/despesa|gasto|conta|lancamento|boleto|pix|pagamento/.test(n))return 'transactions';
  if(/projecao|saldo|sobrar|sobra|fluxo/.test(n))return 'projection';
  if(/recomend|sugest|melhorar|economizar|organizar|planej/.test(n))return 'recommendations';
  return '';
}
function topicFromHistory(history){
  for(let i=history.length-1;i>=0;i--){
    const t=explicitTopic(normalize(history[i].text));
    if(t)return t;
  }
  return '';
}
function statusFromText(n){
  if(/atras|vencid/.test(n))return 'overdue';
  if(/pendente|aberta|falta|faltar|pagar|a pagar|nao paga/.test(n))return 'pending';
  if(/paga|pago|quitei|quitad/.test(n))return 'paid';
  return '';
}
function wantsList(n){return /qual|quais|lista|mostra|mostrar|detalh|quem|quero ver|me diga/.test(n)}
function wantsTotal(n){return /quanto|total|soma|valor|gastei|gasto|recebi|entrou|saiu/.test(n)}
function historyText(history){return history.map(x=>x.text).join(' ')}

async function snapshot(month,user){
  const m=monthOk(month), start=m+'-01', next=addMonth(m)+'-01';
  return withConn(async conn=>{
    const p=await ensureSchema(conn);
    const txPerson=user&&user.person_id ? ' AND (t.person_id=? OR t.person_id IS NULL)' : '';
    const txParams=user&&user.person_id ? [start,next,user.person_id] : [start,next];

    const [[bal]]=await conn.query('SELECT COALESCE(SUM(current_balance),0) v FROM '+p+'accounts WHERE active=1');

    const [[tx]]=await conn.execute(
      "SELECT COALESCE(SUM(CASE WHEN type='income' THEN amount ELSE 0 END),0) income,"+
      "COALESCE(SUM(CASE WHEN type='expense' THEN amount ELSE 0 END),0) expenses,"+
      "COALESCE(SUM(CASE WHEN type='expense' AND status='paid' THEN amount ELSE 0 END),0) paid_expenses,"+
      "COALESCE(SUM(CASE WHEN type='expense' AND status<>'paid' THEN amount ELSE 0 END),0) pending_expenses "+
      "FROM "+p+"transactions t WHERE due_date>=? AND due_date<?"+txPerson, txParams
    );

    const txListParams=user&&user.person_id ? [start,next,user.person_id] : [start,next];
    const [transactions]=await conn.execute(
      "SELECT t.id,t.type,t.description,t.amount,t.status,DATE_FORMAT(t.due_date,'%Y-%m-%d') due_date,"+
      "CASE WHEN t.paid_date IS NULL THEN NULL ELSE DATE_FORMAT(t.paid_date,'%Y-%m-%d') END paid_date,"+
      "COALESCE(c.name,'Outros') category,COALESCE(a.name,'Sem conta') account_name "+
      "FROM "+p+"transactions t LEFT JOIN "+p+"categories c ON c.id=t.category_id LEFT JOIN "+p+"accounts a ON a.id=t.account_id "+
      "WHERE t.due_date>=? AND t.due_date<?"+txPerson+" ORDER BY t.due_date,t.id LIMIT 300",
      txListParams
    );

    const cardPerson=user&&user.person_id ? ' AND (c.person_id=? OR c.person_id IS NULL)' : '';
    const ivParams=user&&user.person_id ? [start,next,user.person_id] : [start,next];
    const [[iv]]=await conn.execute(
      "SELECT COALESCE(SUM(i.amount),0) total,COALESCE(SUM(CASE WHEN i.status='paid' THEN i.amount ELSE 0 END),0) paid,"+
      "COALESCE(SUM(CASE WHEN i.status<>'paid' THEN i.amount ELSE 0 END),0) pending FROM "+p+"card_invoices i "+
      "JOIN "+p+"cards c ON c.id=i.card_id WHERE i.reference_month>=? AND i.reference_month<?"+cardPerson,
      ivParams
    );

    const invoiceParams=user&&user.person_id ? [start,next,user.person_id] : [start,next];
    const [invoices]=await conn.execute(
      "SELECT i.id,i.card_id,c.name card_name,i.amount,i.status,DATE_FORMAT(i.reference_month,'%Y-%m') reference_month,"+
      "DATE_FORMAT(i.due_date,'%Y-%m-%d') due_date,CASE WHEN i.paid_date IS NULL THEN NULL ELSE DATE_FORMAT(i.paid_date,'%Y-%m-%d') END paid_date "+
      "FROM "+p+"card_invoices i JOIN "+p+"cards c ON c.id=i.card_id "+
      "WHERE i.reference_month>=? AND i.reference_month<?"+cardPerson+" ORDER BY i.due_date,c.name",
      invoiceParams
    );

    const overdueParams=user&&user.person_id ? [user.person_id] : [];
    const [[ov]]=await conn.execute(
      "SELECT COUNT(*) c,COALESCE(SUM(amount),0) total FROM "+p+"transactions t "+
      "WHERE type='expense' AND status<>'paid' AND due_date<CURDATE()"+
      (user&&user.person_id ? " AND (t.person_id=? OR t.person_id IS NULL)" : ''),
      overdueParams
    );

    const catParams=user&&user.person_id ? [start,next,user.person_id] : [start,next];
    const [cats]=await conn.execute(
      "SELECT COALESCE(c.name,'Outros') category,COALESCE(SUM(t.amount),0) total FROM "+p+"transactions t "+
      "LEFT JOIN "+p+"categories c ON c.id=t.category_id "+
      "WHERE t.type='expense' AND t.due_date>=? AND t.due_date<?"+txPerson+
      " GROUP BY COALESCE(c.name,'Outros') ORDER BY total DESC LIMIT 12",
      catParams
    );

    const upcomingParams=user&&user.person_id ? [user.person_id] : [];
    const [upcoming]=await conn.execute(
      "SELECT description,amount,DATE_FORMAT(due_date,'%Y-%m-%d') due_date FROM "+p+"transactions t "+
      "WHERE type='expense' AND status<>'paid' AND due_date>=CURDATE() AND due_date<=DATE_ADD(CURDATE(),INTERVAL 7 DAY)"+
      (user&&user.person_id ? " AND (t.person_id=? OR t.person_id IS NULL)" : '')+
      " ORDER BY due_date,amount DESC LIMIT 20",
      upcomingParams
    );

    const [financings]=await conn.query(
      "SELECT id,name,installment_amount,total_installments,paid_installments,active,"+
      "CASE WHEN next_due_date IS NULL THEN NULL ELSE DATE_FORMAT(next_due_date,'%Y-%m-%d') END next_due_date "+
      "FROM "+p+"financings WHERE active=1 ORDER BY next_due_date,id"
    );

    let recommendations=[];
    try{
      const [rr]=await conn.execute(
        "SELECT r.id,r.title,r.message,r.confidence,r.source_key,ks.name source_name "+
        "FROM "+p+"recommendations r LEFT JOIN "+p+"knowledge_sources ks ON ks.source_key=r.source_key "+
        "WHERE r.user_id=? AND r.status='active' ORDER BY r.confidence DESC,r.id DESC LIMIT 6",
        [Number(user&&user.id||0)]
      );
      recommendations=rr;
    }catch(_){}

    const total=Number(tx.expenses||0)+Number(iv.total||0);
    return {
      month:m,
      accounts_balance:Number(bal.v||0),
      income:Number(tx.income||0),
      expenses:Number(tx.expenses||0),
      paid_expenses:Number(tx.paid_expenses||0),
      pending_expenses:Number(tx.pending_expenses||0),
      card_invoices:Number(iv.total||0),
      card_paid:Number(iv.paid||0),
      card_pending:Number(iv.pending||0),
      total_monthly_expenses:total,
      projected:Number(bal.v||0)+Number(tx.income||0)-total,
      overdue_count:Number(ov.c||0),
      overdue_total:Number(ov.total||0),
      financing_monthly:financings.reduce((sum,x)=>sum+Number(x.installment_amount||0),0),
      categories:cats.map(r=>({category:r.category,total:Number(r.total||0)})),
      transactions:transactions.map(r=>Object.assign({},r,{amount:Number(r.amount||0)})),
      invoices:invoices.map(r=>Object.assign({},r,{amount:Number(r.amount||0)})),
      upcoming:upcoming.map(r=>({description:r.description,amount:Number(r.amount||0),due_date:r.due_date})),
      financings:financings.map(r=>Object.assign({},r,{installment_amount:Number(r.installment_amount||0)})),
      recommendations
    };
  });
}

function buildInsights(s){
  const insights=[];
  if(s.projected<0) insights.push({kind:'warning',title:'Projeção negativa',text:'Mantido o cenário do mês, a projeção está em '+brMoney(s.projected)+'.'});
  else insights.push({kind:'ok',title:'Projeção do mês',text:'A projeção atual está em '+brMoney(s.projected)+'.'});
  if(s.overdue_count>0) insights.push({kind:'warning',title:'Contas atrasadas',text:s.overdue_count+' lançamento(s) em atraso, somando '+brMoney(s.overdue_total)+'.'});
  if(s.card_invoices>0) insights.push({kind:'info',title:'Cartões',text:'As faturas do mês somam '+brMoney(s.card_invoices)+'.'});
  if(s.categories[0]) insights.push({kind:'info',title:'Maior categoria',text:s.categories[0].category+' concentra '+brMoney(s.categories[0].total)+' em despesas do mês.'});
  if(s.upcoming.length) insights.push({kind:'info',title:'Próximos 7 dias',text:s.upcoming.length+' despesa(s) pendente(s) vencem nos próximos 7 dias.'});
  return insights.slice(0,5);
}

function findCardMention(text,s){
  const n=normalize(text);
  const cards=[...new Set(s.invoices.map(x=>x.card_name))];
  return cards.find(name=>{
    const k=normalize(name);
    return k&&n.includes(k);
  })||'';
}
function findCategoryMention(text,s){
  const n=normalize(text);
  return s.categories.find(x=>{
    const k=normalize(x.category);
    return k&&n.includes(k);
  })||null;
}
function transactionMatches(text,s){
  const q=words(text);
  if(!q.length)return [];
  return s.transactions.map(r=>{
    const hay=normalize([r.description,r.category,r.account_name].join(' '));
    let score=0;
    for(const w of q)if(hay.includes(w))score+=w.length>=6?2:1;
    return {row:r,score};
  }).filter(x=>x.score>0).sort((a,b)=>b.score-a.score||b.row.amount-a.row.amount);
}
function listInvoices(rows){
  if(!rows.length)return 'Não encontrei faturas com esse filtro no mês selecionado.';
  const total=rows.reduce((a,x)=>a+Number(x.amount||0),0);
  return rows.map(x=>'• '+x.card_name+' — '+brMoney(x.amount)+' · vence '+brDate(x.due_date)+(x.status==='paid'?' · paga':' · pendente')).join('\n')+'\nTotal: '+brMoney(total)+'.';
}
function listTransactions(rows,max=8){
  if(!rows.length)return 'Não encontrei lançamentos com esse filtro.';
  const shown=rows.slice(0,max);
  const total=rows.reduce((a,x)=>a+Number(x.amount||0),0);
  let out=shown.map(x=>'• '+x.description+' — '+brMoney(x.amount)+' · '+brDate(x.due_date)+' · '+x.category).join('\n');
  if(rows.length>shown.length)out+='\n… e mais '+(rows.length-shown.length)+' lançamento(s).';
  return out+'\nTotal: '+brMoney(total)+'.';
}
function naturalFallback(q,s,history){
  const matches=transactionMatches(q,s);
  if(matches.length){
    const topScore=matches[0].score;
    const rows=matches.filter(x=>x.score>=Math.max(1,topScore-1)).slice(0,12).map(x=>x.row);
    return 'Encontrei lançamentos que parecem relacionados ao que você perguntou:\n'+listTransactions(rows,6);
  }
  const last=history.length?history[history.length-1].text:'';
  if(last)return 'Entendi que isso é uma continuação da conversa, mas ainda não consegui ligar a pergunta a um dado específico. Se quiser, diga algo como “quais faturas estão pendentes?”, “o que vence amanhã?” ou cite o nome do cartão/lançamento.';
  return 'Pode perguntar do seu jeito. Eu consigo procurar por cartões, faturas, despesas, receitas, categorias, vencimentos, financiamentos e padrões aprendidos. Se eu não tiver informação suficiente, vou pedir só o detalhe que estiver faltando.';
}
function answerQuestion(q,s,historyRaw){
  const history=safeHistory(historyRaw), n=normalize(q);
  if(!n.trim())return 'Pode mandar a pergunta do jeito que você falaria normalmente.';
  if(/^(oi|ola|bom dia|boa tarde|boa noite|e ai|opa)\b/.test(n))
    return 'Oi! Pode perguntar do seu jeito. Eu consigo cruzar seus lançamentos, cartões, vencimentos e padrões que o GranaOk já aprendeu.';
  if(/obrigad|valeu|show|perfeito/.test(n)&&n.split(' ').length<=5)
    return 'Por nada. Se quiser, posso continuar desse ponto e detalhar os valores, vencimentos ou padrões que apareceram.';

  const h=safeHistory(history);
  const combined=historyText(h);
  const topic=explicitTopic(n)||topicFromHistory(h);
  const status=statusFromText(n)||statusFromText(normalize(combined));
  const card=findCardMention(q,s)||findCardMention(combined,s);
  const category=findCategoryMention(q,s);

  if(card){
    let rows=s.invoices.filter(x=>normalize(x.card_name)===normalize(card));
    if(status==='pending')rows=rows.filter(x=>x.status!=='paid');
    if(status==='paid')rows=rows.filter(x=>x.status==='paid');
    if(rows.length){
      return 'Sobre o '+card+':\n'+listInvoices(rows);
    }
  }

  if(topic==='cards'){
    let rows=s.invoices.slice();
    if(status==='pending'||/qual.*pend|quais.*pend|falta.*pagar|para pagar|a pagar/.test(n))rows=rows.filter(x=>x.status!=='paid');
    if(status==='paid')rows=rows.filter(x=>x.status==='paid');
    if(wantsList(n)||status){
      const prefix=rows.length===1?'A fatura que encontrei é:':'Estas são as faturas que encontrei:';
      return prefix+'\n'+listInvoices(rows);
    }
    return 'As faturas do mês somam '+brMoney(s.card_invoices)+'. Desse valor, '+brMoney(s.card_paid)+' está pago e '+brMoney(s.card_pending)+' está pendente.';
  }

  if(/amanha/.test(n)){
    const d=new Date();d.setDate(d.getDate()+1);const key=d.toISOString().slice(0,10);
    const tx=s.transactions.filter(x=>x.type==='expense'&&x.status!=='paid'&&x.due_date===key);
    const iv=s.invoices.filter(x=>x.status!=='paid'&&x.due_date===key);
    if(!tx.length&&!iv.length)return 'Não encontrei despesas nem faturas pendentes vencendo amanhã.';
    let out='Para amanhã encontrei:';
    if(tx.length)out+='\n'+listTransactions(tx,8);
    if(iv.length)out+='\nFaturas:\n'+listInvoices(iv);
    return out;
  }

  if(/hoje/.test(n)&&(/vence|vencimento|pagar|pendente/.test(n))){
    const key=new Date().toISOString().slice(0,10);
    const tx=s.transactions.filter(x=>x.type==='expense'&&x.status!=='paid'&&x.due_date===key);
    const iv=s.invoices.filter(x=>x.status!=='paid'&&x.due_date===key);
    if(!tx.length&&!iv.length)return 'Não encontrei despesas nem faturas pendentes vencendo hoje.';
    let out='Para hoje encontrei:';
    if(tx.length)out+='\n'+listTransactions(tx,8);
    if(iv.length)out+='\nFaturas:\n'+listInvoices(iv);
    return out;
  }

  if(/proxim|7 dias|semana|venc/.test(n)&&topic!=='cards'){
    const tx=s.upcoming||[];
    const iv=s.invoices.filter(x=>x.status!=='paid'&&x.due_date>=new Date().toISOString().slice(0,10));
    if(!tx.length&&!iv.length)return 'Não identifiquei despesas ou faturas pendentes próximas.';
    let out='Nos próximos dias encontrei:';
    if(tx.length)out+='\n'+tx.slice(0,8).map(x=>'• '+brDate(x.due_date)+' — '+x.description+': '+brMoney(x.amount)).join('\n');
    if(iv.length)out+='\nFaturas:\n'+iv.slice(0,6).map(x=>'• '+x.card_name+' — '+brMoney(x.amount)+' · '+brDate(x.due_date)).join('\n');
    return out;
  }

  if(category){
    const rows=s.transactions.filter(x=>x.type==='expense'&&normalize(x.category)===normalize(category.category));
    const total=rows.reduce((a,x)=>a+x.amount,0);
    const pct=s.expenses>0?total/s.expenses*100:0;
    return 'Em '+category.category+', encontrei '+brMoney(total)+' em despesas no mês, cerca de '+pct.toLocaleString('pt-BR',{maximumFractionDigits:1})+'% das despesas lançadas.\n'+listTransactions(rows,5);
  }

  if(/onde.*gast|maior.*gast|mais.*gast|categoria/.test(n))
    return 'As maiores categorias de despesas do mês são:\n'+joinTop(s.categories);

  if(/recomend|sugest|melhorar|economizar|organizar|planej|o que acha/.test(n)){
    if(s.recommendations.length){
      return 'Com base no seu padrão, eu priorizaria isto:\n'+s.recommendations.slice(0,4).map((x,i)=>(i+1)+'. '+x.title+' — '+x.message+(x.source_name?' ['+x.source_name+']':'')).join('\n');
    }
    return 'Ainda não tenho recomendações aprendidas suficientes. Entre no Motor de Conhecimento e rode o aprendizado; depois consigo relacionar suas perguntas aos padrões históricos.';
  }

  if(topic==='financing'){
    if(!s.financings.length)return 'Não encontrei financiamentos ativos.';
    return 'Financiamentos ativos:\n'+s.financings.map(x=>'• '+x.name+' — parcela '+brMoney(x.installment_amount)+' · '+x.paid_installments+'/'+x.total_installments+' pagas'+(x.next_due_date?' · próxima '+brDate(x.next_due_date):'')).join('\n');
  }

  if(/atras|vencid/.test(n)){
    const rows=s.transactions.filter(x=>x.type==='expense'&&x.status!=='paid'&&x.due_date<new Date().toISOString().slice(0,10));
    return rows.length?'Encontrei estes lançamentos atrasados:\n'+listTransactions(rows,8):'Não identifiquei lançamentos de despesa em atraso agora.';
  }

  if(/(quanto|total).*(gastei|gasto|despesa)|despesas? do mes/.test(n))
    return 'No mês selecionado, as despesas lançadas somam '+brMoney(s.expenses)+'. As faturas dos cartões somam '+brMoney(s.card_invoices)+'. Juntas, representam '+brMoney(s.total_monthly_expenses)+'.';

  if(topic==='income')
    return 'As entradas do mês somam '+brMoney(s.income)+'.';

  if(topic==='projection')
    return 'O saldo atual das contas é '+brMoney(s.accounts_balance)+' e a projeção do mês está em '+brMoney(s.projected)+'.';

  if(topic==='transactions'&&status==='pending'){
    const rows=s.transactions.filter(x=>x.type==='expense'&&x.status!=='paid');
    return rows.length?'Estas despesas estão pendentes no mês:\n'+listTransactions(rows,8):'Não encontrei despesas pendentes no mês.';
  }

  return naturalFallback(q,s,h);
}

async function assistantSummary(month,user){
  const s=await snapshot(month,user);
  return {snapshot:s,insights:buildInsights(s),mode:'local-contextual',privacy:'Os dados são analisados pela API privada do GranaOk e não são enviados a um serviço externo de IA.'};
}
async function assistantAsk(question,month,user,history){
  const s=await snapshot(month,user);
  return {answer:answerQuestion(question,s,history),snapshot:s,mode:'local-contextual'};
}

module.exports={assistantSummary,assistantAsk};
