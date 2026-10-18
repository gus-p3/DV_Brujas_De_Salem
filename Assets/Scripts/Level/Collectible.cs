using System;
using UnityEngine;

/// <summary>
/// Partes de la escoba recolectables por la bruja.
/// </summary>
public enum BroomPart
{
    Branch,
    Straw,
    Rope
}

/// <summary>
/// Ítem recolectable en el nivel (rama, paja o cuerda).
/// Solo puede ser recogido por la bruja en forma humana (TransformationManager.CanCollect).
/// Flota suavemente en su posición inicial y se recoge al entrar en contacto o proximidad.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Collectible : MonoBehaviour
{
    [Header("Tipo y Recompensa")]
    [Tooltip("Parte de la escoba que representa este ítem.")]
    [SerializeField] private BroomPart part = BroomPart.Branch;
    [Tooltip("Puntos sumados al GameManager al recoger.")]
    [SerializeField] private int scoreValue = 100;

    [Header("Efectos Audiovisuales")]
    [Tooltip("Efecto de sonido al recoger.")]
    [SerializeField] private AudioClip pickupSfx;
    [Tooltip("Prefab de partículas instanciado al recoger.")]
    [SerializeField] private GameObject pickupVfx;

    [Header("Animación de flotación (Bobbing)")]
    [Tooltip("Amplitud vertical del vaivén.")]
    [SerializeField] private float bobAmplitude = 0.04f;
    [Tooltip("Velocidad de oscilación.")]
    [SerializeField] private float bobSpeed = 2.5f;

    [Header("Zona de Proximidad")]
    [Tooltip("Radio de proximidad para recoger si el jugador está junto al objeto.")]
    [SerializeField] private float proximityRadius = 1.3f;

    public BroomPart Part => part;
    public int ScoreValue => scoreValue;

    private Collider2D col;
    private Vector3 initialPosition;
    private float messageCooldownTimer = 0f;
    private bool isCollected = false;

    private void Awake()
    {
        col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;

        if (bobAmplitude > 0.05f) bobAmplitude = 0.04f;

        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            sr.sortingLayerName = "Foreground";
            sr.sortingOrder = 10;
        }

        Vector3 pos = transform.position;
        if (pos.y > 0.35f && pos.y < 1.25f)
        {
            pos.y = 0.44f;
            transform.position = pos;
        }
        initialPosition = transform.position;

        int colLayer = LayerMask.NameToLayer("Collectible");
        if (colLayer >= 0)
        {
            gameObject.layer = colLayer;
        }

        int playerLayer = LayerMask.NameToLayer("Player");
        if (colLayer >= 0 && playerLayer >= 0)
        {
            Physics2D.IgnoreLayerCollision(playerLayer, colLayer, false);
        }
    }

    private void Update()
    {
        if (messageCooldownTimer > 0f)
        {
            messageCooldownTimer -= Time.deltaTime;
        }

        // Flotación suave
        float newY = initialPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
        transform.position = new Vector3(initialPosition.x, newY, initialPosition.z);

        // Detección por proximidad complementaria si la física aún no disparó el trigger
        if (!isCollected)
        {
            CheckProximity();
        }
    }

    private void CheckProximity()
    {
        if (isCollected) return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, proximityRadius);
        for (int i = 0; i < hits.Length; i++)
        {
            if (isCollected) break;
            if (hits[i] == null) continue;

            PlayerController pc = hits[i].GetComponentInParent<PlayerController>();
            if (pc != null)
            {
                TryProcessCollection(hits[i]);
                break;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryProcessCollection(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryProcessCollection(other);
    }

    private void TryProcessCollection(Collider2D other)
    {
        if (isCollected) return;

        TransformationManager tm = other.GetComponentInParent<TransformationManager>();
        PlayerController pc = other.GetComponentInParent<PlayerController>();

        if (pc == null) return;

        // Recoger objeto tanto en forma humana como en forma de gato
        isCollected = true;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(scoreValue);
        }

        if (AudioManager.Instance != null)
        {
            if (pickupSfx != null)
            {
                AudioManager.Instance.PlaySFX(pickupSfx);
            }
            else
            {
                AudioManager.Instance.PlayButtonClickSound();
            }
        }

        if (pickupVfx != null)
        {
            Instantiate(pickupVfx, transform.position, Quaternion.identity);
        }

        if (BroomInventory.Instance != null)
        {
            BroomInventory.Instance.Collect(part);
        }

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, proximityRadius);
    }
}
