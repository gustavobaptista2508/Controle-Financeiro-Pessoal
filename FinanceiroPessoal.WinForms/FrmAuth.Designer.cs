namespace FinanceiroPessoal.WinForms
{
    partial class FrmAuth
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        private Panel pnlHeader;
        private Panel pnlLogo;
        private Label lblTitulo;
        private Label lblSubtitulo;
        private Panel pnlContent;
        private Panel pnlAviso;
        private PictureBox picAvisoIcon;
        private Label lblAviso;
        private PictureBox picQrCode;
        private Panel pnlDigitos;
        private TextBox[] txtDigitos;
        private Panel pnlErro;
        private Label lblErro;
        private Button btnEntrar;
        private Panel pnlTimer;
        private Label lblTimer;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();

            // Form
            Text = "Autenticação";
            Size = new Size(420, 680);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            BackColor = Color.White;
            Font = new Font("Segoe UI", 9.5F);

            // Header azul
            pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 130,
                BackColor = Color.FromArgb(24, 95, 165),
            };

            // Ícone no header
            pnlLogo = new Panel
            {
                Size = new Size(48, 48),
                BackColor = Color.FromArgb(230, 241, 251),
                Location = new Point((pnlHeader.Width - 48) / 2, 18),
                Anchor = AnchorStyles.Top,
            };
            UIHelpers.ApplyRoundedRegion(pnlLogo, 24);

            lblTitulo = new Label
            {
                Text = "Controle Financeiro",
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 13F, FontStyle.Bold),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.None,
                Size = new Size(380, 28),
                Location = new Point(0, 72),
            };

            lblSubtitulo = new Label
            {
                Text = "Verificação em dois fatores",
                ForeColor = Color.FromArgb(181, 212, 244),
                Font = new Font("Segoe UI", 10F),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(380, 22),
                Location = new Point(0, 98),
            };

            pnlHeader.Controls.AddRange(new Control[] { pnlLogo, lblTitulo, lblSubtitulo });

            // Conteúdo
            pnlContent = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24),
                BackColor = Color.White,
            };

            // Aviso de instrução
            pnlAviso = new Panel
            {
                Size = new Size(348, 60),
                Location = new Point(24, 16),
                BackColor = Color.FromArgb(245, 250, 255),
            };
            UIHelpers.ApplyRoundedRegion(pnlAviso, 8);

            lblAviso = new Label
            {
                AutoSize = false,
                Size = new Size(300, 50),
                Location = new Point(44, 5),
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(55, 65, 81),
                Text = "Escaneie o QR Code com Google Authenticator\nou Microsoft Authenticator.",
            };

            pnlAviso.Controls.Add(lblAviso);

            // QR Code
            picQrCode = new PictureBox
            {
                Size = new Size(150, 150),
                Location = new Point(111, 88),
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.White,
                Visible = false,
            };

            // Painel dos dígitos
            pnlDigitos = new Panel
            {
                Size = new Size(348, 58),
                Location = new Point(24, 260),
                BackColor = Color.Transparent,
            };

            txtDigitos = new TextBox[6];
            for (int i = 0; i < 6; i++)
            {
                txtDigitos[i] = new TextBox
                {
                    Size = new Size(44, 52),
                    Location = new Point(i * 52, 0),
                    MaxLength = 1,
                    TextAlign = HorizontalAlignment.Center,
                    Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold),
                    BackColor = Color.FromArgb(245, 248, 255),
                    ForeColor = Color.FromArgb(12, 68, 124),
                    BorderStyle = BorderStyle.FixedSingle,
                    Tag = i,
                };
                int idx = i;
                txtDigitos[i].KeyPress += (s, e) =>
                {
                    if (!char.IsDigit(e.KeyChar) && e.KeyChar != (char)Keys.Back)
                        e.Handled = true;
                };
                txtDigitos[i].TextChanged += (s, e) =>
                {
                    if (txtDigitos[idx].Text.Length == 1 && idx < 5)
                        txtDigitos[idx + 1].Focus();
                    AtualizarEstiloDigitos();
                };
                txtDigitos[i].KeyDown += (s, e) =>
                {
                    if (e.KeyCode == Keys.Back && txtDigitos[idx].Text.Length == 0 && idx > 0)
                        txtDigitos[idx - 1].Focus();
                };
                pnlDigitos.Controls.Add(txtDigitos[i]);
            }

            // Painel de erro
            pnlErro = new Panel
            {
                Size = new Size(348, 36),
                Location = new Point(24, 330),
                BackColor = Color.FromArgb(252, 235, 235),
                Visible = false,
            };
            UIHelpers.ApplyRoundedRegion(pnlErro, 8);

            lblErro = new Label
            {
                AutoSize = false,
                Size = new Size(320, 30),
                Location = new Point(24, 4),
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(163, 45, 45),
                Text = "Código inválido ou expirado. Tente novamente.",
            };
            pnlErro.Controls.Add(lblErro);

            // Botão entrar
            btnEntrar = new Button
            {
                Text = "Entrar",
                Size = new Size(348, 42),
                Location = new Point(24, 378),
                BackColor = Color.FromArgb(24, 95, 165),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold),
                Cursor = Cursors.Hand,
            };
            btnEntrar.FlatAppearance.BorderSize = 0;
            btnEntrar.FlatAppearance.MouseOverBackColor = Color.FromArgb(12, 68, 124);
            UIHelpers.ApplyRoundedRegion(btnEntrar, 8);
            //btnEntrar.Click += btnEntrar_Click;

            // Timer label
            pnlTimer = new Panel
            {
                Size = new Size(348, 24),
                Location = new Point(24, 430),
                BackColor = Color.Transparent,
            };

            lblTimer = new Label
            {
                AutoSize = false,
                Size = new Size(348, 24),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(156, 163, 175),
                Text = "código expira em 30s",
            };
            pnlTimer.Controls.Add(lblTimer);

            pnlContent.Controls.AddRange(new Control[]
            {
                pnlAviso,
                picQrCode,
                pnlDigitos,
                pnlErro,
                btnEntrar,
                pnlTimer,
            });

            Controls.AddRange(new Control[] { pnlContent, pnlHeader });
        }

        #endregion
    }
}