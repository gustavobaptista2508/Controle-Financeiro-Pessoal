using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace FinanceiroPessoal.Web.Services;

public sealed class WebAuthStateProvider : AuthenticationStateProvider
{
    private readonly WebAuthSessionService _session;

    public WebAuthStateProvider(WebAuthSessionService session)
    {
        _session = session;
        _session.AuthenticationStateChanged += OnAuthenticationStateChanged;
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        ClaimsPrincipal user;
        if (_session.IsAuthenticated && !string.IsNullOrWhiteSpace(_session.CurrentUserEmail))
        {
            var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Name, _session.CurrentUserName ?? "Usuário"),
                new Claim(ClaimTypes.Email, _session.CurrentUserEmail),
                new Claim(ClaimTypes.NameIdentifier, _session.CurrentUserEmail)
            ],
            authenticationType: "WebSession");
            user = new ClaimsPrincipal(identity);
        }
        else
        {
            user = new ClaimsPrincipal(new ClaimsIdentity());
        }

        return Task.FromResult(new AuthenticationState(user));
    }

    private void OnAuthenticationStateChanged() => NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
}
