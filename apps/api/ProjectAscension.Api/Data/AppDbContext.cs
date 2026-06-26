using Microsoft.EntityFrameworkCore;
using ProjectAscension.Domain.Entities;

namespace ProjectAscension.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Actor> Actors => Set<Actor>();
    public DbSet<Character> Characters => Set<Character>();
    public DbSet<NPC> NPCs => Set<NPC>();
    public DbSet<Contract> Contracts => Set<Contract>();
    public DbSet<ContractMarketAccessPoint> ContractMarketAccessPoints => Set<ContractMarketAccessPoint>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<Equipment> Equipments => Set<Equipment>();
    public DbSet<Loadout> Loadouts => Set<Loadout>();
    public DbSet<Region> Regions => Set<Region>();
    public DbSet<MonsterSpecies> MonsterSpecies => Set<MonsterSpecies>();
    public DbSet<Monster> Monsters => Set<Monster>();
    public DbSet<Discovery> Discoveries => Set<Discovery>();
    public DbSet<DiscoverySkill> DiscoverySkills => Set<DiscoverySkill>();
    public DbSet<Knowledge> Knowledge => Set<Knowledge>();
    public DbSet<DiscoveryCandidate> DiscoveryCandidates => Set<DiscoveryCandidate>();
    public DbSet<DiscoveryProgress> DiscoveryProgresses => Set<DiscoveryProgress>();
    public DbSet<BehaviorWeight> BehaviorWeights => Set<BehaviorWeight>();
    public DbSet<DiscoveryTuningSettings> DiscoveryTuningSettings => Set<DiscoveryTuningSettings>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
