// FinanceiroPessoal.Web/Program.cs
using ApexCharts;
using FinanceiroPessoal.Core.Data;
using FinanceiroPessoal.Core.Repositories;
using FinanceiroPessoal.Core.Services;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddApexCharts();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// DbContext
var connectionString = builder.Configuration.GetConnectionString("MySqlConnection");

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

app.UseForwardedHeaders();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<FinanceiroPessoal.Web.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();
