using Frontend;
using Frontend.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddScoped<AuthState>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<AuthorizedHttpClient>();
builder.Services.AddScoped<TokenGuard>();

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri("https://localhost:7236/")
});

await builder.Build().RunAsync();
