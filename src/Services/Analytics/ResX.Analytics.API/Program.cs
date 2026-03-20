using System.Text.Json;
using ResX.Analytics.Infrastructure;
using Microsoft.OpenApi.Models;
using ResX.Analytics.API;
using ResX.Analytics.Application.Queries.GetEcoStats;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(GetEcoStatsQuery).Assembly));

builder.Services.AddAnalyticsInfrastructure();

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ResX Analytics API",
        Version = "v1",
        Description = "Агрегированная экологическая аналитика платформы ресурс-кроссинга"
    });
});

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(o => 
        o.SwaggerEndpoint(url: "/swagger/v1/swagger.json", name: "ResX Analytics API v1"));
}

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

public partial class Program { }
