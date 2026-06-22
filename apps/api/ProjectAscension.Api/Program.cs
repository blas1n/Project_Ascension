using Microsoft.EntityFrameworkCore;
using ProjectAscension.Api.Data;
using ProjectAscension.Api.Data.Repositories;
using ProjectAscension.Api.Middleware;
using ProjectAscension.Api.Services;
using ProjectAscension.Domain.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// Repositories
builder.Services.AddScoped<ICharacterRepository, CharacterRepository>();
builder.Services.AddScoped<IContractRepository, ContractRepository>();
builder.Services.AddScoped<IDiscoveryRepository, DiscoveryRepository>();
builder.Services.AddScoped<IItemRepository, ItemRepository>();

// Services
builder.Services.AddScoped<ICharacterService, CharacterService>();
builder.Services.AddScoped<IContractService, ContractService>();
builder.Services.AddScoped<IDiscoveryService, DiscoveryService>();
builder.Services.AddScoped<IItemService, ItemService>();
builder.Services.AddScoped<ILoadoutService, LoadoutService>();

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();
