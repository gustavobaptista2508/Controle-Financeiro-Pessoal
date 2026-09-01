# GranaOk Web 0.1

Versão web responsiva do GranaOk, usando o **mesmo banco MySQL** da versão Android.

## Segurança

O navegador **não se conecta diretamente ao MySQL**. Toda a comunicação passa por PHP/PDO no servidor.  
Nunca coloque a senha do MySQL em JavaScript, HTML ou arquivos públicos.

## Requisitos

- PHP 7.4+ (recomendado PHP 8.1+)
- extensão PDO MySQL
- HTTPS
- acesso do servidor PHP ao MySQL do GranaOk

## Instalação

1. Envie a pasta `GranaOk/web` para uma pasta do seu servidor web.
2. Copie `config.example.php` para `config.local.php`.
3. Preencha host, porta, banco, usuário e senha MySQL.
4. Defina uma senha forte em `web_password`.
5. Garanta que `.htaccess` esteja ativo ou, de preferência, mova `config.local.php` para fora da raiz pública e ajuste `lib.php`.
6. Acesse `index.php` por HTTPS.

## Recursos da versão inicial

- Dashboard mensal
- Navegação por mês
- Lançamentos com edição e status Pago/Pendente/Atrasado
- Contas
- Pessoas/casal e categorias
- Cartões de crédito
- Compras e parcelamentos no cartão
- Visualização de fatura por cartão/mês
- **Marcar fatura como paga e reabrir fatura**
- Financiamentos e baixa da parcela atual
- Radar de investimentos com referências do Banco Central
- Simulador de investimentos

## Banco de dados

A versão web reutiliza as tabelas `granaok_*` já usadas pelo Android.  
Ela adiciona apenas a coluna `paid_date` em `granaok_card_invoices` quando necessário.

## Observação

A marcação da fatura como paga **não cria uma segunda despesa em `transactions`**. A fatura já representa a saída financeira no modelo do GranaOk, evitando dupla contagem.
