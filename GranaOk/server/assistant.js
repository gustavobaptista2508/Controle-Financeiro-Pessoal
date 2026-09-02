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
function normalize(s){
  return String(s||'').normalize('NFD').replace(/[\u0300-\u036f]/g,'').toLowerCase();
}
function joinTop(rows){
  if(!rows.length)return 'Ainda não há despesas categorizadas nesse mês.';
  return rows.slice(0,5).map((r,i)=>(i+1)+'. '+r.category+': '+brMoney(r.total)).join('\n');
}

async function snapshot(month,user){
  const m=monthOk(month), start=m+'-01', next=addMonth(m)+'-01';
  return withConn(async conn=>{
    const p=await ensureSchema(conn);
    const personFilter=user&&user.person_id ? ' AND (t.person_id=? OR t.person_id IS NULL)' : '';
    const params=user&&user.person_id ? [start,next,user.person_id] : [start,next];

    const [[bal]]=await conn.query('SELECT COALESCE(SUM(current_balance),0) v FROM '+p+'accounts WHERE active=1');
    const [[tx]]=await conn.execute(
      "SELECT COALESCE(SUM(CASE WHEN type='income' THEN amount ELSE 0 END),0) income,"+
      "COALESCE(SUM(CASE WHEN type='expense' THEN amount ELSE 0 END),0) expenses,"+
      "COALESCE(SUM(CASE WHEN type='expense' AND status='paid' THEN amount ELSE 0 END),0) paid_expenses,"+
      "COALESCE(SUM(CASE WHEN type='expense' AND status<>'paid' THEN amount ELSE 0 END),0) pending_expenses "+
      "FROM "+p+"transactions t WHERE due_date>=? AND due_date<?"+personFilter, params
    );

    const [[iv]]=await conn.execute(
      "SELECT COALESCE(SUM(amount),0) total,COALESCE(SUM(CASE WHEN status='paid' THEN amount ELSE 0 END),0) paid,"+
      "COALESCE(SUM(CASE WHEN status<>'paid' THEN amount ELSE 0 END),0) pending FROM "+p+"card_invoices "+
      "WHERE reference_month>=? AND reference_month<?",
      [start,next]
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
      "WHERE t.type='expense' AND t.due_date>=? AND t.due_date<?"+
      personFilter+
      " GROUP BY COALESCE(c.name,'Outros') ORDER BY total DESC LIMIT 8",
      catParams
    );

    const upcomingParams=user&&user.person_id ? [user.person_id] : [];
    const [upcoming]=await conn.execute(
      "SELECT description,amount,DATE_FORMAT(due_date,'%Y-%m-%d') due_date FROM "+p+"transactions t "+
      "WHERE type='expense' AND status<>'paid' AND due_date>=CURDATE() AND due_date<=DATE_ADD(CURDATE(),INTERVAL 7 DAY)"+
      (user&&user.person_id ? " AND (t.person_id=? OR t.person_id IS NULL)" : '')+
      " ORDER BY due_date,amount DESC LIMIT 8",
      upcomingParams
    );

    const [[fin]]=await conn.query('SELECT COALESCE(SUM(installment_amount),0) monthly FROM '+p+'financings WHERE active=1');
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
      financing_monthly:Number(fin.monthly||0),
      categories:cats.map(r=>({category:r.category,total:Number(r.total||0)})),
      upcoming:upcoming.map(r=>({description:r.description,amount:Number(r.amount||0),due_date:r.due_date}))
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

function answerQuestion(q,s){
  const n=normalize(q);
  if(!n.trim()) return 'Escreva uma pergunta sobre seus dados financeiros.';
  if(/(quanto|total).*(gastei|gasto|despesa)|despesas? do mes/.test(n))
    return 'No mês selecionado, as despesas lançadas somam '+brMoney(s.expenses)+'. As faturas dos cartões somam '+brMoney(s.card_invoices)+'. Total considerado no mês: '+brMoney(s.total_monthly_expenses)+'.';
  if(/receita|entrada|recebi|ganhei/.test(n))
    return 'As entradas do mês somam '+brMoney(s.income)+'.';
  if(/saldo|projecao|projetado|vai sobrar|sobra/.test(n))
    return 'O saldo atual das contas é '+brMoney(s.accounts_balance)+' e a projeção do mês está em '+brMoney(s.projected)+'.';
  if(/categoria|onde.*gast|maior.*gast|mais.*gast/.test(n))
    return 'Maiores categorias de despesas no mês:\n'+joinTop(s.categories);
  if(/atras|vencid/.test(n))
    return s.overdue_count ? 'Há '+s.overdue_count+' lançamento(s) em atraso, totalizando '+brMoney(s.overdue_total)+'.' : 'Não identifiquei lançamentos de despesa em atraso agora.';
  if(/cartao|cartoes|fatura/.test(n))
    return 'As faturas do mês somam '+brMoney(s.card_invoices)+'. Desse valor, '+brMoney(s.card_paid)+' está pago e '+brMoney(s.card_pending)+' está pendente.';
  if(/financiamento|parcela/.test(n))
    return 'Os financiamentos ativos representam '+brMoney(s.financing_monthly)+' por mês.';
  if(/proxim|7 dias|semana|venc/.test(n)){
    if(!s.upcoming.length)return 'Não identifiquei despesas pendentes vencendo nos próximos 7 dias.';
    return 'Próximos vencimentos:\n'+s.upcoming.slice(0,5).map(x=>'• '+x.due_date+' — '+x.description+': '+brMoney(x.amount)).join('\n');
  }
  return 'Resumo do mês: entradas '+brMoney(s.income)+', despesas '+brMoney(s.expenses)+', faturas '+brMoney(s.card_invoices)+' e projeção '+brMoney(s.projected)+'. Você pode perguntar por categorias, atrasos, cartões, próximos vencimentos ou financiamentos.';
}

async function assistantSummary(month,user){
  const s=await snapshot(month,user);
  return {snapshot:s,insights:buildInsights(s),mode:'local',privacy:'Os dados são analisados localmente pela API do GranaOk e não são enviados a um serviço externo de IA.'};
}
async function assistantAsk(question,month,user){
  const s=await snapshot(month,user);
  return {answer:answerQuestion(question,s),snapshot:s,mode:'local'};
}

module.exports={assistantSummary,assistantAsk};
