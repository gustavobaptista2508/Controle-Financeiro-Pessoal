using FinanceiroPessoal.WinForms.Data;
using FinanceiroPessoal.WinForms.Models;
using Microsoft.EntityFrameworkCore;

namespace FinanceiroPessoal.WinForms;

public class FrmPessoas : Form
{
    private readonly DataGridView _grid = new();
    private readonly Button _btnNovo = new();

    public FrmPessoas()
    {
        Text = "Pessoas";
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(700, 450);

        _btnNovo.Text = "+ Nova Pessoa";
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

        Load += async (_, _) => await CarregarPessoas();
    }

    private async void BtnNovo_Click(object? sender, EventArgs e)
    {
        using var frm = new FrmCadastroPessoa();
        if (frm.ShowDialog() == DialogResult.OK)
            await CarregarPessoas();
    }

    private async Task CarregarPessoas()
    {
        await using var db = new FinanceiroDbContext();
        var pessoas = await db.Pessoas
            .OrderBy(c => c.Nome)
            .Select(c => new { c.Id, c.Nome })
            .ToListAsync();

        _grid.DataSource = pessoas;
    }
}
