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
        private readonly LancamentoService _service = new();
        private readonly CadastroAuxiliarService _cadastroService = new();
        public FrmLancamentos()
        {
            InitializeComponent();
        }

        private void FrmLancamentos_Load(object sender, EventArgs e)
        {
            CarregarFiltros();
            CarregarGrid();
        }

        private void CarregarGrid()
        {
            var dados = _service.Filtrar(cmbFiltroPessoa.Text, cmbFiltroStatus.Text)
            .Select(x => new
            {
                x.Id,
                x.Descricao,
                Valor = x.Valor.ToString("N2"),
                Vencimento = x.DataVencimento.HasValue ? x.DataVencimento.Value.ToString("dd/MM/yyyy") : "",
                x.Status,
                Categoria = x.Categoria != null ? x.Categoria.Nome : "",
                Conta = x.Conta != null ? x.Conta.Nome : "",
                Pessoa = x.Pessoa != null ? x.Pessoa.Nome : "",
                x.Competencia
            })
            .ToList();

            dgvLancamentos.DataSource = dados;

            if (dgvLancamentos.Columns["Id"] != null)
                dgvLancamentos.Columns["Id"].Width = 60;

            if (dgvLancamentos.Columns["Descricao"] != null)
                dgvLancamentos.Columns["Descricao"].Width = 250;

        }

        private void CarregarFiltros()
        {
            cmbFiltroPessoa.Items.Clear();
            cmbFiltroPessoa.Items.Add("Todos");

            foreach (var pessoa in _cadastroService.ObterPessoas())
                cmbFiltroPessoa.Items.Add(pessoa.Nome);

            cmbFiltroPessoa.SelectedIndex = 0;

            cmbFiltroStatus.Items.Clear();
            cmbFiltroStatus.Items.Add("Todos");
            cmbFiltroStatus.Items.Add("Pendente");
            cmbFiltroStatus.Items.Add("Pago");
            cmbFiltroStatus.Items.Add("Atrasado");
            cmbFiltroStatus.Items.Add("Parcial");
            cmbFiltroStatus.SelectedIndex = 0;
        }

        private int? ObterIdSelecionado()
        {
            if (dgvLancamentos.CurrentRow == null)
                return null;

            return Convert.ToInt32(dgvLancamentos.CurrentRow.Cells["Id"].Value);
        }

        private void btnNovo_Click(object sender, EventArgs e)
        {
            using var frm = new FrmNovoLancamento();
            if (frm.ShowDialog() == DialogResult.OK)
                CarregarGrid();
        }

        private void btnExcluir_Click(object sender, EventArgs e)
        {
            var id = ObterIdSelecionado();
            if (id == null)
            {
                MessageBox.Show("Selecione um lançamento.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var confirmar = MessageBox.Show(
                "Deseja realmente excluir este lançamento?",
                "Confirmação",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirmar != DialogResult.Yes)
                return;

            _service.Excluir(id.Value);
            CarregarGrid();
        }

        private void btnMarcarPago_Click(object sender, EventArgs e)
        {
            var id = ObterIdSelecionado();
            if (id == null)
            {
                MessageBox.Show("Selecione um lançamento.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _service.MarcarComoPago(id.Value);
            CarregarGrid();
        }

        private void btnAtualizar_Click(object sender, EventArgs e)
        {
            CarregarGrid();
        }

        private void btnFiltrar_Click(object sender, EventArgs e)
        {
            CarregarGrid();
        }

        private void btnEditar_Click(object sender, EventArgs e)
        {
            var id = ObterIdSelecionado();
            if (id == null)
            {
                MessageBox.Show("Selecione um lançamento.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using var frm = new FrmEditarLancamento(id.Value);
            if (frm.ShowDialog() == DialogResult.OK)
                CarregarGrid();
        }
    }
}
