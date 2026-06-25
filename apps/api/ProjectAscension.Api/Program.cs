using Microsoft.EntityFrameworkCore;
using ProjectAscension.Api.Data;
using ProjectAscension.Api.Data.Repositories;
using ProjectAscension.Api.Middleware;
using ProjectAscension.Api.Services;
using ProjectAscension.Domain.Interfaces;
using ProjectAscension.SkillForge;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// Repositories
builder.Services.AddScoped<ICharacterRepository, CharacterRepository>();
builder.Services.AddScoped<IContractRepository, ContractRepository>();
builder.Services.AddScoped<IDiscoveryRepository, DiscoveryRepository>();
builder.Services.AddScoped<IDiscoverySkillRepository, DiscoverySkillRepository>();
builder.Services.AddScoped<IItemRepository, ItemRepository>();

// Services
builder.Services.AddScoped<ICharacterService, CharacterService>();
builder.Services.AddScoped<IContractService, ContractService>();
builder.Services.AddScoped<IDiscoveryService, DiscoveryService>();
builder.Services.AddScoped<IItemService, ItemService>();
builder.Services.AddScoped<ILoadoutService, LoadoutService>();
builder.Services.AddScoped<ISkillCompositionService, SkillCompositionService>();

// AI skill composition: the stub composer for now; the LLM-backed composer
// (Microsoft.Extensions.AI → Ollama/OpenAI/Claude) swaps in here (Stage 1.6).
builder.Services.AddSingleton<ISkillComposer, StubSkillComposer>();
builder.Services.AddHostedService<SkillCompositionWorker>();

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();
