namespace ProjectAscension.Combat
{
    /// <summary>
    /// A monster exposing its discovery context tag. Defeating it flavors the player's
    /// discovery context (combat-framework.md: "몬스터는 발견의 촉매" — certain monsters
    /// induce certain discoveries). Lives in Combat so the discovery observers can read
    /// it off a killed GameObject without referencing the Monsters assembly.
    /// </summary>
    public interface IMonsterInfo
    {
        string DiscoveryTag { get; }
    }
}
