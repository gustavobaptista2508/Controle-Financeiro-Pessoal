using FinanceiroPessoal.WinForms.Services;
using System.Globalization;
using System.Windows.Forms.DataVisualization.Charting;

namespace FinanceiroPessoal.WinForms
{
    public partial class Form1 : Form
    {
        private readonly DashboardService _dashboardService = new();
        public Form1()
        {
            InitializeComponent();
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
            dgvProximosVencimentos.BackgroundColor = Color.White;
            dgvProximosVencimentos.BorderStyle = BorderStyle.None;
            dgvProximosVencimentos.EnableHeadersVisualStyles = false;
            dgvProximosVencimentos.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(245, 247, 250);
            dgvProximosVencimentos.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(55, 65, 81);
            dgvProximosVencimentos.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 10F);
            dgvProximosVencimentos.DefaultCellStyle.Font = new Font("Segoe UI", 10F);
            dgvProximosVencimentos.DefaultCellStyle.SelectionBackColor = Color.FromArgb(230, 240, 255);
            dgvProximosVencimentos.DefaultCellStyle.SelectionForeColor = Color.FromArgb(31, 41, 55);
            dgvProximosVencimentos.GridColor = Color.FromArgb(235, 238, 242);

            dgvResumoCategorias.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvResumoCategorias.ReadOnly = true;
            dgvResumoCategorias.AllowUserToAddRows = false;
            dgvResumoCategorias.AllowUserToDeleteRows = false;
            dgvResumoCategorias.AllowUserToResizeRows = false;
            dgvResumoCategorias.RowHeadersVisible = false;
            dgvResumoCategorias.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvResumoCategorias.MultiSelect = false;
            dgvResumoCategorias.BackgroundColor = Color.White;
            dgvResumoCategorias.BorderStyle = BorderStyle.None;
            dgvResumoCategorias.EnableHeadersVisualStyles = false;
            dgvResumoCategorias.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(245, 247, 250);
            dgvResumoCategorias.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(55, 65, 81);
            dgvResumoCategorias.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 10F);
            dgvResumoCategorias.DefaultCellStyle.Font = new Font("Segoe UI", 10F);
            dgvResumoCategorias.DefaultCellStyle.SelectionBackColor = Color.FromArgb(230, 240, 255);
            dgvResumoCategorias.DefaultCellStyle.SelectionForeColor = Color.FromArgb(31, 41, 55);
            dgvResumoCategorias.GridColor = Color.FromArgb(235, 238, 242);
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

            lblTotalPendenteValor.Text = resumo.TotalPendente.ToString("C2");
            lblTotalPendenteQtd.Text = $"{resumo.QuantidadePendentes} lançamentos";

            lblTotalPagoValor.Text = resumo.TotalPago.ToString("C2");
            lblTotalPagoQtd.Text = $"{resumo.QuantidadePagos} lançamentos";

            lblSemanaValor.Text = resumo.TotalSemana.ToString("C2");
            lblSemanaQtd.Text = $"{resumo.QuantidadeSemana} lançamentos";

            lblMesValor.Text = resumo.TotalMes.ToString("C2");
            lblMesQtd.Text = $"{resumo.QuantidadeMes} lançamentos";

            lblResumoTotalLancamentos.Text = $"Total de Lançamentos: {resumo.TotalLancamentosMes}";
            lblResumoTotalPago.Text = $"Total Pago: {resumo.TotalPago:C2}";
            lblResumoTotalPendente.Text = $"Total Pendente: {resumo.TotalPendente:C2}";
            lblResumoAtrasados.Text = $"Atrasados: {resumo.TotalAtrasados:C2}";
            lblResumoVencemHoje.Text = $"Vencem Hoje: {resumo.TotalVencemHoje:C2}";
            lblResumoVencemSemana.Text = $"Vencem esta Semana: {resumo.TotalVencemSemanaResumo:C2}";
        }

        private void cmbCompetencia_Format(object sender, ListControlConvertEventArgs e)
        {
            if (e.ListItem is DateTime dt)
                e.Value = dt.ToString("MMMM/yyyy", new CultureInfo("pt-BR"));
        }

        private void Form1_Load(object sender, EventArgs e)
        {
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
    }
}
