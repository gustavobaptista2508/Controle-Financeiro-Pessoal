# GranaOk Desktop 0.2.0

Aplicativo Windows baseado em HTML/CSS/JavaScript (Electron), conectado diretamente ao mesmo MySQL usado pelo GranaOk Android.

## Arquitetura

- Interface: HTML/CSS/JavaScript
- Aplicativo: Electron
- Banco: MySQL direto via mysql2
- Credenciais: salvas somente no computador; a senha é criptografada com Electron safeStorage (Windows DPAPI)
- Renderer sem Node.js direto: contextIsolation ativo e acesso ao banco somente por IPC

## Primeira execução

Na primeira abertura, informe:
- host MySQL
- porta
- banco
- usuário
- senha
- prefixo (padrão: granaok_)
- SSL, se aplicável

Use **Testar conexão**. Só depois salve.

## Recursos iniciais

- Dashboard mensal
- Lançamentos e status Pago/Pendente/Atrasado
- Contas
- Pessoas/casal e categorias
- Cartões e compras parceladas
- Fatura por cartão e competência
- Marcar fatura como paga / reabrir
- Financiamentos e baixa da parcela atual
- Backup JSON das tabelas principais
- Tela para alterar a conexão MySQL

## Importante

O app não embute credenciais do banco no executável nem no HTML. A senha informada é guardada criptografada no perfil do Windows.

A conexão direta depende de o provedor MySQL permitir acesso externo na porta configurada.

- Dashboard com **Total de despesas do mês = despesas + faturas dos cartões**.


## Modo iPhone / LAN

O Desktop pode abrir uma interface web apenas na rede local, por padrão na porta 8787.

- O iPhone e o Windows devem estar na mesma rede Wi-Fi.
- O navegador do iPhone nunca recebe a senha do MySQL.
- O Desktop continua sendo a única camada que acessa o MySQL.
- Não encaminhe a porta LAN no roteador para a internet.
- A sessão móvel expira após 12 horas.
- Após 5 tentativas de login inválidas, o endereço é temporariamente limitado.

## Usuários de acesso

A tabela `granaok_app_users` guarda os logins do acesso móvel.
As senhas são armazenadas somente como hash scrypt com salt aleatório.

O primeiro usuário criado é automaticamente Administrador.
Usuários de acesso são independentes de `granaok_people`; opcionalmente um login pode ser vinculado a uma Pessoa/Casal.

Na versão 0.2.0, os usuários autenticados acessam o mesmo conjunto financeiro compartilhado. O campo de perfil deixa a base pronta para permissões mais granulares em versões futuras.
