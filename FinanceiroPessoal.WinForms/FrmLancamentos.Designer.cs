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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmLancamentos));
            btnNovo = new Button();
            btnExcluir = new Button();
            btnMarcarPago = new Button();
            btnAtualizar = new Button();
            btnFiltrar = new Button();
            panel1 = new Panel();
            pnlBotoes = new FlowLayoutPanel();
            btnEditar = new Button();
            panel3 = new Panel();
            btnVoltar = new Button();
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
            btnNovo.Image = Properties.Resources.ic_local_hospital_128_28459;
            btnNovo.ImageAlign = ContentAlignment.MiddleLeft;
            btnNovo.Location = new Point(352, 3);
            btnNovo.Name = "btnNovo";
            btnNovo.Size = new Size(94, 55);
            btnNovo.TabIndex = 0;
            btnNovo.Text = "Novo";
            btnNovo.TextAlign = ContentAlignment.MiddleRight;
            btnNovo.UseVisualStyleBackColor = false;
            btnNovo.Click += btnNovo_Click;
            // 
            // btnExcluir
            // 
            btnExcluir.BackColor = SystemColors.Highlight;
            btnExcluir.FlatAppearance.BorderSize = 0;
            btnExcluir.FlatStyle = FlatStyle.Flat;
            btnExcluir.ForeColor = Color.White;
            btnExcluir.Image = Properties.Resources.ic_highlight_remove_128_28524;
            btnExcluir.ImageAlign = ContentAlignment.MiddleLeft;
            btnExcluir.Location = new Point(552, 3);
            btnExcluir.Name = "btnExcluir";
            btnExcluir.Size = new Size(94, 55);
            btnExcluir.TabIndex = 1;
            btnExcluir.Text = "Excluir";
            btnExcluir.TextAlign = ContentAlignment.MiddleRight;
            btnExcluir.UseVisualStyleBackColor = false;
            btnExcluir.Click += btnExcluir_Click;
            // 
            // btnMarcarPago
            // 
            btnMarcarPago.BackColor = SystemColors.Highlight;
            btnMarcarPago.FlatAppearance.BorderSize = 0;
            btnMarcarPago.FlatStyle = FlatStyle.Flat;
            btnMarcarPago.ForeColor = Color.White;
            btnMarcarPago.Image = Properties.Resources.ic_check_128_28312;
            btnMarcarPago.ImageAlign = ContentAlignment.MiddleLeft;
            btnMarcarPago.Location = new Point(652, 3);
            btnMarcarPago.Name = "btnMarcarPago";
            btnMarcarPago.Size = new Size(178, 55);
            btnMarcarPago.TabIndex = 2;
            btnMarcarPago.Text = "Marcar como Pago";
            btnMarcarPago.TextAlign = ContentAlignment.MiddleRight;
            btnMarcarPago.UseVisualStyleBackColor = false;
            btnMarcarPago.Click += btnMarcarPago_Click;
            // 
            // btnAtualizar
            // 
            btnAtualizar.BackColor = SystemColors.Highlight;
            btnAtualizar.FlatAppearance.BorderSize = 0;
            btnAtualizar.FlatStyle = FlatStyle.Flat;
            btnAtualizar.ForeColor = Color.White;
            btnAtualizar.Image = Properties.Resources.ic_loop_128_28425;
            btnAtualizar.ImageAlign = ContentAlignment.MiddleLeft;
            btnAtualizar.Location = new Point(836, 3);
            btnAtualizar.Name = "btnAtualizar";
            btnAtualizar.Size = new Size(112, 55);
            btnAtualizar.TabIndex = 3;
            btnAtualizar.Text = "Atualizar";
            btnAtualizar.TextAlign = ContentAlignment.MiddleRight;
            btnAtualizar.UseVisualStyleBackColor = false;
            btnAtualizar.Click += btnAtualizar_Click;
            // 
            // btnFiltrar
            // 
            btnFiltrar.BackColor = SystemColors.HotTrack;
            btnFiltrar.FlatStyle = FlatStyle.Flat;
            btnFiltrar.ForeColor = SystemColors.ButtonHighlight;
            btnFiltrar.Image = Properties.Resources.ic_search_128_28722;
            btnFiltrar.ImageAlign = ContentAlignment.MiddleLeft;
            btnFiltrar.Location = new Point(773, 48);
            btnFiltrar.Name = "btnFiltrar";
            btnFiltrar.Size = new Size(108, 29);
            btnFiltrar.TabIndex = 4;
            btnFiltrar.Text = "Filtrar";
            btnFiltrar.TextAlign = ContentAlignment.MiddleRight;
            btnFiltrar.UseVisualStyleBackColor = false;
            btnFiltrar.Click += btnFiltrar_Click;
            // 
            // panel1
            // 
            panel1.Controls.Add(pnlBotoes);
            panel1.Dock = DockStyle.Top;
            panel1.Location = new Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(951, 61);
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
            pnlBotoes.Controls.Add(btnVoltar);
            pnlBotoes.Dock = DockStyle.Fill;
            pnlBotoes.FlowDirection = FlowDirection.RightToLeft;
            pnlBotoes.Location = new Point(0, 0);
            pnlBotoes.Name = "pnlBotoes";
            pnlBotoes.Size = new Size(951, 61);
            pnlBotoes.TabIndex = 5;
            // 
            // btnEditar
            // 
            btnEditar.BackColor = SystemColors.Highlight;
            btnEditar.FlatAppearance.BorderSize = 0;
            btnEditar.FlatStyle = FlatStyle.Flat;
            btnEditar.ForeColor = Color.White;
            btnEditar.Image = Properties.Resources.ic_create_128_28283;
            btnEditar.ImageAlign = ContentAlignment.MiddleLeft;
            btnEditar.Location = new Point(452, 3);
            btnEditar.Name = "btnEditar";
            btnEditar.Size = new Size(94, 55);
            btnEditar.TabIndex = 4;
            btnEditar.Text = "Editar";
            btnEditar.TextAlign = ContentAlignment.MiddleRight;
            btnEditar.UseVisualStyleBackColor = false;
            btnEditar.Click += btnEditar_Click;
            // 
            // panel3
            // 
            panel3.Location = new Point(105, 4);
            panel3.Margin = new Padding(3, 4, 3, 4);
            panel3.Name = "panel3";
            panel3.Size = new Size(241, 53);
            panel3.TabIndex = 5;
            // 
            // btnVoltar
            // 
            btnVoltar.BackColor = SystemColors.Highlight;
            btnVoltar.FlatAppearance.BorderSize = 0;
            btnVoltar.FlatAppearance.MouseDownBackColor = Color.FromArgb(255, 128, 0);
            btnVoltar.FlatAppearance.MouseOverBackColor = Color.Red;
            btnVoltar.FlatStyle = FlatStyle.Flat;
            btnVoltar.ForeColor = Color.White;
            btnVoltar.Image = Properties.Resources.ic_keyboard_backspace_128_28494;
            btnVoltar.ImageAlign = ContentAlignment.MiddleLeft;
            btnVoltar.Location = new Point(5, 3);
            btnVoltar.Name = "btnVoltar";
            btnVoltar.Size = new Size(94, 55);
            btnVoltar.TabIndex = 6;
            btnVoltar.Text = "Voltar";
            btnVoltar.TextAlign = ContentAlignment.MiddleRight;
            btnVoltar.UseVisualStyleBackColor = false;
            btnVoltar.Click += btnVoltar_Click;
            // 
            // lblFiltroPessoa
            // 
            lblFiltroPessoa.AutoSize = true;
            lblFiltroPessoa.Location = new Point(34, 25);
            lblFiltroPessoa.Name = "lblFiltroPessoa";
            lblFiltroPessoa.Size = new Size(53, 20);
            lblFiltroPessoa.TabIndex = 7;
            lblFiltroPessoa.Text = "Pessoa";
            // 
            // cmbFiltroPessoa
            // 
            cmbFiltroPessoa.FormattingEnabled = true;
            cmbFiltroPessoa.Location = new Point(34, 48);
            cmbFiltroPessoa.Name = "cmbFiltroPessoa";
            cmbFiltroPessoa.Size = new Size(151, 28);
            cmbFiltroPessoa.TabIndex = 8;
            cmbFiltroPessoa.Text = "<Todos>";
            // 
            // lblFiltroStatus
            // 
            lblFiltroStatus.AutoSize = true;
            lblFiltroStatus.Location = new Point(197, 24);
            lblFiltroStatus.Name = "lblFiltroStatus";
            lblFiltroStatus.Size = new Size(49, 20);
            lblFiltroStatus.TabIndex = 9;
            lblFiltroStatus.Text = "Status";
            // 
            // dgvLancamentos
            // 
            dgvLancamentos.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvLancamentos.Location = new Point(11, 156);
            dgvLancamentos.Name = "dgvLancamentos";
            dgvLancamentos.RowHeadersWidth = 51;
            dgvLancamentos.Size = new Size(927, 336);
            dgvLancamentos.TabIndex = 11;
            dgvLancamentos.CellFormatting += dgvLancamentos_CellFormatting;
            dgvLancamentos.CellPainting += dgvLancamentos_CellPainting;
            // 
            // cmbFiltroStatus
            // 
            cmbFiltroStatus.FormattingEnabled = true;
            cmbFiltroStatus.Location = new Point(197, 48);
            cmbFiltroStatus.Name = "cmbFiltroStatus";
            cmbFiltroStatus.Size = new Size(151, 28);
            cmbFiltroStatus.TabIndex = 12;
            cmbFiltroStatus.Text = "<Todos>";
            // 
            // cmbFiltroTipo
            // 
            cmbFiltroTipo.FormattingEnabled = true;
            cmbFiltroTipo.Location = new Point(354, 48);
            cmbFiltroTipo.Name = "cmbFiltroTipo";
            cmbFiltroTipo.Size = new Size(151, 28);
            cmbFiltroTipo.TabIndex = 13;
            cmbFiltroTipo.Text = "<Todos>";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(354, 25);
            label1.Name = "label1";
            label1.Size = new Size(42, 20);
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
            panel2.Location = new Point(0, 498);
            panel2.Name = "panel2";
            panel2.Size = new Size(951, 77);
            panel2.TabIndex = 15;
            // 
            // lblSaldoConta
            // 
            lblSaldoConta.AutoSize = true;
            lblSaldoConta.Location = new Point(697, 12);
            lblSaldoConta.Name = "lblSaldoConta";
            lblSaldoConta.Size = new Size(47, 20);
            lblSaldoConta.TabIndex = 4;
            lblSaldoConta.Text = "Saldo";
            // 
            // lblAtrasados
            // 
            lblAtrasados.AutoSize = true;
            lblAtrasados.Location = new Point(411, 48);
            lblAtrasados.Name = "lblAtrasados";
            lblAtrasados.Size = new Size(75, 20);
            lblAtrasados.TabIndex = 3;
            lblAtrasados.Text = "Atrasados";
            // 
            // lblPendentes
            // 
            lblPendentes.AutoSize = true;
            lblPendentes.Location = new Point(411, 12);
            lblPendentes.Name = "lblPendentes";
            lblPendentes.Size = new Size(76, 20);
            lblPendentes.TabIndex = 2;
            lblPendentes.Text = "Pendentes";
            // 
            // lblPagos
            // 
            lblPagos.AutoSize = true;
            lblPagos.Location = new Point(11, 48);
            lblPagos.Name = "lblPagos";
            lblPagos.Size = new Size(48, 20);
            lblPagos.TabIndex = 1;
            lblPagos.Text = "Pagos";
            // 
            // lblTotalGeral
            // 
            lblTotalGeral.AutoSize = true;
            lblTotalGeral.Location = new Point(11, 12);
            lblTotalGeral.Name = "lblTotalGeral";
            lblTotalGeral.Size = new Size(81, 20);
            lblTotalGeral.TabIndex = 0;
            lblTotalGeral.Text = "Total Geral";
            // 
            // dtpDataInicio
            // 
            dtpDataInicio.Format = DateTimePickerFormat.Short;
            dtpDataInicio.Location = new Point(511, 48);
            dtpDataInicio.Name = "dtpDataInicio";
            dtpDataInicio.Size = new Size(114, 27);
            dtpDataInicio.TabIndex = 16;
            // 
            // dtpDataFim
            // 
            dtpDataFim.Format = DateTimePickerFormat.Short;
            dtpDataFim.Location = new Point(651, 49);
            dtpDataFim.Name = "dtpDataFim";
            dtpDataFim.Size = new Size(114, 27);
            dtpDataFim.TabIndex = 17;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(631, 51);
            label2.Name = "label2";
            label2.Size = new Size(15, 20);
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
            groupBox1.Location = new Point(11, 67);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(927, 83);
            groupBox1.TabIndex = 19;
            groupBox1.TabStop = false;
            groupBox1.Text = "Pesquisa";
            // 
            // FrmLancamentos
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.White;
            ClientSize = new Size(951, 575);
            Controls.Add(panel2);
            Controls.Add(dgvLancamentos);
            Controls.Add(panel1);
            Controls.Add(groupBox1);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "FrmLancamentos";
            ShowInTaskbar = false;
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
        private Button btnVoltar;
    }
}