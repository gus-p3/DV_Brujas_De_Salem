using System;
using Level2;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Level3
{
    /// <summary>
    /// Interruptor o palanca rúnica interactuable con [E] para abrir portones de rastra,
    /// compuertas o activar atajos mecánicos en el Nivel 3.
    /// Implements <see cref="IInteractable"/>.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Level3RuneSwitch : MonoBehaviour, IInteractable
    {
        [Header("Identificación")]
        [SerializeField] private string switchId = "rune_switch_1";
        [SerializeField] private string switchDisplayName = "Mecanismo de Runa";
        [SerializeField] private bool singleActivation = true;

        [Header("Objetivo Conectado")]
        [SerializeField] private Level3MechanismGate connectedGate;

        [Header("Visuales")]
        [SerializeField] private SpriteRenderer runeRenderer;
        [SerializeField] private Sprite inactiveSprite;
        [SerializeField] private Sprite activeSprite;
        [SerializeField] private Color inactiveColor = new Color(0.8f, 0.4f, 0.3f, 1f);
        [SerializeField] private Color activeColor = new Color(0.3f, 1f, 0.7f, 1f);
        [SerializeField] private Light2D switchLight;

        [Header("Mensaje y Recompensa")]
        [SerializeField] private string activationMessage = "¡Mecanismo de runa activado!";
        [SerializeField] private int activationScore = 150;

        private bool isActivated = false;

        public string InteractionId => $"switch_{switchId}";
        public int Priority => 25;
        public bool UsesInteractKey => true;
        public Transform Transform => transform;
        public float PromptHeightOffset => 1.2f;

        public bool IsActivated => isActivated;
        public string SwitchId => switchId;

        public event Action<Level3RuneSwitch> OnSwitchToggled;

        private void Awake()
        {
            Collider2D col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;

            if (switchLight == null) switchLight = GetComponentInChildren<Light2D>();
            if (runeRenderer == null) runeRenderer = GetComponentInChildren<SpriteRenderer>();

            UpdateVisuals();
        }

        public void Configure(string id, string displayName, Level3MechanismGate gate, Sprite offSprite = null, Sprite onSprite = null)
        {
            switchId = id;
            switchDisplayName = displayName;
            connectedGate = gate;
            if (offSprite != null) inactiveSprite = offSprite;
            if (onSprite != null) activeSprite = onSprite;
            UpdateVisuals();
        }

        public bool CanInteract(GameObject interactor)
        {
            if (singleActivation && isActivated) return false;
            return true;
        }

        public string GetPromptText(GameObject interactor)
        {
            if (isActivated)
            {
                return $"{switchDisplayName} (Activado)";
            }
            return $"E — Activar {switchDisplayName}";
        }

        public void Interact(GameObject interactor)
        {
            if (singleActivation && isActivated) return;

            isActivated = !isActivated;
            UpdateVisuals();

            if (connectedGate != null)
            {
                connectedGate.NotifySwitchChanged(this);
            }

            OnSwitchToggled?.Invoke(this);

            if (isActivated && activationScore > 0 && GameManager.Instance != null)
            {
                GameManager.Instance.AddScore(activationScore);
            }

            HUDController hud = UnityEngine.Object.FindFirstObjectByType<HUDController>();
            if (hud != null && !string.IsNullOrEmpty(activationMessage))
            {
                hud.ShowMessage(activationMessage, 2.5f);
            }

            if (AudioManager.Instance != null && AudioManager.Instance.potionPickupClip != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.Instance.potionPickupClip);
            }
        }

        private void UpdateVisuals()
        {
            if (runeRenderer != null)
            {
                runeRenderer.color = isActivated ? activeColor : inactiveColor;
                if (isActivated && activeSprite != null) runeRenderer.sprite = activeSprite;
                else if (!isActivated && inactiveSprite != null) runeRenderer.sprite = inactiveSprite;
            }

            if (switchLight != null)
            {
                switchLight.color = isActivated ? activeColor : inactiveColor;
                switchLight.intensity = isActivated ? 1.2f : 0.45f;
            }
        }
    }
}
