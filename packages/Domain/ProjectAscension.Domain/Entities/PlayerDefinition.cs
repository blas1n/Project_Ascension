#nullable enable

namespace ProjectAscension.Domain.Entities
{
    /// <summary>
    /// The single-row player balance stats — health and movement — as
    /// server-managed, runtime-editable data. Moves the numbers out of the client
    /// ScriptableObject/components so balance and AI/dynamic systems can read and reshape
    /// them from the DB. (Camera feel and level geometry stay client-side preferences.)
    /// </summary>
    public class PlayerDefinition
    {
        public int Id { get; set; } // fixed singleton key (1)

        public float MaxHealth { get; set; }

        // Movement (fed into the shared PlayerSimulation).
        public float MoveSpeed { get; set; }
        public float JumpVelocity { get; set; }
        public float Gravity { get; set; }
    }
}
