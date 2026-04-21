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
    public partial class FrmAuth : Form
    {
        private readonly AuthService _authService = new();
        private System.Windows.Forms.Timer _timerCodigo;
        private int _segundosRestantes = 30;
        public FrmAuth()
        {
            InitializeComponent();
            InicializarTimer();
            VerificarPrimeiroAcesso();
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

                // Reposiciona painel de dígitos abaixo do QR Code
                pnlDigitos.Location = new Point(24, 260);
                pnlErro.Location = new Point(24, 330);
                btnEntrar.Location = new Point(24, 378);
                pnlTimer.Location = new Point(24, 430);

                lblAviso.Text =
                    "Primeiro acesso! Escaneie o QR Code com o\n" +
                    "Google Authenticator ou Microsoft Authenticator.";

                Height = 580;
            }
            else
            {
                picQrCode.Visible = false;

                // Sobe os controles quando não há QR Code
                pnlDigitos.Location = new Point(24, 100);
                pnlErro.Location = new Point(24, 170);
                btnEntrar.Location = new Point(24, 218);
                pnlTimer.Location = new Point(24, 272);

                lblAviso.Text = "Digite o código de 6 dígitos\ndo seu aplicativo autenticador.";

                Height = 400;
            }

            txtDigitos[0].Focus();
        }

        private void ExibirErro(string mensagem)
        {
            lblErro.Text = mensagem;
            pnlErro.Visible = true;
        }

        private void OcultarErro()
        {
            pnlErro.Visible = false;
        }
        private void LimparDigitos()
        {
            foreach (var txt in txtDigitos)
                txt.Text = string.Empty;

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

        private Bitmap GerarQrCode(string uri)
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
    }
}
