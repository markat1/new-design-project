using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using OnboardingChecklist;
using OnboardingChecklist.Model;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Side menu + one page at a time; Shell owns that switch.
builder.RootComponents.Add<Shell>("#app");

// Singletons: the draft survives re-renders, the store is shared by both pages.
builder.Services.AddSingleton<QuoteDraft>();
builder.Services.AddSingleton<AppState>();
builder.Services.AddSingleton<QuoteStore>();

await builder.Build().RunAsync();
