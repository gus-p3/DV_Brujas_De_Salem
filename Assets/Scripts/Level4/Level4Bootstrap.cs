using Level2;
using UnityEngine;

namespace Salem.Level4
{
    [DefaultExecutionOrder(-600)]
    public class Level4Bootstrap : MonoBehaviour
    {
        public static Level4Bootstrap Instance { get; private set; }

        [Header("Starting Settings")]
        [SerializeField] private bool ensureMasterPotion = true;
        [SerializeField] private bool ensureBroomUnlocked = true;
        [SerializeField] private AudioClip courtyardMusic;

        public const KeyCode InteractKey = KeyCode.Q;
        public const KeyCode ThrowPotionKey = KeyCode.E;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Time.timeScale = 1f;
            Level2Controls.SetSceneScheme(InteractKey, ThrowPotionKey);
        }

        private void Start()
        {
            InitializePlayer();
            StartMusic();
        }

        private void InitializePlayer()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;

            // Ensure NoiseProducer is on the player
            if (player.GetComponent<NoiseProducer>() == null)
            {
                player.AddComponent<NoiseProducer>();
            }

            // Ensure Broom is unlocked
            if (ensureBroomUnlocked)
            {
                var broomInv = player.GetComponent<BroomInventory>();
                if (broomInv != null)
                {
                    broomInv.GrantBroom();
                }
            }

            // Ensure Master Potion is available
            if (ensureMasterPotion && GameManager.Instance != null)
            {
                GameManager.Instance.UnlockMasterPotionUsage();
                if (GameManager.Instance.MasterCharges <= 0)
                {
                    GameManager.Instance.MasterCharges = 1;
                }
            }
        }

        private void StartMusic()
        {
            if (courtyardMusic != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayMusic(courtyardMusic);
            }
        }
    }
}
