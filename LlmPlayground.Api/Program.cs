using LlmPlayground.Api.Configuration;
using LlmPlayground.Api.Services;
using LlmPlayground.Services.Extensions;
using LlmPlayground.Utilities.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add configuration
builder.Services.Configure<GameGenerationSettings>(
    builder.Configuration.GetSection("GameGeneration"));

// Add services
builder.Services.AddLlmPlaygroundServices();
builder.Services.AddLlmPlaygroundUtilities();
builder.Services.AddScoped<IGameGeneratorService, GameGeneratorService>();

// Add controllers
builder.Services.AddControllers();

// Add Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "LLM Playground API",
        Version = "v1",
        Description = "API for generating Prolog games using LLM providers"
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Make the implicit Program class public for testing
public partial class Program { }

