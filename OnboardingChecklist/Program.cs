using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using OnboardingChecklist;
using OnboardingChecklist.Model;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Single page, no router: Onboarding is the root component.
builder.RootComponents.Add<Onboarding>("#app");

// Singleton so answers survive re-renders.
builder.Services.AddSingleton<OnboardingState>();

await builder.Build().RunAsync();
