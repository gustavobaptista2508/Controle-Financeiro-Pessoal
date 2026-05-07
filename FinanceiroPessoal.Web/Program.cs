using ApexCharts;
using FinanceiroPessoal.Core.Repositories;
using FinanceiroPessoal.Core.Services;
using FinanceiroPessoal.Web;
using FinanceiroPessoal.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents();
builder.Services.AddHttpClient();
builder.Services.AddApexCharts();

builder.Services.AddScoped<ILancamentoRepository, InMemoryLancamentoRepository>();
builder.Services.AddScoped<ICadastroAuxiliarRepository, InMemoryCadastroAuxiliarRepository>();

builder.Services.AddScoped<LancamentoService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<CadastroAuxiliarService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<WebAuthSessionService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>();

app.Run();
