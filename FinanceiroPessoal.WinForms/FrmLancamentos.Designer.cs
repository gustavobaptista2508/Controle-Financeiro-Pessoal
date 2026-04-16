namespace FinanceiroPessoal.WinForms
{
    partial class FrmLancamentos
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
            btnNovo = new Button();
            btnExcluir = new Button();
            btnMarcarPago = new Button();
            btnAtualizar = new Button();
            btnFiltrar = new Button();
            panel1 = new Panel();
            pnlBotoes = new FlowLayoutPanel();
            btnEditar = new Button();
            lblFiltroPessoa = new Label();
            cmbFiltroPessoa = new ComboBox();
            lblFiltroStatus = new Label();
            dgvLancamentos = new DataGridView();
            cmbFiltroStatus = new ComboBox();
            cmbFiltroTipo = new ComboBox();
            label1 = new Label();
            panel2 = new Panel();
            lblSaldoConta = new Label();
            lblAtrasados = new Label();
            lblPendentes = new Label();
            lblPagos = new Label();
            lblTotalGeral = new Label();
            dtpDataInicio = new DateTimePicker();
            dtpDataFim = new DateTimePicker();
            label2 = new Label();
            groupBox1 = new GroupBox();
            panel3 = new Panel();
            panel1.SuspendLayout();
            pnlBotoes.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvLancamentos).BeginInit();
            panel2.SuspendLayout();
            groupBox1.SuspendLayout();
            SuspendLayout();
            // 
            // btnNovo
            // 
            btnNovo.BackColor = SystemColors.Highlight;
            btnNovo.FlatAppearance.BorderSize = 0;
            btnNovo.FlatStyle = FlatStyle.Flat;
            btnNovo.ForeColor = Color.White;
            btnNovo.Location = new Point(335, 2);
            btnNovo.Margin = new Padding(3, 2, 3, 2);
            btnNovo.Name = "btnNovo";
            btnNovo.Size = new Size(82, 41);
            btnNovo.TabIndex = 0;
            btnNovo.Text = "Novo";
            btnNovo.UseVisualStyleBackColor = false;
            btnNovo.Click += btnNovo_Click;
            // 
            // btnExcluir
            // 
            btnExcluir.BackColor = SystemColors.Highlight;
            btnExcluir.FlatAppearance.BorderSize = 0;
            btnExcluir.FlatStyle = FlatStyle.Flat;
            btnExcluir.ForeColor = Color.White;
            btnExcluir.Location = new Point(511, 2);
            btnExcluir.Margin = new Padding(3, 2, 3, 2);
            btnExcluir.Name = "btnExcluir";
            btnExcluir.Size = new Size(82, 41);
            btnExcluir.TabIndex = 1;
            btnExcluir.Text = "Excluir";
            btnExcluir.UseVisualStyleBackColor = false;
            btnExcluir.Click += btnExcluir_Click;
            // 
            // btnMarcarPago
            // 
            btnMarcarPago.BackColor = SystemColors.Highlight;
            btnMarcarPago.FlatAppearance.BorderSize = 0;
            btnMarcarPago.FlatStyle = FlatStyle.Flat;
            btnMarcarPago.ForeColor = Color.White;
            btnMarcarPago.Location = new Point(599, 2);
            btnMarcarPago.Margin = new Padding(3, 2, 3, 2);
            btnMarcarPago.Name = "btnMarcarPago";
            btnMarcarPago.Size = new Size(142, 41);
            btnMarcarPago.TabIndex = 2;
            btnMarcarPago.Text = "Marcar como Pago";
            btnMarcarPago.UseVisualStyleBackColor = false;
            btnMarcarPago.Click += btnMarcarPago_Click;
            // 
            // btnAtualizar
            // 
            btnAtualizar.BackColor = SystemColors.Highlight;
            btnAtualizar.FlatAppearance.BorderSize = 0;
            btnAtualizar.FlatStyle = FlatStyle.Flat;
            btnAtualizar.ForeColor = Color.White;
            btnAtualizar.Location = new Point(747, 2);
            btnAtualizar.Margin = new Padding(3, 2, 3, 2);
            btnAtualizar.Name = "btnAtualizar";
            btnAtualizar.Size = new Size(82, 41);
            btnAtualizar.TabIndex = 3;
            btnAtualizar.Text = "Atualizar";
            btnAtualizar.UseVisualStyleBackColor = false;
            btnAtualizar.Click += btnAtualizar_Click;
            // 
            // btnFiltrar
            // 
            btnFiltrar.BackColor = SystemColors.HotTrack;
            btnFiltrar.FlatStyle = FlatStyle.Flat;
            btnFiltrar.ForeColor = SystemColors.ButtonHighlight;
            btnFiltrar.Location = new Point(676, 36);
            btnFiltrar.Margin = new Padding(3, 2, 3, 2);
            btnFiltrar.Name = "btnFiltrar";
            btnFiltrar.Size = new Size(82, 22);
            btnFiltrar.TabIndex = 4;
            btnFiltrar.Text = "Filtrar";
            btnFiltrar.UseVisualStyleBackColor = false;
            btnFiltrar.Click += btnFiltrar_Click;
            // 
            // panel1
            // 
            panel1.Controls.Add(pnlBotoes);
            panel1.Dock = DockStyle.Top;
            panel1.Location = new Point(0, 0);
            panel1.Margin = new Padding(3, 2, 3, 2);
            panel1.Name = "panel1";
            panel1.Size = new Size(832, 46);
            panel1.TabIndex = 6;
            // 
            // pnlBotoes
            // 
            pnlBotoes.BackColor = Color.Gray;
            pnlBotoes.Controls.Add(btnAtualizar);
            pnlBotoes.Controls.Add(btnMarcarPago);
            pnlBotoes.Controls.Add(btnExcluir);
            pnlBotoes.Controls.Add(btnEditar);
            pnlBotoes.Controls.Add(btnNovo);
            pnlBotoes.Controls.Add(panel3);
            pnlBotoes.Dock = DockStyle.Fill;
            pnlBotoes.FlowDirection = FlowDirection.RightToLeft;
            pnlBotoes.Location = new Point(0, 0);
            pnlBotoes.Margin = new Padding(3, 2, 3, 2);
            pnlBotoes.Name = "pnlBotoes";
            pnlBotoes.Size = new Size(832, 46);
            pnlBotoes.TabIndex = 5;
            // 
            // btnEditar
            // 
            btnEditar.BackColor = SystemColors.Highlight;
            btnEditar.FlatAppearance.BorderSize = 0;
            btnEditar.FlatStyle = FlatStyle.Flat;
            btnEditar.ForeColor = Color.White;
            btnEditar.Location = new Point(423, 2);
            btnEditar.Margin = new Padding(3, 2, 3, 2);
            btnEditar.Name = "btnEditar";
            btnEditar.Size = new Size(82, 41);
            btnEditar.TabIndex = 4;
            btnEditar.Text = "Editar";
            btnEditar.UseVisualStyleBackColor = false;
            btnEditar.Click += btnEditar_Click;
            // 
            // lblFiltroPessoa
            // 
            lblFiltroPessoa.AutoSize = true;
            lblFiltroPessoa.Location = new Point(30, 19);
            lblFiltroPessoa.Name = "lblFiltroPessoa";
            lblFiltroPessoa.Size = new Size(43, 15);
            lblFiltroPessoa.TabIndex = 7;
            lblFiltroPessoa.Text = "Pessoa";
            // 
            // cmbFiltroPessoa
            // 
            cmbFiltroPessoa.FormattingEnabled = true;
            cmbFiltroPessoa.Location = new Point(30, 36);
            cmbFiltroPessoa.Margin = new Padding(3, 2, 3, 2);
            cmbFiltroPessoa.Name = "cmbFiltroPessoa";
            cmbFiltroPessoa.Size = new Size(133, 23);
            cmbFiltroPessoa.TabIndex = 8;
            cmbFiltroPessoa.Text = "<Todos>";
            // 
            // lblFiltroStatus
            // 
            lblFiltroStatus.AutoSize = true;
            lblFiltroStatus.Location = new Point(172, 18);
            lblFiltroStatus.Name = "lblFiltroStatus";
            lblFiltroStatus.Size = new Size(39, 15);
            lblFiltroStatus.TabIndex = 9;
            lblFiltroStatus.Text = "Status";
            // 
            // dgvLancamentos
            // 
            dgvLancamentos.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvLancamentos.Location = new Point(10, 117);
            dgvLancamentos.Margin = new Padding(3, 2, 3, 2);
            dgvLancamentos.Name = "dgvLancamentos";
            dgvLancamentos.RowHeadersWidth = 51;
            dgvLancamentos.Size = new Size(811, 252);
            dgvLancamentos.TabIndex = 11;
            dgvLancamentos.CellFormatting += dgvLancamentos_CellFormatting;
            dgvLancamentos.CellPainting += dgvLancamentos_CellPainting;
            // 
            // cmbFiltroStatus
            // 
            cmbFiltroStatus.FormattingEnabled = true;
            cmbFiltroStatus.Location = new Point(172, 36);
            cmbFiltroStatus.Margin = new Padding(3, 2, 3, 2);
            cmbFiltroStatus.Name = "cmbFiltroStatus";
            cmbFiltroStatus.Size = new Size(133, 23);
            cmbFiltroStatus.TabIndex = 12;
            cmbFiltroStatus.Text = "<Todos>";
            // 
            // cmbFiltroTipo
            // 
            cmbFiltroTipo.FormattingEnabled = true;
            cmbFiltroTipo.Location = new Point(310, 36);
            cmbFiltroTipo.Margin = new Padding(3, 2, 3, 2);
            cmbFiltroTipo.Name = "cmbFiltroTipo";
            cmbFiltroTipo.Size = new Size(133, 23);
            cmbFiltroTipo.TabIndex = 13;
            cmbFiltroTipo.Text = "<Todos>";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(310, 19);
            label1.Name = "label1";
            label1.Size = new Size(34, 15);
            label1.TabIndex = 14;
            label1.Text = "Tipo:";
            // 
            // panel2
            // 
            panel2.BackColor = Color.Gray;
            panel2.Controls.Add(lblSaldoConta);
            panel2.Controls.Add(lblAtrasados);
            panel2.Controls.Add(lblPendentes);
            panel2.Controls.Add(lblPagos);
            panel2.Controls.Add(lblTotalGeral);
            panel2.Dock = DockStyle.Bottom;
            panel2.Location = new Point(0, 373);
            panel2.Margin = new Padding(3, 2, 3, 2);
            panel2.Name = "panel2";
            panel2.Size = new Size(832, 58);
            panel2.TabIndex = 15;
            // 
            // lblSaldoConta
            // 
            lblSaldoConta.AutoSize = true;
            lblSaldoConta.Location = new Point(610, 9);
            lblSaldoConta.Name = "lblSaldoConta";
            lblSaldoConta.Size = new Size(36, 15);
            lblSaldoConta.TabIndex = 4;
            lblSaldoConta.Text = "Saldo";
            // 
            // lblAtrasados
            // 
            lblAtrasados.AutoSize = true;
            lblAtrasados.Location = new Point(360, 36);
            lblAtrasados.Name = "lblAtrasados";
            lblAtrasados.Size = new Size(59, 15);
            lblAtrasados.TabIndex = 3;
            lblAtrasados.Text = "Atrasados";
            // 
            // lblPendentes
            // 
            lblPendentes.AutoSize = true;
            lblPendentes.Location = new Point(360, 9);
            lblPendentes.Name = "lblPendentes";
            lblPendentes.Size = new Size(62, 15);
            lblPendentes.TabIndex = 2;
            lblPendentes.Text = "Pendentes";
            // 
            // lblPagos
            // 
            lblPagos.AutoSize = true;
            lblPagos.Location = new Point(10, 36);
            lblPagos.Name = "lblPagos";
            lblPagos.Size = new Size(39, 15);
            lblPagos.TabIndex = 1;
            lblPagos.Text = "Pagos";
            // 
            // lblTotalGeral
            // 
            lblTotalGeral.AutoSize = true;
            lblTotalGeral.Location = new Point(10, 9);
            lblTotalGeral.Name = "lblTotalGeral";
            lblTotalGeral.Size = new Size(63, 15);
            lblTotalGeral.TabIndex = 0;
            lblTotalGeral.Text = "Total Geral";
            // 
            // dtpDataInicio
            // 
            dtpDataInicio.Format = DateTimePickerFormat.Short;
            dtpDataInicio.Location = new Point(447, 36);
            dtpDataInicio.Margin = new Padding(3, 2, 3, 2);
            dtpDataInicio.Name = "dtpDataInicio";
            dtpDataInicio.Size = new Size(100, 23);
            dtpDataInicio.TabIndex = 16;
            // 
            // dtpDataFim
            // 
            dtpDataFim.Format = DateTimePickerFormat.Short;
            dtpDataFim.Location = new Point(570, 37);
            dtpDataFim.Margin = new Padding(3, 2, 3, 2);
            dtpDataFim.Name = "dtpDataFim";
            dtpDataFim.Size = new Size(100, 23);
            dtpDataFim.TabIndex = 17;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(552, 38);
            label2.Name = "label2";
            label2.Size = new Size(12, 15);
            label2.TabIndex = 18;
            label2.Text = "-";
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(label2);
            groupBox1.Controls.Add(cmbFiltroPessoa);
            groupBox1.Controls.Add(dtpDataFim);
            groupBox1.Controls.Add(lblFiltroPessoa);
            groupBox1.Controls.Add(dtpDataInicio);
            groupBox1.Controls.Add(cmbFiltroTipo);
            groupBox1.Controls.Add(btnFiltrar);
            groupBox1.Controls.Add(label1);
            groupBox1.Controls.Add(lblFiltroStatus);
            groupBox1.Controls.Add(cmbFiltroStatus);
            groupBox1.Location = new Point(10, 50);
            groupBox1.Margin = new Padding(3, 2, 3, 2);
            groupBox1.Name = "groupBox1";
            groupBox1.Padding = new Padding(3, 2, 3, 2);
            groupBox1.Size = new Size(811, 62);
            groupBox1.TabIndex = 19;
            groupBox1.TabStop = false;
            groupBox1.Text = "Pesquisa";
            // 
            // panel3
            // 
            panel3.Location = new Point(3, 3);
            panel3.Name = "panel3";
            panel3.Size = new Size(326, 40);
            panel3.TabIndex = 5;
            // 
            // FrmLancamentos
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.White;
            ClientSize = new Size(832, 431);
            Controls.Add(panel2);
            Controls.Add(dgvLancamentos);
            Controls.Add(panel1);
            Controls.Add(groupBox1);
            Margin = new Padding(3, 2, 3, 2);
            Name = "FrmLancamentos";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Financeiro Pessoal - Lançamentos";
            Load += FrmLancamentos_Load;
            panel1.ResumeLayout(false);
            pnlBotoes.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvLancamentos).EndInit();
            panel2.ResumeLayout(false);
            panel2.PerformLayout();
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private Button btnNovo;
        private Button btnExcluir;
        private Button btnMarcarPago;
        private Button btnAtualizar;
        private Button btnFiltrar;
        private Panel panel1;
        private Label lblFiltroPessoa;
        private ComboBox cmbFiltroPessoa;
        private Label lblFiltroStatus;
        private DataGridView dgvLancamentos;
        private ComboBox cmbFiltroStatus;
        private Button btnEditar;
        private ComboBox cmbFiltroTipo;
        private Label label1;
        private Panel panel2;
        private Label lblAtrasados;
        private Label lblPendentes;
        private Label lblPagos;
        private Label lblTotalGeral;
        private FlowLayoutPanel pnlBotoes;
        private DateTimePicker dtpDataInicio;
        private DateTimePicker dtpDataFim;
        private Label label2;
        private GroupBox groupBox1;
        private Label lblSaldoConta;
        private Panel panel3;
    }
}