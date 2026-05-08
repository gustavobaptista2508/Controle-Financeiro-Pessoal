namespace FinanceiroPessoal.WinForms;

partial class FrmLogin
{
    private System.ComponentModel.IContainer? components = null;
    private TextBox txtEmail = null!;
    private TextBox txtSenha = null!;
    private Button btnEntrar = null!;
    private Label lblErro = null!;
    private Button btnCadastrar = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null)) components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        txtEmail = new TextBox { Location = new Point(30, 50), Size = new Size(300, 27) };
        txtSenha = new TextBox { Location = new Point(30, 105), Size = new Size(300, 27), UseSystemPasswordChar = true };
        btnEntrar = new Button { Location = new Point(30, 155), Size = new Size(300, 34), Text = "Entrar" };
        lblErro = new Label { Location = new Point(30, 200), Size = new Size(320, 24), ForeColor = Color.Firebrick, Visible = false };
        btnCadastrar = new Button { Location = new Point(30, 230), Size = new Size(300, 30), Text = "Criar conta" };
        var lbl1 = new Label { Location = new Point(30, 25), Text = "E-mail", AutoSize = true };
        var lbl2 = new Label { Location = new Point(30, 82), Text = "Senha", AutoSize = true };
        btnEntrar.Click += btnEntrar_Click;
        btnCadastrar.Click += btnCadastrar_Click;

        ClientSize = new Size(380, 280);
        Controls.AddRange([lbl1, txtEmail, lbl2, txtSenha, btnEntrar, lblErro, btnCadastrar]);
        Text = "Login";
        StartPosition = FormStartPosition.CenterScreen;
    }
}
