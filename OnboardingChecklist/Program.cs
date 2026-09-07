using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using OnboardingChecklist;
using OnboardingChecklist.Model;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Side menu + one page at a time; Shell owns that switch.
builder.RootComponents.Add<Shell>("#app");

// Singleton so answers survive re-renders.
builder.Services.AddSingleton<OnboardingState>();
builder.Services.AddSingleton<AppState>();

await builder.Build().RunAsync();
