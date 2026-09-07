const crypto = require('crypto');
const { withConn, ensureSchema } = require('./db');
const { assistantSummary, assistantAsk } = require('./assistant');
const { buildInvestmentRadar } = require('./investments');
const { rebuildKnowledge, getKnowledgeSummary, saveFeedback } = require('./knowledge_engine');

function monthOk(v){return /^\d{4}-(0[1-9]|1[0-2])$/.test(String(v||''))?String(v):new Date().toISOString().slice(0,7)}
function dateOk(v){if(!/^\d{4}-\d{2}-\d{2}$/.test(String(v||'')))throw new Error('Data inválida.');return String(v)}
function money(v){let s=String(v==null?'0':v).trim();if(s.includes(','))s=s.replace(/\./g,'').replace(',','.');const n=Math.abs(Number(s));return Number.isFinite(n)?Math.round(n*100)/100:0}
function signedMoney(v){let s=String(v==null?'0':v).trim();if(s.includes(','))s=s.replace(/\./g,'').replace(',','.');const n=Number(s);if(!Number.isFinite(n))throw new Error('Valor inválido.');return Math.round(n*100)/100}
function addMonths(dateStr,n){const d=new Date(dateStr+'T12:00:00');d.setMonth(d.getMonth()+n);return d.toISOString().slice(0,10)}
function invoiceDue(month,dueDay){const d=new Date(month+'-01T12:00:00');d.setDate(Math.min(Math.max(1,Number(dueDay||10)),new Date(d.getFullYear(),d.getMonth()+1,0).getDate()));return d.toISOString().slice(0,10)}
async function categoryId(conn,p,name,kind){name=String(name||'Outros').trim()||'Outros';await conn.execute('INSERT IGNORE INTO '+p+'categories(name,kind,active) VALUES(?,?,1)',[name,kind]);const [r]=await conn.execute('SELECT id FROM '+p+'categories WHERE name=? AND kind=? LIMIT 1',[name,kind]);return r[0]?Number(r[0].id):null}
function signedDelta(type,amount){return (type==='income'?1:-1)*Number(amount||0)}
async function applyAccountDelta(conn,p,accountId,delta,user,sourceType,sourceId,eventDate,note){
  accountId=Number(accountId||0);delta=Number(delta||0);
  if(!accountId||!Number.isFinite(delta)||delta===0)return null;
  const [rows]=await conn.execute('SELECT id,current_balance FROM '+p+'accounts WHERE id=? AND active=1 FOR UPDATE',[accountId]);
  if(!rows[0])throw new Error('Conta não encontrada.');
  const next=Math.round((Number(rows[0].current_balance||0)+delta)*100)/100;
  await conn.execute('UPDATE '+p+'accounts SET current_balance=? WHERE id=?',[next,accountId]);
  await conn.execute(
    'INSERT INTO '+p+'account_balance_events(account_id,user_id,source_type,source_id,delta,balance_after,event_date,note) VALUES(?,?,?,?,?,?,?,?)',
    [accountId,user&&user.id?Number(user.id):null,String(sourceType||'manual'),sourceId?Number(sourceId):null,delta,next,eventDate||null,String(note||'').slice(0,255)||null]
  );
  return next;
}
async function setAccountBalance(conn,p,accountId,newBalance,user,note){
  accountId=Number(accountId||0);newBalance=Number(newBalance);
  if(!accountId||!Number.isFinite(newBalance))throw new Error('Saldo inválido.');
  const [rows]=await conn.execute('SELECT current_balance FROM '+p+'accounts WHERE id=? AND active=1 FOR UPDATE',[accountId]);
  if(!rows[0])throw new Error('Conta não encontrada.');
  const old=Number(rows[0].current_balance||0),next=Math.round(newBalance*100)/100,delta=Math.round((next-old)*100)/100;
  await conn.execute('UPDATE '+p+'accounts SET current_balance=? WHERE id=?',[next,accountId]);
  await conn.execute(
    'INSERT INTO '+p+'account_balance_events(account_id,user_id,source_type,delta,balance_after,event_date,note) VALUES(?,?,\'manual_adjustment\',?,?,CURDATE(),?)',
    [accountId,user&&user.id?Number(user.id):null,delta,next,String(note||'Ajuste manual de saldo').slice(0,255)]
  );
  return {old_balance:old,current_balance:next,delta};
}
async function syncInvoice(conn,p,cardId,month,dueDate){const [sum]=await conn.execute("SELECT COALESCE(SUM(amount),0) total FROM "+p+"card_purchases WHERE card_id=? AND DATE_FORMAT(due_date,'%Y-%m')=?",[cardId,month]);const total=Number(sum[0].total||0);const [rows]=await conn.execute("SELECT id FROM "+p+"card_invoices WHERE card_id=? AND DATE_FORMAT(reference_month,'%Y-%m')=? ORDER BY id LIMIT 1",[cardId,month]);if(rows[0])await conn.execute('UPDATE '+p+'card_invoices SET amount=?,due_date=? WHERE id=?',[total,dueDate,rows[0].id]);else await conn.execute("INSERT INTO "+p+"card_invoices(card_id,reference_month,due_date,amount,status) VALUES(?,?,?,?, 'open')",[cardId,month+'-01',dueDate,total])}

const aliases={transactions:'transactions:list',transaction_save:'transaction:save',transaction_status:'transaction:status',account_save:'account:save',person_add:'person:add',category_add:'category:add',card_save:'card:save',card_purchase_add:'card:purchase',invoice:'invoice:get',invoice_pay:'invoice:pay',invoice_reopen:'invoice:reopen',financings:'financings:list',financing_pay:'financing:pay',assistant_summary:'assistant:summary',assistant_ask:'assistant:ask',investment_radar:'investments:radar',knowledge_rebuild:'knowledge:rebuild',knowledge_summary:'knowledge:summary',knowledge_feedback:'knowledge:feedback',account_balance_set:'account:balance:set'};
const writeActions=new Set(['transaction:save','transaction:status','account:save','person:add','category:add','card:save','card:purchase','invoice:pay','invoice:reopen','financing:pay','account:balance:set']);

async function runAction(name,a,user,context){
  name=aliases[name]||name;
  context=Object.assign({},context||{});
  const clientSource=context.client==='android'?'android':'web';
  if(writeActions.has(name)&&user&&user.role==='readonly')throw new Error('Seu usuário é somente leitura.');
  a=Object.assign({},a||{});
  if(name==='assistant:summary') return assistantSummary(a.month,user);
  if(name==='assistant:ask') return assistantAsk(a.question,a.month,user,a.history);
  if(name==='investments:radar') return buildInvestmentRadar();
  if(name==='knowledge:rebuild') return rebuildKnowledge(user,a.months||12);
  if(name==='knowledge:summary') return getKnowledgeSummary(user);
  if(name==='knowledge:feedback') return saveFeedback(user,a.recommendation_id,a.feedback,a.comment);
  if(user&&user.person_id&&!Number(a.person_id||0))a.person_id=user.person_id;

  return withConn(async conn=>{
    const p=await ensureSchema(conn);

    if(name==='account:balance:set'){
      return setAccountBalance(conn,p,Number(a.account_id||0),signedMoney(a.current_balance),user,a.note||'Ajuste manual pelo GranaOk Web');
    }

    if(name==='dashboard'){
      const m=monthOk(a.month),start=m+'-01',next=addMonths(start,1);
      const [[bal]]=await conn.query('SELECT COALESCE(SUM(current_balance),0) v FROM '+p+'accounts WHERE active=1');
      const [[tx]]=await conn.execute("SELECT COALESCE(SUM(CASE WHEN type='income' THEN amount ELSE 0 END),0) income,COALESCE(SUM(CASE WHEN type='expense' THEN amount ELSE 0 END),0) expenses,COALESCE(SUM(CASE WHEN type='expense' AND status='paid' THEN amount ELSE 0 END),0) paid_expenses,COALESCE(SUM(CASE WHEN type='expense' AND status<>'paid' THEN amount ELSE 0 END),0) pending_expenses FROM "+p+"transactions WHERE due_date>=? AND due_date<?",[start,next]);
      const [[iv]]=await conn.execute("SELECT COALESCE(SUM(amount),0) total,COALESCE(SUM(CASE WHEN status='paid' THEN amount ELSE 0 END),0) paid,COALESCE(SUM(CASE WHEN status<>'paid' THEN amount ELSE 0 END),0) pending FROM "+p+"card_invoices WHERE reference_month>=? AND reference_month<?",[start,next]);
      const [[ov]]=await conn.query("SELECT COUNT(*) c FROM "+p+"transactions WHERE type='expense' AND status<>'paid' AND due_date<CURDATE()");
      const [[fin]]=await conn.query('SELECT COALESCE(SUM(installment_amount),0) v FROM '+p+'financings WHERE active=1');
      const total=Number(tx.expenses||0)+Number(iv.total||0);
      return {month:m,accounts_balance:Number(bal.v||0),income:Number(tx.income||0),expenses:Number(tx.expenses||0),paid_expenses:Number(tx.paid_expenses||0),pending_expenses:Number(tx.pending_expenses||0),card_invoices:Number(iv.total||0),card_paid:Number(iv.paid||0),card_pending:Number(iv.pending||0),total_monthly_expenses:total,overdue_count:Number(ov.c||0),financing_monthly:Number(fin.v||0),projected:Number(bal.v||0)+Number(tx.income||0)-total};
    }

    if(name==='context'){
      const [people]=await conn.query("SELECT id,name,COALESCE(entity_kind,'person') kind,COALESCE(partner_name,'') partner_name,active FROM "+p+"people ORDER BY active DESC,name");
      const [accounts]=await conn.query("SELECT a.id,a.person_id,a.name,a.type,a.initial_balance,a.current_balance,a.active,COALESCE(a.bank_code,'other') bank_code,COALESCE(pe.name,'') person_name FROM "+p+"accounts a LEFT JOIN "+p+"people pe ON pe.id=a.person_id ORDER BY a.active DESC,a.name");
      const [categories]=await conn.query('SELECT id,name,kind,active FROM '+p+'categories ORDER BY kind,name');
      const [cards]=await conn.query("SELECT c.id,c.person_id,c.name,c.closing_day,c.due_day,c.limit_amount,c.active,COALESCE(pe.name,'') person_name FROM "+p+"cards c LEFT JOIN "+p+"people pe ON pe.id=c.person_id ORDER BY c.active DESC,c.name");
      return {people,accounts,categories,cards};
    }

    if(name==='transactions:list'){
      const m=monthOk(a.month),monthStart=m+'-01',monthNext=addMonths(monthStart,1),type=['income','expense'].includes(a.type)?a.type:'',status=['paid','pending','overdue'].includes(a.status)?a.status:'',description=String(a.search||'').trim(),observation=String(a.observation||'').trim(),source=String(a.source||'').trim(),accountId=Number(a.account_id||0),personId=Number(a.person_id||0),categoryId=Number(a.category_id||0);
      const minAmount=(a.min_amount===null||a.min_amount===undefined||String(a.min_amount).trim()==='')?null:money(a.min_amount),maxAmount=(a.max_amount===null||a.max_amount===undefined||String(a.max_amount).trim()==='')?null:money(a.max_amount),dateFrom=/^\d{4}-\d{2}-\d{2}$/.test(String(a.date_from||''))?String(a.date_from):'',dateTo=/^\d{4}-\d{2}-\d{2}$/.test(String(a.date_to||''))?String(a.date_to):'',installments=['cash','installment'].includes(a.installments)?a.installments:'';
      const orderMap={date_asc:'t.due_date ASC,t.id ASC',date_desc:'t.due_date DESC,t.id DESC',amount_desc:'t.amount DESC,t.due_date ASC',amount_asc:'t.amount ASC,t.due_date ASC',description_asc:'t.description ASC,t.due_date ASC'},orderBy=orderMap[a.order]||orderMap.date_asc,effective="CASE WHEN t.status='paid' THEN 'paid' WHEN t.status='overdue' THEN 'overdue' WHEN t.status='pending' AND t.due_date<CURDATE() THEN 'overdue' ELSE t.status END";
      let sql="SELECT t.id,t.person_id,t.account_id,t.category_id,t.type,t.description,t.amount,DATE_FORMAT(t.due_date,'%Y-%m-%d') due_date,CASE WHEN t.paid_date IS NULL THEN NULL ELSE DATE_FORMAT(t.paid_date,'%Y-%m-%d') END paid_date,t.status,"+effective+" effective_status,COALESCE(c.name,'Outros') category,COALESCE(ac.name,'') account_name,COALESCE(pe.name,'') person_name,COALESCE(t.observations,'') observations,COALESCE(t.source,'') source,COALESCE(t.installment_number,1) installment_number,COALESCE(t.installment_total,1) installment_total FROM "+p+"transactions t LEFT JOIN "+p+"categories c ON c.id=t.category_id LEFT JOIN "+p+"accounts ac ON ac.id=t.account_id LEFT JOIN "+p+"people pe ON pe.id=t.person_id WHERE t.due_date>=? AND t.due_date<?";
      const params=[monthStart,monthNext];
      if(type){sql+=' AND t.type=?';params.push(type)}if(status){sql+=' AND '+effective+'=?';params.push(status)}if(description){sql+=' AND t.description LIKE ?';params.push('%'+description+'%')}if(observation){sql+=" AND COALESCE(t.observations,'') LIKE ?";params.push('%'+observation+'%')}if(source){sql+=" AND COALESCE(t.source,'') LIKE ?";params.push('%'+source+'%')}if(accountId){sql+=' AND t.account_id=?';params.push(accountId)}if(personId){sql+=' AND t.person_id=?';params.push(personId)}if(categoryId){sql+=' AND t.category_id=?';params.push(categoryId)}if(minAmount!==null){sql+=' AND t.amount>=?';params.push(minAmount)}if(maxAmount!==null){sql+=' AND t.amount<=?';params.push(maxAmount)}if(dateFrom){sql+=' AND t.due_date>=?';params.push(dateFrom)}if(dateTo){sql+=' AND t.due_date<=?';params.push(dateTo)}if(installments==='cash')sql+=' AND COALESCE(t.installment_total,1)<=1';if(installments==='installment')sql+=' AND COALESCE(t.installment_total,1)>1';sql+=' ORDER BY '+orderBy;
      const [rows]=await conn.execute(sql,params);return {rows,month:m};
    }

    if(name==='transaction:save'){
      const id=Number(a.id||0),type=a.type==='income'?'income':'expense',desc=String(a.description||'').trim(),amt=money(a.amount),due=dateOk(a.due_date),status=['paid','pending','overdue'].includes(a.status)?a.status:'pending',obs=String(a.observations||''),person=Number(a.person_id||0)||null,account=Number(a.account_id||0)||null,cat=await categoryId(conn,p,a.category||'Outros',type);if(!desc||amt<=0)throw new Error('Informe descrição e valor.');
      await conn.beginTransaction();
      try{
        if(id){
          const [oldRows]=await conn.execute('SELECT id,account_id,type,amount,status,COALESCE(balance_applied,0) balance_applied FROM '+p+'transactions WHERE id=? FOR UPDATE',[id]);
          if(!oldRows[0])throw new Error('Lançamento não encontrado.');
          const old=oldRows[0];
          if(Number(old.balance_applied)===1&&old.account_id){
            await applyAccountDelta(conn,p,old.account_id,-signedDelta(old.type,old.amount),user,'transaction_reverse',id,due,'Reversão por edição de lançamento');
          }
          const paidDate=status==='paid'?(a.paid_date||new Date().toISOString().slice(0,10)):null;
          let applied=0;
          await conn.execute('UPDATE '+p+'transactions SET person_id=?,account_id=?,category_id=?,type=?,description=?,amount=?,due_date=?,paid_date=?,status=?,observations=?,balance_applied=0 WHERE id=?',[person,account,cat,type,desc,amt,due,paidDate,status,obs,id]);
          if(status==='paid'&&account){
            await applyAccountDelta(conn,p,account,signedDelta(type,amt),user,'transaction',id,paidDate||due,desc);
            applied=1;
            await conn.execute('UPDATE '+p+'transactions SET balance_applied=1 WHERE id=?',[id]);
          }
        }else{
          const paidDate=status==='paid'?new Date().toISOString().slice(0,10):null;
          const [ins]=await conn.execute("INSERT INTO "+p+"transactions(person_id,account_id,category_id,type,description,amount,due_date,paid_date,status,source,observations,balance_applied) VALUES(?,?,?,?,?,?,?,?,?,?,?,0)",[person,account,cat,type,desc,amt,due,paidDate,status,clientSource,obs]);
          const newId=Number(ins.insertId||0);
          if(status==='paid'&&account){
            await applyAccountDelta(conn,p,account,signedDelta(type,amt),user,'transaction',newId,paidDate||due,desc);
            await conn.execute('UPDATE '+p+'transactions SET balance_applied=1 WHERE id=?',[newId]);
          }
        }
        await conn.commit();
      }catch(e){await conn.rollback();throw e}
      return {message:id?'Lançamento atualizado.':'Lançamento criado.'};
    }

    if(name==='transaction:status'){
      const id=Number(a.id||0),newStatus=['paid','pending','overdue'].includes(a.status)?a.status:null;if(!id||!newStatus)throw new Error('Dados inválidos.');
      await conn.beginTransaction();
      try{
        const [rows]=await conn.execute('SELECT id,account_id,type,amount,status,COALESCE(balance_applied,0) balance_applied,description FROM '+p+'transactions WHERE id=? FOR UPDATE',[id]);
        if(!rows[0])throw new Error('Lançamento não encontrado.');
        const old=rows[0],paidDate=newStatus==='paid'?(a.paid_date||new Date().toISOString().slice(0,10)):null;
        if(old.status!=='paid'&&newStatus==='paid'&&old.account_id){
          await applyAccountDelta(conn,p,old.account_id,signedDelta(old.type,old.amount),user,'transaction',id,paidDate,old.description);
          await conn.execute('UPDATE '+p+'transactions SET status=?,paid_date=?,balance_applied=1 WHERE id=?',[newStatus,paidDate,id]);
        }else if(old.status==='paid'&&newStatus!=='paid'&&Number(old.balance_applied)===1&&old.account_id){
          await applyAccountDelta(conn,p,old.account_id,-signedDelta(old.type,old.amount),user,'transaction_reverse',id,new Date().toISOString().slice(0,10),'Reabertura: '+old.description);
          await conn.execute('UPDATE '+p+'transactions SET status=?,paid_date=NULL,balance_applied=0 WHERE id=?',[newStatus,id]);
        }else{
          await conn.execute('UPDATE '+p+'transactions SET status=?,paid_date=? WHERE id=?',[newStatus,paidDate,id]);
        }
        await conn.commit();
      }catch(e){await conn.rollback();throw e}
      return {};
    }

    if(name==='account:save'){const id=Number(a.id||0),n=String(a.name||'').trim();if(!n)throw new Error('Informe o nome da conta.');const vals=[Number(a.person_id||0)||null,n,String(a.type||'checking'),money(a.initial_balance),money(a.current_balance),String(a.bank_code||'other')];if(id)await conn.execute('UPDATE '+p+'accounts SET person_id=?,name=?,type=?,initial_balance=?,current_balance=?,bank_code=? WHERE id=?',vals.concat([id]));else await conn.execute('INSERT INTO '+p+'accounts(person_id,name,type,initial_balance,current_balance,active,bank_code) VALUES(?,?,?,?,?,1,?)',vals);return {message:'Conta salva.'}}

    if(name==='person:add'){const n=String(a.name||'').trim();if(!n)throw new Error('Informe o nome.');await conn.execute('INSERT INTO '+p+'people(name,active,entity_kind,partner_name) VALUES(?,1,?,?)',[n,a.kind==='couple'?'couple':'person',String(a.partner_name||'').trim()||null]);return {}}
    if(name==='category:add'){const n=String(a.name||'').trim();if(!n)throw new Error('Informe a categoria.');await conn.execute('INSERT IGNORE INTO '+p+'categories(name,kind,active) VALUES(?,?,1)',[n,a.kind==='income'?'income':'expense']);return {}}

    if(name==='card:save'){const n=String(a.name||'').trim();if(!n)throw new Error('Informe o cartão.');await conn.execute('INSERT INTO '+p+'cards(person_id,name,closing_day,due_day,limit_amount,active) VALUES(?,?,?,?,?,1)',[Number(a.person_id||0)||null,n,Math.max(1,Math.min(31,Number(a.closing_day||1))),Math.max(1,Math.min(31,Number(a.due_day||10))),money(a.limit_amount)]);return {}}

    if(name==='card:purchase'){
      const cardId=Number(a.card_id||0),desc=String(a.description||'').trim(),total=money(a.amount),purchase=dateOk(a.purchase_date),inst=Math.max(1,Math.min(60,Number(a.installments||1)));if(!cardId||!desc||total<=0)throw new Error('Confira cartão, descrição e valor.');
      const [[card]]=await conn.execute('SELECT closing_day,due_day,person_id FROM '+p+'cards WHERE id=? AND active=1',[cardId]);if(!card)throw new Error('Cartão não encontrado.');
      let first=new Date(purchase+'T12:00:00');const closing=Number(card.closing_day||1),due=Number(card.due_day||10);if(first.getDate()>closing)first.setMonth(first.getMonth()+1);if(due<=closing)first.setMonth(first.getMonth()+1);const firstMonth=first.toISOString().slice(0,7),person=Number(a.person_id||0)||Number(card.person_id||0)||null,cat=await categoryId(conn,p,a.category||'Outros','expense'),group=inst>1?crypto.randomUUID():null,base=Math.floor((total/inst)*100)/100;let allocated=0;
      await conn.beginTransaction();try{for(let i=1;i<=inst;i++){const amount=i===inst?Math.round((total-allocated)*100)/100:base;allocated=Math.round((allocated+amount)*100)/100;const mdate=addMonths(firstMonth+'-01',i-1).slice(0,7),dueDate=invoiceDue(mdate,due);await conn.execute('INSERT INTO '+p+'card_purchases(card_id,person_id,category_id,description,purchase_date,amount,invoice_month,due_date,installment_group,installment_number,installment_total,observations) VALUES(?,?,?,?,?,?,?,?,?,?,?,?)',[cardId,person,cat,desc,purchase,amount,mdate+'-01',dueDate,group,i,inst,String(a.observations||'')]);await syncInvoice(conn,p,cardId,mdate,dueDate)}await conn.commit()}catch(e){await conn.rollback();throw e}return {month:firstMonth,message:'Compra incluída na fatura.'};
    }

    if(name==='invoice:get'){
      const cardId=Number(a.card_id||0),m=monthOk(a.month);const [[card]]=await conn.execute('SELECT id,name,closing_day,due_day,limit_amount FROM '+p+'cards WHERE id=?',[cardId]);if(!card)throw new Error('Cartão não encontrado.');
      const [rows]=await conn.execute("SELECT cp.id,cp.description,DATE_FORMAT(cp.purchase_date,'%Y-%m-%d') purchase_date,cp.amount,cp.installment_number,cp.installment_total,COALESCE(c.name,'Outros') category,COALESCE(cp.observations,'') observations FROM "+p+"card_purchases cp LEFT JOIN "+p+"categories c ON c.id=cp.category_id WHERE cp.card_id=? AND DATE_FORMAT(cp.due_date,'%Y-%m')=? ORDER BY cp.purchase_date,cp.id",[cardId,m]);
      const total=rows.reduce((s,r)=>s+Number(r.amount||0),0),[ir]=await conn.execute("SELECT i.id,i.status,DATE_FORMAT(i.due_date,'%Y-%m-%d') due_date,CASE WHEN i.paid_date IS NULL THEN NULL ELSE DATE_FORMAT(i.paid_date,'%Y-%m-%d') END paid_date,i.paid_account_id,COALESCE(i.balance_applied,0) balance_applied,COALESCE(a.name,'') paid_account_name FROM "+p+"card_invoices i LEFT JOIN "+p+"accounts a ON a.id=i.paid_account_id WHERE i.card_id=? AND DATE_FORMAT(i.reference_month,'%Y-%m')=? ORDER BY i.id LIMIT 1",[cardId,m]),inv=ir[0]||{};return {card,rows,total,month:m,status:inv.status||'open',paid_date:inv.paid_date||null,due_date:inv.due_date||invoiceDue(m,card.due_day),paid_account_id:inv.paid_account_id||null,paid_account_name:inv.paid_account_name||'',balance_applied:Number(inv.balance_applied||0)};
    }

    if(name==='invoice:pay'||name==='invoice:reopen'){
      const cardId=Number(a.card_id||0),m=monthOk(a.month);if(!cardId)throw new Error('Cartão inválido.');
      await conn.beginTransaction();
      try{
        const [ir]=await conn.execute("SELECT id,amount,status,paid_account_id,COALESCE(balance_applied,0) balance_applied FROM "+p+"card_invoices WHERE card_id=? AND DATE_FORMAT(reference_month,'%Y-%m')=? LIMIT 1 FOR UPDATE",[cardId,m]);
        if(name==='invoice:reopen'){
          if(!ir[0])throw new Error('Fatura não encontrada.');
          const inv=ir[0];
          if(Number(inv.balance_applied)===1&&inv.paid_account_id){
            await applyAccountDelta(conn,p,inv.paid_account_id,Number(inv.amount||0),user,'card_invoice_reverse',inv.id,new Date().toISOString().slice(0,10),'Reabertura de fatura');
          }
          await conn.execute("UPDATE "+p+"card_invoices SET status='open',paid_date=NULL,paid_account_id=NULL,balance_applied=0 WHERE id=?",[inv.id]);
          await conn.commit();return {};
        }
        const paid=dateOk(a.paid_date||new Date().toISOString().slice(0,10)),accountId=Number(a.account_id||0)||null;
        const [[card]]=await conn.execute('SELECT due_day FROM '+p+'cards WHERE id=?',[cardId]);
        const [[sum]]=await conn.execute("SELECT COALESCE(SUM(amount),0) total FROM "+p+"card_purchases WHERE card_id=? AND DATE_FORMAT(due_date,'%Y-%m')=?",[cardId,m]);
        const dueDate=invoiceDue(m,card?card.due_day:10),total=Number(sum.total||0);
        let invoiceId=ir[0]?Number(ir[0].id):0;
        if(ir[0]&&ir[0].status==='paid'){
          if(Number(ir[0].balance_applied)===0&&accountId){
            const alreadyPaidAmount=Number(ir[0].amount||total||0);
            await applyAccountDelta(conn,p,accountId,-alreadyPaidAmount,user,'card_invoice',invoiceId,paid,'Aplicação retroativa do pagamento da fatura');
            await conn.execute('UPDATE '+p+'card_invoices SET paid_account_id=?,balance_applied=1 WHERE id=?',[accountId,invoiceId]);
            await conn.commit();
            return {message:'Pagamento aplicado ao saldo da conta.'};
          }
          await conn.rollback();return {message:'Fatura já estava paga.'};
        }
        if(ir[0]){
          await conn.execute("UPDATE "+p+"card_invoices SET amount=?,due_date=?,status='paid',paid_date=?,paid_account_id=?,balance_applied=0 WHERE id=?",[total,dueDate,paid,accountId,invoiceId]);
        }else{
          const [ins]=await conn.execute("INSERT INTO "+p+"card_invoices(card_id,reference_month,due_date,amount,status,paid_date,paid_account_id,balance_applied) VALUES(?,?,?,?, 'paid',?,?,0)",[cardId,m+'-01',dueDate,total,paid,accountId]);
          invoiceId=Number(ins.insertId||0);
        }
        if(accountId){
          await applyAccountDelta(conn,p,accountId,-total,user,'card_invoice',invoiceId,paid,'Pagamento de fatura');
          await conn.execute('UPDATE '+p+'card_invoices SET balance_applied=1 WHERE id=?',[invoiceId]);
        }
        await conn.commit();return {};
      }catch(e){try{await conn.rollback()}catch(_){ }throw e}
    }

    if(name==='financings:list'){const [rows]=await conn.query("SELECT id,name,total_amount,installment_amount,total_installments,paid_installments,active,CASE WHEN next_due_date IS NULL THEN NULL ELSE DATE_FORMAT(next_due_date,'%Y-%m-%d') END next_due_date,CASE WHEN last_paid_date IS NULL THEN NULL ELSE DATE_FORMAT(last_paid_date,'%Y-%m-%d') END last_paid_date FROM "+p+"financings ORDER BY active DESC,id DESC");return {rows}}

    if(name==='financing:pay'){
      const id=Number(a.id||0),paid=dateOk(a.paid_date||new Date().toISOString().slice(0,10));if(!id)throw new Error('Financiamento inválido.');
      await conn.beginTransaction();try{const [rr]=await conn.execute("SELECT id,installment_number FROM "+p+"financing_installments WHERE financing_id=? AND status<>'paid' ORDER BY installment_number LIMIT 1 FOR UPDATE",[id]);if(!rr[0])throw new Error('Não há parcela pendente.');await conn.execute("UPDATE "+p+"financing_installments SET status='paid',paid_date=? WHERE id=?",[paid,rr[0].id]);const [[cnt]]=await conn.execute("SELECT COUNT(*) c FROM "+p+"financing_installments WHERE financing_id=? AND status='paid'",[id]),[[f]]=await conn.execute('SELECT total_installments FROM '+p+'financings WHERE id=?',[id]),[nx]=await conn.execute("SELECT due_date FROM "+p+"financing_installments WHERE financing_id=? AND status<>'paid' ORDER BY installment_number LIMIT 1",[id]),paidN=Number(cnt.c||0),totalN=Number(f.total_installments||0);await conn.execute('UPDATE '+p+'financings SET paid_installments=?,last_paid_date=?,next_due_date=?,active=? WHERE id=?',[paidN,paid,nx[0]?nx[0].due_date:null,paidN<totalN?1:0,id]);await conn.commit()}catch(e){await conn.rollback();throw e}return {};
    }

    throw new Error('Operação desconhecida.');
  });
}

module.exports={runAction,writeActions};
