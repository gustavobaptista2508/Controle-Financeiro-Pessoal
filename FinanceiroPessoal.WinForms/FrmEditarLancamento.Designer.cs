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
            SuspendLayout();
            // 
            // cmbTipo
            // 
            cmbTipo.FormattingEnabled = true;
            cmbTipo.Location = new Point(113, 20);
            cmbTipo.Name = "cmbTipo";
            cmbTipo.Size = new Size(151, 28);
            cmbTipo.TabIndex = 45;
            cmbTipo.SelectedIndexChanged += cmbTipo_SelectedIndexChanged;
            // 
            // label10
            // 
            label10.AutoSize = true;
            label10.Location = new Point(17, 23);
            label10.Name = "label10";
            label10.Size = new Size(93, 20);
            label10.TabIndex = 44;
            label10.Text = "Lançamento:";
            // 
            // txtValor
            // 
            txtValor.Location = new Point(113, 87);
            txtValor.Name = "txtValor";
            txtValor.Size = new Size(151, 27);
            txtValor.TabIndex = 43;
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Location = new Point(11, 325);
            label9.Name = "label9";
            label9.Size = new Size(96, 20);
            label9.TabIndex = 42;
            label9.Text = "Observações:";
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new Point(7, 292);
            label8.Name = "label8";
            label8.Size = new Size(100, 20);
            label8.TabIndex = 41;
            label8.Text = "Competência:";
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new Point(55, 258);
            label7.Name = "label7";
            label7.Size = new Size(52, 20);
            label7.TabIndex = 40;
            label7.Text = "Status:";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(51, 224);
            label6.Name = "label6";
            label6.Size = new Size(56, 20);
            label6.TabIndex = 39;
            label6.Text = "Pessoa:";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(56, 190);
            label5.Name = "label5";
            label5.Size = new Size(51, 20);
            label5.TabIndex = 38;
            label5.Text = "Conta:";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(30, 156);
            label4.Name = "label4";
            label4.Size = new Size(77, 20);
            label4.TabIndex = 37;
            label4.Text = "Categoria:";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(17, 125);
            label3.Name = "label3";
            label3.Size = new Size(90, 20);
            label3.TabIndex = 36;
            label3.Text = "Vencimento:";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(61, 90);
            label2.Name = "label2";
            label2.Size = new Size(46, 20);
            label2.TabIndex = 35;
            label2.Text = "Valor:";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(30, 57);
            label1.Name = "label1";
            label1.Size = new Size(77, 20);
            label1.TabIndex = 34;
            label1.Text = "Descrição:";
            // 
            // btnCancelar
            // 
            btnCancelar.Location = new Point(574, 451);
            btnCancelar.Name = "btnCancelar";
            btnCancelar.Size = new Size(94, 29);
            btnCancelar.TabIndex = 33;
            btnCancelar.Text = "Cancelar";
            btnCancelar.UseVisualStyleBackColor = true;
            // 
            // btnSalvar
            // 
            btnSalvar.BackColor = SystemColors.HotTrack;
            btnSalvar.FlatAppearance.BorderColor = Color.RoyalBlue;
            btnSalvar.FlatAppearance.BorderSize = 0;
            btnSalvar.ForeColor = SystemColors.ButtonHighlight;
            btnSalvar.Location = new Point(474, 451);
            btnSalvar.Name = "btnSalvar";
            btnSalvar.Size = new Size(94, 29);
            btnSalvar.TabIndex = 32;
            btnSalvar.Text = "Salvar";
            btnSalvar.UseVisualStyleBackColor = false;
            // 
            // dtpVencimento
            // 
            dtpVencimento.Format = DateTimePickerFormat.Short;
            dtpVencimento.Location = new Point(113, 120);
            dtpVencimento.Name = "dtpVencimento";
            dtpVencimento.Size = new Size(151, 27);
            dtpVencimento.TabIndex = 31;
            // 
            // cmbStatus
            // 
            cmbStatus.FormattingEnabled = true;
            cmbStatus.Location = new Point(113, 255);
            cmbStatus.Name = "cmbStatus";
            cmbStatus.Size = new Size(151, 28);
            cmbStatus.TabIndex = 30;
            // 
            // cmbPessoa
            // 
            cmbPessoa.FormattingEnabled = true;
            cmbPessoa.Location = new Point(113, 221);
            cmbPessoa.Name = "cmbPessoa";
            cmbPessoa.Size = new Size(555, 28);
            cmbPessoa.TabIndex = 29;
            // 
            // cmbConta
            // 
            cmbConta.FormattingEnabled = true;
            cmbConta.Location = new Point(113, 187);
            cmbConta.Name = "cmbConta";
            cmbConta.Size = new Size(555, 28);
            cmbConta.TabIndex = 28;
            // 
            // cmbCategoria
            // 
            cmbCategoria.FormattingEnabled = true;
            cmbCategoria.Location = new Point(113, 153);
            cmbCategoria.Name = "cmbCategoria";
            cmbCategoria.Size = new Size(555, 28);
            cmbCategoria.TabIndex = 27;
            // 
            // txtCompetencia
            // 
            txtCompetencia.Location = new Point(113, 289);
            txtCompetencia.Name = "txtCompetencia";
            txtCompetencia.Size = new Size(555, 27);
            txtCompetencia.TabIndex = 26;
            // 
            // txtObservacoes
            // 
            txtObservacoes.Location = new Point(113, 322);
            txtObservacoes.Multiline = true;
            txtObservacoes.Name = "txtObservacoes";
            txtObservacoes.Size = new Size(555, 111);
            txtObservacoes.TabIndex = 25;
            // 
            // txtDescricao
            // 
            txtDescricao.Location = new Point(113, 54);
            txtDescricao.Name = "txtDescricao";
            txtDescricao.Size = new Size(555, 27);
            txtDescricao.TabIndex = 24;
            // 
            // FrmEditarLancamento
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(675, 501);
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
            Name = "FrmEditarLancamento";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "FrmEditarLancamento";
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
    }
}