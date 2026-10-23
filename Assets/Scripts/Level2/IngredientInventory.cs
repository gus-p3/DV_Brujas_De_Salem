using System;
using System.Collections.Generic;
using UnityEngine;

namespace Level2
{
    /// <summary>
    /// Inventario dedicado para los ingredientes de poción del Nivel 2 (Acto 2).
    /// Completamente desacoplado de PotionInventory (frascos) y BroomInventory (escoba).
    /// Gestiona:
    /// - Requisitos por tipo de ingrediente (configurables sin recompilar).
    /// - Conteo acumulado y prevención de doble recolección por ID.
    /// - Eventos para el HUD y el portón de salida (IngredientGate).
    /// - Persistencia con claves "L2_" para no alterar las claves globales ni el Nivel 1.
    /// </summary>
    public class IngredientInventory : MonoBehaviour
    {
        private static IngredientInventory _instance;
        public static IngredientInventory Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = UnityEngine.Object.FindFirstObjectByType<IngredientInventory>();
                }
                return _instance;
            }
            private set => _instance = value;
        }

        [Header("Requisitos por Tipo de Ingrediente")]
        [Tooltip("Hongos necesarios para completar el acto.")]
        [SerializeField] private int requiredMushrooms = 2;

        [Tooltip("Arañas estilizadas necesarias para completar el acto.")]
        [SerializeField] private int requiredSpiders = 2;

        [Tooltip("Flores marchitas necesarias para completar el acto.")]
        [SerializeField] private int requiredFlowers = 2;

        [Tooltip("Raíces necesarias para completar el acto.")]
        [SerializeField] private int requiredRoots = 2;

        [Header("Persistencia")]
        [Tooltip("Si está activo, carga el progreso de sesión al iniciar la escena.")]
        [SerializeField] private bool persistAcrossCheckpoints = true;

        // Conteos en memoria
        private int currentMushrooms = 0;
        private int currentSpiders = 0;
        private int currentFlowers = 0;
        private int currentRoots = 0;

        private readonly HashSet<string> collectedIds = new HashSet<string>();
        private bool allRequirementsMetFired = false;

        // Claves de persistencia propias (L2_)
        private const string KeySessionActive = "L2_Session_Active";
        private const string KeyMushrooms = "L2_Session_Mushrooms";
        private const string KeySpiders = "L2_Session_Spiders";
        private const string KeyFlowers = "L2_Session_Flowers";
        private const string KeyRoots = "L2_Session_Roots";
        private const string KeyCollectedIds = "L2_Session_CollectedIDs";

        // Claves de guardado final para Acto 3
        public const string KeyFinalMushrooms = "L2_Final_Mushrooms";
        public const string KeyFinalSpiders = "L2_Final_Spiders";
        public const string KeyFinalFlowers = "L2_Final_Flowers";
        public const string KeyFinalRoots = "L2_Final_Roots";
        public const string KeyAct2Complete = "L2_Act2_Ingredients_Complete";

        /// <summary>
        /// Evento disparado cuando se recoge un ingrediente.
        /// Pasa: (tipo, cantidadActualDelTipo, cantidadRequeridaDelTipo)
        /// </summary>
        public event Action<IngredientType, int, int> OnIngredientCollected;

        /// <summary>
        /// Evento disparado una sola vez cuando se cumplen todos los requisitos mínimos de ingredientes.
        /// </summary>
        public event Action OnAllRequiredCollected;

        /// <summary>
        /// Evento disparado cuando el inventario se reinicia (ej. Game Over).
        /// </summary>
        public event Action OnInventoryReset;

        public int RequiredMushrooms { get => requiredMushrooms; set => requiredMushrooms = value; }
        public int RequiredSpiders { get => requiredSpiders; set => requiredSpiders = value; }
        public int RequiredFlowers { get => requiredFlowers; set => requiredFlowers = value; }
        public int RequiredRoots { get => requiredRoots; set => requiredRoots = value; }

        public int CurrentMushrooms => currentMushrooms;
        public int CurrentSpiders => currentSpiders;
        public int CurrentFlowers => currentFlowers;
        public int CurrentRoots => currentRoots;

        public int TotalRequired => requiredMushrooms + requiredSpiders + requiredFlowers + requiredRoots;
        public int TotalCollected => currentMushrooms + currentSpiders + currentFlowers + currentRoots;

        public bool AreAllRequiredCollected =>
            currentMushrooms >= requiredMushrooms &&
            currentSpiders >= requiredSpiders &&
            currentFlowers >= requiredFlowers &&
            currentRoots >= requiredRoots;

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

            if (persistAcrossCheckpoints && PlayerPrefs.GetInt(KeySessionActive, 0) == 1)
            {
                LoadSessionProgress();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// Comprueba si un ingrediente por su ID ya fue recogido en esta sesión.
        /// </summary>
        public bool IsCollected(string id)
        {
            return !string.IsNullOrEmpty(id) && collectedIds.Contains(id);
        }

        /// <summary>
        /// Registra la recolección de un ingrediente.
        /// </summary>
        public void Collect(Ingredient ingredient)
        {
            if (ingredient == null) return;
            Collect(ingredient.Type, ingredient.Id);
        }

        /// <summary>
        /// Registra la recolección por tipo e ID.
        /// </summary>
        public void Collect(IngredientType type, string id)
        {
            if (!string.IsNullOrEmpty(id))
            {
                if (collectedIds.Contains(id))
                {
                    Debug.LogWarning($"[IngredientInventory] Intento de doble recolección para ID: {id}");
                    return;
                }
                collectedIds.Add(id);
            }

            int current = 0;
            int required = 0;

            switch (type)
            {
                case IngredientType.Mushroom:
                    currentMushrooms++;
                    current = currentMushrooms;
                    required = requiredMushrooms;
                    break;
                case IngredientType.Spider:
                    currentSpiders++;
                    current = currentSpiders;
                    required = requiredSpiders;
                    break;
                case IngredientType.WitheredFlower:
                    currentFlowers++;
                    current = currentFlowers;
                    required = requiredFlowers;
                    break;
                case IngredientType.Root:
                    currentRoots++;
                    current = currentRoots;
                    required = requiredRoots;
                    break;
            }

            Debug.Log($"[IngredientInventory] Recolectado: {type} ({current}/{required}). Total: {TotalCollected}/{TotalRequired}");

            SaveSessionProgress();
            OnIngredientCollected?.Invoke(type, current, required);

            if (AreAllRequiredCollected && !allRequirementsMetFired)
            {
                allRequirementsMetFired = true;
                Debug.Log("[IngredientInventory] ¡Todos los requisitos de ingredientes han sido completados!");
                OnAllRequiredCollected?.Invoke();
            }
        }

        /// <summary>
        /// Obtiene el conteo actual de un tipo de ingrediente.
        /// </summary>
        public int GetCount(IngredientType type)
        {
            switch (type)
            {
                case IngredientType.Mushroom: return currentMushrooms;
                case IngredientType.Spider: return currentSpiders;
                case IngredientType.WitheredFlower: return currentFlowers;
                case IngredientType.Root: return currentRoots;
                default: return 0;
            }
        }

        /// <summary>
        /// Obtiene la cantidad requerida de un tipo de ingrediente.
        /// </summary>
        public int GetRequired(IngredientType type)
        {
            switch (type)
            {
                case IngredientType.Mushroom: return requiredMushrooms;
                case IngredientType.Spider: return requiredSpiders;
                case IngredientType.WitheredFlower: return requiredFlowers;
                case IngredientType.Root: return requiredRoots;
                default: return 0;
            }
        }

        /// <summary>
        /// Guarda el estado de la sesión activa en PlayerPrefs con claves "L2_".
        /// </summary>
        public void SaveSessionProgress()
        {
            PlayerPrefs.SetInt(KeySessionActive, 1);
            PlayerPrefs.SetInt(KeyMushrooms, currentMushrooms);
            PlayerPrefs.SetInt(KeySpiders, currentSpiders);
            PlayerPrefs.SetInt(KeyFlowers, currentFlowers);
            PlayerPrefs.SetInt(KeyRoots, currentRoots);

            string ids = string.Join(";", collectedIds);
            PlayerPrefs.SetString(KeyCollectedIds, ids);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Carga el estado de sesión guardado.
        /// </summary>
        public void LoadSessionProgress()
        {
            currentMushrooms = PlayerPrefs.GetInt(KeyMushrooms, 0);
            currentSpiders = PlayerPrefs.GetInt(KeySpiders, 0);
            currentFlowers = PlayerPrefs.GetInt(KeyFlowers, 0);
            currentRoots = PlayerPrefs.GetInt(KeyRoots, 0);

            collectedIds.Clear();
            string ids = PlayerPrefs.GetString(KeyCollectedIds, "");
            if (!string.IsNullOrEmpty(ids))
            {
                string[] parts = ids.Split(';');
                foreach (string id in parts)
                {
                    if (!string.IsNullOrEmpty(id))
                    {
                        collectedIds.Add(id);
                    }
                }
            }

            allRequirementsMetFired = AreAllRequiredCollected;
            Debug.Log($"[IngredientInventory] Progreso de sesión cargado: {TotalCollected}/{TotalRequired} ingredientes.");
        }

        /// <summary>
        /// Reinicia la sesión de recolección (al reiniciar el nivel tras GameOver o volver al Menú).
        /// </summary>
        public void ResetProgress()
        {
            currentMushrooms = 0;
            currentSpiders = 0;
            currentFlowers = 0;
            currentRoots = 0;
            collectedIds.Clear();
            allRequirementsMetFired = false;

            PlayerPrefs.DeleteKey(KeySessionActive);
            PlayerPrefs.DeleteKey(KeyMushrooms);
            PlayerPrefs.DeleteKey(KeySpiders);
            PlayerPrefs.DeleteKey(KeyFlowers);
            PlayerPrefs.DeleteKey(KeyRoots);
            PlayerPrefs.DeleteKey(KeyCollectedIds);
            PlayerPrefs.Save();

            OnInventoryReset?.Invoke();
            Debug.Log("[IngredientInventory] Progreso de sesión reiniciado.");
        }

        /// <summary>
        /// Registra la finalización del Acto 2 y persiste los totales definitivos para el Acto 3.
        /// </summary>
        public void CommitAct2Completion()
        {
            PlayerPrefs.SetInt(KeyAct2Complete, 1);
            PlayerPrefs.SetInt(KeyFinalMushrooms, currentMushrooms);
            PlayerPrefs.SetInt(KeyFinalSpiders, currentSpiders);
            PlayerPrefs.SetInt(KeyFinalFlowers, currentFlowers);
            PlayerPrefs.SetInt(KeyFinalRoots, currentRoots);
            PlayerPrefs.Save();
            Debug.Log($"[IngredientInventory] ¡Acto 2 completado! Ingredientes guardados para Acto 3: M={currentMushrooms}, S={currentSpiders}, F={currentFlowers}, R={currentRoots}.");
        }
    }
}
