using FinanceiroPessoal.Core.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace FinanceiroPessoal.WinForms
{
    public partial class FrmExtratoCartao : Form
    {
        private readonly int _contaId;
        private readonly string _nomeCartao;
        private readonly DateTime _referencia;
        private readonly LancamentoService _service;

        public FrmExtratoCartao(int contaId, string nomeCartao, DateTime referencia, LancamentoService service)
        {
            InitializeComponent();
            _contaId = contaId;
            _nomeCartao = nomeCartao;
            _referencia = referencia;
            _service = service;

            Text = $"Extrato do Cartão: {_nomeCartao} - {referencia:MM/yyyy}";
        }

        private async Task CarregarExtrato()
        {
            // Reutiliza o Filtrar existente — filtra pela conta e período
            var todos = await _service.Filtrar(
                pessoa: "",
                status: "Todos",
                tipo: "Saída",
                dataInicio: new DateTime(_referencia.Year, _referencia.Month, 1),
                dataFim: new DateTime(_referencia.Year, _referencia.Month, 1).AddMonths(1).AddDays(-1));

            // Filtra apenas os do cartão selecionado
            var lancamentos = todos.Where(x => x.ContaId == _contaId).ToList();

            // Grid
            dgvExtrato.DataSource = lancamentos.Select(x => new
            {
                Vencimento = x.DataVencimento?.ToString("dd/MM/yyyy") ?? "",
                x.Descricao,
                Categoria = x.Categoria?.Nome ?? "Sem categoria",
                Valor = x.Valor.ToString("N2"),
                x.Status,
            }).ToList();

            // Cards de resumo
            var total = lancamentos.Sum(x => x.Valor);
            var pago = lancamentos.Where(x => x.Status == "Pago").Sum(x => x.Valor);
            var pendente = lancamentos.Where(x => x.Status != "Pago").Sum(x => x.Valor);

            lblTotalFatura.Text = $"{total:C2}";
            lblTotalPago.Text = $"{pago:C2}";
            lblTotalPendente.Text = $"{pendente:C2}";

            // Colore status
            foreach (DataGridViewRow row in dgvExtrato.Rows)
            {
                var status = row.Cells["Status"].Value?.ToString();
                row.DefaultCellStyle.BackColor = status switch
                {
                    "Pago" => Color.FromArgb(245, 255, 250),
                    "Atrasado" => Color.FromArgb(255, 245, 245),
                    _ => Color.FromArgb(255, 255, 235),
                };
            }
        }

        private void btnFechar_Click(object sender, EventArgs e)
        {
            Close();
        }

        private async void btnMarcarPago_Click(object sender, EventArgs e)
        {
            var confirm = MessageBox.Show(
                $"Marcar toda a fatura de {_nomeCartao} como paga?",
                "Confirmar pagamento",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            // Reutiliza MarcarComoPago existente para cada lançamento
            var todos = await _service.Filtrar("", "Todos", "Saída",
                new DateTime(_referencia.Year, _referencia.Month, 1),
                new DateTime(_referencia.Year, _referencia.Month, 1).AddMonths(1).AddDays(-1));

            var pendentes = todos
                .Where(x => x.ContaId == _contaId && x.Status != "Pago")
                .ToList();

            foreach (var l in pendentes)
                await _service.MarcarComoPago(l.Id);

            await CarregarExtrato();

            MessageBox.Show("Fatura marcada como paga!", "Sucesso",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private async void FrmExtratoCartao_Load_1(object sender, EventArgs e)
        {
            await CarregarExtrato();
            lblExtrato.Text = $"Extrato do Cartão: {_nomeCartao}";
            lblReferencia.Text = $"Referência: {_referencia:MM/yyyy}";
        }
    }
}
