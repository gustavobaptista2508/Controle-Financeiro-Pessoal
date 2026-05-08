namespace FinanceiroPessoal.WinForms;

partial class FrmCadastroUsuario
{
    private TextBox txtNome = null!;
    private TextBox txtEmail = null!;
    private TextBox txtSenha = null!;
    private Button btnCadastrar = null!;

    private void InitializeComponent()
    {
        txtNome = new TextBox { Location = new Point(25, 45), Size = new Size(320, 27) };
        txtEmail = new TextBox { Location = new Point(25, 100), Size = new Size(320, 27) };
        txtSenha = new TextBox { Location = new Point(25, 155), Size = new Size(320, 27), UseSystemPasswordChar = true };
        btnCadastrar = new Button { Location = new Point(25, 205), Size = new Size(320, 35), Text = "Cadastrar" };
        btnCadastrar.Click += btnCadastrar_Click;
        Controls.AddRange([
            new Label { Location = new Point(25, 20), Text = "Nome", AutoSize = true }, txtNome,
            new Label { Location = new Point(25, 75), Text = "E-mail", AutoSize = true }, txtEmail,
            new Label { Location = new Point(25, 130), Text = "Senha", AutoSize = true }, txtSenha,
            btnCadastrar
        ]);
        ClientSize = new Size(380, 270);
        Text = "Cadastro de Usuário";
        StartPosition = FormStartPosition.CenterParent;
    }
}
