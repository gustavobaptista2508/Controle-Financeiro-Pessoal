using FinanceiroPessoal.WinForms.Models;
using FinanceiroPessoal.WinForms.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace FinanceiroPessoal.WinForms
{
    public partial class FrmLancamentos : Form
    {
        private readonly LancamentoService _service;
        private readonly CadastroAuxiliarService _cadastroService;
        public FrmLancamentos()
        {
            var tipoBanco = TipoBanco.OnlineMySql; // ✅ Configuração centralizada
            var lancamentoRepo = DatabaseFactory.CriarLancamentoRepository(tipoBanco);
            var cadastroRepo = DatabaseFactory.CriarCadastroAuxiliarRepository(tipoBanco);
            _service = new LancamentoService(lancamentoRepo);
            _cadastroService = new CadastroAuxiliarService(cadastroRepo);
            InitializeComponent();
            // Vincula eventos ANTES de carregar dados
            dgvLancamentos.CellPainting += dgvLancamentos_CellPainting;
            dgvLancamentos.CellFormatting += dgvLancamentos_CellFormatting;


        }

        private async void FrmLancamentos_Load(object sender, EventArgs e)
        {
            dtpDataInicio.Value = DateTime.Now.AddMonths(-1);
            dtpDataFim.Value = DateTime.Now;
            await CarregarFiltros();
            await CarregarGrid();
        }

        private async Task AtualizarDashboard()
        {
            try
            {
                var lancamentos = await _service.Filtrar(
                    cmbFiltroPessoa.Text,
                    cmbFiltroStatus.Text,
                    cmbFiltroTipo.Text,
                    dtpDataInicio.Value,
                    dtpDataFim.Value);

                if (!lancamentos.Any())
                {
                    ZerarDashboard();
                    return;
                }

                // ✅ NOVOS TOTALS OTIMIZADOS
                var totalPendente = await _service.ObterTotalPendenteSaidas();
                var totalPago = await _service.ObterTotalPagoSaidas();

                var saldo = await _service.CalcularSaldoConta(
                    cmbFiltroPessoa.Text, cmbFiltroStatus.Text, cmbFiltroTipo.Text,
                    dtpDataInicio.Value, dtpDataFim.Value);

                // Atualiza labels
                lblTotalGeral.Text = $"💰 SAÍDAS: R$ {totalPendente + totalPago:N2}";
                lblPagos.Text = $"🟢 PAGAS: R$ {totalPago:N2}";
                lblPendentes.Text = $"🟡 PENDENTES: R$ {totalPendente:N2}";
                lblSaldoConta.Text = $"💳 SALDO: R$ {saldo:N2}";

                // Cores
                lblSaldoConta.ForeColor = saldo >= 0 ? Color.Green : Color.Red;
            }
            catch (Exception ex)
            {
                ZerarDashboard();
                System.Diagnostics.Debug.WriteLine($"Dashboard Error: {ex.Message}");
            }
        }

        private void ZerarDashboard()
        {
            lblTotalGeral.Text = "💰 SAÍDAS: R$ 0,00";
            lblPagos.Text = "🟢 PAGAS: R$ 0,00 (0%)";
            lblPendentes.Text = "🟡 PENDENTES: R$ 0,00";
            lblAtrasados.Text = "🔴 ATRASADOS: R$ 0,00";
        }

        private async Task CarregarGrid()
        {
            try
            {
                var lancamentos = await _service.Filtrar(
                    cmbFiltroPessoa.Text,
                    cmbFiltroStatus.Text,
                    cmbFiltroTipo.Text,
                    dtpDataInicio.Value,
                    dtpDataFim.Value);
                var dados = lancamentos.Select(x => new
                {
                    x.Id,
                    Tipo = x.Tipo == TipoLancamento.Entrada ? "Entrada" : "Saída",
                    x.Descricao,
                    Valor = x.Valor.ToString("N2"),
                    Vencimento = x.DataVencimento.HasValue ? x.DataVencimento.Value.ToString("dd/MM/yyyy") : "",
                    StatusIcone = x.Status.ToString(),  // ✅ Garantir que é string
                    Categoria = x.Categoria?.Nome ?? "",
                    Conta = x.Conta?.Nome ?? "",
                    Pessoa = x.Pessoa?.Nome ?? "",
                    x.Competencia
                })
                .ToList();

                dgvLancamentos.DataSource = null;
                dgvLancamentos.DataSource = dados;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar a tabela de lançamentos: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                ConfigurarColunas();
                // ✅ Refresh para aplicar formatação
                dgvLancamentos.Refresh();
                AtualizarDashboard();
            }
        }

        private void ConfigurarColunas()
        {
            // StatusIcone como primeira coluna (ícone)
            if (dgvLancamentos.Columns["StatusIcone"] != null)
            {
                dgvLancamentos.Columns["StatusIcone"].DisplayIndex = 0; // Primeira coluna
                dgvLancamentos.Columns["StatusIcone"].Width = 45;
                dgvLancamentos.Columns["StatusIcone"].HeaderText = "Status";
                dgvLancamentos.Columns["StatusIcone"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                dgvLancamentos.Columns["StatusIcone"].SortMode = DataGridViewColumnSortMode.NotSortable;
            }

            // Id em segunda posição
            if (dgvLancamentos.Columns["Id"] != null)
            {
                dgvLancamentos.Columns["Id"].DisplayIndex = 1;
                dgvLancamentos.Columns["Id"].Width = 60;
            }

            // Suas outras configurações...
            if (dgvLancamentos.Columns["Tipo"] != null)
                dgvLancamentos.Columns["Tipo"].Width = 90;
            if (dgvLancamentos.Columns["Descricao"] != null)
                dgvLancamentos.Columns["Descricao"].Width = 250;
        }

        private async Task CarregarFiltros()
        {
            try
            {
                var pessoas = await _cadastroService.ObterPessoas();

                cmbFiltroPessoa.Items.Clear();
                cmbFiltroPessoa.Items.Add("Todos");

                foreach (var pessoa in pessoas)
                    cmbFiltroPessoa.Items.Add(pessoa.Nome);

                cmbFiltroPessoa.SelectedIndex = 0;

                cmbFiltroStatus.Items.Clear();
                cmbFiltroStatus.Items.Add("Todos");
                cmbFiltroStatus.Items.Add("Pendente");
                cmbFiltroStatus.Items.Add("Pago");
                cmbFiltroStatus.Items.Add("Atrasado");
                cmbFiltroStatus.Items.Add("Parcial");
                cmbFiltroStatus.SelectedIndex = 0;

                cmbFiltroTipo.Items.Clear();
                cmbFiltroTipo.Items.Add("Todos");
                cmbFiltroTipo.Items.Add("Entrada");
                cmbFiltroTipo.Items.Add("Saída");
                cmbFiltroTipo.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar filtros: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private int? ObterIdSelecionado()
        {
            if (dgvLancamentos.CurrentRow == null)
                return null;

            return Convert.ToInt32(dgvLancamentos.CurrentRow.Cells["Id"].Value);
        }

        private async void btnNovo_Click(object sender, EventArgs e)
        {
            using var frm = new FrmNovoLancamento();
            if (frm.ShowDialog() == DialogResult.OK)
                await CarregarGrid();
        }

        private async void btnExcluir_Click(object sender, EventArgs e)
        {
            var id = ObterIdSelecionado();
            if (id == null)
            {
                MessageBox.Show("Selecione um lançamento.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (MessageBox.Show("Deseja excluir?", "Confirmação", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                await _service.Excluir(id.Value); // ✅ Assumindo que existe
                await CarregarGrid();
            }
        }

        private async void btnMarcarPago_Click(object sender, EventArgs e)
        {
            var id = ObterIdSelecionado();
            if (id == null)
            {
                MessageBox.Show("Selecione um lançamento.", "Aviso");
                return;
            }

            await _service.MarcarComoPago(id.Value); // ✅ Assumindo que existe
            await CarregarGrid();
        }

        private async void btnAtualizar_Click(object sender, EventArgs e)
        {
            await CarregarGrid();
        }

        private async void btnFiltrar_Click(object sender, EventArgs e)
        {
            await CarregarGrid();
        }

        private async void btnEditar_Click(object sender, EventArgs e)
        {
            var id = ObterIdSelecionado();
            if (id == null)
            {
                MessageBox.Show("Selecione um lançamento.", "Aviso");
                return;
            }

            using var frm = new FrmEditarLancamento(id.Value);
            if (frm.ShowDialog() == DialogResult.OK)
                await CarregarGrid();
        }

        private void dgvLancamentos_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                var row = dgvLancamentos.Rows[e.RowIndex];
                var statusCell = row.Cells["StatusIcone"];

                if (statusCell?.Value != null)
                {
                    var status = statusCell.Value.ToString();
                    var backColor = status switch
                    {
                        "Pago" => Color.FromArgb(245, 255, 250),  // Verde bem claro
                        "Pendente" => Color.FromArgb(255, 255, 235), // Amarelo bem claro
                        "Atrasado" => Color.FromArgb(255, 245, 245), // Vermelho bem claro
                        "Parcial" => Color.FromArgb(240, 248, 255),  // Azul bem claro
                        _ => dgvLancamentos.DefaultCellStyle.BackColor
                    };

                    dgvLancamentos.Rows[e.RowIndex].DefaultCellStyle.BackColor = backColor;

                    if (status == "Atrasado")
                        dgvLancamentos.Rows[e.RowIndex].DefaultCellStyle.ForeColor = Color.DarkRed;
                }
            }
        }

        private void dgvLancamentos_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            try
            {
                // Verificações completas
                if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
                if (dgvLancamentos.ColumnCount == 0) return;

                var colunaStatus = dgvLancamentos.Columns["StatusIcone"];
                if (colunaStatus == null || colunaStatus.Index != e.ColumnIndex) return;

                var row = dgvLancamentos.Rows[e.RowIndex];
                var statusCell = row.Cells["StatusIcone"];
                if (statusCell?.Value == null) return;

                e.PaintBackground(e.CellBounds, true);

                var status = statusCell.Value.ToString();
                var cor = ObterCorBolinha(status);

                // Bolinha perfeitamente centralizada
                var diametro = 14;
                var x = e.CellBounds.X + (e.CellBounds.Width - diametro) / 2;
                var y = e.CellBounds.Y + (e.CellBounds.Height - diametro) / 2;

                var rect = new Rectangle(x, y, diametro, diametro);

                using (var brush = new SolidBrush(cor))
                    e.Graphics.FillEllipse(brush, rect);

                // Borda sutil
                using (var pen = new Pen(Color.FromArgb(120, Color.Black), 1))
                    e.Graphics.DrawEllipse(pen, rect);

                e.Handled = true;
            }
            catch (Exception)
            {
                // Ignora erros de pintura
                e.Handled = true;
            }
        }

        private Color ObterCorBolinha(string? status)
        {
            return status switch
            {
                "Pago" => Color.FromArgb(46, 204, 113),
                "Pendente" => Color.FromArgb(241, 196, 15),
                "Atrasado" => Color.FromArgb(231, 76, 60),
                "Parcial" => Color.FromArgb(52, 152, 219),
                _ => Color.Gray
            };
        }

        private void btnVoltar_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
