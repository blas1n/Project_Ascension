#nullable enable

namespace ProjectAscension.Domain.Entities
{
    /// <summary>
    /// The persisted player progress (save/load) — currency, standing, materials, and the
    /// knowledge already licensed. Server-persistent so progress survives a quit. (Singleton
    /// for the single-player slice; per-character later.) Settlement and discovery records
    /// persist separately; this is the client-side progress that was otherwise ephemeral.
    /// </summary>
    public class PlayerProfile
    {
        public int Id { get; set; } // fixed singleton key (1)
        public int Currency { get; set; }
        public int Reputation { get; set; }
        public string ResourcesJson { get; set; } = "{}";       // { "hide": 2, "core": 1 }
        public string SoldKnowledgeJson { get; set; } = "[]";   // [ "Flame Bolt", ... ]
    }
}
