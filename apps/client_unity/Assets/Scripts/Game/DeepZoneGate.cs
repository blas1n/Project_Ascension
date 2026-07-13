using UnityEngine;
using ProjectAscension.GameSimulation.Items;

namespace ProjectAscension.Game
{
    /// <summary>
    /// The pass into the deep frontier. It is not locked — it is UNMAPPED: without the chart you cannot
    /// find the way through, and the ground turns you back. Holding the map is what opens it
    /// (GameSimulation Maps.CanEnterDeepFrontier — the rule, not this glue).
    ///
    /// This is what makes the first hour causal rather than merely ordered: the survey you did in stage
    /// 6 is what buys the contract that kills you in stage 8. And it's why "지도는 자산이다" is true —
    /// a map you could lose is a map whose loss closes a road.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public sealed class DeepZoneGate : MonoBehaviour
    {
        private const float PushBack = 3f;
        private const float NoticeSeconds = 3f;

        private float _noticeUntil;

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            var session = GameSession.Instance;
            var inventory = session != null && session.PlayerState != null ? session.PlayerState.Inventory : null;
            if (Maps.CanEnterDeepFrontier(inventory)) return; // charted — the way is plain

            // Unmapped: the player simply cannot find the pass. Turn them back at the threshold.
            _noticeUntil = Time.time + NoticeSeconds;

            var controller = other.GetComponent<CharacterController>();
            var back = (other.transform.position - transform.position);
            back.y = 0f;
            if (back.sqrMagnitude < 0.0001f) back = -transform.forward;
            back = back.normalized * PushBack;

            if (controller != null)
            {
                controller.enabled = false;
                other.transform.position += back;
                controller.enabled = true;
            }
            else
            {
                other.transform.position += back;
            }
        }

        private void OnGUI()
        {
            if (Time.time > _noticeUntil) return;

            var style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 15,
                fontStyle = FontStyle.Bold,
            };
            style.normal.textColor = new Color(0.95f, 0.85f, 0.8f);

            GUI.Label(new Rect((Screen.width - 620f) * 0.5f, Screen.height * 0.42f, 620f, 26f),
                "The way in is unmapped. You would not find it again.", style);
        }
    }
}
