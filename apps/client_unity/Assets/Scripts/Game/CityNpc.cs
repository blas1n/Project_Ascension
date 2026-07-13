using UnityEngine;
using ProjectAscension.GameSimulation.Tutorial;

namespace ProjectAscension.Game
{
    /// <summary>
    /// A person in the city, standing where the work is. The first hour's last two lessons are supposed
    /// to be OFFERED — 위임 after you die ("이 계약을 다른 사람에게 넘길 수 있습니다"), and then 발주
    /// ("그렇다면 직접 해결할 사람을 구해보는 건 어떻습니까?"). They were an always-open dev panel instead:
    /// the game's two most human ideas, delivered as a toolbar.
    ///
    /// So they come from a person now. What each NPC says depends on where the player is in the first
    /// hour (the director decides that; this only reads it), and the issuing panel opens only when you
    /// are actually standing with the quartermaster who suggests it.
    /// </summary>
    public sealed class CityNpc : MonoBehaviour
    {
        public const float TalkReach = 3.5f;

        /// <summary>True while the player stands with the NPC who takes commissions — the issue panel
        /// (발주) is his to offer, not a window that is simply always there.</summary>
        public static bool NearIssuer { get; private set; }

        public enum Role { Quartermaster, Serjeant, Clerk }

        private string _name;
        private Role _role;
        private Transform _player;
        private bool _near;

        public void Configure(string npcName, Role role)
        {
            _name = npcName;
            _role = role;
        }

        private void OnDisable()
        {
            if (_near && _role == Role.Quartermaster) NearIssuer = false;
            _near = false;
        }

        private void Update()
        {
            if (_player == null)
            {
                var go = GameObject.FindWithTag("Player");
                if (go == null) return;
                _player = go.transform;
            }

            var p = _player.position;
            var me = transform.position;
            bool near = new Vector2(p.x - me.x, p.z - me.z).magnitude <= TalkReach;
            if (near == _near) return;

            _near = near;
            if (_role == Role.Quartermaster) NearIssuer = near;
        }

        private void OnGUI()
        {
            if (!_near || string.IsNullOrEmpty(_name)) return;

            var line = LineFor(_role, TutorialRunner.Instance != null
                ? TutorialRunner.Instance.Progress.Step
                : TutorialStep.Complete);
            if (string.IsNullOrEmpty(line)) return;

            const float w = 640f;
            var box = new Rect((Screen.width - w) * 0.5f, Screen.height * 0.62f, w, 56f);

            var name = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 12 };
            name.normal.textColor = new Color(0.75f, 0.8f, 0.9f);
            GUI.Label(new Rect(box.x, box.y, box.width, 18f), _name, name);

            var say = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 15,
                wordWrap = true,
            };
            say.normal.textColor = new Color(0.95f, 0.94f, 0.9f);
            GUI.Label(new Rect(box.x, box.y + 18f, box.width, 38f), $"“{line}”", say);
        }

        /// <summary>What this person has to say to a player at this point in their first hour. The people
        /// react to what has actually happened to you — the doc's beats, in their mouths.</summary>
        private static string LineFor(Role role, TutorialStep step) => role switch
        {
            Role.Serjeant => step switch
            {
                TutorialStep.Training => "Hit something. You'll learn more from the doing than from me.",
                TutorialStep.ChooseEquipment => "Two hands, two choices. Neither of them is wrong.",
                TutorialStep.FirstDeath => "The deep took better than you. Come back when you're heavier.",
                _ => "Keep your shield between you and it.",
            },

            Role.Clerk => step switch
            {
                TutorialStep.AcceptSurveyContract => "The board has work. Nothing out there is charted yet.",
                TutorialStep.EarnMap => "Walk it, mark it, come back. That's all a survey is.",
                TutorialStep.AcceptDeepContract => "You have the chart now. That opens roads I'd think twice about.",
                // The 위임 beat — offered, right after the world has just proved the point.
                TutorialStep.DelegateContract =>
                    "You can't finish that one. You don't have to — hand it to someone who can (위임).",
                _ => "Everything past the wall is someone's guess until you go and look.",
            },

            // The 발주 beat, verbatim in spirit: "그렇다면 직접 해결할 사람을 구해보는 건 어떻습니까?"
            _ => step switch
            {
                TutorialStep.IssueContract =>
                    "So hire someone who can. Post the work and pay for it — that's how a city gets things done (발주).",
                TutorialStep.Return => "You came back. Most of the story is just that.",
                TutorialStep.Complete => "There's always more work. That's the good news and the bad.",
                _ => "Coin for goods, goods for coin. Come see me when you have either.",
            },
        };
    }
}
