using FinanceiroPessoal.WinForms.Models;
using FinanceiroPessoal.WinForms.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Text;
using System.Windows.Forms;

namespace FinanceiroPessoal.WinForms
{
    public partial class FrmNovoLancamento : Form
    {
        private readonly LancamentoService _lancamentoService;
        private readonly CadastroAuxiliarService _cadastroAuxiliarService;
        private bool _carregandoCombos = false;
        public FrmNovoLancamento()
        {
            try
            {
                var tipoBanco = TipoBanco.OnlineMySql; // Mude para OnlineMySql se quiser
                var lancamentoRepo = DatabaseFactory.CriarLancamentoRepository(tipoBanco);
                var cadastroRepo = DatabaseFactory.CriarCadastroAuxiliarRepository(tipoBanco);

                _lancamentoService = new LancamentoService(lancamentoRepo);
                _cadastroAuxiliarService = new CadastroAuxiliarService(cadastroRepo);

                InitializeComponent();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Erro ao inicializar FrmNovoLancamento");
                throw;
            }
        }

        private async void FrmNovoLancamento_Load(object sender, EventArgs e)
        {
            await CarregagarCombos();
            await CarregarLabels();
        }

        private async Task CarregarLabels()
        {
            try
            {
                var totalPago = await _lancamentoService.ObterTotalPagoSaidas();
                var totalPendente = await _lancamentoService.ObterTotalPendenteSaidas();
                var geral = totalPago + totalPendente;

                lblTotalPago.Text = $"Total Pago: R$ {totalPago:N2}";
                lblTotalGeral.Text = $"Total Geral: R$ {geral:N2}";
                lblTotalPendente.Text = $"Total Pendente: R$ {totalPendente:N2}";
            }
            catch (Exception ex)
            {
                ZerarLabels();
                MessageBox.Show(ex.ToString(), "Erro ao carregar totais");
            }
        }

        private void ZerarLabels()
        {
            lblTotalPago.Text = "Total Pago: R$ 0,00";
            lblTotalGeral.Text = "Total Geral: R$ 0,00";
            lblTotalPendente.Text = "Total Pendente: R$ 0,00";
        }

        private async Task CarregagarCombos()
        {
            try
            {
                _carregandoCombos = true;
                cmbTipo.Items.Clear();
                cmbTipo.Items.Add("Entrada");
                cmbTipo.Items.Add("Saída");
                cmbTipo.SelectedIndex = 1;

                cmbStatus.Items.Clear();
                cmbStatus.Items.Add("Pendente");
                cmbStatus.Items.Add("Pago");
                cmbStatus.Items.Add("Atrasado");
                cmbStatus.Items.Add("Parcial");
                cmbStatus.SelectedIndex = 0;

                cmbCategoria.DataSource = await _cadastroAuxiliarService.ObterCategorias();
                cmbCategoria.DisplayMember = "Nome";
                cmbCategoria.ValueMember = "Id";
                cmbCategoria.SelectedIndex = -1;

                cmbConta.DataSource = await _cadastroAuxiliarService.ObterContas();
                cmbConta.DisplayMember = "Nome";
                cmbConta.ValueMember = "Id";
                cmbConta.SelectedIndex = -1;

                cmbPessoa.DataSource = await _cadastroAuxiliarService.ObterPessoas();
                cmbPessoa.DisplayMember = "Nome";
                cmbPessoa.ValueMember = "Id";
                cmbPessoa.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Erro ao carregar combos em FrmNovoLancamento");
            }
            finally
            {
                _carregandoCombos = false;
                AtualizarComportamentoTipo();
            }

        }

        private void AtualizarComportamentoTipo()
        {
            if (cmbTipo.Items.Count == 0 || cmbStatus.Items.Count == 0)
                return;

            bool isEntrada = cmbTipo.Text == "Entrada";
            cmbStatus.Enabled = !isEntrada;

            if (isEntrada)
            {
                cmbStatus.SelectedItem = "Pago";
                txtValor.ForeColor = Color.Green; // Verde para entradas
            }
            else
            {
                txtValor.ForeColor = Color.Red; // Vermelho para saídas
                if (cmbStatus.SelectedIndex < 0)
                    cmbStatus.SelectedIndex = 0;
            }
        }

        private async void btnSalvar_Click(object sender, EventArgs e)
        {
            try
            {
                if (!ValidarFormulario())
                    return;
                int qtdParcelas = 1;
                if (!string.IsNullOrEmpty(dudParcelas.Text) && int.TryParse(dudParcelas.Text, out qtdParcelas)) ;

                string primeiraParcela = cmbStatus.Text;
                DateTime competenicaAtual;
                var competenciaTexto = txtCompetencia.Text.Trim();
                if (!DateTime.TryParseExact(
                    "01/" + competenciaTexto,
                    "dd/MM/yyyy",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out competenicaAtual))
                {
                    competenicaAtual = dtpVencimento.Value;
                }

                DateTime VencimentoBase = dtpVencimento.Value.Date;

                var tipo = cmbTipo.Text == "Entrada"
                    ? TipoLancamento.Entrada
                    : TipoLancamento.Saida;

                var status = tipo == TipoLancamento.Entrada
                    ? "Pago"
                    : cmbStatus.Text;

                for (int i = 1; i <= qtdParcelas; i++)
                {
                    var lancamento = new Lancamento
                    {
                        Descricao = $"{txtDescricao.Text.Trim()} - Parcela {i} de {qtdParcelas}",
                        Valor = ParseDecimal(txtValor.Text),
                        DataVencimento = VencimentoBase.AddMonths(i - 1),
                        Status = (i == 1) ? primeiraParcela : "Pendente",
                        Tipo = tipo,
                        CategoriaId = cmbCategoria.SelectedValue != null ? Convert.ToInt32(cmbCategoria.SelectedValue) : null,
                        ContaId = cmbConta.SelectedValue != null ? Convert.ToInt32(cmbConta.SelectedValue) : null,
                        PessoaId = cmbPessoa.SelectedValue != null ? Convert.ToInt32(cmbPessoa.SelectedValue) : null,
                        Observacoes = string.IsNullOrWhiteSpace(txtObservacoes.Text) ? null : txtObservacoes.Text.Trim(),
                        Competencia = competenicaAtual.AddMonths(i - 1).ToString("MM/yyyy"),
                        DataPagamento = (i == 1 && primeiraParcela == "Pago") ? DateTime.Now : null
                    };
                    await _lancamentoService.Adicionar(lancamento);
                }

                MessageBox.Show("Lançamento(s) salvo(s) com sucesso.", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Erro ao salvar lançamento");
            }
        }

        private decimal ParseDecimal(string texto)
        {
            texto = texto.Replace("R$", "").Trim();

            return decimal.Parse(texto, NumberStyles.Any, new CultureInfo("pt-BR"));
        }

        private bool ValidarFormulario()
        {
            if (string.IsNullOrWhiteSpace(txtDescricao.Text))
            {
                MessageBox.Show("Informe a descrição.", "Validação", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtDescricao.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtValor.Text))
            {
                MessageBox.Show("Informe o valor.", "Validação", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtValor.Focus();
                return false;
            }

            if (!decimal.TryParse(txtValor.Text.Replace("R$", "").Trim(), NumberStyles.Any, new CultureInfo("pt-BR"), out _))
            {
                MessageBox.Show("Valor inválido.", "Validação", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtValor.Focus();
                return false;
            }

            if (cmbTipo.SelectedIndex < 0)
            {
                MessageBox.Show("Selecione o tipo.", "Validação", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cmbTipo.Focus();
                return false;
            }

            if (cmbTipo.Text == "Saída" && cmbStatus.SelectedIndex < 0)
            {
                MessageBox.Show("Selecione o status.", "Validação", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cmbStatus.Focus();
                return false;
            }

            return true;
        }

        private void btnCancelar_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void cmbTipo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_carregandoCombos)
                return;
            AtualizarComportamentoTipo();
        }
    }
}
