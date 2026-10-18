using System;
using UnityEngine;

/// <summary>
/// Gestiona la selección y lanzamiento de pociones por parte de la bruja.
/// Selección con teclas numéricas 1, 2 y 3.
/// Lanzamiento con 'E' o clic izquierdo (con cooldown de 0.5s).
/// Respeta si la bruja está en forma humana (TransformationManager.CanCastSpells)
/// y bloquea el lanzamiento mientras un tip de tutorial esté en pantalla.
/// </summary>
public class PotionInventory : MonoBehaviour
{
    [Header("Configuración de Lanzamiento")]
    [Tooltip("Tiempo de enfriamiento mínimo entre lanzamientos (s).")]
    [SerializeField] private float throwCooldown = 0.5f;
    [Tooltip("Desplazamiento horizontal relativo desde donde se engendra el proyectil.")]
    [SerializeField] private float spawnOffsetX = 0.7f;
    [Tooltip("Desplazamiento vertical relativo desde donde se engendra el proyectil.")]
    [SerializeField] private float spawnOffsetY = 0.5f;

    [Header("Prefabs de Proyectiles")]
    [Tooltip("Prefab del proyectil de Cegar.")]
    [SerializeField] private GameObject blindProjectilePrefab;
    [Tooltip("Prefab del proyectil de Hipnotizar.")]
    [SerializeField] private GameObject hypnotizeProjectilePrefab;
    [Tooltip("Prefab del proyectil de Dormir.")]
    [SerializeField] private GameObject sleepProjectilePrefab;

    [Header("Audio")]
    [Tooltip("Sonido de lanzamiento exitoso.")]
    [SerializeField] private AudioClip throwSound;
    [Tooltip("Sonido de error al intentar lanzar sin cargas.")]
    [SerializeField] private AudioClip emptyChargesSound;

    private PlayerController playerController;
    private TransformationManager transformationManager;
    private PotionType selectedPotion = PotionType.Blind;
    private float lastThrowTime = -10f;
    private int lastTutorialOpenFrame = -10;

    /// <summary>
    /// Evento disparado cuando cambia el tipo de poción seleccionada en el inventario.
    /// </summary>
    public event Action<PotionType> OnPotionSelected;

    public PotionType SelectedPotion => selectedPotion;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
        transformationManager = GetComponent<TransformationManager>();
        if (transformationManager == null)
        {
            transformationManager = UnityEngine.Object.FindFirstObjectByType<TransformationManager>();
        }
    }

    private void Start()
    {
        // Notificar selección inicial al HUD
        OnPotionSelected?.Invoke(selectedPotion);
    }

    private void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Playing)
        {
            return;
        }

        // Bloquear mientras haya un diálogo/tip de tutorial activo
        if (TutorialManager.Instance != null && TutorialManager.Instance.IsOpen)
        {
            lastTutorialOpenFrame = Time.frameCount;
            return;
        }

        // Evitar que el mismo clic que cerró el tutorial dispare una poción en el mismo frame
        if (Time.frameCount <= lastTutorialOpenFrame + 1)
        {
            return;
        }

        HandleSelectionInput();
        HandleThrowInput();
    }

    private void HandleSelectionInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
        {
            SelectPotion(PotionType.Blind);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
        {
            SelectPotion(PotionType.Hypnotize);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
        {
            SelectPotion(PotionType.Sleep);
        }
    }

    public void SelectPotion(PotionType type)
    {
        selectedPotion = type;
        OnPotionSelected?.Invoke(selectedPotion);

        HUDController hud = UnityEngine.Object.FindFirstObjectByType<HUDController>();
        if (hud != null)
        {
            hud.SetSelectedPotion((int)selectedPotion);
        }
    }

    private void HandleThrowInput()
    {
        bool mouseClick = Input.GetMouseButtonDown(0);
        if (mouseClick && UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            // Si el clic fue sobre un botón de UI interactivo, no lanzar
            var selectedGo = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject;
            if (selectedGo != null && selectedGo.GetComponent<UnityEngine.UI.Button>() != null)
            {
                mouseClick = false;
            }
        }

        // Lanzar con tecla E, Q o clic izquierdo
        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Q) || mouseClick)
        {
            TryThrowPotion();
        }
    }

    private void TryThrowPotion()
    {
        // Cooldown de 0.5 segundos
        if (Time.time < lastThrowTime + throwCooldown)
        {
            return;
        }

        // Verificar si la bruja puede lanzar hechizos (solo en forma humana)
        if (transformationManager != null && !transformationManager.CanCastSpells)
        {
            HUDController hud = UnityEngine.Object.FindFirstObjectByType<HUDController>();
            if (hud != null)
            {
                hud.ShowMessage("¡No puedes lanzar pociones convertida en gato!", 1.5f);
            }
            PlayErrorSound();
            return;
        }

        // Si el tipo seleccionado no tiene cargas pero el jugador tiene de otro tipo, auto-seleccionar el disponible
        int spellIndex = (int)selectedPotion;
        if (GetChargesForType(selectedPotion) <= 0)
        {
            if (GameManager.Instance.BlindCharges > 0) SelectPotion(PotionType.Blind);
            else if (GameManager.Instance.HypnotizeCharges > 0) SelectPotion(PotionType.Hypnotize);
            else if (GameManager.Instance.SleepCharges > 0) SelectPotion(PotionType.Sleep);
            spellIndex = (int)selectedPotion;
        }

        // Intentar consumir carga del GameManager
        bool consumed = GameManager.Instance.TryConsumeSpell(spellIndex);

        if (!consumed)
        {
            // Parpadear el contador en el HUD y emitir sonido de advertencia
            HUDController hud = UnityEngine.Object.FindFirstObjectByType<HUDController>();
            if (hud != null)
            {
                hud.FlashEmptyCharges(spellIndex);
            }
            PlayErrorSound();
            return;
        }

        // Lanzar proyectil
        lastThrowTime = Time.time;
        SpawnAndLaunchProjectile();

        // Si se agotaron las cargas de esta poción y hay otra disponible, cambiar selección automáticamente
        if (GetChargesForType(selectedPotion) <= 0)
        {
            if (GameManager.Instance.BlindCharges > 0) SelectPotion(PotionType.Blind);
            else if (GameManager.Instance.HypnotizeCharges > 0) SelectPotion(PotionType.Hypnotize);
            else if (GameManager.Instance.SleepCharges > 0) SelectPotion(PotionType.Sleep);
        }

        if (AudioManager.Instance != null && throwSound != null)
        {
            AudioManager.Instance.PlaySFX(throwSound);
        }
    }

    private int GetChargesForType(PotionType type)
    {
        if (GameManager.Instance == null) return 0;
        return type switch
        {
            PotionType.Blind => GameManager.Instance.BlindCharges,
            PotionType.Hypnotize => GameManager.Instance.HypnotizeCharges,
            PotionType.Sleep => GameManager.Instance.SleepCharges,
            _ => 0
        };
    }

    private void SpawnAndLaunchProjectile()
    {
        GameObject prefab = selectedPotion switch
        {
            PotionType.Blind => blindProjectilePrefab,
            PotionType.Hypnotize => hypnotizeProjectilePrefab,
            PotionType.Sleep => sleepProjectilePrefab,
            _ => blindProjectilePrefab
        };

        int facingDir = 1;
        if (playerController != null)
        {
            facingDir = playerController.FacingDirection;
        }

        Vector3 spawnPos = transform.position + new Vector3(facingDir * spawnOffsetX, spawnOffsetY, 0f);

        GameObject projectileObj = null;
        if (prefab != null)
        {
            projectileObj = Instantiate(prefab, spawnPos, Quaternion.identity);
        }
        else
        {
            // Respaldo dinámico si el prefab no estuviese enlazado en el inspector
            projectileObj = new GameObject($"Potion_{selectedPotion}");
            projectileObj.transform.position = spawnPos;
            PotionProjectile projectile = projectileObj.AddComponent<PotionProjectile>();
            CircleCollider2D col = projectileObj.AddComponent<CircleCollider2D>();
            col.radius = 0.25f;
            SpriteRenderer sr = projectileObj.AddComponent<SpriteRenderer>();
            sr.sortingLayerName = "Default";
            sr.sortingOrder = 10;
        }

        // Ignorar colisiones con la propia bruja para que el proyectil salga limpio en arco
        Collider2D projCol = projectileObj.GetComponent<Collider2D>();
        if (projCol != null)
        {
            Collider2D[] playerCols = GetComponentsInChildren<Collider2D>(true);
            foreach (Collider2D pc in playerCols)
            {
                if (pc != null)
                {
                    Physics2D.IgnoreCollision(projCol, pc, true);
                }
            }
        }

        PotionProjectile projComp = projectileObj.GetComponent<PotionProjectile>();
        if (projComp != null)
        {
            projComp.Launch(facingDir);
        }
    }

    private void PlayErrorSound()
    {
        if (AudioManager.Instance != null && emptyChargesSound != null)
        {
            AudioManager.Instance.PlaySFX(emptyChargesSound);
        }
    }
}
