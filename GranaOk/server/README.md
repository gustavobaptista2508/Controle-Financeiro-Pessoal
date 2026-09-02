# GranaOk Server

Node.js + MySQL + PWA para o GranaOk.

## Produção

O processo Node escuta somente em 127.0.0.1:3000. O Nginx é a entrada pública nas portas 80/443.

## Arquivos sensíveis

Nunca envie o arquivo .env para o GitHub. Use .env.example apenas como modelo.

## Primeiro administrador

Depois de configurar o .env e rodar npm install:

```bash
node scripts/create-admin.js gustavo "Gustavo"
```

O script gera uma senha inicial aleatória e mostra uma única vez no terminal.

## Compatibilidade

As consultas foram mantidas compatíveis com MySQL 5.6.
