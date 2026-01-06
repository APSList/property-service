using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Prometheus;
using property_service.Database;
using property_service.GraphQl.Queries;
using property_service.Interfaces;
using property_service.Options;
using property_service.Services;
using Serilog;
using Serilog.Formatting.Compact;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularDev", policy =>
    {
        policy
            .WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod();
        // .AllowCredentials(); // samo èe uporabljaš cookies/auth z withCredentials
    });
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//Options
builder.Services
    .AddOptions<SupabaseStorageOptions>()
    .Bind(builder.Configuration.GetSection(SupabaseStorageOptions.SectionName))
    .ValidateDataAnnotations()
    .Validate(opt => !string.IsNullOrEmpty(opt.Url), "Url should not be empty")
    .Validate(opt => !string.IsNullOrEmpty(opt.ServiceRoleKey), "Url should not be empty")
    .ValidateOnStart();

//Dependency Injection
builder.Services.AddScoped<IPropertyService, PropertyService>();
builder.Services.AddSingleton<ISupabaseStorageService, SupabaseStorageService>();

builder.Services
    .AddGraphQLServer()
    .ModifyCostOptions(o =>
    {
        o.MaxFieldCost = 5_000;
        o.MaxTypeCost = 5_000;
    })
    .AddQueryType<PropertyQuery>()
    .AddFiltering()
    .AddSorting();

//Database
builder.Services.AddDbContext<PropertyDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Supabase")));

//Logging
builder.Host.UseSerilog((context, config) =>
{
    config
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning) // Manjši šum
        .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Service", "payment-service")
        .WriteTo.Console(new RenderedCompactJsonFormatter());
});

//Health checks
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy())
    .AddNpgSql(
        builder.Configuration.GetConnectionString("Supabase") ?? "",
        name: "postgres",
        failureStatus: HealthStatus.Unhealthy
    );

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors("AllowAngularDev");
}

// Health endpoints
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Name == "self"
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = _ => true
});

// GraphQL endpoint
app.MapGraphQL("/graphql");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapGet("/ok", () =>
{
    Log.Information("OK endpoint called");
    return "OK";
});

app.MapGet("/error", () =>
{
    Log.Error("Something went wrong");
    return Results.Problem("Error");
});

app.UseHttpMetrics();     // meri HTTP odzivnost, status kode, metode
app.MapMetrics();         // metrics endpoint

app.UseSerilogRequestLogging();

app.MapControllers();

app.Run();