using FinanceiroPessoal.WinForms.Services;
using System.Globalization;
using System.Windows.Forms.DataVisualization.Charting;

namespace FinanceiroPessoal.WinForms
{
    public partial class Form1 : Form
    {
        private readonly DashboardService _dashboardService = new();

        private readonly Color _menuSidebar = Color.White;
        private readonly Color _menuAtivo = Color.FromArgb(37, 99, 235);      // azul
        private readonly Color _menuInativo = Color.White;                    // fundo normal
        private readonly Color _menuHover = Color.FromArgb(239, 244, 255);    // hover
        private readonly Color _textoAtivo = Color.White;
        private readonly Color _textoInativo = Color.FromArgb(31, 41, 55);
        public Form1()
        {
            InitializeComponent();

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
            botao.Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold);
            botao.TextAlign = ContentAlignment.MiddleLeft;
            botao.Padding = new Padding(16, 0, 0, 0);
            botao.Cursor = Cursors.Hand;
            botao.Height = 46;
            botao.UseVisualStyleBackColor = false;
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
                botao.Padding = new Padding(14, 0, 0, 0);
                botao.Cursor = Cursors.Hand;
                botao.Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold);
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
            botaoAtivo.ForeColor = Color.White;
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
            CarregarDashboard();
        }

        private void btnAcaoNovo_Click(object sender, EventArgs e)
        {
            using var frm = new FrmNovoLancamento();
            if (frm.ShowDialog() == DialogResult.OK)
                CarregarDashboard();
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
        }

        private void CarregarDashboard()
        {
            if (cmbCompetencia.SelectedItem == null)
                return;

            var referencia = (DateTime)cmbCompetencia.SelectedItem;

            CarregarCards(referencia);
            CarregarGridProximosVencimentos();
            CarregarGraficoCategorias(referencia);
            //AtualizarRodape();
        }

        private void CarregarGraficoCategorias(DateTime referencia)
        {
            var dados = _dashboardService.ObterGastosPorCategoria(referencia);

            chartCategorias.Series.Clear();
            chartCategorias.ChartAreas.Clear();
            chartCategorias.Legends.Clear();

            var chartArea = new ChartArea("AreaPrincipal");
            chartArea.BackColor = Color.White;
            chartArea.Area3DStyle.Enable3D = false;
            chartCategorias.ChartAreas.Add(chartArea);

            var legend = new Legend("Legenda");
            legend.Docking = Docking.Right;
            legend.Font = new Font("Segoe UI", 9F);
            chartCategorias.Legends.Add(legend);

            var series = new Series("Categorias")
            {
                ChartType = SeriesChartType.Doughnut,
                IsValueShownAsLabel = true,
                LabelFormat = "C0",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                LegendText = "#AXISLABEL"
            };

            foreach (var item in dados)
                series.Points.AddXY(item.Categoria, item.Valor);

            chartCategorias.Series.Add(series);

            dgvResumoCategorias.DataSource = dados
                .Select(x => new
                {
                    Categoria = x.Categoria,
                    Valor = x.Valor.ToString("C2")
                })
                .ToList();
        }

        private void CarregarGridProximosVencimentos()
        {
            var dados = _dashboardService.ObterProximosVencimentos();

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

        private void CarregarCards(DateTime referencia)
        {
            var resumo = _dashboardService.ObterResumo(referencia);

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

        private void cmbCompetencia_Format(object sender, ListControlConvertEventArgs e)
        {
            if (e.ListItem is DateTime dt)
                e.Value = dt.ToString("MMMM/yyyy", new CultureInfo("pt-BR"));
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            EstilizarBotaoMenu(btnLancamentos);
            EstilizarBotaoMenu(btnCategorias);
            EstilizarBotaoMenu(btnContas);
            EstilizarBotaoMenu(btnSair);

            CarregarCompetencias();
            ConfigurarGrids();
            CarregarDashboard();
        }

        private void btnVerTodos_Click_1(object sender, EventArgs e)
        {
            using var frm = new FrmLancamentos();
            frm.ShowDialog();
            CarregarDashboard();
        }

        private void btnLancamentos_Click(object sender, EventArgs e)
        {
            AtivarBotaoMenu(btnLancamentos);
            using var frm = new FrmLancamentos();
            frm.ShowDialog();

            CarregarDashboard();
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
    }
}
