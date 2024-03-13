using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Raft_Gateway.Options;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();
builder.Services.AddControllers();
builder.AddApiOptions();


Uri collector_uri = new Uri(builder?.Configuration["CollectorURL"] ?? throw new Exception("No Collector Menu Found"));
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resourceBuilder =>
    {
        resourceBuilder
                .AddService("Gateway");
    })
  .WithTracing(tracing =>
  {
      tracing
              .AddAspNetCoreInstrumentation() // Automatic instrumentation for ASP.NET Core
              .AddHttpClientInstrumentation() // Automatic instrumentation for HttpClient
              .AddEntityFrameworkCoreInstrumentation()
              .AddSource("Gateway")
              .AddOtlpExporter(options =>
        {
            options.Endpoint = collector_uri; // OTLP exporter endpoint
        });
      // You can add more instrumentation or exporters as needed
  }).WithMetrics(metrics =>
  {
      metrics.AddMeter("Microsoft.AspNetCore.Hosting")
      .AddMeter("Microsoft.AspNetCore.Http")
      .AddPrometheusExporter()
      // The rest of your setup code goes here too
      .AddOtlpExporter(options =>
      {
          options.Endpoint = collector_uri;
      });
  });

builder.Services.AddLogging(l =>
{
    l.AddOpenTelemetry(o =>
    {
        o.SetResourceBuilder(
            ResourceBuilder.CreateDefault().AddService("Gateway"))
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
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();