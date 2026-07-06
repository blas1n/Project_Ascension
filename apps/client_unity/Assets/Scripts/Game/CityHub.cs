using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectAscension.Domain.Enums;
using ProjectAscension.Equipment;
using ProjectAscension.GameSimulation.Combat;
using ProjectAscension.Net;

namespace ProjectAscension.Game
{
    /// <summary>
    /// City hub UI (dev OnGUI): contract board, loadout selection, turn-in, depart, and a
    /// contract-issuing panel. Cursor is unlocked here so the buttons are clickable.
    /// </summary>
    public sealed class CityHub : MonoBehaviour
    {
        private const string IssuerActorId = "11111111-1111-1111-1111-111111111111";
        private const float DelegationSeconds = 20f; // stub contractor finishes a delegated contract in this time
        private const int KnowledgeGoldPerPoint = 6;  // knowledge license price per power point
        private const int KnowledgePointsPerRep = 5;  // power per standing point from a license sale
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

        private void OnEnable()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void OnGUI()
        {
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

            // Ability bar: bind discovered commands to the Q/E/R/F hotkeys. Equipment-locked
            // commands still show here (you equip the weapon to use them, ADR 0005 재개정).
            var commands = session.DiscoveredSkills.Commands;
            if (commands.Count > 0)
            {
                session.EnsureDefaultCommandSlots();
                GUILayout.Space(4);
                GUILayout.Label("Ability slots (hotkeys):");
                for (int i = 0; i < session.CommandSlots.Length; i++)
                    DrawAbilitySlot(i, session, commands);
            }
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
                    contracts.Accept(toAccept);
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
                if (c.IsComplete)
                {
                    string rep = c.RewardReputation > 0 ? $", +{c.RewardReputation} rep" : "";
                    if (GUILayout.Button($"Turn In  (+{c.RewardCurrency}g{rep})", GUILayout.Height(28)))
                    {
                        var r = contracts.TurnIn();
                        ps.Currency += r.Currency;
                        ps.Reputation += r.Reputation;
                    }
                }
                else
                {
                    // Delegation tutorial: a too-hard contract can be handed to a contractor
                    // instead of cleared alone. The hint appears after a death.
                    if (c.DelegationAllowed)
                    {
                        if (session.SuggestDelegation)
                            GUILayout.Label("You fell in battle. Too hard? Delegate it — a contractor will handle it.");
                        bool affordable = ps.Currency >= c.RewardCurrency;
                        GUI.enabled = affordable;
                        if (GUILayout.Button(affordable ? $"Delegate  (-{c.RewardCurrency}g)" : "Delegate  (not enough gold)", GUILayout.Height(28)))
                        {
                            ps.Currency = Mathf.Max(0, ps.Currency - c.RewardCurrency); // escrow the contractor's pay
                            contracts.DelegateActive(DelegationSeconds);
                            session.SuggestDelegation = false;
                        }
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
            GUILayout.Label($"DISCOVERIES ({session.Discovery.DiscoveredCount})");
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
                string effect = d.Manifestation == ManifestationKind.Passive
                    ? SkillSummary.DescribePassive(d.Skill)
                    : SkillSummary.Describe(d.Skill);
                GUILayout.Label($"• {d.Name}  [{hint}]  {effect}");
                any = true;
            }
            if (!any) // fall back to the journal titles if the skill set isn't populated yet
                foreach (var discovery in session.Discovery.DiscoveredCandidates())
                {
                    GUILayout.Label($"• {discovery.Title}");
                    any = true;
                }
            if (!any)
                GUILayout.Label("None yet — fight and explore to discover.");
            GUILayout.EndScrollView();

            GUILayout.Space(8);
            GUILayout.Label("KNOWLEDGE MARKET (지식 거래)");
            bool anySellable = false;
            _marketScroll = GUILayout.BeginScrollView(_marketScroll, GUILayout.Height(120));
            foreach (var discovered in session.DiscoveredSkills.All)
            {
                if (ps.SoldKnowledge.Contains(discovered.Name)) continue;
                anySellable = true;
                int price = KnowledgeValuation.LicensePrice(discovered.Skill, KnowledgeGoldPerPoint);
                int rep = KnowledgeValuation.LicenseReputation(discovered.Skill, KnowledgePointsPerRep);
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{discovered.Name}", GUILayout.Width(150));
                if (GUILayout.Button($"Sell +{price}g +{rep}rep", GUILayout.Width(150)))
                {
                    ps.Currency += price;
                    ps.Reputation += rep;
                    ps.SoldKnowledge.Add(discovered.Name);
                }
                GUILayout.EndHorizontal();
            }
            if (!anySellable)
                GUILayout.Label("No unsold knowledge — discover skills to license.");
            GUILayout.EndScrollView();
            GUILayout.EndArea();

            DrawIssuePanel(session, ps);
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
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{item.displayName} (x{have})", GUILayout.Width(150));
                GUI.enabled = item.sellPrice > 0 && have > 0;
                if (GUILayout.Button($"Sell +{item.sellPrice}g", GUILayout.Width(95)))
                {
                    if (ps.SpendResource(item.key, 1)) ps.Currency += item.sellPrice;
                }
                GUI.enabled = item.buyPrice > 0 && ps.Currency >= item.buyPrice;
                if (GUILayout.Button($"Buy -{item.buyPrice}g", GUILayout.Width(95)))
                {
                    ps.Currency -= item.buyPrice;
                    ps.AddResource(item.key, 1);
                }
                GUI.enabled = true;
                GUILayout.EndHorizontal();
                if (!string.IsNullOrEmpty(item.description))
                    GUILayout.Label($"   {item.description}");
            }
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
            ManifestationKind.Passive => "passive: always on",
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
            int tier = reputation >= 30 ? 2 : reputation >= 10 ? 1 : 0;
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
            yield return _api.IssueContract(request, dto =>
            {
                if (dto == null) return;
                var purpose = System.Enum.TryParse<ContractPurpose>(dto.purpose, out var p) ? p : ContractPurpose.Hunt;
                session.Contracts.AddIssued(new ContractInstance
                {
                    Purpose = purpose,
                    Title = dto.title,
                    Description = dto.description,
                    TargetCount = Mathf.Max(1, dto.targetCount),
                    RewardCurrency = dto.rewardCurrency,
                    Target = dto.target,
                });
                ps.Currency = Mathf.Max(0, ps.Currency - dto.rewardCurrency); // escrow the reward
                _quote = null;
            });
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
