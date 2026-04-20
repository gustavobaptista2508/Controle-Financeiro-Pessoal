namespace FinanceiroPessoal.WinForms
{
    partial class FrmEditarLancamento
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmEditarLancamento));
            cmbTipo = new ComboBox();
            label10 = new Label();
            txtValor = new TextBox();
            label9 = new Label();
            label8 = new Label();
            label7 = new Label();
            label6 = new Label();
            label5 = new Label();
            label4 = new Label();
            label3 = new Label();
            label2 = new Label();
            label1 = new Label();
            btnCancelar = new Button();
            btnSalvar = new Button();
            dtpVencimento = new DateTimePicker();
            cmbStatus = new ComboBox();
            cmbPessoa = new ComboBox();
            cmbConta = new ComboBox();
            cmbCategoria = new ComboBox();
            txtCompetencia = new TextBox();
            txtObservacoes = new TextBox();
            txtDescricao = new TextBox();
            lblTotalPago = new Label();
            lblTotalPendente = new Label();
            lblTotalGeral = new Label();
            label11 = new Label();
            SuspendLayout();
            // 
            // cmbTipo
            // 
            cmbTipo.FormattingEnabled = true;
            cmbTipo.Location = new Point(17, 42);
            cmbTipo.Name = "cmbTipo";
            cmbTipo.Size = new Size(213, 28);
            cmbTipo.TabIndex = 45;
            cmbTipo.SelectedIndexChanged += cmbTipo_SelectedIndexChanged;
            // 
            // label10
            // 
            label10.AutoSize = true;
            label10.Location = new Point(17, 19);
            label10.Name = "label10";
            label10.Size = new Size(42, 20);
            label10.TabIndex = 44;
            label10.Text = "Tipo:";
            // 
            // txtValor
            // 
            txtValor.Location = new Point(15, 158);
            txtValor.Name = "txtValor";
            txtValor.Size = new Size(215, 27);
            txtValor.TabIndex = 43;
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Location = new Point(16, 326);
            label9.Name = "label9";
            label9.Size = new Size(96, 20);
            label9.TabIndex = 42;
            label9.Text = "Observações:";
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new Point(15, 264);
            label8.Name = "label8";
            label8.Size = new Size(100, 20);
            label8.TabIndex = 41;
            label8.Text = "Competência:";
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new Point(275, 19);
            label7.Name = "label7";
            label7.Size = new Size(52, 20);
            label7.TabIndex = 40;
            label7.Text = "Status:";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(339, 198);
            label6.Name = "label6";
            label6.Size = new Size(56, 20);
            label6.TabIndex = 39;
            label6.Text = "Pessoa:";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(179, 198);
            label5.Name = "label5";
            label5.Size = new Size(51, 20);
            label5.TabIndex = 38;
            label5.Text = "Conta:";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(17, 198);
            label4.Name = "label4";
            label4.Size = new Size(77, 20);
            label4.TabIndex = 37;
            label4.Text = "Categoria:";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(275, 135);
            label3.Name = "label3";
            label3.Size = new Size(90, 20);
            label3.TabIndex = 36;
            label3.Text = "Vencimento:";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(17, 163);
            label2.Name = "label2";
            label2.Size = new Size(46, 20);
            label2.TabIndex = 35;
            label2.Text = "Valor:";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(15, 73);
            label1.Name = "label1";
            label1.Size = new Size(77, 20);
            label1.TabIndex = 34;
            label1.Text = "Descrição:";
            // 
            // btnCancelar
            // 
            btnCancelar.Location = new Point(399, 477);
            btnCancelar.Name = "btnCancelar";
            btnCancelar.Size = new Size(94, 29);
            btnCancelar.TabIndex = 33;
            btnCancelar.Text = "Cancelar";
            btnCancelar.UseVisualStyleBackColor = true;
            btnCancelar.Click += btnCancelar_Click_1;
            // 
            // btnSalvar
            // 
            btnSalvar.BackColor = SystemColors.HotTrack;
            btnSalvar.FlatAppearance.BorderColor = Color.RoyalBlue;
            btnSalvar.FlatAppearance.BorderSize = 0;
            btnSalvar.ForeColor = SystemColors.ButtonHighlight;
            btnSalvar.Location = new Point(299, 477);
            btnSalvar.Name = "btnSalvar";
            btnSalvar.Size = new Size(94, 29);
            btnSalvar.TabIndex = 32;
            btnSalvar.Text = "Salvar";
            btnSalvar.UseVisualStyleBackColor = false;
            btnSalvar.Click += btnSalvar_Click_1;
            // 
            // dtpVencimento
            // 
            dtpVencimento.Format = DateTimePickerFormat.Short;
            dtpVencimento.Location = new Point(275, 158);
            dtpVencimento.Name = "dtpVencimento";
            dtpVencimento.Size = new Size(218, 27);
            dtpVencimento.TabIndex = 31;
            // 
            // cmbStatus
            // 
            cmbStatus.FormattingEnabled = true;
            cmbStatus.Location = new Point(275, 42);
            cmbStatus.Name = "cmbStatus";
            cmbStatus.Size = new Size(215, 28);
            cmbStatus.TabIndex = 30;
            // 
            // cmbPessoa
            // 
            cmbPessoa.FormattingEnabled = true;
            cmbPessoa.Location = new Point(339, 221);
            cmbPessoa.Name = "cmbPessoa";
            cmbPessoa.Size = new Size(154, 28);
            cmbPessoa.TabIndex = 29;
            cmbPessoa.Text = "<Selecione>";
            // 
            // cmbConta
            // 
            cmbConta.FormattingEnabled = true;
            cmbConta.Location = new Point(179, 221);
            cmbConta.Name = "cmbConta";
            cmbConta.Size = new Size(154, 28);
            cmbConta.TabIndex = 28;
            cmbConta.Text = "<Selecione>";
            // 
            // cmbCategoria
            // 
            cmbCategoria.FormattingEnabled = true;
            cmbCategoria.Location = new Point(17, 221);
            cmbCategoria.Name = "cmbCategoria";
            cmbCategoria.Size = new Size(154, 28);
            cmbCategoria.TabIndex = 27;
            cmbCategoria.Text = "<Selecione>";
            // 
            // txtCompetencia
            // 
            txtCompetencia.Location = new Point(17, 287);
            txtCompetencia.Name = "txtCompetencia";
            txtCompetencia.Size = new Size(476, 27);
            txtCompetencia.TabIndex = 26;
            // 
            // txtObservacoes
            // 
            txtObservacoes.Location = new Point(17, 349);
            txtObservacoes.Multiline = true;
            txtObservacoes.Name = "txtObservacoes";
            txtObservacoes.Size = new Size(476, 111);
            txtObservacoes.TabIndex = 25;
            // 
            // txtDescricao
            // 
            txtDescricao.Location = new Point(16, 96);
            txtDescricao.Name = "txtDescricao";
            txtDescricao.Size = new Size(477, 27);
            txtDescricao.TabIndex = 24;
            // 
            // lblTotalPago
            // 
            lblTotalPago.AutoSize = true;
            lblTotalPago.Location = new Point(17, 520);
            lblTotalPago.Name = "lblTotalPago";
            lblTotalPago.Size = new Size(58, 20);
            lblTotalPago.TabIndex = 51;
            lblTotalPago.Text = "label15";
            // 
            // lblTotalPendente
            // 
            lblTotalPendente.AutoSize = true;
            lblTotalPendente.Location = new Point(215, 520);
            lblTotalPendente.Name = "lblTotalPendente";
            lblTotalPendente.Size = new Size(58, 20);
            lblTotalPendente.TabIndex = 52;
            lblTotalPendente.Text = "label11";
            // 
            // lblTotalGeral
            // 
            lblTotalGeral.AutoSize = true;
            lblTotalGeral.Location = new Point(432, 520);
            lblTotalGeral.Name = "lblTotalGeral";
            lblTotalGeral.Size = new Size(58, 20);
            lblTotalGeral.TabIndex = 53;
            lblTotalGeral.Text = "label12";
            // 
            // label11
            // 
            label11.AutoSize = true;
            label11.Location = new Point(17, 135);
            label11.Name = "label11";
            label11.Size = new Size(46, 20);
            label11.TabIndex = 54;
            label11.Text = "Valor:";
            // 
            // FrmEditarLancamento
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.White;
            ClientSize = new Size(507, 562);
            Controls.Add(label11);
            Controls.Add(lblTotalGeral);
            Controls.Add(lblTotalPendente);
            Controls.Add(lblTotalPago);
            Controls.Add(cmbTipo);
            Controls.Add(label10);
            Controls.Add(txtValor);
            Controls.Add(label9);
            Controls.Add(label8);
            Controls.Add(label7);
            Controls.Add(label6);
            Controls.Add(label5);
            Controls.Add(label4);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(btnCancelar);
            Controls.Add(btnSalvar);
            Controls.Add(dtpVencimento);
            Controls.Add(cmbStatus);
            Controls.Add(cmbPessoa);
            Controls.Add(cmbConta);
            Controls.Add(cmbCategoria);
            Controls.Add(txtCompetencia);
            Controls.Add(txtObservacoes);
            Controls.Add(txtDescricao);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "FrmEditarLancamento";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Editar Lançamento";
            Load += FrmEditarLancamento_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private ComboBox cmbTipo;
        private Label label10;
        private TextBox txtValor;
        private Label label9;
        private Label label8;
        private Label label7;
        private Label label6;
        private Label label5;
        private Label label4;
        private Label label3;
        private Label label2;
        private Label label1;
        private Button btnCancelar;
        private Button btnSalvar;
        private DateTimePicker dtpVencimento;
        private ComboBox cmbStatus;
        private ComboBox cmbPessoa;
        private ComboBox cmbConta;
        private ComboBox cmbCategoria;
        private TextBox txtCompetencia;
        private TextBox txtObservacoes;
        private TextBox txtDescricao;
        private Label lblTotalPago;
        private Label lblTotalPendente;
        private Label lblTotalGeral;
        private Label label11;
    }
}