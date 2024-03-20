using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Raft_Library.Gateway.shared;
using Raft_Library.Shop.shared;
using Raft_Library.Shop.shared.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddHttpClient(
    "Gatewayclient",
    client => client.BaseAddress = new Uri("http://gateway:8080")
);

builder.Services.AddScoped<IGatewayClient, GatewayService>(sp => new GatewayService(
    sp.GetRequiredService<IHttpClientFactory>().CreateClient("Gatewayclient")
));

builder.Services.AddScoped<IUserService, UserService>();

builder.Services.AddScoped<IInventoryService, ShopInventoryService>();

await builder.Build().RunAsync();
