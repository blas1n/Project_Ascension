using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using OllamaSharp;
using ProjectAscension.Api.Data;
using ProjectAscension.Api.Data.Repositories;
using ProjectAscension.Api.Middleware;
using ProjectAscension.Api.Services;
using ProjectAscension.Domain.Interfaces;
using ProjectAscension.SkillForge;

var builder = WebApplication.CreateBuilder(args);

// Serialize/accept enums as their names (e.g. "Skill", "Ready") rather than ints.
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// Repositories
builder.Services.AddScoped<ICharacterRepository, CharacterRepository>();
builder.Services.AddScoped<IContractRepository, ContractRepository>();
builder.Services.AddScoped<IDiscoveryRepository, DiscoveryRepository>();
builder.Services.AddScoped<IDiscoverySkillRepository, DiscoverySkillRepository>();
builder.Services.AddScoped<IKnowledgeRepository, KnowledgeRepository>();
builder.Services.AddScoped<IDiscoveryTuningRepository, DiscoveryTuningRepository>();
builder.Services.AddScoped<IItemRepository, ItemRepository>();

// Services
builder.Services.AddScoped<ICharacterService, CharacterService>();
builder.Services.AddScoped<IContractService, ContractService>();
builder.Services.AddScoped<IDiscoveryService, DiscoveryService>();
builder.Services.AddScoped<IItemService, ItemService>();
builder.Services.AddScoped<ILoadoutService, LoadoutService>();
builder.Services.AddScoped<ISkillCompositionService, SkillCompositionService>();
builder.Services.AddScoped<IDiscoveryTuningProvider, DiscoveryTuningProvider>();
builder.Services.AddScoped<IKnowledgeService, KnowledgeService>();
builder.Services.AddSingleton<CompositionMetrics>();

// AI skill composition. Provider-agnostic via Microsoft.Extensions.AI IChatClient:
// "Ollama" (default endpoint per config) uses the LLM composer; anything else
// (e.g. "Stub") uses the deterministic stub — handy for offline/CI and tests.
var composerProvider = builder.Configuration["SkillForge:Provider"] ?? "Stub";
if (string.Equals(composerProvider, "Ollama", StringComparison.OrdinalIgnoreCase))
{
    var endpoint = builder.Configuration["SkillForge:Ollama:Endpoint"] ?? "http://localhost:11434";
    var model = builder.Configuration["SkillForge:Ollama:Model"] ?? "llama3.2:3b";
    var timeoutSeconds = builder.Configuration.GetValue("SkillForge:Ollama:TimeoutSeconds", 30);
    builder.Services.AddSingleton(new LlmComposerOptions { Timeout = TimeSpan.FromSeconds(timeoutSeconds) });
    builder.Services.AddSingleton<IChatClient>(_ => new OllamaApiClient(new Uri(endpoint), model));
    builder.Services.AddSingleton<ISkillComposer, LlmSkillComposer>();
}
else
{
    builder.Services.AddSingleton<ISkillComposer, StubSkillComposer>();
}
builder.Services.AddHostedService<SkillCompositionWorker>();

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();
