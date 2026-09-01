# GranaOk Desktop 0.1

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
