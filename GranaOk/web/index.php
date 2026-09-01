<?php
require __DIR__ . '/lib.php';
$appName = 'GranaOk Web';
try { $appName = (string)(cfg()['app_name'] ?? $appName); } catch (Throwable $e) {}
?>
<!doctype html>
<html lang="pt-BR">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width,initial-scale=1,viewport-fit=cover">
<meta name="theme-color" content="#0f172a">
<title><?= htmlspecialchars($appName, ENT_QUOTES, 'UTF-8') ?></title>
<link rel="stylesheet" href="assets/styles.css?v=1">
</head>
<body>
<div id="login-screen" class="login-screen hidden">
  <form id="login-form" class="login-card">
    <div class="logo">Grana<span>Ok</span><small>WEB</small></div>
    <h1>Seu financeiro, também no navegador.</h1>
    <p>Use a senha configurada no servidor.</p>
    <label>Senha</label>
    <input id="login-password" type="password" autocomplete="current-password" required autofocus>
    <button type="submit" class="primary">Entrar</button>
    <div id="login-out"></div>
  </form>
</div>

<div id="app-shell" class="app-shell hidden">
  <aside class="sidebar">
    <div class="logo side-logo">Grana<span>Ok</span><small>WEB 0.1</small></div>
    <nav id="nav">
      <button data-view="dashboard" class="active">⌂ <span>Visão geral</span></button>
      <button data-view="transactions">⇄ <span>Lançamentos</span></button>
      <button data-view="cards">▣ <span>Cartões</span></button>
      <button data-view="accounts">🏦 <span>Contas</span></button>
      <button data-view="financings">▤ <span>Financiamentos</span></button>
      <button data-view="registry">＋ <span>Cadastros</span></button>
      <button data-view="radar">◉ <span>Radar</span></button>
    </nav>
    <button id="logout" class="logout">Sair</button>
  </aside>

  <main>
    <header class="topbar">
      <button id="menu-toggle" class="menu-toggle">☰</button>
      <div><b id="page-title">Visão geral</b><small id="page-subtitle">GranaOk Web</small></div>
      <button id="refresh" class="ghost">↻ Atualizar</button>
    </header>
    <section id="content" class="content"></section>
  </main>
</div>

<div id="modal" class="modal hidden">
  <div class="modal-card">
    <button id="modal-close" class="modal-close">×</button>
    <div id="modal-content"></div>
  </div>
</div>

<script src="assets/app.js?v=1"></script>
</body>
</html>
