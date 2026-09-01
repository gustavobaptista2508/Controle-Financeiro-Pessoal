(()=>{
  const bridge=()=>window.GranaInvoicePayment||null;
  const parse=s=>{try{return JSON.parse(s)}catch{return {ok:false,error:'Resposta inválida.'}}};
  const today=()=>new Date().toISOString().slice(0,10);
  const base=window.GranaOkCardInvoice;

  if(typeof base==='function'){
    window.GranaOkCardInvoice=function(s){
      base(s);
      const d=parse(s);
      if(!d.ok)return;
      const el=document.getElementById('invoice-content');
      if(!el)return;
      const card=d.card||{}, cardId=Number(card.id||0), month=d.month||'';
      const box=document.createElement('div');
      box.className='card';
      box.id='invoice-payment-060';
      if(d.status==='paid'){
        box.innerHTML='<div class="finance-head"><div><b>✓ Fatura paga</b><div class="muted">Esta fatura já está marcada como paga.</div></div><button class="secondary" id="invoice-reopen-060">Reabrir</button></div><div id="invoice-pay-out"></div>';
        const b=box.querySelector('#invoice-reopen-060');
        if(b)b.onclick=()=>{
          if(!confirm('Reabrir esta fatura?'))return;
          b.disabled=true;
          bridge()?.reopenInvoice?.(JSON.stringify({cardId,month}));
        };
      }else{
        box.innerHTML='<div class="finance-head"><div><b>Pagamento da fatura</b><div class="muted">Marque a fatura como paga quando o pagamento for realizado.</div></div></div><label>Data do pagamento</label><input id="invoice-paid-date-060" type="date" value="'+today()+'"><button class="primary" id="invoice-pay-060">✓ Marcar fatura como paga</button><div id="invoice-pay-out"></div>';
        const b=box.querySelector('#invoice-pay-060');
        if(b)b.onclick=()=>{
          const paidDate=(document.getElementById('invoice-paid-date-060')||{}).value||today();
          if(!confirm('Confirmar pagamento desta fatura?'))return;
          b.disabled=true;
          const out=document.getElementById('invoice-pay-out');if(out)out.innerHTML='<div class="note">Registrando pagamento...</div>';
          bridge()?.payInvoice?.(JSON.stringify({cardId,month,paidDate}));
        };
      }
      const kpis=el.querySelector('.invoice-kpis');
      if(kpis)kpis.insertAdjacentElement('afterend',box); else el.prepend(box);
    };
  }

  window.GranaOkInvoicePayment060=function(s){
    const d=parse(s),out=document.getElementById('invoice-pay-out');
    if(!d.ok){
      if(out)out.innerHTML='<div class="note err">'+String(d.error||'Não foi possível atualizar a fatura.')+'</div>';
      const b=document.getElementById('invoice-pay-060')||document.getElementById('invoice-reopen-060');if(b)b.disabled=false;
      return;
    }
    if(out)out.innerHTML='<div class="note ok">'+String(d.message||'Fatura atualizada.')+'</div>';
    setTimeout(()=>window.cardInvoiceView?.(Number(d.card_id),d.month),350);
  };
})();