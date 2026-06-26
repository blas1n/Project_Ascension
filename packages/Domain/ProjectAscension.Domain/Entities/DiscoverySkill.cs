#nullable enable
using System;
using ProjectAscension.Domain.Enums;

namespace ProjectAscension.Domain.Entities
{
    /// <summary>
    /// The AI-created content for a <see cref="Discovery"/> (ADR 0002 fact/content
    /// separation). The fact is fixed instantly; this is composed asynchronously and
    /// frozen once <see cref="DiscoveryContentStatus.Ready"/>. Seed fields are
    /// captured at trigger time; content fields are filled when composed.
    /// PrimaryBehavior / primitives are stored as strings / JSON so the Domain stays
    /// free of the server-only composition package.
    /// </summary>
    public class DiscoverySkill
    {
        public Guid Id { get; set; }
        public Guid DiscoveryId { get; set; }
        public DiscoveryContentStatus Status { get; set; } = DiscoveryContentStatus.Pending;

        /// <summary>Optional client-supplied key for idempotent triggers — a retried
        /// trigger with the same key returns the existing discovery instead of a
        /// duplicate. Unique when present (Postgres treats NULLs as distinct).</summary>
        public string? IdempotencyKey { get; set; }

        // Seed (captured at trigger).
        public string Theme { get; set; } = string.Empty;
        public string ContextTagsJson { get; set; } = "[]";
        public string PrimaryBehavior { get; set; } = string.Empty; // PrimitiveKind name

        /// <summary>The player behaviors that triggered this discovery (e.g. ["Dodge",
        /// "MeleeAttack"]) — a Command is invoked by re-performing this combo. JSON array.</summary>
        public string BehaviorsJson { get; set; } = "[]";

        public int PowerBudget { get; set; }

        // Content (filled when Ready).
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? PrimitivesJson { get; set; }
        public int? PowerCost { get; set; }

        /// <summary>How the skill is wielded — "Weapon" (a synthesized-magic weapon) or
        /// "Command" (an invoked technique). Classified deterministically at compose
        /// time. Stored as a string so the Domain stays free of the SkillForge package.</summary>
        public string? Manifestation { get; set; }

        public int Attempts { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ComposedAt { get; set; }

        public Discovery? Discovery { get; set; }
    }
}
