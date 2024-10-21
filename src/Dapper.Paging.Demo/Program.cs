using Dapper.Paging.Demo.Extensions;
using Dapper.Paging.Demo.Middleware;
using Dapper.Paging.Demo.Services.Repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.IO;
using OpenTelemetry.Trace;


Console.WriteLine("Started");
var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();
builder.Host.UseContentRoot(Directory.GetCurrentDirectory());

// Because we are accessing a Scoped service via the IOptionsSnapshot provider,
// we must disable the dependency injection scope validation feature:
builder.Host.UseDefaultServiceProvider(options => options.ValidateScopes = false);

// If needed, Clear default providers
// builder.Logging.ClearProviders();
builder.Services.AddOpenTelemetry()
      .WithTracing(tracing => tracing
          .AddAspNetCoreInstrumentation()
          .AddSqlClientInstrumentation()
          .AddOtlpExporter(otlpOptions =>
          {
              otlpOptions.Endpoint = new Uri("http://splunk-otel-collector-agent.splunk.svc.cluster.local:4317");
          }));

// Use Serilog
builder.Host.UseSerilog((hostingContext, loggerConfiguration) => loggerConfiguration
    .ReadFrom.Configuration(hostingContext.Configuration)
    .Enrich.FromLogContext());

// Add services required for using options
builder.Services.AddOptions();

// Configure AppSettings
builder.Services.AppSettingsConfiguration(builder.Configuration);

builder.Services.AddRazorPages();

// Add services
builder.Services.AddTransient<IPersonRepository, PersonRepository>();

builder.Services.AddMvc(options => options.EnableEndpointRouting = false);

var app = builder.Build();

// Handling Errors Globally with the Custom Middleware
app.UseMiddleware<ExceptionMiddleware>();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseCors();

app.MapRazorPages();

app.UseMvc();

Console.WriteLine("before run");
app.Run();