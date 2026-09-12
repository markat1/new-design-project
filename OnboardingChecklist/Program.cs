using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using OnboardingChecklist;
using OnboardingChecklist.Model;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Side menu + one page at a time; Shell owns that switch.
builder.RootComponents.Add<Shell>("#app");

// Singletons: the draft survives re-renders, the store is shared by both pages.
builder.Services.AddSingleton<MailDraft>();
builder.Services.AddSingleton<AppState>();
builder.Services.AddSingleton<MailStore>();

var host = builder.Build();

// The price sheets are real workbooks. They are read once here, with the Open
// XML SDK, so every page below can ask the accounting system a plain question
// and get an answer without awaiting anything.
using (var http = new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) })
{
    await Accounting.LoadAll(http, MarketingGroup.All.Select(p => p.Company));
}

await host.RunAsync();
