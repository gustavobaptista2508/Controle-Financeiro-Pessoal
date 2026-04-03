Relatório Técnico: Sistema de Controle Financeiro Pessoal 
1. Identificação do Projeto 
● Nome do Sistema:  Controle Financeiro Pessoal 
● Autor e Desenvolvedor Principal: Luiz Gustavo Carvalho Baptista 
● Data de Referência:  Abril de 2026 
2. Introdução e Visão GeralEste documento descreve a arquitetura e as especificações 
técnicas do software de Gestão de Finanças Pessoais, desenvolvido por Gustavo. O 
sistema foi projetado sob uma perspectiva de arquitetura robusta para ambiente desktop, 
visando a centralização do fluxo de caixa individual através do controle rigoroso de 
lançamentos, categorias, contas e pessoas.A escolha da plataforma tecnológica foi 
estratégica, priorizando a estabilidade e a produtividade no desenvolvimento: 
● Rapidez de Implementação:  Agilidade no ciclo de vida de desenvolvimento para 
sistemas de uso pessoal. 
● Eficiência para Sistemas Locais:  Alta performance na manipulação de dados e 
persistência em arquivos locais. 
● Otimização de UI/UX:  Facilidade na construção de interfaces baseadas em grids 
complexos e filtros dinâmicos, o que combina idealmente com rotinas de 
lançamentos financeiros, importação de dados e geração de relatórios. 
3. Stack TecnológicaO ecossistema técnico foi selecionado para garantir o 
desacoplamento de responsabilidades e a segurança de tipos.| Componente | 
Tecnologia/Ferramenta || ------ | ------ || Linguagem | C# (Versão 12) || Framework | .NET 8 
|| Interface de Usuário | Windows Forms (WinForms) || Banco de Dados | SQLite || ORM | 
Entity Framework Core (EF Core) || Manipulação de Excel | ClosedXML | 
4. Arquitetura do SistemaO projeto FinanceiroPessoal.WinForms adota um padrão de 
organização modular, garantindo que a lógica de persistência e regras de negócio sejam 
independentes da camada de visualização. 
FinanceiroPessoal.WinForms 
├── Data 
│   
├── FinanceiroDbContext.cs 
│   
└── DbInitializer.cs 
├── Models 
│   
├── Lancamento.cs 
│   
├── Categoria.cs 
│   
├── Conta.cs 
│   
└── Pessoa.cs 
├── Services 
│   
├── ExcelImportService.cs 
│   
├── LancamentoService.cs 
│   
└── CadastroAuxiliarService.cs 
├── Forms 
│   
├── FrmPrincipal.cs 
│   
├── FrmLancamentos.cs 
│   
├── FrmNovoLancamento.cs 
│   
└── FrmEditarLancamento.cs 
├── Program.cs 
└── appsettings.json 
Responsabilidades das Camadas: 
● Data:  Gerencia o contexto de persistência via FinanceiroDbContext. Inclui o 
DbInitializer, responsável pela execução do EnsureCreated(), garantindo a 
integridade do esquema SQLite no primeiro arranque. 
● Models:  Define as entidades do domínio com forte tipagem. 
● Services:  Camada de lógica de negócio. O LancamentoService orquestra 
transações e mudanças de estado (como transições de status de pagamento), 
enquanto o CadastroAuxiliarService facilita o provimento de dados para 
componentes de interface. 
● Forms:  Implementa a lógica de interação do usuário, consumindo os serviços via 
injeção de dependência simplificada. 
5. Modelagem de Dados (Entidades)O sistema utiliza o Entity Framework Core para 
mapear objetos de domínio para o banco de dados SQLite, mantendo relações de 1:N entre 
Lançamentos e suas tabelas auxiliares. 
● Lancamento:  Entidade central que registra as transações. Atributos: Id, Descricao, 
Valor, DataVencimento, DataPagamento, Status (Pendente, Pago, Atrasado), 
Competencia, Observacoes, e as Foreign Keys para Categoria, Conta e Pessoa. 
● Categoria:  Classificação semântica dos gastos (ex: Moradia, Alimentação). Possui 
Id e Nome. 
● Conta:  Define a origem financeira (ex: Caixa, Banco). Atributos: Id, Nome e Tipo 
(valor padrão "Outro"). 
● Pessoa:  Identifica os envolvidos na transação. Possui Id e Nome.A persistência é 
centralizada na classe FinanceiroDbContext, que expõe os DbSet correspondentes a 
cada entidade. 
6. Funcionalidades Principais 
6.1 Dashboard (Painel Principal) 
O FrmPrincipal atua como um cockpit financeiro, sintetizando os dados em tempo real: 
● Cards de Indicadores:  Exibição do "Total Pendente", "Total Pago", "Vencimentos 
nos próximos 7 dias" e "Total do Mês". 
● Painel "Resumo do Mês":  Monitoramento granular que inclui métricas de itens 
"Atrasados", "Vencem Hoje" e "Vencem esta Semana". 
● Ações Rápidas:  Atalhos operacionais para "Novo Lançamento", "Ver 
Lançamentos", "Importar Excel" e "Gerar Relatório". 
● Visualização Analítica:  Gráfico de "Gastos por Categoria" (Pizza) e grade de 
"Próximos Vencimentos" para controle antecipado de caixa. 
6.2 Gestão de Lançamentos (CRUD) 
A tela FrmLancamentos gerencia o ciclo de vida das transações: 
● Filtros Dinâmicos:  Pesquisa refinada por Pessoa e Status. 
● Operações de Estado:  Além de Inserir, Editar e Excluir, o sistema implementa o 
método MarcarComoPago(id) via LancamentoService, que automatiza a atualização 
da DataPagamento e do status para "Pago". 
6.3 Importação de Dados 
O ExcelImportService utiliza a biblioteca  ClosedXML  para realizar o parsing de arquivos 
.xlsx. Diferente de uma importação simples, o serviço executa uma validação lógica que 
retorna um sumário detalhado contendo: 
● Contagem de registros lidos versus importados com sucesso. 
● Registros ignorados por inconsistência. 
● Lista de avisos (Warnings) gerados durante o processamento de linhas e colunas. 
7. Fluxo de Execução e InicializaçãoO ponto de entrada da aplicação (Program.cs) segue 
uma sequência rigorosa para garantir a estabilidade do ambiente: 
1. 
Inicialização da Configuração:  Ativação do ApplicationConfiguration.Initialize(). 
2. 
3. 
Garantia de Persistência:  Chamada ao método estático DbInitializer.Initialize(). 
Este passo verifica a presença do banco SQLite e realiza o  Seed  de dados básicos, 
caso necessário. 
Boot da UI:  Execução do Application.Run(new FrmPrincipal()), elevando a interface 
de usuário principal. 
8. Considerações de Interface (UI/UX)Gustavo implementou padrões de design que 
priorizam a integridade dos dados e a produtividade do usuário final: 
● DataGridView:  Utilizado para visualização tabular densa, permitindo rápida triagem 
de dados. 
● Integridade Referencial na UI:  O uso de ComboBox configurados como 
DropDownList impede a entrada de valores órfãos em campos de Categoria, Conta 
e Pessoa. 
● Controles Especializados:  Utilização de DateTimePicker para precisão 
cronológica e txtObservacoes com a propriedade Multiline ativa para descritivos 
detalhados. 
9. Conclusão e Próximos PassosO sistema "Controle Financeiro Pessoal" estabelece 
uma fundação técnica sólida, pautada em separação de preocupações e robustez de dados. 
A arquitetura atual permite a escalabilidade do sistema para novas funcionalidades sem 
comprometer a estabilidade existente.Próximos Passos Planejados: 
● Modo 2 de Importação:  Desenvolvimento de um motor de parsing adaptado para 
planilhas complexas (detecção de abas específicas e exclusão automática de linhas 
de totais). 
● Mapeamento Inteligente:  Implementação de mapeamento automático de campos 
de planilhas externas para as entidades do sistema. 
