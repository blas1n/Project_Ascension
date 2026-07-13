using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectAscension.GameSimulation.Contracts;
using ProjectAscension.Net;

namespace ProjectAscension.Game
{
    /// <summary>
    /// The 게시판 (contract board) — CONTRACTS ONLY: browse what's on offer, accept one, track the
    /// active contract, and turn it in. Issuing a contract (발주) and delegating one you can't finish
    /// both belong to a PERSON, not this board (docs/02-systems/contract-market-system.md's "권한과
    /// 접근의 분리" — see <see cref="ContractClerkPanel"/>, the Contract Clerk). The board used to be
    /// where all of that lived at once; splitting it out is the whole point of this pass — a reviewer
    /// should be able to find "accept a contract" by looking at the file named for the board.
    /// </summary>
    public sealed class ContractBoardPanel : CityStationPanel
    {
        private bool _busy;
        private string _txMessage = "";
        private CatalogApiClient _api;

        private void Start()
        {
            if (CityBlockout.BoardInteractable != null)
                CityBlockout.BoardInteractable.Interacted += Toggle;
        }

        protected override void OnDestroy()
        {
            if (CityBlockout.BoardInteractable != null)
                CityBlockout.BoardInteractable.Interacted -= Toggle;
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

            var contracts = session.Contracts;
            var ps = session.PlayerState;

            var o = ModalOrigin(420f, 560f);
            GUILayout.BeginArea(new Rect(o.x, o.y, 420f, 560f), GUI.skin.box);
            GUILayout.Label($"CONTRACT BOARD      Gold: {ps.Currency}    Rep: {ps.Reputation}");
            GUILayout.Space(8);

            if (contracts.Active == null)
            {
                GUILayout.Label("Available:");
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
                    // Too hard to finish? Delegating it is the Contract Clerk's business now, not a
                    // button here — see ContractClerkPanel. Abandoning stays here: it's a local
                    // decision about what's on THIS board, not a market transaction.
                    if (c.DelegationAllowed && serverBacked && session.SuggestDelegation)
                        GUILayout.Label("You fell in battle. Too hard? The clerk can delegate it for you.");
                    if (GUILayout.Button("Abandon"))
                        contracts.Abandon();
                }
            }

            // Stub contractor status (the delegation payoff) — read-only, so it belongs on the board
            // even though delegating itself now happens at the clerk.
            foreach (var d in contracts.InProgress)
                GUILayout.Label($"Contractor working: {d.Contract.Title}  ({Mathf.CeilToInt(d.Remaining)}s)");
            foreach (var title in contracts.ContractorCompleted)
                GUILayout.Label($"✓ Contractor completed your delegated contract: {title}");
            foreach (var msg in contracts.FailedRecently)
                GUILayout.Label($"✗ Contract failed: {msg}");
            if (!string.IsNullOrEmpty(_txMessage))
                GUILayout.Label(_txMessage);

            GUILayout.EndArea();
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
    }
}
