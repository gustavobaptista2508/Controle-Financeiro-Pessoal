using FinanceiroPessoal.WinForms.Data;
using FinanceiroPessoal.WinForms.Models;
using Microsoft.EntityFrameworkCore;

namespace FinanceiroPessoal.WinForms;

public class FrmCadastroCategorias : Form
{
    private readonly TextBox _txtNome = new();
    private readonly Button _btnSalvar = new();

    public FrmCadastroCategorias()
    {
        Text = "Cadastro de Categoria";
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(420, 180);

        var lblNome = new Label
        {
            Text = "Nome da categoria",
            Left = 20,
            Top = 20,
            Width = 200
        };

        _txtNome.Left = 20;
        _txtNome.Top = 45;
        _txtNome.Width = 360;

        _btnSalvar.Text = "Salvar";
        _btnSalvar.Left = 280;
        _btnSalvar.Top = 85;
        _btnSalvar.Width = 100;
        _btnSalvar.Click += BtnSalvar_Click;

        Controls.Add(lblNome);
        Controls.Add(_txtNome);
        Controls.Add(_btnSalvar);
    }

    private async void BtnSalvar_Click(object? sender, EventArgs e)
    {
        var nome = _txtNome.Text.Trim();
        if (string.IsNullOrWhiteSpace(nome))
        {
            MessageBox.Show("Informe o nome da categoria.", "Validação", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        await using var db = new FinanceiroDbContext();
        var existe = await db.Categorias.AnyAsync(c => c.Nome == nome);
        if (existe)
        {
            MessageBox.Show("Esta categoria já existe.", "Validação", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        db.Categorias.Add(new Categoria { Nome = nome });
        await db.SaveChangesAsync();

        DialogResult = DialogResult.OK;
        Close();
    }
}
