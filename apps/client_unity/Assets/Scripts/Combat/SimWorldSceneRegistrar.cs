using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectAscension.Combat
{
    /// <summary>
    /// Describes a freshly-loaded scene's ALREADY-BUILT geometry into <see cref="SimWorld"/> in one
    /// pass — the level blockout (CityBlockout/FrontierBlockout build themselves in Awake, before
    /// this runs), NPCs, the training dummy, the contract board, and the player (baked into the
    /// scene, present by the time Awake has run). Anything spawned LATER in the scene's life
    /// (monsters, via MonsterFactory) registers itself explicitly at creation instead — this scan
    /// only runs once per scene load and would otherwise miss them entirely.
    ///
    /// Self-installs (the same DontDestroyOnLoad + RuntimeInitializeOnLoadMethod idiom as
    /// TutorialRunner) so nothing needs to be hand-placed in any scene, and re-scans on EVERY scene
    /// load — City and Frontier switch via LoadSceneMode.Single (a full scene swap, not additive),
    /// so each one needs its own pass; SimWorld.Collision itself isn't cleared between scenes, since
    /// every SimBody unregisters its own body in OnDestroy as the old scene's objects go away.
    /// </summary>
    public sealed class SimWorldSceneRegistrar : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (FindAnyObjectByType<SimWorldSceneRegistrar>() != null) return;
            var go = new GameObject("SimWorldSceneRegistrar");
            DontDestroyOnLoad(go);
            go.AddComponent<SimWorldSceneRegistrar>();
        }

        private void Awake()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            ScanAndAttach(); // the scene already loaded by the time we self-install
        }

        private void OnDestroy() => SceneManager.sceneLoaded -= OnSceneLoaded;

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => ScanAndAttach();

        /// <summary>Every enabled, non-trigger Collider in the scene without a SimBody yet gets one.
        /// Anything carrying a HitReceiver (the player, a training dummy — a monster only if it
        /// somehow predates this pass) gets its own actor id, shared by every collider under that
        /// SAME HitReceiver (a dummy's body AND its separate head collider must resolve to ONE
        /// actor, or a single swing would double-damage it via OverlapSphere's per-actor dedupe
        /// silently not applying). Everything else is static level geometry (actor 0).</summary>
        private static void ScanAndAttach()
        {
            var actorIdsByReceiver = new Dictionary<HitReceiver, int>();
            var colliders = FindObjectsByType<Collider>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

            foreach (var collider in colliders)
            {
                if (collider.isTrigger) continue; // a volume, not solid geometry (e.g. the deep-zone gate)
                if (collider.GetComponent<SimBody>() != null) continue; // already registered

                var body = collider.gameObject.AddComponent<SimBody>();
                var receiver = collider.GetComponentInParent<HitReceiver>();
                int actorId = 0;
                if (receiver != null)
                {
                    if (!actorIdsByReceiver.TryGetValue(receiver, out actorId))
                    {
                        actorId = SimWorld.AllocateActorId(receiver.gameObject);
                        actorIdsByReceiver[receiver] = actorId;
                    }
                }
                body.Configure(actorId);
            }
        }
    }
}
