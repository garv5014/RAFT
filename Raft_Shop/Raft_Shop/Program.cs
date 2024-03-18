using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Raft_Shop.Client.Pages;
using Raft_Shop.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder
    .Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Counter).Assembly);

app.Run();
