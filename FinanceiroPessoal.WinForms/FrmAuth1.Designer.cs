namespace FinanceiroPessoal.WinForms
{
    partial class FrmAuth1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

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
            pnlHeader = new Panel();
            lblTitulo = new Label();
            lblSubtitulo = new Label();
            pnlLogo = new Panel();
            btnEntrar = new Button();
            pnlContent = new Panel();
            label1 = new Label();
            lblTimer = new Label();
            pnlAviso = new Panel();
            lblAviso = new Label();
            lblErro = new Label();
            picQrCode = new PictureBox();
            pnlDigitos = new Panel();
            pnlHeader.SuspendLayout();
            pnlContent.SuspendLayout();
            pnlAviso.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)picQrCode).BeginInit();
            SuspendLayout();
            // 
            // pnlHeader
            // 
            pnlHeader.BackColor = Color.FromArgb(24, 95, 165);
            pnlHeader.Controls.Add(lblTitulo);
            pnlHeader.Controls.Add(lblSubtitulo);
            pnlHeader.Controls.Add(pnlLogo);
            pnlHeader.Dock = DockStyle.Top;
            pnlHeader.Location = new Point(0, 0);
            pnlHeader.Name = "pnlHeader";
            pnlHeader.Size = new Size(442, 130);
            pnlHeader.TabIndex = 0;
            // 
            // lblTitulo
            // 
            lblTitulo.AutoSize = true;
            lblTitulo.Font = new Font("Segoe UI Semibold", 13.2000008F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblTitulo.ForeColor = Color.White;
            lblTitulo.Location = new Point(112, 50);
            lblTitulo.Name = "lblTitulo";
            lblTitulo.Size = new Size(218, 31);
            lblTitulo.TabIndex = 0;
            lblTitulo.Text = "Controle Financeiro";
            lblTitulo.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblSubtitulo
            // 
            lblSubtitulo.AutoSize = true;
            lblSubtitulo.Font = new Font("Segoe UI", 10F);
            lblSubtitulo.ForeColor = Color.FromArgb(181, 212, 244);
            lblSubtitulo.Location = new Point(114, 81);
            lblSubtitulo.Name = "lblSubtitulo";
            lblSubtitulo.Size = new Size(214, 23);
            lblSubtitulo.TabIndex = 1;
            lblSubtitulo.Text = "Verificação em dois fatores";
            // 
            // pnlLogo
            // 
            pnlLogo.BackgroundImage = Properties.Resources.qrcode_icon_187848;
            pnlLogo.BackgroundImageLayout = ImageLayout.Stretch;
            pnlLogo.Location = new Point(197, 3);
            pnlLogo.Name = "pnlLogo";
            pnlLogo.Size = new Size(48, 48);
            pnlLogo.TabIndex = 1;
            // 
            // btnEntrar
            // 
            btnEntrar.BackColor = Color.RoyalBlue;
            btnEntrar.FlatStyle = FlatStyle.Flat;
            btnEntrar.ForeColor = SystemColors.ButtonHighlight;
            btnEntrar.Location = new Point(50, 449);
            btnEntrar.Name = "btnEntrar";
            btnEntrar.Size = new Size(345, 30);
            btnEntrar.TabIndex = 0;
            btnEntrar.Text = "Entrar";
            btnEntrar.UseVisualStyleBackColor = false;
            btnEntrar.Click += btnEntrar_Click;
            // 
            // pnlContent
            // 
            pnlContent.Controls.Add(label1);
            pnlContent.Controls.Add(btnEntrar);
            pnlContent.Controls.Add(lblTimer);
            pnlContent.Controls.Add(pnlAviso);
            pnlContent.Controls.Add(lblErro);
            pnlContent.Controls.Add(picQrCode);
            pnlContent.Controls.Add(pnlDigitos);
            pnlContent.Dock = DockStyle.Fill;
            pnlContent.Location = new Point(0, 130);
            pnlContent.Name = "pnlContent";
            pnlContent.Size = new Size(442, 503);
            pnlContent.TabIndex = 2;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(50, 312);
            label1.Name = "label1";
            label1.Size = new Size(145, 21);
            label1.TabIndex = 7;
            label1.Text = "Código de 6 dígitos";
            // 
            // lblTimer
            // 
            lblTimer.AutoSize = true;
            lblTimer.Location = new Point(195, 414);
            lblTimer.Name = "lblTimer";
            lblTimer.Size = new Size(52, 21);
            lblTimer.TabIndex = 4;
            lblTimer.Text = "label5";
            lblTimer.Visible = false;
            // 
            // pnlAviso
            // 
            pnlAviso.BackColor = Color.FromArgb(245, 250, 255);
            pnlAviso.Controls.Add(lblAviso);
            pnlAviso.Location = new Point(43, 34);
            pnlAviso.Name = "pnlAviso";
            pnlAviso.Size = new Size(348, 60);
            pnlAviso.TabIndex = 3;
            // 
            // lblAviso
            // 
            lblAviso.AutoSize = true;
            lblAviso.ForeColor = Color.FromArgb(55, 65, 81);
            lblAviso.Location = new Point(7, 9);
            lblAviso.Name = "lblAviso";
            lblAviso.RightToLeft = RightToLeft.No;
            lblAviso.Size = new Size(334, 42);
            lblAviso.TabIndex = 2;
            lblAviso.Text = "Escaneie o QR Code com Google Authenticator\r\n\\Microsoft Authenticator.";
            lblAviso.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblErro
            // 
            lblErro.AutoSize = true;
            lblErro.Location = new Point(195, 277);
            lblErro.Name = "lblErro";
            lblErro.Size = new Size(52, 21);
            lblErro.TabIndex = 3;
            lblErro.Text = "label4";
            lblErro.Visible = false;
            // 
            // picQrCode
            // 
            picQrCode.Location = new Point(146, 111);
            picQrCode.Name = "picQrCode";
            picQrCode.Size = new Size(150, 150);
            picQrCode.SizeMode = PictureBoxSizeMode.Zoom;
            picQrCode.TabIndex = 1;
            picQrCode.TabStop = false;
            // 
            // pnlDigitos
            // 
            pnlDigitos.Location = new Point(47, 336);
            pnlDigitos.Name = "pnlDigitos";
            pnlDigitos.Size = new Size(348, 58);
            pnlDigitos.TabIndex = 6;
            // 
            // FrmAuth1
            // 
            AutoScaleDimensions = new SizeF(9F, 21F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.White;
            ClientSize = new Size(442, 633);
            Controls.Add(pnlContent);
            Controls.Add(pnlHeader);
            Font = new Font("Segoe UI", 9.5F);
            FormBorderStyle = FormBorderStyle.None;
            MaximizeBox = false;
            Name = "FrmAuth1";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "FrmAuth1";
            Load += FrmAuth1_Load;
            pnlHeader.ResumeLayout(false);
            pnlHeader.PerformLayout();
            pnlContent.ResumeLayout(false);
            pnlContent.PerformLayout();
            pnlAviso.ResumeLayout(false);
            pnlAviso.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)picQrCode).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private Panel pnlHeader;
        private Panel pnlLogo;
        private Panel pnlContent;
        private Panel pnlAviso;
        private Button btnEntrar;
        private PictureBox picQrCode;
        private Label lblTimer;
        private Label lblErro;
        private Label lblAviso;
        private Label lblSubtitulo;
        private Label lblTitulo;
        private Panel pnlDigitos;
        private Label label1;
    }
}