using FinanceiroPessoal.WinForms.Services;

namespace FinanceiroPessoal.WinForms;

public partial class FrmLogin : Form
{
    private readonly AuthService _authService = new();

    public FrmLogin()
    {
        InitializeComponent();
    }

    private void btnEntrar_Click(object sender, EventArgs e)
    {
        var email = txtEmail.Text.Trim();
        var senha = txtSenha.Text;

        var usuario = _authService.LoginUsuario(email, senha);

        if (usuario is null)
        {
            lblErro.Text = "E-mail ou senha inválidos.";
            lblErro.Visible = true;
            return;
        }

        DialogResult = DialogResult.OK;
        Close();
    }

    private void btnCadastrar_Click(object sender, EventArgs e)
    {
        using var frm = new FrmCadastroUsuario(_authService);
        frm.ShowDialog(this);
    }
}
