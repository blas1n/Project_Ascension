namespace ProjectAscension.GameSimulation.Combat
{
    /// <summary>The discrete button inputs a command's invocation combo is built from.
    /// Mirrors the server's <c>ProjectAscension.SkillForge.InputToken</c> — names match
    /// the API's combo strings, so they parse straight in.</summary>
    public enum InputToken
    {
        Jump,
        Dodge,
        LeftClick,
        RightClick,
    }
}
