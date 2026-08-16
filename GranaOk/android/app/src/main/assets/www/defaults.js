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
    if(ssl) ssl.value=defaults.ssl;
  }

  document.addEventListener('DOMContentLoaded',apply);
  new MutationObserver(apply).observe(document.documentElement,{childList:true,subtree:true});
  setTimeout(apply,0);
})();
