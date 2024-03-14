namespace Raft_Node;
public class ApiOptions
{
    public int NodeIdentifier { get; set; } = 0;
    public string NodeServiceName { get; set; } = "raftnode";
    public int NodeServicePort { get; set; } = 8080;
    public int NodeCount { get; set; } = 3;
    public string EntryLogPath { get; set; } = "entrylogs";
    public double LogMessageIntervalSeconds { get; set; } = 10;
}

public static class ApiOptionsExtensions
{
    public static WebApplicationBuilder AddApiOptions(this WebApplicationBuilder builder)
    {
        var apiOptions = new ApiOptions();
        builder.Configuration.Bind(nameof(ApiOptions), apiOptions);
        builder.Services.AddSingleton(apiOptions);
        return builder;
    }
}