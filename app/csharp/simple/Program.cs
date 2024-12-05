using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using DotNetEnv;
using System;

AppContext.SetSwitch("Azure.Experimental.EnableActivitySource", true);
AppContext.SetSwitch("Azure.Experimental.TraceGenAIMessageContent", true);

Env.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});

builder.Services.AddSingleton<Simple.Services.AoaiService>();
builder.Services.AddSingleton<Simple.Services.SearchClientBuilder>();
builder.Services.AddSingleton<Simple.Services.AiSearchService>();

builder.Services.AddOpenTelemetry().UseAzureMonitor().WithTracing();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseCors("AllowAll");

app.UseRouting();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});

app.Run("http://0.0.0.0:8080");