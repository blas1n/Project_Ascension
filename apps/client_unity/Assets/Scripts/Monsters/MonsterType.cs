namespace ProjectAscension.Monsters
{
    /// <summary>Guardian is not a spawn — it is PLACED, in the deep arena, and it is meant to kill you
    /// (docs/03-gameplay/first-hour-experience.md, stage 8: 사망은 연출된 경험이다).
    /// <see cref="Training"/> is also PLACED, not spawned in a wave — the city training ground's
    /// sparring partner (stage 2's "evade a wind-up, and strike"), tuned gentle so a brand-new,
    /// unequipped-for-real-danger player can safely learn to read a telegraph.</summary>
    public enum MonsterType { Melee, Ranged, Elite, Guardian, Training }
}
