using System.Collections;
using UnityEngine;
using ProjectAscension.Net;

namespace ProjectAscension.Game
{
    /// <summary>
    /// Quartermaster Hale's business: he's the Shopkeeper (docs/03's NPC roster), so the shop (sell
    /// monster-drop materials for gold, buy materials for the frontier outpost) and the settlement's
    /// resource delivery are both his — logistics is logistics, whether it's coin over a counter or
    /// materials off your back and into the outpost's stores. Neither belongs on the contract board
    /// or bolted to a menu; you walk up to him and talk business.
    /// </summary>
    public sealed class QuartermasterPanel : CityStationPanel
    {
        private bool _busy;
        private string _txMessage = "";
        private CatalogApiClient _api;

        private void Start()
        {
            if (CityNpc.QuartermasterInteractable != null)
                CityNpc.QuartermasterInteractable.Interacted += Toggle;
        }

        protected override void OnDestroy()
        {
            if (CityNpc.QuartermasterInteractable != null)
                CityNpc.QuartermasterInteractable.Interacted -= Toggle;
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

            DrawShop(session, ps);
            DrawSettlement(session, ps);
        }

        // City shop: sell monster-drop materials for gold, or buy materials (for settlement
        // supply). Prices are DB-driven (fetched into GameSession.ShopItems).
        private void DrawShop(GameSession session, PlayerStateService ps)
        {
            var o = ModalOrigin(820f, 330f);
            GUILayout.BeginArea(new Rect(o.x, o.y, 420f, 320f), GUI.skin.box);
            GUILayout.Label($"SHOP (materials)      Gold: {ps.Currency}");
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

        // Frontier outpost (정착지): deliver monster-drop materials to mature infrastructure
        // and advance the settlement's civilization stage. Server-persistent.
        private void DrawSettlement(GameSession session, PlayerStateService ps)
        {
            var o = ModalOrigin(820f, 330f);
            GUILayout.BeginArea(new Rect(o.x + 440f, o.y, 380f, 250f), GUI.skin.box);
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

        // --- Server-authoritative economy transactions (ADR 0014) --------------

        private IEnumerator Deliver(GameSession session, string key, int amount)
        {
            _busy = true;
            _api ??= new CatalogApiClient(session.ServerUrl);
            yield return _api.DeliverResource(new DeliverResourceDto { itemKey = key, amount = amount },
                dto => session.SetSettlement(dto));
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
    }
}
