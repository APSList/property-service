using Microsoft.EntityFrameworkCore;
using property_service.Database;
using property_service.GraphQl.Queries;
using property_service.Interfaces;
using property_service.Options;
using property_service.Services;
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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors("AllowAngularDev");
}

// GraphQL endpoint
app.MapGraphQL("/graphql");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();