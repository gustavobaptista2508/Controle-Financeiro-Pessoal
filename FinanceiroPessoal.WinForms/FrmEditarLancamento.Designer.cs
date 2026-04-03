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
            label9 = new Label();
            label8 = new Label();
            label7 = new Label();
            label6 = new Label();
            label5 = new Label();
            label4 = new Label();
            label3 = new Label();
            label2 = new Label();
            txtValor = new MaskedTextBox();
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
            // label9
            // 
            label9.AutoSize = true;
            label9.Location = new Point(6, 286);
            label9.Name = "label9";
            label9.Size = new Size(96, 20);
            label9.TabIndex = 40;
            label9.Text = "Observações:";
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new Point(2, 253);
            label8.Name = "label8";
            label8.Size = new Size(100, 20);
            label8.TabIndex = 39;
            label8.Text = "Competência:";
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new Point(50, 219);
            label7.Name = "label7";
            label7.Size = new Size(52, 20);
            label7.TabIndex = 38;
            label7.Text = "Status:";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(46, 185);
            label6.Name = "label6";
            label6.Size = new Size(56, 20);
            label6.TabIndex = 37;
            label6.Text = "Pessoa:";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(51, 151);
            label5.Name = "label5";
            label5.Size = new Size(51, 20);
            label5.TabIndex = 36;
            label5.Text = "Conta:";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(25, 117);
            label4.Name = "label4";
            label4.Size = new Size(77, 20);
            label4.TabIndex = 35;
            label4.Text = "Categoria:";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(12, 86);
            label3.Name = "label3";
            label3.Size = new Size(90, 20);
            label3.TabIndex = 34;
            label3.Text = "Vencimento:";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(56, 51);
            label2.Name = "label2";
            label2.Size = new Size(46, 20);
            label2.TabIndex = 33;
            label2.Text = "Valor:";
            // 
            // txtValor
            // 
            txtValor.Location = new Point(108, 48);
            txtValor.Mask = "$0.000,00";
            txtValor.Name = "txtValor";
            txtValor.Size = new Size(151, 27);
            txtValor.TabIndex = 32;
            txtValor.TextAlign = HorizontalAlignment.Right;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(25, 18);
            label1.Name = "label1";
            label1.Size = new Size(77, 20);
            label1.TabIndex = 31;
            label1.Text = "Descrição:";
            // 
            // btnCancelar
            // 
            btnCancelar.Location = new Point(569, 412);
            btnCancelar.Name = "btnCancelar";
            btnCancelar.Size = new Size(94, 29);
            btnCancelar.TabIndex = 30;
            btnCancelar.Text = "Cancelar";
            btnCancelar.UseVisualStyleBackColor = true;
            btnCancelar.Click += btnCancelar_Click;
            // 
            // btnSalvar
            // 
            btnSalvar.BackColor = SystemColors.HotTrack;
            btnSalvar.FlatAppearance.BorderColor = Color.RoyalBlue;
            btnSalvar.FlatAppearance.BorderSize = 0;
            btnSalvar.ForeColor = SystemColors.ButtonHighlight;
            btnSalvar.Location = new Point(469, 412);
            btnSalvar.Name = "btnSalvar";
            btnSalvar.Size = new Size(94, 29);
            btnSalvar.TabIndex = 29;
            btnSalvar.Text = "Salvar";
            btnSalvar.UseVisualStyleBackColor = false;
            btnSalvar.Click += btnSalvar_Click;
            // 
            // dtpVencimento
            // 
            dtpVencimento.Format = DateTimePickerFormat.Short;
            dtpVencimento.Location = new Point(108, 81);
            dtpVencimento.Name = "dtpVencimento";
            dtpVencimento.Size = new Size(151, 27);
            dtpVencimento.TabIndex = 28;
            // 
            // cmbStatus
            // 
            cmbStatus.FormattingEnabled = true;
            cmbStatus.Location = new Point(108, 216);
            cmbStatus.Name = "cmbStatus";
            cmbStatus.Size = new Size(151, 28);
            cmbStatus.TabIndex = 27;
            // 
            // cmbPessoa
            // 
            cmbPessoa.FormattingEnabled = true;
            cmbPessoa.Location = new Point(108, 182);
            cmbPessoa.Name = "cmbPessoa";
            cmbPessoa.Size = new Size(555, 28);
            cmbPessoa.TabIndex = 26;
            // 
            // cmbConta
            // 
            cmbConta.FormattingEnabled = true;
            cmbConta.Location = new Point(108, 148);
            cmbConta.Name = "cmbConta";
            cmbConta.Size = new Size(555, 28);
            cmbConta.TabIndex = 25;
            // 
            // cmbCategoria
            // 
            cmbCategoria.FormattingEnabled = true;
            cmbCategoria.Location = new Point(108, 114);
            cmbCategoria.Name = "cmbCategoria";
            cmbCategoria.Size = new Size(555, 28);
            cmbCategoria.TabIndex = 24;
            // 
            // txtCompetencia
            // 
            txtCompetencia.Location = new Point(108, 250);
            txtCompetencia.Name = "txtCompetencia";
            txtCompetencia.Size = new Size(555, 27);
            txtCompetencia.TabIndex = 23;
            // 
            // txtObservacoes
            // 
            txtObservacoes.Location = new Point(108, 283);
            txtObservacoes.Multiline = true;
            txtObservacoes.Name = "txtObservacoes";
            txtObservacoes.Size = new Size(555, 111);
            txtObservacoes.TabIndex = 22;
            // 
            // txtDescricao
            // 
            txtDescricao.Location = new Point(108, 15);
            txtDescricao.Name = "txtDescricao";
            txtDescricao.Size = new Size(555, 27);
            txtDescricao.TabIndex = 21;
            // 
            // FrmEditarLancamento
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(675, 450);
            Controls.Add(label9);
            Controls.Add(label8);
            Controls.Add(label7);
            Controls.Add(label6);
            Controls.Add(label5);
            Controls.Add(label4);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(txtValor);
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
            Text = "FrmEditarLancamento";
            Load += FrmEditarLancamento_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label9;
        private Label label8;
        private Label label7;
        private Label label6;
        private Label label5;
        private Label label4;
        private Label label3;
        private Label label2;
        private MaskedTextBox txtValor;
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