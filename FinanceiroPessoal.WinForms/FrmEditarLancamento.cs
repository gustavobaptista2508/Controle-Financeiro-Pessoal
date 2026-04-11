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
    public partial class FrmEditarLancamento : Form
    {
        private readonly int _lancamentoId;
        private readonly LancamentoService _lancamentoService = new();
        private readonly CadastroAuxiliarService _cadastroAuxiliarService = new();
        public FrmEditarLancamento(int lancamentoId)
        {
            _lancamentoId = lancamentoId;
            InitializeComponent();
        }

        private void FrmEditarLancamento_Load(object sender, EventArgs e)
        {
            CarregarCombos();
            CarregarDados();
        }

        private void CarregarCombos()
        {
            cmbTipo.Items.Clear();
            cmbTipo.Items.Add("Entrada");
            cmbTipo.Items.Add("Saída");

            cmbStatus.Items.Clear();
            cmbStatus.Items.Add("Pendente");
            cmbStatus.Items.Add("Pago");
            cmbStatus.Items.Add("Atrasado");
            cmbStatus.Items.Add("Parcial");

            cmbCategoria.DataSource = _cadastroAuxiliarService.ObterCategorias();
            cmbCategoria.DisplayMember = "Nome";
            cmbCategoria.ValueMember = "Id";
            cmbCategoria.SelectedIndex = -1;

            cmbConta.DataSource = _cadastroAuxiliarService.ObterContas();
            cmbConta.DisplayMember = "Nome";
            cmbConta.ValueMember = "Id";
            cmbConta.SelectedIndex = -1;

            cmbPessoa.DataSource = _cadastroAuxiliarService.ObterPessoas();
            cmbPessoa.DisplayMember = "Nome";
            cmbPessoa.ValueMember = "Id";
            cmbPessoa.SelectedIndex = -1;
        }

        private void CarregarDados()
        {
            var lancamento = _lancamentoService.ObterPorId(_lancamentoId);

            if (lancamento == null)
            {
                MessageBox.Show("Lançamento não encontrado.", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
                return;
            }

            txtDescricao.Text = lancamento.Descricao;
            txtValor.Text = lancamento.Valor.ToString("N2");
            dtpVencimento.Value = lancamento.DataVencimento ?? DateTime.Now;
            txtCompetencia.Text = lancamento.Competencia ?? "";
            txtObservacoes.Text = lancamento.Observacoes ?? "";
            cmbStatus.Text = lancamento.Status;
            cmbTipo.Text = lancamento.Tipo == TipoLancamento.Entrada ? "Entrada" : "Saída";

            if (lancamento.CategoriaId.HasValue)
                cmbCategoria.SelectedValue = lancamento.CategoriaId.Value;

            if (lancamento.ContaId.HasValue)
                cmbConta.SelectedValue = lancamento.ContaId.Value;

            if (lancamento.PessoaId.HasValue)
                cmbPessoa.SelectedValue = lancamento.PessoaId.Value;

            AtualizarComportamentoTipo();
        }

        private void AtualizarComportamentoTipo()
        {
            bool isEntrada = cmbTipo.Text == "Entrada";
            cmbStatus.Enabled = !isEntrada;

            if (isEntrada)
                cmbStatus.Text = "Pago";
        }

        private void btnSalvar_Click(object sender, EventArgs e)
        {
            if (!ValidarFormulario())
                return;

            var lancamento = _lancamentoService.ObterPorId(_lancamentoId);
            if (lancamento == null)
                return;

            var tipo = cmbTipo.Text == "Entrada"
                ? TipoLancamento.Entrada
                : TipoLancamento.Saida;

            lancamento.Descricao = txtDescricao.Text.Trim();
            lancamento.Valor = ParseDecimal(txtValor.Text);
            lancamento.DataVencimento = dtpVencimento.Value.Date;
            lancamento.Tipo = tipo;
            lancamento.Status = tipo == TipoLancamento.Entrada ? "Pago" : cmbStatus.Text;
            lancamento.CategoriaId = cmbCategoria.SelectedValue != null ? Convert.ToInt32(cmbCategoria.SelectedValue) : null;
            lancamento.ContaId = cmbConta.SelectedValue != null ? Convert.ToInt32(cmbConta.SelectedValue) : null;
            lancamento.PessoaId = cmbPessoa.SelectedValue != null ? Convert.ToInt32(cmbPessoa.SelectedValue) : null;
            lancamento.Observacoes = string.IsNullOrWhiteSpace(txtObservacoes.Text) ? null : txtObservacoes.Text.Trim();
            lancamento.Competencia = txtCompetencia.Text.Trim();

            if (lancamento.Status == "Pago" && !lancamento.DataPagamento.HasValue)
                lancamento.DataPagamento = DateTime.Now;

            _lancamentoService.Atualizar(lancamento);

            MessageBox.Show("Lançamento atualizado com sucesso.", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);

            DialogResult = DialogResult.OK;
            Close();
        }

        private bool ValidarFormulario()
        {
            if (string.IsNullOrWhiteSpace(txtDescricao.Text))
            {
                MessageBox.Show("Informe a descrição.");
                txtDescricao.Focus();
                return false;
            }

            if (!decimal.TryParse(txtValor.Text.Replace("R$", "").Trim(), NumberStyles.Any, new CultureInfo("pt-BR"), out _))
            {
                MessageBox.Show("Informe um valor válido.");
                txtValor.Focus();
                return false;
            }

            if (cmbTipo.SelectedIndex < 0 && string.IsNullOrWhiteSpace(cmbTipo.Text))
            {
                MessageBox.Show("Selecione o tipo.");
                cmbTipo.Focus();
                return false;
            }

            if (cmbTipo.Text == "Saída" && cmbStatus.SelectedIndex < 0 && string.IsNullOrWhiteSpace(cmbStatus.Text))
            {
                MessageBox.Show("Selecione o status.");
                cmbStatus.Focus();
                return false;
            }

            return true;
        }

        private decimal ParseDecimal(string texto)
        {
            texto = texto.Replace("R$", "").Trim();
            return decimal.Parse(texto, NumberStyles.Any, new CultureInfo("pt-BR"));
        }

        private void btnCancelar_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void cmbTipo_SelectedIndexChanged(object sender, EventArgs e)
        {
            AtualizarComportamentoTipo();
        }
    }
}
