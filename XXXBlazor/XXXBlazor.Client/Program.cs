using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using XXXBlazor.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Services.AddScoped<IHdf5FileReader, Hdf5FileReader>();

builder.Services.AddSingleton<Hdf5StateService>();

builder.Services.AddDevExpressBlazor(options => { options.BootstrapVersion = DevExpress.Blazor.BootstrapVersion.v5; });

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

await builder.Build().RunAsync();
