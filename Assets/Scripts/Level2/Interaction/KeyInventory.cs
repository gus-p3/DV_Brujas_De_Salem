using System;
using System.Collections.Generic;
using UnityEngine;

namespace Level2
{
    /// <summary>
    /// Gestiona las llaves, secretos y objetos de exploración recogidos en el Nivel 2.
    /// Permite desbloquear puertas cerradas y cofres especiales sin duplicar recompensas.
    /// Persiste el estado dentro de la sesión del Nivel 2 usando el prefijo "L2_".
    /// </summary>
    public class KeyInventory : MonoBehaviour
    {
        private static KeyInventory _instance;
        public static KeyInventory Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = UnityEngine.Object.FindFirstObjectByType<KeyInventory>();
                }
                return _instance;
            }
            private set => _instance = value;
        }

        private readonly HashSet<string> collectedKeys = new HashSet<string>();
        private readonly HashSet<string> openedChests = new HashSet<string>();
        private readonly HashSet<string> unlockedDoors = new HashSet<string>();
        private readonly HashSet<string> discoveredSecrets = new HashSet<string>();

        private const string PrefsKeys = "L2_Session_Keys";
        private const string PrefsChests = "L2_Session_Chests";
        private const string PrefsDoors = "L2_Session_Doors";
        private const string PrefsSecrets = "L2_Session_Secrets";

        public event Action<string> OnKeyCollected;
        public event Action<string> OnChestOpened;
        public event Action<string> OnSecretDiscovered;

        public int KeysCount => collectedKeys.Count;
        public int OpenedChestsCount => openedChests.Count;
        public int DiscoveredSecretsCount => discoveredSecrets.Count;

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
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public bool HasKey(string keyId)
        {
            return !string.IsNullOrEmpty(keyId) && collectedKeys.Contains(keyId);
        }

        public bool AddKey(string keyId, string displayName = "")
        {
            if (string.IsNullOrEmpty(keyId) || collectedKeys.Contains(keyId))
            {
                return false;
            }

            collectedKeys.Add(keyId);
            SaveSet(PrefsKeys, collectedKeys);
            OnKeyCollected?.Invoke(keyId);

            HUDController hud = UnityEngine.Object.FindFirstObjectByType<HUDController>();
            if (hud != null)
            {
                string label = string.IsNullOrEmpty(displayName) ? keyId : displayName;
                hud.ShowMessage($"Obtenido: {label}", 2.2f);
            }
            return true;
        }

        public bool IsChestOpened(string chestId)
        {
            return !string.IsNullOrEmpty(chestId) && openedChests.Contains(chestId);
        }

        public bool MarkChestOpened(string chestId)
        {
            if (string.IsNullOrEmpty(chestId) || openedChests.Contains(chestId))
            {
                return false;
            }

            openedChests.Add(chestId);
            SaveSet(PrefsChests, openedChests);
            OnChestOpened?.Invoke(chestId);
            return true;
        }

        public bool IsDoorUnlocked(string doorId)
        {
            return !string.IsNullOrEmpty(doorId) && unlockedDoors.Contains(doorId);
        }

        public void MarkDoorUnlocked(string doorId)
        {
            if (string.IsNullOrEmpty(doorId)) return;
            if (unlockedDoors.Add(doorId))
            {
                SaveSet(PrefsDoors, unlockedDoors);
            }
        }

        public bool IsSecretDiscovered(string secretId)
        {
            return !string.IsNullOrEmpty(secretId) && discoveredSecrets.Contains(secretId);
        }

        public bool MarkSecretDiscovered(string secretId)
        {
            if (string.IsNullOrEmpty(secretId) || discoveredSecrets.Contains(secretId))
            {
                return false;
            }

            discoveredSecrets.Add(secretId);
            SaveSet(PrefsSecrets, discoveredSecrets);
            OnSecretDiscovered?.Invoke(secretId);
            return true;
        }

        public void ResetExplorationProgress()
        {
            collectedKeys.Clear();
            openedChests.Clear();
            unlockedDoors.Clear();
            discoveredSecrets.Clear();

            PlayerPrefs.DeleteKey(PrefsKeys);
            PlayerPrefs.DeleteKey(PrefsChests);
            PlayerPrefs.DeleteKey(PrefsDoors);
            PlayerPrefs.DeleteKey(PrefsSecrets);
            PlayerPrefs.Save();
        }

        private static void SaveSet(string key, HashSet<string> set)
        {
            PlayerPrefs.SetString(key, string.Join(";", set));
            PlayerPrefs.Save();
        }
    }
}
