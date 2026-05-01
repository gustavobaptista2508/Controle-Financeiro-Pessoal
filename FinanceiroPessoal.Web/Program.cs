// FinanceiroPessoal.Web/Program.cs
using FinanceiroPessoal.Core.Data;
using FinanceiroPessoal.Core.Repositories;
using FinanceiroPessoal.Core.Services;
using Microsoft.EntityFrameworkCore;
using ApexCharts;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddApexCharts();

// DbContext
var connectionString = builder.Configuration.GetConnectionString("MySqlConnection");
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
