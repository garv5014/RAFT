using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Raft_Node;
using Raft_Node.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.AddApiOptions();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();

Uri collector_uri = new Uri(
    builder?.Configuration["CollectorURL"] ?? throw new Exception("No Collector Menu Found")
);

builder
    .Services.AddOpenTelemetry()
    .ConfigureResource(resourceBuilder =>
    {
        resourceBuilder.AddService("Gateway");
    })
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation() // Automatic instrumentation for ASP.NET Core
            .AddHttpClientInstrumentation() // Automatic instrumentation for HttpClient
            .AddEntityFrameworkCoreInstrumentation()
            .AddSource("Node")
            .AddOtlpExporter(options =>
            {
                options.Endpoint = collector_uri; // OTLP exporter endpoint
            });
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddMeter("Microsoft.AspNetCore.Hosting")
            .AddMeter("Microsoft.AspNetCore.Http")
            .AddPrometheusExporter()
            .AddOtlpExporter(options =>
            {
                options.Endpoint = collector_uri;
            });
    });

builder.Services.AddLogging(l =>
{
    l.AddOpenTelemetry(o =>
    {
        o.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("Node"))
            .AddOtlpExporter(options =>
            {
                options.Endpoint = collector_uri;
            });
    });
});

builder.Services.AddSingleton<IRaftNodeClient, RaftNodeClient>();

builder.Services.AddSingleton<RaftNodeService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<RaftNodeService>());
var app = builder.Build();

try
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapControllers();
    Console.WriteLine(
        "Node Identifier: " + app.Services.GetRequiredService<ApiOptions>().NodeIdentifier
            ?? "no service"
    );
    app.Run();
}
catch (Exception ex)
{
    Console.WriteLine($"Error in the startup peaches {ex.Message}");
}
