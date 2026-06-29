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
    public DbSet<DiscoveryLineage> DiscoveryLineages => Set<DiscoveryLineage>();
    public DbSet<DiscoveryCandidate> DiscoveryCandidates => Set<DiscoveryCandidate>();
    public DbSet<DiscoveryProgress> DiscoveryProgresses => Set<DiscoveryProgress>();
    public DbSet<BehaviorWeight> BehaviorWeights => Set<BehaviorWeight>();
    public DbSet<FactorWeight> FactorWeights => Set<FactorWeight>();
    public DbSet<DiscoveryTuningSettings> DiscoveryTuningSettings => Set<DiscoveryTuningSettings>();
    public DbSet<CombatTuningSettings> CombatTuningSettings => Set<CombatTuningSettings>();
    public DbSet<WeaponDefinition> WeaponDefinitions => Set<WeaponDefinition>();
    public DbSet<MonsterDefinition> MonsterDefinitions => Set<MonsterDefinition>();
    public DbSet<PlayerDefinition> PlayerDefinitions => Set<PlayerDefinition>();
    public DbSet<ContractRewardTuning> ContractRewardTuning => Set<ContractRewardTuning>();
    public DbSet<ItemDefinition> ItemDefinitions => Set<ItemDefinition>();
    public DbSet<Settlement> Settlements => Set<Settlement>();
    public DbSet<PlayerProfile> PlayerProfiles => Set<PlayerProfile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
