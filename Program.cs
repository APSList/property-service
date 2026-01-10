using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Prometheus;
using property_service;
using property_service.Database;
using property_service.GraphQl.Queries;
using property_service.Interfaces;
using property_service.Options;
using property_service.Services;
using Serilog;
using Serilog.Formatting.Compact;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularDev", policy =>
    {
        policy
            .WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();

// Options
builder.Services
    .AddOptions<SupabaseStorageOptions>()
    .Bind(builder.Configuration.GetSection(SupabaseStorageOptions.SectionName))
    .ValidateDataAnnotations()
    .Validate(opt => !string.IsNullOrEmpty(opt.Url), "Url should not be empty")
    .Validate(opt => !string.IsNullOrEmpty(opt.ServiceRoleKey), "ServiceRoleKey should not be empty")
    .ValidateOnStart();

builder.Services.AddHttpContextAccessor();

// Dependency Injection
builder.Services.AddScoped<IPropertyService, PropertyService>();
builder.Services.AddSingleton<ISupabaseStorageService, SupabaseStorageService>();
builder.Services.AddScoped<IOrganizationContext, OrganizationContext>();

// GraphQL
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

// Database
builder.Services.AddDbContext<PropertyDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Supabase")));

// Logging
builder.Host.UseSerilog((context, config) =>
{
    config
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Service", "property-service")
        .WriteTo.Console(new RenderedCompactJsonFormatter());
});

// Health checks
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy())
    .AddNpgSql(
        builder.Configuration.GetConnectionString("Supabase") ?? "",
        name: "postgres",
        failureStatus: HealthStatus.Unhealthy
    );

// Auth (Supabase issuer)
var issuer = "https://frauwrkbphmjngymcdyk.supabase.co/auth/v1";

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = issuer;
        options.RequireHttpsMetadata = true;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,

            ValidateAudience = true,
            ValidAudience = "authenticated",

            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(5),

            ValidAlgorithms = new[] { SecurityAlgorithms.EcdsaSha256 },

            NameClaimType = ClaimTypes.NameIdentifier,
            RoleClaimType = ClaimTypes.Role
        };

        options.ConfigurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
            $"{issuer}/.well-known/openid-configuration",
            new OpenIdConnectConfigurationRetriever()
        );

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                var identity = context.Principal?.Identity as ClaimsIdentity;
                if (identity is null)
                {
                    context.Fail("Missing identity.");
                    return Task.CompletedTask;
                }

                var userMetadataJson = context.Principal!.FindFirst("user_metadata")?.Value;
                if (string.IsNullOrWhiteSpace(userMetadataJson))
                {
                    context.Fail("Missing user_metadata.");
                    return Task.CompletedTask;
                }

                try
                {
                    using var doc = JsonDocument.Parse(userMetadataJson);

                    if (!doc.RootElement.TryGetProperty("organization_id", out var orgEl))
                    {
                        context.Fail("Missing user_metadata.organization_id.");
                        return Task.CompletedTask;
                    }

                    int? orgId =
                        orgEl.ValueKind == JsonValueKind.Number ? orgEl.GetInt32() :
                        orgEl.ValueKind == JsonValueKind.String && int.TryParse(orgEl.GetString(), out var parsed) ? parsed :
                        (int?)null;

                    if (orgId is null || orgId <= 0)
                    {
                        context.Fail("Invalid organization_id.");
                        return Task.CompletedTask;
                    }

                    identity.AddClaim(new Claim("organization_id", orgId.Value.ToString()));

                    if (doc.RootElement.TryGetProperty("role", out var roleEl) &&
                        roleEl.ValueKind == JsonValueKind.String)
                    {
                        var role = roleEl.GetString();
                        if (!string.IsNullOrWhiteSpace(role))
                            identity.AddClaim(new Claim(ClaimTypes.Role, role));
                    }

                    return Task.CompletedTask;
                }
                catch (JsonException)
                {
                    context.Fail("Invalid user_metadata JSON.");
                    return Task.CompletedTask;
                }
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("OrgRequired", policy => policy.RequireClaim("organization_id"));
});

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "JWT Authorization header using the Bearer scheme."
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("bearer", document)] = []
    });
});

var app = builder.Build();

var cfgPrefix = builder.Configuration["SwaggerPrefix"];

if (!string.IsNullOrEmpty(cfgPrefix))
{
    app.UseSwagger(c =>
    {
        c.PreSerializeFilters.Add((swaggerDoc, httpReq) =>
        {
            swaggerDoc.Servers = new List<OpenApiServer>
            {
                new() { Url = $"https://{httpReq.Host}{cfgPrefix}" }
            };
        });
    });
}
else
{
    app.UseSwagger();
}

app.UseSwaggerUI(c =>
{
    c.RoutePrefix = "swagger";
    c.SwaggerEndpoint("./v1/swagger.json", "property-service v1");
});

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

app.UseHttpMetrics();
app.MapMetrics();

app.UseHttpsRedirection();

app.UseAuthentication();   // <- dodano (manjkalo)
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

app.UseSerilogRequestLogging();

app.MapControllers();

app.Run();
