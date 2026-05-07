using FinanceiroPessoal.WinForms.Data;
using FinanceiroPessoal.WinForms.Models;
using Microsoft.EntityFrameworkCore;

namespace FinanceiroPessoal.WinForms;

public class FrmCategorias : Form
{
    private readonly DataGridView _grid = new();
    private readonly Button _btnNovo = new();

    public FrmCategorias()
    {
        Text = "Categorias";
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(700, 450);

        _btnNovo.Text = "+ Nova Categoria";
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

        Load += async (_, _) => await CarregarCategorias();
    }

    private async void BtnNovo_Click(object? sender, EventArgs e)
    {
        using var frm = new FrmCadastroCategorias();
        if (frm.ShowDialog() == DialogResult.OK)
            await CarregarCategorias();
    }

    private async Task CarregarCategorias()
    {
        await using var db = new FinanceiroDbContext();
        var categorias = await db.Categorias
            .OrderBy(c => c.Nome)
            .Select(c => new { c.Id, c.Nome })
            .ToListAsync();

        _grid.DataSource = categorias;
    }
}
