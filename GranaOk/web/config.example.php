<?php
// Copie este arquivo para config.local.php e ajuste os dados.
// Nunca publique config.local.php em repositório.
return [
    'db' => [
        'host' => 'SEU_HOST_MYSQL',
        'port' => 3306,
        'name' => 'SEU_BANCO',
        'user' => 'SEU_USUARIO',
        'password' => 'SUA_SENHA',
        'charset' => 'utf8mb4',
        'prefix' => 'granaok_',
    ],
    // Senha exclusiva para entrar na versão web.
    // Pode ser diferente da senha do banco.
    'web_password' => 'TROQUE_ESTA_SENHA',
    'app_name' => 'GranaOk Web',
];
