using ApexCharts;
using FinanceiroPessoal.Core.Repositories;
using FinanceiroPessoal.Core.Services;
using FinanceiroPessoal.Web;
using FinanceiroPessoal.Web.Services;
using Microsoft.AspNetCore.Localization;
using System.Globalization;
using MudBlazor.Services;
using Microsoft.AspNetCore.Components.Authorization;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpClient();
builder.Services.AddApexCharts();
builder.Services.AddMudServices();
builder.Services.AddAuthorizationCore();

// Usa os repositórios reais (MySQL) para manter dados persistidos e dashboard funcional
builder.Services.AddScoped<WebAuthSessionService>();
builder.Services.AddScoped<AuthenticationStateProvider, WebAuthStateProvider>();
builder.Services.AddScoped<ILancamentoRepository, InMemoryLancamentoRepository>();
builder.Services.AddScoped<ICadastroAuxiliarRepository, InMemoryCadastroAuxiliarRepository>();

builder.Services.AddScoped<LancamentoService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<CadastroAuxiliarService>();
builder.Services.AddScoped<AuthService>();

var app = builder.Build();

var culturaPtBr = new CultureInfo("pt-BR");
CultureInfo.DefaultThreadCurrentCulture = culturaPtBr;
CultureInfo.DefaultThreadCurrentUICulture = culturaPtBr;

app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(culturaPtBr),
    SupportedCultures = [culturaPtBr],
    SupportedUICultures = [culturaPtBr]
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
