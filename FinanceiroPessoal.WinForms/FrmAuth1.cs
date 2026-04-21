using FinanceiroPessoal.WinForms.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace FinanceiroPessoal.WinForms
{
    public partial class FrmAuth1 : Form
    {
        private readonly AuthService _authService = new();
        private System.Windows.Forms.Timer _timerCodigo;
        private int _segundosRestantes = 30;
        private TextBox[] txtDigitos = new TextBox[6];
        public FrmAuth1()
        {
            InitializeComponent();
            ConfigurarPnl();
            InicializarTimer();
            VerificarPrimeiroAcesso();
            //TextBox[] txtDigitos;
        }

        private void ConfigurarPnl()
        {
            try
            {
                pnlDigitos.Controls.Clear();

                for (int i = 0; i < 6; i++)
                {
                    txtDigitos[i] = new TextBox
                    {
                        Size = new Size(44, 52),
                        Location = new Point(i * 52, 0),
                        MaxLength = 1,
                        TextAlign = HorizontalAlignment.Center,
                        Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold),
                        BackColor = Color.FromArgb(245, 248, 255),
                        ForeColor = Color.FromArgb(12, 68, 124),
                        BorderStyle = BorderStyle.FixedSingle,
                        Tag = i,
                    };
                    int idx = i;
                    txtDigitos[i].KeyPress += (s, e) =>
                    {
                        if (!char.IsDigit(e.KeyChar) && e.KeyChar != (char)Keys.Back)
                            e.Handled = true;
                    };
                    txtDigitos[i].TextChanged += (s, e) =>
                    {
                        if (txtDigitos[idx].Text.Length == 1 && idx < 5)
                            txtDigitos[idx + 1].Focus();
                        AtualizarEstiloDigitos();
                    };
                    txtDigitos[i].KeyDown += (s, e) =>
                    {
                        if (e.KeyCode == Keys.Back && txtDigitos[idx].Text.Length == 0 && idx > 0)
                            txtDigitos[idx - 1].Focus();
                    };
                    pnlDigitos.Controls.Add(txtDigitos[i]);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao configurar painel de dígitos: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void VerificarPrimeiroAcesso()
        {
            if (!_authService.ChaveExiste())
            {
                // Primeiro acesso — gera chave e exibe QR Code
                var base32 = _authService.GerarNovaChave();
                var uri = _authService.GerarUriQrCode(base32);

                picQrCode.Image = GerarQrCode(uri);
                picQrCode.Visible = true;

                lblAviso.Text =
                    "Primeiro acesso! Escaneie o QR Code com o\n" +
                    "Google Authenticator ou Microsoft Authenticator.";
            }
            else
            {
                picQrCode.Visible = false;
            }

            txtDigitos[0].Focus();
        }

        private Image GerarQrCode(string uri)
        {
            using var qrGenerator = new QRCoder.QRCodeGenerator();
            var qrData = qrGenerator.CreateQrCode(uri, QRCoder.QRCodeGenerator.ECCLevel.Q);
            var qrCode = new QRCoder.QRCode(qrData);
            return qrCode.GetGraphic(5);
        }

        private void InicializarTimer()
        {
            _timerCodigo = new System.Windows.Forms.Timer { Interval = 1000 };
            _timerCodigo.Tick += (s, e) =>
            {
                // Calcula segundos restantes no ciclo TOTP de 30s
                _segundosRestantes = 30 - (int)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() % 30);
                lblTimer.Text = $"código expira em {_segundosRestantes}s";

                // Alerta visual quando está prestes a expirar
                lblTimer.ForeColor = _segundosRestantes <= 5
                    ? Color.FromArgb(163, 45, 45)
                    : Color.FromArgb(156, 163, 175);

                // Limpa automaticamente os dígitos ao renovar o código
                if (_segundosRestantes == 30)
                {
                    LimparDigitos();
                    OcultarErro();
                }
            };
            _timerCodigo.Start();
        }

        private void OcultarErro()
        {
            lblErro.Visible = false;
        }

        private void LimparDigitos()
        {
            try
            {
                foreach (var txt in txtDigitos)
                    if (txt != null)
                        txt.Text = string.Empty;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao limpar dígitos: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            AtualizarEstiloDigitos();
        }

        private void AtualizarEstiloDigitos()
        {
            foreach (var txt in txtDigitos)
            {
                txt.BackColor = txt.Text.Length == 1
                    ? Color.FromArgb(230, 241, 251)   // preenchido — azul claro
                    : Color.FromArgb(245, 248, 255);  // vazio — cinza claro
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _timerCodigo?.Stop();
            _timerCodigo?.Dispose();
            base.OnFormClosing(e);
        }

        private void btnEntrar_Click(object sender, EventArgs e)
        {
            var codigo = string.Concat(txtDigitos.Select(t => t.Text));

            if (codigo.Length != 6)
            {
                ExibirErro("Preencha todos os 6 dígitos.");
                return;
            }

            if (_authService.VerificarCodigo(codigo))
            {
                _timerCodigo.Stop();
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                ExibirErro("Código inválido ou expirado. Tente novamente.");
                LimparDigitos();
                txtDigitos[0].Focus();
            }
        }

        private void ExibirErro(string mensagem)
        {
            lblErro.Text = mensagem;
            lblErro.Visible = true;
        }

        private void FrmAuth1_Load(object sender, EventArgs e)
        {
          
        }
    }
}
