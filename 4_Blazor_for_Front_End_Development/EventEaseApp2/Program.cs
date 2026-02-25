using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using EventEaseApp2;
using EventEaseApp2.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped<LocalStorageService>();
builder.Services.AddScoped<EventService>();
builder.Services.AddScoped<UserSessionService>();
builder.Services.AddScoped<AttendanceTrackerService>();

await builder.Build().RunAsync();
