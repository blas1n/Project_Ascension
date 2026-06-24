using UnityEngine;
using ProjectAscension.Equipment;

namespace ProjectAscension.Game
{
    /// <summary>
    /// Cross-scene game state. Created in the Bootstrap scene and kept alive
    /// (DontDestroyOnLoad) so contract progress, currency, and the chosen loadout
    /// survive City&lt;-&gt;Frontier transitions. Accessed via <see cref="Instance"/>.
    /// (A pragmatic singleton for the slice; can move under VContainer later.)
    /// </summary>
    public sealed class GameSession : MonoBehaviour
    {
        [SerializeField] private WeaponData[] ownedWeapons;

        public static GameSession Instance { get; private set; }

        public ContractService Contracts { get; private set; }
        public PlayerStateService PlayerState { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            Contracts = new ContractService();
            PlayerState = new PlayerStateService(ownedWeapons ?? new WeaponData[0]);
        }
    }
}
