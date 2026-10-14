using System;
using UnityEngine;

/// <summary>
/// Gestiona la visualización de la escoba montada en la bruja durante el vuelo.
/// Aparece cuando la bruja activa el vuelo con la tecla F (tras completar la escoba)
/// y se oculta al aterrizar o al transformarse en gato.
/// </summary>
public class BroomVisual : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("SpriteRenderer de la escoba. Si no se asigna, se busca o crea automáticamente.")]
    [SerializeField] private SpriteRenderer broomRenderer;
    [Tooltip("Sprite de la escoba montable.")]
    [SerializeField] private Sprite broomSprite;

    [Header("Alineación y Escala")]
    [Tooltip("Desplazamiento local de la escoba respecto a la bruja al mirar a la derecha.")]
    [SerializeField] private Vector3 flyingOffset = new Vector3(0.05f, 0.45f, 0f);
    [Tooltip("Escala local del objeto visual de la escoba.")]
    [SerializeField] private Vector3 broomScale = new Vector3(0.55f, 0.55f, 1f);
    [Tooltip("Capa de ordenamiento del sprite de la escoba.")]
    [SerializeField] private string sortingLayerName = "Gameplay";
    [Tooltip("Orden dentro de la capa (se coloca justo detrás de la bruja o encima según convenga).")]
    [SerializeField] private int sortingOrder = 1;

    private PlayerController playerController;
    private TransformationManager transformationManager;
    private SpriteRenderer witchRenderer;

    private void Awake()
    {
        playerController = GetComponentInParent<PlayerController>();
        transformationManager = GetComponentInParent<TransformationManager>();

        if (playerController != null)
        {
            witchRenderer = playerController.GetComponentInChildren<SpriteRenderer>();
        }

        EnsureBroomObject();
    }

    private void OnEnable()
    {
        if (playerController != null)
        {
            playerController.OnFlightChanged += HandleFlightChanged;
        }

        if (transformationManager != null)
        {
            transformationManager.OnTransformed += HandleTransformed;
        }

        // Estado inicial: sincronizar con el vuelo actual
        bool isCurrentlyFlying = playerController != null && playerController.IsFlying;
        SetBroomVisible(isCurrentlyFlying);
    }

    private void OnDisable()
    {
        if (playerController != null)
        {
            playerController.OnFlightChanged -= HandleFlightChanged;
        }

        if (transformationManager != null)
        {
            transformationManager.OnTransformed -= HandleTransformed;
        }

        SetBroomVisible(false);
    }

    private void LateUpdate()
    {
        if (broomRenderer == null || !broomRenderer.gameObject.activeSelf)
        {
            return;
        }

        UpdateBroomOrientation();
    }

    /// <summary>
    /// Crea o localiza el objeto visual hijo de la escoba y configura su SpriteRenderer.
    /// </summary>
    public void EnsureBroomObject()
    {
        if (broomRenderer == null)
        {
            Transform existing = transform.Find("BroomVisual");
            if (existing != null)
            {
                broomRenderer = existing.GetComponent<SpriteRenderer>();
            }
        }

        if (broomRenderer == null)
        {
            GameObject broomObj = new GameObject("BroomVisual");
            broomObj.transform.SetParent(transform, false);
            broomRenderer = broomObj.AddComponent<SpriteRenderer>();
        }

        if (broomSprite == null)
        {
            broomSprite = Resources.Load<Sprite>("Witch_Broom");
            if (broomSprite == null)
            {
                #if UNITY_EDITOR
                broomSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Characters/Witch/Witch_Broom.png");
                #endif
            }
        }

        if (broomRenderer != null)
        {
            if (broomSprite != null)
            {
                broomRenderer.sprite = broomSprite;
            }
            broomRenderer.sortingLayerName = sortingLayerName;
            broomRenderer.sortingOrder = sortingOrder;
            broomRenderer.gameObject.SetActive(false);
        }
    }

    private void HandleFlightChanged(bool isFlying)
    {
        // Si está en forma de gato, no debe mostrar la escoba
        if (transformationManager != null && transformationManager.IsCatForm)
        {
            SetBroomVisible(false);
            return;
        }

        SetBroomVisible(isFlying);
        if (isFlying)
        {
            UpdateBroomOrientation();
        }
    }

    private void HandleTransformed(bool isCat)
    {
        if (isCat)
        {
            SetBroomVisible(false);
        }
        else if (playerController != null && playerController.IsFlying)
        {
            SetBroomVisible(true);
            UpdateBroomOrientation();
        }
    }

    private void SetBroomVisible(bool visible)
    {
        if (broomRenderer != null && broomRenderer.gameObject != null)
        {
            broomRenderer.gameObject.SetActive(visible);
        }
    }

    private void UpdateBroomOrientation()
    {
        if (broomRenderer == null) return;

        bool isFlipped = false;
        if (witchRenderer != null)
        {
            isFlipped = witchRenderer.flipX;
        }

        broomRenderer.flipX = isFlipped;

        // Ajustar posición según orientación
        Vector3 localPos = flyingOffset;
        if (isFlipped)
        {
            localPos.x = -flyingOffset.x;
        }

        broomRenderer.transform.localPosition = localPos;
        broomRenderer.transform.localScale = broomScale;
    }
}
