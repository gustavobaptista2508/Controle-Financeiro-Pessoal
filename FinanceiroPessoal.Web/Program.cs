// FinanceiroPessoal.Web/Program.cs
using FinanceiroPessoal.Core.Data;
using FinanceiroPessoal.Core.Repositories;
using FinanceiroPessoal.Core.Services;
using Microsoft.EntityFrameworkCore;
using ApexCharts;
using System.Security.Cryptography;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddApexCharts();

// DbContext
var connectionString = builder.Configuration.GetConnectionString("MySqlConnection");
var encryptedConnectionString = builder.Configuration["ConnectionStrings:MySqlConnectionEncrypted"];
var encryptionKey = builder.Configuration["ConnectionStrings:MySqlConnectionEncryptionKey"];

if (string.IsNullOrWhiteSpace(connectionString) && !string.IsNullOrWhiteSpace(encryptedConnectionString))
{
    if (string.IsNullOrWhiteSpace(encryptionKey))
    {
        throw new InvalidOperationException(
            "A chave de criptografia 'ConnectionStrings:MySqlConnectionEncryptionKey' não foi configurada.");
    }

    connectionString = DecryptConnectionString(encryptedConnectionString, encryptionKey);
}

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Connection string 'ConnectionStrings:MySqlConnection' não configurada. " +
        "Defina no appsettings, Secret Manager ou variável de ambiente.");
}

builder.Services.AddDbContext<MySqlDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)),
    ServiceLifetime.Scoped);

// Repositórios
builder.Services.AddScoped<ILancamentoRepository, MySqlLancamentoRepository>();
builder.Services.AddScoped<ICadastroAuxiliarRepository, MySqlCadastroAuxiliarRepository>();

// Serviços
builder.Services.AddScoped<LancamentoService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<CadastroAuxiliarService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<FinanceiroPessoal.Web.Services.WebAuthSessionService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<FinanceiroPessoal.Web.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();

static string DecryptConnectionString(string encryptedValue, string key)
{
    var parts = encryptedValue.Split(':', 2);
    if (parts.Length != 2)
    {
        throw new InvalidOperationException("Formato inválido para MySqlConnectionEncrypted. Use 'IV_BASE64:CIPHER_BASE64'.");
    }

    var iv = Convert.FromBase64String(parts[0]);
    var cipherBytes = Convert.FromBase64String(parts[1]);
    var keyBytes = SHA256.HashData(Encoding.UTF8.GetBytes(key));

    using var aes = Aes.Create();
    aes.Key = keyBytes;
    aes.IV = iv;
    aes.Mode = CipherMode.CBC;
    aes.Padding = PaddingMode.PKCS7;

    using var decryptor = aes.CreateDecryptor();
    var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
    return Encoding.UTF8.GetString(plainBytes);
}
