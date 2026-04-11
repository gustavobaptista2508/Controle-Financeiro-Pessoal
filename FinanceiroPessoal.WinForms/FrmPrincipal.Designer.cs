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
            lblSubtitulo = new Label();
            panel1 = new Panel();
            pnlResumo = new Panel();
            label3 = new Label();
            lblResumoVencemSemana = new Label();
            lblResumoVencemHoje = new Label();
            lblResumoAtrasados = new Label();
            lblResumoTotalPendente = new Label();
            lblResumoTotalPago = new Label();
            lblResumoTotalLancamentos = new Label();
            pnlGrafico = new Panel();
            lblGastoPorCategoria = new Label();
            chartCategorias = new System.Windows.Forms.DataVisualization.Charting.Chart();
            dgvResumoCategorias = new DataGridView();
            pnlAcoesRapidas = new Panel();
            label2 = new Label();
            btnAcaoRelatorio = new Button();
            btnAcaoLancamentos = new Button();
            btnAcaoImportar = new Button();
            btnAcaoNovo = new Button();
            pnlTabela = new Panel();
            label1 = new Label();
            dgvProximosVencimentos = new DataGridView();
            btnVerTodos = new Button();
            pnlCardSemana = new Panel();
            label6 = new Label();
            lblSemanaQtd = new Label();
            lblSemanaValor = new Label();
            pnlCardPago = new Panel();
            label5 = new Label();
            lblTotalPagoQtd = new Label();
            lblTotalPagoValor = new Label();
            pnlCardMes = new Panel();
            label7 = new Label();
            lblMesQtd = new Label();
            lblMesValor = new Label();
            pnlCardPendente = new Panel();
            label4 = new Label();
            lblTotalPendenteQtd = new Label();
            lblTotalPendenteValor = new Label();
            panel2 = new Panel();
            lblTituloDashboard = new Label();
            cmbCompetencia = new ComboBox();
            lblUsuario = new Label();
            lblBoasVindas = new Label();
            flowLayoutPanel1 = new FlowLayoutPanel();
            button1 = new Button();
            button2 = new Button();
            pnlSidebar.SuspendLayout();
            panel1.SuspendLayout();
            pnlResumo.SuspendLayout();
            pnlGrafico.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)chartCategorias).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dgvResumoCategorias).BeginInit();
            pnlAcoesRapidas.SuspendLayout();
            pnlTabela.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvProximosVencimentos).BeginInit();
            pnlCardSemana.SuspendLayout();
            pnlCardPago.SuspendLayout();
            pnlCardMes.SuspendLayout();
            pnlCardPendente.SuspendLayout();
            flowLayoutPanel1.SuspendLayout();
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
            panel1.Location = new Point(230, 60);
            panel1.Name = "panel1";
            panel1.Size = new Size(1370, 760);
            panel1.TabIndex = 1;
            // 
            // pnlResumo
            // 
            pnlResumo.BackColor = Color.White;
            pnlResumo.Controls.Add(label3);
            pnlResumo.Controls.Add(lblResumoVencemSemana);
            pnlResumo.Controls.Add(lblResumoVencemHoje);
            pnlResumo.Controls.Add(lblResumoAtrasados);
            pnlResumo.Controls.Add(lblResumoTotalPendente);
            pnlResumo.Controls.Add(lblResumoTotalPago);
            pnlResumo.Controls.Add(lblResumoTotalLancamentos);
            pnlResumo.Location = new Point(900, 500);
            pnlResumo.Name = "pnlResumo";
            pnlResumo.Size = new Size(420, 250);
            pnlResumo.TabIndex = 9;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new Font("Segoe UI", 10.2F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label3.Location = new Point(20, 15);
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
            pnlGrafico.Controls.Add(lblGastoPorCategoria);
            pnlGrafico.Controls.Add(chartCategorias);
            pnlGrafico.Controls.Add(dgvResumoCategorias);
            pnlGrafico.Location = new Point(30, 500);
            pnlGrafico.Name = "pnlGrafico";
            pnlGrafico.Size = new Size(840, 250);
            pnlGrafico.TabIndex = 8;
            // 
            // lblGastoPorCategoria
            // 
            lblGastoPorCategoria.AutoSize = true;
            lblGastoPorCategoria.Font = new Font("Segoe UI", 10.2F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblGastoPorCategoria.Location = new Point(18, 15);
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
            pnlAcoesRapidas.Controls.Add(label2);
            pnlAcoesRapidas.Controls.Add(btnAcaoRelatorio);
            pnlAcoesRapidas.Controls.Add(btnAcaoLancamentos);
            pnlAcoesRapidas.Controls.Add(btnAcaoImportar);
            pnlAcoesRapidas.Controls.Add(btnAcaoNovo);
            pnlAcoesRapidas.Location = new Point(900, 230);
            pnlAcoesRapidas.Name = "pnlAcoesRapidas";
            pnlAcoesRapidas.Size = new Size(420, 250);
            pnlAcoesRapidas.TabIndex = 7;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Segoe UI", 10.2F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label2.Location = new Point(20, 18);
            label2.Name = "label2";
            label2.Size = new Size(124, 23);
            label2.TabIndex = 8;
            label2.Text = "Ações Rápidas";
            // 
            // btnAcaoRelatorio
            // 
            btnAcaoRelatorio.Location = new Point(220, 145);
            btnAcaoRelatorio.Name = "btnAcaoRelatorio";
            btnAcaoRelatorio.Size = new Size(170, 70);
            btnAcaoRelatorio.TabIndex = 3;
            btnAcaoRelatorio.Text = "Relatório";
            btnAcaoRelatorio.UseVisualStyleBackColor = true;
            // 
            // btnAcaoLancamentos
            // 
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
            btnAcaoImportar.Location = new Point(20, 145);
            btnAcaoImportar.Name = "btnAcaoImportar";
            btnAcaoImportar.Size = new Size(170, 70);
            btnAcaoImportar.TabIndex = 1;
            btnAcaoImportar.Text = "Importar";
            btnAcaoImportar.UseVisualStyleBackColor = true;
            // 
            // btnAcaoNovo
            // 
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
            pnlTabela.Controls.Add(label1);
            pnlTabela.Controls.Add(dgvProximosVencimentos);
            pnlTabela.Controls.Add(btnVerTodos);
            pnlTabela.Location = new Point(30, 230);
            pnlTabela.Name = "pnlTabela";
            pnlTabela.Size = new Size(840, 250);
            pnlTabela.TabIndex = 5;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 10.2F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label1.Location = new Point(18, 18);
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
            pnlCardSemana.BackColor = Color.White;
            pnlCardSemana.Controls.Add(label6);
            pnlCardSemana.Controls.Add(lblSemanaQtd);
            pnlCardSemana.Controls.Add(lblSemanaValor);
            pnlCardSemana.Location = new Point(650, 90);
            pnlCardSemana.Name = "pnlCardSemana";
            pnlCardSemana.Size = new Size(280, 100);
            pnlCardSemana.TabIndex = 4;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label6.Location = new Point(45, 8);
            label6.Name = "label6";
            label6.Size = new Size(208, 20);
            label6.TabIndex = 4;
            label6.Text = "Vencem nos próximos 7 dias";
            // 
            // lblSemanaQtd
            // 
            lblSemanaQtd.AutoSize = true;
            lblSemanaQtd.Location = new Point(45, 56);
            lblSemanaQtd.Name = "lblSemanaQtd";
            lblSemanaQtd.Size = new Size(55, 23);
            lblSemanaQtd.TabIndex = 1;
            lblSemanaQtd.Text = "label5";
            // 
            // lblSemanaValor
            // 
            lblSemanaValor.AutoSize = true;
            lblSemanaValor.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblSemanaValor.ForeColor = Color.Gold;
            lblSemanaValor.Location = new Point(45, 28);
            lblSemanaValor.Name = "lblSemanaValor";
            lblSemanaValor.Size = new Size(70, 28);
            lblSemanaValor.TabIndex = 0;
            lblSemanaValor.Text = "label4";
            // 
            // pnlCardPago
            // 
            pnlCardPago.BackColor = Color.White;
            pnlCardPago.Controls.Add(label5);
            pnlCardPago.Controls.Add(lblTotalPagoQtd);
            pnlCardPago.Controls.Add(lblTotalPagoValor);
            pnlCardPago.Location = new Point(340, 90);
            pnlCardPago.Name = "pnlCardPago";
            pnlCardPago.Size = new Size(280, 100);
            pnlCardPago.TabIndex = 3;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label5.Location = new Point(30, 8);
            label5.Name = "label5";
            label5.Size = new Size(83, 20);
            label5.TabIndex = 3;
            label5.Text = "Total Pago";
            // 
            // lblTotalPagoQtd
            // 
            lblTotalPagoQtd.AutoSize = true;
            lblTotalPagoQtd.Location = new Point(30, 56);
            lblTotalPagoQtd.Name = "lblTotalPagoQtd";
            lblTotalPagoQtd.Size = new Size(55, 23);
            lblTotalPagoQtd.TabIndex = 1;
            lblTotalPagoQtd.Text = "label5";
            // 
            // lblTotalPagoValor
            // 
            lblTotalPagoValor.AutoSize = true;
            lblTotalPagoValor.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblTotalPagoValor.ForeColor = Color.ForestGreen;
            lblTotalPagoValor.Location = new Point(30, 28);
            lblTotalPagoValor.Name = "lblTotalPagoValor";
            lblTotalPagoValor.Size = new Size(70, 28);
            lblTotalPagoValor.TabIndex = 0;
            lblTotalPagoValor.Text = "label4";
            // 
            // pnlCardMes
            // 
            pnlCardMes.BackColor = Color.White;
            pnlCardMes.Controls.Add(label7);
            pnlCardMes.Controls.Add(lblMesQtd);
            pnlCardMes.Controls.Add(lblMesValor);
            pnlCardMes.Location = new Point(960, 90);
            pnlCardMes.Name = "pnlCardMes";
            pnlCardMes.Size = new Size(280, 100);
            pnlCardMes.TabIndex = 2;
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label7.Location = new Point(26, 8);
            label7.Name = "label7";
            label7.Size = new Size(99, 20);
            label7.TabIndex = 5;
            label7.Text = "Total do Mês";
            // 
            // lblMesQtd
            // 
            lblMesQtd.AutoSize = true;
            lblMesQtd.Location = new Point(26, 56);
            lblMesQtd.Name = "lblMesQtd";
            lblMesQtd.Size = new Size(55, 23);
            lblMesQtd.TabIndex = 1;
            lblMesQtd.Text = "label5";
            // 
            // lblMesValor
            // 
            lblMesValor.AutoSize = true;
            lblMesValor.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblMesValor.ForeColor = Color.SlateBlue;
            lblMesValor.Location = new Point(26, 28);
            lblMesValor.Name = "lblMesValor";
            lblMesValor.Size = new Size(70, 28);
            lblMesValor.TabIndex = 0;
            lblMesValor.Text = "label4";
            // 
            // pnlCardPendente
            // 
            pnlCardPendente.BackColor = Color.White;
            pnlCardPendente.Controls.Add(label4);
            pnlCardPendente.Controls.Add(lblTotalPendenteQtd);
            pnlCardPendente.Controls.Add(lblTotalPendenteValor);
            pnlCardPendente.Location = new Point(30, 90);
            pnlCardPendente.Name = "pnlCardPendente";
            pnlCardPendente.Size = new Size(280, 100);
            pnlCardPendente.TabIndex = 1;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label4.Location = new Point(55, 8);
            label4.Name = "label4";
            label4.Size = new Size(114, 20);
            label4.TabIndex = 2;
            label4.Text = "Total Pendente";
            // 
            // lblTotalPendenteQtd
            // 
            lblTotalPendenteQtd.AutoSize = true;
            lblTotalPendenteQtd.Location = new Point(55, 56);
            lblTotalPendenteQtd.Name = "lblTotalPendenteQtd";
            lblTotalPendenteQtd.Size = new Size(55, 23);
            lblTotalPendenteQtd.TabIndex = 1;
            lblTotalPendenteQtd.Text = "label5";
            // 
            // lblTotalPendenteValor
            // 
            lblTotalPendenteValor.AutoSize = true;
            lblTotalPendenteValor.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblTotalPendenteValor.ForeColor = Color.Red;
            lblTotalPendenteValor.Location = new Point(55, 28);
            lblTotalPendenteValor.Name = "lblTotalPendenteValor";
            lblTotalPendenteValor.Size = new Size(70, 28);
            lblTotalPendenteValor.TabIndex = 0;
            lblTotalPendenteValor.Text = "label4";
            // 
            // panel2
            // 
            panel2.BackColor = Color.FromArgb(245, 246, 250);
            panel2.Location = new Point(230, 820);
            panel2.Name = "panel2";
            panel2.Size = new Size(1370, 40);
            panel2.TabIndex = 2;
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
            // flowLayoutPanel1
            // 
            flowLayoutPanel1.Controls.Add(button1);
            flowLayoutPanel1.Controls.Add(button2);
            flowLayoutPanel1.Dock = DockStyle.Fill;
            flowLayoutPanel1.Location = new Point(0, 0);
            flowLayoutPanel1.Name = "flowLayoutPanel1";
            flowLayoutPanel1.Size = new Size(230, 860);
            flowLayoutPanel1.TabIndex = 0;
            // 
            // button1
            // 
            button1.Location = new Point(3, 3);
            button1.Name = "button1";
            button1.Size = new Size(221, 66);
            button1.TabIndex = 0;
            button1.Text = "button1";
            button1.UseVisualStyleBackColor = true;
            // 
            // button2
            // 
            button2.Location = new Point(3, 75);
            button2.Name = "button2";
            button2.Size = new Size(221, 66);
            button2.TabIndex = 1;
            button2.Text = "button2";
            button2.UseVisualStyleBackColor = true;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(9F, 23F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1555, 923);
            Controls.Add(lblSubtitulo);
            Controls.Add(cmbCompetencia);
            Controls.Add(lblUsuario);
            Controls.Add(lblBoasVindas);
            Controls.Add(lblTituloDashboard);
            Controls.Add(panel2);
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
            panel1.ResumeLayout(false);
            pnlResumo.ResumeLayout(false);
            pnlResumo.PerformLayout();
            pnlGrafico.ResumeLayout(false);
            pnlGrafico.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)chartCategorias).EndInit();
            ((System.ComponentModel.ISupportInitialize)dgvResumoCategorias).EndInit();
            pnlAcoesRapidas.ResumeLayout(false);
            pnlAcoesRapidas.PerformLayout();
            pnlTabela.ResumeLayout(false);
            pnlTabela.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dgvProximosVencimentos).EndInit();
            pnlCardSemana.ResumeLayout(false);
            pnlCardSemana.PerformLayout();
            pnlCardPago.ResumeLayout(false);
            pnlCardPago.PerformLayout();
            pnlCardMes.ResumeLayout(false);
            pnlCardMes.PerformLayout();
            pnlCardPendente.ResumeLayout(false);
            pnlCardPendente.PerformLayout();
            flowLayoutPanel1.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Panel pnlSidebar;
        private Panel panel1;
        private Label lblUsuario;
        private ComboBox cmbCompetencia;
        private Label lblSubtitulo;
        private Panel panel2;
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
        private Label lblTotalPendenteValor;
        private Label lblTotalPendenteQtd;
        private Label lblTotalPagoQtd;
        private Label lblTotalPagoValor;
        private Label lblSemanaQtd;
        private Label lblSemanaValor;
        private Label lblMesQtd;
        private Label lblMesValor;
        private Label label6;
        private Label label5;
        private Label label7;
        private Label label4;
        private FlowLayoutPanel flowLayoutPanel1;
        private Button button1;
        private Button button2;
    }
}
