#nullable enable

namespace ProjectAscension.Domain.Entities
{
    /// <summary>
    /// Single-row tuning for player-issued contract reward calibration. When a player
    /// issues a contract they choose the objective; the server computes a fair reward from
    /// the objective's difficulty (monster stats are already DB-driven) and only lets the
    /// player pick within a band — so issuing is a meaningful choice, not balance math or
    /// an economy exploit (ADR 0002: numbers are server-authoritative). Runtime-editable.
    /// </summary>
    public class ContractRewardTuning
    {
        public int Id { get; set; } // fixed singleton key (1)

        public float BaseRewardPerCount { get; set; }  // flat reward per objective unit
        public float DifficultyScale { get; set; }     // how much a target monster's stats add
        public int BandMinPercent { get; set; }         // player may offer down to this % of suggested
        public int BandMaxPercent { get; set; }         // ...and up to this % (more generous = more attractive)
    }
}
