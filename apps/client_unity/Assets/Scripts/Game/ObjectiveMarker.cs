using UnityEngine;
using ProjectAscension.Combat;
using ProjectAscension.GameSimulation.Tutorial;

namespace ProjectAscension.Game
{
    /// <summary>
    /// "Where do I go next?" made visible: a beam + ring at the current tutorial step's target
    /// station, so the guide's spoken line ("the board's this way") has something to actually walk
    /// toward. Reads <see cref="TutorialGuideScript"/> for the step's station ID and
    /// <see cref="TutorialGuideStations"/> to resolve it in whichever scene is loaded; draws nothing
    /// for a station-less step — a behaviour beat, or the directed stage-8 ambush (see
    /// TutorialGuideStation.None's doc comment for why those must NOT be beaconed) — or once the
    /// station simply isn't in this scene.
    ///
    /// Procedural VFX vocabulary, same idea as CombatVfx/SkillVfx (a bright unlit HDR material that
    /// catches URP bloom, see <see cref="CombatVfx.Glow"/>) but PERSISTENT rather than one-shot — a
    /// marker has to stay lit until the player reaches it or the step moves on, not flash and vanish.
    ///
    /// Self-installs once for the whole session (survives City &lt;-&gt; Frontier) — it needs no
    /// scene-specific references; TutorialGuideStations already handles "not in this scene" cleanly.
    /// </summary>
    public sealed class ObjectiveMarker : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (FindObjectOfType<ObjectiveMarker>() != null) return;
            var go = new GameObject("ObjectiveMarker");
            DontDestroyOnLoad(go);
            go.AddComponent<ObjectiveMarker>();
        }

        // Warm gold — reads as "go here", distinct from combat/skill palettes (reds/blues/purples)
        // and matches the guide's own lantern, so the two visibly belong to the same voice.
        private static readonly Color MarkerColor = new Color(1f, 0.82f, 0.35f);

        private const float BeamHeight = 7f;
        private const float RingRadius = 1.1f;
        private const int RingSegments = 28;
        private const float SpinDegreesPerSecond = 40f;
        private const float BobAmplitude = 0.3f;
        private const float BobPeriodSeconds = 1.6f;

        private LineRenderer _beam;
        private LineRenderer _ring;

        private void Awake()
        {
            _beam = MakeLine("ObjectiveMarker_Beam", 2, 0.08f, loop: false);
            _ring = MakeLine("ObjectiveMarker_Ring", RingSegments, 0.06f, loop: true);
            SetVisible(false);
        }

        private LineRenderer MakeLine(string name, int points, float width, bool loop)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.material = CombatVfx.Glow(MarkerColor);
            lr.startColor = lr.endColor = MarkerColor;
            lr.startWidth = lr.endWidth = width;
            lr.positionCount = points;
            lr.numCapVertices = 4;
            lr.loop = loop;
            lr.useWorldSpace = true;
            return lr;
        }

        private void Update()
        {
            var runner = TutorialRunner.Instance;
            var station = runner != null
                ? TutorialGuideScript.For(runner.Progress.Step).Station
                : TutorialGuideStation.None;

            if (station == TutorialGuideStation.None || !TutorialGuideStations.TryResolve(station, out var basePos))
            {
                SetVisible(false);
                return;
            }

            SetVisible(true);

            float bob = Mathf.Max(0f, Mathf.Sin(Time.time * (Mathf.PI * 2f / BobPeriodSeconds))) * BobAmplitude;
            var bottom = basePos + Vector3.up * (0.05f + bob);
            _beam.SetPosition(0, bottom);
            _beam.SetPosition(1, bottom + Vector3.up * BeamHeight);

            float spin = Time.time * SpinDegreesPerSecond * Mathf.Deg2Rad;
            for (int i = 0; i < _ring.positionCount; i++)
            {
                float a = spin + i * (Mathf.PI * 2f / _ring.positionCount);
                _ring.SetPosition(i, basePos + new Vector3(Mathf.Cos(a) * RingRadius, 0.08f, Mathf.Sin(a) * RingRadius));
            }
        }

        private void SetVisible(bool visible)
        {
            _beam.enabled = visible;
            _ring.enabled = visible;
        }
    }
}
