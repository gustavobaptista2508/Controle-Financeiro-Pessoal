(()=>{
  const version='BETA 0.6.0';
  try{
    window.brand=function(){
      return '<div class="brandrow"><span class="logo-mark"></span><div class="brand">Grana<span>Ok</span><small>'+version+'</small></div></div>';
    };
  }catch(e){}

  document.querySelectorAll('.brand small').forEach(el=>{
    if(el.textContent!==version) el.textContent=version;
  });
})();
