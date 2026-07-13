#nullable enable
using System;

namespace ProjectAscension.Domain.Entities
{
    /// <summary>
    /// Ownership of a discovery as a knowledge asset (discovery.md: 발견 → 지식 →
    /// 소유권). The first discoverer becomes the first owner; ownership can later move
    /// (trade/license) — that economy is out of slice scope, so this is an architecture
    /// hook only (CLAUDE.md). "발견은 역사 자산, 보유는 경제 자산."
    /// </summary>
    public class Knowledge
    {
        public Guid Id { get; set; }
        public Guid DiscoveryId { get; set; }
        public Guid OwnerActorId { get; set; }
        public DateTime CreatedAt { get; set; }

        /// <summary>Whether the owner has sold a license for this knowledge (server-
        /// authoritative — a license can be sold exactly ONCE per discovery; the discoverer
        /// keeps the discovery itself, ADR 0002/0014). Set the instant the sale is paid out.</summary>
        public bool Licensed { get; set; }
        public DateTime? LicensedAt { get; set; }

        public Discovery? Discovery { get; set; }
        public Actor? Owner { get; set; }
    }
}
