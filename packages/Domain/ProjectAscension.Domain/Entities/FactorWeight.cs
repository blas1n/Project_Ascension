#nullable enable

namespace ProjectAscension.Domain.Entities
{
    /// <summary>
    /// Server-authoritative significance weight for a discovery context factor — an
    /// environment, a piece of equipment, or prior knowledge (discovery.md 발견 생성
    /// 요소). A behavior performed amid notable factors is more significant, so the
    /// same behavior at a waterfall vs an ice wall scores — and discovers —
    /// differently. <see cref="Category"/> is descriptive metadata; only the weight
    /// affects scoring. Stored as rows so balance designers can add factors or retune
    /// at runtime.
    /// </summary>
    public class FactorWeight
    {
        public string Key { get; set; } = string.Empty; // primary key (e.g. "waterfall", "fire")
        public string Category { get; set; } = string.Empty; // "Environment" | "Equipment" | "Knowledge"
        public int Weight { get; set; }
    }
}
