(()=>{
  const version='BETA 0.3.4';
  try{
    window.brand=function(){return '<div class="brandrow"><span class="logo-mark"></span><div class="brand">Grana<span>Ok</span><small>'+version+'</small></div></div>'};
  }catch(e){}
  const apply=()=>document.querySelectorAll('.brand small').forEach(el=>{el.textContent=version});
  apply();
  new MutationObserver(apply).observe(document.documentElement,{childList:true,subtree:true});
})();
