using FinanceiroPessoal.WinForms.Data;
using FinanceiroPessoal.WinForms.Models;
using Microsoft.EntityFrameworkCore;

namespace FinanceiroPessoal.WinForms;

public class FrmCadastroConta : Form
{
    private readonly TextBox _txtNome = new();
    private readonly TextBox _txtTipo = new();
    private readonly Button _btnSalvar = new();

    public FrmCadastroConta()
    {
        Text = "Cadastro de Conta";
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(420, 230);

        Controls.Add(new Label { Text = "Nome da conta", Left = 20, Top = 20, Width = 200 });
        _txtNome.SetBounds(20, 45, 360, 30);

        Controls.Add(new Label { Text = "Tipo", Left = 20, Top = 85, Width = 200 });
        _txtTipo.SetBounds(20, 110, 360, 30);

        _btnSalvar.Text = "Salvar";
        _btnSalvar.SetBounds(280, 155, 100, 30);
        _btnSalvar.Click += BtnSalvar_Click;

        Controls.Add(_txtNome);
        Controls.Add(_txtTipo);
        Controls.Add(_btnSalvar);
    }

    private async void BtnSalvar_Click(object? sender, EventArgs e)
    {
        var nome = _txtNome.Text.Trim();
        var tipo = _txtTipo.Text.Trim();

        if (string.IsNullOrWhiteSpace(nome) || string.IsNullOrWhiteSpace(tipo))
        {
            MessageBox.Show("Informe nome e tipo da conta.", "Validação", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        await using var db = new FinanceiroDbContext();
        var existe = await db.Contas.AnyAsync(c => c.Nome == nome);
        if (existe)
        {
            MessageBox.Show("Esta conta já existe.", "Validação", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        db.Contas.Add(new Conta { Nome = nome, Tipo = tipo });
        await db.SaveChangesAsync();

        DialogResult = DialogResult.OK;
        Close();
    }
}
