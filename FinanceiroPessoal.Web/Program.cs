using ApexCharts;
using FinanceiroPessoal.Core.Repositories;
using FinanceiroPessoal.Core.Services;
using FinanceiroPessoal.Web;
using FinanceiroPessoal.Web.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddApexCharts();

// TODO (Cloudflare/WASM): substituir os repositórios locais por clientes HTTP para FinanceiroPessoal.Api.
builder.Services.AddScoped<ILancamentoRepository, InMemoryLancamentoRepository>();
builder.Services.AddScoped<ICadastroAuxiliarRepository, InMemoryCadastroAuxiliarRepository>();

builder.Services.AddScoped<LancamentoService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<CadastroAuxiliarService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<WebAuthSessionService>();

await builder.Build().RunAsync();
