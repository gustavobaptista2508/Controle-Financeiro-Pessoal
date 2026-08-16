(()=>{
  const defaults={
    host:'gadobd.mysql.uhserver.com',
    port:'3306',
    database:'gadobd',
    user:'gustavobaptista',
    ssl:'false'
  };

  function fill(id,value){
    const el=document.getElementById(id);
    if(el && !String(el.value||'').trim()) el.value=value;
  }

  function apply(){
    fill('host',defaults.host);
    fill('port',defaults.port);
    fill('db',defaults.database);
    fill('dbuser',defaults.user);
    const ssl=document.getElementById('ssl');
    if(ssl && !ssl.dataset.defaultApplied){
      ssl.value=defaults.ssl;
      ssl.dataset.defaultApplied='1';
    }
  }

  document.addEventListener('DOMContentLoaded',apply,{once:true});
  const observer=new MutationObserver(()=>apply());
  observer.observe(document.getElementById('app'),{childList:true,subtree:true});
  setTimeout(apply,0);
})();
