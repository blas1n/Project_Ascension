using System.Collections.Generic;
using UnityEngine;
using ProjectAscension.GameSimulation.Physics;

namespace ProjectAscension.Combat
{
    /// <summary>
    /// The client's window into the deterministic <see cref="CollisionWorld"/> (ADR 0013): the one
    /// <see cref="Collision"/> the whole scene describes itself into via <see cref="SimBody"/>, plus
    /// the actor-id &lt;-&gt; GameObject bookkeeping combat needs to turn a hit's actor id back into
    /// something it can call TakeDamage on. Unity's job is to DESCRIBE the world here and RENDER
    /// whatever the sim decides — it no longer reaches its own verdict.
    ///
    /// One registry for the process's lifetime, not per-scene: City and Frontier are separate scene
    /// loads (LoadSceneMode.Single), so every SimBody re-registers on enable and unregisters on
    /// destroy — the world just empties out and refills as scenes switch.
    /// </summary>
    public static class SimWorld
    {
        public static CollisionWorld Collision { get; } = new CollisionWorld();

        private static int _nextBodyId = 1;
        private static int _nextActorId = 1; // 0 is reserved for static level geometry — never allocated
        private static readonly Dictionary<int, GameObject> Actors = new();

        public static int AllocateBodyId() => _nextBodyId++;

        /// <summary>A fresh actor id for something that can be damaged/targeted (player, monster,
        /// training dummy...). Actor id 0 always means static level geometry / "nobody".</summary>
        public static int AllocateActorId(GameObject owner)
        {
            int id = _nextActorId++;
            Actors[id] = owner;
            return id;
        }

        public static void ReleaseActor(int actorId) => Actors.Remove(actorId);

        public static GameObject ActorGameObject(int actorId)
            => Actors.TryGetValue(actorId, out var go) ? go : null;

        /// <summary>The IDamageable behind an actor id, if any — false for actor 0 (static geometry)
        /// or any id nothing ever claimed.</summary>
        public static bool TryGetDamageable(int actorId, out IDamageable damageable)
        {
            damageable = null;
            var go = ActorGameObject(actorId);
            return go != null && go.TryGetComponent(out damageable);
        }

        /// <summary>The actor id a GameObject fights AS — the id to pass as a sweep/overlap's
        /// ignoreActorId so an attack never hits its own owner. 0 (excludes nobody) if the object
        /// (or nothing above it) carries a <see cref="SimBody"/> yet.</summary>
        public static int ActorIdOf(GameObject go)
            => go != null && go.TryGetComponent<SimBody>(out var body) ? body.ActorId : 0;
    }
}
