namespace Raft_Shop.Options;

public class GatewayApiOptions
{
    public string GatewayAddress { get; set; } = $"http://Gateway:8080";
}

public static class ApiOptionsExtensions
{
    public static WebApplicationBuilder AddApiOptions(this WebApplicationBuilder builder)
    {
        var apiOptions = new GatewayApiOptions();
        builder.Configuration.Bind(nameof(GatewayApiOptions), apiOptions);
        builder.Services.AddSingleton(apiOptions);
        return builder;
    }
}
