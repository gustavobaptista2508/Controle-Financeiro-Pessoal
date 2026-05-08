using FinanceiroPessoal.WinForms.Services;

namespace FinanceiroPessoal.WinForms;

public partial class FrmCadastroUsuario : Form
{
    private readonly AuthService _authService;

    public FrmCadastroUsuario(AuthService authService)
    {
        _authService = authService;
        InitializeComponent();
    }

    private void btnCadastrar_Click(object sender, EventArgs e)
    {
        var r = _authService.CadastrarUsuario(txtNome.Text.Trim(), txtEmail.Text.Trim(), txtSenha.Text);
        MessageBox.Show(r.mensagem, r.ok ? "Cadastro" : "Erro", MessageBoxButtons.OK,
            r.ok ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        if (r.ok) Close();
    }
}
