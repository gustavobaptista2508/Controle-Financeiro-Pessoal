<?php
declare(strict_types=1);
require __DIR__ . '/lib.php';

try {
    $action = $_GET['action'] ?? '';
    $in = input_json();

    if ($action === 'login') {
        $cfg = cfg();
        $password = (string)($in['password'] ?? '');
        $expected = (string)($cfg['web_password'] ?? '');
        if ($expected === '' || $expected === 'TROQUE_ESTA_SENHA') {
            respond(['ok'=>false,'error'=>'Defina uma senha segura em config.local.php antes de usar a versão web.'], 503);
        }
        if (!hash_equals($expected, $password)) respond(['ok'=>false,'error'=>'Senha inválida.'], 401);
        session_regenerate_id(true);
        $_SESSION['granaok_web_auth'] = true;
        respond(['ok'=>true,'message'=>'Acesso liberado.']);
    }

    if ($action === 'logout') {
        $_SESSION = [];
        if (session_status() === PHP_SESSION_ACTIVE) session_destroy();
        respond(['ok'=>true]);
    }

    if ($action === 'session') {
        respond(['ok'=>true,'authenticated'=>!empty($_SESSION['granaok_web_auth'])]);
    }

    require_auth();
    $db = db();
    ensure_web_columns($db);
    $p = prefix();

    if ($action === 'dashboard') {
        $month = safe_month($in['month'] ?? null);
        $next = (new DateTime($month.'-01'))->modify('+1 month')->format('Y-m-d');
        $start = $month . '-01';

        $accounts = (float)$db->query("SELECT COALESCE(SUM(current_balance),0) FROM {$p}accounts WHERE active=1")->fetchColumn();

        $st = $db->prepare("SELECT
            COALESCE(SUM(CASE WHEN type='income' THEN amount ELSE 0 END),0) income,
            COALESCE(SUM(CASE WHEN type='expense' THEN amount ELSE 0 END),0) expenses,
            COALESCE(SUM(CASE WHEN type='expense' AND status='paid' THEN amount ELSE 0 END),0) paid_expenses,
            COALESCE(SUM(CASE WHEN type='expense' AND status<>'paid' THEN amount ELSE 0 END),0) pending_expenses
            FROM {$p}transactions WHERE due_date>=? AND due_date<?");
        $st->execute([$start,$next]);
        $tx = $st->fetch() ?: [];

        $st = $db->prepare("SELECT COALESCE(SUM(amount),0) total,
            COALESCE(SUM(CASE WHEN status='paid' THEN amount ELSE 0 END),0) paid,
            COALESCE(SUM(CASE WHEN status<>'paid' THEN amount ELSE 0 END),0) pending
            FROM {$p}card_invoices WHERE reference_month>=? AND reference_month<?");
        $st->execute([$start,$next]);
        $inv = $st->fetch() ?: [];

        $overdue = (int)$db->query("SELECT COUNT(*) FROM {$p}transactions WHERE type='expense' AND status<>'paid' AND due_date<CURDATE()")->fetchColumn();
        $fin = 0.0;
        try { $fin = (float)$db->query("SELECT COALESCE(SUM(installment_amount),0) FROM {$p}financings WHERE active=1")->fetchColumn(); } catch (Throwable $e) {}

        respond([
            'ok'=>true,'month'=>$month,'accounts_balance'=>$accounts,
            'income'=>(float)($tx['income'] ?? 0),'expenses'=>(float)($tx['expenses'] ?? 0),
            'paid_expenses'=>(float)($tx['paid_expenses'] ?? 0),'pending_expenses'=>(float)($tx['pending_expenses'] ?? 0),
            'card_invoices'=>(float)($inv['total'] ?? 0),'card_paid'=>(float)($inv['paid'] ?? 0),'card_pending'=>(float)($inv['pending'] ?? 0),
            'financing_monthly'=>$fin,'overdue_count'=>$overdue,
            'projected'=>$accounts + (float)($tx['income'] ?? 0) - (float)($tx['expenses'] ?? 0) - (float)($inv['total'] ?? 0)
        ]);
    }

    if ($action === 'context') {
        $people = $db->query("SELECT id,name,COALESCE(entity_kind,'person') kind,COALESCE(partner_name,'') partner_name,active FROM {$p}people ORDER BY active DESC,name")->fetchAll();
        $accounts = $db->query("SELECT a.id,a.person_id,a.name,a.type,a.initial_balance,a.current_balance,a.active,COALESCE(a.bank_code,'other') bank_code,COALESCE(ppl.name,'') person_name FROM {$p}accounts a LEFT JOIN {$p}people ppl ON ppl.id=a.person_id ORDER BY a.active DESC,a.name")->fetchAll();
        $categories = $db->query("SELECT id,name,kind,active FROM {$p}categories ORDER BY kind,name")->fetchAll();
        $cards = $db->query("SELECT c.id,c.person_id,c.name,c.closing_day,c.due_day,c.limit_amount,c.active,COALESCE(ppl.name,'') person_name FROM {$p}cards c LEFT JOIN {$p}people ppl ON ppl.id=c.person_id ORDER BY c.active DESC,c.name")->fetchAll();
        respond(['ok'=>true,'people'=>$people,'accounts'=>$accounts,'categories'=>$categories,'cards'=>$cards]);
    }

    if ($action === 'transactions') {
        $month = safe_month($in['month'] ?? null);
        $start = $month.'-01';
        $next = (new DateTime($start))->modify('+1 month')->format('Y-m-d');
        $type = in_array(($in['type'] ?? ''), ['income','expense'], true) ? $in['type'] : '';
        $status = in_array(($in['status'] ?? ''), ['paid','pending','overdue'], true) ? $in['status'] : '';
        $search = trim((string)($in['search'] ?? ''));
        $effective = "CASE WHEN t.status='paid' THEN 'paid' WHEN t.status='overdue' THEN 'overdue' WHEN t.status='pending' AND t.due_date<CURDATE() THEN 'overdue' ELSE t.status END";
        $sql = "SELECT t.id,t.person_id,t.account_id,t.category_id,t.type,t.description,t.amount,
            DATE_FORMAT(t.due_date,'%Y-%m-%d') due_date,
            CASE WHEN t.paid_date IS NULL THEN NULL ELSE DATE_FORMAT(t.paid_date,'%Y-%m-%d') END paid_date,
            t.status,{$effective} effective_status,COALESCE(c.name,'Outros') category,
            COALESCE(a.name,'') account_name,COALESCE(ppl.name,'') person_name,
            COALESCE(t.observations,'') observations,COALESCE(t.installment_number,1) installment_number,
            COALESCE(t.installment_total,1) installment_total
            FROM {$p}transactions t
            LEFT JOIN {$p}categories c ON c.id=t.category_id
            LEFT JOIN {$p}accounts a ON a.id=t.account_id
            LEFT JOIN {$p}people ppl ON ppl.id=t.person_id
            WHERE t.due_date>=? AND t.due_date<?
              AND (?='' OR t.type=?)
              AND (?='' OR {$effective}=?)
              AND (?='' OR t.description LIKE ? OR COALESCE(t.observations,'') LIKE ?)
            ORDER BY t.due_date,t.id";
        $st = $db->prepare($sql);
        $like = '%'.$search.'%';
        $st->execute([$start,$next,$type,$type,$status,$status,$search,$like,$like]);
        respond(['ok'=>true,'month'=>$month,'rows'=>$st->fetchAll()]);
    }

    if ($action === 'transaction_save') {
        $id = (int)($in['id'] ?? 0);
        $type = ($in['type'] ?? '') === 'income' ? 'income' : 'expense';
        $description = trim((string)($in['description'] ?? ''));
        $amount = money($in['amount'] ?? 0);
        $due = safe_date($in['due_date'] ?? null);
        $status = in_array(($in['status'] ?? 'pending'), ['paid','pending','overdue'], true) ? $in['status'] : 'pending';
        $obs = trim((string)($in['observations'] ?? ''));
        $catId = category_id($db, trim((string)($in['category'] ?? 'Outros')), $type);
        $personId = (int)($in['person_id'] ?? 0) ?: null;
        $accountId = (int)($in['account_id'] ?? 0) ?: null;
        if ($description === '' || $amount <= 0) throw new InvalidArgumentException('Informe descrição e valor.');

        if ($id > 0) {
            $sql = "UPDATE {$p}transactions SET person_id=?,account_id=?,category_id=?,type=?,description=?,amount=?,due_date=?,status=?,paid_date=?,observations=? WHERE id=?";
            $st = $db->prepare($sql);
            $st->execute([$personId,$accountId,$catId,$type,$description,$amount,$due,$status,$status==='paid'?date('Y-m-d'):null,$obs,$id]);
        } else {
            $st = $db->prepare("INSERT INTO {$p}transactions(person_id,account_id,category_id,type,description,amount,due_date,paid_date,status,source,observations) VALUES(?,?,?,?,?,?,?,?,?,'web',?)");
            $st->execute([$personId,$accountId,$catId,$type,$description,$amount,$due,$status==='paid'?date('Y-m-d'):null,$status,$obs]);
        }
        respond(['ok'=>true,'message'=>$id>0?'Lançamento atualizado.':'Lançamento criado.']);
    }

    if ($action === 'transaction_status') {
        $id = (int)($in['id'] ?? 0);
        $status = in_array(($in['status'] ?? ''), ['paid','pending','overdue'], true) ? $in['status'] : '';
        if ($id<=0 || $status==='') throw new InvalidArgumentException('Dados inválidos.');
        $st = $db->prepare("UPDATE {$p}transactions SET status=?,paid_date=? WHERE id=?");
        $st->execute([$status,$status==='paid'?date('Y-m-d'):null,$id]);
        respond(['ok'=>true,'message'=>'Status atualizado.']);
    }

    if ($action === 'account_save') {
        $id = (int)($in['id'] ?? 0);
        $name = trim((string)($in['name'] ?? ''));
        $type = trim((string)($in['type'] ?? 'checking'));
        $bank = trim((string)($in['bank_code'] ?? 'other'));
        $current = money($in['current_balance'] ?? 0);
        $initial = money($in['initial_balance'] ?? $current);
        $personId = (int)($in['person_id'] ?? 0) ?: null;
        if ($name==='') throw new InvalidArgumentException('Informe o nome da conta.');
        if ($id>0) {
            $st=$db->prepare("UPDATE {$p}accounts SET person_id=?,name=?,type=?,initial_balance=?,current_balance=?,bank_code=? WHERE id=?");
            $st->execute([$personId,$name,$type,$initial,$current,$bank,$id]);
        } else {
            $st=$db->prepare("INSERT INTO {$p}accounts(person_id,name,type,initial_balance,current_balance,active,bank_code) VALUES(?,?,?,?,?,1,?)");
            $st->execute([$personId,$name,$type,$initial,$current,$bank]);
        }
        respond(['ok'=>true,'message'=>'Conta salva.']);
    }

    if ($action === 'person_add') {
        $name = trim((string)($in['name'] ?? ''));
        $kind = ($in['kind'] ?? '') === 'couple' ? 'couple' : 'person';
        $partner = trim((string)($in['partner_name'] ?? ''));
        if ($name==='') throw new InvalidArgumentException('Informe o nome.');
        $st=$db->prepare("INSERT INTO {$p}people(name,active,entity_kind,partner_name) VALUES(?,1,?,?)");
        $st->execute([$name,$kind,$partner!==''?$partner:null]);
        respond(['ok'=>true,'message'=>'Pessoa/casal cadastrado.']);
    }

    if ($action === 'category_add') {
        $name=trim((string)($in['name'] ?? ''));
        $kind=($in['kind'] ?? '')==='income'?'income':'expense';
        if($name==='') throw new InvalidArgumentException('Informe a categoria.');
        $st=$db->prepare("INSERT IGNORE INTO {$p}categories(name,kind,active) VALUES(?,?,1)");
        $st->execute([$name,$kind]);
        respond(['ok'=>true,'message'=>'Categoria cadastrada.']);
    }

    if ($action === 'cards') {
        $rows=$db->query("SELECT c.id,c.name,c.closing_day,c.due_day,c.limit_amount,c.active,COALESCE(ppl.name,'') person_name FROM {$p}cards c LEFT JOIN {$p}people ppl ON ppl.id=c.person_id ORDER BY c.active DESC,c.name")->fetchAll();
        respond(['ok'=>true,'rows'=>$rows]);
    }

    if ($action === 'card_save') {
        $name=trim((string)($in['name']??''));
        $closing=max(1,min(31,(int)($in['closing_day']??1)));
        $due=max(1,min(31,(int)($in['due_day']??10)));
        $limit=money($in['limit_amount']??0);
        $personId=(int)($in['person_id']??0)?:null;
        if($name==='') throw new InvalidArgumentException('Informe o nome do cartão.');
        $st=$db->prepare("INSERT INTO {$p}cards(person_id,name,closing_day,due_day,limit_amount,active) VALUES(?,?,?,?,?,1)");
        $st->execute([$personId,$name,$closing,$due,$limit]);
        respond(['ok'=>true,'message'=>'Cartão cadastrado.']);
    }

    if ($action === 'card_purchase_add') {
        $cardId=(int)($in['card_id']??0);
        $description=trim((string)($in['description']??''));
        $category=trim((string)($in['category']??'Outros')) ?: 'Outros';
        $purchaseDate=safe_date($in['purchase_date']??null);
        $total=money($in['amount']??0);
        $installments=max(1,min(60,(int)($in['installments']??1)));
        $personId=(int)($in['person_id']??0)?:null;
        $obs=trim((string)($in['observations']??''));
        if($cardId<=0||$description===''||$total<=0) throw new InvalidArgumentException('Confira cartão, descrição e valor.');
        $st=$db->prepare("SELECT closing_day,due_day,person_id FROM {$p}cards WHERE id=? AND active=1");
        $st->execute([$cardId]); $card=$st->fetch();
        if(!$card) throw new InvalidArgumentException('Cartão não encontrado.');
        if(!$personId && !empty($card['person_id'])) $personId=(int)$card['person_id'];
        $catId=category_id($db,$category,'expense');

        $purchase=new DateTime($purchaseDate);
        $closing=(int)$card['closing_day']; $due=(int)$card['due_day'];
        $closeDay=min($closing,(int)$purchase->format('t'));
        if((int)$purchase->format('d')>$closeDay) $purchase->modify('first day of next month');
        $invoiceMonth=$purchase->format('Y-m');
        if($due <= $closing) $invoiceMonth=(new DateTime($invoiceMonth.'-01'))->modify('+1 month')->format('Y-m');

        $group=$installments>1?bin2hex(random_bytes(16)):null;
        $base=floor(($total/$installments)*100)/100;
        $allocated=0.0;
        $db->beginTransaction();
        try{
            for($i=1;$i<=$installments;$i++){
                $amount=$i===$installments?round($total-$allocated,2):$base; $allocated=round($allocated+$amount,2);
                $m=(new DateTime($invoiceMonth.'-01'))->modify('+'.($i-1).' month')->format('Y-m');
                $dueDate=invoice_due_date($m,$due);
                $st=$db->prepare("INSERT INTO {$p}card_purchases(card_id,person_id,category_id,description,purchase_date,amount,invoice_month,due_date,installment_group,installment_number,installment_total,observations) VALUES(?,?,?,?,?,?,?,?,?,?,?,?)");
                $st->execute([$cardId,$personId,$catId,$description,$purchaseDate,$amount,$m.'-01',$dueDate,$group,$i,$installments,$obs]);
                sync_invoice($db,$cardId,$m,$dueDate);
            }
            $db->commit();
        }catch(Throwable $e){$db->rollBack();throw $e;}
        respond(['ok'=>true,'message'=>'Compra incluída na fatura.','month'=>$invoiceMonth]);
    }

    if ($action === 'invoice') {
        $cardId=(int)($in['card_id']??0); $month=safe_month($in['month']??null);
        $st=$db->prepare("SELECT id,name,closing_day,due_day,limit_amount FROM {$p}cards WHERE id=?");
        $st->execute([$cardId]); $card=$st->fetch(); if(!$card) throw new InvalidArgumentException('Cartão não encontrado.');
        $st=$db->prepare("SELECT cp.id,cp.description,cp.purchase_date,cp.amount,DATE_FORMAT(cp.due_date,'%Y-%m-%d') due_date,cp.installment_number,cp.installment_total,COALESCE(c.name,'Outros') category,COALESCE(cp.observations,'') observations FROM {$p}card_purchases cp LEFT JOIN {$p}categories c ON c.id=cp.category_id WHERE cp.card_id=? AND DATE_FORMAT(cp.due_date,'%Y-%m')=? ORDER BY cp.purchase_date,cp.id");
        $st->execute([$cardId,$month]); $rows=$st->fetchAll();
        $total=array_sum(array_map(fn($r)=>(float)$r['amount'],$rows));
        $st=$db->prepare("SELECT id,status,DATE_FORMAT(due_date,'%Y-%m-%d') due_date,CASE WHEN paid_date IS NULL THEN NULL ELSE DATE_FORMAT(paid_date,'%Y-%m-%d') END paid_date,amount FROM {$p}card_invoices WHERE card_id=? AND DATE_FORMAT(reference_month,'%Y-%m')=? ORDER BY id LIMIT 1");
        $st->execute([$cardId,$month]); $invoice=$st->fetch();
        $due=$invoice['due_date']??invoice_due_date($month,(int)$card['due_day']);
        respond(['ok'=>true,'card'=>$card,'month'=>$month,'rows'=>$rows,'total'=>$total,'status'=>$invoice['status']??'open','paid_date'=>$invoice['paid_date']??null,'due_date'=>$due]);
    }

    if ($action === 'invoice_pay' || $action === 'invoice_reopen') {
        $cardId=(int)($in['card_id']??0); $month=safe_month($in['month']??null);
        if($cardId<=0) throw new InvalidArgumentException('Cartão inválido.');
        if($action==='invoice_reopen'){
            $st=$db->prepare("UPDATE {$p}card_invoices SET status='open',paid_date=NULL WHERE card_id=? AND DATE_FORMAT(reference_month,'%Y-%m')=?");
            $st->execute([$cardId,$month]);
            respond(['ok'=>true,'message'=>'Fatura reaberta.']);
        }
        $paid=safe_date($in['paid_date']??null);
        $st=$db->prepare("SELECT due_day FROM {$p}cards WHERE id=?");$st->execute([$cardId]);$dueDay=(int)$st->fetchColumn();
        $due=invoice_due_date($month,$dueDay?:10);
        $st=$db->prepare("SELECT COALESCE(SUM(amount),0) FROM {$p}card_purchases WHERE card_id=? AND DATE_FORMAT(due_date,'%Y-%m')=?");$st->execute([$cardId,$month]);$total=(float)$st->fetchColumn();
        $st=$db->prepare("SELECT id FROM {$p}card_invoices WHERE card_id=? AND DATE_FORMAT(reference_month,'%Y-%m')=? LIMIT 1");$st->execute([$cardId,$month]);$id=$st->fetchColumn();
        if($id){
            $st=$db->prepare("UPDATE {$p}card_invoices SET amount=?,due_date=?,status='paid',paid_date=? WHERE id=?");$st->execute([$total,$due,$paid,$id]);
        }else{
            $st=$db->prepare("INSERT INTO {$p}card_invoices(card_id,reference_month,due_date,amount,status,paid_date) VALUES(?,?,?,?, 'paid',?)");$st->execute([$cardId,$month.'-01',$due,$total,$paid]);
        }
        respond(['ok'=>true,'message'=>'Fatura marcada como paga.']);
    }

    if ($action === 'financings') {
        $rows=[];
        try{
            $sql="SELECT f.id,f.name,f.total_amount,f.installment_amount,f.total_installments,f.paid_installments,f.active,
                DATE_FORMAT(f.next_due_date,'%Y-%m-%d') next_due_date,
                CASE WHEN f.last_paid_date IS NULL THEN NULL ELSE DATE_FORMAT(f.last_paid_date,'%Y-%m-%d') END last_paid_date
                FROM {$p}financings f ORDER BY f.active DESC,f.id DESC";
            $rows=$db->query($sql)->fetchAll();
        }catch(Throwable $e){
            $rows=$db->query("SELECT id,name,total_amount,installment_amount,total_installments,paid_installments,active,NULL next_due_date,NULL last_paid_date FROM {$p}financings ORDER BY active DESC,id DESC")->fetchAll();
        }
        respond(['ok'=>true,'rows'=>$rows]);
    }

    if ($action === 'financing_pay') {
        $id=(int)($in['id']??0); $paidDate=safe_date($in['paid_date']??null);
        if($id<=0) throw new InvalidArgumentException('Financiamento inválido.');
        $db->beginTransaction();
        try{
            $st=$db->prepare("SELECT id,installment_number FROM {$p}financing_installments WHERE financing_id=? AND status<>'paid' ORDER BY installment_number LIMIT 1 FOR UPDATE");
            $st->execute([$id]);$inst=$st->fetch(); if(!$inst) throw new RuntimeException('Não há parcela pendente.');
            $st=$db->prepare("UPDATE {$p}financing_installments SET status='paid',paid_date=? WHERE id=?");$st->execute([$paidDate,$inst['id']]);
            $st=$db->prepare("SELECT COUNT(*) FROM {$p}financing_installments WHERE financing_id=? AND status='paid'");$st->execute([$id]);$paid=(int)$st->fetchColumn();
            $st=$db->prepare("SELECT total_installments FROM {$p}financings WHERE id=?");$st->execute([$id]);$total=(int)$st->fetchColumn();
            $st=$db->prepare("SELECT due_date FROM {$p}financing_installments WHERE financing_id=? AND status<>'paid' ORDER BY installment_number LIMIT 1");$st->execute([$id]);$next=$st->fetchColumn();
            $st=$db->prepare("UPDATE {$p}financings SET paid_installments=?,last_paid_date=?,next_due_date=?,active=? WHERE id=?");
            $st->execute([$paid,$paidDate,$next?:null,$paid<$total?1:0,$id]);
            $db->commit();
        }catch(Throwable $e){$db->rollBack();throw $e;}
        respond(['ok'=>true,'message'=>'Parcela paga e financiamento avançado.']);
    }

    if ($action === 'radar') {
        $fetch=function(int $serie): ?array {
            $end=new DateTime();$start=(clone $end)->modify('-90 days');
            $url='https://api.bcb.gov.br/dados/serie/bcdata.sgs.'.$serie.'/dados?formato=json&dataInicial='.$start->format('d/m/Y').'&dataFinal='.$end->format('d/m/Y');
            $raw=false;
            if(function_exists('curl_init')){
                $ch=curl_init($url);curl_setopt_array($ch,[CURLOPT_RETURNTRANSFER=>true,CURLOPT_TIMEOUT=>8,CURLOPT_USERAGENT=>'GranaOk-Web/0.1']);$raw=curl_exec($ch);curl_close($ch);
            }else{$raw=@file_get_contents($url);}
            $data=$raw?json_decode($raw,true):null;
            return is_array($data)&&$data?end($data):null;
        };
        respond(['ok'=>true,'selic'=>$fetch(1178),'ipca'=>$fetch(433)]);
    }

    respond(['ok'=>false,'error'=>'Ação desconhecida.'],404);
} catch (Throwable $e) {
    respond(['ok'=>false,'error'=>$e->getMessage()],500);
}
