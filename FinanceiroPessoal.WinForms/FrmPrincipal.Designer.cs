namespace FinanceiroPessoal.WinForms
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea1 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Legend legend1 = new System.Windows.Forms.DataVisualization.Charting.Legend();
            System.Windows.Forms.DataVisualization.Charting.Series series1 = new System.Windows.Forms.DataVisualization.Charting.Series();
            pnlSidebar = new Panel();
            flowLayoutPanel1 = new FlowLayoutPanel();
            pictureBox8 = new PictureBox();
            btnLancamentos = new Button();
            btnCategorias = new Button();
            btnContas = new Button();
            btnSair = new Button();
            lblSubtitulo = new Label();
            panel1 = new Panel();
            pnlResumo = new Panel();
            pictureBox10 = new PictureBox();
            label3 = new Label();
            lblResumoVencemSemana = new Label();
            lblResumoVencemHoje = new Label();
            lblResumoAtrasados = new Label();
            lblResumoTotalPendente = new Label();
            lblResumoTotalPago = new Label();
            lblResumoTotalLancamentos = new Label();
            pnlGrafico = new Panel();
            pictureBox2 = new PictureBox();
            lblGastoPorCategoria = new Label();
            chartCategorias = new System.Windows.Forms.DataVisualization.Charting.Chart();
            dgvResumoCategorias = new DataGridView();
            pnlAcoesRapidas = new Panel();
            pictureBox3 = new PictureBox();
            label2 = new Label();
            btnAcaoRelatorio = new Button();
            btnAcaoLancamentos = new Button();
            btnAcaoImportar = new Button();
            btnAcaoNovo = new Button();
            pnlTabela = new Panel();
            pictureBox1 = new PictureBox();
            label1 = new Label();
            dgvProximosVencimentos = new DataGridView();
            btnVerTodos = new Button();
            pnlCardSemana = new Panel();
            pictureBox4 = new PictureBox();
            label6 = new Label();
            lblSemanaQtd = new Label();
            lblSemanaValor = new Label();
            pnlCardPago = new Panel();
            pictureBox7 = new PictureBox();
            label5 = new Label();
            lblTotalEntradaQtd = new Label();
            lblTotalEntradaValor = new Label();
            pnlCardMes = new Panel();
            pictureBox5 = new PictureBox();
            label7 = new Label();
            lblSaldoMesQtd = new Label();
            lblSaldoMesValor = new Label();
            pnlCardPendente = new Panel();
            pictureBox6 = new PictureBox();
            label4 = new Label();
            lblTotalSaidaQtd = new Label();
            lblTotalSaidaValor = new Label();
            lblTituloDashboard = new Label();
            cmbCompetencia = new ComboBox();
            lblUsuario = new Label();
            lblBoasVindas = new Label();
            pictureBox9 = new PictureBox();
            panel2 = new Panel();
            pnlSidebar.SuspendLayout();
            flowLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox8).BeginInit();
            panel1.SuspendLayout();
            pnlResumo.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox10).BeginInit();
            pnlGrafico.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox2).BeginInit();
            ((System.ComponentModel.ISupportInitialize)chartCategorias).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dgvResumoCategorias).BeginInit();
            pnlAcoesRapidas.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox3).BeginInit();
            pnlTabela.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dgvProximosVencimentos).BeginInit();
            pnlCardSemana.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox4).BeginInit();
            pnlCardPago.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox7).BeginInit();
            pnlCardMes.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox5).BeginInit();
            pnlCardPendente.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox6).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox9).BeginInit();
            SuspendLayout();
            // 
            // pnlSidebar
            // 
            pnlSidebar.BackColor = Color.White;
            pnlSidebar.Controls.Add(flowLayoutPanel1);
            pnlSidebar.Location = new Point(0, 0);
            pnlSidebar.Name = "pnlSidebar";
            pnlSidebar.Size = new Size(230, 860);
            pnlSidebar.TabIndex = 0;
            // 
            // flowLayoutPanel1
            // 
            flowLayoutPanel1.Controls.Add(pictureBox8);
            flowLayoutPanel1.Controls.Add(btnLancamentos);
            flowLayoutPanel1.Controls.Add(btnCategorias);
            flowLayoutPanel1.Controls.Add(btnContas);
            flowLayoutPanel1.Controls.Add(btnSair);
            flowLayoutPanel1.Dock = DockStyle.Fill;
            flowLayoutPanel1.Location = new Point(0, 0);
            flowLayoutPanel1.Name = "flowLayoutPanel1";
            flowLayoutPanel1.Size = new Size(230, 860);
            flowLayoutPanel1.TabIndex = 0;
            // 
            // pictureBox8
            // 
            pictureBox8.Image = Properties.Resources.menu_dark;
            pictureBox8.Location = new Point(3, 3);
            pictureBox8.Name = "pictureBox8";
            pictureBox8.Size = new Size(70, 62);
            pictureBox8.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox8.TabIndex = 4;
            pictureBox8.TabStop = false;
            // 
            // btnLancamentos
            // 
            btnLancamentos.FlatStyle = FlatStyle.Popup;
            btnLancamentos.Location = new Point(3, 71);
            btnLancamentos.Name = "btnLancamentos";
            btnLancamentos.Size = new Size(221, 66);
            btnLancamentos.TabIndex = 0;
            btnLancamentos.Text = "button1";
            btnLancamentos.UseVisualStyleBackColor = true;
            btnLancamentos.Click += btnLancamentos_Click;
            // 
            // btnCategorias
            // 
            btnCategorias.FlatStyle = FlatStyle.Popup;
            btnCategorias.Location = new Point(3, 143);
            btnCategorias.Name = "btnCategorias";
            btnCategorias.Size = new Size(221, 66);
            btnCategorias.TabIndex = 1;
            btnCategorias.Text = "button2";
            btnCategorias.UseVisualStyleBackColor = true;
            btnCategorias.Click += btnCategorias_Click;
            // 
            // btnContas
            // 
            btnContas.FlatStyle = FlatStyle.Popup;
            btnContas.Location = new Point(3, 215);
            btnContas.Name = "btnContas";
            btnContas.Size = new Size(221, 66);
            btnContas.TabIndex = 2;
            btnContas.Text = "button3";
            btnContas.UseVisualStyleBackColor = true;
            btnContas.Click += btnContas_Click;
            // 
            // btnSair
            // 
            btnSair.FlatStyle = FlatStyle.Popup;
            btnSair.Location = new Point(3, 287);
            btnSair.Name = "btnSair";
            btnSair.Size = new Size(221, 66);
            btnSair.TabIndex = 3;
            btnSair.Text = "button4";
            btnSair.UseVisualStyleBackColor = true;
            btnSair.Click += btnSair_Click;
            // 
            // lblSubtitulo
            // 
            lblSubtitulo.AutoSize = true;
            lblSubtitulo.Font = new Font("Segoe UI", 7.8F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblSubtitulo.Location = new Point(246, 40);
            lblSubtitulo.Name = "lblSubtitulo";
            lblSubtitulo.Size = new Size(180, 17);
            lblSubtitulo.TabIndex = 0;
            lblSubtitulo.Text = "Visão geral das suas finanças";
            // 
            // panel1
            // 
            panel1.BackColor = Color.FromArgb(245, 246, 250);
            panel1.Controls.Add(pnlResumo);
            panel1.Controls.Add(pnlGrafico);
            panel1.Controls.Add(pnlAcoesRapidas);
            panel1.Controls.Add(pnlTabela);
            panel1.Controls.Add(pnlCardSemana);
            panel1.Controls.Add(pnlCardPago);
            panel1.Controls.Add(pnlCardMes);
            panel1.Controls.Add(pnlCardPendente);
            panel1.Location = new Point(230, 71);
            panel1.Name = "panel1";
            panel1.Size = new Size(1370, 789);
            panel1.TabIndex = 1;
            // 
            // pnlResumo
            // 
            pnlResumo.BackColor = Color.White;
            pnlResumo.Controls.Add(pictureBox10);
            pnlResumo.Controls.Add(label3);
            pnlResumo.Controls.Add(lblResumoVencemSemana);
            pnlResumo.Controls.Add(lblResumoVencemHoje);
            pnlResumo.Controls.Add(lblResumoAtrasados);
            pnlResumo.Controls.Add(lblResumoTotalPendente);
            pnlResumo.Controls.Add(lblResumoTotalPago);
            pnlResumo.Controls.Add(lblResumoTotalLancamentos);
            pnlResumo.Location = new Point(900, 449);
            pnlResumo.Name = "pnlResumo";
            pnlResumo.Size = new Size(420, 250);
            pnlResumo.TabIndex = 9;
            // 
            // pictureBox10
            // 
            pictureBox10.Image = Properties.Resources.dashboard_blue;
            pictureBox10.Location = new Point(22, 10);
            pictureBox10.Name = "pictureBox10";
            pictureBox10.Size = new Size(59, 34);
            pictureBox10.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox10.TabIndex = 10;
            pictureBox10.TabStop = false;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new Font("Segoe UI", 10.2F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label3.Location = new Point(87, 15);
            label3.Name = "label3";
            label3.Size = new Size(136, 23);
            label3.TabIndex = 9;
            label3.Text = "Resumo do Mês";
            // 
            // lblResumoVencemSemana
            // 
            lblResumoVencemSemana.AutoSize = true;
            lblResumoVencemSemana.ForeColor = Color.Coral;
            lblResumoVencemSemana.Location = new Point(20, 215);
            lblResumoVencemSemana.Name = "lblResumoVencemSemana";
            lblResumoVencemSemana.Size = new Size(203, 23);
            lblResumoVencemSemana.TabIndex = 5;
            lblResumoVencemSemana.Text = "Resumo Vencem Semana";
            // 
            // lblResumoVencemHoje
            // 
            lblResumoVencemHoje.AutoSize = true;
            lblResumoVencemHoje.Location = new Point(20, 183);
            lblResumoVencemHoje.Name = "lblResumoVencemHoje";
            lblResumoVencemHoje.Size = new Size(177, 23);
            lblResumoVencemHoje.TabIndex = 4;
            lblResumoVencemHoje.Text = "Resumo Vencem Hoje";
            // 
            // lblResumoAtrasados
            // 
            lblResumoAtrasados.AutoSize = true;
            lblResumoAtrasados.ForeColor = Color.Red;
            lblResumoAtrasados.Location = new Point(20, 151);
            lblResumoAtrasados.Name = "lblResumoAtrasados";
            lblResumoAtrasados.Size = new Size(151, 23);
            lblResumoAtrasados.TabIndex = 3;
            lblResumoAtrasados.Text = "Resumo Atrasados";
            // 
            // lblResumoTotalPendente
            // 
            lblResumoTotalPendente.AutoSize = true;
            lblResumoTotalPendente.ForeColor = Color.Red;
            lblResumoTotalPendente.Location = new Point(20, 119);
            lblResumoTotalPendente.Name = "lblResumoTotalPendente";
            lblResumoTotalPendente.Size = new Size(189, 23);
            lblResumoTotalPendente.TabIndex = 2;
            lblResumoTotalPendente.Text = "Resumo Total Pendente";
            // 
            // lblResumoTotalPago
            // 
            lblResumoTotalPago.AutoSize = true;
            lblResumoTotalPago.ForeColor = Color.LimeGreen;
            lblResumoTotalPago.Location = new Point(20, 87);
            lblResumoTotalPago.Name = "lblResumoTotalPago";
            lblResumoTotalPago.Size = new Size(155, 23);
            lblResumoTotalPago.TabIndex = 1;
            lblResumoTotalPago.Text = "Resumo Total Pago";
            // 
            // lblResumoTotalLancamentos
            // 
            lblResumoTotalLancamentos.AutoSize = true;
            lblResumoTotalLancamentos.Location = new Point(20, 55);
            lblResumoTotalLancamentos.Name = "lblResumoTotalLancamentos";
            lblResumoTotalLancamentos.Size = new Size(242, 23);
            lblResumoTotalLancamentos.TabIndex = 0;
            lblResumoTotalLancamentos.Text = "Resumo Total de Lançamentos";
            // 
            // pnlGrafico
            // 
            pnlGrafico.BackColor = Color.White;
            pnlGrafico.Controls.Add(pictureBox2);
            pnlGrafico.Controls.Add(lblGastoPorCategoria);
            pnlGrafico.Controls.Add(chartCategorias);
            pnlGrafico.Controls.Add(dgvResumoCategorias);
            pnlGrafico.Location = new Point(30, 449);
            pnlGrafico.Name = "pnlGrafico";
            pnlGrafico.Size = new Size(840, 250);
            pnlGrafico.TabIndex = 8;
            // 
            // pictureBox2
            // 
            pictureBox2.Image = Properties.Resources.dashboard_dark;
            pictureBox2.Location = new Point(13, 10);
            pictureBox2.Name = "pictureBox2";
            pictureBox2.Size = new Size(59, 34);
            pictureBox2.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox2.TabIndex = 9;
            pictureBox2.TabStop = false;
            // 
            // lblGastoPorCategoria
            // 
            lblGastoPorCategoria.AutoSize = true;
            lblGastoPorCategoria.Font = new Font("Segoe UI", 10.2F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblGastoPorCategoria.Location = new Point(78, 15);
            lblGastoPorCategoria.Name = "lblGastoPorCategoria";
            lblGastoPorCategoria.Size = new Size(171, 23);
            lblGastoPorCategoria.TabIndex = 2;
            lblGastoPorCategoria.Text = "Gasto por Categoria";
            // 
            // chartCategorias
            // 
            chartArea1.Name = "ChartArea1";
            chartCategorias.ChartAreas.Add(chartArea1);
            legend1.Name = "Legend1";
            chartCategorias.Legends.Add(legend1);
            chartCategorias.Location = new Point(20, 50);
            chartCategorias.Name = "chartCategorias";
            series1.ChartArea = "ChartArea1";
            series1.Legend = "Legend1";
            series1.Name = "Series1";
            chartCategorias.Series.Add(series1);
            chartCategorias.Size = new Size(500, 180);
            chartCategorias.TabIndex = 1;
            chartCategorias.Text = "Categorias";
            // 
            // dgvResumoCategorias
            // 
            dgvResumoCategorias.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvResumoCategorias.Location = new Point(540, 55);
            dgvResumoCategorias.Name = "dgvResumoCategorias";
            dgvResumoCategorias.RowHeadersWidth = 51;
            dgvResumoCategorias.Size = new Size(270, 170);
            dgvResumoCategorias.TabIndex = 0;
            // 
            // pnlAcoesRapidas
            // 
            pnlAcoesRapidas.BackColor = Color.White;
            pnlAcoesRapidas.Controls.Add(pictureBox3);
            pnlAcoesRapidas.Controls.Add(label2);
            pnlAcoesRapidas.Controls.Add(btnAcaoRelatorio);
            pnlAcoesRapidas.Controls.Add(btnAcaoLancamentos);
            pnlAcoesRapidas.Controls.Add(btnAcaoImportar);
            pnlAcoesRapidas.Controls.Add(btnAcaoNovo);
            pnlAcoesRapidas.Location = new Point(900, 164);
            pnlAcoesRapidas.Name = "pnlAcoesRapidas";
            pnlAcoesRapidas.Size = new Size(420, 250);
            pnlAcoesRapidas.TabIndex = 7;
            // 
            // pictureBox3
            // 
            pictureBox3.Image = Properties.Resources.acoes_orange;
            pictureBox3.Location = new Point(8, 12);
            pictureBox3.Name = "pictureBox3";
            pictureBox3.Size = new Size(59, 34);
            pictureBox3.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox3.TabIndex = 9;
            pictureBox3.TabStop = false;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Segoe UI", 10.2F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label2.Location = new Point(73, 18);
            label2.Name = "label2";
            label2.Size = new Size(124, 23);
            label2.TabIndex = 8;
            label2.Text = "Ações Rápidas";
            // 
            // btnAcaoRelatorio
            // 
            btnAcaoRelatorio.FlatStyle = FlatStyle.Popup;
            btnAcaoRelatorio.Location = new Point(220, 145);
            btnAcaoRelatorio.Name = "btnAcaoRelatorio";
            btnAcaoRelatorio.Size = new Size(170, 70);
            btnAcaoRelatorio.TabIndex = 3;
            btnAcaoRelatorio.Text = "Relatório";
            btnAcaoRelatorio.UseVisualStyleBackColor = true;
            // 
            // btnAcaoLancamentos
            // 
            btnAcaoLancamentos.FlatStyle = FlatStyle.Popup;
            btnAcaoLancamentos.Location = new Point(220, 55);
            btnAcaoLancamentos.Name = "btnAcaoLancamentos";
            btnAcaoLancamentos.Size = new Size(170, 70);
            btnAcaoLancamentos.TabIndex = 2;
            btnAcaoLancamentos.Text = "Lançamentos";
            btnAcaoLancamentos.UseVisualStyleBackColor = true;
            btnAcaoLancamentos.Click += btnAcaoLancamentos_Click;
            // 
            // btnAcaoImportar
            // 
            btnAcaoImportar.FlatStyle = FlatStyle.Popup;
            btnAcaoImportar.Location = new Point(20, 145);
            btnAcaoImportar.Name = "btnAcaoImportar";
            btnAcaoImportar.Size = new Size(170, 70);
            btnAcaoImportar.TabIndex = 1;
            btnAcaoImportar.Text = "Importar";
            btnAcaoImportar.UseVisualStyleBackColor = true;
            // 
            // btnAcaoNovo
            // 
            btnAcaoNovo.BackgroundImageLayout = ImageLayout.None;
            btnAcaoNovo.FlatStyle = FlatStyle.Popup;
            btnAcaoNovo.ImageAlign = ContentAlignment.TopCenter;
            btnAcaoNovo.Location = new Point(20, 55);
            btnAcaoNovo.Name = "btnAcaoNovo";
            btnAcaoNovo.Size = new Size(170, 70);
            btnAcaoNovo.TabIndex = 0;
            btnAcaoNovo.Text = "Novo";
            btnAcaoNovo.UseVisualStyleBackColor = true;
            btnAcaoNovo.Click += btnAcaoNovo_Click;
            // 
            // pnlTabela
            // 
            pnlTabela.BackColor = Color.White;
            pnlTabela.Controls.Add(pictureBox1);
            pnlTabela.Controls.Add(label1);
            pnlTabela.Controls.Add(dgvProximosVencimentos);
            pnlTabela.Controls.Add(btnVerTodos);
            pnlTabela.Location = new Point(30, 164);
            pnlTabela.Name = "pnlTabela";
            pnlTabela.Size = new Size(840, 250);
            pnlTabela.TabIndex = 5;
            // 
            // pictureBox1
            // 
            pictureBox1.Image = Properties.Resources.lancamentos_dark;
            pictureBox1.Location = new Point(13, 12);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(59, 34);
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.TabIndex = 8;
            pictureBox1.TabStop = false;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 10.2F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label1.Location = new Point(78, 18);
            label1.Name = "label1";
            label1.Size = new Size(190, 23);
            label1.TabIndex = 7;
            label1.Text = "Próximos Vencimentos";
            // 
            // dgvProximosVencimentos
            // 
            dgvProximosVencimentos.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvProximosVencimentos.Location = new Point(18, 52);
            dgvProximosVencimentos.Name = "dgvProximosVencimentos";
            dgvProximosVencimentos.RowHeadersWidth = 51;
            dgvProximosVencimentos.Size = new Size(800, 180);
            dgvProximosVencimentos.TabIndex = 0;
            // 
            // btnVerTodos
            // 
            btnVerTodos.FlatStyle = FlatStyle.Popup;
            btnVerTodos.Location = new Point(718, 12);
            btnVerTodos.Name = "btnVerTodos";
            btnVerTodos.Size = new Size(100, 34);
            btnVerTodos.TabIndex = 6;
            btnVerTodos.Text = "Ver todos";
            btnVerTodos.UseVisualStyleBackColor = true;
            btnVerTodos.Click += btnVerTodos_Click_1;
            // 
            // pnlCardSemana
            // 
            pnlCardSemana.BackColor = Color.FromArgb(251, 140, 0);
            pnlCardSemana.Controls.Add(pictureBox4);
            pnlCardSemana.Controls.Add(label6);
            pnlCardSemana.Controls.Add(lblSemanaQtd);
            pnlCardSemana.Controls.Add(lblSemanaValor);
            pnlCardSemana.Location = new Point(960, 38);
            pnlCardSemana.Name = "pnlCardSemana";
            pnlCardSemana.Size = new Size(280, 100);
            pnlCardSemana.TabIndex = 4;
            // 
            // pictureBox4
            // 
            pictureBox4.Image = Properties.Resources.calendario_dark;
            pictureBox4.Location = new Point(4, 8);
            pictureBox4.Name = "pictureBox4";
            pictureBox4.Size = new Size(59, 71);
            pictureBox4.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox4.TabIndex = 9;
            pictureBox4.TabStop = false;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label6.ForeColor = Color.White;
            label6.Location = new Point(69, 8);
            label6.Name = "label6";
            label6.Size = new Size(208, 20);
            label6.TabIndex = 4;
            label6.Text = "Vencem nos próximos 7 dias";
            // 
            // lblSemanaQtd
            // 
            lblSemanaQtd.AutoSize = true;
            lblSemanaQtd.ForeColor = Color.White;
            lblSemanaQtd.Location = new Point(69, 56);
            lblSemanaQtd.Name = "lblSemanaQtd";
            lblSemanaQtd.Size = new Size(55, 23);
            lblSemanaQtd.TabIndex = 1;
            lblSemanaQtd.Text = "label5";
            // 
            // lblSemanaValor
            // 
            lblSemanaValor.AutoSize = true;
            lblSemanaValor.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblSemanaValor.ForeColor = Color.White;
            lblSemanaValor.Location = new Point(69, 28);
            lblSemanaValor.Name = "lblSemanaValor";
            lblSemanaValor.Size = new Size(70, 28);
            lblSemanaValor.TabIndex = 0;
            lblSemanaValor.Text = "label4";
            // 
            // pnlCardPago
            // 
            pnlCardPago.BackColor = Color.FromArgb(57, 184, 116);
            pnlCardPago.Controls.Add(pictureBox7);
            pnlCardPago.Controls.Add(label5);
            pnlCardPago.Controls.Add(lblTotalEntradaQtd);
            pnlCardPago.Controls.Add(lblTotalEntradaValor);
            pnlCardPago.Location = new Point(30, 38);
            pnlCardPago.Name = "pnlCardPago";
            pnlCardPago.Size = new Size(280, 100);
            pnlCardPago.TabIndex = 3;
            // 
            // pictureBox7
            // 
            pictureBox7.Image = Properties.Resources.entrada_dark;
            pictureBox7.Location = new Point(18, 8);
            pictureBox7.Name = "pictureBox7";
            pictureBox7.Size = new Size(59, 71);
            pictureBox7.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox7.TabIndex = 12;
            pictureBox7.TabStop = false;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label5.ForeColor = Color.White;
            label5.Location = new Point(96, 8);
            label5.Name = "label5";
            label5.Size = new Size(125, 20);
            label5.TabIndex = 3;
            label5.Text = "Entradas do Mês";
            // 
            // lblTotalEntradaQtd
            // 
            lblTotalEntradaQtd.AutoSize = true;
            lblTotalEntradaQtd.ForeColor = Color.White;
            lblTotalEntradaQtd.Location = new Point(96, 56);
            lblTotalEntradaQtd.Name = "lblTotalEntradaQtd";
            lblTotalEntradaQtd.Size = new Size(55, 23);
            lblTotalEntradaQtd.TabIndex = 1;
            lblTotalEntradaQtd.Text = "label5";
            // 
            // lblTotalEntradaValor
            // 
            lblTotalEntradaValor.AutoSize = true;
            lblTotalEntradaValor.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblTotalEntradaValor.ForeColor = Color.White;
            lblTotalEntradaValor.Location = new Point(96, 28);
            lblTotalEntradaValor.Name = "lblTotalEntradaValor";
            lblTotalEntradaValor.Size = new Size(70, 28);
            lblTotalEntradaValor.TabIndex = 0;
            lblTotalEntradaValor.Text = "label4";
            // 
            // pnlCardMes
            // 
            pnlCardMes.BackColor = Color.FromArgb(59, 130, 246);
            pnlCardMes.Controls.Add(pictureBox5);
            pnlCardMes.Controls.Add(label7);
            pnlCardMes.Controls.Add(lblSaldoMesQtd);
            pnlCardMes.Controls.Add(lblSaldoMesValor);
            pnlCardMes.Location = new Point(650, 38);
            pnlCardMes.Name = "pnlCardMes";
            pnlCardMes.Size = new Size(280, 100);
            pnlCardMes.TabIndex = 2;
            // 
            // pictureBox5
            // 
            pictureBox5.Image = Properties.Resources.saldo_dark;
            pictureBox5.Location = new Point(20, 8);
            pictureBox5.Name = "pictureBox5";
            pictureBox5.Size = new Size(59, 71);
            pictureBox5.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox5.TabIndex = 10;
            pictureBox5.TabStop = false;
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label7.ForeColor = Color.White;
            label7.Location = new Point(102, 8);
            label7.Name = "label7";
            label7.Size = new Size(102, 20);
            label7.TabIndex = 5;
            label7.Text = "Saldo do Mês";
            // 
            // lblSaldoMesQtd
            // 
            lblSaldoMesQtd.AutoSize = true;
            lblSaldoMesQtd.ForeColor = Color.White;
            lblSaldoMesQtd.Location = new Point(102, 56);
            lblSaldoMesQtd.Name = "lblSaldoMesQtd";
            lblSaldoMesQtd.Size = new Size(55, 23);
            lblSaldoMesQtd.TabIndex = 1;
            lblSaldoMesQtd.Text = "label5";
            // 
            // lblSaldoMesValor
            // 
            lblSaldoMesValor.AutoSize = true;
            lblSaldoMesValor.BackColor = Color.Transparent;
            lblSaldoMesValor.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblSaldoMesValor.ForeColor = Color.White;
            lblSaldoMesValor.Location = new Point(102, 28);
            lblSaldoMesValor.Name = "lblSaldoMesValor";
            lblSaldoMesValor.Size = new Size(70, 28);
            lblSaldoMesValor.TabIndex = 0;
            lblSaldoMesValor.Text = "label4";
            // 
            // pnlCardPendente
            // 
            pnlCardPendente.BackColor = Color.FromArgb(242, 75, 75);
            pnlCardPendente.Controls.Add(pictureBox6);
            pnlCardPendente.Controls.Add(label4);
            pnlCardPendente.Controls.Add(lblTotalSaidaQtd);
            pnlCardPendente.Controls.Add(lblTotalSaidaValor);
            pnlCardPendente.Location = new Point(340, 38);
            pnlCardPendente.Name = "pnlCardPendente";
            pnlCardPendente.Size = new Size(280, 100);
            pnlCardPendente.TabIndex = 1;
            // 
            // pictureBox6
            // 
            pictureBox6.Image = Properties.Resources.saida_dark;
            pictureBox6.Location = new Point(26, 8);
            pictureBox6.Name = "pictureBox6";
            pictureBox6.Size = new Size(59, 71);
            pictureBox6.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox6.TabIndex = 11;
            pictureBox6.TabStop = false;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label4.ForeColor = Color.White;
            label4.Location = new Point(120, 8);
            label4.Name = "label4";
            label4.Size = new Size(108, 20);
            label4.TabIndex = 2;
            label4.Text = "Saídas do Mês";
            // 
            // lblTotalSaidaQtd
            // 
            lblTotalSaidaQtd.AutoSize = true;
            lblTotalSaidaQtd.ForeColor = Color.White;
            lblTotalSaidaQtd.Location = new Point(120, 56);
            lblTotalSaidaQtd.Name = "lblTotalSaidaQtd";
            lblTotalSaidaQtd.Size = new Size(55, 23);
            lblTotalSaidaQtd.TabIndex = 1;
            lblTotalSaidaQtd.Text = "label5";
            // 
            // lblTotalSaidaValor
            // 
            lblTotalSaidaValor.AutoSize = true;
            lblTotalSaidaValor.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblTotalSaidaValor.ForeColor = Color.White;
            lblTotalSaidaValor.Location = new Point(120, 28);
            lblTotalSaidaValor.Name = "lblTotalSaidaValor";
            lblTotalSaidaValor.Size = new Size(70, 28);
            lblTotalSaidaValor.TabIndex = 0;
            lblTotalSaidaValor.Text = "label4";
            // 
            // lblTituloDashboard
            // 
            lblTituloDashboard.AutoSize = true;
            lblTituloDashboard.BackColor = Color.Transparent;
            lblTituloDashboard.Font = new Font("Segoe UI", 10.2F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblTituloDashboard.Location = new Point(246, 16);
            lblTituloDashboard.Name = "lblTituloDashboard";
            lblTituloDashboard.Size = new Size(97, 23);
            lblTituloDashboard.TabIndex = 3;
            lblTituloDashboard.Text = "Dashboard";
            // 
            // cmbCompetencia
            // 
            cmbCompetencia.FormattingEnabled = true;
            cmbCompetencia.Location = new Point(720, 18);
            cmbCompetencia.Name = "cmbCompetencia";
            cmbCompetencia.Size = new Size(250, 31);
            cmbCompetencia.TabIndex = 1;
            cmbCompetencia.Format += cmbCompetencia_Format;
            // 
            // lblUsuario
            // 
            lblUsuario.AutoSize = true;
            lblUsuario.Font = new Font("Segoe UI", 10.2F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblUsuario.ForeColor = SystemColors.ActiveCaptionText;
            lblUsuario.Location = new Point(1030, 16);
            lblUsuario.Name = "lblUsuario";
            lblUsuario.Size = new Size(111, 23);
            lblUsuario.TabIndex = 2;
            lblUsuario.Text = "Olá, Gustavo";
            // 
            // lblBoasVindas
            // 
            lblBoasVindas.AutoSize = true;
            lblBoasVindas.Font = new Font("Segoe UI", 7.8F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblBoasVindas.Location = new Point(1032, 40);
            lblBoasVindas.Name = "lblBoasVindas";
            lblBoasVindas.Size = new Size(226, 17);
            lblBoasVindas.TabIndex = 4;
            lblBoasVindas.Text = "Bem-vindo ao seu controle financeiro";
            // 
            // pictureBox9
            // 
            pictureBox9.Image = Properties.Resources.usuario_dark;
            pictureBox9.Location = new Point(1270, 4);
            pictureBox9.Name = "pictureBox9";
            pictureBox9.Size = new Size(59, 53);
            pictureBox9.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox9.TabIndex = 10;
            pictureBox9.TabStop = false;
            // 
            // panel2
            // 
            panel2.Dock = DockStyle.Bottom;
            panel2.Location = new Point(0, 866);
            panel2.Name = "panel2";
            panel2.Size = new Size(1555, 57);
            panel2.TabIndex = 11;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(9F, 23F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(245, 246, 250);
            ClientSize = new Size(1555, 923);
            Controls.Add(panel2);
            Controls.Add(pictureBox9);
            Controls.Add(lblSubtitulo);
            Controls.Add(cmbCompetencia);
            Controls.Add(lblUsuario);
            Controls.Add(lblBoasVindas);
            Controls.Add(lblTituloDashboard);
            Controls.Add(panel1);
            Controls.Add(pnlSidebar);
            Font = new Font("Segoe UI", 10.2F, FontStyle.Regular, GraphicsUnit.Point, 0);
            MinimumSize = new Size(1573, 970);
            Name = "Form1";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Controle Financeiro Pessoal - Principal";
            WindowState = FormWindowState.Maximized;
            Load += Form1_Load;
            pnlSidebar.ResumeLayout(false);
            flowLayoutPanel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)pictureBox8).EndInit();
            panel1.ResumeLayout(false);
            pnlResumo.ResumeLayout(false);
            pnlResumo.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox10).EndInit();
            pnlGrafico.ResumeLayout(false);
            pnlGrafico.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox2).EndInit();
            ((System.ComponentModel.ISupportInitialize)chartCategorias).EndInit();
            ((System.ComponentModel.ISupportInitialize)dgvResumoCategorias).EndInit();
            pnlAcoesRapidas.ResumeLayout(false);
            pnlAcoesRapidas.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox3).EndInit();
            pnlTabela.ResumeLayout(false);
            pnlTabela.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ((System.ComponentModel.ISupportInitialize)dgvProximosVencimentos).EndInit();
            pnlCardSemana.ResumeLayout(false);
            pnlCardSemana.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox4).EndInit();
            pnlCardPago.ResumeLayout(false);
            pnlCardPago.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox7).EndInit();
            pnlCardMes.ResumeLayout(false);
            pnlCardMes.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox5).EndInit();
            pnlCardPendente.ResumeLayout(false);
            pnlCardPendente.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox6).EndInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox9).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Panel pnlSidebar;
        private Panel panel1;
        private Label lblUsuario;
        private ComboBox cmbCompetencia;
        private Label lblSubtitulo;
        private Label lblTituloDashboard;
        private Panel pnlCardSemana;
        private Panel pnlCardPago;
        private Panel pnlCardMes;
        private Panel pnlCardPendente;
        private Label lblBoasVindas;
        private Panel pnlTabela;
        private Button btnVerTodos;
        private DataGridView dgvProximosVencimentos;
        private Panel pnlAcoesRapidas;
        private Button btnAcaoRelatorio;
        private Button btnAcaoLancamentos;
        private Button btnAcaoImportar;
        private Button btnAcaoNovo;
        private Panel pnlGrafico;
        private DataGridView dgvResumoCategorias;
        private Panel pnlResumo;
        private Label lblResumoVencemSemana;
        private Label lblResumoVencemHoje;
        private Label lblResumoAtrasados;
        private Label lblResumoTotalPendente;
        private Label lblResumoTotalPago;
        private Label lblResumoTotalLancamentos;
        private System.Windows.Forms.DataVisualization.Charting.Chart chartCategorias;
        private Label label1;
        private Label label3;
        private Label lblGastoPorCategoria;
        private Label label2;
        private Label lblTotalSaidaValor;
        private Label lblTotalSaidaQtd;
        private Label lblTotalEntradaQtd;
        private Label lblTotalEntradaValor;
        private Label lblSemanaQtd;
        private Label lblSemanaValor;
        private Label lblSaldoMesQtd;
        private Label lblSaldoMesValor;
        private Label label6;
        private Label label5;
        private Label label7;
        private Label label4;
        private FlowLayoutPanel flowLayoutPanel1;
        private Button btnLancamentos;
        private Button btnCategorias;
        private Button btnContas;
        private Button btnSair;
        private PictureBox pictureBox2;
        private PictureBox pictureBox3;
        private PictureBox pictureBox1;
        private PictureBox pictureBox4;
        private PictureBox pictureBox5;
        private PictureBox pictureBox6;
        private PictureBox pictureBox7;
        private PictureBox pictureBox8;
        private PictureBox pictureBox9;
        private PictureBox pictureBox10;
        private Panel panel2;
    }
}
