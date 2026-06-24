namespace ProjectAscension.Game
{
    /// <summary>Stepping onto the return pad takes the player back to the City.</summary>
    public sealed class ReturnZone : PlayerTriggerVolume
    {
        protected override void OnPlayerEntered() => GameScenes.LoadCity();
    }
}
