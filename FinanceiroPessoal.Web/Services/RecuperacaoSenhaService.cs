using System.Security.Cryptography;
using FinanceiroPessoal.Core.Data;
using FinanceiroPessoal.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace FinanceiroPessoal.Web.Services;

public class RecuperacaoSenhaService
{
    private readonly FinanceiroDbContext _db;
    private readonly IPasswordHasherService _passwordHasher;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;

    public RecuperacaoSenhaService(
        FinanceiroDbContext db,
        IPasswordHasherService passwordHasher,
        IEmailService emailService,
        IConfiguration configuration)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _emailService = emailService;
        _configuration = configuration;
    }

    public async Task SolicitarRecuperacaoAsync(string email)
    {
        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Email == email);
        if (usuario is null)
            return;

        usuario.TokenRecuperacao = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        usuario.TokenExpiracao = DateTime.UtcNow.AddHours(1);

        await _db.SaveChangesAsync();

        var baseUrl = _configuration["App:BaseUrl"]?.TrimEnd('/') ?? "http://localhost:5095";
        var link = $"{baseUrl}/redefinir-senha?token={Uri.EscapeDataString(usuario.TokenRecuperacao)}";
        await _emailService.EnviarRecuperacaoSenhaAsync(usuario, link);
    }

    public async Task<bool> RedefinirSenhaAsync(string token, string novaSenha)
    {
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(novaSenha) || novaSenha.Length < 6)
            return false;

        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u =>
            u.TokenRecuperacao == token &&
            u.TokenExpiracao != null &&
            u.TokenExpiracao > DateTime.UtcNow);

        if (usuario is null)
            return false;

        usuario.SenhaHash = _passwordHasher.HashPassword(novaSenha);
        usuario.TokenRecuperacao = null;
        usuario.TokenExpiracao = null;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> TokenValidoAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;

        return await _db.Usuarios.AnyAsync(u =>
            u.TokenRecuperacao == token &&
            u.TokenExpiracao != null &&
            u.TokenExpiracao > DateTime.UtcNow);
    }
}
