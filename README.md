Controle Financeiro Pessoal
Este é um sistema robusto de gestão financeira desktop desenvolvido por Gustavo para centralizar o fluxo de caixa individual por meio de um controle rigoroso de lançamentos, categorias, contas e pessoas
. O projeto foi construído com foco em estabilidade, rapidez de implementação e uma experiência de usuário (UX) otimizada para rotinas financeiras
.
🚀 Tecnologias Utilizadas
O ecossistema técnico foi selecionado para garantir o desacoplamento de responsabilidades e a segurança de tipos
:
Linguagem: C# (Versão 12)
.
Framework: .NET 8
.
Interface de Usuário: Windows Forms (WinForms)
.
Banco de Dados: SQLite (persistência local leve e eficiente)
.
ORM: Entity Framework Core (EF Core)
.
Manipulação de Excel: ClosedXML
.
🏗️ Arquitetura do Projeto
O projeto adota uma organização modular, garantindo que a lógica de persistência e as regras de negócio sejam independentes da camada de visualização
:
/Data: Gerencia o contexto de persistência (FinanceiroDbContext) e a inicialização automática do banco de dados (DbInitializer)
.
/Models: Define as entidades de domínio (Lançamento, Categoria, Conta, Pessoa) com tipagem forte
.
/Services: Camada de lógica de negócio, incluindo a orquestração de transações e o motor de importação de arquivos
.
/Forms: Implementação da interface e interação com o usuário
.
✨ Funcionalidades Principais
Dashboard Inteligente: Painel principal que atua como um cockpit financeiro, exibindo KPIs como Total Pendente, Total Pago, Vencimentos Próximos e Resumo Mensal em tempo real
.
Gestão de Lançamentos (CRUD): Controle completo do ciclo de vida das transações, permitindo inserir, editar, excluir e "Marcar como Pago" com atualização automática de status
.
Filtros Dinâmicos: Pesquisa refinada por Pessoa e Status para facilitar a auditoria dos dados
.
Importação de Excel: Módulo de ETL que utiliza a biblioteca ClosedXML para realizar o parsing de planilhas .xlsx, com validação lógica e sumário de importação
.
Integridade de Dados: Uso de controles especializados e restrições na interface para impedir a entrada de valores inconsistentes
.
🛠️ Configuração e Instalação
Pré-requisitos
SDK do .NET 8 ou superior
.
Visual Studio 2022 ou VS Code.
Instalação de Dependências
Para configurar o ambiente, execute os seguintes comandos no terminal do projeto:
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.Sqlite
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package ClosedXML
``` [10]

## 🏃 Como Executar

1.  Clone o repositório.
2.  Compile o projeto para restaurar as dependências.
3.  Execute a aplicação. O sistema utiliza o `DbInitializer` para garantir que o esquema do banco de dados SQLite seja criado automaticamente no primeiro arranque [4, 11].

## 📈 Roadmap de Evolução

*   Desenvolvimento de um motor de parsing adaptado para planilhas complexas com detecção automática de abas [12].
*   Implementação de mapeamento inteligente de campos para entidades do sistema [12].
*   Geração de relatórios financeiros detalhados em PDF.

---
**Autor:** Luiz Gustavo Carvalho Baptista

## ☁️ Deploy no Cloudflare (pronto para proxy/tunnel)

O projeto Web (`FinanceiroPessoal.Web`) agora está preparado para rodar atrás do Cloudflare com:

- suporte a headers `X-Forwarded-*` (IP real + protocolo HTTPS);
- `Dockerfile` para build/publicação da aplicação em `net8.0`;
- porta padrão `8080`, ideal para Cloudflare Tunnel.

### 1) Build da imagem

```bash
docker build -f FinanceiroPessoal.Web/Dockerfile -t financeiro-web:latest .
```

### 2) Rodar localmente com variáveis de ambiente

```bash
docker run --rm -p 8080:8080 \
  -e ConnectionStrings__MySqlConnection="server=SEU_HOST;port=3306;database=SEU_DB;user=SEU_USER;password=SEU_PASS" \
  financeiro-web:latest
```

### 3) Publicar com Cloudflare Tunnel

No `cloudflared`, aponte o hostname para `http://SEU_CONTAINER:8080`.

Exemplo de regra de ingress:

```yaml
ingress:
  - hostname: financeiro.seudominio.com
    service: http://financeiro-web:8080
  - service: http_status:404
```

> Observação: como o app usa renderização interativa do Blazor Server, mantenha WebSockets habilitado no proxy do Cloudflare.
