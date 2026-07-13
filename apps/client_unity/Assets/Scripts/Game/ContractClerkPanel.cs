using System.Collections;
using UnityEngine;
using ProjectAscension.Domain.Enums;
using ProjectAscension.GameSimulation.Combat;
using ProjectAscension.GameSimulation.Contracts;
using ProjectAscension.Net;

namespace ProjectAscension.Game
{
    /// <summary>
    /// Survey Clerk Mira's business: she's the Contract Clerk (docs/03's NPC roster), and issuing
    /// (발주) and delegating a contract are both paperwork — HER job, not the board's and not the
    /// quartermaster's (docs/02-systems/contract-market-system.md separates 발주 권한 from 계약 시장
    /// 접근: the board is the access point, but who processes the terms is a person). She's also the
    /// first-hour doc's stage 9 "위임" NPC, so the delegation offer for your current contract lives
    /// here too, right next to issuing — the same conversation.
    ///
    /// Knowledge licensing (docs/02-systems/knowledge-economy.md: "지식은 계약 시장을 통해 거래된다" —
    /// knowledge trades THROUGH the contract market) lives here as well, for the same reason issuing
    /// does: it is market paperwork, not a simple accept/turn-in the board handles, and not the
    /// quartermaster's goods-for-coin counter. One clerk, one desk, all of the city's contract and
    /// knowledge administration.
    /// </summary>
    public sealed class ContractClerkPanel : CityStationPanel
    {
        private const string IssuerActorId = "11111111-1111-1111-1111-111111111111";
        private const float DelegationSeconds = 20f; // stub contractor finishes a delegated contract in this time
        private static readonly string[] TargetKeys = { "", "melee", "ranged", "elite" };

        // Contract-issuing state. The player picks the objective; the server quotes a fair reward +
        // band (so it's a choice, not balance math); the player tunes generosity.
        private ContractPurpose _iPurpose = ContractPurpose.Hunt;
        private int _iTargetIdx;   // index into TargetKeys (Hunt only)
        private int _iCount = 1;
        private int _iReward;
        private ContractQuoteDto _quote;
        private bool _busy;
        private CatalogApiClient _api;
        private string _txMessage = "";
        private Vector2 _marketScroll;

        private void Start()
        {
            if (CityNpc.ClerkInteractable != null)
                CityNpc.ClerkInteractable.Interacted += Toggle;
        }

        protected override void OnDestroy()
        {
            if (CityNpc.ClerkInteractable != null)
                CityNpc.ClerkInteractable.Interacted -= Toggle;
            base.OnDestroy();
        }

        protected override void DrawPanel()
        {
            var session = GameSession.Instance;
            if (session == null)
            {
                GUI.Label(new Rect(20, 20, 500, 20), "No GameSession — start play from the Bootstrap scene.");
                return;
            }
            var ps = session.PlayerState;

            var o = ModalOrigin(820f, 560f);
            GUILayout.BeginArea(new Rect(o.x, o.y, 420f, 560f), GUI.skin.box);
            GUILayout.Label($"CONTRACT CLERK — Survey Clerk Mira      Gold: {ps.Currency}");
            GUILayout.Space(6);

            DrawDelegateSection(session, ps);
            DrawIssueSection(session, ps);

            if (!string.IsNullOrEmpty(_txMessage))
                GUILayout.Label(_txMessage);
            GUILayout.EndArea();

            DrawKnowledgeMarket(session, ps);
        }

        // 위임: your current contract, if it's stuck. The doc's stage 9 beat, offered right after
        // death — a too-hard contract can be handed to a contractor instead of cleared alone.
        private void DrawDelegateSection(GameSession session, PlayerStateService ps)
        {
            var c = session.Contracts.Active;
            bool serverBacked = c != null && !string.IsNullOrWhiteSpace(session.ServerUrl) && c.Id != System.Guid.Empty;
            if (c == null || c.IsComplete || !c.DelegationAllowed || !serverBacked) return;

            GUILayout.Label("DELEGATE YOUR ACTIVE CONTRACT (위임)");
            if (session.SuggestDelegation)
                GUILayout.Label("You fell in battle. Too hard? Delegate it — a contractor will handle it.");
            GUILayout.Label($"{c.Title}   {c.Progress}/{c.TargetCount}");
            bool affordable = ps.Currency >= c.RewardCurrency;
            GUI.enabled = affordable && !_busy;
            if (GUILayout.Button(affordable ? $"Delegate  (-{c.RewardCurrency}g)" : "Delegate  (not enough gold)", GUILayout.Height(28)))
                StartCoroutine(DoDelegate(session, c));
            GUI.enabled = true;
            GUILayout.Space(10);
        }

        // Player-issued contract (발주). The player chooses what (purpose / target / count) and how
        // generous; the server calibrates the reward + band and writes the copy.
        private void DrawIssueSection(GameSession session, PlayerStateService ps)
        {
            GUILayout.Label("ISSUE A CONTRACT (발주)");

            if (string.IsNullOrWhiteSpace(session.ServerUrl))
            {
                GUILayout.Label("Offline — needs the server (set GameSession.serverUrl).");
                return;
            }

            GUILayout.Space(4);
            GUILayout.Label("Purpose:");
            GUILayout.BeginHorizontal();
            if (PurposeButton(ContractPurpose.Hunt)) ChangeChoice(session, () => _iPurpose = ContractPurpose.Hunt);
            if (PurposeButton(ContractPurpose.Survey)) ChangeChoice(session, () => _iPurpose = ContractPurpose.Survey);
            if (PurposeButton(ContractPurpose.Collection)) ChangeChoice(session, () => _iPurpose = ContractPurpose.Collection);
            GUILayout.EndHorizontal();

            if (_iPurpose == ContractPurpose.Hunt)
            {
                GUILayout.Label("Target:");
                GUILayout.BeginHorizontal();
                for (int i = 0; i < TargetKeys.Length; i++)
                {
                    string label = i == 0 ? "any" : TargetKeys[i];
                    bool on = _iTargetIdx == i;
                    if (GUILayout.Toggle(on, label) && !on)
                    {
                        int idx = i;
                        ChangeChoice(session, () => _iTargetIdx = idx);
                    }
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label($"Count: {_iCount}", GUILayout.Width(90));
            if (GUILayout.Button("-", GUILayout.Width(28)) && _iCount > 1) ChangeChoice(session, () => _iCount--);
            if (GUILayout.Button("+", GUILayout.Width(28)) && _iCount < 20) ChangeChoice(session, () => _iCount++);
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            if (_quote == null)
            {
                GUILayout.Label(_busy ? "Quoting…" : "Pick an objective for a quote.");
                if (!_busy && GUILayout.Button("Get Quote")) StartCoroutine(RefreshQuote(session));
                return;
            }

            GUILayout.Label($"Suggested: {_quote.suggestedReward}g   (band {_quote.minReward}–{_quote.maxReward})");
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Reward: {_iReward}g", GUILayout.Width(110));
            if (GUILayout.Button("-10", GUILayout.Width(40))) _iReward = Mathf.Max(_quote.minReward, _iReward - 10);
            if (GUILayout.Button("+10", GUILayout.Width(40))) _iReward = Mathf.Min(_quote.maxReward, _iReward + 10);
            GUILayout.EndHorizontal();
            GUILayout.Label(_iReward > _quote.suggestedReward ? "Generous — more attractive to takers." : "Lean — may sit on the board.");

            GUILayout.Space(6);
            bool affordable = ps.Currency >= _iReward;
            GUI.enabled = !_busy && affordable;
            if (GUILayout.Button(affordable ? $"Issue  (-{_iReward}g escrow)" : "Issue  (not enough gold)", GUILayout.Height(30)))
                StartCoroutine(DoIssue(session, ps));
            GUI.enabled = true;
        }

        // 지식 시장 (docs/02-systems/knowledge-economy.md): sell a discovered skill's usage license.
        // Price/reputation are server-computed (DB-driven tuning, ADR 0014) — the client shows no
        // pre-sale estimate so it can never drift from the authoritative one.
        private void DrawKnowledgeMarket(GameSession session, PlayerStateService ps)
        {
            var o = ModalOrigin(820f, 560f);
            GUILayout.BeginArea(new Rect(o.x + 440f, o.y, 380f, 400f), GUI.skin.box);
            GUILayout.Label("KNOWLEDGE MARKET (지식 거래)");
            bool marketOnline = !string.IsNullOrWhiteSpace(session.ServerUrl);
            bool anySellable = false;
            _marketScroll = GUILayout.BeginScrollView(_marketScroll, GUILayout.Height(320));
            foreach (var discovered in session.DiscoveredSkills.All)
            {
                if (ps.SoldKnowledge.Contains(discovered.Name)) continue;
                if (discovered.DiscoveryId == System.Guid.Empty) continue; // no server-backed discovery to license
                anySellable = true;
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{discovered.Name}", GUILayout.Width(150));
                GUI.enabled = marketOnline && !_busy;
                if (GUILayout.Button("License knowledge", GUILayout.Width(150)))
                    StartCoroutine(DoLicense(session, discovered));
                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }
            if (!marketOnline)
                GUILayout.Label("Offline — needs the server to license knowledge.");
            else if (!anySellable)
                GUILayout.Label("No unsold knowledge — discover skills to license.");
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private bool PurposeButton(ContractPurpose purpose)
        {
            bool on = _iPurpose == purpose;
            return GUILayout.Toggle(on, purpose.ToString()) && !on;
        }

        // Apply a choice change and re-quote (clears the stale quote first).
        private void ChangeChoice(GameSession session, System.Action apply)
        {
            apply();
            _quote = null;
            if (!_busy) StartCoroutine(RefreshQuote(session));
        }

        // --- Server-authoritative economy transactions (ADR 0014) --------------

        private IEnumerator RefreshQuote(GameSession session)
        {
            _busy = true;
            _api ??= new CatalogApiClient(session.ServerUrl);
            string target = _iPurpose == ContractPurpose.Hunt ? TargetKeys[_iTargetIdx] : "";
            yield return _api.GetContractQuote(_iPurpose.ToString(), target, _iCount, q =>
            {
                _quote = q;
                if (q != null) _iReward = Mathf.Clamp(_iReward == 0 ? q.suggestedReward : _iReward, q.minReward, q.maxReward);
            });
            _busy = false;
        }

        private IEnumerator DoIssue(GameSession session, PlayerStateService ps)
        {
            _busy = true;
            _api ??= new CatalogApiClient(session.ServerUrl);
            var request = new IssueContractDto
            {
                issuerActorId = IssuerActorId,
                purpose = _iPurpose.ToString(),
                target = _iPurpose == ContractPurpose.Hunt ? TargetKeys[_iTargetIdx] : "",
                targetCount = _iCount,
                desiredReward = _iReward,
                durationHours = 24,
            };
            yield return _api.IssueContract(request, response =>
            {
                if (response?.contract == null) return;
                var dto = response.contract;
                // Same mapping as the board (ContractMapping) — carries the full terms, not just a
                // subset, so a player-issued contract keeps its reputation/deadline/fail conditions.
                session.Contracts.AddIssued(ContractMapping.FromFields(
                    dto.purpose, dto.title, dto.description, dto.targetCount, dto.rewardCurrency, dto.target,
                    dto.issuer, dto.delegationAllowed, dto.rewardReputation, dto.minReputation,
                    dto.timeLimitSeconds, dto.failOnTimeout, dto.failOnDeath,
                    dto.rewardItemKey, dto.rewardItemAmount, dto.id));
                // The escrow was already taken server-side — apply the RETURNED state, don't guess it.
                if (response.playerState != null) session.ApplyPlayerState(response.playerState);
                _quote = null;
                _txMessage = "";
            },
            error => _txMessage = "Issue failed: " + CatalogApiClient.ParseErrorMessage(error));
            _busy = false;
        }

        private IEnumerator DoDelegate(GameSession session, ContractInstance c)
        {
            _busy = true;
            _api ??= new CatalogApiClient(session.ServerUrl);
            PlayerStateDto response = null;
            yield return _api.DelegateContract(c.Id.ToString(), session.ActorId,
                r => response = r,
                error => _txMessage = "Delegate failed: " + CatalogApiClient.ParseErrorMessage(error));
            if (response != null)
            {
                session.ApplyPlayerState(response);
                session.Contracts.DelegateActive(DelegationSeconds);
                session.SuggestDelegation = false;
                _txMessage = "";
            }
            _busy = false;
        }

        private IEnumerator DoLicense(GameSession session, DiscoveredSkill discovered)
        {
            _busy = true;
            _api ??= new CatalogApiClient(session.ServerUrl);
            int beforeCurrency = session.PlayerState.Currency;
            int beforeReputation = session.PlayerState.Reputation;
            PlayerStateDto response = null;
            yield return _api.LicenseKnowledge(session.ActorId, discovered.DiscoveryId.ToString(),
                r => response = r,
                error => _txMessage = "License failed: " + CatalogApiClient.ParseErrorMessage(error));
            if (response != null)
            {
                session.ApplyPlayerState(response);
                session.PlayerState.SoldKnowledge.Add(discovered.Name); // UI cache only — the server flag is authoritative
                _txMessage = $"Licensed '{discovered.Name}' for +{response.currency - beforeCurrency}g +{response.reputation - beforeReputation}rep";
            }
            _busy = false;
        }
    }
}
