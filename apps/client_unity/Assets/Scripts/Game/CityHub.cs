using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using ProjectAscension.Domain.Enums;
using ProjectAscension.Equipment;
using ProjectAscension.GameSimulation.Combat;
using ProjectAscension.GameSimulation.Contracts;
using ProjectAscension.Net;
using ProjectAscension.Player;

namespace ProjectAscension.Game
{
    /// <summary>
    /// City hub UI (dev OnGUI): contract board, loadout selection, turn-in, depart, and a
    /// contract-issuing panel. The cursor unlocks (and gameplay input disables — BUG 3, UiFocus)
    /// while a panel is open, so the buttons are clickable and typing/clicking never also drives
    /// the player.
    /// </summary>
    public sealed class CityHub : MonoBehaviour
    {
        private const string IssuerActorId = "11111111-1111-1111-1111-111111111111";
        private const float DelegationSeconds = 20f; // stub contractor finishes a delegated contract in this time
        private static readonly string[] TargetKeys = { "", "melee", "ranged", "elite" };

        // Scroll positions for the discovery journal and knowledge market (they grow long).
        private Vector2 _discoveryScroll;
        private Vector2 _marketScroll;

        // Contract-issuing panel state. The player picks the objective; the server quotes a
        // fair reward + band (so it's a choice, not balance math); the player tunes generosity.
        private ContractPurpose _iPurpose = ContractPurpose.Hunt;
        private int _iTargetIdx;   // index into TargetKeys (Hunt only)
        private int _iCount = 1;
        private int _iReward;
        private ContractQuoteDto _quote;
        private bool _busy;
        private CatalogApiClient _api;

        // The last economy transaction's outcome (ADR 0014) — the server's OWN reason on a
        // rejection, never a local guess. Shown near whichever panel triggered it.
        private string _txMessage = "";

        // The board and the quartermaster open their own panels — both press-[F] actions, not
        // proximity. Both still free the cursor (and gameplay input — UiFocus) the same way the
        // old "_busyHere" did.
        private bool _boardOpen;
        private bool _quartermasterOpen;
        private bool _focusHeld; // whether WE currently hold the UiFocus gate (Push/Pop exactly once)

        // The city is a place, so its business is a THING YOU DO — press [F] at the board or the
        // quartermaster — not a panel that opens the moment you happen to be standing nearby. F again
        // (or Esc) closes whatever is open, same as walking away used to.
        private void Start()
        {
            if (CityBlockout.BoardInteractable != null)
                CityBlockout.BoardInteractable.Interacted += OnBoardInteracted;
            if (CityNpc.QuartermasterInteractable != null)
                CityNpc.QuartermasterInteractable.Interacted += OnQuartermasterInteracted;
        }

        private void OnDestroy()
        {
            if (CityBlockout.BoardInteractable != null)
                CityBlockout.BoardInteractable.Interacted -= OnBoardInteracted;
            if (CityNpc.QuartermasterInteractable != null)
                CityNpc.QuartermasterInteractable.Interacted -= OnQuartermasterInteracted;

            // "Depart to Frontier" is a button INSIDE the board panel (see OnGUI) — the scene
            // unloads (destroying this) while _boardOpen is still true, never having gone through
            // ApplyCursor's close path. An unmatched Push would leave gameplay input disabled for
            // the entire Frontier scene, so release it here regardless of how we got destroyed.
            if (_focusHeld) { UiFocus.Pop(); _focusHeld = false; }
        }

        private void OnBoardInteracted()
        {
            _boardOpen = !_boardOpen;
            ApplyCursor();
        }

        private void OnQuartermasterInteracted()
        {
            _quartermasterOpen = !_quartermasterOpen;
            ApplyCursor();
        }

        private void ApplyCursor()
        {
            bool busy = _boardOpen || _quartermasterOpen;
            if (busy && !_focusHeld) { UiFocus.Push(); _focusHeld = true; }
            else if (!busy && _focusHeld) { UiFocus.Pop(); _focusHeld = false; }
        }

        private void Update()
        {
            // Esc is the universal "back out" — closes whatever city panel is open, same as walking
            // away used to when this was proximity-driven.
            if ((_boardOpen || _quartermasterOpen) &&
                Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                _boardOpen = false;
                _quartermasterOpen = false;
                ApplyCursor();
            }
        }

        private void OnGUI()
        {
            // Nothing open — just a hint of where to go and what to press.
            if (!_boardOpen && !_quartermasterOpen)
            {
                var style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 13 };
                GUI.Label(new Rect((Screen.width - 400f) * 0.5f, Screen.height - 92f, 400f, 20f),
                    "The contract board is in the square.", style);
                return;
            }

            var session = GameSession.Instance;
            if (session == null)
            {
                GUI.Label(new Rect(20, 20, 500, 20), "No GameSession — start play from the Bootstrap scene.");
                return;
            }

            var contracts = session.Contracts;
            var ps = session.PlayerState;

            GUILayout.BeginArea(new Rect(20, 20, 400, 620), GUI.skin.box);
            GUILayout.Label($"CITY      Gold: {ps.Currency}    Rep: {ps.Reputation}");
            if (ps.Resources.Count > 0)
            {
                var parts = new List<string>();
                foreach (var kv in ps.Resources) parts.Add($"{kv.Key} x{kv.Value}");
                GUILayout.Label($"Materials: {string.Join(", ", parts)}");
            }
            GUILayout.Space(8);

            if (session.Npcs.Count > 0)
            {
                GUILayout.Label("City staff:");
                foreach (var n in session.Npcs)
                    GUILayout.Label($"  {n.name} ({n.role}): \"{NpcReaction(n.role, ps.Reputation)}\"");
                GUILayout.Space(6);
            }

            GUILayout.Label("Loadout (chosen from inventory):");
            // Left hand fires on RMB, right hand on LMB (PlayerCombat) — label it so the combo
            // guide's "LMB/RMB" is unambiguous.
            DrawWeaponSelector("Left (RMB) ", ps.SelectedLeft, ps.SetLeft, ps.OwnedWeapons);
            DrawWeaponSelector("Right (LMB)", ps.SelectedRight, ps.SetRight, ps.OwnedWeapons);

            // Ability bar: bind discovered commands to the Q/E/R/F hotkeys. Always shown, so the
            // player can see the slots even before discovering any command.
            var commands = session.DiscoveredSkills.Commands;
            session.EnsureDefaultCommandSlots();
            GUILayout.Space(4);
            GUILayout.Label("Ability slots (hotkeys):");
            if (commands.Count == 0)
                GUILayout.Label("  (none yet — discover a non-magic combat skill)");
            else
                for (int i = 0; i < session.CommandSlots.Length; i++)
                    DrawAbilitySlot(i, session, commands);
            GUILayout.Space(10);

            if (contracts.Active == null)
            {
                GUILayout.Label("Contract Board:");
                ContractInstance toAccept = null;
                foreach (var c in contracts.Available)
                {
                    GUILayout.BeginHorizontal();
                    string rep = c.RewardReputation > 0 ? $", +{c.RewardReputation} rep" : "";
                    string by = string.IsNullOrEmpty(c.Issuer) ? "" : $" — by {c.Issuer}";
                    GUILayout.Label($"{c.Title}  ({c.Purpose}, +{c.RewardCurrency}g{rep}){by}");
                    if (ContractService.CanAccept(c, ps.Reputation))
                    {
                        if (GUILayout.Button("Accept", GUILayout.Width(70)))
                            toAccept = c;
                    }
                    else
                    {
                        GUILayout.Label($"needs {c.MinReputation} rep", GUILayout.Width(90));
                    }
                    GUILayout.EndHorizontal();
                }
                if (toAccept != null)
                {
                    // A server-backed contract must be assigned server-side first (so a later
                    // turn-in/delegate can be validated there, ADR 0014); a local-only board entry
                    // (offline defaults, Id == Guid.Empty) has nothing to call.
                    if (!string.IsNullOrWhiteSpace(session.ServerUrl) && toAccept.Id != System.Guid.Empty)
                        StartCoroutine(DoAccept(session, toAccept));
                    else
                        contracts.Accept(toAccept);
                }
            }
            else
            {
                var c = contracts.Active;
                string clock = c.FailOnTimeout ? $"   ⏱ {Mathf.CeilToInt(c.Remaining)}s left" : "";
                GUILayout.Label($"Active: {c.Title}   {c.Progress}/{c.TargetCount}{clock}");
                GUILayout.Label(c.Description);
                if (c.FailOnTimeout || c.FailOnDeath)
                {
                    var conds = new List<string>();
                    if (c.FailOnTimeout) conds.Add("timeout");
                    if (c.FailOnDeath) conds.Add("death");
                    GUILayout.Label($"Fails on: {string.Join(", ", conds)}");
                }
                bool serverBacked = !string.IsNullOrWhiteSpace(session.ServerUrl) && c.Id != System.Guid.Empty;
                if (c.IsComplete)
                {
                    string rep = c.RewardReputation > 0 ? $", +{c.RewardReputation} rep" : "";
                    string item = c.RewardItemAmount > 0 && !string.IsNullOrEmpty(c.RewardItemKey)
                        ? $", {c.RewardItemKey}" : "";
                    if (serverBacked)
                    {
                        GUI.enabled = !_busy;
                        if (GUILayout.Button($"Turn In  (+{c.RewardCurrency}g{rep}{item})", GUILayout.Height(28)))
                            StartCoroutine(DoTurnIn(session, c));
                        GUI.enabled = true;
                    }
                    else
                    {
                        GUILayout.Label("Complete — offline, needs the server to claim the reward.");
                    }
                }
                else
                {
                    // Delegation tutorial: a too-hard contract can be handed to a contractor
                    // instead of cleared alone. The hint appears after a death.
                    if (c.DelegationAllowed && serverBacked)
                    {
                        if (session.SuggestDelegation)
                            GUILayout.Label("You fell in battle. Too hard? Delegate it — a contractor will handle it.");
                        bool affordable = ps.Currency >= c.RewardCurrency;
                        GUI.enabled = affordable && !_busy;
                        if (GUILayout.Button(affordable ? $"Delegate  (-{c.RewardCurrency}g)" : "Delegate  (not enough gold)", GUILayout.Height(28)))
                            StartCoroutine(DoDelegate(session, c));
                        GUI.enabled = true;
                    }
                    if (GUILayout.Button("Abandon"))
                        contracts.Abandon();
                }
            }

            // Stub contractor status (the delegation payoff).
            foreach (var d in contracts.InProgress)
                GUILayout.Label($"Contractor working: {d.Contract.Title}  ({Mathf.CeilToInt(d.Remaining)}s)");
            foreach (var title in contracts.ContractorCompleted)
                GUILayout.Label($"✓ Contractor completed your delegated contract: {title}");
            foreach (var msg in contracts.FailedRecently)
                GUILayout.Label($"✗ Contract failed: {msg}");
            if (!string.IsNullOrEmpty(_txMessage))
                GUILayout.Label(_txMessage);

            GUILayout.Space(12);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Save", GUILayout.Height(34), GUILayout.Width(80)))
                session.Save();
            if (GUILayout.Button("Depart to Frontier", GUILayout.Height(34)))
            {
                session.Save(); // persist progress before leaving
                GameScenes.LoadFrontier();
            }
            GUILayout.EndHorizontal();

            GUILayout.EndArea();

            // Discovery journal.
            GUILayout.BeginArea(new Rect(440, 20, 360, 360), GUI.skin.box);
            int discoveredCount = session.DiscoveredSkills.Weapons.Count
                + session.DiscoveredSkills.Commands.Count + session.DiscoveredSkills.Passives.Count;
            GUILayout.Label($"DISCOVERIES ({discoveredCount})");
            GUILayout.Space(4);
            bool any = false;
            // Show each discovered skill WITH how to use it (weapon/command hotkey/passive) and
            // a short EFFECT summary. Scrollable — the list grows well past the panel.
            _discoveryScroll = GUILayout.BeginScrollView(_discoveryScroll, GUILayout.Height(170));
            foreach (var d in session.DiscoveredSkills.All)
            {
                string hint = d.Manifestation == ManifestationKind.Command
                    ? CommandHint(d, session)
                    : UseHint(d);
                GUILayout.Label($"• {d.Name}  [{hint}]");
                // The AI-composed description (a sentence, like a real game's skill text).
                string desc = !string.IsNullOrWhiteSpace(d.Description)
                    ? d.Description
                    : SkillSummary.Describe(d); // fallback if the model gave none (graph-derived)
                GUILayout.Label($"     {desc}");
                any = true;
            }
            if (!any)
                GUILayout.Label("None yet — fight and explore to discover.");
            GUILayout.EndScrollView();

            GUILayout.Space(8);
            GUILayout.Label("KNOWLEDGE MARKET (지식 거래)");
            bool marketOnline = !string.IsNullOrWhiteSpace(session.ServerUrl);
            bool anySellable = false;
            _marketScroll = GUILayout.BeginScrollView(_marketScroll, GUILayout.Height(120));
            foreach (var discovered in session.DiscoveredSkills.All)
            {
                if (ps.SoldKnowledge.Contains(discovered.Name)) continue;
                if (discovered.DiscoveryId == System.Guid.Empty) continue; // no server-backed discovery to license
                anySellable = true;
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{discovered.Name}", GUILayout.Width(150));
                // Price/reputation are server-computed (DB-driven tuning, ADR 0014) — the client
                // shows no pre-sale estimate so it can never drift from the authoritative one.
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

            // 발주 is the quartermaster's suggestion to make (stage 10) — not a window that is simply
            // always open. Press [F] on him and it's there.
            if (_quartermasterOpen) DrawIssuePanel(session, ps);
            DrawShop(session, ps);
            DrawSettlement(session, ps);
        }

        // Frontier outpost (정착지): deliver monster-drop materials to mature infrastructure
        // and advance the settlement's civilization stage. Server-persistent.
        private void DrawSettlement(GameSession session, PlayerStateService ps)
        {
            GUILayout.BeginArea(new Rect(440, 390, 360, 250), GUI.skin.box);
            GUILayout.Label("FRONTIER OUTPOST (정착지)");
            var s = session.Settlement;
            if (s == null)
            {
                GUILayout.Label(string.IsNullOrWhiteSpace(session.ServerUrl) ? "Offline — needs the server." : "Loading…");
                GUILayout.EndArea();
                return;
            }

            GUILayout.Label($"{s.name} — [{s.stage}]  (maturity {s.totalLevel}/12)");
            GUILayout.Label($"Shelter L{s.shelterLevel}/4   Market L{s.marketLevel}/4   Defense L{s.defenseLevel}/4");
            GUILayout.Space(4);
            GUILayout.Label("Deliver materials to develop:");
            DeliverRow(session, ps, "hide", "Shelter");
            DeliverRow(session, ps, "feather", "Market");
            DeliverRow(session, ps, "core", "Defense");
            GUILayout.EndArea();
        }

        private void DeliverRow(GameSession session, PlayerStateService ps, string key, string track)
        {
            ps.Resources.TryGetValue(key, out var have);
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{key} (x{have}) → {track}", GUILayout.Width(180));
            GUI.enabled = !_busy && have > 0;
            if (GUILayout.Button("Deliver 5", GUILayout.Width(90)))
            {
                int amount = Mathf.Min(5, have);
                if (ps.SpendResource(key, amount)) StartCoroutine(Deliver(session, key, amount));
            }
            GUI.enabled = true;
            GUILayout.EndHorizontal();
        }

        private IEnumerator Deliver(GameSession session, string key, int amount)
        {
            _busy = true;
            _api ??= new CatalogApiClient(session.ServerUrl);
            yield return _api.DeliverResource(new DeliverResourceDto { itemKey = key, amount = amount },
                dto => session.SetSettlement(dto));
            _busy = false;
        }

        // City shop: sell monster-drop materials for gold, or buy materials (for settlement
        // supply). Prices are DB-driven (fetched into GameSession.ShopItems).
        private void DrawShop(GameSession session, PlayerStateService ps)
        {
            GUILayout.BeginArea(new Rect(820, 390, 380, 250), GUI.skin.box);
            GUILayout.Label("SHOP (materials)");
            if (session.ShopItems.Count == 0)
            {
                GUILayout.Label(string.IsNullOrWhiteSpace(session.ServerUrl) ? "Offline — needs the server." : "No items.");
                GUILayout.EndArea();
                return;
            }

            foreach (var item in session.ShopItems)
            {
                ps.Resources.TryGetValue(item.key, out var have);
                string key = item.key; // captured per-iteration for the coroutine closures below
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{item.displayName} (x{have})", GUILayout.Width(150));
                GUI.enabled = item.sellPrice > 0 && have > 0 && !_busy;
                if (GUILayout.Button($"Sell +{item.sellPrice}g", GUILayout.Width(95)))
                    StartCoroutine(DoSell(session, key));
                GUI.enabled = item.buyPrice > 0 && ps.Currency >= item.buyPrice && !_busy;
                if (GUILayout.Button($"Buy -{item.buyPrice}g", GUILayout.Width(95)))
                    StartCoroutine(DoBuy(session, key));
                GUI.enabled = true;
                GUILayout.EndHorizontal();
                if (!string.IsNullOrEmpty(item.description))
                    GUILayout.Label($"   {item.description}");
            }
            if (!string.IsNullOrEmpty(_txMessage))
                GUILayout.Label(_txMessage);
            GUILayout.EndArea();
        }

        // Player-issued contract panel. The player chooses what (purpose / target / count)
        // and how generous; the server calibrates the reward + band and writes the copy.
        private void DrawIssuePanel(GameSession session, PlayerStateService ps)
        {
            GUILayout.BeginArea(new Rect(820, 20, 380, 360), GUI.skin.box);
            GUILayout.Label("ISSUE A CONTRACT (발주)");

            if (string.IsNullOrWhiteSpace(session.ServerUrl))
            {
                GUILayout.Label("Offline — needs the server (set GameSession.serverUrl).");
                GUILayout.EndArea();
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
                GUILayout.EndArea();
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

            GUILayout.EndArea();
        }

        // How the player uses a discovered skill — the guide text shown in the DISCOVERIES panel.
        private static string UseHint(DiscoveredSkill d) => d.Manifestation switch
        {
            ManifestationKind.Weapon => "weapon: equip & fire",
            ManifestationKind.Passive => $"passive: {SkillSummary.DescribePassive(d)}",
            _ => "command",
        };

        // A command is cast from the ability hotkey the player bound it to; if it's weapon-bound
        // it also shows the equipment it needs (ADR 0005 재개정), so the player knows what to
        // equip before departing.
        private static string CommandHint(DiscoveredSkill d, GameSession session)
        {
            int slot = session.SlotOf(d);
            var required = CommandGate.RequiredEquipment(d);
            string reqTxt = required.Count > 0 ? $"  (needs {string.Join("/", required)})" : "";
            return slot >= 0 ? $"key [{AbilitySlots.SlotLabel(slot)}]{reqTxt}" : $"unassigned{reqTxt}";
        }

        // NPCs react to the player's standing (the slice's "명성 — NPC 반응 변화").
        private static string NpcReaction(string role, int reputation)
        {
            int tier = ProjectAscension.GameSimulation.Player.ReputationTier.Of(reputation);
            switch (role)
            {
                case "Guard": return tier == 2 ? "An honor to have you, Warden." : tier == 1 ? "Stay sharp out there." : "State your business.";
                case "Shopkeeper": return tier == 2 ? "For you — only the finest." : tier == 1 ? "Good to see a regular." : "Coin first, talk later.";
                case "Contract Clerk": return tier == 2 ? "The board is yours to pick from." : tier == 1 ? "More work? I have plenty." : "Fill out the form like everyone else.";
                default: return tier == 2 ? "Your name carries weight here." : tier == 1 ? "I have heard of you." : "Another newcomer.";
            }
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

        // --- Server-authoritative economy transactions (ADR 0014) --------------

        private IEnumerator DoAccept(GameSession session, ContractInstance template)
        {
            _busy = true;
            _api ??= new CatalogApiClient(session.ServerUrl);
            bool ok = false;
            yield return _api.AcceptContract(template.Id.ToString(), session.ActorId,
                dto => ok = dto != null,
                error => _txMessage = "Accept failed: " + CatalogApiClient.ParseErrorMessage(error));
            if (ok)
            {
                session.Contracts.Accept(template);
                _txMessage = "";
            }
            _busy = false;
        }

        private IEnumerator DoTurnIn(GameSession session, ContractInstance c)
        {
            _busy = true;
            _api ??= new CatalogApiClient(session.ServerUrl);
            string id = c.Id.ToString();

            // Report the tracked progress first — kill/objective credit is still client-reported
            // (no server combat simulation yet); TurnInContract checks THIS stored count, so the
            // payout itself stays server-computed regardless.
            yield return _api.UpdateContractProgress(id, session.ActorId, c.Progress, _ => { });

            ContractTurnInResponseDto response = null;
            string failure = null;
            yield return _api.TurnInContract(id, session.ActorId,
                r => response = r,
                error => failure = CatalogApiClient.ParseErrorMessage(error));

            if (response != null)
            {
                session.ApplyPlayerState(response.playerState);
                // An item reward is a POSSESSION, not a payout — the survey hands over a map. The
                // key/amount come from the server's OWN contract record, never invented locally.
                var reward = response.contract;
                if (reward != null && reward.rewardItemAmount > 0 && !string.IsNullOrEmpty(reward.rewardItemKey))
                {
                    session.PlayerState.Inventory.Add(reward.rewardItemKey, reward.rewardItemAmount);
                    if (TutorialRunner.Instance != null)
                        TutorialRunner.Instance.Signal(GameSimulation.Tutorial.TutorialSignal.MapReceived);
                }
                session.Contracts.ClearActiveAfterServerTurnIn();
                _txMessage = "";
            }
            else
            {
                _txMessage = "Turn-in failed: " + (failure ?? "unknown error");
            }
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

        private IEnumerator DoBuy(GameSession session, string itemKey)
        {
            _busy = true;
            _api ??= new CatalogApiClient(session.ServerUrl);
            PlayerStateDto response = null;
            yield return _api.BuyItem(itemKey, 1,
                r => response = r,
                error => _txMessage = "Buy failed: " + CatalogApiClient.ParseErrorMessage(error));
            if (response != null)
            {
                session.ApplyPlayerState(response);
                _txMessage = "";
            }
            _busy = false;
        }

        private IEnumerator DoSell(GameSession session, string itemKey)
        {
            _busy = true;
            _api ??= new CatalogApiClient(session.ServerUrl);
            PlayerStateDto response = null;
            yield return _api.SellItem(itemKey, 1,
                r => response = r,
                error => _txMessage = "Sell failed: " + CatalogApiClient.ParseErrorMessage(error));
            if (response != null)
            {
                session.ApplyPlayerState(response);
                _txMessage = "";
            }
            _busy = false;
        }

        private static void DrawWeaponSelector(string label, WeaponData current,
            System.Action<WeaponData> set, IReadOnlyList<WeaponData> owned)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{label}: {(current != null ? current.DisplayName : "-")}", GUILayout.Width(170));
            if (GUILayout.Button("<", GUILayout.Width(30))) set(Cycle(owned, current, -1));
            if (GUILayout.Button(">", GUILayout.Width(30))) set(Cycle(owned, current, +1));
            GUILayout.EndHorizontal();
        }

        private static WeaponData Cycle(IReadOnlyList<WeaponData> owned, WeaponData current, int dir)
        {
            if (owned.Count == 0) return current;
            int index = 0;
            for (int i = 0; i < owned.Count; i++)
                if (owned[i] == current) { index = i; break; }
            index = (index + dir + owned.Count) % owned.Count;
            return owned[index];
        }

        private static void DrawAbilitySlot(int index, GameSession session, IReadOnlyList<DiscoveredSkill> commands)
        {
            var current = session.CommandSlots[index];
            var required = current != null ? CommandGate.RequiredEquipment(current) : System.Array.Empty<string>();
            string req = required.Count > 0 ? "  needs " + string.Join("/", required) : "";
            GUILayout.BeginHorizontal();
            GUILayout.Label($"[{AbilitySlots.SlotLabel(index)}] {(current != null ? current.Name : "(none)")}{req}", GUILayout.Width(260));
            if (GUILayout.Button("<", GUILayout.Width(30))) session.AssignCommandSlot(index, CycleCommand(commands, current, -1));
            if (GUILayout.Button(">", GUILayout.Width(30))) session.AssignCommandSlot(index, CycleCommand(commands, current, +1));
            GUILayout.EndHorizontal();
        }

        // Cycle through the commands plus a "(none)" entry at position 0.
        private static DiscoveredSkill CycleCommand(IReadOnlyList<DiscoveredSkill> commands, DiscoveredSkill current, int dir)
        {
            int n = commands.Count + 1;
            int idx = 0; // 0 = none
            for (int i = 0; i < commands.Count; i++)
                if (ReferenceEquals(commands[i], current)) { idx = i + 1; break; }
            idx = (idx + dir + n) % n;
            return idx == 0 ? null : commands[idx - 1];
        }
    }
}
