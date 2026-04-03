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
        private readonly LancamentoService _lancamentoService = new();
        private readonly CadastroAuxiliarService _cadastroAuxiliarService = new();
        public FrmNovoLancamento()
        {
            InitializeComponent();
        }

        private void FrmNovoLancamento_Load(object sender, EventArgs e)
        {
            CarregagarCombos();
            cmbStatus.SelectedIndex = 1;
        }

        private void CarregagarCombos()
        {
            var categorias = _cadastroAuxiliarService.ObterCategorias();
            cmbCategoria.DataSource = categorias;
            cmbCategoria.DisplayMember = "Nome";
            cmbCategoria.ValueMember = "Id";
            cmbCategoria.SelectedIndex = -1;

            var contas = _cadastroAuxiliarService.ObterContas();
            cmbConta.DataSource = contas;
            cmbConta.DisplayMember = "Nome";
            cmbConta.ValueMember = "Id";
            cmbConta.SelectedIndex = -1;

            var pessoas = _cadastroAuxiliarService.ObterPessoas();
            cmbPessoa.DataSource = pessoas;
            cmbPessoa.DisplayMember = "Nome";
            cmbPessoa.ValueMember = "Id";
            cmbPessoa.SelectedIndex = -1;

            cmbStatus.Items.Clear();
            cmbStatus.Items.Add("Pendente");
            cmbStatus.Items.Add("Pago");
            cmbStatus.Items.Add("Atrasado");
            cmbStatus.Items.Add("Parcial");
        }

        private void btnSalvar_Click(object sender, EventArgs e)
        {
            if (!ValidarFormulario())
                return;

            var lancamento = new Lancamento
            {
                Descricao = txtDescricao.Text.Trim(),
                Valor = ParseDecimal(txtValor.Text),
                DataVencimento = dtpVencimento.Value.Date,
                Status = cmbStatus.Text,
                CategoriaId = cmbCategoria.SelectedValue != null ? Convert.ToInt32(cmbCategoria.SelectedValue) : null,
                ContaId = cmbConta.SelectedValue != null ? Convert.ToInt32(cmbConta.SelectedValue) : null,
                PessoaId = cmbPessoa.SelectedValue != null ? Convert.ToInt32(cmbPessoa.SelectedValue) : null,
                Observacoes = string.IsNullOrWhiteSpace(txtObservacoes.Text) ? null : txtObservacoes.Text.Trim(),
                Competencia = txtCompetencia.Text.Trim()
            };

            if (lancamento.Status == "Pago")
                lancamento.DataPagamento = DateTime.Now;

            _lancamentoService.Adicionar(lancamento);

            MessageBox.Show("Lançamento salvo com sucesso.", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);

            DialogResult = DialogResult.OK;
            Close();
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

            if (!decimal.TryParse(txtValor.Text.Replace("R$", "").Trim(),
                NumberStyles.Any,
                new CultureInfo("pt-BR"),
                out _))
            {
                MessageBox.Show("Valor inválido.", "Validação", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtValor.Focus();
                return false;
            }

            if (cmbStatus.SelectedIndex < 0)
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
    }
}
