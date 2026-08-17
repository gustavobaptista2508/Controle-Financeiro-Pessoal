(()=>{
  window.saveTransaction=function(){
    const type=document.getElementById('txtype').value;
    const desc=document.getElementById('txdesc').value.trim();
    const raw=document.getElementById('txamount').value.trim();
    const normalized=raw.replace(/\./g,'').replace(',','.');
    const out=document.getElementById('out');
    if(!desc||!normalized||Number(normalized)<=0){out.innerHTML=`<div class="note err">Informe descrição e valor.</div>`;return}
    const category=document.getElementById('txcat').value||'Outros';
    const dueDate=document.getElementById('txdate').value||window.today();
    out.innerHTML='<div class="note">Salvando lançamento...</div>';
    if(type==='expense'){
      const installments=document.getElementById('txinstall-mode').value==='2'?Math.max(2,Math.min(120,Number(document.getElementById('txinstallments').value||2))):1;
      window.GranaExtras?.addExpense?.(JSON.stringify({description:desc,category,totalAmount:raw,dueDate,installments}));
    }else{
      window.GranaNative?.addTransaction?.(JSON.stringify({type:'income',description:desc,category,amount:normalized,dueDate}));
    }
  };
})();
