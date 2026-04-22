namespace FinanceiroPessoal.WinForms
{
    partial class FrmExtratoCartao
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
            panel1 = new Panel();
            btnFechar = new Button();
            lblReferencia = new Label();
            lblExtrato = new Label();
            pictureBox1 = new PictureBox();
            panel2 = new Panel();
            lblTotalFatura = new Label();
            label1 = new Label();
            panel3 = new Panel();
            lblTotalPago = new Label();
            label2 = new Label();
            panel4 = new Panel();
            lblTotalPendente = new Label();
            label3 = new Label();
            panel5 = new Panel();
            btnMarcarPago = new Button();
            label4 = new Label();
            panel6 = new Panel();
            dgvExtrato = new DataGridView();
            panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            panel2.SuspendLayout();
            panel3.SuspendLayout();
            panel4.SuspendLayout();
            panel5.SuspendLayout();
            panel6.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvExtrato).BeginInit();
            SuspendLayout();
            // 
            // panel1
            // 
            panel1.BackColor = Color.FromArgb(24, 95, 165);
            panel1.Controls.Add(btnFechar);
            panel1.Controls.Add(lblReferencia);
            panel1.Controls.Add(lblExtrato);
            panel1.Controls.Add(pictureBox1);
            panel1.Dock = DockStyle.Top;
            panel1.Location = new Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(789, 86);
            panel1.TabIndex = 0;
            // 
            // btnFechar
            // 
            btnFechar.BackColor = Color.FromArgb(230, 241, 251);
            btnFechar.FlatAppearance.BorderColor = Color.White;
            btnFechar.FlatAppearance.BorderSize = 0;
            btnFechar.FlatStyle = FlatStyle.Flat;
            btnFechar.ForeColor = Color.FromArgb(12, 68, 124);
            btnFechar.Image = Properties.Resources.interface_decline_reject_close_delete_failed_icon_133000;
            btnFechar.ImageAlign = ContentAlignment.MiddleLeft;
            btnFechar.Location = new Point(672, 24);
            btnFechar.Name = "btnFechar";
            btnFechar.Size = new Size(105, 43);
            btnFechar.TabIndex = 3;
            btnFechar.Text = "Fechar";
            btnFechar.TextAlign = ContentAlignment.MiddleRight;
            btnFechar.UseVisualStyleBackColor = false;
            btnFechar.Click += btnFechar_Click;
            // 
            // lblReferencia
            // 
            lblReferencia.AutoSize = true;
            lblReferencia.ForeColor = Color.FromArgb(230, 241, 251);
            lblReferencia.Location = new Point(97, 47);
            lblReferencia.Name = "lblReferencia";
            lblReferencia.Size = new Size(79, 20);
            lblReferencia.TabIndex = 2;
            lblReferencia.Text = "Referencia";
            // 
            // lblExtrato
            // 
            lblExtrato.AutoSize = true;
            lblExtrato.Font = new Font("Segoe UI", 10.2F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblExtrato.ForeColor = SystemColors.ButtonHighlight;
            lblExtrato.Location = new Point(97, 24);
            lblExtrato.Name = "lblExtrato";
            lblExtrato.Size = new Size(68, 23);
            lblExtrato.TabIndex = 1;
            lblExtrato.Text = "Extrato";
            // 
            // pictureBox1
            // 
            pictureBox1.Image = Properties.Resources.credit_card_payment_finance_credit_card_icon_124664__1_;
            pictureBox1.Location = new Point(12, 12);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(79, 62);
            pictureBox1.SizeMode = PictureBoxSizeMode.CenterImage;
            pictureBox1.TabIndex = 0;
            pictureBox1.TabStop = false;
            // 
            // panel2
            // 
            panel2.BackColor = SystemColors.ButtonHighlight;
            panel2.Controls.Add(lblTotalFatura);
            panel2.Controls.Add(label1);
            panel2.Location = new Point(12, 92);
            panel2.Name = "panel2";
            panel2.Size = new Size(250, 125);
            panel2.TabIndex = 1;
            // 
            // lblTotalFatura
            // 
            lblTotalFatura.AutoSize = true;
            lblTotalFatura.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblTotalFatura.Location = new Point(3, 46);
            lblTotalFatura.Name = "lblTotalFatura";
            lblTotalFatura.Size = new Size(142, 28);
            lblTotalFatura.TabIndex = 1;
            lblTotalFatura.Text = "lblTotalFatura";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(3, 12);
            label1.Name = "label1";
            label1.Size = new Size(110, 20);
            label1.TabIndex = 0;
            label1.Text = "Total da Fatura:";
            // 
            // panel3
            // 
            panel3.BackColor = SystemColors.ButtonHighlight;
            panel3.Controls.Add(lblTotalPago);
            panel3.Controls.Add(label2);
            panel3.Location = new Point(268, 92);
            panel3.Name = "panel3";
            panel3.Size = new Size(250, 125);
            panel3.TabIndex = 2;
            // 
            // lblTotalPago
            // 
            lblTotalPago.AutoSize = true;
            lblTotalPago.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblTotalPago.Location = new Point(3, 46);
            lblTotalPago.Name = "lblTotalPago";
            lblTotalPago.Size = new Size(130, 28);
            lblTotalPago.TabIndex = 2;
            lblTotalPago.Text = "lblTotalPago";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(3, 12);
            label2.Name = "label2";
            label2.Size = new Size(84, 20);
            label2.TabIndex = 0;
            label2.Text = "Total pago:";
            // 
            // panel4
            // 
            panel4.BackColor = SystemColors.ButtonHighlight;
            panel4.Controls.Add(lblTotalPendente);
            panel4.Controls.Add(label3);
            panel4.Location = new Point(524, 92);
            panel4.Name = "panel4";
            panel4.Size = new Size(250, 125);
            panel4.TabIndex = 2;
            // 
            // lblTotalPendente
            // 
            lblTotalPendente.AutoSize = true;
            lblTotalPendente.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblTotalPendente.Location = new Point(3, 46);
            lblTotalPendente.Name = "lblTotalPendente";
            lblTotalPendente.Size = new Size(171, 28);
            lblTotalPendente.TabIndex = 3;
            lblTotalPendente.Text = "lblTotalPendente";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(3, 12);
            label3.Name = "label3";
            label3.Size = new Size(112, 20);
            label3.TabIndex = 0;
            label3.Text = "Total pendente:";
            // 
            // panel5
            // 
            panel5.BackColor = SystemColors.ButtonHighlight;
            panel5.Controls.Add(btnMarcarPago);
            panel5.Controls.Add(label4);
            panel5.Controls.Add(panel6);
            panel5.Location = new Point(12, 223);
            panel5.Name = "panel5";
            panel5.Size = new Size(762, 394);
            panel5.TabIndex = 3;
            // 
            // btnMarcarPago
            // 
            btnMarcarPago.BackColor = Color.FromArgb(21, 128, 61);
            btnMarcarPago.FlatAppearance.BorderSize = 0;
            btnMarcarPago.FlatStyle = FlatStyle.Flat;
            btnMarcarPago.ForeColor = Color.White;
            btnMarcarPago.Location = new Point(546, 16);
            btnMarcarPago.Name = "btnMarcarPago";
            btnMarcarPago.Size = new Size(213, 29);
            btnMarcarPago.TabIndex = 2;
            btnMarcarPago.Text = "Marcar Fatura como Paga";
            btnMarcarPago.UseVisualStyleBackColor = false;
            btnMarcarPago.Click += btnMarcarPago_Click;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Font = new Font("Segoe UI", 10.2F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label4.Location = new Point(3, 18);
            label4.Name = "label4";
            label4.Size = new Size(198, 23);
            label4.TabIndex = 1;
            label4.Text = "Lançamentos do Cartão";
            // 
            // panel6
            // 
            panel6.Controls.Add(dgvExtrato);
            panel6.Location = new Point(3, 54);
            panel6.Name = "panel6";
            panel6.Size = new Size(756, 337);
            panel6.TabIndex = 0;
            // 
            // dgvExtrato
            // 
            dgvExtrato.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvExtrato.Dock = DockStyle.Fill;
            dgvExtrato.Location = new Point(0, 0);
            dgvExtrato.Name = "dgvExtrato";
            dgvExtrato.RowHeadersWidth = 51;
            dgvExtrato.Size = new Size(756, 337);
            dgvExtrato.TabIndex = 0;
            // 
            // FrmExtratoCartao
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(789, 629);
            Controls.Add(panel5);
            Controls.Add(panel4);
            Controls.Add(panel3);
            Controls.Add(panel2);
            Controls.Add(panel1);
            FormBorderStyle = FormBorderStyle.None;
            Name = "FrmExtratoCartao";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "FrmExtratoCartao";
            Load += FrmExtratoCartao_Load_1;
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            panel2.ResumeLayout(false);
            panel2.PerformLayout();
            panel3.ResumeLayout(false);
            panel3.PerformLayout();
            panel4.ResumeLayout(false);
            panel4.PerformLayout();
            panel5.ResumeLayout(false);
            panel5.PerformLayout();
            panel6.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvExtrato).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private Panel panel1;
        private Button btnFechar;
        private Label lblReferencia;
        private Label lblExtrato;
        private PictureBox pictureBox1;
        private Panel panel2;
        private Label lblTotalFatura;
        private Label label1;
        private Panel panel3;
        private Label label2;
        private Panel panel4;
        private Label label3;
        private Label lblTotalPago;
        private Label lblTotalPendente;
        private Panel panel5;
        private Panel panel6;
        private DataGridView dgvExtrato;
        private Button btnMarcarPago;
        private Label label4;
    }
}