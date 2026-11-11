using SendChecklist;
using SendChecklist.Model;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Two pages, Trinliste and Tjekliste, behind one layout that holds the picker.
builder.RootComponents.Add<App>("#app");

builder.Services.AddSingleton<Checklist>();

await builder.Build().RunAsync();
