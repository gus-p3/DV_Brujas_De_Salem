using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Salem.Level4
{
    /// <summary>
    /// Ventana gótica alta con barrotes rotos: solo la forma de gato cabe. Con [Q] en forma de gato
    /// la bruja pasa a la nave de la iglesia (se mueve al punto interior, cambian límites de cámara,
    /// checkpoint, luz ambiental y se apaga el patio con su tormenta).
    /// Con requireCat = false sirve de puerta/escalera entre los pisos de la torre (cualquier forma pasa).
    /// </summary>
    public class CathedralHighWindowEntry : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private Transform interiorSpawnPoint;
        [SerializeField] private GameObject courtyardSection;
        [SerializeField] private GameObject churchInteriorSection;
        [SerializeField] private string humanHintMessage = "Los barrotes rotos son muy estrechos para una humana. Transfórmate en gato [T] para colarte.";
        [SerializeField] private string catPrompt = "[Q] Deslizarse por los barrotes al interior de la iglesia";
        [SerializeField] private bool requireCat = true;
        [SerializeField] private string arrivalMessage = "La nave de la iglesia. Pisa las alfombras y evita las duelas flojas: el Inquisidor tiene oído agudo.";

        [Header("Interior")]
        [SerializeField] private Vector2 interiorCameraMin = new Vector2(99f, -1f);
        [SerializeField] private Vector2 interiorCameraMax = new Vector2(151f, 12f);
        [SerializeField] private Light2D globalLight;
        [SerializeField] private Color interiorAmbientColor = new Color(0.55f, 0.45f, 0.38f, 1f);
        [SerializeField] private float interiorAmbientIntensity = 0.5f;
        [SerializeField] private AudioClip interiorMusic;

        [Header("FX / Visuals")]
        [SerializeField] private ParticleSystem enterFx;
        [SerializeField] private AudioClip enterSfx;

        private GameObject _player;
        private bool _lastWasCat;
        private bool _entered;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            _player = other.gameObject;
            _lastWasCat = !(IsCat() || !requireCat); // fuerza mostrar el aviso en el primer Update
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            _player = null;
            Level2.InteractionPromptUI.Instance?.HidePrompt();
        }

        private bool IsCat()
        {
            var tm = _player != null ? _player.GetComponent<TransformationManager>() : null;
            return tm != null && tm.IsCatForm;
        }

        private void Update()
        {
            if (_player == null || _entered) return;

            bool isCat = IsCat() || !requireCat;
            if (isCat != _lastWasCat)
            {
                _lastWasCat = isCat;
                Level2.InteractionPromptUI.Instance?.ShowPrompt(isCat ? catPrompt : humanHintMessage, transform, 1.6f);
            }

            if (Input.GetKeyDown(Level4Bootstrap.InteractKey))
            {
                if (isCat) EnterCathedral();
                else FindFirstObjectByType<HUDController>()?.ShowMessage(humanHintMessage, 2.5f);
            }
        }

        private void EnterCathedral()
        {
            _entered = true;
            // Apagar el patio desactiva este disparador y Unity llama OnTriggerExit2D al instante (borra _player)
            GameObject player = _player;
            Level2.InteractionPromptUI.Instance?.HidePrompt();
            if (enterFx != null) enterFx.Play();
            if (enterSfx != null) AudioManager.Instance?.PlaySFX(enterSfx);

            if (churchInteriorSection != null) churchInteriorSection.SetActive(true);
            // Antes de tocar la luz: el OnDisable de la tormenta restaura la intensidad del patio.
            // El método sigue ejecutándose aunque este objeto (dentro del patio) quede inactivo.
            if (courtyardSection != null) courtyardSection.SetActive(false);

            if (interiorSpawnPoint != null && player != null)
            {
                var rb = player.GetComponent<Rigidbody2D>();
                if (rb != null) rb.linearVelocity = Vector2.zero;
                player.transform.position = interiorSpawnPoint.position;
                Physics2D.SyncTransforms();
                CheckpointManager.Instance?.SetCheckpoint(interiorSpawnPoint.position);
            }

            var cam = FindFirstObjectByType<CameraFollow>();
            if (cam != null)
            {
                cam.SetLimits(interiorCameraMin, interiorCameraMax);
                cam.SnapToTarget();
            }

            if (globalLight != null)
            {
                globalLight.color = interiorAmbientColor;
                globalLight.intensity = interiorAmbientIntensity;
            }

            if (interiorMusic != null) AudioManager.Instance?.PlayMusic(interiorMusic);
            if (!string.IsNullOrEmpty(arrivalMessage)) FindFirstObjectByType<HUDController>()?.ShowMessage(arrivalMessage, 4f);
        }
    }
}
