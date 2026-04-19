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
            btnSalvar = new Button();
            btnCancelar = new Button();
            label11 = new Label();
            lblTotalGeral = new Label();
            lblTotalPendente = new Label();
            lblTotalPago = new Label();
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
            dtpVencimento = new DateTimePicker();
            cmbStatus = new ComboBox();
            cmbPessoa = new ComboBox();
            cmbConta = new ComboBox();
            cmbCategoria = new ComboBox();
            txtCompetencia = new TextBox();
            txtObservacoes = new TextBox();
            txtDescricao = new TextBox();
            dudParcelas = new DomainUpDown();
            label12 = new Label();
            SuspendLayout();
            // 
            // btnSalvar
            // 
            btnSalvar.BackColor = SystemColors.HotTrack;
            btnSalvar.FlatAppearance.BorderColor = Color.RoyalBlue;
            btnSalvar.FlatAppearance.BorderSize = 0;
            btnSalvar.ForeColor = SystemColors.ButtonHighlight;
            btnSalvar.Location = new Point(296, 478);
            btnSalvar.Name = "btnSalvar";
            btnSalvar.Size = new Size(94, 29);
            btnSalvar.TabIndex = 9;
            btnSalvar.Text = "Salvar";
            btnSalvar.UseVisualStyleBackColor = false;
            btnSalvar.Click += btnSalvar_Click;
            // 
            // btnCancelar
            // 
            btnCancelar.Location = new Point(394, 478);
            btnCancelar.Name = "btnCancelar";
            btnCancelar.Size = new Size(94, 29);
            btnCancelar.TabIndex = 10;
            btnCancelar.Text = "Cancelar";
            btnCancelar.UseVisualStyleBackColor = true;
            btnCancelar.Click += btnCancelar_Click;
            // 
            // label11
            // 
            label11.AutoSize = true;
            label11.Location = new Point(12, 128);
            label11.Name = "label11";
            label11.Size = new Size(46, 20);
            label11.TabIndex = 80;
            label11.Text = "Valor:";
            // 
            // lblTotalGeral
            // 
            lblTotalGeral.AutoSize = true;
            lblTotalGeral.Font = new Font("Segoe UI", 7F);
            lblTotalGeral.Location = new Point(364, 528);
            lblTotalGeral.Name = "lblTotalGeral";
            lblTotalGeral.Size = new Size(44, 15);
            lblTotalGeral.TabIndex = 79;
            lblTotalGeral.Text = "label12";
            // 
            // lblTotalPendente
            // 
            lblTotalPendente.AutoSize = true;
            lblTotalPendente.Font = new Font("Segoe UI", 7F);
            lblTotalPendente.Location = new Point(187, 528);
            lblTotalPendente.Name = "lblTotalPendente";
            lblTotalPendente.Size = new Size(44, 15);
            lblTotalPendente.TabIndex = 78;
            lblTotalPendente.Text = "label11";
            // 
            // lblTotalPago
            // 
            lblTotalPago.AutoSize = true;
            lblTotalPago.Font = new Font("Segoe UI", 7F);
            lblTotalPago.Location = new Point(10, 528);
            lblTotalPago.Name = "lblTotalPago";
            lblTotalPago.Size = new Size(44, 15);
            lblTotalPago.TabIndex = 77;
            lblTotalPago.Text = "label15";
            // 
            // cmbTipo
            // 
            cmbTipo.FormattingEnabled = true;
            cmbTipo.Location = new Point(12, 35);
            cmbTipo.Name = "cmbTipo";
            cmbTipo.Size = new Size(213, 28);
            cmbTipo.TabIndex = 76;
            // 
            // label10
            // 
            label10.AutoSize = true;
            label10.Location = new Point(12, 12);
            label10.Name = "label10";
            label10.Size = new Size(42, 20);
            label10.TabIndex = 75;
            label10.Text = "Tipo:";
            // 
            // txtValor
            // 
            txtValor.Location = new Point(10, 151);
            txtValor.Name = "txtValor";
            txtValor.Size = new Size(156, 27);
            txtValor.TabIndex = 74;
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Location = new Point(11, 319);
            label9.Name = "label9";
            label9.Size = new Size(96, 20);
            label9.TabIndex = 73;
            label9.Text = "Observações:";
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new Point(10, 257);
            label8.Name = "label8";
            label8.Size = new Size(100, 20);
            label8.TabIndex = 72;
            label8.Text = "Competência:";
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new Point(270, 12);
            label7.Name = "label7";
            label7.Size = new Size(52, 20);
            label7.TabIndex = 71;
            label7.Text = "Status:";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(334, 191);
            label6.Name = "label6";
            label6.Size = new Size(56, 20);
            label6.TabIndex = 70;
            label6.Text = "Pessoa:";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(174, 191);
            label5.Name = "label5";
            label5.Size = new Size(51, 20);
            label5.TabIndex = 69;
            label5.Text = "Conta:";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(12, 191);
            label4.Name = "label4";
            label4.Size = new Size(77, 20);
            label4.TabIndex = 68;
            label4.Text = "Categoria:";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(174, 128);
            label3.Name = "label3";
            label3.Size = new Size(90, 20);
            label3.TabIndex = 67;
            label3.Text = "Vencimento:";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(12, 156);
            label2.Name = "label2";
            label2.Size = new Size(46, 20);
            label2.TabIndex = 66;
            label2.Text = "Valor:";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(10, 66);
            label1.Name = "label1";
            label1.Size = new Size(77, 20);
            label1.TabIndex = 65;
            label1.Text = "Descrição:";
            // 
            // dtpVencimento
            // 
            dtpVencimento.Format = DateTimePickerFormat.Short;
            dtpVencimento.Location = new Point(174, 151);
            dtpVencimento.Name = "dtpVencimento";
            dtpVencimento.Size = new Size(154, 27);
            dtpVencimento.TabIndex = 62;
            // 
            // cmbStatus
            // 
            cmbStatus.FormattingEnabled = true;
            cmbStatus.Location = new Point(270, 35);
            cmbStatus.Name = "cmbStatus";
            cmbStatus.Size = new Size(215, 28);
            cmbStatus.TabIndex = 61;
            // 
            // cmbPessoa
            // 
            cmbPessoa.FormattingEnabled = true;
            cmbPessoa.Location = new Point(334, 214);
            cmbPessoa.Name = "cmbPessoa";
            cmbPessoa.Size = new Size(154, 28);
            cmbPessoa.TabIndex = 60;
            cmbPessoa.Text = "<Selecione>";
            // 
            // cmbConta
            // 
            cmbConta.FormattingEnabled = true;
            cmbConta.Location = new Point(174, 214);
            cmbConta.Name = "cmbConta";
            cmbConta.Size = new Size(154, 28);
            cmbConta.TabIndex = 59;
            cmbConta.Text = "<Selecione>";
            // 
            // cmbCategoria
            // 
            cmbCategoria.FormattingEnabled = true;
            cmbCategoria.Location = new Point(12, 214);
            cmbCategoria.Name = "cmbCategoria";
            cmbCategoria.Size = new Size(154, 28);
            cmbCategoria.TabIndex = 58;
            cmbCategoria.Text = "<Selecione>";
            // 
            // txtCompetencia
            // 
            txtCompetencia.Location = new Point(12, 280);
            txtCompetencia.Name = "txtCompetencia";
            txtCompetencia.Size = new Size(476, 27);
            txtCompetencia.TabIndex = 57;
            // 
            // txtObservacoes
            // 
            txtObservacoes.Location = new Point(12, 342);
            txtObservacoes.Multiline = true;
            txtObservacoes.Name = "txtObservacoes";
            txtObservacoes.Size = new Size(476, 111);
            txtObservacoes.TabIndex = 56;
            // 
            // txtDescricao
            // 
            txtDescricao.Location = new Point(11, 89);
            txtDescricao.Name = "txtDescricao";
            txtDescricao.Size = new Size(477, 27);
            txtDescricao.TabIndex = 55;
            // 
            // dudParcelas
            // 
            dudParcelas.Items.Add("2");
            dudParcelas.Items.Add("3");
            dudParcelas.Items.Add("4");
            dudParcelas.Items.Add("5");
            dudParcelas.Items.Add("6");
            dudParcelas.Items.Add("7");
            dudParcelas.Items.Add("8");
            dudParcelas.Items.Add("9");
            dudParcelas.Items.Add("10");
            dudParcelas.Items.Add("11");
            dudParcelas.Items.Add("12");
            dudParcelas.Items.Add("13");
            dudParcelas.Items.Add("14");
            dudParcelas.Items.Add("15");
            dudParcelas.Items.Add("16");
            dudParcelas.Items.Add("17");
            dudParcelas.Items.Add("18");
            dudParcelas.Items.Add("19");
            dudParcelas.Items.Add("20");
            dudParcelas.Items.Add("21");
            dudParcelas.Items.Add("22");
            dudParcelas.Items.Add("23");
            dudParcelas.Items.Add("24");
            dudParcelas.Items.Add("25");
            dudParcelas.Items.Add("26");
            dudParcelas.Items.Add("27");
            dudParcelas.Items.Add("28");
            dudParcelas.Items.Add("29");
            dudParcelas.Items.Add("30");
            dudParcelas.Items.Add("31");
            dudParcelas.Items.Add("32");
            dudParcelas.Items.Add("33");
            dudParcelas.Items.Add("34");
            dudParcelas.Items.Add("35");
            dudParcelas.Items.Add("36");
            dudParcelas.Items.Add("37");
            dudParcelas.Items.Add("38");
            dudParcelas.Items.Add("39");
            dudParcelas.Items.Add("40");
            dudParcelas.Items.Add("41");
            dudParcelas.Items.Add("42");
            dudParcelas.Items.Add("43");
            dudParcelas.Items.Add("44");
            dudParcelas.Items.Add("45");
            dudParcelas.Items.Add("46");
            dudParcelas.Items.Add("47");
            dudParcelas.Items.Add("48");
            dudParcelas.Items.Add("49");
            dudParcelas.Items.Add("50");
            dudParcelas.Items.Add("51");
            dudParcelas.Items.Add("52");
            dudParcelas.Items.Add("53");
            dudParcelas.Items.Add("54");
            dudParcelas.Items.Add("55");
            dudParcelas.Items.Add("56");
            dudParcelas.Items.Add("57");
            dudParcelas.Items.Add("58");
            dudParcelas.Items.Add("59");
            dudParcelas.Items.Add("60");
            dudParcelas.Location = new Point(334, 149);
            dudParcelas.Name = "dudParcelas";
            dudParcelas.Size = new Size(150, 27);
            dudParcelas.TabIndex = 81;
            dudParcelas.Text = "1";
            // 
            // label12
            // 
            label12.AutoSize = true;
            label12.Location = new Point(334, 128);
            label12.Name = "label12";
            label12.Size = new Size(65, 20);
            label12.TabIndex = 82;
            label12.Text = "Parcelas:";
            // 
            // FrmNovoLancamento
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.White;
            ClientSize = new Size(501, 552);
            Controls.Add(label12);
            Controls.Add(dudParcelas);
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
            Controls.Add(dtpVencimento);
            Controls.Add(cmbStatus);
            Controls.Add(cmbPessoa);
            Controls.Add(cmbConta);
            Controls.Add(cmbCategoria);
            Controls.Add(txtCompetencia);
            Controls.Add(txtObservacoes);
            Controls.Add(txtDescricao);
            Controls.Add(btnCancelar);
            Controls.Add(btnSalvar);
            MaximizeBox = false;
            Name = "FrmNovoLancamento";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Novo Lançamento";
            Load += FrmNovoLancamento_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private Button btnSalvar;
        private Button btnCancelar;
        private Label label11;
        private Label lblTotalGeral;
        private Label lblTotalPendente;
        private Label lblTotalPago;
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
        private DateTimePicker dtpVencimento;
        private ComboBox cmbStatus;
        private ComboBox cmbPessoa;
        private ComboBox cmbConta;
        private ComboBox cmbCategoria;
        private TextBox txtCompetencia;
        private TextBox txtObservacoes;
        private TextBox txtDescricao;
        private DomainUpDown dudParcelas;
        private Label label12;
    }
}