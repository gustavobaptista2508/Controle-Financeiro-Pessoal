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
            btnEditar = new Button();
            lblFiltroPessoa = new Label();
            cmbFiltroPessoa = new ComboBox();
            lblFiltroStatus = new Label();
            dgvLancamentos = new DataGridView();
            cmbFiltroStatus = new ComboBox();
            cmbFiltroTipo = new ComboBox();
            label1 = new Label();
            panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvLancamentos).BeginInit();
            SuspendLayout();
            // 
            // btnNovo
            // 
            btnNovo.Location = new Point(12, 12);
            btnNovo.Name = "btnNovo";
            btnNovo.Size = new Size(94, 29);
            btnNovo.TabIndex = 0;
            btnNovo.Text = "Novo";
            btnNovo.UseVisualStyleBackColor = true;
            btnNovo.Click += btnNovo_Click;
            // 
            // btnExcluir
            // 
            btnExcluir.Location = new Point(212, 12);
            btnExcluir.Name = "btnExcluir";
            btnExcluir.Size = new Size(94, 29);
            btnExcluir.TabIndex = 1;
            btnExcluir.Text = "Excluir";
            btnExcluir.UseVisualStyleBackColor = true;
            btnExcluir.Click += btnExcluir_Click;
            // 
            // btnMarcarPago
            // 
            btnMarcarPago.Location = new Point(314, 12);
            btnMarcarPago.Name = "btnMarcarPago";
            btnMarcarPago.Size = new Size(162, 29);
            btnMarcarPago.TabIndex = 2;
            btnMarcarPago.Text = "Marcar como Pago";
            btnMarcarPago.UseVisualStyleBackColor = true;
            btnMarcarPago.Click += btnMarcarPago_Click;
            // 
            // btnAtualizar
            // 
            btnAtualizar.Location = new Point(482, 12);
            btnAtualizar.Name = "btnAtualizar";
            btnAtualizar.Size = new Size(94, 29);
            btnAtualizar.TabIndex = 3;
            btnAtualizar.Text = "Atualizar";
            btnAtualizar.UseVisualStyleBackColor = true;
            btnAtualizar.Click += btnAtualizar_Click;
            // 
            // btnFiltrar
            // 
            btnFiltrar.BackColor = SystemColors.HotTrack;
            btnFiltrar.ForeColor = SystemColors.ButtonHighlight;
            btnFiltrar.Location = new Point(694, 67);
            btnFiltrar.Name = "btnFiltrar";
            btnFiltrar.Size = new Size(94, 29);
            btnFiltrar.TabIndex = 4;
            btnFiltrar.Text = "Filtrar";
            btnFiltrar.UseVisualStyleBackColor = false;
            btnFiltrar.Click += btnFiltrar_Click;
            // 
            // panel1
            // 
            panel1.Controls.Add(btnEditar);
            panel1.Controls.Add(btnAtualizar);
            panel1.Controls.Add(btnMarcarPago);
            panel1.Controls.Add(btnExcluir);
            panel1.Dock = DockStyle.Top;
            panel1.Location = new Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(800, 61);
            panel1.TabIndex = 6;
            // 
            // btnEditar
            // 
            btnEditar.Location = new Point(112, 12);
            btnEditar.Name = "btnEditar";
            btnEditar.Size = new Size(94, 29);
            btnEditar.TabIndex = 4;
            btnEditar.Text = "Editar";
            btnEditar.UseVisualStyleBackColor = true;
            btnEditar.Click += btnEditar_Click;
            // 
            // lblFiltroPessoa
            // 
            lblFiltroPessoa.AutoSize = true;
            lblFiltroPessoa.Location = new Point(12, 71);
            lblFiltroPessoa.Name = "lblFiltroPessoa";
            lblFiltroPessoa.Size = new Size(53, 20);
            lblFiltroPessoa.TabIndex = 7;
            lblFiltroPessoa.Text = "Pessoa";
            // 
            // cmbFiltroPessoa
            // 
            cmbFiltroPessoa.FormattingEnabled = true;
            cmbFiltroPessoa.Location = new Point(73, 67);
            cmbFiltroPessoa.Name = "cmbFiltroPessoa";
            cmbFiltroPessoa.Size = new Size(151, 28);
            cmbFiltroPessoa.TabIndex = 8;
            // 
            // lblFiltroStatus
            // 
            lblFiltroStatus.AutoSize = true;
            lblFiltroStatus.Location = new Point(232, 71);
            lblFiltroStatus.Name = "lblFiltroStatus";
            lblFiltroStatus.Size = new Size(49, 20);
            lblFiltroStatus.TabIndex = 9;
            lblFiltroStatus.Text = "Status";
            // 
            // dgvLancamentos
            // 
            dgvLancamentos.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvLancamentos.Location = new Point(12, 102);
            dgvLancamentos.Name = "dgvLancamentos";
            dgvLancamentos.RowHeadersWidth = 51;
            dgvLancamentos.Size = new Size(776, 336);
            dgvLancamentos.TabIndex = 11;
            // 
            // cmbFiltroStatus
            // 
            cmbFiltroStatus.FormattingEnabled = true;
            cmbFiltroStatus.Location = new Point(289, 67);
            cmbFiltroStatus.Name = "cmbFiltroStatus";
            cmbFiltroStatus.Size = new Size(151, 28);
            cmbFiltroStatus.TabIndex = 12;
            // 
            // cmbFiltroTipo
            // 
            cmbFiltroTipo.FormattingEnabled = true;
            cmbFiltroTipo.Location = new Point(506, 67);
            cmbFiltroTipo.Name = "cmbFiltroTipo";
            cmbFiltroTipo.Size = new Size(151, 28);
            cmbFiltroTipo.TabIndex = 13;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(448, 71);
            label1.Name = "label1";
            label1.Size = new Size(42, 20);
            label1.TabIndex = 14;
            label1.Text = "Tipo:";
            // 
            // FrmLancamentos
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(label1);
            Controls.Add(cmbFiltroTipo);
            Controls.Add(cmbFiltroStatus);
            Controls.Add(dgvLancamentos);
            Controls.Add(lblFiltroStatus);
            Controls.Add(cmbFiltroPessoa);
            Controls.Add(lblFiltroPessoa);
            Controls.Add(btnFiltrar);
            Controls.Add(btnNovo);
            Controls.Add(panel1);
            Name = "FrmLancamentos";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "FrmLancamentos";
            Load += FrmLancamentos_Load;
            panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvLancamentos).EndInit();
            ResumeLayout(false);
            PerformLayout();
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
    }
}