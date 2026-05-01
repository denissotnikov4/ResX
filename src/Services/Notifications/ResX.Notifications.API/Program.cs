using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using ResX.Common.Extensions;
using ResX.EventBus.RabbitMQ.Abstractions;
using ResX.Notifications.Application.Commands;
using ResX.Notifications.Application.IntegrationEvents;
using ResX.Notifications.Infrastructure;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using ResX.Notifications.API;
using ResX.Notifications.Application.Commands.CreateNotification;
using ResX.Notifications.Application.IntegrationEvents.MessageSent;
using ResX.Notifications.Application.IntegrationEvents.TransactionCompleted;
using ResX.Notifications.Application.IntegrationEvents.TransactionCreated;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddMediatRWithValidation(typeof(CreateNotificationCommand).Assembly);
builder.Services.AddNotificationsInfrastructure(builder.Configuration);

// Register event handlers
builder.Services.AddScoped<TransactionCreatedNotificationHandler>();
builder.Services.AddScoped<TransactionCompletedNotificationHandler>();
builder.Services.AddScoped<MessageReceivedNotificationHandler>();

var jwtKey = builder.Configuration["Jwt:SecretKey"]!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = true, ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true, ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true, ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase);
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new OpenApiInfo { Title = "ResX Notifications API", Version = "v1" });
    o.ApplyResXDefaults();
    o.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header
    });
    o.OperationFilter<AuthOperationFilter>();
});

builder.Services.AddHealthChecks()
    .AddNpgSql(
        builder.Configuration.GetConnectionString("NotificationsDb")!,
        name: "postgres",
        failureStatus: HealthStatus.Unhealthy);

var app = builder.Build();
app.Services.RunMigrations();

// Subscribe to RabbitMQ events
var eventBus = app.Services.GetRequiredService<IEventBus>();
eventBus.Subscribe<TransactionCreatedIntegrationEvent, TransactionCreatedNotificationHandler>();
eventBus.Subscribe<TransactionCompletedIntegrationEvent, TransactionCompletedNotificationHandler>();
eventBus.Subscribe<MessageSentIntegrationEvent, MessageReceivedNotificationHandler>();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

public partial class Program { }
