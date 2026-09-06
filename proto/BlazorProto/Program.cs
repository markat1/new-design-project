using BlazorProto;
using BlazorProto.Model;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// No Router and no layout — the harness is a single page, so Proto is the root.
builder.RootComponents.Add<Proto>("#app");

// Singleton, so answers survive a variant switch.
builder.Services.AddSingleton<OnboardingState>();

await builder.Build().RunAsync();
