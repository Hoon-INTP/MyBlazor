using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.Forms;
using MyHdfViewer;
using MyHdfViewer.Services;
using MudBlazor.Services;
using Microsoft.FluentUI.AspNetCore.Components;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

//builder.Services.AddHttpClient();
builder.Services.AddFluentUIComponents();
builder.Services.AddMudServices(config => {
    config.SnackbarConfiguration.PositionClass = MudBlazor.Defaults.Classes.Position.TopRight;
});
//builder.Services.AddDataGridEntityFrameworkAdapter();
builder.Services.AddScoped<IHdf5FileReader, Hdf5FileReader>();
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

await builder.Build().RunAsync();
