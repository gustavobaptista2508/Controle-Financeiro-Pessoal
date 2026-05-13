using FinanceiroPessoal.Core.Data;
using Microsoft.EntityFrameworkCore;

namespace FinanceiroPessoal.WinForms;

public class FrmContas : Form
{
    private readonly DataGridView _grid = new();
    private readonly Button _btnNovo = new();

    public FrmContas()
    {
        Text = "Contas";
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(700, 450);

        _btnNovo.Text = "+ Nova Conta";
        _btnNovo.Dock = DockStyle.Top;
        _btnNovo.Height = 40;
        _btnNovo.Click += BtnNovo_Click;

        _grid.Dock = DockStyle.Fill;
        _grid.ReadOnly = true;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

        Controls.Add(_grid);
        Controls.Add(_btnNovo);

        Load += async (_, _) => await CarregarContas();
    }

    private async void BtnNovo_Click(object? sender, EventArgs e)
    {
        using var frm = new FrmCadastroConta();
        if (frm.ShowDialog() == DialogResult.OK)
            await CarregarContas();
    }

    private async Task CarregarContas()
    {
        await using var db = new FinanceiroDbContext();
        var contas = await db.Contas
            .OrderBy(c => c.Nome)
            .Select(c => new { c.Id, c.Nome, c.Tipo })
            .ToListAsync();

        _grid.DataSource = contas;
    }
}
