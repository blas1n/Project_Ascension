using UnityEngine;
using ProjectAscension.Combat;
using ProjectAscension.World;

namespace ProjectAscension.Game
{
    /// <summary>
    /// Builds the starting city procedurally at load — no authored art needed yet (the art track swaps
    /// meshes into these seams later). The city has to be a PLACE, not a menu: the first hour has the
    /// player walk to the training ground, the board, the armoury, and the people
    /// (docs/03-gameplay/first-hour-experience.md), and stage 1 requires a city to actually have
    /// 훈련장 / 게시판 / 안전 구역 (stage 3's "첫 장비 선택" needs somewhere to happen too — the armoury).
    ///
    /// Styled per docs/05-art/art-direction.md — the south-east civilisation (폭포 초원도시): stone
    /// masonry, a windmill, green/sky-blue/white, silhouette and readability over detail. Deterministic
    /// (fixed seed) so the city is the same place every run.
    /// </summary>
    public sealed class CityBlockout : MonoBehaviour
    {
        // South-east palette: 녹색, 하늘색, 흰색 + stone.
        private static readonly Color Grass = new Color(0.42f, 0.62f, 0.35f);
        private static readonly Color Stone = new Color(0.86f, 0.86f, 0.82f);
        private static readonly Color StoneDark = new Color(0.62f, 0.63f, 0.62f);
        private static readonly Color Roof = new Color(0.35f, 0.55f, 0.72f);   // slate/sky
        private static readonly Color Timber = new Color(0.55f, 0.42f, 0.30f);
        private static readonly Color BoardWood = new Color(0.48f, 0.36f, 0.24f);
        private static readonly Color SafeLine = new Color(0.55f, 0.85f, 1f);
        private static readonly Color RackMetal = new Color(0.7f, 0.72f, 0.76f);

        /// <summary>Where the player should start — inside the safe zone, facing the board.</summary>
        public static readonly Vector3 PlayerSpawn = new Vector3(0f, 0f, -6f);
        /// <summary>The training ground's centre — walk here to learn the verbs.</summary>
        public static readonly Vector3 TrainingGround = new Vector3(-16f, 0f, 6f);
        /// <summary>The contract board (게시판).</summary>
        public static readonly Vector3 BoardSpot = new Vector3(0f, 0f, 4f);

        /// <summary>The armoury rack (stage 3's "첫 장비 선택") — its own station, away from the
        /// board and the NPCs so a player walking the plaza can tell them apart at a glance.</summary>
        public static readonly Vector3 EquipmentSpot = new Vector3(6f, 0f, -6f);

        /// <summary>How close to the board you must stand to press [F] and read it — a board is
        /// legible from further away than a lootable, hence its own (larger) reach.</summary>
        public const float BoardReach = 4.5f;

        /// <summary>How close to the armoury you must stand to press [F] — a workbench-scale reach,
        /// same idea as the board's but for something you stand right at.</summary>
        public const float EquipmentReach = 3.5f;

        /// <summary>The board's interactable, so ContractBoardPanel can subscribe to it without
        /// polling distance itself. Set once, when the city is built; there is exactly one board in
        /// the slice.</summary>
        public static Interactable BoardInteractable { get; private set; }

        /// <summary>The armoury's interactable, so EquipmentStationPanel can subscribe to it without
        /// polling distance itself. Set once, when the city is built; there is exactly one armoury in
        /// the slice.</summary>
        public static Interactable EquipmentInteractable { get; private set; }

        private void Awake()
        {
            var root = new GameObject("CityBlockout_Generated").transform;
            root.SetParent(transform, false);

            Ground(root);
            SafeZone(root);
            Quarter(root);
            Windmill(root, new Vector3(14f, 0f, 12f));
            Board(root);
            Equipment(root);
            Training(root);
            Npcs(root);
        }

        // --- pieces -----------------------------------------------------------------------------

        private static void Ground(Transform root)
        {
            var g = Box(root, "Grass", new Vector3(0f, -0.05f, 0f), new Vector3(70f, 0.1f, 70f), Grass);
            g.isStatic = true;
        }

        /// <summary>The 안전 구역: a low stone wall ringing the heart of the city, with a marked
        /// threshold. It should be legible at a glance that inside here nothing hunts you.</summary>
        private static void SafeZone(Transform root)
        {
            const float half = 22f, h = 1.6f, t = 0.8f;
            Box(root, "SafeWall_N", new Vector3(0f, h * 0.5f, half), new Vector3(half * 2f, h, t), Stone);
            Box(root, "SafeWall_S_L", new Vector3(-half * 0.6f, h * 0.5f, -half), new Vector3(half * 0.8f, h, t), Stone);
            Box(root, "SafeWall_S_R", new Vector3(half * 0.6f, h * 0.5f, -half), new Vector3(half * 0.8f, h, t), Stone);
            Box(root, "SafeWall_W", new Vector3(-half, h * 0.5f, 0f), new Vector3(t, h, half * 2f), Stone);
            Box(root, "SafeWall_E", new Vector3(half, h * 0.5f, 0f), new Vector3(t, h, half * 2f), Stone);

            // The gate threshold — the line you cross to leave safety.
            Box(root, "GateLine", new Vector3(0f, 0.02f, -half), new Vector3(6f, 0.04f, 1.2f), SafeLine);
            Box(root, "GatePost_L", new Vector3(-3.2f, 1.4f, -half), new Vector3(0.6f, 2.8f, 0.9f), StoneDark);
            Box(root, "GatePost_R", new Vector3(3.2f, 1.4f, -half), new Vector3(0.6f, 2.8f, 0.9f), StoneDark);
        }

        /// <summary>Stone masonry with pitched slate roofs — the civilisation's silhouette. Placed by a
        /// fixed table rather than randomness so the city reads the same every time you come home.</summary>
        private static void Quarter(Transform root)
        {
            var plots = new[]
            {
                new Vector3(-12f, 0f, 16f), new Vector3(-5f, 0f, 17f), new Vector3(6f, 0f, 16f),
                new Vector3(15f, 0f, 4f),   new Vector3(15f, 0f, -6f), new Vector3(-16f, 0f, -8f),
                new Vector3(-8f, 0f, -14f), new Vector3(8f, 0f, -14f),
            };
            var sizes = new[]
            {
                new Vector3(6f, 5f, 6f), new Vector3(5f, 7f, 5f), new Vector3(7f, 4f, 5f),
                new Vector3(5f, 6f, 7f), new Vector3(6f, 5f, 6f), new Vector3(5f, 4f, 5f),
                new Vector3(6f, 6f, 5f), new Vector3(5f, 5f, 6f),
            };

            for (int i = 0; i < plots.Length; i++)
            {
                var p = plots[i];
                var s = sizes[i];
                Box(root, $"House_{i}", p + new Vector3(0f, s.y * 0.5f, 0f), s, i % 3 == 0 ? StoneDark : Stone);
                // A pitched roof, faked with a rotated slab — silhouette over fidelity.
                var roof = Box(root, $"Roof_{i}", p + new Vector3(0f, s.y + 0.5f, 0f),
                    new Vector3(s.x * 1.15f, 0.5f, s.z * 1.15f), Roof);
                roof.transform.localRotation = Quaternion.Euler(0f, 0f, i % 2 == 0 ? 8f : -8f);
            }

            // The cathedral spire the doc calls for — the landmark you navigate home by.
            Box(root, "Cathedral", new Vector3(-2f, 6f, 20f), new Vector3(8f, 12f, 8f), Stone);
            Box(root, "Spire", new Vector3(-2f, 16f, 20f), new Vector3(2f, 8f, 2f), Roof);
        }

        private static void Windmill(Transform root, Vector3 at)
        {
            Box(root, "Windmill_Tower", at + new Vector3(0f, 4f, 0f), new Vector3(3f, 8f, 3f), Stone);
            var hub = Box(root, "Windmill_Blades", at + new Vector3(0f, 8.5f, -1.8f), new Vector3(0.4f, 0.4f, 0.4f), Timber);
            for (int i = 0; i < 4; i++)
            {
                var blade = Box(hub.transform, $"Blade_{i}", Vector3.zero, new Vector3(0.6f, 7f, 0.15f), Timber);
                blade.transform.localRotation = Quaternion.Euler(0f, 0f, i * 45f);
                blade.transform.localPosition = Vector3.zero;
            }
            hub.AddComponent<Spin>();
        }

        /// <summary>The 게시판 — where contracts are taken. A physical place, not a menu button: you
        /// walk up to it AND press [F] to read it (ContractBoardPanel opens on that interact, not on
        /// proximity alone).</summary>
        private static void Board(Transform root)
        {
            Box(root, "Board_Post_L", BoardSpot + new Vector3(-1.6f, 1f, 0f), new Vector3(0.25f, 2f, 0.25f), Timber);
            Box(root, "Board_Post_R", BoardSpot + new Vector3(1.6f, 1f, 0f), new Vector3(0.25f, 2f, 0.25f), Timber);
            var face = Box(root, "Board_Face", BoardSpot + new Vector3(0f, 2.1f, 0f), new Vector3(3.6f, 2.2f, 0.2f), BoardWood);
            Box(root, "Board_Trim", BoardSpot + new Vector3(0f, 3.3f, 0f), new Vector3(4f, 0.25f, 0.35f), Timber);

            var interactable = face.AddComponent<Interactable>();
            interactable.Label = "Contract Board";
            interactable.Reach = BoardReach;
            BoardInteractable = interactable;
        }

        /// <summary>The armoury (stage 3's "첫 장비 선택") — a weapon rack over a low table, its own
        /// station away from the board and the NPCs. Walk up AND press [F] to open it
        /// (EquipmentStationPanel opens on that interact, same discipline as the board).</summary>
        private static void Equipment(Transform root)
        {
            var table = Box(root, "Armoury_Table", EquipmentSpot + new Vector3(0f, 0.5f, 0f), new Vector3(2.2f, 1f, 1.2f), Timber);
            Box(root, "Armoury_Post_L", EquipmentSpot + new Vector3(-1f, 1.3f, 0f), new Vector3(0.2f, 1.6f, 0.2f), StoneDark);
            Box(root, "Armoury_Post_R", EquipmentSpot + new Vector3(1f, 1.3f, 0f), new Vector3(0.2f, 1.6f, 0.2f), StoneDark);
            Box(root, "Armoury_Rail", EquipmentSpot + new Vector3(0f, 2.0f, 0f), new Vector3(2.4f, 0.15f, 0.15f), StoneDark);
            // Two hanging silhouettes — a blade and a bow — read as "weapons here" at a glance;
            // fidelity is the art track's job later, readability is this pass's.
            Box(root, "Armoury_Blade", EquipmentSpot + new Vector3(-0.5f, 1.4f, 0f), new Vector3(0.12f, 1.1f, 0.12f), RackMetal);
            Box(root, "Armoury_Bow", EquipmentSpot + new Vector3(0.5f, 1.4f, 0f), new Vector3(0.12f, 1.1f, 0.12f), Timber);

            var interactable = table.AddComponent<Interactable>();
            interactable.Label = "Equipment Station";
            interactable.Reach = EquipmentReach;
            EquipmentInteractable = interactable;
        }

        /// <summary>The 훈련장: an arena with dummies to hit. Everything the first hour teaches — move,
        /// jump, evade the tell, attack — is learned here by doing it.</summary>
        private static void Training(Transform root)
        {
            // A sand floor marks it out from the grass.
            Box(root, "TrainingFloor", TrainingGround + new Vector3(0f, 0.01f, 0f),
                new Vector3(14f, 0.05f, 14f), new Color(0.78f, 0.72f, 0.55f));

            // A low fence so it reads as its own room.
            const float h = 0.9f, half = 7f;
            Box(root, "TrainFence_N", TrainingGround + new Vector3(0f, h * 0.5f, half), new Vector3(14f, h, 0.3f), Timber);
            Box(root, "TrainFence_S", TrainingGround + new Vector3(0f, h * 0.5f, -half), new Vector3(14f, h, 0.3f), Timber);
            Box(root, "TrainFence_W", TrainingGround + new Vector3(-half, h * 0.5f, 0f), new Vector3(0.3f, h, 14f), Timber);

            // Something to hit. Three of them, spread so the player has to move between blows.
            Dummy(root, TrainingGround + new Vector3(-3f, 0f, 3f));
            Dummy(root, TrainingGround + new Vector3(2f, 0f, 4f));
            Dummy(root, TrainingGround + new Vector3(4f, 0f, -2f));

            // A ledge to jump onto — the "repeated jump" and "air attack" behaviours need somewhere to happen.
            Box(root, "TrainLedge", TrainingGround + new Vector3(-4.5f, 0.6f, -3.5f), new Vector3(3f, 1.2f, 3f), StoneDark);
            Box(root, "TrainLedge_Hi", TrainingGround + new Vector3(0f, 1.2f, -4.5f), new Vector3(2.5f, 2.4f, 2.5f), StoneDark);
        }

        private static void Dummy(Transform root, Vector3 at)
        {
            var post = Box(root, "Dummy", at + new Vector3(0f, 1f, 0f), new Vector3(0.9f, 2f, 0.9f), new Color(0.72f, 0.6f, 0.42f));
            post.AddComponent<HitReceiver>().SetMaxHealth(60f);
            post.AddComponent<TrainingDummy>();
            // Head, so a hit reads at eye level in first person.
            Box(post.transform, "Head", new Vector3(0f, 0.65f, 0f), new Vector3(0.55f, 0.35f, 0.55f), new Color(0.62f, 0.5f, 0.34f));
        }

        /// <summary>The people who issue the work. Bodies for the three seeded NPCs — dialogue comes with
        /// the NPC pass; for now the city simply has someone standing in it.</summary>
        private static void Npcs(Transform root)
        {
            // The quartermaster stands where the commissioning happens — 발주 is HIS offer to make.
            Npc(root, "Quartermaster Hale", CityNpc.Role.Quartermaster, new Vector3(-6f, 0f, 2f), new Color(0.45f, 0.5f, 0.62f));
            Npc(root, "Serjeant Bran", CityNpc.Role.Serjeant, new Vector3(-13f, 0f, 3f), new Color(0.62f, 0.45f, 0.42f)); // by the training ground
            Npc(root, "Survey Clerk Mira", CityNpc.Role.Clerk, new Vector3(3.5f, 0f, 5f), new Color(0.5f, 0.6f, 0.5f)); // by the board
        }

        private static void Npc(Transform root, string name, CityNpc.Role role, Vector3 at, Color color)
        {
            var body = Box(root, $"NPC_{name}", at + new Vector3(0f, 0.9f, 0f), new Vector3(0.6f, 1.8f, 0.5f), color);
            Box(body.transform, "Head", new Vector3(0f, 0.62f, 0f), new Vector3(0.6f, 0.3f, 0.7f), new Color(0.85f, 0.75f, 0.65f));
            body.AddComponent<CityNpc>().Configure(name, role);
        }

        // --- primitives -------------------------------------------------------------------------

        private static GameObject Box(Transform parent, string name, Vector3 localPos, Vector3 size, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = size;
            var r = go.GetComponent<Renderer>();
            if (r != null) r.material.color = color;
            return go;
        }

        /// <summary>A slowly turning windmill — the city should look alive even before it has animation.</summary>
        private sealed class Spin : MonoBehaviour
        {
            private void Update() => transform.Rotate(0f, 0f, 18f * Time.deltaTime, Space.Self);
        }
    }
}
