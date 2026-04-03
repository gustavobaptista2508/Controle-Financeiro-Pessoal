namespace FinanceiroPessoal.WinForms
{
    partial class FrmNovoLancamento
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
            txtDescricao = new TextBox();
            txtObservacoes = new TextBox();
            txtCompetencia = new TextBox();
            cmbCategoria = new ComboBox();
            cmbConta = new ComboBox();
            cmbPessoa = new ComboBox();
            cmbStatus = new ComboBox();
            dtpVencimento = new DateTimePicker();
            btnSalvar = new Button();
            btnCancelar = new Button();
            label1 = new Label();
            txtValor = new MaskedTextBox();
            label2 = new Label();
            label3 = new Label();
            label4 = new Label();
            label5 = new Label();
            label6 = new Label();
            label7 = new Label();
            label8 = new Label();
            label9 = new Label();
            SuspendLayout();
            // 
            // txtDescricao
            // 
            txtDescricao.Location = new Point(108, 12);
            txtDescricao.Name = "txtDescricao";
            txtDescricao.Size = new Size(555, 27);
            txtDescricao.TabIndex = 0;
            // 
            // txtObservacoes
            // 
            txtObservacoes.Location = new Point(108, 280);
            txtObservacoes.Multiline = true;
            txtObservacoes.Name = "txtObservacoes";
            txtObservacoes.Size = new Size(555, 111);
            txtObservacoes.TabIndex = 2;
            // 
            // txtCompetencia
            // 
            txtCompetencia.Location = new Point(108, 247);
            txtCompetencia.Name = "txtCompetencia";
            txtCompetencia.Size = new Size(555, 27);
            txtCompetencia.TabIndex = 3;
            // 
            // cmbCategoria
            // 
            cmbCategoria.FormattingEnabled = true;
            cmbCategoria.Location = new Point(108, 111);
            cmbCategoria.Name = "cmbCategoria";
            cmbCategoria.Size = new Size(555, 28);
            cmbCategoria.TabIndex = 4;
            // 
            // cmbConta
            // 
            cmbConta.FormattingEnabled = true;
            cmbConta.Location = new Point(108, 145);
            cmbConta.Name = "cmbConta";
            cmbConta.Size = new Size(555, 28);
            cmbConta.TabIndex = 5;
            // 
            // cmbPessoa
            // 
            cmbPessoa.FormattingEnabled = true;
            cmbPessoa.Location = new Point(108, 179);
            cmbPessoa.Name = "cmbPessoa";
            cmbPessoa.Size = new Size(555, 28);
            cmbPessoa.TabIndex = 6;
            // 
            // cmbStatus
            // 
            cmbStatus.FormattingEnabled = true;
            cmbStatus.Location = new Point(108, 213);
            cmbStatus.Name = "cmbStatus";
            cmbStatus.Size = new Size(151, 28);
            cmbStatus.TabIndex = 7;
            // 
            // dtpVencimento
            // 
            dtpVencimento.Format = DateTimePickerFormat.Short;
            dtpVencimento.Location = new Point(108, 78);
            dtpVencimento.Name = "dtpVencimento";
            dtpVencimento.Size = new Size(151, 27);
            dtpVencimento.TabIndex = 8;
            // 
            // btnSalvar
            // 
            btnSalvar.BackColor = SystemColors.HotTrack;
            btnSalvar.FlatAppearance.BorderColor = Color.RoyalBlue;
            btnSalvar.FlatAppearance.BorderSize = 0;
            btnSalvar.ForeColor = SystemColors.ButtonHighlight;
            btnSalvar.Location = new Point(469, 409);
            btnSalvar.Name = "btnSalvar";
            btnSalvar.Size = new Size(94, 29);
            btnSalvar.TabIndex = 9;
            btnSalvar.Text = "Salvar";
            btnSalvar.UseVisualStyleBackColor = false;
            btnSalvar.Click += btnSalvar_Click;
            // 
            // btnCancelar
            // 
            btnCancelar.Location = new Point(569, 409);
            btnCancelar.Name = "btnCancelar";
            btnCancelar.Size = new Size(94, 29);
            btnCancelar.TabIndex = 10;
            btnCancelar.Text = "Cancelar";
            btnCancelar.UseVisualStyleBackColor = true;
            btnCancelar.Click += btnCancelar_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(25, 15);
            label1.Name = "label1";
            label1.Size = new Size(77, 20);
            label1.TabIndex = 11;
            label1.Text = "Descrição:";
            // 
            // txtValor
            // 
            txtValor.Location = new Point(108, 45);
            txtValor.Mask = "$0.000,00";
            txtValor.Name = "txtValor";
            txtValor.Size = new Size(151, 27);
            txtValor.TabIndex = 12;
            txtValor.TextAlign = HorizontalAlignment.Right;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(56, 48);
            label2.Name = "label2";
            label2.Size = new Size(46, 20);
            label2.TabIndex = 13;
            label2.Text = "Valor:";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(12, 83);
            label3.Name = "label3";
            label3.Size = new Size(90, 20);
            label3.TabIndex = 14;
            label3.Text = "Vencimento:";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(25, 114);
            label4.Name = "label4";
            label4.Size = new Size(77, 20);
            label4.TabIndex = 15;
            label4.Text = "Categoria:";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(51, 148);
            label5.Name = "label5";
            label5.Size = new Size(51, 20);
            label5.TabIndex = 16;
            label5.Text = "Conta:";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(46, 182);
            label6.Name = "label6";
            label6.Size = new Size(56, 20);
            label6.TabIndex = 17;
            label6.Text = "Pessoa:";
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new Point(50, 216);
            label7.Name = "label7";
            label7.Size = new Size(52, 20);
            label7.TabIndex = 18;
            label7.Text = "Status:";
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new Point(2, 250);
            label8.Name = "label8";
            label8.Size = new Size(100, 20);
            label8.TabIndex = 19;
            label8.Text = "Competência:";
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Location = new Point(6, 283);
            label9.Name = "label9";
            label9.Size = new Size(96, 20);
            label9.TabIndex = 20;
            label9.Text = "Observações:";
            // 
            // FrmNovoLancamento
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
            Name = "FrmNovoLancamento";
            Text = "FrmNovoLancamento";
            Load += FrmNovoLancamento_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TextBox txtDescricao;
        private TextBox txtObservacoes;
        private TextBox txtCompetencia;
        private ComboBox cmbCategoria;
        private ComboBox cmbConta;
        private ComboBox cmbPessoa;
        private ComboBox cmbStatus;
        private DateTimePicker dtpVencimento;
        private Button btnSalvar;
        private Button btnCancelar;
        private Label label1;
        private MaskedTextBox txtValor;
        private Label label2;
        private Label label3;
        private Label label4;
        private Label label5;
        private Label label6;
        private Label label7;
        private Label label8;
        private Label label9;
    }
}