using FinanceiroPessoal.WinForms.Models;
using FinanceiroPessoal.WinForms.Services;
using ScottPlot;
using System.Globalization;
using System.Windows.Forms.DataVisualization.Charting;
//using Color = ScottPlot.Color;
using System.Drawing;

namespace FinanceiroPessoal.WinForms
{
    public partial class Form1 : Form
    {
        private readonly DashboardService _dashboardService;

        private readonly System.Drawing.Color _menuSidebar = System.Drawing.Color.White;
        private readonly System.Drawing.Color _menuAtivo = System.Drawing.Color.FromArgb(37, 99, 235);      // azul
        private readonly System.Drawing.Color _menuInativo = System.Drawing.Color.White;                    // fundo normal
        private readonly System.Drawing.Color _menuHover = System.Drawing.Color.FromArgb(239, 244, 255);    // hover
        private readonly System.Drawing.Color _textoAtivo = System.Drawing.Color.White;
        private readonly System.Drawing.Color _textoInativo = System.Drawing.Color.FromArgb(31, 41, 55);
        public Form1()
        {
            InitializeComponent();

            // Configuração inicial do serviço e repositório
            var tipoBanco = TipoBanco.OnlineMySql;
            var repo = DatabaseFactory.CriarLancamentoRepository(tipoBanco);
            _dashboardService = new DashboardService(repo);

            // Configurações iniciais dos botões do menu lateral
            btnLancamentos.Text = "☰  Lançamentos";
            btnCategorias.Text = "◈  Categorias";
            btnContas.Text = "◉  Contas";
            btnSair.Text = "⏻  Sair";
        }

        private void AplicarBordasArredondadas()
        {
            UIHelpers.ApplyRoundedRegion(pnlCardPago, 18);
            UIHelpers.ApplyRoundedRegion(pnlCardPendente, 18);
            UIHelpers.ApplyRoundedRegion(pnlCardMes, 18);
            UIHelpers.ApplyRoundedRegion(pnlCardSemana, 18);

            UIHelpers.ApplyRoundedRegion(pnlTabela, 18);
            UIHelpers.ApplyRoundedRegion(pnlAcoesRapidas, 18);
            UIHelpers.ApplyRoundedRegion(pnlGrafico, 18);
            UIHelpers.ApplyRoundedRegion(pnlResumo, 18);
        }

        private void EstilizarBotaoMenu(Button botao)
        {
            botao.FlatStyle = FlatStyle.Flat;
            botao.FlatAppearance.BorderSize = 0;
            botao.FlatAppearance.MouseOverBackColor = _menuHover;
            botao.FlatAppearance.MouseDownBackColor = _menuHover;
            botao.BackColor = _menuInativo;
            botao.ForeColor = _textoInativo;
            botao.Font = new System.Drawing.Font("Segoe UI Semibold", 11F, System.Drawing.FontStyle.Bold);
            botao.TextAlign = ContentAlignment.MiddleLeft;
            botao.Padding = new Padding(16, 0, 0, 0);
            botao.Cursor = Cursors.Hand;
            botao.Height = 46;
            botao.UseVisualStyleBackColor = false;
        }

        private void EstilizarGrid(DataGridView grid)
        {
            // Aparência geral
            grid.BorderStyle = BorderStyle.None;
            grid.BackgroundColor = System.Drawing.Color.White;
            grid.GridColor = System.Drawing.Color.FromArgb(230, 235, 245);
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;

            // Cabeçalho
            grid.ColumnHeadersDefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(37, 99, 235);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = System.Drawing.Color.White;
            grid.ColumnHeadersDefaultCellStyle.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F, System.Drawing.FontStyle.Bold);
            grid.ColumnHeadersDefaultCellStyle.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            grid.ColumnHeadersHeight = 38;
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            grid.EnableHeadersVisualStyles = false;

            // Linhas
            grid.DefaultCellStyle.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            grid.DefaultCellStyle.ForeColor = System.Drawing.Color.FromArgb(31, 41, 55);
            grid.DefaultCellStyle.BackColor = System.Drawing.Color.White;
            grid.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(219, 234, 254);
            grid.DefaultCellStyle.SelectionForeColor = System.Drawing.Color.FromArgb(31, 41, 55);
            grid.DefaultCellStyle.Padding = new System.Windows.Forms.Padding(8, 4, 8, 4);
            grid.RowTemplate.Height = 36;

            // Linhas alternadas
            grid.AlternatingRowsDefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(248, 250, 255);
        }

        private void MarcarBotaoAtivo(Button botaoAtivo)
        {
            var botoes = new List<Button>
    {
        btnLancamentos,
        btnCategorias,
        btnContas,
        btnSair
    };

            foreach (var botao in botoes)
            {
                botao.BackColor = _menuInativo;
                botao.ForeColor = _textoInativo;
            }

            botaoAtivo.BackColor = _menuAtivo;
            botaoAtivo.ForeColor = _textoAtivo;
        }

        private void ConfigurarMenuLateral()
        {
            var botoes = new List<Button>
    {
        btnLancamentos,
        btnCategorias,
        btnContas,
        btnSair
    };

            foreach (var botao in botoes)
            {
                botao.FlatStyle = FlatStyle.Flat;
                botao.FlatAppearance.BorderSize = 0;
                botao.FlatAppearance.MouseDownBackColor = _menuHover;
                botao.FlatAppearance.MouseOverBackColor = _menuHover;
                botao.BackColor = _menuSidebar;
                botao.ForeColor = _textoInativo;
                botao.TextAlign = ContentAlignment.MiddleLeft;
                botao.Padding = new System.Windows.Forms.Padding(14, 0, 0, 0);
                botao.Cursor = System.Windows.Forms.Cursors.Hand;
                botao.Font = new System.Drawing.Font("Segoe UI Semibold", 11F, System.Drawing.FontStyle.Bold);
            }
        }

        private void AtivarBotaoMenu(Button botaoAtivo)
        {
            var botoes = new List<Button>
    {
        btnLancamentos,
        btnCategorias,
        btnContas,
        btnSair
    };

            foreach (var botao in botoes)
            {
                botao.BackColor = _menuSidebar;
                botao.ForeColor = _textoInativo;
            }

            botaoAtivo.BackColor = _menuAtivo;
            botaoAtivo.ForeColor = System.Drawing.Color.White;
        }

        private void CarregarCompetencias()
        {
            cmbCompetencia.Items.Clear();

            var dataBase = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

            for (int i = -6; i <= 6; i++)
                cmbCompetencia.Items.Add(dataBase.AddMonths(i));

            cmbCompetencia.SelectedItem = dataBase;

            cmbCompetencia.Format -= cmbCompetencia_Format;
            cmbCompetencia.Format += cmbCompetencia_Format;
        }

        private void btnAcaoLancamentos_Click(object sender, EventArgs e)
        {
            using var frm = new FrmLancamentos();
            frm.ShowDialog();
            _ = CarregarDashboard();
        }

        private void btnAcaoNovo_Click(object sender, EventArgs e)
        {
            using var frm = new FrmNovoLancamento();
            if (frm.ShowDialog() == DialogResult.OK)
                _ = CarregarDashboard();
        }

        private void ConfigurarGrids()
        {
            dgvProximosVencimentos.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvProximosVencimentos.ReadOnly = true;
            dgvProximosVencimentos.AllowUserToAddRows = false;
            dgvProximosVencimentos.AllowUserToDeleteRows = false;
            dgvProximosVencimentos.AllowUserToResizeRows = false;
            dgvProximosVencimentos.RowHeadersVisible = false;
            dgvProximosVencimentos.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvProximosVencimentos.MultiSelect = false;

            dgvResumoCategorias.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvResumoCategorias.ReadOnly = true;
            dgvResumoCategorias.AllowUserToAddRows = false;
            dgvResumoCategorias.AllowUserToDeleteRows = false;
            dgvResumoCategorias.AllowUserToResizeRows = false;
            dgvResumoCategorias.RowHeadersVisible = false;
            dgvResumoCategorias.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvResumoCategorias.MultiSelect = false;

            EstilizarGrid(dgvProximosVencimentos);
            EstilizarGrid(dgvResumoCategorias);

            dgvProximosVencimentos.CellFormatting += dgvProximosVencimentos_CellFormatting;

            dgvResumoCategorias.CellClick += dgvResumoCategorias_CellClick;
            formsPlotCategorias.MouseClick += (s, e) => ResetarDestaqueCategorias();
        }

        private async Task CarregarDashboard()
        {
            if (cmbCompetencia.SelectedItem == null)
                return;

            var referencia = (DateTime)cmbCompetencia.SelectedItem;

            var resumo = await _dashboardService.ObterResumo(referencia);
            var proximosVenc = await _dashboardService.ObterProximosVencimentos();
            var gastosPorCategoria = await _dashboardService.ObterGastosPorCategoria(referencia);

            CarregarCards(resumo);
            CarregarGridProximosVencimentos(proximosVenc);
            CarregarGraficoCategorias(gastosPorCategoria);

        }

        private void CarregarGraficoCategorias(List<GastoCategoriaDto> dados)
        {
            var top = dados
        .OrderByDescending(x => x.Valor)
        .Take(6)
        .ToList();

            formsPlotCategorias.Reset();
            var plt = formsPlotCategorias.Plot;

            // Salva barras no campo da classe para o evento de clique acessar
            _barras = top.Select((item, i) => new ScottPlot.Bar
            {
                Value = (double)item.Valor,
                Position = i,
                FillColor = _cores[i % _cores.Length],
                Label = item.Valor.ToString("C0"),
                LineColor = ScottPlot.Colors.Transparent,
                Size = 0.6,
            }).ToList();

            var barPlot = plt.Add.Bars(_barras);
            barPlot.ValueLabelStyle.Bold = true;
            barPlot.ValueLabelStyle.FontSize = 11;
            barPlot.ValueLabelStyle.ForeColor = ScottPlot.Color.FromHex("#1F2937");
            barPlot.LabelsOnTop = true;

            // Eixo X — nomes das categorias
            var tickGen = new ScottPlot.TickGenerators.NumericManual();
            for (int i = 0; i < top.Count; i++)
                tickGen.AddMajor(i, top[i].Categoria);

            plt.Axes.Bottom.TickGenerator = tickGen;
            plt.Axes.Bottom.TickLabelStyle.FontSize = 11;
            plt.Axes.Bottom.TickLabelStyle.Bold = true;
            plt.Axes.Bottom.TickLabelStyle.ForeColor = ScottPlot.Color.FromHex("#1F2937");
            plt.Axes.Bottom.MajorTickStyle.Length = 0;
            plt.Axes.Bottom.FrameLineStyle.Color = ScottPlot.Color.FromHex("#E2E8F0");

            // Eixo Y — formato R$
            plt.Axes.Left.TickLabelStyle.FontSize = 10;
            plt.Axes.Left.TickLabelStyle.ForeColor = ScottPlot.Color.FromHex("#6B7280");
            plt.Axes.Left.MajorTickStyle.Length = 0;
            plt.Axes.Left.FrameLineStyle.IsVisible = false;

            var tickGenY = new ScottPlot.TickGenerators.NumericAutomatic();
            tickGenY.LabelFormatter = v => v.ToString("C0");
            plt.Axes.Left.TickGenerator = tickGenY;

            // Grid
            plt.Grid.MajorLineColor = ScottPlot.Color.FromHex("#E2E8F0");
            plt.Grid.MajorLineWidth = 1;
            plt.Grid.XAxisStyle.IsVisible = false;

            // Fundo
            plt.FigureBackground.Color = ScottPlot.Colors.White;
            plt.DataBackground.Color = ScottPlot.Colors.White;

            // Margens
            plt.Axes.Margins(bottom: 0, top: 0.25, left: 0.02, right: 0.02);

            // Remove legenda
            plt.Legend.IsVisible = false;

            formsPlotCategorias.Refresh();

            // Tabela resumo
            dgvResumoCategorias.DataSource = dados
                .OrderByDescending(x => x.Valor)
                .Select(x => new
                {
                    Categoria = x.Categoria,
                    Valor = x.Valor.ToString("C2")
                })
                .ToList();
        }

        private void ResetarDestaqueCategorias()
        {
            for (int i = 0; i < _barras.Count; i++)
            {
                _barras[i].FillColor = _cores[i % _cores.Length];
                _barras[i].LineColor = ScottPlot.Colors.Transparent;
                _barras[i].LineWidth = 0;
                _barras[i].Size = 0.6;
            }

            dgvResumoCategorias.ClearSelection();
            formsPlotCategorias.Refresh();
        }

        private void ConfigurarGrafico()
        {
            // Remove borda padrão do controle WinForms
            formsPlotCategorias.BackColor = System.Drawing.Color.White;
            formsPlotCategorias.BorderStyle = BorderStyle.None;

            // Desabilita interação de zoom/pan (não faz sentido num dashboard)
            formsPlotCategorias.UserInputProcessor.IsEnabled = false;
            formsPlotCategorias.Plot.Axes.ContinuouslyAutoscale = false;
        }

        private void CarregarGridProximosVencimentos(List<ProximoVencimentoDto> dados)
        {
            dgvProximosVencimentos.DataSource = dados
                .Select(x => new
                {
                    Vencimento = x.Vencimento.HasValue ? x.Vencimento.Value.ToString("dd/MM/yyyy") : "",
                    x.Descricao,
                    Valor = x.Valor.ToString("C2"),
                    x.Conta,
                    x.Pessoa,
                    x.Status
                })
                .ToList();
        }

        private void CarregarCards(DashboardResumo resumo)
        {
            lblTotalEntradaValor.Text = resumo.TotalEntradas.ToString("C2");
            lblTotalEntradaQtd.Text = $"{resumo.QuantidadeEntradas} lançamentos";

            lblTotalSaidaValor.Text = resumo.TotalSaidas.ToString("C2");
            lblTotalSaidaQtd.Text = $"{resumo.QuantidadeSaidas} lançamentos";

            lblSaldoMesValor.Text = resumo.SaldoMes.ToString("C2");
            lblSaldoMesQtd.Text = $"{resumo.TotalLancamentosMes} lançamentos";

            lblSemanaValor.Text = resumo.TotalSemana.ToString("C2");
            lblSemanaQtd.Text = $"{resumo.QuantidadeSemana} vencimentos";

            lblResumoTotalLancamentos.Text = $"Total de Lançamentos: {resumo.TotalLancamentosMes}";
            lblResumoTotalPago.Text = $"Saídas Pagas: {resumo.TotalPago:C2}";
            lblResumoTotalPendente.Text = $"Saídas Pendentes: {resumo.TotalPendente:C2}";
            lblResumoAtrasados.Text = $"Atrasados: {resumo.TotalAtrasados:C2}";
            lblResumoVencemHoje.Text = $"Vencem Hoje: {resumo.TotalVencemHoje:C2}";
            lblResumoVencemSemana.Text = $"Próximos 7 dias: {resumo.TotalSemana:C2}";
        }

        private void cmbCompetencia_Format(object? sender, ListControlConvertEventArgs e)
        {
            if (e.ListItem is DateTime dt)
                e.Value = dt.ToString("MMMM/yyyy", new CultureInfo("pt-BR"));
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            EstilizarBotaoMenu(btnLancamentos);
            EstilizarBotaoMenu(btnCategorias);
            EstilizarBotaoMenu(btnContas);
            EstilizarBotaoMenu(btnSair);

            ConfigurarGrafico();
            CarregarCompetencias();
            ConfigurarMenuLateral();
            AplicarBordasArredondadas();
            ConfigurarGrids();
            await CarregarDashboard();
        }

        private void btnVerTodos_Click_1(object sender, EventArgs e)
        {
            using var frm = new FrmLancamentos();
            frm.ShowDialog();
            _ = CarregarDashboard();
        }

        private void btnLancamentos_Click(object sender, EventArgs e)
        {
            AtivarBotaoMenu(btnLancamentos);
            MarcarBotaoAtivo(btnLancamentos);
            using var frm = new FrmLancamentos();
            frm.ShowDialog();

            _ = CarregarDashboard();
            LimparSelecaoMenu();
        }

        private void LimparSelecaoMenu()
        {
            var botoes = new List<Button>
    {
        btnLancamentos,
        btnCategorias,
        btnContas,
        btnSair
    };

            foreach (var botao in botoes)
            {
                botao.BackColor = _menuSidebar;
                botao.ForeColor = _menuInativo;
            }
        }

        private void btnCategorias_Click(object sender, EventArgs e)
        {

        }

        private void btnContas_Click(object sender, EventArgs e)
        {

        }

        private void btnSair_Click(object sender, EventArgs e)
        {
            AtivarBotaoMenu(btnSair);
            MarcarBotaoAtivo(btnSair);
            var resultado = MessageBox.Show(
                "Deseja realmente sair do sistema?",
                "Sair",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (resultado == DialogResult.Yes)
                Close();
            else
                LimparSelecaoMenu();
        }

        private void dgvProximosVencimentos_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (dgvProximosVencimentos.Columns[e.ColumnIndex].Name == "Status" && e.Value != null)
            {
                var status = e.Value.ToString();
                var cell = dgvProximosVencimentos.Rows[e.RowIndex].DefaultCellStyle;

                switch (status)
                {
                    case "Atrasado":
                        cell.ForeColor = System.Drawing.Color.FromArgb(220, 38, 38);   // vermelho
                        cell.BackColor = System.Drawing.Color.FromArgb(254, 242, 242);
                        break;
                    case "Pendente":
                        cell.ForeColor = System.Drawing.Color.FromArgb(180, 83, 9);    // laranja
                        cell.BackColor = System.Drawing.Color.FromArgb(255, 251, 235);
                        break;
                    case "Pago":
                        cell.ForeColor = System.Drawing.Color.FromArgb(21, 128, 61);   // verde
                        cell.BackColor = System.Drawing.Color.FromArgb(240, 253, 244);
                        break;
                }
            }
        }

        private List<ScottPlot.Bar> _barras = new();
        private ScottPlot.Color[] _cores = new[]
        {
            ScottPlot.Color.FromHex("#378ADD"),
            ScottPlot.Color.FromHex("#1D9E75"),
            ScottPlot.Color.FromHex("#EF9F27"),
            ScottPlot.Color.FromHex("#D85A30"),
            ScottPlot.Color.FromHex("#7F77DD"),
            ScottPlot.Color.FromHex("#D4537E"),
        };

        private void dgvResumoCategorias_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            
        }

        private void dgvResumoCategorias_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _barras.Count)
                return;

            for (int i = 0; i < _barras.Count; i++)
            {
                if (i == e.RowIndex)
                {
                    // Barra selecionada — mantém cor original e aumenta brilho
                    _barras[i].FillColor = _cores[i % _cores.Length];
                    _barras[i].LineColor = ScottPlot.Color.FromHex("#1F2937");
                    _barras[i].LineWidth = 2;
                    _barras[i].Size = 0.65;
                }
                else
                {
                    // Demais barras — esmaece
                    _barras[i].FillColor = _cores[i % _cores.Length].WithAlpha(0.3f);
                    _barras[i].LineColor = ScottPlot.Colors.Transparent;
                    _barras[i].LineWidth = 0;
                    _barras[i].Size = 0.6;
                }
            }

            formsPlotCategorias.Refresh();
        }
    }
}
